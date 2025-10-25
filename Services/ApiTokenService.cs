using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Miller_Craft_Tools.Utils;
using Miller_Craft_Tools.Model;

namespace Miller_Craft_Tools.Services
{
    /// <summary>
    /// Manages API tokens for authentication with the Miller Craft web API
    /// </summary>
    public class ApiTokenService
    {
        // Constants for token storage
        private const string API_TOKEN_SETTING_KEY = "ApiToken";
        private const string API_TOKEN_EXPIRY_KEY = "ApiTokenExpiry";
        
        /// <summary>
        /// Stores an API token securely in user settings
        /// </summary>
        /// <param name="token">The API token to store</param>
        /// <param name="expiryDate">Optional expiry date for the token</param>
        public void StoreToken(string token, DateTime? expiryDate = null)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentNullException(nameof(token), "API token cannot be null or empty");
            }
            
            // Store token in settings
            var userSettings = UserSettings.Load();
            userSettings.ApiToken = token;
            
            // Store expiry if provided
            if (expiryDate.HasValue)
            {
                userSettings.TokenExpiration = expiryDate.Value.ToString("o");
            }
            
            userSettings.Save();
            
            // Log token storage (without the actual token)
            TelemetryLogger.LogInfo("New API token stored");
        }
        
        /// <summary>
        /// Retrieves the stored API token
        /// </summary>
        /// <returns>The stored API token, or null if no token is stored</returns>
        public string GetToken()
        {
            var userSettings = UserSettings.Load();
            return userSettings.ApiToken;
        }
        
        /// <summary>
        /// Checks if the stored token is valid (exists and not expired)
        /// </summary>
        /// <returns>True if the token is valid, false otherwise</returns>
        public bool IsTokenValid()
        {
            var userSettings = UserSettings.Load();
            
            // Check if token exists
            if (string.IsNullOrEmpty(userSettings.ApiToken))
            {
                return false;
            }
            
            // Check if token is expired
            if (!string.IsNullOrEmpty(userSettings.TokenExpiration))
            {
                if (DateTime.TryParse(userSettings.TokenExpiration, out DateTime expiryDate))
                {
                    // Add a buffer of 5 minutes to avoid using nearly expired tokens
                    return DateTime.UtcNow.AddMinutes(5) < expiryDate;
                }
            }
            
            // If no expiry is set, assume the token is valid
            return true;
        }
        
        /// <summary>
        /// Clears the stored API token
        /// </summary>
        public void ClearToken()
        {
            var userSettings = UserSettings.Load();
            userSettings.ApiToken = null;
            userSettings.TokenExpiration = null;
            userSettings.Save();
            
            TelemetryLogger.LogInfo("API token cleared");
        }
        
        /// <summary>
        /// Creates an HTTP client configured with the stored API token
        /// </summary>
        /// <returns>Configured HttpClient or null if no token is available</returns>
        public HttpClient CreateAuthenticatedHttpClient()
        {
            string token = GetToken();
            
            if (string.IsNullOrEmpty(token))
            {
                Logger.LogError("No API token available for authentication");
                return null;
            }
            
            var httpClient = new HttpClient();
            
            // Add Bearer token authentication
            httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);
            
            // Add Revit-specific token header
            httpClient.DefaultRequestHeaders.Add("X-Revit-Token", token);
            
            return httpClient;
        }
        
        /// <summary>
        /// Validates the token against the API
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>True if the token is valid according to the API</returns>
        public async Task<bool> ValidateTokenWithApiAsync(CancellationToken cancellationToken = default)
        {
            string token = GetToken();
            
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }
            
            try
            {
                using (var httpClient = CreateAuthenticatedHttpClient())
                {
                    if (httpClient == null)
                    {
                        return false;
                    }
                    
                    httpClient.Timeout = TimeSpan.FromSeconds(30);
                    
                    // Validate against the tokens endpoint
                    var response = await httpClient.GetAsync(
                        "https://app.millercraftllc.com/api/revit/tokens/validate", 
                        cancellationToken);
                    
                    // If we get a success response, the token is valid
                    return response.IsSuccessStatusCode;
                }
            }
            catch (HttpRequestException ex)
            {
                Logger.LogError($"Network error during token validation: {ex.Message}");
                return false;
            }
            catch (TaskCanceledException)
            {
                Logger.LogError("Token validation timed out or was canceled");
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error validating token: {ex.Message}");
                return false;
            }
        }
    }
    
    /// <summary>
    /// Model class for API token responses
    /// </summary>
    internal class ApiTokenResponse
    {
        [JsonProperty("token")]
        public string Token { get; set; }
        
        [JsonProperty("expiresAt")]
        public DateTime? ExpiresAt { get; set; }
        
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("createdAt")]
        public DateTime? CreatedAt { get; set; }
        
        [JsonProperty("permissions")]
        public string[] Permissions { get; set; }
    }
}
