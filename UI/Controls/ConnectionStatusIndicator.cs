using System;
using System.Drawing;
using System.Windows.Forms;
using Miller_Craft_Tools.UI.Styles;
using Miller_Craft_Tools.Services;

namespace Miller_Craft_Tools.UI.Controls
{
    /// <summary>
    /// A small control that indicates the connection status 
    /// for use in the Revit ribbon
    /// </summary>
    public class ConnectionStatusIndicator : UserControl
    {
        private PictureBox _statusIcon;
        private ToolTip _tooltip;
        private AuthenticationService _authService;
        
        /// <summary>
        /// Gets the current status display color
        /// </summary>
        public Color StatusColor { get; private set; }
        
        /// <summary>
        /// Gets the current status message
        /// </summary>
        public string StatusMessage { get; private set; }
        
        /// <summary>
        /// Creates a new connection status indicator
        /// </summary>
        public ConnectionStatusIndicator()
        {
            // Initialize authentication service
            _authService = new AuthenticationService();
            
            // Initialize the control
            InitializeComponent();
            
            // Update status initially
            UpdateConnectionStatus();
        }
        
        private void InitializeComponent()
        {
            // Create status icon
            _statusIcon = new PictureBox();
            _statusIcon.Size = new Size(16, 16);
            _statusIcon.Location = new Point(0, 0);
            _statusIcon.SizeMode = PictureBoxSizeMode.StretchImage;
            Controls.Add(_statusIcon);
            
            // Create tooltip for the icon
            _tooltip = new ToolTip();
            _tooltip.InitialDelay = 100;
            _tooltip.AutoPopDelay = 5000;
            
            // Set control size based on the icon
            Size = new Size(16, 16);
            
            // Handle click event
            this.Click += ConnectionStatusIndicator_Click;
            _statusIcon.Click += ConnectionStatusIndicator_Click;
        }
        
        /// <summary>
        /// Initializes and starts the connection status monitoring
        /// </summary>
        public void Initialize()
        {
            // This method is called from the Revit ribbon setup
            // to explicitly start monitoring after the control is created
            UpdateConnectionStatus();
        }
        
        /// <summary>
        /// Updates the connection status indicator based on current authentication state
        /// </summary>
        public void UpdateConnectionStatus()
        {
            try
            {
                // Check if we're authenticated
                bool isAuthenticated = _authService.IsAuthenticated();
                
                if (isAuthenticated)
                {
                    // Check if the token is valid
                    bool hasValidToken = Model.UserSettings.Load().HasValidToken();
                    
                    if (hasValidToken)
                    {
                        // Green: Connected with valid token
                        StatusColor = BrandColors.SuccessColor;
                        StatusMessage = "Connected with valid API token";
                        _statusIcon.BackColor = StatusColor;
                        _tooltip.SetToolTip(_statusIcon, StatusMessage);
                    }
                    else
                    {
                        // Yellow: Connected but token needs validation
                        StatusColor = BrandColors.WarningColor;
                        StatusMessage = "Connected but token may need validation";
                        _statusIcon.BackColor = StatusColor;
                        _tooltip.SetToolTip(_statusIcon, StatusMessage);
                    }
                }
                else
                {
                    // Red: Not connected or token invalid
                    StatusColor = BrandColors.ErrorColor;
                    StatusMessage = "Not connected or token invalid";
                    _statusIcon.BackColor = StatusColor;
                    _tooltip.SetToolTip(_statusIcon, StatusMessage);
                }
            }
            catch (Exception ex)
            {
                // In case of error, set to error state
                StatusColor = BrandColors.ErrorColor;
                StatusMessage = $"Error checking connection status: {ex.Message}";
                _statusIcon.BackColor = StatusColor;
                _tooltip.SetToolTip(_statusIcon, StatusMessage);
                
                // Log the error
                Utils.Logger.LogError($"Error updating connection status: {ex.Message}");
            }
        }
        
        private void ConnectionStatusIndicator_Click(object sender, EventArgs e)
        {
            try
            {
                // When clicked, open the Connection Manager dialog
                using (var dialog = new ConnectionManagerDialog())
                {
                    dialog.ShowDialog();
                }
                
                // Update status after dialog closes
                UpdateConnectionStatus();
            }
            catch (Exception ex)
            {
                Utils.Logger.LogError($"Error opening Connection Manager from indicator: {ex.Message}");
            }
        }
    }
}
