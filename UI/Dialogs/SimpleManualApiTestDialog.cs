using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Miller_Craft_Tools.Utils;
using Miller_Craft_Tools.UI.Styles;

namespace Miller_Craft_Tools.UI.Dialogs
{
    /// <summary>
    /// Simplified dialog for manual API testing that doesn't use BrandedForm
    /// </summary>
    public class SimpleManualApiTestDialog : System.Windows.Forms.Form
    {
        #region UI Controls
        private System.Windows.Forms.Label lblEndpoint;
        private System.Windows.Forms.ComboBox cmbEndpoint;
        private System.Windows.Forms.Label lblAuthType;
        private System.Windows.Forms.ComboBox cmbAuthType;
        private System.Windows.Forms.Label lblCustomToken;
        private System.Windows.Forms.TextBox txtCustomToken;
        private System.Windows.Forms.Button btnTestEndpoint;
        private System.Windows.Forms.Button btnRunSequentialTests;
        private System.Windows.Forms.Label lblResults;
        private System.Windows.Forms.RichTextBox txtResults;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Panel headerPanel;
        private System.Windows.Forms.Label headerLabel;
        #endregion

        private string _customToken = null;
        private bool _useCustomToken = false;

        public SimpleManualApiTestDialog()
        {
            InitializeComponent();
        }

        #region Component Initialization

        private void InitializeComponent()
        {
            // Initialize form properties
            this.Text = "Manual API Test";
            this.Size = new System.Drawing.Size(700, 600);
            this.MinimumSize = new System.Drawing.Size(600, 500);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            
            // Create header panel
            this.headerPanel = new System.Windows.Forms.Panel();
            this.headerPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(66)))), ((int)(((byte)(133)))), ((int)(((byte)(244)))));
            this.headerPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.headerPanel.Height = 60;
            
            // Header label
            this.headerLabel = new System.Windows.Forms.Label();
            this.headerLabel.Text = "Manual API Test";
            this.headerLabel.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.headerLabel.ForeColor = System.Drawing.Color.White;
            this.headerLabel.Location = new System.Drawing.Point(20, 15);
            this.headerLabel.AutoSize = true;
            this.headerPanel.Controls.Add(this.headerLabel);
            
            // Create endpoint label and combobox
            this.lblEndpoint = new System.Windows.Forms.Label();
            this.lblEndpoint.Text = "Endpoint:";
            this.lblEndpoint.AutoSize = true;
            this.lblEndpoint.Location = new System.Drawing.Point(20, 80);
            
            this.cmbEndpoint = new System.Windows.Forms.ComboBox();
            this.cmbEndpoint.Location = new System.Drawing.Point(120, 77);
            this.cmbEndpoint.Size = new System.Drawing.Size(540, 23);
            this.cmbEndpoint.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            
            // Create authentication type label and combobox
            this.lblAuthType = new System.Windows.Forms.Label();
            this.lblAuthType.Text = "Authentication:";
            this.lblAuthType.AutoSize = true;
            this.lblAuthType.Location = new System.Drawing.Point(20, 110);
            
            this.cmbAuthType = new System.Windows.Forms.ComboBox();
            this.cmbAuthType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAuthType.Location = new System.Drawing.Point(120, 107);
            this.cmbAuthType.Size = new System.Drawing.Size(200, 23);
            this.cmbAuthType.SelectedIndexChanged += new System.EventHandler(this.cmbAuthType_SelectedIndexChanged);
            
            // Create custom token label and textbox
            this.lblCustomToken = new System.Windows.Forms.Label();
            this.lblCustomToken.Text = "Custom Token:";
            this.lblCustomToken.AutoSize = true;
            this.lblCustomToken.Location = new System.Drawing.Point(20, 140);
            
            this.txtCustomToken = new System.Windows.Forms.TextBox();
            this.txtCustomToken.Location = new System.Drawing.Point(120, 137);
            this.txtCustomToken.Size = new System.Drawing.Size(540, 23);
            this.txtCustomToken.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.txtCustomToken.TextChanged += new System.EventHandler(this.txtCustomToken_TextChanged);
            
            // Create results label and textbox
            this.lblResults = new System.Windows.Forms.Label();
            this.lblResults.Text = "Results:";
            this.lblResults.AutoSize = true;
            this.lblResults.Location = new System.Drawing.Point(20, 170);
            
            this.txtResults = new System.Windows.Forms.RichTextBox();
            this.txtResults.Location = new System.Drawing.Point(20, 190);
            this.txtResults.Size = new System.Drawing.Size(640, 320);
            this.txtResults.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | 
                         System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.txtResults.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtResults.ReadOnly = true;
            
            // Create buttons
            this.btnTestEndpoint = new System.Windows.Forms.Button();
            this.btnTestEndpoint.Text = "Test Selected Endpoint";
            this.btnTestEndpoint.Location = new System.Drawing.Point(20, 520);
            this.btnTestEndpoint.Size = new System.Drawing.Size(160, 30);
            this.btnTestEndpoint.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            this.btnTestEndpoint.Click += new System.EventHandler(this.btnTestEndpoint_Click);
            
            this.btnRunSequentialTests = new System.Windows.Forms.Button();
            this.btnRunSequentialTests.Text = "Run Sequential Tests";
            this.btnRunSequentialTests.Location = new System.Drawing.Point(190, 520);
            this.btnRunSequentialTests.Size = new System.Drawing.Size(160, 30);
            this.btnRunSequentialTests.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            this.btnRunSequentialTests.Click += new System.EventHandler(this.btnRunSequentialTests_Click);
            
            this.btnClose = new System.Windows.Forms.Button();
            this.btnClose.Text = "Close";
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnClose.Location = new System.Drawing.Point(580, 520);
            this.btnClose.Size = new System.Drawing.Size(80, 30);
            this.btnClose.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            
            // Add all controls to the form
            this.Controls.Add(this.headerPanel);
            this.Controls.Add(this.lblEndpoint);
            this.Controls.Add(this.cmbEndpoint);
            this.Controls.Add(this.lblAuthType);
            this.Controls.Add(this.cmbAuthType);
            this.Controls.Add(this.lblCustomToken);
            this.Controls.Add(this.txtCustomToken);
            this.Controls.Add(this.lblResults);
            this.Controls.Add(this.txtResults);
            this.Controls.Add(this.btnTestEndpoint);
            this.Controls.Add(this.btnRunSequentialTests);
            this.Controls.Add(this.btnClose);
            
            // Set the cancel button
            this.CancelButton = this.btnClose;
            
            // Register the form load event
            this.Load += new System.EventHandler(this.SimpleManualApiTestDialog_Load);
        }

        #endregion

        #region UI Event Handlers

        private void SimpleManualApiTestDialog_Load(object sender, EventArgs e)
        {
            // Set up default endpoints
            cmbEndpoint.Items.AddRange(new string[] {
                "/api/health",
                "/api/revit/test",
                "/api/tokens/validate", // Updated token validation endpoint
                "/api/user/revit-token", // Token generation endpoint (POST)
                "/api/parameter-mappings",
                "/api/projects/00000000-0000-0000-0000-000000000000/parameter-mappings",
                "/api/revit/tokens" // List user tokens endpoint
            });
            
            cmbEndpoint.SelectedIndex = 0;
            
            // Set up authentication types
            cmbAuthType.Items.AddRange(new string[] {
                "No Authentication",
                "Use Stored Token",
                "Use Custom Token"
            });
            
            cmbAuthType.SelectedIndex = 0;
            txtCustomToken.Enabled = false;
            
            // Ensure UI is ready
            System.Windows.Forms.Application.DoEvents();
        }

        private void cmbAuthType_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Enable/disable custom token textbox based on auth type
            txtCustomToken.Enabled = cmbAuthType.SelectedIndex == 2;
            
            // Update custom token setting
            _useCustomToken = cmbAuthType.SelectedIndex == 2;
        }

        private void txtCustomToken_TextChanged(object sender, EventArgs e)
        {
            if (_useCustomToken)
            {
                _customToken = txtCustomToken.Text;
            }
        }

        private async void btnTestEndpoint_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(cmbEndpoint.Text))
            {
                System.Windows.Forms.MessageBox.Show("Please select or enter an endpoint to test.", "Missing Endpoint", 
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                return;
            }
            
            SetControlsEnabled(false);
            ClearResults();
            
            try
            {
                SimpleEndpointTestResult result;
                string endpoint = cmbEndpoint.Text;
                string token = null;
                
                // Determine which token to use based on authentication type
                switch (cmbAuthType.SelectedIndex)
                {
                    case 0: // No Authentication
                        token = null;
                        break;
                    case 1: // Use Stored Token
                        var tokenService = new Services.ApiTokenService();
                        token = tokenService.GetToken();
                        break;
                    case 2: // Use Custom Token
                        token = txtCustomToken.Text;
                        break;
                }
                
                // Execute the test
                LogMessage($"Testing endpoint: {endpoint} {(token == null ? "without authentication" : "with authentication")}");
                
                // Special handling for token generation endpoint
                if (endpoint.Equals("/api/user/revit-token", StringComparison.OrdinalIgnoreCase))
                {
                    // For token generation endpoint, use POST with payload
                    var tokenRequest = new
                    {
                        name = "Test Token from Revit",
                        expirationDays = 30
                    };
                    string requestBody = Newtonsoft.Json.JsonConvert.SerializeObject(tokenRequest);
                    result = await SimpleApiTester.TestEndpointAsync(endpoint, token, "POST", requestBody);
                }
                else
                {
                    // Normal GET request for other endpoints
                    result = await SimpleApiTester.TestEndpointGetAsync(endpoint, token);
                }
                
                // Display the result
                DisplayResult(result);
            }
            catch (Exception ex)
            {
                LogError($"Error testing endpoint: {ex.Message}");
                Utils.Logger.LogError($"Manual API test error: {ex.Message}", LogSeverity.Warning);
            }
            finally
            {
                SetControlsEnabled(true);
            }
        }

        private async void btnRunSequentialTests_Click(object sender, EventArgs e)
        {
            SetControlsEnabled(false);
            ClearResults();
            
            try
            {
                LogMessage("Starting sequential API tests...");
                
                // Determine which token to use based on authentication type
                string token = null;
                switch (cmbAuthType.SelectedIndex)
                {
                    case 0: // No Authentication
                        token = null;
                        break;
                    case 1: // Use Stored Token
                        var tokenService = new Services.ApiTokenService();
                        token = tokenService.GetToken();
                        break;
                    case 2: // Use Custom Token
                        token = txtCustomToken.Text;
                        break;
                }
                
                // Run all tests
                var results = await SimpleApiTester.RunSequentialTestsAsync(token);
                
                // Display summary
                var successCount = results.Count(r => r.IsSuccessful);
                LogMessage($"Sequential tests completed. {successCount}/{results.Count} tests succeeded.");
                
                // Show each result
                foreach (var result in results)
                {
                    DisplayResult(result);
                }
            }
            catch (Exception ex)
            {
                LogError($"Error running sequential tests: {ex.Message}");
                Utils.Logger.LogError($"Manual API sequential tests error: {ex.Message}", LogSeverity.Warning);
            }
            finally
            {
                SetControlsEnabled(true);
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Close();
        }

        #endregion

        #region Helper Methods

        private void SetControlsEnabled(bool enabled)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SetControlsEnabled(enabled)));
                return;
            }
        
            btnTestEndpoint.Enabled = enabled;
            btnRunSequentialTests.Enabled = enabled;
            cmbEndpoint.Enabled = enabled;
            cmbAuthType.Enabled = enabled;
            txtCustomToken.Enabled = enabled && cmbAuthType.SelectedIndex == 2;
            
            // Update cursor
            this.Cursor = enabled ? System.Windows.Forms.Cursors.Default : System.Windows.Forms.Cursors.WaitCursor;
        }

        private void ClearResults()
        {
            if (txtResults.InvokeRequired)
            {
                txtResults.Invoke(new Action(() => ClearResults()));
                return;
            }
        
            txtResults.Clear();
        }

        private void DisplayResult(SimpleEndpointTestResult result)
        {
            if (result == null) return;
            
            // Add separator
            LogMessage(new string('-', 50));
            
            // Add basic info
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Endpoint: {result.Url}");
            sb.AppendLine($"Status: {(result.IsSuccessful ? "SUCCESS" : "FAILED")}");
            sb.AppendLine($"HTTP Status: {(int)result.StatusCode} ({result.StatusCode})");
            sb.AppendLine($"Response Time: {result.ResponseTime}ms");
            
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                sb.AppendLine($"Error: {result.ErrorMessage}");
            }
            
            if (result.TokenProvided && !string.IsNullOrEmpty(result.AuthHeader))
            {
                // Show only the first and last few characters of the token
                string maskedToken = MaskToken(result.AuthHeader.Replace("Bearer ", ""));
                sb.AppendLine($"Token Used: {maskedToken}");
            }
            
            LogMessage(sb.ToString());
            
            // Add response content if available
            if (!string.IsNullOrEmpty(result.ResponseContent))
            {
                LogMessage("Response Content:");
                LogMessage(FormatJsonIfPossible(result.ResponseContent));
            }
        }

        private string MaskToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return string.Empty;
                
            if (token.Length <= 10)
                return "***";
                
            return token.Substring(0, 4) + "..." + token.Substring(token.Length - 4);
        }

        private string FormatJsonIfPossible(string content)
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;
                
            try
            {
                // Try to parse and format as JSON
                var parsedJson = Newtonsoft.Json.JsonConvert.DeserializeObject(content);
                string formattedJson = Newtonsoft.Json.JsonConvert.SerializeObject(parsedJson, Newtonsoft.Json.Formatting.Indented);
                
                // Truncate if very long
                if (formattedJson.Length > 5000)
                {
                    formattedJson = formattedJson.Substring(0, 5000) + "\r\n... (truncated)";
                }
                
                return formattedJson;
            }
            catch
            {
                // If not valid JSON, return as is with possible truncation
                if (content.Length > 5000)
                {
                    return content.Substring(0, 5000) + "\r\n... (truncated)";
                }
                
                return content;
            }
        }

        private void LogMessage(string message)
        {
            if (txtResults.InvokeRequired)
            {
                txtResults.Invoke(new Action(() => LogMessage(message)));
                return;
            }
            
            txtResults.AppendText(message + Environment.NewLine);
            txtResults.ScrollToCaret();
        }

        private void LogError(string message)
        {
            if (txtResults.InvokeRequired)
            {
                txtResults.Invoke(new Action(() => LogError(message)));
                return;
            }
            
            // Use red text for errors
            System.Drawing.Color originalColor = txtResults.ForeColor;
            txtResults.SelectionStart = txtResults.TextLength;
            txtResults.SelectionLength = 0;
            txtResults.SelectionColor = System.Drawing.Color.Red;
            txtResults.AppendText(message + Environment.NewLine);
            txtResults.SelectionColor = originalColor;
            txtResults.ScrollToCaret();
        }

        #endregion
    }
}
