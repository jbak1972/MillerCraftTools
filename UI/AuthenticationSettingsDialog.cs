using System;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Miller_Craft_Tools.Services;
using Miller_Craft_Tools.Utils;
using Miller_Craft_Tools.Model;
using Miller_Craft_Tools.UI.Styles;
using Miller_Craft_Tools.UI.Controls;

namespace Miller_Craft_Tools.UI
{
    /// <summary>
    /// Dialog for managing Miller Craft Assistant authentication settings
    /// </summary>
    public class AuthenticationSettingsDialog : BrandedForm
    {
        private readonly AuthenticationService _authService;
        private readonly CancellationTokenSource _cts;
        private readonly AuthenticationUIHelper _authUIHelper;
        
        // UI Controls
        private LoginCredentialsControl _credentialsControl;
        private AuthStatusControl _statusControl;
        private Button _loginButton;
        private Button _logoutButton;
        
        /// <summary>
        /// Creates a new authentication settings dialog
        /// </summary>
        public AuthenticationSettingsDialog()
        {
            // Initialize services
            _authService = new AuthenticationService();
            _cts = new CancellationTokenSource();
            
            // Set form title
            Text = Terms.AuthDialogTitle;
            
            // Initialize UI components
            InitializeComponent();
            
            // Initialize authentication helper
            _authUIHelper = new AuthenticationUIHelper(_authService, _statusControl, _cts.Token);
            _authUIHelper.UpdateStatusDisplay();
            
            // Log dialog opened
            TelemetryLogger.LogInfo("Authentication settings dialog opened");
        }
        
        /// <summary>
        /// Initializes the dialog UI components
        /// </summary>
        private void InitializeComponent()
        {
            // Set form properties
            this.Size = new Size(500, 450);
            
            // Create main authentication panel
            System.Windows.Forms.Panel authPanel = new System.Windows.Forms.Panel();
            authPanel.Dock = DockStyle.Fill;
            authPanel.Padding = new Padding(UISettings.WidePadding);
            ContentPanel.Controls.Add(authPanel);
            
            // Load current user settings
            var userSettings = UserSettings.Load();
            
            // Create and add login credentials control
            _credentialsControl = new LoginCredentialsControl();
            _credentialsControl.Location = new Point(UISettings.StandardPadding, UISettings.StandardPadding);
            _credentialsControl.InputChanged += (s, e) => UpdateLoginButtonState();
            
            // Pre-populate the username if it exists in settings
            if (!string.IsNullOrEmpty(userSettings.Username))
            {
                _credentialsControl.Username = userSettings.Username;
            }
            
            authPanel.Controls.Add(_credentialsControl);
            
            // Create and add auth status control
            _statusControl = new AuthStatusControl();
            _statusControl.Location = new Point(UISettings.StandardPadding, _credentialsControl.Bottom + UISettings.WidePadding * 2);
            authPanel.Controls.Add(_statusControl);
            
            // Create login/logout buttons - these will be dynamically shown/hidden in the footer
            _loginButton = new Button();
            _loginButton.Text = Terms.LoginText;
            _loginButton.Size = new Size(100, 30);
            _loginButton.Click += LoginButton_Click;
            UISettings.ApplyPrimaryButtonStyle(_loginButton);
            
            _logoutButton = new Button();
            _logoutButton.Text = Terms.LogoutText;
            _logoutButton.Size = new Size(100, 30);
            _logoutButton.Click += LogoutButton_Click;
            UISettings.ApplyOutlineButtonStyle(_logoutButton);
            
            // Add buttons to footer
            AddCloseButton();
            AddPrimaryButton(Terms.LoginText, LoginButton_Click);
        }
                    
        /// <summary>
        /// Updates the UI based on current authentication status
        /// </summary>
        private void UpdateUIBasedOnAuthStatus()
        {
            try
            {
                bool isAuthenticated = _authService.IsAuthenticated();
                UserSettings settings = UserSettings.Load();
                
                // Update authentication status using the helper
                _authUIHelper.UpdateStatusDisplay();
                
                // Update credential control
                if (isAuthenticated && settings.HasValidToken())
                {
                    // Disable credential fields when authenticated
                    _credentialsControl.Enabled = false;
                    _credentialsControl.Username = settings.Username;
                    _credentialsControl.ClearPassword();
                    
                    // Update button visibility
                    SetButtonVisibility(true);
                }
                else
                {
                    // Enable credential fields when not authenticated
                    _credentialsControl.Enabled = true;
                    
                    // If we have a saved username, pre-fill it
                    if (!string.IsNullOrEmpty(settings.Username))
                    {
                        _credentialsControl.Username = settings.Username;
                    }
                    
                    // Update button visibility
                    SetButtonVisibility(false);
                    
                    // Check if we can try to refresh the token
                    if (!string.IsNullOrEmpty(settings.RefreshToken))
                    {
                        // Attempt token refresh in the background
                        TryRefreshTokenAsync();
                    }
                }
                
                // Update login button state
                UpdateLoginButtonState();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error checking authentication status: {ex.Message}");
                _statusControl.SetStatusMessage("Error checking authentication status", Color.Red);
            }
        }
        
        /// <summary>
        /// Set the visibility of login/logout buttons
        /// </summary>
        private void SetButtonVisibility(bool isAuthenticated)
        {
            // Position logout button in footer when authenticated
            if (isAuthenticated)
            {
                _logoutButton.Visible = true;
                _logoutButton.Location = new Point(FooterPanel.Width - UISettings.WidePadding - 220, UISettings.StandardPadding);
                _loginButton.Visible = false;
            }
            else
            {
                // Position login button in footer when not authenticated
                _loginButton.Visible = true;
                _loginButton.Location = new Point(FooterPanel.Width - UISettings.WidePadding - 220, UISettings.StandardPadding);
                _logoutButton.Visible = false;
            }
        }
        
        /// <summary>
        /// Updates the login button state based on credential fields
        /// </summary>
        private void UpdateLoginButtonState()
        {
            _loginButton.Enabled = _credentialsControl.Validate();
        }
        
        /// <summary>
        /// Attempts to refresh the token using the refresh token
        /// </summary>
        private async void TryRefreshTokenAsync()
        {
            // Use the helper to attempt token refresh
            if (await _authUIHelper.RefreshTokenAsync())
            {
                // Update UI if refresh was successful
                UpdateUIBasedOnAuthStatus();
            }
        }
        
        /// <summary>
        /// Handles login button click
        /// </summary>
        private async void LoginButton_Click(object sender, EventArgs e)
        {
            if (!_credentialsControl.Validate())
            {
                _statusControl.SetStatusMessage("Please enter both username and password", BrandColors.ErrorColor);
                return;
            }
            
            // Disable controls during login
            _loginButton.Enabled = false;
            _credentialsControl.Enabled = false;
            
            // Get credentials
            string username = _credentialsControl.Username;
            string password = _credentialsControl.Password;
            
            // Attempt authentication using the helper
            if (await _authUIHelper.AuthenticateAsync(username, password))
            {
                // Authentication successful, update UI
                UpdateUIBasedOnAuthStatus();
            }
            else
            {
                // Re-enable controls on failure
                _loginButton.Enabled = true;
                _credentialsControl.Enabled = true;
                _credentialsControl.FocusUsername();
            }
        }

        /// <summary>
        /// Handles logout button click
        /// </summary>
        private void LogoutButton_Click(object sender, EventArgs e)
        {
            // Use the helper to log out
            if (_authUIHelper.Logout())
            {
                // Update UI after successful logout
                UpdateUIBasedOnAuthStatus();
            }
        }

        /// <summary>
        /// Clean up resources on disposal
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cts.Cancel();
                _cts.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
