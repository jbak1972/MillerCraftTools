using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Miller_Craft_Tools.Services;
using Miller_Craft_Tools.Services.SyncUtilities;
using Miller_Craft_Tools.Utils;
using Miller_Craft_Tools.Model;
using Autodesk.Revit.DB;

namespace Miller_Craft_Tools.UI
{
    /// <summary>
    /// Unified dialog for all web app integration features
    /// Consolidates connection management, sync operations, and diagnostics
    /// </summary>
    public partial class WebAppIntegrationDialog : System.Windows.Forms.Form
    {
        private readonly Document _document;
        private readonly AuthenticationService _authService;
        private readonly ApiTokenService _apiTokenService;
        private CancellationTokenSource _cancellationTokenSource;
        
        // Tab pages
        private System.Windows.Forms.TabControl mainTabControl;
        private System.Windows.Forms.TabPage connectionTab;
        private System.Windows.Forms.TabPage syncTab;
        private System.Windows.Forms.TabPage diagnosticsTab;
        
        // Connection Tab controls
        private System.Windows.Forms.Label connectionStatusLabel;
        private System.Windows.Forms.TextBox tokenTextBox;
        private System.Windows.Forms.Button validateTokenButton;
        private System.Windows.Forms.Button testConnectionButton;
        private System.Windows.Forms.Label tokenStatusLabel;
        private System.Windows.Forms.LinkLabel getTokenLinkLabel;
        
        // Sync Tab controls
        private System.Windows.Forms.Label projectGuidLabel;
        private System.Windows.Forms.TextBox projectGuidTextBox;
        private System.Windows.Forms.Button syncNowButton;
        private System.Windows.Forms.Button openQueueButton;
        private System.Windows.Forms.TextBox syncHistoryTextBox;
        private System.Windows.Forms.Label lastSyncLabel;
        
        // Diagnostics Tab controls
        private System.Windows.Forms.TextBox diagnosticsTextBox;
        private System.Windows.Forms.Button runDiagnosticsButton;
        private System.Windows.Forms.Button copyDiagnosticsButton;
        
        // Common controls
        private System.Windows.Forms.Button closeButton;
        
        /// <summary>
        /// Creates a new WebAppIntegrationDialog
        /// </summary>
        /// <param name="document">Current Revit document</param>
        public WebAppIntegrationDialog(Document document)
        {
            _document = document;
            _authService = new AuthenticationService();
            _apiTokenService = new ApiTokenService();
            _cancellationTokenSource = new CancellationTokenSource();
            
            InitializeComponent();
            LoadCurrentSettings();
        }
        
        /// <summary>
        /// Initialize all UI components
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Form settings
            this.Text = "Miller Craft Web App Integration";
            this.Size = new System.Drawing.Size(700, 600);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            
            // Main tab control
            mainTabControl = new System.Windows.Forms.TabControl
            {
                Location = new System.Drawing.Point(12, 12),
                Size = new System.Drawing.Size(660, 500),
                SelectedIndex = 0
            };
            
            // Create tabs
            CreateConnectionTab();
            CreateSyncTab();
            CreateDiagnosticsTab();
            
            // Add tabs to control
            mainTabControl.TabPages.Add(connectionTab);
            mainTabControl.TabPages.Add(syncTab);
            mainTabControl.TabPages.Add(diagnosticsTab);
            
            // Close button
            closeButton = new System.Windows.Forms.Button
            {
                Text = "Close",
                Location = new System.Drawing.Point(585, 525),
                Size = new System.Drawing.Size(87, 27),
                DialogResult = System.Windows.Forms.DialogResult.Cancel
            };
            closeButton.Click += (s, e) => this.Close();
            
            // Add controls to form
            this.Controls.Add(mainTabControl);
            this.Controls.Add(closeButton);
            
            this.CancelButton = closeButton;
            this.ResumeLayout(false);
        }
        
        /// <summary>
        /// Creates the Connection tab
        /// </summary>
        private void CreateConnectionTab()
        {
            connectionTab = new System.Windows.Forms.TabPage("Connection");
            
            int yPos = 20;
            
            // Connection status
            var statusTitleLabel = new System.Windows.Forms.Label
            {
                Text = "Connection Status:",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(120, 20),
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold)
            };
            connectionTab.Controls.Add(statusTitleLabel);
            
            connectionStatusLabel = new System.Windows.Forms.Label
            {
                Text = "Not connected",
                Location = new System.Drawing.Point(140, yPos),
                Size = new System.Drawing.Size(400, 20),
                ForeColor = System.Drawing.Color.Gray
            };
            connectionTab.Controls.Add(connectionStatusLabel);
            
            yPos += 40;
            
            // Test connection button
            testConnectionButton = new System.Windows.Forms.Button
            {
                Text = "Test Connection",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(150, 30)
            };
            testConnectionButton.Click += TestConnectionButton_Click;
            connectionTab.Controls.Add(testConnectionButton);
            
            yPos += 50;
            
            // Token section
            var tokenSectionLabel = new System.Windows.Forms.Label
            {
                Text = "Authentication Token",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(400, 20),
                Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold)
            };
            connectionTab.Controls.Add(tokenSectionLabel);
            
            yPos += 30;
            
            // Get token link
            getTokenLinkLabel = new System.Windows.Forms.LinkLabel
            {
                Text = "Get token from web app →",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(200, 20)
            };
            getTokenLinkLabel.LinkClicked += (s, e) => 
            {
                try
                {
                    System.Diagnostics.Process.Start("https://app.millercraftllc.com/settings/tokens");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to open browser: {ex.Message}");
                }
            };
            connectionTab.Controls.Add(getTokenLinkLabel);
            
            yPos += 30;
            
            // Token label
            var tokenLabel = new System.Windows.Forms.Label
            {
                Text = "API Token:",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(80, 20)
            };
            connectionTab.Controls.Add(tokenLabel);
            
            // Token textbox
            tokenTextBox = new System.Windows.Forms.TextBox
            {
                Location = new System.Drawing.Point(20, yPos + 25),
                Size = new System.Drawing.Size(500, 25),
                UseSystemPasswordChar = true,
                Font = new System.Drawing.Font("Consolas", 9F)
            };
            connectionTab.Controls.Add(tokenTextBox);
            
            yPos += 60;
            
            // Validate token button
            validateTokenButton = new System.Windows.Forms.Button
            {
                Text = "Validate & Save Token",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(150, 30)
            };
            validateTokenButton.Click += ValidateTokenButton_Click;
            connectionTab.Controls.Add(validateTokenButton);
            
            // Token status
            tokenStatusLabel = new System.Windows.Forms.Label
            {
                Text = "",
                Location = new System.Drawing.Point(180, yPos + 5),
                Size = new System.Drawing.Size(400, 20),
                ForeColor = System.Drawing.Color.Gray
            };
            connectionTab.Controls.Add(tokenStatusLabel);
            
            yPos += 50;
            
            // Instructions
            var instructionsLabel = new System.Windows.Forms.Label
            {
                Text = "Instructions:\n" +
                       "1. Click 'Get token from web app' to open the token settings page\n" +
                       "2. Copy your API token from the web app\n" +
                       "3. Paste it in the field above\n" +
                       "4. Click 'Validate & Save Token' to verify and store it",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(600, 100),
                ForeColor = System.Drawing.Color.DarkGray,
                Font = new System.Drawing.Font("Segoe UI", 8.5F)
            };
            connectionTab.Controls.Add(instructionsLabel);
        }
        
        /// <summary>
        /// Creates the Sync tab
        /// </summary>
        private void CreateSyncTab()
        {
            syncTab = new System.Windows.Forms.TabPage("Sync");
            
            int yPos = 20;
            
            // Project GUID section
            var guidLabel = new System.Windows.Forms.Label
            {
                Text = "Revit Project GUID:",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(150, 20),
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold)
            };
            syncTab.Controls.Add(guidLabel);
            
            projectGuidLabel = new System.Windows.Forms.Label
            {
                Text = "Loading...",
                Location = new System.Drawing.Point(175, yPos),
                Size = new System.Drawing.Size(300, 20),
                ForeColor = System.Drawing.Color.Blue,
                Font = new System.Drawing.Font("Consolas", 9F)
            };
            syncTab.Controls.Add(projectGuidLabel);
            
            yPos += 40;
            
            // Sync now button
            syncNowButton = new System.Windows.Forms.Button
            {
                Text = "Sync Now",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(150, 35),
                Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold),
                BackColor = System.Drawing.Color.FromArgb(0, 120, 215),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = System.Windows.Forms.FlatStyle.Flat
            };
            syncNowButton.Click += SyncNowButton_Click;
            syncTab.Controls.Add(syncNowButton);
            
            // Open queue button
            openQueueButton = new System.Windows.Forms.Button
            {
                Text = "Open Queue in Browser",
                Location = new System.Drawing.Point(180, yPos),
                Size = new System.Drawing.Size(180, 35)
            };
            openQueueButton.Click += (s, e) => 
            {
                try
                {
                    System.Diagnostics.Process.Start("https://app.millercraftllc.com/revit/queue");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to open browser: {ex.Message}");
                }
            };
            syncTab.Controls.Add(openQueueButton);
            
            yPos += 50;
            
            // Last sync info
            lastSyncLabel = new System.Windows.Forms.Label
            {
                Text = "Last sync: Never",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(400, 20),
                ForeColor = System.Drawing.Color.Gray
            };
            syncTab.Controls.Add(lastSyncLabel);
            
            yPos += 30;
            
            // Sync history section
            var historyLabel = new System.Windows.Forms.Label
            {
                Text = "Sync History (Last 10):",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(200, 20),
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold)
            };
            syncTab.Controls.Add(historyLabel);
            
            yPos += 25;
            
            syncHistoryTextBox = new System.Windows.Forms.TextBox
            {
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(610, 250),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = System.Windows.Forms.ScrollBars.Vertical,
                Font = new System.Drawing.Font("Consolas", 8.5F),
                BackColor = System.Drawing.Color.White
            };
            syncTab.Controls.Add(syncHistoryTextBox);
        }
        
        /// <summary>
        /// Creates the Diagnostics tab
        /// </summary>
        private void CreateDiagnosticsTab()
        {
            diagnosticsTab = new System.Windows.Forms.TabPage("Diagnostics");
            
            int yPos = 20;
            
            // Plugin Version Display
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            var buildDate = System.IO.File.GetLastWriteTime(System.Reflection.Assembly.GetExecutingAssembly().Location);
            
            var versionLabel = new System.Windows.Forms.Label
            {
                Text = $"Plugin Version: {version.Major}.{version.Minor}.{version.Build}",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(300, 20),
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.DarkBlue
            };
            diagnosticsTab.Controls.Add(versionLabel);
            
            yPos += 22;
            
            var buildDateLabel = new System.Windows.Forms.Label
            {
                Text = $"Build Date: {buildDate:yyyy-MM-dd HH:mm:ss}",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(300, 18),
                Font = new System.Drawing.Font("Segoe UI", 8.5F),
                ForeColor = System.Drawing.Color.Gray
            };
            diagnosticsTab.Controls.Add(buildDateLabel);
            
            yPos += 30;
            
            // Run diagnostics button
            runDiagnosticsButton = new System.Windows.Forms.Button
            {
                Text = "Run API Diagnostics",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(150, 30)
            };
            runDiagnosticsButton.Click += RunDiagnosticsButton_Click;
            diagnosticsTab.Controls.Add(runDiagnosticsButton);
            
            // Copy button
            copyDiagnosticsButton = new System.Windows.Forms.Button
            {
                Text = "Copy Results",
                Location = new System.Drawing.Point(180, yPos),
                Size = new System.Drawing.Size(120, 30)
            };
            copyDiagnosticsButton.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(diagnosticsTextBox.Text))
                {
                    System.Windows.Forms.Clipboard.SetText(diagnosticsTextBox.Text);
                    System.Windows.Forms.MessageBox.Show("Diagnostics copied to clipboard!", "Success", 
                        System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                }
            };
            diagnosticsTab.Controls.Add(copyDiagnosticsButton);
            
            yPos += 40;
            
            // Diagnostics output
            diagnosticsTextBox = new System.Windows.Forms.TextBox
            {
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(610, 390),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = System.Windows.Forms.ScrollBars.Vertical,
                Font = new System.Drawing.Font("Consolas", 8.5F),
                BackColor = System.Drawing.Color.White
            };
            diagnosticsTab.Controls.Add(diagnosticsTextBox);
        }
        
        /// <summary>
        /// Loads current settings and status
        /// </summary>
        private void LoadCurrentSettings()
        {
            // Load token
            string token = _apiTokenService.GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                tokenTextBox.Text = token;
                tokenStatusLabel.Text = "Token loaded from settings";
                tokenStatusLabel.ForeColor = System.Drawing.Color.Green;
            }
            
            // Load project GUID using ParameterHelper
            if (_document != null)
            {
                try
                {
                    // Use helper to safely get GUID (handles empty strings properly)
                    string guid = ParameterHelper.GetProjectInfoStringValue(_document, "sp.MC.ProjectGUID");
                    
                    if (!string.IsNullOrWhiteSpace(guid))
                    {
                        projectGuidLabel.Text = guid;
                        projectGuidLabel.ForeColor = System.Drawing.Color.Blue;
                    }
                    else
                    {
                        // Check if parameter exists but is empty
                        var guidParam = _document.ProjectInformation?.LookupParameter("sp.MC.ProjectGUID");
                        if (guidParam != null)
                        {
                            projectGuidLabel.Text = "Not set (empty - click Sync Now to generate)";
                            projectGuidLabel.ForeColor = System.Drawing.Color.Orange;
                        }
                        else
                        {
                            projectGuidLabel.Text = "Not set (parameter missing)";
                            projectGuidLabel.ForeColor = System.Drawing.Color.Red;
                        }
                    }
                }
                catch (Exception ex)
                {
                    projectGuidLabel.Text = $"Error: {ex.Message}";
                    projectGuidLabel.ForeColor = System.Drawing.Color.Red;
                }
            }
            
            // Load sync history
            LoadSyncHistory();
        }
        
        /// <summary>
        /// Loads sync history from settings
        /// </summary>
        private void LoadSyncHistory()
        {
            try
            {
                var settings = UserSettings.Load();
                if (settings.SyncHistory != null && settings.SyncHistory.Count > 0)
                {
                    syncHistoryTextBox.Clear();
                    foreach (var entry in settings.SyncHistory)
                    {
                        syncHistoryTextBox.AppendText($"{entry}\r\n");
                    }
                    
                    // Update last sync label
                    lastSyncLabel.Text = $"Last sync: {settings.SyncHistory[0]}";
                }
                else
                {
                    syncHistoryTextBox.Text = "No sync history available.";
                    lastSyncLabel.Text = "Last sync: Never";
                }
            }
            catch (Exception ex)
            {
                syncHistoryTextBox.Text = $"Error loading sync history: {ex.Message}";
                Logger.LogError($"Failed to load sync history: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Test connection button click handler
        /// </summary>
        private async void TestConnectionButton_Click(object sender, EventArgs e)
        {
            testConnectionButton.Enabled = false;
            connectionStatusLabel.Text = "Testing...";
            connectionStatusLabel.ForeColor = System.Drawing.Color.Blue;
            
            try
            {
                string token = tokenTextBox.Text;
                bool isValid = await _authService.ValidateTokenWithTestEndpoint(token, _cancellationTokenSource.Token);
                
                if (isValid)
                {
                    connectionStatusLabel.Text = "✅ Connected successfully!";
                    connectionStatusLabel.ForeColor = System.Drawing.Color.Green;
                }
                else
                {
                    connectionStatusLabel.Text = "❌ Connection failed";
                    connectionStatusLabel.ForeColor = System.Drawing.Color.Red;
                }
            }
            catch (Exception ex)
            {
                connectionStatusLabel.Text = $"❌ Error: {ex.Message}";
                connectionStatusLabel.ForeColor = System.Drawing.Color.Red;
                Logger.LogError($"Connection test failed: {ex.Message}");
            }
            finally
            {
                testConnectionButton.Enabled = true;
            }
        }
        
        /// <summary>
        /// Validate token button click handler
        /// </summary>
        private async void ValidateTokenButton_Click(object sender, EventArgs e)
        {
            string token = tokenTextBox.Text?.Trim();
            
            if (string.IsNullOrEmpty(token))
            {
                tokenStatusLabel.Text = "Please enter a token";
                tokenStatusLabel.ForeColor = System.Drawing.Color.Red;
                return;
            }
            
            validateTokenButton.Enabled = false;
            tokenStatusLabel.Text = "Validating...";
            tokenStatusLabel.ForeColor = System.Drawing.Color.Blue;
            
            try
            {
                bool isValid = await _authService.ValidateTokenWithTestEndpoint(token, _cancellationTokenSource.Token);
                
                if (isValid)
                {
                    // Save token
                    _apiTokenService.StoreToken(token);
                    PluginSettings.SetToken(token);
                    
                    tokenStatusLabel.Text = "✅ Token validated and saved!";
                    tokenStatusLabel.ForeColor = System.Drawing.Color.Green;
                    
                    connectionStatusLabel.Text = "✅ Connected";
                    connectionStatusLabel.ForeColor = System.Drawing.Color.Green;
                }
                else
                {
                    tokenStatusLabel.Text = "❌ Invalid token";
                    tokenStatusLabel.ForeColor = System.Drawing.Color.Red;
                }
            }
            catch (Exception ex)
            {
                tokenStatusLabel.Text = $"❌ Error: {ex.Message}";
                tokenStatusLabel.ForeColor = System.Drawing.Color.Red;
                Logger.LogError($"Token validation failed: {ex.Message}");
            }
            finally
            {
                validateTokenButton.Enabled = true;
            }
        }
        
        /// <summary>
        /// Sync now button click handler
        /// </summary>
        private async void SyncNowButton_Click(object sender, EventArgs e)
        {
            if (_document == null)
            {
                System.Windows.Forms.MessageBox.Show("No Revit document available.", "Error",
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return;
            }
            
            // Get project GUID using ParameterHelper for safe empty string handling
            string projectGuid = null;
            try
            {
                // Try to get existing GUID (helper handles empty strings correctly)
                projectGuid = ParameterHelper.GetProjectInfoStringValue(_document, "sp.MC.ProjectGUID");
                
                if (string.IsNullOrWhiteSpace(projectGuid))
                {
                    // No valid GUID - prompt to generate one
                    var result = System.Windows.Forms.MessageBox.Show(
                        "This project doesn't have a Project GUID yet.\n\n" +
                        "A new GUID will be generated and saved to identify this project.\n\n" +
                        "Continue with sync?",
                        "Generate Project GUID",
                        System.Windows.Forms.MessageBoxButtons.YesNo,
                        System.Windows.Forms.MessageBoxIcon.Question);
                    
                    if (result != System.Windows.Forms.DialogResult.Yes)
                    {
                        return;
                    }
                    
                    // Generate new GUID
                    projectGuid = Guid.NewGuid().ToString();
                    
                    // Set it in the project (requires transaction)
                    using (Transaction tx = new Transaction(_document, "Set Project GUID"))
                    {
                        tx.Start();
                        
                        // First, ensure the parameter exists (create it if needed)
                        bool parameterExists = ParameterCreationHelper.EnsureProjectGuidParameterExists(_document);
                        
                        if (!parameterExists)
                        {
                            // Failed to create parameter
                            System.Windows.Forms.MessageBox.Show(
                                "Failed to create the sp.MC.ProjectGUID parameter.\n\n" +
                                "Please check the log file for details.",
                                "Parameter Creation Failed",
                                System.Windows.Forms.MessageBoxButtons.OK,
                                System.Windows.Forms.MessageBoxIcon.Error);
                            tx.RollBack();
                            return;
                        }
                        
                        // Now set the value
                        bool success = ParameterHelper.SetParameterStringValue(
                            _document.ProjectInformation, 
                            "sp.MC.ProjectGUID", 
                            projectGuid);
                        
                        if (!success)
                        {
                            // Couldn't set the value
                            System.Windows.Forms.MessageBox.Show(
                                "The parameter was created but the value couldn't be set.\n\n" +
                                "Please check the log file for details.",
                                "Parameter Set Failed",
                                System.Windows.Forms.MessageBoxButtons.OK,
                                System.Windows.Forms.MessageBoxIcon.Error);
                            tx.RollBack();
                            return;
                        }
                        
                        tx.Commit();
                    }
                    
                    // Update the UI
                    projectGuidLabel.Text = projectGuid;
                    projectGuidLabel.ForeColor = System.Drawing.Color.Blue;
                    
                    Logger.LogInfo($"Generated new Project GUID: {projectGuid}");
                    
                    // Show success message
                    System.Windows.Forms.MessageBox.Show(
                        $"Project GUID created successfully!\n\n" +
                        $"GUID: {projectGuid}\n\n" +
                        $"The parameter 'sp.MC.ProjectGUID' has been added to your project.",
                        "Success",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Information);
                }
                else
                {
                    // Use existing GUID
                    Logger.LogInfo($"Using existing Project GUID: {projectGuid}");
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Error reading project GUID: {ex.Message}", "Error",
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return;
            }
            
            // Check for token
            string token = _apiTokenService.GetToken();
            if (string.IsNullOrEmpty(token))
            {
                var result = System.Windows.Forms.MessageBox.Show(
                    "No API token found.\n\nWould you like to go to the Connection tab to add one?",
                    "Token Required",
                    System.Windows.Forms.MessageBoxButtons.YesNo,
                    System.Windows.Forms.MessageBoxIcon.Warning);
                
                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    mainTabControl.SelectedIndex = 0; // Connection tab
                }
                return;
            }
            
            // Disable button during sync
            syncNowButton.Enabled = false;
            syncNowButton.Text = "Syncing...";
            
            try
            {
                // Create sync service with progress reporter
                var progressReporter = new SimpleProgressReporter();
                var syncService = new SyncServiceV2(
                    progressHandler: progressReporter,
                    cancellationToken: _cancellationTokenSource.Token);
                
                // Perform sync
                var syncResult = await syncService.InitiateSyncAsync(_document, projectGuid);
                
                // Log to history
                string historyEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ";
                if (syncResult.Action == "queue")
                {
                    historyEntry += $"✓ Queued - QueueID: {syncResult.QueueId}";
                }
                else if (syncResult.Action == "sync")
                {
                    historyEntry += $"✓ Synced - {syncResult.ProjectName} ({syncResult.Data?.ChangesApplied ?? 0} changes)";
                }
                else
                {
                    historyEntry += $"✓ {syncResult.Message}";
                }
                
                var settings = UserSettings.Load();
                settings.AddSyncHistoryEntry(historyEntry);
                
                // Refresh history display
                LoadSyncHistory();
                
                // Show result dialog
                if (syncResult.Action == "queue")
                {
                    var dlgResult = System.Windows.Forms.MessageBox.Show(
                        $"{syncResult.Message}\n\n" +
                        $"Queue ID: {syncResult.QueueId}\n\n" +
                        "Would you like to open the web app to associate this project?",
                        "Project Association Required",
                        System.Windows.Forms.MessageBoxButtons.YesNo,
                        System.Windows.Forms.MessageBoxIcon.Information);
                    
                    if (dlgResult == System.Windows.Forms.DialogResult.Yes)
                    {
                        try
                        {
                            System.Diagnostics.Process.Start("https://app.millercraftllc.com/revit/queue");
                        }
                        catch { }
                    }
                }
                else if (syncResult.Action == "sync")
                {
                    System.Windows.Forms.MessageBox.Show(
                        $"{syncResult.Message}\n\n" +
                        $"Project: {syncResult.ProjectName}\n" +
                        $"Parameters Updated: {syncResult.Data?.ChangesApplied ?? 0}",
                        "Sync Successful",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Information);
                }
            }
            catch (UnauthorizedAccessException)
            {
                System.Windows.Forms.MessageBox.Show(
                    "Authentication failed. Please check your token in the Connection tab.",
                    "Authentication Error",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
                    
                mainTabControl.SelectedIndex = 0; // Switch to Connection tab
            }
            catch (Exception ex)
            {
                // Log full error details for debugging
                Logger.LogError($"Sync failed: {ex.Message}\nStack Trace: {ex.StackTrace}");
                
                // Show detailed error to user
                System.Windows.Forms.MessageBox.Show(
                    $"Sync failed:\n\n{ex.Message}\n\n" +
                    $"Exception Type: {ex.GetType().Name}\n\n" +
                    $"Check log file for details:\n{System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Miller Craft Assistant", "errors.log")}",
                    "Sync Error",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
                
                // Log failed sync to history with exception type
                var settings = UserSettings.Load();
                settings.AddSyncHistoryEntry($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ✗ Failed - {ex.GetType().Name}: {ex.Message}");
                LoadSyncHistory();
            }
            finally
            {
                syncNowButton.Enabled = true;
                syncNowButton.Text = "Sync Now";
            }
        }
        
        /// <summary>
        /// Run diagnostics button click handler
        /// </summary>
        private async void RunDiagnosticsButton_Click(object sender, EventArgs e)
        {
            runDiagnosticsButton.Enabled = false;
            diagnosticsTextBox.Clear();
            diagnosticsTextBox.AppendText("Running API diagnostics...\n\n");
            
            try
            {
                string token = _apiTokenService.GetToken();
                if (string.IsNullOrEmpty(token))
                {
                    diagnosticsTextBox.AppendText("❌ No API token available. Please add a token in the Connection tab.\n");
                    return;
                }
                
                diagnosticsTextBox.AppendText("Testing API endpoints...\n\n");
                
                // Test token validation
                diagnosticsTextBox.AppendText("Testing token validation endpoint...\n");
                var validationResult = await SimpleApiTester.TestEndpointGetAsync("/api/revit/test", token);
                diagnosticsTextBox.AppendText($"   Result: {validationResult.StatusCode}\n");
                if (!string.IsNullOrEmpty(validationResult.ResponseContent) && validationResult.ResponseContent.Length < 200)
                {
                    diagnosticsTextBox.AppendText($"   Response: {validationResult.ResponseContent}\n");
                }
                diagnosticsTextBox.AppendText("\n");
                
                // Test sync endpoint
                diagnosticsTextBox.AppendText("Testing sync endpoint: /api/revit/sync (POST)...\n");
                string minimalSyncBody = "{\"revitProjectGuid\":\"test-guid\",\"revitFileName\":\"test\",\"parameters\":[],\"version\":\"1.0\",\"timestamp\":\"" + DateTime.UtcNow.ToString("o") + "\"}";
                var syncResult = await SimpleApiTester.TestEndpointAsync("/api/revit/sync", token, "POST", minimalSyncBody);
                diagnosticsTextBox.AppendText($"   Result: {syncResult.StatusCode}\n");
                if (!string.IsNullOrEmpty(syncResult.ResponseContent) && syncResult.ResponseContent.Length < 500)
                {
                    diagnosticsTextBox.AppendText($"   Response: {syncResult.ResponseContent}\n");
                }
                diagnosticsTextBox.AppendText("\n");
                
                diagnosticsTextBox.AppendText("API endpoint tests completed.\n");
            }
            catch (Exception ex)
            {
                diagnosticsTextBox.AppendText($"\nERROR: {ex.Message}\n");
                Logger.LogError($"Diagnostics failed: {ex.Message}");
            }
            finally
            {
                runDiagnosticsButton.Enabled = true;
            }
        }
        
        /// <summary>
        /// Switches to a specific tab
        /// </summary>
        /// <param name="tabIndex">0=Connection, 1=Sync, 2=Diagnostics</param>
        public void SwitchToTab(int tabIndex)
        {
            if (tabIndex >= 0 && tabIndex < mainTabControl.TabPages.Count)
            {
                mainTabControl.SelectedIndex = tabIndex;
            }
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
