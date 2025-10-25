using System;
using Miller_Craft_Tools.Services;
using Miller_Craft_Tools.UI.Styles;

namespace Miller_Craft_Tools.UI.Dialogs
{
    /// <summary>
    /// Dialog for managing API tokens
    /// </summary>
    public partial class ApiTokenDialog : System.Windows.Forms.Form
    {
        private readonly ApiTokenService _apiTokenService;
        private bool _isTokenValid = false;
        
        public ApiTokenDialog()
        {
            InitializeComponent();
            _apiTokenService = new ApiTokenService();
            
            // Load existing token if any
            LoadExistingToken();
        }
        
        private void LoadExistingToken()
        {
            string token = _apiTokenService.GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                // Mask the token for display (show only first and last 4 chars)
                string maskedToken = MaskToken(token);
                tokenTextBox.Text = maskedToken;
                
                // Check if token is valid according to local validation
                _isTokenValid = _apiTokenService.IsTokenValid();
                
                // Update status
                UpdateTokenStatus();
            }
        }
        
        private string MaskToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return string.Empty;
            
            if (token.Length <= 8)
                return token;
                
            int visibleChars = 4;
            string firstPart = token.Substring(0, visibleChars);
            string lastPart = token.Substring(token.Length - visibleChars);
            
            return $"{firstPart}...{lastPart}";
        }
        
        private void UpdateTokenStatus()
        {
            if (string.IsNullOrEmpty(_apiTokenService.GetToken()))
            {
                tokenStatusLabel.Text = "No token configured";
                tokenStatusLabel.ForeColor = System.Drawing.Color.Red;
                validateButton.Enabled = false;
                removeTokenButton.Enabled = false;
            }
            else
            {
                if (_isTokenValid)
                {
                    tokenStatusLabel.Text = "Token is configured";
                    tokenStatusLabel.ForeColor = System.Drawing.Color.Green;
                }
                else
                {
                    tokenStatusLabel.Text = "Token may be expired";
                    tokenStatusLabel.ForeColor = System.Drawing.Color.Orange;
                }
                validateButton.Enabled = true;
                removeTokenButton.Enabled = true;
            }
        }
        
        private async void saveTokenButton_Click(object sender, EventArgs e)
        {
            string token = newTokenTextBox.Text.Trim();
            
            if (string.IsNullOrEmpty(token))
            {
                System.Windows.Forms.MessageBox.Show(
                    "Please enter a valid API token",
                    "Empty Token",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Warning);
                return;
            }
            
            try
            {
                // Save the token
                _apiTokenService.StoreToken(token);
                
                // Update the UI
                tokenTextBox.Text = MaskToken(token);
                newTokenTextBox.Text = string.Empty;
                _isTokenValid = true;
                UpdateTokenStatus();
                
                // Show success message
                System.Windows.Forms.MessageBox.Show(
                    "API token saved successfully",
                    "Success",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(
                    $"Failed to save API token: {ex.Message}",
                    "Error",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
            }
        }
        
        private void removeTokenButton_Click(object sender, EventArgs e)
        {
            if (System.Windows.Forms.MessageBox.Show(
                "Are you sure you want to remove the API token? This will require re-entering a token to access the API.",
                "Confirm Removal",
                System.Windows.Forms.MessageBoxButtons.YesNo,
                System.Windows.Forms.MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
            {
                _apiTokenService.ClearToken();
                tokenTextBox.Text = string.Empty;
                _isTokenValid = false;
                UpdateTokenStatus();
            }
        }
        
        private async void validateButton_Click(object sender, EventArgs e)
        {
            validateButton.Enabled = false;
            validateButton.Text = "Validating...";
            tokenStatusLabel.Text = "Checking token...";
            
            try
            {
                bool isValid = await _apiTokenService.ValidateTokenWithApiAsync();
                
                if (isValid)
                {
                    tokenStatusLabel.Text = "Token is valid";
                    tokenStatusLabel.ForeColor = System.Drawing.Color.Green;
                    _isTokenValid = true;
                    
                    System.Windows.Forms.MessageBox.Show(
                        "API token is valid and working correctly.",
                        "Validation Success",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Information);
                }
                else
                {
                    tokenStatusLabel.Text = "Token is invalid";
                    tokenStatusLabel.ForeColor = System.Drawing.Color.Red;
                    _isTokenValid = false;
                    
                    System.Windows.Forms.MessageBox.Show(
                        "API token is invalid or has expired. Please request a new token.",
                        "Validation Failed",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                tokenStatusLabel.Text = "Validation failed";
                tokenStatusLabel.ForeColor = System.Drawing.Color.Red;
                
                System.Windows.Forms.MessageBox.Show(
                    $"Failed to validate token: {ex.Message}",
                    "Error",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
            }
            finally
            {
                validateButton.Enabled = true;
                validateButton.Text = "Validate Token";
            }
        }
        
        private void closeButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            Close();
        }
        
        private void helpButton_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.MessageBox.Show(
                "To generate an API token:\n\n" +
                "1. Log into the Miller Craft Assistant web app\n" +
                "2. Go to Account Settings > API Access\n" +
                "3. Click 'Generate New Token'\n" +
                "4. Enter a descriptive name and select permissions\n" +
                "5. Copy the generated token and paste it here\n\n" +
                "Note: The token will only be shown once when generated.",
                "API Token Help",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Information);
        }
        
        #region Designer Generated Code
        
        private void InitializeComponent()
        {
            this.labelCurrentToken = new System.Windows.Forms.Label();
            this.tokenTextBox = new System.Windows.Forms.TextBox();
            this.validateButton = new System.Windows.Forms.Button();
            this.removeTokenButton = new System.Windows.Forms.Button();
            this.groupBoxNewToken = new System.Windows.Forms.GroupBox();
            this.saveTokenButton = new System.Windows.Forms.Button();
            this.newTokenTextBox = new System.Windows.Forms.TextBox();
            this.labelNewToken = new System.Windows.Forms.Label();
            this.tokenStatusLabel = new System.Windows.Forms.Label();
            this.closeButton = new System.Windows.Forms.Button();
            this.helpButton = new System.Windows.Forms.Button();
            this.groupBoxNewToken.SuspendLayout();
            this.SuspendLayout();
            // 
            // labelCurrentToken
            this.labelCurrentToken.AutoSize = true;
            this.labelCurrentToken.Location = new System.Drawing.Point(12, 15);
            this.labelCurrentToken.Name = "labelCurrentToken";
            this.labelCurrentToken.Size = new System.Drawing.Size(81, 13);
            this.labelCurrentToken.TabIndex = 0;
            this.labelCurrentToken.Text = "Current API Token:";
            // 
            // tokenTextBox
            this.tokenTextBox.Location = new System.Drawing.Point(12, 31);
            this.tokenTextBox.Name = "tokenTextBox";
            this.tokenTextBox.ReadOnly = true;
            this.tokenTextBox.Size = new System.Drawing.Size(456, 20);
            this.tokenTextBox.TabIndex = 1;
            // 
            // validateButton
            this.validateButton.Enabled = false;
            this.validateButton.Location = new System.Drawing.Point(12, 57);
            this.validateButton.Name = "validateButton";
            this.validateButton.Size = new System.Drawing.Size(95, 23);
            this.validateButton.TabIndex = 2;
            this.validateButton.Text = "Validate Token";
            this.validateButton.UseVisualStyleBackColor = true;
            this.validateButton.Click += new System.EventHandler(this.validateButton_Click);
            // 
            // removeTokenButton
            this.removeTokenButton.Enabled = false;
            this.removeTokenButton.Location = new System.Drawing.Point(113, 57);
            this.removeTokenButton.Name = "removeTokenButton";
            this.removeTokenButton.Size = new System.Drawing.Size(95, 23);
            this.removeTokenButton.TabIndex = 3;
            this.removeTokenButton.Text = "Remove Token";
            this.removeTokenButton.UseVisualStyleBackColor = true;
            this.removeTokenButton.Click += new System.EventHandler(this.removeTokenButton_Click);
            // 
            // groupBoxNewToken
            this.groupBoxNewToken.Controls.Add(this.saveTokenButton);
            this.groupBoxNewToken.Controls.Add(this.newTokenTextBox);
            this.groupBoxNewToken.Controls.Add(this.labelNewToken);
            this.groupBoxNewToken.Location = new System.Drawing.Point(12, 101);
            this.groupBoxNewToken.Name = "groupBoxNewToken";
            this.groupBoxNewToken.Size = new System.Drawing.Size(456, 95);
            this.groupBoxNewToken.TabIndex = 4;
            this.groupBoxNewToken.TabStop = false;
            this.groupBoxNewToken.Text = "Add New Token";
            // 
            // saveTokenButton
            this.saveTokenButton.Location = new System.Drawing.Point(9, 59);
            this.saveTokenButton.Name = "saveTokenButton";
            this.saveTokenButton.Size = new System.Drawing.Size(95, 23);
            this.saveTokenButton.TabIndex = 2;
            this.saveTokenButton.Text = "Save Token";
            this.saveTokenButton.UseVisualStyleBackColor = true;
            this.saveTokenButton.Click += new System.EventHandler(this.saveTokenButton_Click);
            // 
            // newTokenTextBox
            this.newTokenTextBox.Location = new System.Drawing.Point(9, 33);
            this.newTokenTextBox.Name = "newTokenTextBox";
            this.newTokenTextBox.Size = new System.Drawing.Size(441, 20);
            this.newTokenTextBox.TabIndex = 1;
            // 
            // labelNewToken
            this.labelNewToken.AutoSize = true;
            this.labelNewToken.Location = new System.Drawing.Point(6, 16);
            this.labelNewToken.Name = "labelNewToken";
            this.labelNewToken.Size = new System.Drawing.Size(244, 13);
            this.labelNewToken.TabIndex = 0;
            this.labelNewToken.Text = "Enter new API token from Miller Craft Assistant web app:";
            // 
            // tokenStatusLabel
            this.tokenStatusLabel.AutoSize = true;
            this.tokenStatusLabel.Location = new System.Drawing.Point(214, 62);
            this.tokenStatusLabel.Name = "tokenStatusLabel";
            this.tokenStatusLabel.Size = new System.Drawing.Size(92, 13);
            this.tokenStatusLabel.TabIndex = 5;
            this.tokenStatusLabel.Text = "No token configured";
            this.tokenStatusLabel.ForeColor = System.Drawing.Color.Red;
            // 
            // closeButton
            this.closeButton.Location = new System.Drawing.Point(373, 202);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(95, 23);
            this.closeButton.TabIndex = 6;
            this.closeButton.Text = "Close";
            this.closeButton.UseVisualStyleBackColor = true;
            this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
            // 
            // helpButton
            this.helpButton.Location = new System.Drawing.Point(12, 202);
            this.helpButton.Name = "helpButton";
            this.helpButton.Size = new System.Drawing.Size(95, 23);
            this.helpButton.TabIndex = 7;
            this.helpButton.Text = "Help";
            this.helpButton.UseVisualStyleBackColor = true;
            this.helpButton.Click += new System.EventHandler(this.helpButton_Click);
            // 
            // ApiTokenDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(480, 237);
            this.Controls.Add(this.helpButton);
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.tokenStatusLabel);
            this.Controls.Add(this.groupBoxNewToken);
            this.Controls.Add(this.removeTokenButton);
            this.Controls.Add(this.validateButton);
            this.Controls.Add(this.tokenTextBox);
            this.Controls.Add(this.labelCurrentToken);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ApiTokenDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Miller Craft API Token Management";
            this.groupBoxNewToken.ResumeLayout(false);
            this.groupBoxNewToken.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        
        #endregion
        
        private System.Windows.Forms.Label labelCurrentToken;
        private System.Windows.Forms.TextBox tokenTextBox;
        private System.Windows.Forms.Button validateButton;
        private System.Windows.Forms.Button removeTokenButton;
        private System.Windows.Forms.GroupBox groupBoxNewToken;
        private System.Windows.Forms.Button saveTokenButton;
        private System.Windows.Forms.TextBox newTokenTextBox;
        private System.Windows.Forms.Label labelNewToken;
        private System.Windows.Forms.Label tokenStatusLabel;
        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.Button helpButton;
    }
}
