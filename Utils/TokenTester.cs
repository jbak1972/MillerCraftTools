using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Miller_Craft_Tools.Services;
using Miller_Craft_Tools.Model;
using Newtonsoft.Json;

namespace Miller_Craft_Tools.Utils
{
    /// <summary>
    /// Utility class for testing API token functionality
    /// </summary>
    public class TokenTester
    {
        private readonly ApiTokenService _apiTokenService;
        private const string BaseUrl = "https://app.millercraftllc.com";
        
        public TokenTester()
        {
            _apiTokenService = new ApiTokenService();
        }
        
        /// <summary>
        /// Tests if the stored token is valid by making an API request
        /// </summary>
        /// <param name="progress">Optional progress reporter to receive status updates</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Result of the token validation test</returns>
        public async Task<TokenTestResult> TestTokenAsync(IProgress<string> progress = null, CancellationToken cancellationToken = default)
        {
            var result = new TokenTestResult();
            
            try
            {
                // Check if we have a token
                progress?.Report("Checking for API token...");
                string token = _apiTokenService.GetToken();
                if (string.IsNullOrEmpty(token))
                {
                    result.Success = false;
                    result.Message = "No API token found. Please add a token using the API Token Management dialog.";
                    return result;
                }
                
                // Check if cancellation was requested
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException("API token test was canceled.");
                }
                
                // Get HttpClient with token auth
                using (var httpClient = _apiTokenService.CreateAuthenticatedHttpClient())
                {
                    if (httpClient == null)
                    {
                        result.Success = false;
                        result.Message = "Failed to create authenticated HTTP client.";
                        return result;
                    }
                    
                    // Test token validation endpoint first
                    progress?.Report("Testing token validation endpoint...");
                    var validationResponse = await httpClient.GetAsync(BaseUrl + "/api/tokens/validate");
                    
                    // Check for cancellation
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    if (validationResponse.IsSuccessStatusCode)
                    {
                        result.TokenValid = true;
                        result.ValidationMessage = "Token validation successful";
                    }
                    else
                    {
                        result.TokenValid = false;
                        result.ValidationMessage = $"Token validation failed with status code: {validationResponse.StatusCode}";
                        result.Success = false;
                        result.Message = "API token is not valid or has expired.";
                        return result;
                    }
                    
                    // Test parameter mappings endpoint
                    progress?.Report("Testing parameter mappings endpoint...");
                    var mappingsResponse = await httpClient.GetAsync(BaseUrl + "/api/parameter-mappings");
                    
                    // Check for cancellation
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    if (mappingsResponse.IsSuccessStatusCode)
                    {
                        string responseContent = await mappingsResponse.Content.ReadAsStringAsync();
                        result.ParameterMappingsEndpointMessage = "Parameter mappings endpoint accessible";
                        result.ParameterMappingsResponseSample = TruncateResponse(responseContent);
                    }
                    else
                    {
                        result.ParameterMappingsEndpointMessage = $"Parameter mappings endpoint returned status code: {mappingsResponse.StatusCode}";
                    }
                    
                    // Test project-specific endpoint with a test GUID
                    progress?.Report("Testing project-specific endpoint...");
                    var projectGuid = "00000000-0000-0000-0000-000000000000"; // Test GUID
                    var projectResponse = await httpClient.GetAsync(BaseUrl + $"/api/projects/{projectGuid}/parameter-mappings");
                    
                    // Check for cancellation
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    if (projectResponse.IsSuccessStatusCode)
                    {
                        string responseContent = await projectResponse.Content.ReadAsStringAsync();
                        result.ProjectEndpointMessage = "Project-specific endpoint accessible";
                        result.ProjectEndpointResponseSample = TruncateResponse(responseContent);
                    }
                    else
                    {
                        // 404 is expected for a test GUID, but other errors indicate problems
                        if (projectResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            result.ProjectEndpointMessage = "Project endpoint returned 404 Not Found (expected for test GUID)";
                        }
                        else
                        {
                            result.ProjectEndpointMessage = $"Project endpoint returned status code: {projectResponse.StatusCode}";
                        }
                    }
                    
                    progress?.Report("Finalizing test results...");
                    result.Success = true;
                    result.Message = "API token testing completed successfully.";
                }
            }
            catch (HttpRequestException ex)
            {
                result.Success = false;
                result.Message = $"Network error during API test: {ex.Message}";
                progress?.Report($"Error: {result.Message}");
                Logger.LogError(result.Message, LogSeverity.Error);
            }
            catch (OperationCanceledException)
            {
                result.Success = false;
                result.Message = "API token test was canceled by user.";
                throw; // Rethrow to signal cancellation to caller
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error testing API token: {ex.Message}";
                Logger.LogError(result.Message, LogSeverity.Error);
            }
            
            return result;
        }
        
        /// <summary>
        /// Truncates a JSON response to a reasonable length for display
        /// </summary>
        private string TruncateResponse(string response, int maxLength = 200)
        {
            try
            {
                // Try to parse and format JSON for better display
                var parsedJson = JsonConvert.DeserializeObject(response);
                var formatted = JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
                
                if (formatted.Length <= maxLength)
                {
                    return formatted;
                }
                
                return formatted.Substring(0, maxLength) + "...";
            }
            catch
            {
                // If JSON parsing fails, just truncate the string
                if (response.Length <= maxLength)
                {
                    return response;
                }
                
                return response.Substring(0, maxLength) + "...";
            }
        }
    }
    
    /// <summary>
    /// Represents the result of an API token test
    /// </summary>
    public class TokenTestResult
    {
        /// <summary>
        /// Whether the overall test process completed without errors
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Overall status message
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// Whether the token validation endpoint accepted the token
        /// </summary>
        public bool TokenValid { get; set; }
        
        /// <summary>
        /// Message from token validation test
        /// </summary>
        public string ValidationMessage { get; set; }
        
        /// <summary>
        /// Message about parameter mappings endpoint test
        /// </summary>
        public string ParameterMappingsEndpointMessage { get; set; }
        
        /// <summary>
        /// Sample of parameter mappings response (truncated)
        /// </summary>
        public string ParameterMappingsResponseSample { get; set; }
        
        /// <summary>
        /// Message about project-specific endpoint test
        /// </summary>
        public string ProjectEndpointMessage { get; set; }
        
        /// <summary>
        /// Sample of project endpoint response (truncated)
        /// </summary>
        public string ProjectEndpointResponseSample { get; set; }
    }
}
