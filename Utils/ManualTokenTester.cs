using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Generic;
// Using shared types from ApiTestingTypes.cs

namespace Miller_Craft_Tools.Utils
{
    /// <summary>
    /// Utility for manual step-by-step testing of API endpoints and token authentication
    /// </summary>
    public class ManualTokenTester
    {
        private const string BaseUrl = "https://app.millercraftllc.com";
        private HttpClient _httpClient;
        private bool _useHardcodedToken = false;
        private string _hardcodedToken = null;
        
        // Delegates for logging events
        public delegate void LogMessageHandler(string message);
        public event LogMessageHandler OnLogMessage;
        public event LogMessageHandler OnLogError;

        public ManualTokenTester()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            // Default headers
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "MillerCraftTools/1.0");
        }

        /// <summary>
        /// Enables or disables using a hardcoded token for testing
        /// </summary>
        public void EnableHardcodedToken(bool enable, string token = null)
        {
            _useHardcodedToken = enable;
            _hardcodedToken = token;
            
            LogMessage($"Hardcoded token {(_useHardcodedToken ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Test simple endpoint without authentication
        /// </summary>
        public async Task<Miller_Craft_Tools.Utils.EndpointTestResult> TestUnauthenticatedEndpointAsync(string endpointPath)
        {
            string url = CombineUrl(BaseUrl, endpointPath);
            LogMessage($"Testing unauthenticated endpoint: {url}");
            
            try
            {
                // Remove authorization header if present
                if (_httpClient.DefaultRequestHeaders.Authorization != null)
                {
                    _httpClient.DefaultRequestHeaders.Remove("Authorization");
                }
                
                var response = await _httpClient.GetAsync(url);
                string content = await response.Content.ReadAsStringAsync();
                
                var result = new Miller_Craft_Tools.Utils.EndpointTestResult
                {
                    Url = url,
                    StatusCode = response.StatusCode,
                    IsSuccessful = response.IsSuccessStatusCode,
                    ResponseContent = content,
                    Headers = GetHeadersDictionary(response.Headers)
                };
                
                LogEndpointResult(result);
                return result;
            }
            catch (Exception ex)
            {
                LogError($"Error testing endpoint {url}: {ex.Message}");
                return new Miller_Craft_Tools.Utils.EndpointTestResult
                {
                    Url = url,
                    IsSuccessful = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Test endpoint with authentication
        /// </summary>
        public async Task<Miller_Craft_Tools.Utils.EndpointTestResult> TestAuthenticatedEndpointAsync(string endpointPath, string token = null)
        {
            string url = CombineUrl(BaseUrl, endpointPath);
            string authToken = GetAuthToken(token);
            
            LogMessage($"Testing authenticated endpoint: {url}");
            if (string.IsNullOrEmpty(authToken))
            {
                LogError("No token available for authentication test");
                return new Miller_Craft_Tools.Utils.EndpointTestResult
                {
                    Url = url,
                    IsSuccessful = false,
                    ErrorMessage = "No token available"
                };
            }

            try
            {
                // Add Authorization header
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", authToken);
                
                var response = await _httpClient.GetAsync(url);
                string content = await response.Content.ReadAsStringAsync();
                
                var result = new Miller_Craft_Tools.Utils.EndpointTestResult
                {
                    Url = url,
                    StatusCode = response.StatusCode,
                    IsSuccessful = response.IsSuccessStatusCode,
                    ResponseContent = content,
                    Headers = GetHeadersDictionary(response.Headers),
                    TokenUsed = authToken
                };
                
                LogEndpointResult(result);
                return result;
            }
            catch (Exception ex)
            {
                LogError($"Error testing endpoint {url} with authentication: {ex.Message}");
                return new Miller_Craft_Tools.Utils.EndpointTestResult
                {
                    Url = url,
                    IsSuccessful = false,
                    ErrorMessage = ex.Message,
                    TokenUsed = authToken
                };
            }
        }

        /// <summary>
        /// Run all API tests in sequence
        /// </summary>
        public async Task<List<Miller_Craft_Tools.Utils.EndpointTestResult>> RunSequentialTestsAsync()
        {
            var results = new List<Miller_Craft_Tools.Utils.EndpointTestResult>();
            
            LogMessage("Starting sequential API tests...");
            
            // Step 1: Test simple health endpoint without auth
            var healthResult = await TestUnauthenticatedEndpointAsync("/api/health");
            results.Add(healthResult);
            
            // Step 2: Test simple test endpoint without auth (if it exists)
            var testResult = await TestUnauthenticatedEndpointAsync("/api/revit/test");
            results.Add(testResult);
            
            // Step 3: Test token validation endpoint with auth
            var tokenResult = await TestAuthenticatedEndpointAsync("/api/tokens/validate");
            results.Add(tokenResult);
            
            // Step 4: Test parameter mappings endpoint with auth
            var mappingsResult = await TestAuthenticatedEndpointAsync("/api/parameter-mappings");
            results.Add(mappingsResult);
            
            // Step 5: Test project endpoint with auth (using test GUID)
            var projectGuid = "00000000-0000-0000-0000-000000000000"; // Test GUID
            var projectResult = await TestAuthenticatedEndpointAsync($"/api/projects/{projectGuid}/parameter-mappings");
            results.Add(projectResult);
            
            LogMessage($"Sequential API tests completed. {results.Count} tests run.");
            return results;
        }

        /// <summary>
        /// Get authentication token, using hardcoded if enabled
        /// </summary>
        private string GetAuthToken(string providedToken = null)
        {
            // Priority: 1. Provided token, 2. Hardcoded token, 3. Stored token
            if (!string.IsNullOrEmpty(providedToken))
            {
                return providedToken;
            }
            
            if (_useHardcodedToken && !string.IsNullOrEmpty(_hardcodedToken))
            {
                return _hardcodedToken;
            }
            
            // Fall back to stored token from ApiTokenService
            try
            {
                var tokenService = new Services.ApiTokenService();
                return tokenService.GetToken();
            }
            catch (Exception ex)
            {
                LogError($"Error retrieving token from ApiTokenService: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Format URL by combining base and path
        /// </summary>
        private string CombineUrl(string baseUrl, string path)
        {
            return $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
        }
        
        /// <summary>
        /// Convert headers to dictionary for easier display
        /// </summary>
        private Dictionary<string, string> GetHeadersDictionary(HttpHeaders headers)
        {
            var dict = new Dictionary<string, string>();
            
            foreach (var header in headers)
            {
                dict[header.Key] = string.Join(", ", header.Value);
            }
            
            return dict;
        }
        
        /// <summary>
        /// Log endpoint test results
        /// </summary>
        private void LogEndpointResult(Miller_Craft_Tools.Utils.EndpointTestResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Endpoint: {result.Url}");
            sb.AppendLine($"Success: {result.IsSuccessful}");
            
            if (result.StatusCode.HasValue)
            {
                sb.AppendLine($"Status Code: {(int)result.StatusCode} ({result.StatusCode})");
            }
            
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                sb.AppendLine($"Error: {result.ErrorMessage}");
            }
            
            if (result.Headers != null && result.Headers.Count > 0)
            {
                sb.AppendLine("Headers:");
                foreach (var header in result.Headers)
                {
                    sb.AppendLine($"  {header.Key}: {header.Value}");
                }
            }
            
            // Try to format JSON response for better readability
            if (!string.IsNullOrEmpty(result.ResponseContent))
            {
                try
                {
                    var parsedJson = JsonConvert.DeserializeObject(result.ResponseContent);
                    string prettyJson = JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
                    
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
                    // If not valid JSON, just show as is (truncated)
                    string content = result.ResponseContent;
                    if (content.Length > 1000)
                    {
                        content = content.Substring(0, 1000) + "... (truncated)";
                    }
                    
                    sb.AppendLine("Response Content (not JSON):");
                    sb.AppendLine(content);
                }
            }
            
            // Log the formatted result
            if (result.IsSuccessful)
            {
                LogMessage(sb.ToString());
            }
            else
            {
                LogError(sb.ToString());
            }
        }

        /// <summary>
        /// Log a message
        /// </summary>
        private void LogMessage(string message)
        {
            OnLogMessage?.Invoke(message);
            Logger.LogInfo(message);
        }

        /// <summary>
        /// Log an error
        /// </summary>
        private void LogError(string message)
        {
            OnLogError?.Invoke(message);
            Logger.LogError(message, LogSeverity.Warning);
        }
    }

    // Using shared EndpointTestResult class from ApiTestingTypes.cs
}
