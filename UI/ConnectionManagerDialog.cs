using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Miller_Craft_Tools.Services;
using Miller_Craft_Tools.Utils;
using Miller_Craft_Tools.Model;
using Miller_Craft_Tools.UI.Styles;
using Miller_Craft_Tools.UI.Controls;

namespace Miller_Craft_Tools.UI
{
    /// <summary>
    /// Consolidated dialog for managing all connection-related functionality:
    /// - Authentication
    /// - API token management
    /// - Connection diagnostics
    /// </summary>
    public class ConnectionManagerDialog : BrandedForm
    {
        // Tab control
        private System.Windows.Forms.TabControl _tabControl;
        
        // Tabs
        private System.Windows.Forms.TabPage _authTab;
        private System.Windows.Forms.TabPage _tokenTab;
        private System.Windows.Forms.TabPage _diagnosticsTab;
        
        // Auth tab controls
        private LoginCredentialsControl _credentialsControl;
        private AuthStatusControl _statusControl;
        private Button _loginButton;
        private Button _logoutButton;
        
        // Services
        private readonly AuthenticationService _authService;
        private readonly CancellationTokenSource _cts;
        private readonly AuthenticationUIHelper _authUIHelper;
        
        /// <summary>
        /// Creates a new connection manager dialog
        /// </summary>
        public ConnectionManagerDialog()
        {
            // Initialize services
            _authService = new AuthenticationService();
            _cts = new CancellationTokenSource();
            
            // Set form title
            Text = "Miller Craft Connection Manager";
            
            // Initialize UI components
            InitializeComponent();
            
            // Initialize authentication helper after controls are created
            _authUIHelper = new AuthenticationUIHelper(_authService, _statusControl, _cts.Token);
            _authUIHelper.UpdateStatusDisplay();
            
            // Log dialog opened
            TelemetryLogger.LogInfo("Connection manager dialog opened");
        }
        
        /// <summary>
        /// Initializes the dialog UI components
        /// </summary>
        private void InitializeComponent()
        {
            // Set form properties
            this.Size = new System.Drawing.Size(600, 500);
            
            // Create tabbed interface
            _tabControl = new System.Windows.Forms.TabControl();
            _tabControl.Dock = DockStyle.Fill;
            
            // Create tabs
            _authTab = new System.Windows.Forms.TabPage("Authentication");
            _tokenTab = new System.Windows.Forms.TabPage("API Token");
            _diagnosticsTab = new System.Windows.Forms.TabPage("Diagnostics");
            
            // Setup authentication tab
            SetupAuthenticationTab();
            
            // Setup token tab
            SetupTokenTab();
            
            // Setup diagnostics tab
            SetupDiagnosticsTab();
            
            // Add tabs to control
            _tabControl.TabPages.Add(_authTab);
            _tabControl.TabPages.Add(_tokenTab);
            _tabControl.TabPages.Add(_diagnosticsTab);
            
            // Add tab control to form
            ContentPanel.Controls.Add(_tabControl);
            
            // Add close button to footer
            AddCloseButton();
        }
        
        /// <summary>
        /// Sets up the Authentication tab
        /// </summary>
        private void SetupAuthenticationTab()
        {
            // Create main panel for the tab
            System.Windows.Forms.Panel authPanel = new System.Windows.Forms.Panel();
            authPanel.Dock = DockStyle.Fill;
            authPanel.Padding = new System.Windows.Forms.Padding(UISettings.WidePadding);
            _authTab.Controls.Add(authPanel);
            
            // Load current user settings
            var userSettings = UserSettings.Load();
            
            // Create and add login credentials control
            _credentialsControl = new LoginCredentialsControl();
            _credentialsControl.Location = new System.Drawing.Point(UISettings.StandardPadding, UISettings.StandardPadding);
            _credentialsControl.InputChanged += (s, e) => UpdateLoginButtonState();
            
            // Pre-populate the username if it exists in settings
            if (!string.IsNullOrEmpty(userSettings.Username))
            {
                _credentialsControl.Username = userSettings.Username;
            }
            
            authPanel.Controls.Add(_credentialsControl);
            
            // Create and add auth status control
            _statusControl = new AuthStatusControl();
            _statusControl.Location = new System.Drawing.Point(UISettings.StandardPadding, _credentialsControl.Bottom + UISettings.WidePadding * 2);
            authPanel.Controls.Add(_statusControl);
            
            // Create login/logout buttons
            _loginButton = new Button();
            _loginButton.Text = "Login";
            _loginButton.Size = new System.Drawing.Size(100, 30);
            _loginButton.Location = new System.Drawing.Point(UISettings.StandardPadding, _statusControl.Bottom + UISettings.WidePadding);
            _loginButton.Click += LoginButton_Click;
            UISettings.ApplyPrimaryButtonStyle(_loginButton);
            
            _logoutButton = new Button();
            _logoutButton.Text = "Logout";
            _logoutButton.Size = new System.Drawing.Size(100, 30);
            _logoutButton.Location = new System.Drawing.Point(UISettings.StandardPadding, _statusControl.Bottom + UISettings.WidePadding);
            _logoutButton.Click += LogoutButton_Click;
            UISettings.ApplyOutlineButtonStyle(_logoutButton);
            
            // Add buttons to the panel
            authPanel.Controls.Add(_loginButton);
            authPanel.Controls.Add(_logoutButton);
            
            // Update button visibility based on auth status
            UpdateUIBasedOnAuthStatus();
        }
        
        /// <summary>
        /// Sets up the API Token tab
        /// </summary>
        private void SetupTokenTab()
        {
            // Create main panel for the tab
            System.Windows.Forms.Panel tokenPanel = new System.Windows.Forms.Panel();
            tokenPanel.Dock = DockStyle.Fill;
            tokenPanel.Padding = new System.Windows.Forms.Padding(UISettings.WidePadding);
            _tokenTab.Controls.Add(tokenPanel);
            
            // Token Display Section
            Label tokenLabel = new Label();
            tokenLabel.Text = "API Token:";
            tokenLabel.AutoSize = true;
            tokenLabel.Location = new System.Drawing.Point(UISettings.StandardPadding, UISettings.StandardPadding);
            UISettings.ApplySubheadingStyle(tokenLabel);
            tokenPanel.Controls.Add(tokenLabel);
            
            TextBox tokenTextBox = new TextBox();
            tokenTextBox.Size = new System.Drawing.Size(450, 30);
            tokenTextBox.Location = new System.Drawing.Point(UISettings.StandardPadding, tokenLabel.Bottom + UISettings.StandardPadding);
            tokenTextBox.Text = GetMaskedToken();
            tokenTextBox.ReadOnly = true;
            tokenPanel.Controls.Add(tokenTextBox);
            
            // Token Actions
            Button validateButton = new Button();
            validateButton.Text = "Validate Token";
            validateButton.Size = new System.Drawing.Size(150, 30);
            validateButton.Location = new System.Drawing.Point(UISettings.StandardPadding, tokenTextBox.Bottom + UISettings.WidePadding);
            validateButton.Click += async (s, e) => await ValidateTokenAsync();
            UISettings.ApplyPrimaryButtonStyle(validateButton);
            tokenPanel.Controls.Add(validateButton);
            
            Button manageButton = new Button();
            manageButton.Text = "Manage Token";
            manageButton.Size = new System.Drawing.Size(150, 30);
            manageButton.Location = new System.Drawing.Point(validateButton.Right + UISettings.StandardPadding, tokenTextBox.Bottom + UISettings.WidePadding);
            manageButton.Click += ManageToken_Click;
            UISettings.ApplyOutlineButtonStyle(manageButton);
            tokenPanel.Controls.Add(manageButton);
            
            // Token Status
            AuthStatusControl tokenStatus = new AuthStatusControl();
            tokenStatus.Location = new System.Drawing.Point(UISettings.StandardPadding, validateButton.Bottom + UISettings.WidePadding);
            tokenStatus.SetStatusMessage("Token status will appear here after validation.", Color.Black);
            tokenPanel.Controls.Add(tokenStatus);
        }
        
        /// <summary>
        /// Sets up the Diagnostics tab
        /// </summary>
        private void SetupDiagnosticsTab()
        {
            // Create main panel for the tab
            System.Windows.Forms.Panel diagnosticsPanel = new System.Windows.Forms.Panel();
            diagnosticsPanel.Dock = DockStyle.Fill;
            diagnosticsPanel.Padding = new System.Windows.Forms.Padding(UISettings.WidePadding);
            _diagnosticsTab.Controls.Add(diagnosticsPanel);
            
            // Connection Test Section
            Label connectionLabel = new Label();
            connectionLabel.Text = "Connection Tests:";
            connectionLabel.AutoSize = true;
            connectionLabel.Location = new System.Drawing.Point(UISettings.StandardPadding, UISettings.StandardPadding);
            UISettings.ApplySubheadingStyle(connectionLabel);
            diagnosticsPanel.Controls.Add(connectionLabel);
            
            // Test buttons
            Button basicTestButton = new Button();
            basicTestButton.Text = "Basic Connectivity Test";
            basicTestButton.Size = new System.Drawing.Size(200, 30);
            basicTestButton.Location = new System.Drawing.Point(UISettings.StandardPadding, connectionLabel.Bottom + UISettings.StandardPadding);
            basicTestButton.Click += async (s, e) => await RunBasicConnectivityTestAsync();
            UISettings.ApplyPrimaryButtonStyle(basicTestButton);
            diagnosticsPanel.Controls.Add(basicTestButton);
            
            Button apiTestButton = new Button();
            apiTestButton.Text = "API Endpoint Test";
            apiTestButton.Size = new System.Drawing.Size(200, 30);
            apiTestButton.Location = new System.Drawing.Point(UISettings.StandardPadding, basicTestButton.Bottom + UISettings.StandardPadding);
            apiTestButton.Click += async (s, e) => await RunApiEndpointTestAsync();
            UISettings.ApplyOutlineButtonStyle(apiTestButton);
            diagnosticsPanel.Controls.Add(apiTestButton);
            
            // Results section
            Label resultsLabel = new Label();
            resultsLabel.Text = "Test Results:";
            resultsLabel.AutoSize = true;
            resultsLabel.Location = new System.Drawing.Point(UISettings.StandardPadding, apiTestButton.Bottom + UISettings.WidePadding);
            UISettings.ApplySubheadingStyle(resultsLabel);
            diagnosticsPanel.Controls.Add(resultsLabel);
            
            RichTextBox resultsTextBox = new RichTextBox();
            resultsTextBox.Size = new System.Drawing.Size(500, 200);
            resultsTextBox.Location = new System.Drawing.Point(UISettings.StandardPadding, resultsLabel.Bottom + UISettings.StandardPadding);
            resultsTextBox.ReadOnly = true;
            resultsTextBox.BackColor = Color.White;
            resultsTextBox.Text = "Run a test to see results...";
            diagnosticsPanel.Controls.Add(resultsTextBox);
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
                    _loginButton.Visible = false;
                    _logoutButton.Visible = true;
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
                    _loginButton.Visible = true;
                    _logoutButton.Visible = false;
                    
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
        /// Returns a masked version of the token for display
        /// </summary>
        private string GetMaskedToken()
        {
            string token = PluginSettings.GetToken();
            if (string.IsNullOrEmpty(token))
            {
                return "[No token available]";
            }
            
            // Show first 5 and last 5 characters, mask the rest
            if (token.Length > 10)
            {
                return $"{token.Substring(0, 5)}...{token.Substring(token.Length - 5)}";
            }
            
            return "••••••••••";
        }
        
        /// <summary>
        /// Validates the current token with the server
        /// </summary>
        private async Task ValidateTokenAsync()
        {
            // Get the token control from the Token tab
            var tokenPanel = _tokenTab.Controls[0] as System.Windows.Forms.Panel;
            var statusControl = tokenPanel.Controls.Find("AuthStatusControl", true)[0] as AuthStatusControl;
            
            if (statusControl == null)
            {
                return;
            }
            
            try
            {
                statusControl.SetStatusMessage("Validating token...", Color.Black);
                
                string token = PluginSettings.GetToken();
                if (string.IsNullOrEmpty(token))
                {
                    statusControl.SetStatusMessage("No token available to validate.", Color.Red);
                    return;
                }
                
                bool isValid = await _authService.ValidateTokenWithServerAsync(token, _cts.Token);
                
                if (isValid)
                {
                    statusControl.SetStatusMessage("Token is valid and active.", Color.Green);
                    statusControl.SetStatus(Miller_Craft_Tools.UI.Styles.SyncStatus.Complete);
                }
                else
                {
                    statusControl.SetStatusMessage("Token is invalid or expired.", Color.Red);
                    statusControl.SetStatus(Miller_Craft_Tools.UI.Styles.SyncStatus.Error);
                }
            }
            catch (Exception ex)
            {
                statusControl.SetStatusMessage($"Error validating token: {ex.Message}", Color.Red);
                statusControl.SetStatus(Miller_Craft_Tools.UI.Styles.SyncStatus.Error);
                Logger.LogError($"Token validation error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Opens the token management dialog
        /// </summary>
        private void ManageToken_Click(object sender, EventArgs e)
        {
            try
            {
                using (var tokenDialog = new UI.Dialogs.ApiTokenDialog())
                {
                    tokenDialog.ShowDialog();
                }
                
                // Update the token display after management
                var tokenPanel = _tokenTab.Controls[0] as System.Windows.Forms.Panel;
                var tokenTextBox = tokenPanel.Controls[1] as TextBox;
                
                if (tokenTextBox != null)
                {
                    tokenTextBox.Text = GetMaskedToken();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error opening token management: {ex.Message}");
                Autodesk.Revit.UI.TaskDialog.Show("Error", $"Could not open token management: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Runs a basic connectivity test
        /// </summary>
        private async Task RunBasicConnectivityTestAsync()
        {
            var diagnosticsPanel = _diagnosticsTab.Controls[0] as System.Windows.Forms.Panel;
            var resultsTextBox = diagnosticsPanel.Controls[diagnosticsPanel.Controls.Count - 1] as RichTextBox;
            
            if (resultsTextBox == null)
            {
                return;
            }
            
            resultsTextBox.Clear();
            resultsTextBox.AppendText("Running basic connectivity test...\n\n");
            
            try
            {
                // Run basic connectivity tests
                var results = new List<ApiTestingResult>();
                
                // Test 1: Check basic internet connectivity
                try {
                    using (var client = new System.Net.Http.HttpClient())
                    {
                        var response = await client.GetAsync("https://app.millercraftllc.com/api/health");
                        results.Add(new ApiTestingResult { 
                            TestName = "Basic Internet Connectivity", 
                            IsSuccess = response.IsSuccessStatusCode, 
                            Message = $"HTTP Status: {response.StatusCode}" 
                        });
                    }
                }
                catch (Exception ex)
                {
                    results.Add(new ApiTestingResult { 
                        TestName = "Basic Internet Connectivity", 
                        IsSuccess = false, 
                        Message = ex.Message 
                    });
                }
                
                // Test 2: DNS resolution
                try {
                    var hostEntry = await System.Net.Dns.GetHostEntryAsync("app.millercraftllc.com");
                    results.Add(new ApiTestingResult { 
                        TestName = "DNS Resolution", 
                        IsSuccess = true, 
                        Message = $"Resolved to {hostEntry.AddressList.Length} addresses" 
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new ApiTestingResult { 
                        TestName = "DNS Resolution", 
                        IsSuccess = false, 
                        Message = ex.Message 
                    });
                }
                
                // Display results
                resultsTextBox.AppendText("==== CONNECTIVITY TEST RESULTS ====\n\n");
                
                foreach (var result in results)
                {
                    string statusText = result.IsSuccess ? "SUCCESS" : "FAILED";
                    Color statusColor = result.IsSuccess ? Color.Green : Color.Red;
                    
                    resultsTextBox.AppendText($"Test: {result.TestName}\n");
                    resultsTextBox.AppendText($"Status: ");
                    
                    // Save selection position
                    int selectionStart = resultsTextBox.TextLength;
                    resultsTextBox.AppendText(statusText);
                    int selectionEnd = resultsTextBox.TextLength;
                    
                    // Apply color to status text only
                    resultsTextBox.Select(selectionStart, selectionEnd - selectionStart);
                    resultsTextBox.SelectionColor = statusColor;
                    resultsTextBox.SelectionLength = 0;
                    
                    resultsTextBox.AppendText($"\nDetails: {result.Message}\n\n");
                }
            }
            catch (Exception ex)
            {
                resultsTextBox.AppendText($"ERROR: {ex.Message}");
                Logger.LogError($"Connectivity test error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Runs an API endpoint test
        /// </summary>
        private async Task RunApiEndpointTestAsync()
        {
            var diagnosticsPanel = _diagnosticsTab.Controls[0] as System.Windows.Forms.Panel;
            var resultsTextBox = diagnosticsPanel.Controls[diagnosticsPanel.Controls.Count - 1] as RichTextBox;
            
            if (resultsTextBox == null)
            {
                return;
            }
            
            resultsTextBox.Clear();
            resultsTextBox.AppendText("Testing API endpoints...\n\n");
            
            try
            {
                string token = PluginSettings.GetToken();
                if (string.IsNullOrEmpty(token))
                {
                    resultsTextBox.AppendText("No API token available. Please add a token using the API Token tab.");
                    return;
                }
                
                // Test token validation endpoint (GET /api/revit/test)
                resultsTextBox.AppendText("Testing token validation endpoint...\n");
                var validationResult = await SimpleApiTester.TestEndpointGetAsync("/api/revit/test", token);
                
                if (validationResult.IsSuccessful)
                {
                    resultsTextBox.AppendText("✅ Token validation successful\n\n");
                }
                else
                {
                    resultsTextBox.AppendText($"❌ Token validation failed with status code: {validationResult.StatusCode}\n\n");
                }
                
                // Test sync endpoints (try multiple paths)
                string minimalSyncBody = "{\"revitProjectGuid\":\"test-guid\",\"revitFileName\":\"test\",\"parameters\":[],\"version\":\"1.0\",\"timestamp\":\"" + DateTime.UtcNow.ToString("o") + "\"}";
                
                // Try primary endpoint: /api/revit/sync
                resultsTextBox.AppendText("Testing sync endpoint: /api/revit/sync (POST)...\n");
                var syncResult = await SimpleApiTester.TestEndpointAsync("/api/revit/sync", token, "POST", minimalSyncBody);
                resultsTextBox.AppendText($"   Result: {syncResult.StatusCode}\n");
                if (!string.IsNullOrEmpty(syncResult.ResponseContent) && syncResult.ResponseContent.Length < 200)
                {
                    resultsTextBox.AppendText($"   Response: {syncResult.ResponseContent}\n");
                }
                resultsTextBox.AppendText("\n");
                
                // Try legacy endpoint: /api/revit-sync/upload
                resultsTextBox.AppendText("Testing legacy endpoint: /api/revit-sync/upload (POST)...\n");
                var legacyResult = await SimpleApiTester.TestEndpointAsync("/api/revit-sync/upload", token, "POST", minimalSyncBody);
                resultsTextBox.AppendText($"   Result: {legacyResult.StatusCode}\n");
                if (!string.IsNullOrEmpty(legacyResult.ResponseContent) && legacyResult.ResponseContent.Length < 200)
                {
                    resultsTextBox.AppendText($"   Response: {legacyResult.ResponseContent}\n");
                }
                resultsTextBox.AppendText("\n");
                
                // Try alternative: /api/parameter-mappings/upload
                resultsTextBox.AppendText("Testing alternative: /api/parameter-mappings/upload (POST)...\n");
                var altResult = await SimpleApiTester.TestEndpointAsync("/api/parameter-mappings/upload", token, "POST", minimalSyncBody);
                resultsTextBox.AppendText($"   Result: {altResult.StatusCode}\n");
                if (!string.IsNullOrEmpty(altResult.ResponseContent) && altResult.ResponseContent.Length < 200)
                {
                    resultsTextBox.AppendText($"   Response: {altResult.ResponseContent}\n");
                }
                resultsTextBox.AppendText("\n");
                
                resultsTextBox.AppendText("API endpoint tests completed.");
            }
            catch (Exception ex)
            {
                resultsTextBox.AppendText($"ERROR: {ex.Message}");
                Logger.LogError($"API endpoint test error: {ex.Message}");
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
