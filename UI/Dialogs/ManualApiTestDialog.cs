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
    /// Dialog for manual step-by-step API testing
    /// </summary>
    public partial class ManualApiTestDialog : BrandedForm
    {
        private string _customToken = null;
        private bool _useCustomToken = false;

        public ManualApiTestDialog()
        {
            // Initialize the component before trying to adjust the layout
            InitializeComponent();
            
            // The base BrandedForm uses its own panels which may be hiding our controls
            // Ensure our dialog settings are applied correctly
            this.BackColor = System.Drawing.SystemColors.Control;
            this.AutoScroll = true;
        }

        #region UI Event Handlers
        
        /// <summary>
        /// Override OnLoad to ensure our controls are visible
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            
            try
            {
                // Special handling for BrandedForm setup
                // The ContentPanel should be created by BrandedForm but move all our controls there
                if (base.ContentPanel != null)
                {
                    // Collect controls we want to move to avoid collection modification issues
                    List<Control> controlsToMove = new List<Control>();
                    
                    // Find all non-panel controls that we need to move
                    foreach (Control ctrl in Controls)
                    {
                        if (ctrl != base.HeaderPanel && ctrl != base.ContentPanel && ctrl != base.FooterPanel)
                        {
                            controlsToMove.Add(ctrl);
                        }
                    }
                    
                    // Now move them all to the content panel
                    foreach (Control ctrl in controlsToMove)
                    {
                        Controls.Remove(ctrl);
                        base.ContentPanel.Controls.Add(ctrl);
                        
                        // Adjust for panel padding
                        ctrl.Left -= base.ContentPanel.Padding.Left;
                        ctrl.Top -= base.ContentPanel.Padding.Top;
                    }
                    
                    // Ensure layout updates are applied
                    base.ContentPanel.PerformLayout();
                    this.PerformLayout();
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"UI Layout Error: {ex.Message}", "Dialog Initialization Error", 
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        private void ManualApiTestDialog_Load(object sender, EventArgs e)
        {
            // Set up default endpoints
            cmbEndpoint.Items.AddRange(new string[] {
                "/api/health",
                "/api/revit/test",
                "/api/tokens/validate",
                "/api/parameter-mappings",
                "/api/projects/00000000-0000-0000-0000-000000000000/parameter-mappings"
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
                result = await SimpleApiTester.TestEndpointGetAsync(endpoint, token);
                
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
