using System;
using System.Windows.Forms;
using System.Drawing;
using Miller_Craft_Tools.Utils;

namespace Miller_Craft_Tools.UI.Styles
{
    /// <summary>
    /// Enumeration of possible sync statuses
    /// </summary>
    public enum SyncStatus
    {
        Idle,
        Uploading,
        Pending,
        Processing,
        Complete,
        Error
    }
    
    /// <summary>
    /// Status indicator control that provides consistent visual feedback about sync status
    /// </summary>
    public class StatusIndicator : UserControl
    {
        private Label _statusLabel;
        private PictureBox _statusIcon;
        private SyncStatus _currentStatus;
        
        /// <summary>
        /// Gets or sets the current sync status
        /// </summary>
        public SyncStatus Status
        {
            get => _currentStatus;
            set
            {
                _currentStatus = value;
                UpdateDisplay();
            }
        }
        
        /// <summary>
        /// Gets or sets additional status text to display
        /// </summary>
        public string StatusText { get; set; }
        
        /// <summary>
        /// Creates a new status indicator control
        /// </summary>
        public StatusIndicator()
        {
            this.Height = 24;
            this.AutoSize = true;
            
            _statusIcon = new PictureBox();
            _statusIcon.Width = 16;
            _statusIcon.Height = 16;
            _statusIcon.SizeMode = PictureBoxSizeMode.Zoom;
            _statusIcon.Location = new Point(0, 0);
            
            _statusLabel = new Label();
            _statusLabel.AutoSize = true;
            _statusLabel.Location = new Point(20, 0);
            
            this.Controls.Add(_statusIcon);
            this.Controls.Add(_statusLabel);
            
            // Set default status
            Status = SyncStatus.Idle;
        }
        
        /// <summary>
        /// Updates the display based on current status
        /// </summary>
        private void UpdateDisplay()
        {
            string iconName = string.Empty;
            string statusText = string.Empty;
            Color textColor = Color.Gray;
            
            switch (Status)
            {
                case SyncStatus.Idle:
                    statusText = Terms.SyncStatusIdle;
                    iconName = IconProvider.IconNames.StatusIdle;
                    textColor = Color.Gray;
                    break;
                    
                case SyncStatus.Uploading:
                    statusText = Terms.SyncStatusUploading;
                    iconName = IconProvider.IconNames.StatusUploading;
                    textColor = BrandColors.PrimaryColor;
                    break;
                    
                case SyncStatus.Pending:
                    statusText = Terms.SyncStatusPending;
                    iconName = IconProvider.IconNames.StatusPending;
                    textColor = BrandColors.WarningColor;
                    break;
                    
                case SyncStatus.Processing:
                    statusText = Terms.SyncStatusProcessing;
                    iconName = IconProvider.IconNames.StatusProcessing;
                    textColor = BrandColors.PrimaryColor;
                    break;
                    
                case SyncStatus.Complete:
                    statusText = Terms.SyncStatusComplete;
                    iconName = IconProvider.IconNames.StatusComplete;
                    textColor = BrandColors.SuccessColor;
                    break;
                    
                case SyncStatus.Error:
                    statusText = Terms.SyncStatusError;
                    iconName = IconProvider.IconNames.StatusError;
                    textColor = BrandColors.ErrorColor;
                    break;
            }
            
            // Set status text, appending any additional text if available
            if (!string.IsNullOrEmpty(StatusText))
            {
                statusText = $"{statusText}: {StatusText}";
            }
            
            _statusLabel.Text = statusText;
            _statusLabel.ForeColor = textColor;
            
            // Set icon if available
            try
            {
                _statusIcon.Image = IconProvider.GetIcon(iconName);
            }
            catch (Exception ex)
            {
                // Log error but don't crash if icon is missing
                Logger.LogError($"Error loading status icon: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Updates the status and optional additional text
        /// </summary>
        /// <param name="status">New status</param>
        /// <param name="statusText">Optional additional text</param>
        public void UpdateStatus(SyncStatus status, string statusText = null)
        {
            StatusText = statusText;
            Status = status;
        }
    }
}
