using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Miller_Craft_Tools.Model;
using Miller_Craft_Tools.Utils;
using Miller_Craft_Tools.UI.Controls;
using Miller_Craft_Tools.UI.Styles;

namespace Miller_Craft_Tools.Services
{
    /// <summary>
    /// Helper class for authentication UI operations
    /// </summary>
    public class AuthenticationUIHelper
    {
        private readonly AuthenticationService _authService;
        private readonly AuthStatusControl _statusControl;
        private CancellationToken _cancellationToken;

        /// <summary>
        /// Creates a new authentication UI helper
        /// </summary>
        /// <param name="authService">Authentication service to use</param>
        /// <param name="statusControl">Status control for displaying feedback</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public AuthenticationUIHelper(
            AuthenticationService authService, 
            AuthStatusControl statusControl,
            CancellationToken cancellationToken)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _statusControl = statusControl ?? throw new ArgumentNullException(nameof(statusControl));
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Attempts to authenticate a user with the provided credentials
        /// </summary>
        /// <param name="username">Username to authenticate</param>
        /// <param name="password">Password to authenticate</param>
        /// <returns>True if authentication succeeds</returns>
        public async Task<bool> AuthenticateAsync(string username, string password)
        {
            try
            {
                // Update status UI
                _statusControl.SetStatusMessage("Logging in...", Color.DarkGray);
                _statusControl.SetStatus(Miller_Craft_Tools.UI.Styles.SyncStatus.Processing, "Authenticating");
                
                // Log authentication attempt (without password)
                TelemetryLogger.LogInfo($"Authentication attempt for user: {username}");
                TelemetryLogger.StartTimer("Authentication");
                
                // Attempt to authenticate
                string token = await _authService.Authenticate(username, password, _cancellationToken);
                
                // Record authentication performance
                TelemetryLogger.StopTimerAndRecord("Authentication");
                
                if (!string.IsNullOrEmpty(token))
                {
                    // Login successful
                    _statusControl.SetStatusMessage("Login successful!", BrandColors.SuccessColor);
                    _statusControl.SetStatus(Miller_Craft_Tools.UI.Styles.SyncStatus.Complete);
                    
                    // Log success (without tokens)
                    TelemetryLogger.LogInfo("Authentication successful");
                    return true;
                }
                else
                {
                    // Login failed
                    _statusControl.SetStatusMessage("Login failed. Please check your credentials.", BrandColors.ErrorColor);
                    _statusControl.SetStatus(Miller_Craft_Tools.UI.Styles.SyncStatus.Error, "Failed");
                    
                    // Log failure
                    TelemetryLogger.LogWarning("Authentication failed: Invalid credentials");
                    return false;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                // Invalid credentials
                _statusControl.SetStatusMessage(ex.Message, Color.Red);
                return false;
            }
            catch (Exception ex)
            {
                // Authentication exception
                _statusControl.SetStatusMessage($"Login error: {ex.Message}", BrandColors.ErrorColor);
                _statusControl.SetStatus(Miller_Craft_Tools.UI.Styles.SyncStatus.Error, "Error");
                
                // Log error
                TelemetryLogger.LogError("Authentication error", ex);
                return false;
            }
        }

        /// <summary>
        /// Attempts to refresh the token
        /// </summary>
        /// <returns>True if refresh was successful</returns>
        public async Task<bool> RefreshTokenAsync()
        {
            try
            {
                // Update status
                _statusControl.SetStatusMessage("Refreshing authentication...", Color.DarkGray);
                _statusControl.SetStatus(Miller_Craft_Tools.UI.Styles.SyncStatus.Processing, "Refreshing");
                
                // Log refresh attempt
                TelemetryLogger.LogInfo("Attempting to refresh token");
                TelemetryLogger.StartTimer("TokenRefresh");
                
                // Attempt to refresh the token
                bool refreshed = await _authService.RefreshToken(_cancellationToken);
                
                // Record refresh performance
                TelemetryLogger.StopTimerAndRecord("TokenRefresh");
                
                if (refreshed)
                {
                    // Refresh successful
                    _statusControl.SetStatusMessage("Authentication refreshed successfully", BrandColors.SuccessColor);
                    _statusControl.SetStatus(Miller_Craft_Tools.UI.Styles.SyncStatus.Complete, Terms.AuthenticatedText);
                    
                    // Log success
                    TelemetryLogger.LogInfo("Token refresh successful");
                    return true;
                }
                else
                {
                    // Refresh failed
                    _statusControl.SetStatusMessage("Authentication expired. Please log in again", BrandColors.WarningColor);
                    _statusControl.SetStatus(Miller_Craft_Tools.UI.Styles.SyncStatus.Error, "Expired");
                    
                    // Log failure
                    TelemetryLogger.LogWarning("Token refresh failed - token may be expired");
                    return false;
                }
            }
            catch (Exception ex)
            {
                // Refresh error
                _statusControl.SetStatusMessage("Error refreshing authentication", BrandColors.ErrorColor);
                _statusControl.SetStatus(Miller_Craft_Tools.UI.Styles.SyncStatus.Error, "Error");
                
                // Log error
                TelemetryLogger.LogError("Error during token refresh", ex);
                return false;
            }
        }

        /// <summary>
        /// Logs the user out
        /// </summary>
        /// <returns>True if logout was successful</returns>
        public bool Logout()
        {
            try
            {
                // Log logout action
                TelemetryLogger.LogInfo("Logout initiated");

                // Update status
                _statusControl.SetStatusMessage("Logging out...", Color.DarkGray);
                _statusControl.SetStatus(Miller_Craft_Tools.UI.Styles.SyncStatus.Processing, "Logging out");

                // Log the user out
                _authService.Logout();

                // Show message
                _statusControl.SetStatusMessage("Logged out successfully", BrandColors.SuccessColor);
                _statusControl.SetStatus(Miller_Craft_Tools.UI.Styles.SyncStatus.Idle, Terms.NotAuthenticatedText);

                // Log success
                TelemetryLogger.LogInfo("Logout successful");
                return true;
            }
            catch (Exception ex)
            {
                // Log and show error
                TelemetryLogger.LogError("Error during logout", ex);
                _statusControl.SetStatusMessage($"Error: {ex.Message}", BrandColors.ErrorColor);
                _statusControl.SetStatus(Miller_Craft_Tools.UI.Styles.SyncStatus.Error, "Error");
                return false;
            }
        }

        /// <summary>
        /// Updates the UI based on current authentication status
        /// </summary>
        public void UpdateStatusDisplay()
        {
            try
            {
                bool isAuthenticated = _authService.IsAuthenticated();
                UserSettings settings = UserSettings.Load();
                
                if (isAuthenticated && settings.HasValidToken())
                {
                    // Show token expiration if available
                    if (!string.IsNullOrEmpty(settings.TokenExpiration) && 
                        DateTime.TryParse(settings.TokenExpiration, out DateTime expirationTime))
                    {
                        TimeSpan remaining = expirationTime - DateTime.UtcNow;
                        string timeMessage;
                        
                        if (remaining.TotalHours > 24)
                        {
                            timeMessage = $"Token valid for {(int)remaining.TotalDays} days";
                        }
                        else
                        {
                            timeMessage = $"Token valid for {(int)remaining.TotalHours} hours";
                        }
                        
                        _statusControl.SetStatusMessage(timeMessage, BrandColors.SuccessColor);
                    }
                    
                    // Update status display with fully qualified SyncStatus
                    _statusControl.SetStatus(Miller_Craft_Tools.UI.Styles.SyncStatus.Complete, Terms.AuthenticatedText);
                    
                    // Show username if available, otherwise show a generic message
                    string displayUsername = !string.IsNullOrEmpty(settings.Username) 
                        ? settings.Username 
                        : "Authenticated User";
                        
                    _statusControl.UserInfo = $"Logged in as: {displayUsername}";
                    _statusControl.ShowUserInfo(true);
                }
                else
                {
                    // Set status indicator with fully qualified SyncStatus
                    _statusControl.SetStatus(Miller_Craft_Tools.UI.Styles.SyncStatus.Idle, Terms.NotAuthenticatedText);
                    _statusControl.SetStatusMessage("Please log in to access Miller Craft Assistant", Color.DarkGray);
                    _statusControl.ShowUserInfo(false);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error checking authentication status: {ex.Message}");
                _statusControl.SetStatusMessage("Error checking authentication status", Color.Red);
            }
        }
    }
}
