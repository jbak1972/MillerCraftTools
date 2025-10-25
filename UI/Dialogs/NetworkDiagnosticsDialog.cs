using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Miller_Craft_Tools.Utils;
using Miller_Craft_Tools.UI.Styles;
using Miller_Craft_Tools.Services.SyncUtilities;

namespace Miller_Craft_Tools.UI.Dialogs
{
    /// <summary>
    /// Dialog for running network diagnostics to help troubleshoot connection issues
    /// </summary>
    public class NetworkDiagnosticsDialog : Form
    {
        private TextBox _resultsTextBox;
        private Button _runButton;
        private Button _closeButton;
        private ComboBox _endpointComboBox;
        private Label _statusLabel;
        private BackgroundWorker _backgroundWorker;
        private ProgressBar _progressBar;

        public NetworkDiagnosticsDialog()
        {
            InitializeComponent();
            SetupEndpointComboBox();
            BrandColors.ApplyTheme(this);
        }

        private void InitializeComponent()
        {
            this.Text = "Network Diagnostics";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(600, 400);

            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                RowCount = 4,
                ColumnCount = 1,
                RowStyles = {
                    new RowStyle(SizeType.AutoSize),
                    new RowStyle(SizeType.AutoSize),
                    new RowStyle(SizeType.Percent, 100F),
                    new RowStyle(SizeType.AutoSize)
                }
            };

            // Top panel with endpoint selection
            TableLayoutPanel topPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 10)
            };
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            Label endpointLabel = new Label
            {
                Text = "Test Endpoint:",
                AutoSize = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 5, 5, 0)
            };

            _endpointComboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 0, 10, 0)
            };

            _runButton = new Button
            {
                Text = "Run Diagnostics",
                Anchor = AnchorStyles.Right,
                AutoSize = true,
                UseVisualStyleBackColor = true,
                Padding = new Padding(10, 5, 10, 5),
            };
            _runButton.Click += RunButton_Click;

            topPanel.Controls.Add(endpointLabel, 0, 0);
            topPanel.Controls.Add(_endpointComboBox, 1, 0);
            topPanel.Controls.Add(_runButton, 2, 0);
            
            // Status panel
            TableLayoutPanel statusPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 10)
            };
            statusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            statusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            
            _statusLabel = new Label
            {
                Text = "Ready to run diagnostics",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                TextAlign = ContentAlignment.MiddleLeft
            };
            
            _progressBar = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Size = new Size(150, 23),
                Visible = false
            };
            
            statusPanel.Controls.Add(_statusLabel, 0, 0);
            statusPanel.Controls.Add(_progressBar, 1, 0);

            // Results text box
            _resultsTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new Font("Consolas", 9F),
                BackColor = Color.White
            };

            // Bottom panel with buttons
            TableLayoutPanel bottomPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 1
            };
            bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            Button copyButton = new Button
            {
                Text = "Copy Results",
                AutoSize = true,
                UseVisualStyleBackColor = true,
                Padding = new Padding(10, 5, 10, 5),
                Margin = new Padding(0, 0, 10, 0)
            };
            copyButton.Click += (s, e) => { 
                if (!string.IsNullOrWhiteSpace(_resultsTextBox.Text)) 
                    Clipboard.SetText(_resultsTextBox.Text); 
            };

            _closeButton = new Button
            {
                Text = "Close",
                AutoSize = true,
                UseVisualStyleBackColor = true,
                Padding = new Padding(10, 5, 10, 5)
            };
            _closeButton.Click += (s, e) => Close();

            bottomPanel.Controls.Add(copyButton, 0, 0);
            bottomPanel.Controls.Add(_closeButton, 1, 0);

            mainLayout.Controls.Add(topPanel, 0, 0);
            mainLayout.Controls.Add(statusPanel, 0, 1);
            mainLayout.Controls.Add(_resultsTextBox, 0, 2);
            mainLayout.Controls.Add(bottomPanel, 0, 3);

            this.Controls.Add(mainLayout);

            // Set up background worker for async operations
            _backgroundWorker = new BackgroundWorker();
            _backgroundWorker.DoWork += BackgroundWorker_DoWork;
            _backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
            _backgroundWorker.WorkerSupportsCancellation = true;
        }

        private void SetupEndpointComboBox()
        {
            // Create an instance of ApiEndpointManager
            var apiEndpointManager = new ApiEndpointManager();
            const string dummySyncId = "00000000-0000-0000-0000-000000000000"; // Dummy ID for endpoint format
            
            // Add Miller Craft Assistant API endpoints
            _endpointComboBox.Items.Add(new EndpointItem("Miller Craft Assistant API (Base)", "https://app.millercraftllc.com"));
            _endpointComboBox.Items.Add(new EndpointItem("Revit Test Endpoint (No Auth Required)", "https://app.millercraftllc.com/api/revit/test"));
            _endpointComboBox.Items.Add(new EndpointItem("Parameter Mappings Endpoint", apiEndpointManager.GetSyncEndpoint()));
            _endpointComboBox.Items.Add(new EndpointItem("Parameter Mappings Status Endpoint", apiEndpointManager.GetStatusEndpoint(dummySyncId)));
            _endpointComboBox.Items.Add(new EndpointItem("Apply API Endpoint", apiEndpointManager.GetApplyEndpoint(dummySyncId)));
            _endpointComboBox.Items.Add(new EndpointItem("Authentication Endpoint", "https://app.millercraftllc.com/api/revit/tokens"));
            
            // Add common test endpoints
            _endpointComboBox.Items.Add(new EndpointItem("Google", "https://www.google.com"));
            _endpointComboBox.Items.Add(new EndpointItem("Microsoft", "https://www.microsoft.com"));
            
            // Select the first item
            if (_endpointComboBox.Items.Count > 0)
            {
                _endpointComboBox.SelectedIndex = 0;
            }
        }

        private async void RunButton_Click(object sender, EventArgs e)
        {
            if (_backgroundWorker.IsBusy)
            {
                _backgroundWorker.CancelAsync();
                return;
            }

            var selectedItem = _endpointComboBox.SelectedItem as EndpointItem;
            if (selectedItem == null)
            {
                MessageBox.Show("Please select an endpoint to test.", "Endpoint Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _resultsTextBox.Text = $"Running diagnostics for {selectedItem.Name} ({selectedItem.Url})...\r\n\r\nPlease wait while tests complete. This may take up to 30 seconds.";
            _runButton.Text = "Cancel";
            _progressBar.Visible = true;
            _statusLabel.Text = "Running diagnostics...";

            _backgroundWorker.RunWorkerAsync(selectedItem.Url);
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            string url = (string)e.Argument;
            try
            {
                // Try to get the base URL (before the path)
                string baseUrl = null;
                try 
                {
                    var uri = new Uri(url);
                    baseUrl = $"{uri.Scheme}://{uri.Host}"; 
                    if (!uri.IsDefaultPort) baseUrl += $":{uri.Port}";
                }
                catch 
                { 
                    baseUrl = url; // If parsing fails, just use the full URL
                }
                
                // Get the token if available
                string token = null;
                try
                {
                    var tokenService = new Services.ApiTokenService();
                    token = tokenService.GetToken();
                }
                catch (Exception tokenEx)
                {
                    Utils.Logger.LogWarning($"Could not retrieve API token: {tokenEx.Message}");
                }

                // Run diagnostics asynchronously but wait for it to complete
                var task = Task.Run(async () => 
                {
                    Miller_Craft_Tools.Utils.ApiTestResult result;
                    try
                    {
                        result = await Miller_Craft_Tools.Utils.ApiConnectivityTester.TestApiConnectivityAsync(baseUrl, token);
                    }
                    catch (Exception connectivityEx)
                    {
                        Utils.Logger.LogWarning($"ApiConnectivityTester failed: {connectivityEx.Message}, falling back to NetworkDiagnostics");
                        // Fall back to the old diagnostics if there's an issue
                        var diagnosticsReport = await Utils.NetworkDiagnostics.RunDiagnosticsAsync(url);
                        result = ConvertDiagnosticsReportToApiTestResult(diagnosticsReport, url);
                    }
                    return result;
                });
                
                task.Wait();
                e.Result = task.Result;
            }
            catch (Exception ex)
            {
                e.Result = ex;
            }
        }

        private Miller_Craft_Tools.Utils.ApiTestResult ConvertDiagnosticsReportToApiTestResult(
            Miller_Craft_Tools.Utils.NetworkDiagnostics.DiagnosticsReport report, string url)
        {
            // Create a new ApiTestResult
            var result = new Miller_Craft_Tools.Utils.ApiTestResult
            {
                BaseUrl = url,
                TestEndpoint = "/",
                HasToken = false,
                Timestamp = DateTime.Now
            };
            
            // Create an EndpointTestResult for the unauthenticated test
            var endpointResult = new Miller_Craft_Tools.Utils.EndpointTestResult
            {
                Url = url,
                IsAuthenticated = false,
                IsSuccessful = report.HttpsConnection?.Success ?? report.TcpConnection?.Success ?? false,
                ResponseTimeMs = (long)(report.HttpsConnection?.Duration?.TotalMilliseconds ?? 0)
            };
            
            // Set HTTP status code if available
            if (report.HttpsConnection?.Success == true)
            {
                string statusText = report.HttpsConnection.Message;
                if (statusText.Contains("status"))
                {
                    try
                    {
                        // Extract status code from message like "HTTPS connection succeeded with status 200 (OK)"
                        int start = statusText.IndexOf("status") + 7;
                        int end = statusText.IndexOf(" ", start);
                        if (end > start && int.TryParse(statusText.Substring(start, end - start), out int statusCode))
                        {
                            endpointResult.StatusCode = (System.Net.HttpStatusCode)statusCode;
                        }
                    }
                    catch { /* Ignore parsing errors */ }
                }
            }
            
            // Add content and error info
            endpointResult.ResponseContent = report.GetFormattedReport();
            
            if (!endpointResult.IsSuccessful)
            {
                endpointResult.ErrorMessage = report.HttpsConnection?.Message ?? 
                                            report.TcpConnection?.Message ?? 
                                            "Network diagnostics reported connectivity issues";
            }
            
            // Set headers if available
            if (report.HttpsConnection?.Details != null)
            {
                endpointResult.Headers["Diagnostic-Info"] = report.HttpsConnection.Details;
            }
            
            // Add diagnostic info
            endpointResult.DiagnosticInfo = new Miller_Craft_Tools.Utils.ApiDiagnosticInfo
            {
                ServerInfo = report.IpAddress,
                ClientIpAddress = "local",
                ApiVersion = "legacy",
                AuthenticationStatus = "N/A"
            };
            
            // Set the result in the ApiTestResult
            result.UnauthenticatedTestResult = endpointResult;
            
            return result;
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _runButton.Text = "Run Diagnostics";
            _progressBar.Visible = false;

            if (e.Error != null)
            {
                _statusLabel.Text = "Error running diagnostics";
                _resultsTextBox.Text = $"Error running diagnostics: {e.Error.Message}";
                return;
            }

            if (e.Cancelled)
            {
                _statusLabel.Text = "Diagnostics cancelled";
                _resultsTextBox.Text = "Diagnostic test was cancelled.";
                return;
            }

            if (e.Result is Exception ex)
            {
                _statusLabel.Text = "Error running diagnostics";
                _resultsTextBox.Text = $"Error running diagnostics: {ex.Message}";
                return;
            }

            if (e.Result is Miller_Craft_Tools.Utils.ApiTestResult apiTestResult)
            {
                _statusLabel.Text = "API Connectivity Test completed";
                _resultsTextBox.Text = Miller_Craft_Tools.Utils.ApiConnectivityTester.GenerateTestReport(apiTestResult);
                
                // Add a summary at the top of the results
                bool basicConnectivity = apiTestResult.UnauthenticatedTestResult?.IsSuccessful == true;
                bool authSuccess = apiTestResult.HasToken && apiTestResult.AuthenticatedTestResult?.IsSuccessful == true;
                
                string statusMessage = basicConnectivity ? " Basic connectivity successful" : " Basic connectivity failed";
                if (apiTestResult.HasToken)
                {
                    statusMessage += $"\r\n{(authSuccess ? " Authentication successful" : " Authentication failed")}";
                }
                
                _statusLabel.Text = statusMessage;
                return;
            }
            
            if (e.Result is Utils.NetworkDiagnostics.DiagnosticsReport report)
            {
                _statusLabel.Text = "Legacy Diagnostics completed";
                _resultsTextBox.Text = report.GetFormattedReport();
            }
        }

        /// <summary>
        /// Helper class for endpoint items in the combo box
        /// </summary>
        private class EndpointItem
        {
            public string Name { get; }
            public string Url { get; }

            public EndpointItem(string name, string url)
            {
                Name = name;
                Url = url;
            }

            public override string ToString()
            {
                return Name;
            }
        }
    }
}
