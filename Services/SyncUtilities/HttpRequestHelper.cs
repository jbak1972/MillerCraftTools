using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Miller_Craft_Tools.Utils;

namespace Miller_Craft_Tools.Services.SyncUtilities
{
    /// <summary>
    /// Helper class for handling HTTP requests to the Miller Craft Assistant API
    /// </summary>
    public class HttpRequestHelper
    {
        private readonly AuthenticationService _authService;
        private readonly CancellationToken _cancellationToken;
        
        /// <summary>
        /// Creates a new instance of HttpRequestHelper
        /// </summary>
        /// <param name="authService">Authentication service for token management</param>
        /// <param name="cancellationToken">Cancellation token for cancelling operations</param>
        public HttpRequestHelper(
            AuthenticationService authService, 
            CancellationToken cancellationToken = default)
        {
            _authService = authService;
            _cancellationToken = cancellationToken;
        }
        
        /// <summary>
        /// Sends a JSON request to the specified endpoint with automatic retry for transient errors
        /// </summary>
        /// <param name="endpoint">API endpoint to send the request to</param>
        /// <param name="jsonContent">JSON content to send</param>
        /// <param name="token">Authentication token</param>
        /// <returns>Response as string</returns>
        public async Task<string> SendJsonRequestAsync(string endpoint, string jsonContent, string token)
        {
            // Use RetryHelper to automatically retry on transient errors with exponential backoff
            return await RetryHelper.ExecuteWithRetryAsync(
                async () => await SendJsonRequestInternalAsync(endpoint, jsonContent, token),
                maxRetryCount: 3,             // Try up to 3 times
                initialDelayMs: 1000,         // Start with 1 second delay
                maxDelayMs: 10000,            // Maximum 10 second delay
                cancellationToken: _cancellationToken);
        }
        
        /// <summary>
        /// Internal implementation of sending a JSON request without retry logic
        /// </summary>
        private async Task<string> SendJsonRequestInternalAsync(string endpoint, string jsonContent, string token)
        {
            // Create a custom timeout for this operation - 2 minutes is reasonable for sync operations
            using (var httpClient = AuthenticationService.CreateAuthenticatedHttpClient(token))
            {
                // Set a reasonable timeout to prevent hanging indefinitely
                httpClient.Timeout = TimeSpan.FromMinutes(2);
                
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                try
                {
                    // Add logging to help diagnose issues
                    Logger.LogJson(new { Action = "Sending Request", Endpoint = endpoint, ContentLength = jsonContent.Length }, "http_request");
                    
                    var response = await httpClient.PostAsync(endpoint, content, _cancellationToken);
                    
                    // Log the response status
                    Logger.LogJson(new { Action = "Received Response", Endpoint = endpoint, Status = response.StatusCode }, "http_response");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        
                        // Log response size but not the content for privacy
                        Logger.LogJson(new { Action = "Response Content", ContentLength = responseContent?.Length ?? 0 }, "http_response");
                        
                        return responseContent;
                    }
                    
                    // Handle specific error codes
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        Logger.LogError($"Authentication failed: {errorContent}");
                        throw new UnauthorizedAccessException("Authentication failed. Please log in again.");
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.RequestTimeout ||
                             response.StatusCode == System.Net.HttpStatusCode.GatewayTimeout)
                    {
                        throw new TimeoutException($"Request timed out with status code: {response.StatusCode}");
                    }
                    
                    // For other error statuses, include the response body in the error
                    string responseBody = await response.Content.ReadAsStringAsync();
                    Logger.LogError($"Request failed with status code: {response.StatusCode}, body: {responseBody}");
                    
                    // Use NetworkErrorLogger to provide enhanced error details
                    NetworkErrorLogger.LogNetworkException(
                        new HttpRequestException($"Request failed with status code: {response.StatusCode}"),
                        endpoint, 
                        "HTTP Request");
                        
                    throw new HttpRequestException($"Request failed with status code: {response.StatusCode}. Server message: {responseBody}");
                }
                catch (TaskCanceledException ex)
                {
                    if (_cancellationToken.IsCancellationRequested)
                    {
                        Logger.LogError("Request was canceled by user");
                        throw new OperationCanceledException("Request was canceled", ex, _cancellationToken);
                    }
                    
                    Logger.LogError("Request timed out");
                    NetworkErrorLogger.LogNetworkException(ex, endpoint, "HTTP Request Timeout");
                    throw new TimeoutException("The request timed out. Please check your network connection and try again.", ex);
                }
                catch (HttpRequestException ex)
                {
                    Logger.LogError($"Network error: {ex.Message}");
                    NetworkErrorLogger.LogNetworkException(ex, endpoint, "HTTP Request");
                    throw new SyncNetworkException("Unable to connect to Miller Craft Assistant. Please check your network connection.", ex);
                }
            }
        }
        
        /// <summary>
        /// Gets a valid authentication token, refreshing if necessary
        /// </summary>
        /// <returns>Valid authentication token</returns>
        public async Task<string> GetValidTokenAsync()
        {
            return await _authService.GetValidTokenAsync(_cancellationToken);
        }
        
        /// <summary>
        /// Tests connectivity to the API test endpoint
        /// GET /api/revit/test - works with or without authentication
        /// </summary>
        /// <param name="token">Optional authentication token to test</param>
        /// <returns>True if connectivity test succeeds</returns>
        public async Task<bool> TestConnectivityAsync(string token = null)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(30);
                    
                    // Add token if provided
                    if (!string.IsNullOrEmpty(token))
                    {
                        httpClient.DefaultRequestHeaders.Add("X-Revit-Token", token);
                    }
                    
                    var response = await httpClient.GetAsync("https://app.millercraftllc.com/api/revit/test", _cancellationToken);
                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Connectivity test failed: {ex.Message}");
                return false;
            }
        }
    }
}
