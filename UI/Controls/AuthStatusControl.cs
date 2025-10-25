using System;
using System.Windows.Forms;
using System.Drawing;
using Miller_Craft_Tools.UI.Styles;
using Miller_Craft_Tools.Utils;
using Miller_Craft_Tools.Model;

namespace Miller_Craft_Tools.UI.Controls
{
    /// <summary>
    /// Reusable control for displaying authentication status
    /// </summary>
    public class AuthStatusControl : UserControl
    {
        private StatusIndicator _statusIndicator;
        private Label _statusIndicatorLabel;
        private Label _userInfoLabel;
        private Label _statusMessageLabel;

        /// <summary>
        /// Gets or sets the status message
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessageLabel.Text;
            set => _statusMessageLabel.Text = value;
        }

        /// <summary>
        /// Gets or sets the user info message
        /// </summary>
        public string UserInfo
        {
            get => _userInfoLabel.Text;
            set => _userInfoLabel.Text = value;
        }

        /// <summary>
        /// Sets the status indicator state
        /// </summary>
        /// <param name="status">The current sync status</param>
        /// <param name="statusText">Optional status text</param>
        public void SetStatus(Miller_Craft_Tools.UI.Styles.SyncStatus status, string statusText = null)
        {
            _statusIndicator.UpdateStatus(status, statusText);
        }

        /// <summary>
        /// Sets the status message with specified color
        /// </summary>
        /// <param name="message">Message to display</param>
        /// <param name="color">Text color</param>
        public void SetStatusMessage(string message, Color color)
        {
            _statusMessageLabel.Text = message;
            _statusMessageLabel.ForeColor = color;
        }

        /// <summary>
        /// Shows or hides the user info label
        /// </summary>
        /// <param name="visible">Whether the label should be visible</param>
        public void ShowUserInfo(bool visible)
        {
            _userInfoLabel.Visible = visible;
        }

        /// <summary>
        /// Creates a new AuthStatusControl
        /// </summary>
        public AuthStatusControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // Auth status indicator label
            _statusIndicatorLabel = new Label
            {
                Text = "Status:",
                Location = new Point(UISettings.StandardPadding, UISettings.StandardPadding),
                AutoSize = true
            };
            UISettings.ApplyBodyStyle(_statusIndicatorLabel);
            Controls.Add(_statusIndicatorLabel);
            
            // Status indicator
            _statusIndicator = new StatusIndicator
            {
                Location = new Point(UISettings.StandardPadding, _statusIndicatorLabel.Bottom + UISettings.StandardPadding)
            };
            Controls.Add(_statusIndicator);

            // User info label
            _userInfoLabel = new Label
            {
                Location = new Point(UISettings.StandardPadding, _statusIndicator.Bottom + UISettings.StandardPadding),
                Size = new Size(350, 20),
                AutoSize = true,
                Visible = false
            };
            UISettings.ApplyBodyStyle(_userInfoLabel);
            Controls.Add(_userInfoLabel);

            // Status message label
            _statusMessageLabel = new Label
            {
                Location = new Point(UISettings.StandardPadding, _userInfoLabel.Bottom + UISettings.StandardPadding),
                Size = new Size(350, 20),
                AutoSize = true
            };
            UISettings.ApplyBodyStyle(_statusMessageLabel);
            Controls.Add(_statusMessageLabel);

            // Set control size
            Height = _statusMessageLabel.Bottom + UISettings.StandardPadding;
            Width = 400;
        }
    }
}
