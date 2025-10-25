using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Miller_Craft_Tools.Utils;
using System.Net.Http.Headers;
using Miller_Craft_Tools.Model;

namespace Miller_Craft_Tools.Services
{
    /// <summary>
    /// Handles authentication with the Miller Craft Assistant web application
    /// This class provides both token-based and OAuth2 authentication methods
    /// </summary>
    public class AuthenticationService
    {
        // Base API URL
        private const string BaseApiUrl = "https://app.millercraftllc.com/api";
        
        // Auth endpoints
        private const string TokenEndpoint = BaseApiUrl + "/revit/tokens";
        private const string RefreshEndpoint = BaseApiUrl + "/revit/tokens/refresh";
        private const string ValidateTokenEndpoint = BaseApiUrl + "/tokens/validate";
        
        // Default timeout for auth requests
        private const int DefaultTimeoutSeconds = 30;
        
        /// <summary>
        /// Authenticates a user with username and password using OAuth2
        /// </summary>
        /// <param name="username">User's username or email</param>
        /// <param name="password">User's password</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Authentication token if successful</returns>
        public async Task<string> Authenticate(
            string username, 
            string password, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Prepare authentication request data
                var authData = new
                {
                    username = username,
                    password = password
                };
                
                // Convert to JSON
                string authJson = JsonConvert.SerializeObject(authData);
                
                // Send authentication request
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds);
                    
                    var content = new StringContent(
                        authJson,
                        Encoding.UTF8,
                        "application/json");
                    
                    var response = await httpClient.PostAsync(
                        TokenEndpoint,
                        content,
                        cancellationToken);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var resultJson = await response.Content.ReadAsStringAsync();
                        var authResult = JsonConvert.DeserializeObject<AuthResult>(resultJson);
                        
                        if (authResult != null && !string.IsNullOrEmpty(authResult.Token))
                        {
                            // Store the token and refresh token in legacy settings
                            PluginSettings.SetToken(authResult.Token);
                            
                            if (!string.IsNullOrEmpty(authResult.RefreshToken))
                            {
                                PluginSettings.SetRefreshToken(authResult.RefreshToken);
                            }
                            
                            // Store expiration time if provided
                            if (authResult.ExpiresIn > 0)
                            {
                                DateTime expirationTime = DateTime.UtcNow.AddSeconds(authResult.ExpiresIn);
                                PluginSettings.SetTokenExpiration(expirationTime.ToString("o"));
                            }
                            
                            // Save all authentication data to UserSettings
                            var userSettings = UserSettings.Load();
                            userSettings.Username = username;
                            userSettings.ApiToken = authResult.Token;
                            userSettings.RefreshToken = authResult.RefreshToken;
                            userSettings.TokenExpiration = authResult.ExpiresIn > 0 
                                ? DateTime.UtcNow.AddSeconds(authResult.ExpiresIn).ToString("o")
                                : null;
                            userSettings.Save();
                            
                            // Log successful authentication with username (but don't log token details)
                            TelemetryLogger.LogInfo($"User {username} authenticated successfully");
                            
                            return authResult.Token;
                        }
                        
                        throw new InvalidOperationException("Authentication response did not contain a valid token");
                    }
                    
                    // Handle specific error responses
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        throw new UnauthorizedAccessException("Invalid username or password");
                    }
                    
                    throw new HttpRequestException($"Authentication failed with status code: {response.StatusCode}");
                }
            }
            catch (HttpRequestException ex)
            {
                Logger.LogError($"Network error during authentication: {ex.Message}");
                throw new SyncNetworkException("Unable to connect to Miller Craft Assistant. Please check your network connection.", ex);
            }
            catch (TaskCanceledException ex)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Logger.LogError("Authentication was canceled by user");
                    throw new OperationCanceledException("Authentication was canceled", ex, cancellationToken);
                }
                
                Logger.LogError("Authentication request timed out");
                throw new TimeoutException("Authentication request timed out. Please try again.", ex);
            }
            catch (Exception ex) when (!(ex is UnauthorizedAccessException || ex is SyncNetworkException || ex is OperationCanceledException))
            {
                Logger.LogError($"Authentication failed: {ex.Message}");
                throw new InvalidOperationException("Authentication failed. Please try again later.", ex);
            }
        }
        
        /// <summary>
        /// Refreshes the access token using the refresh token
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>True if refresh was successful</returns>
        public async Task<bool> RefreshToken(CancellationToken cancellationToken = default)
        {
            string refreshToken = PluginSettings.GetRefreshToken();
            
            if (string.IsNullOrEmpty(refreshToken))
            {
                Logger.LogError("No refresh token available");
                return false;
            }
            
            try
            {
                // Prepare refresh request data
                var refreshData = new
                {
                    refresh_token = refreshToken
                };
                
                // Convert to JSON
                string refreshJson = JsonConvert.SerializeObject(refreshData);
                
                // Send refresh request
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds);
                    
                    var content = new StringContent(
                        refreshJson,
                        Encoding.UTF8,
                        "application/json");
                    
                    var response = await httpClient.PostAsync(
                        RefreshEndpoint,
                        content,
                        cancellationToken);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var resultJson = await response.Content.ReadAsStringAsync();
                        var refreshResult = JsonConvert.DeserializeObject<AuthResult>(resultJson);
                        
                        if (refreshResult != null && !string.IsNullOrEmpty(refreshResult.Token))
                        {
                            // Store the new token in legacy settings
                            PluginSettings.SetToken(refreshResult.Token);
                            
                            // Update refresh token if provided
                            if (!string.IsNullOrEmpty(refreshResult.RefreshToken))
                            {
                                PluginSettings.SetRefreshToken(refreshResult.RefreshToken);
                            }
                            
                            // Store expiration time if provided
                            DateTime? expirationTime = null;
                            if (refreshResult.ExpiresIn > 0)
                            {
                                expirationTime = DateTime.UtcNow.AddSeconds(refreshResult.ExpiresIn);
                                PluginSettings.SetTokenExpiration(expirationTime.Value.ToString("o"));
                            }
                            
                            // Update UserSettings (preserving the existing Username)
                            var userSettings = UserSettings.Load();
                            userSettings.ApiToken = refreshResult.Token;
                            
                            if (!string.IsNullOrEmpty(refreshResult.RefreshToken))
                            {
                                userSettings.RefreshToken = refreshResult.RefreshToken;
                            }
                            
                            if (expirationTime.HasValue)
                            {
                                userSettings.TokenExpiration = expirationTime.Value.ToString("o");
                            }
                            
                            userSettings.Save();
                            
                            // Log successful token refresh
                            TelemetryLogger.LogInfo($"Token refreshed successfully for user: {userSettings.Username ?? "Unknown"}");
                            
                            return true;
                        }
                        
                        Logger.LogError("Refresh response did not contain a valid token");
                        return false;
                    }
                    
                    // If the refresh token is invalid or expired, we need to re-authenticate
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || 
                        response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        // Clear stored tokens to force re-authentication
                        PluginSettings.SetToken(string.Empty);
                        PluginSettings.SetRefreshToken(string.Empty);
                        PluginSettings.SetTokenExpiration(string.Empty);
                        
                        Logger.LogError("Refresh token is invalid or expired");
                        return false;
                    }
                    
                    Logger.LogError($"Token refresh failed with status code: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Token refresh failed: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Validates if the current token is valid and not expired
        /// </summary>
        /// <returns>True if token is valid</returns>
        public bool ValidateToken()
        {
            string token = PluginSettings.GetToken();
            
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }
            
            // Check if token is expired
            string expirationStr = PluginSettings.GetTokenExpiration();
            
            if (!string.IsNullOrEmpty(expirationStr))
            {
                if (DateTime.TryParse(expirationStr, out DateTime expirationTime))
                {
                    // Add buffer time (1 minute) to ensure we don't use a token that's about to expire
                    if (DateTime.UtcNow.AddMinutes(1) >= expirationTime)
                    {
                        Logger.LogError("Token has expired or is about to expire");
                        return false;
                    }
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Checks if the user is currently authenticated
        /// </summary>
        /// <returns>True if the user has a stored API token</returns>
        public bool IsAuthenticated()
        {
            var settings = UserSettings.Load();
            return !string.IsNullOrEmpty(settings.ApiToken);
        }
        
        /// <summary>
        /// Logs the user out by clearing authentication tokens from settings
        /// </summary>
        public void Logout()
        {
            try
            {
                // Load current settings
                var settings = UserSettings.Load();
                
                // Capture username for logging before clearing
                string username = settings.Username;
                
                // Clear authentication tokens and user info
                settings.Username = null;
                settings.ApiToken = null;
                settings.RefreshToken = null;
                settings.TokenExpiration = null;
                settings.WebSessionCookie = null;
                
                // Save changes
                settings.Save();
                
                // Also clear legacy settings
                PluginSettings.SetToken(string.Empty);
                PluginSettings.SetRefreshToken(string.Empty);
                PluginSettings.SetTokenExpiration(string.Empty);
                
                // Log the action
                if (!string.IsNullOrEmpty(username))
                {
                    TelemetryLogger.LogInfo($"User {username} logged out successfully");
                }
                else
                {
                    TelemetryLogger.LogInfo("User logged out successfully");
                }
            }
            catch (Exception ex)
            {
                // Log any errors
                TelemetryLogger.LogError("Error during logout", ex);
                throw;
            }
        }
        
        /// <summary>
        /// Validates the token with the server
        /// </summary>
        /// <param name="token">Token to validate</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>True if the token is valid according to the server</returns>
        public async Task<bool> ValidateTokenWithServerAsync(string token, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }
            
            try
            {
                // Create HTTP client with the token
                using (var httpClient = CreateAuthenticatedHttpClient(token))
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds);
                    
                    // Send GET request to validation endpoint
                    var response = await httpClient.GetAsync(
                        ValidateTokenEndpoint, 
                        cancellationToken);
                    
                    // 200 OK means the token is valid
                    if (response.IsSuccessStatusCode)
                    {
                        var resultJson = await response.Content.ReadAsStringAsync();
                        var validationResult = JsonConvert.DeserializeObject<TokenValidationResult>(resultJson);
                        
                        // Log successful validation but not the token itself
                        TelemetryLogger.LogInfo("Token validated successfully");
                        
                        return validationResult?.Valid ?? false;
                    }
                    
                    // 401 or any other status means the token is invalid
                    Logger.LogError($"Token validation failed with status code: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Token validation error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Gets the current token, refreshing it if necessary
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Valid token or null if unable to get a valid token</returns>
        public async Task<string> GetValidTokenAsync(CancellationToken cancellationToken = default)
        {
            // Get the current token
            string token = PluginSettings.GetToken();
            
            // For simple API tokens (Miller Craft web app), just return the token if it exists
            // These tokens don't expire and don't need server validation on every request
            if (!string.IsNullOrEmpty(token))
            {
                return token;
            }
            
            // No token available - user needs to set one in the Web App Integration dialog
            Logger.LogInfo("No API token available - user needs to configure token");
            return null;
        }
        
        /// <summary>
        /// Sets up HTTP client with authentication headers per REVIT_PLUGIN_INTEGRATION_PROMPT.md spec
        /// Uses X-Revit-Token as primary header format
        /// </summary>
        /// <param name="token">Authentication token</param>
        /// <returns>Configured HttpClient</returns>
        public static HttpClient CreateAuthenticatedHttpClient(string token)
        {
            var httpClient = new HttpClient();
            
            if (!string.IsNullOrEmpty(token))
            {
                // Per web app spec: X-Revit-Token is the recommended header format
                httpClient.DefaultRequestHeaders.Add("X-Revit-Token", token);
                
                // Also add Authorization: Bearer as fallback for compatibility
                // (both formats are supported per spec)
                httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", token);
            }
            
            return httpClient;
        }
        
        /// <summary>
        /// Validates a token using the test endpoint (GET /api/revit/test)
        /// This is the recommended method per web app spec
        /// </summary>
        /// <param name="token">Token to validate</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>True if token is valid</returns>
        public async Task<bool> ValidateTokenWithTestEndpoint(
            string token,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(token))
                return false;
                
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds);
                    httpClient.DefaultRequestHeaders.Add("X-Revit-Token", token);
                    
                    var response = await httpClient.GetAsync(
                        "https://app.millercraftllc.com/api/revit/test",
                        cancellationToken);
                    
                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Token validation failed: {ex.Message}");
                return false;
            }
        }
    }
    
    /// <summary>
    /// Model class for authentication responses
    /// </summary>
    internal class AuthResult
    {
        [JsonProperty("token")]
        public string Token { get; set; }
        
        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }
        
        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }
    }
    
    /// <summary>
    /// Model class for token validation responses
    /// </summary>
    internal class TokenValidationResult
    {
        [JsonProperty("valid")]
        public bool Valid { get; set; }
        
        [JsonProperty("message")]
        public string Message { get; set; }
        
        [JsonProperty("token_info")]
        public TokenInfo TokenInfo { get; set; }
    }
    
    /// <summary>
    /// Information about a token from validation
    /// </summary>
    internal class TokenInfo
    {
        [JsonProperty("userId")]
        public string UserId { get; set; }
        
        [JsonProperty("expires")]
        public string Expires { get; set; }
    }
}
