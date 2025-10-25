using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Miller_Craft_Tools.Utils
{
    /// <summary>
    /// Provides simple API testing capabilities that work with or without authentication
    /// </summary>
    public class SimpleApiTester
    {
        private const string DEFAULT_BASE_URL = "https://app.millercraftllc.com";
        private const string TEST_ENDPOINT = "/api/revit/test";
        private const string HEALTH_ENDPOINT = "/api/health";

        /// <summary>
        /// Test a simple endpoint that accepts both authenticated and unauthenticated requests
        /// </summary>
        /// <param name="token">Optional authentication token</param>
        /// <param name="baseUrl">Base URL of the API (defaults to app.millercraftllc.com)</param>
        /// <returns>Test result with details about the request and response</returns>
        public static async Task<SimpleEndpointTestResult> TestSimpleEndpointAsync(string token = null, string baseUrl = DEFAULT_BASE_URL)
        {
            return await TestEndpointGetAsync(TEST_ENDPOINT, token, baseUrl);
        }

        /// <summary>
        /// Test the API health endpoint (typically unauthenticated)
        /// </summary>
        /// <param name="baseUrl">Base URL of the API (defaults to app.millercraftllc.com)</param>
        /// <returns>Test result with details about the request and response</returns>
        public static async Task<SimpleEndpointTestResult> TestHealthEndpointAsync(string baseUrl = DEFAULT_BASE_URL)
        {
            return await TestEndpointGetAsync(HEALTH_ENDPOINT, null, baseUrl);
        }

        /// <summary>
        /// Test any API endpoint with optional authentication using GET method
        /// </summary>
        /// <param name="endpoint">API endpoint path (e.g., /api/revit/test)</param>
        /// <param name="token">Optional authentication token</param>
        /// <param name="baseUrl">Base URL of the API</param>
        /// <returns>Test result with details about the request and response</returns>
        public static async Task<SimpleEndpointTestResult> TestEndpointGetAsync(string endpoint, string token = null, string baseUrl = DEFAULT_BASE_URL)
        {
            return await TestEndpointAsync(endpoint, token, "GET", null, baseUrl);
        }

        /// <summary>
        /// Test any API endpoint with optional authentication using specified HTTP method and optional request body
        /// </summary>
        /// <param name="endpoint">API endpoint path (e.g., /api/revit/test)</param>
        /// <param name="token">Optional authentication token</param>
        /// <param name="method">HTTP method (GET, POST, PUT, DELETE, etc.)</param>
        /// <param name="requestBody">Optional request body for POST/PUT requests</param>
        /// <param name="baseUrl">Base URL of the API</param>
        /// <returns>Test result with details about the request and response</returns>
        public static async Task<SimpleEndpointTestResult> TestEndpointAsync(
            string endpoint, 
            string token = null, 
            string method = "GET", 
            string requestBody = null, 
            string baseUrl = DEFAULT_BASE_URL)
        {
            string url = $"{baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";

            var result = new SimpleEndpointTestResult
            {
                Url = url,
                HttpMethod = method,
                TokenProvided = !string.IsNullOrEmpty(token),
                Timestamp = DateTime.Now,
                RequestBody = requestBody
            };

            try
            {
                using (var httpClient = new HttpClient())
                {
                    // Set default headers
                    httpClient.DefaultRequestHeaders.Accept.Add(
                        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "MillerCraftTools/1.0");

                    // Add authentication token if provided
                    // Per web app spec: X-Revit-Token is primary, Bearer is fallback
                    if (!string.IsNullOrEmpty(token))
                    {
                        httpClient.DefaultRequestHeaders.Add("X-Revit-Token", token);
                        httpClient.DefaultRequestHeaders.Authorization = 
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                        result.AuthHeader = $"X-Revit-Token: {token}, Bearer {token}";
                    }

                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    HttpResponseMessage response;

                    // Execute request based on HTTP method
                    switch (method.ToUpper())
                    {
                        case "POST":
                            HttpContent content = null;
                            if (!string.IsNullOrEmpty(requestBody))
                            {
                                content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json");
                            }
                            response = await httpClient.PostAsync(url, content);
                            break;

                        case "PUT":
                            HttpContent putContent = null;
                            if (!string.IsNullOrEmpty(requestBody))
                            {
                                putContent = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json");
                            }
                            response = await httpClient.PutAsync(url, putContent);
                            break;

                        case "DELETE":
                            response = await httpClient.DeleteAsync(url);
                            break;

                        case "GET":
                        default:
                            response = await httpClient.GetAsync(url);
                            break;
                    }

                    watch.Stop();

                    result.StatusCode = response.StatusCode;
                    result.IsSuccessful = response.IsSuccessStatusCode;
                    result.ResponseTime = watch.ElapsedMilliseconds;
                    result.ResponseContent = await response.Content.ReadAsStringAsync();

                    // Capture headers
                    foreach (var header in response.Headers)
                    {
                        result.Headers.Add(header.Key, string.Join(", ", header.Value));
                    }
                }
            }
            catch (Exception ex)
            {
                result.IsSuccessful = false;
                result.ErrorMessage = ex.Message;
                Logger.LogError($"Error testing endpoint {url} with method {method}: {ex.Message}", LogSeverity.Warning);
            }

            return result;
        }

        /// <summary>
        /// Run a sequence of API tests with and without authentication
        /// </summary>
        /// <param name="token">Optional authentication token</param>
        /// <param name="baseUrl">Base URL of the API</param>
        /// <returns>Collection of test results</returns>
        public static async Task<List<SimpleEndpointTestResult>> RunSequentialTestsAsync(string token = null, string baseUrl = DEFAULT_BASE_URL)
        {
            var results = new List<SimpleEndpointTestResult>();
            
            // Test 1: Health endpoint (unauthenticated)
            results.Add(await TestHealthEndpointAsync(baseUrl));
            
            // Test 2: Test endpoint (unauthenticated)
            results.Add(await TestEndpointAsync(TEST_ENDPOINT, null, baseUrl));
            
            // Test 3: Test endpoint (authenticated)
            if (!string.IsNullOrEmpty(token))
            {
                results.Add(await TestEndpointAsync(TEST_ENDPOINT, token, baseUrl));
            }
            
            return results;
        }
    }

    /// <summary>
    /// Represents the result of a simple API endpoint test
    /// </summary>
    public class SimpleEndpointTestResult
    {
        /// <summary>
        /// The URL that was tested
        /// </summary>
        public string Url { get; set; }
        
        /// <summary>
        /// The HTTP method used for the request (GET, POST, etc.)
        /// </summary>
        public string HttpMethod { get; set; } = "GET";
        
        /// <summary>
        /// Request body sent with POST/PUT requests (if any)
        /// </summary>
        public string RequestBody { get; set; }
        
        /// <summary>
        /// Whether a token was provided for authentication
        /// </summary>
        public bool TokenProvided { get; set; }
        
        /// <summary>
        /// The authorization header used (if any)
        /// </summary>
        public string AuthHeader { get; set; }
        
        /// <summary>
        /// When the test was executed
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Whether the request was successful
        /// </summary>
        public bool IsSuccessful { get; set; }
        
        /// <summary>
        /// HTTP status code
        /// </summary>
        public System.Net.HttpStatusCode StatusCode { get; set; }
        
        /// <summary>
        /// Time taken for the response in milliseconds
        /// </summary>
        public long ResponseTime { get; set; }
        
        /// <summary>
        /// Response content as string
        /// </summary>
        public string ResponseContent { get; set; }
        
        /// <summary>
        /// Error message if any
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// Response headers
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// Get a formatted summary of the test result
        /// </summary>
        public string GetSummary()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"API Endpoint Test: {Url}");
            sb.AppendLine($"Method: {HttpMethod}");
            sb.AppendLine($"Timestamp: {Timestamp}");
            sb.AppendLine($"Authentication: {(TokenProvided ? "Yes" : "No")}");
            sb.AppendLine($"Result: {(IsSuccessful ? "SUCCESS" : "FAILED")}");
            sb.AppendLine($"Status Code: {(int)StatusCode} ({StatusCode})");
            sb.AppendLine($"Response Time: {ResponseTime}ms");
            
            if (!string.IsNullOrEmpty(RequestBody))
            {
                string truncatedBody = RequestBody.Length > 100 ? RequestBody.Substring(0, 100) + "..." : RequestBody;
                sb.AppendLine($"Request Body: {truncatedBody}");
            }
            
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                sb.AppendLine($"Error: {ErrorMessage}");
            }
            
            if (!string.IsNullOrEmpty(ResponseContent))
            {
                try
                {
                    // Try to pretty-print if it's JSON
                    var parsedJson = Newtonsoft.Json.JsonConvert.DeserializeObject(ResponseContent);
                    string prettyJson = Newtonsoft.Json.JsonConvert.SerializeObject(parsedJson, Newtonsoft.Json.Formatting.Indented);
                    
                    // Truncate if too long
                    if (prettyJson.Length > 1000)
                    {
                        prettyJson = prettyJson.Substring(0, 1000) + "... (truncated)";
                    }
                    
                    sb.AppendLine("Response Content:");
                    sb.AppendLine(prettyJson);
                }
                catch
                {
                    // If not valid JSON, show as is (truncated)
                    string content = ResponseContent;
                    if (content.Length > 1000)
                    {
                        content = content.Substring(0, 1000) + "... (truncated)";
                    }
                    
                    sb.AppendLine("Response Content (not JSON):");
                    sb.AppendLine(content);
                }
            }
            
            return sb.ToString();
        }
    }
}
