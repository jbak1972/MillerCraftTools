using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
// Using shared types from ApiTestingTypes.cs

namespace Miller_Craft_Tools.Utils
{
    /// <summary>
    /// Comprehensive API connectivity tester for Miller Craft Tools web app
    /// </summary>
    public class ApiConnectivityTester
    {
        private const string DEFAULT_BASE_URL = "https://app.millercraftllc.com";
        private const string TEST_ENDPOINT = "/api/revit/test";
        
        /// <summary>
        /// Tests connectivity to the API with detailed diagnostics
        /// </summary>
        /// <param name="baseUrl">Base URL of the API (defaults to app.millercraftllc.com)</param>
        /// <param name="token">Optional auth token - test will run with and without if provided</param>
        /// <returns>Detailed API test result</returns>
        public static async Task<Miller_Craft_Tools.Utils.ApiTestResult> TestApiConnectivityAsync(string baseUrl = DEFAULT_BASE_URL, string token = null)
        {
            var result = new Miller_Craft_Tools.Utils.ApiTestResult
            {
                BaseUrl = baseUrl,
                TestEndpoint = TEST_ENDPOINT,
                HasToken = !string.IsNullOrEmpty(token),
                Timestamp = DateTime.Now
            };
            
            // Test 1: Basic connectivity without auth
            result.UnauthenticatedTestResult = await TestEndpointAsync(baseUrl + TEST_ENDPOINT);
            
            // Test 2: With auth if token provided
            if (!string.IsNullOrEmpty(token))
            {
                result.AuthenticatedTestResult = await TestEndpointWithTokenAsync(baseUrl + TEST_ENDPOINT, token);
            }
            
            return result;
        }
        
        /// <summary>
        /// Test an endpoint without authentication
        /// </summary>
        private static async Task<Miller_Craft_Tools.Utils.EndpointTestResult> TestEndpointAsync(string url)
        {
            var result = new Miller_Craft_Tools.Utils.EndpointTestResult
            {
                Url = url,
                IsAuthenticated = false
            };
            
            try
            {
                using (var httpClient = new HttpClient())
                {
                    // Set default headers
                    httpClient.DefaultRequestHeaders.Accept.Add(
                        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "MillerCraftTools/1.0");
                    
                    // Capture timing
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var response = await httpClient.GetAsync(url);
                    watch.Stop();
                    
                    // Capture result data
                    result.StatusCode = response.StatusCode;
                    result.IsSuccessful = response.IsSuccessStatusCode;
                    result.ResponseTimeMs = watch.ElapsedMilliseconds;
                    result.ResponseContent = await response.Content.ReadAsStringAsync();
                    
                    // Capture response headers
                    foreach (var header in response.Headers)
                    {
                        result.Headers.Add(header.Key, string.Join(", ", header.Value));
                    }
                    
                    // Try to parse detailed diagnostic info if successful
                    if (response.IsSuccessStatusCode)
                    {
                        try
                        {
                            var diagnosticInfo = JsonConvert.DeserializeObject<Miller_Craft_Tools.Utils.ApiDiagnosticInfo>(result.ResponseContent);
                            result.DiagnosticInfo = diagnosticInfo;
                        }
                        catch {}
                    }
                }
            }
            catch (Exception ex)
            {
                result.IsSuccessful = false;
                result.ErrorMessage = ex.Message;
                result.Exception = ex;
            }
            
            return result;
        }
        
        /// <summary>
        /// Test an endpoint with authentication token
        /// </summary>
        private static async Task<Miller_Craft_Tools.Utils.EndpointTestResult> TestEndpointWithTokenAsync(string url, string token)
        {
            var result = new Miller_Craft_Tools.Utils.EndpointTestResult
            {
                Url = url,
                IsAuthenticated = true
            };
            
            try
            {
                using (var httpClient = new HttpClient())
                {
                    // Set default headers
                    httpClient.DefaultRequestHeaders.Accept.Add(
                        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "MillerCraftTools/1.0");
                    
                    // Add auth header
                    httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    
                    // Capture timing
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var response = await httpClient.GetAsync(url);
                    watch.Stop();
                    
                    // Capture result data
                    result.StatusCode = response.StatusCode;
                    result.IsSuccessful = response.IsSuccessStatusCode;
                    result.ResponseTimeMs = watch.ElapsedMilliseconds;
                    result.ResponseContent = await response.Content.ReadAsStringAsync();
                    
                    // Capture response headers
                    foreach (var header in response.Headers)
                    {
                        result.Headers.Add(header.Key, string.Join(", ", header.Value));
                    }
                    
                    // Try to parse detailed diagnostic info if successful
                    if (response.IsSuccessStatusCode)
                    {
                        try
                        {
                            var diagnosticInfo = JsonConvert.DeserializeObject<Miller_Craft_Tools.Utils.ApiDiagnosticInfo>(result.ResponseContent);
                            result.DiagnosticInfo = diagnosticInfo;
                        }
                        catch {}
                    }
                }
            }
            catch (Exception ex)
            {
                result.IsSuccessful = false;
                result.ErrorMessage = ex.Message;
                result.Exception = ex;
            }
            
            return result;
        }
        
        /// <summary>
        /// Generate a detailed report of the API connectivity test results
        /// </summary>
        public static string GenerateTestReport(Miller_Craft_Tools.Utils.ApiTestResult testResult)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("=== API CONNECTIVITY TEST REPORT ===");
            sb.AppendLine($"Timestamp: {testResult.Timestamp}");
            sb.AppendLine($"Base URL: {testResult.BaseUrl}");
            sb.AppendLine($"Test Endpoint: {testResult.TestEndpoint}");
            sb.AppendLine($"Auth Token Provided: {(testResult.HasToken ? "Yes" : "No")}");
            sb.AppendLine();
            
            // Unauthenticated test results
            sb.AppendLine("--- UNAUTHENTICATED TEST ---");
            AppendEndpointTestReport(sb, testResult.UnauthenticatedTestResult);
            
            // Authenticated test results (if token was provided)
            if (testResult.HasToken && testResult.AuthenticatedTestResult != null)
            {
                sb.AppendLine();
                sb.AppendLine("--- AUTHENTICATED TEST ---");
                AppendEndpointTestReport(sb, testResult.AuthenticatedTestResult);
            }
            
            sb.AppendLine();
            sb.AppendLine("=== SUMMARY ===");
            bool basicConnectivity = testResult.UnauthenticatedTestResult?.IsSuccessful == true;
            bool authSuccess = testResult.HasToken && testResult.AuthenticatedTestResult?.IsSuccessful == true;
            
            sb.AppendLine($"Basic Connectivity: {(basicConnectivity ? "SUCCESS" : "FAILED")}");
            if (testResult.HasToken)
            {
                sb.AppendLine($"Authenticated Access: {(authSuccess ? "SUCCESS" : "FAILED")}");
            }
            
            sb.AppendLine();
            sb.AppendLine("=== RECOMMENDATIONS ===");
            if (!basicConnectivity)
            {
                sb.AppendLine("- Check network connectivity and firewall settings");
                sb.AppendLine("- Verify the base URL is correct");
                sb.AppendLine("- Ensure the API server is running");
            }
            else if (testResult.HasToken && !authSuccess)
            {
                sb.AppendLine("- Authentication is failing. Check if the token is valid");
                sb.AppendLine("- Try generating a new token from the API Token Management dialog");
                sb.AppendLine("- Verify the token format is correct");
            }
            else
            {
                sb.AppendLine("- All tests passed. API connectivity is working correctly.");
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Helper method to append endpoint test details to the report
        /// </summary>
        private static void AppendEndpointTestReport(StringBuilder sb, Miller_Craft_Tools.Utils.EndpointTestResult result)
        {
            if (result == null)
            {
                sb.AppendLine("No test result available");
                return;
            }
            
            sb.AppendLine($"Status: {(result.IsSuccessful ? "SUCCESS" : "FAILED")}");
            sb.AppendLine($"HTTP Status Code: {(int)result.StatusCode} ({result.StatusCode})");
            sb.AppendLine($"Response Time: {result.ResponseTimeMs}ms");
            
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                sb.AppendLine($"Error: {result.ErrorMessage}");
            }
            
            if (result.DiagnosticInfo != null)
            {
                sb.AppendLine("Diagnostic Info:");
                sb.AppendLine($"  Server: {result.DiagnosticInfo.ServerInfo}");
                sb.AppendLine($"  API Version: {result.DiagnosticInfo.ApiVersion}");
                sb.AppendLine($"  Auth Status: {result.DiagnosticInfo.AuthenticationStatus}");
                sb.AppendLine($"  Client IP: {result.DiagnosticInfo.ClientIpAddress}");
            }
            
            // Include truncated response if available
            if (!string.IsNullOrEmpty(result.ResponseContent))
            {
                string content = result.ResponseContent;
                if (content.Length > 500)
                {
                    content = content.Substring(0, 500) + "... (truncated)";
                }
                
                sb.AppendLine("Response Content:");
                sb.AppendLine(content);
            }
        }
    }
}
