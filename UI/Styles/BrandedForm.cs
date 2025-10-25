using System;
using System.Windows.Forms;
using System.Drawing;

namespace Miller_Craft_Tools.UI.Styles
{
    /// <summary>
    /// Base form class that applies consistent branding and styling for all dialogs
    /// </summary>
    public class BrandedForm : Form
    {
        protected Panel HeaderPanel { get; private set; }
        protected Panel ContentPanel { get; private set; }
        protected Panel FooterPanel { get; private set; }
        
        /// <summary>
        /// Creates a new branded form with consistent styling
        /// </summary>
        public BrandedForm()
        {
            // Apply standard form styling
            UISettings.ApplyFormDefaults(this);
            
            // Set up the layout
            InitializeLayout();
        }
        
        /// <summary>
        /// Initializes the standard layout with header, content, and footer panels
        /// </summary>
        private void InitializeLayout()
        {
            // Create header panel with logo
            HeaderPanel = new Panel();
            HeaderPanel.Dock = DockStyle.Top;
            HeaderPanel.Height = 50;
            HeaderPanel.BackColor = BrandColors.PrimaryColor;
            
            PictureBox logoImage = new PictureBox();
            logoImage.Image = IconProvider.GetIcon(IconProvider.IconNames.LogoWhite);
            logoImage.SizeMode = PictureBoxSizeMode.Zoom;
            logoImage.Height = 40;
            logoImage.Width = 150;
            logoImage.Location = new Point(UISettings.StandardPadding, 5);
            
            // Add a form title label
            Label titleLabel = new Label();
            titleLabel.Text = this.Text;
            titleLabel.ForeColor = Color.White;
            titleLabel.Font = new Font(UISettings.DefaultFontFamily, UISettings.SubheadingSize, FontStyle.Bold);
            titleLabel.AutoSize = true;
            titleLabel.Location = new Point(logoImage.Right + UISettings.StandardPadding, 15);
            
            HeaderPanel.Controls.Add(logoImage);
            HeaderPanel.Controls.Add(titleLabel);
            
            // Create content panel
            ContentPanel = new Panel();
            ContentPanel.Dock = DockStyle.Fill;
            ContentPanel.Padding = new Padding(UISettings.StandardPadding);
            ContentPanel.BackColor = Color.White;
            
            // Create footer panel
            FooterPanel = new Panel();
            FooterPanel.Dock = DockStyle.Bottom;
            FooterPanel.Height = 50;
            FooterPanel.Padding = new Padding(UISettings.StandardPadding);
            FooterPanel.BackColor = Color.WhiteSmoke;
            
            // Add panels to form
            this.Controls.Add(ContentPanel);
            this.Controls.Add(FooterPanel);
            this.Controls.Add(HeaderPanel);
            
            // Update the form title when the Text property changes
            this.TextChanged += (sender, e) => 
            {
                if (titleLabel != null)
                    titleLabel.Text = this.Text;
            };
        }
        
        /// <summary>
        /// Adds a standard close button to the footer
        /// </summary>
        protected void AddCloseButton()
        {
            Button closeButton = new Button();
            closeButton.Text = Terms.CloseButtonText;
            closeButton.DialogResult = DialogResult.Cancel;
            closeButton.Location = new Point(FooterPanel.Width - UISettings.WidePadding - 100, UISettings.StandardPadding);
            closeButton.Size = new Size(100, 30);
            closeButton.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            
            UISettings.ApplyOutlineButtonStyle(closeButton);
            
            FooterPanel.Controls.Add(closeButton);
            this.CancelButton = closeButton;
        }
        
        /// <summary>
        /// Adds a primary action button to the footer
        /// </summary>
        /// <param name="text">Button text</param>
        /// <param name="clickHandler">Click event handler</param>
        /// <returns>The created button</returns>
        protected Button AddPrimaryButton(string text, EventHandler clickHandler)
        {
            Button primaryButton = new Button();
            primaryButton.Text = text;
            primaryButton.Location = new Point(FooterPanel.Width - UISettings.WidePadding - 210, UISettings.StandardPadding);
            primaryButton.Size = new Size(100, 30);
            primaryButton.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            primaryButton.Click += clickHandler;
            
            UISettings.ApplyPrimaryButtonStyle(primaryButton);
            
            FooterPanel.Controls.Add(primaryButton);
            this.AcceptButton = primaryButton;
            
            return primaryButton;
        }
        
        /// <summary>
        /// Adds a status message to the footer panel
        /// </summary>
        /// <param name="message">Status message to display</param>
        /// <param name="isError">True if this is an error message</param>
        protected Label AddStatusMessage(string message, bool isError = false)
        {
            Label statusLabel = new Label();
            statusLabel.Text = message;
            statusLabel.ForeColor = isError ? BrandColors.ErrorColor : Color.DarkGray;
            statusLabel.AutoSize = true;
            statusLabel.Location = new Point(UISettings.StandardPadding, UISettings.StandardPadding + 5);
            statusLabel.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            
            FooterPanel.Controls.Add(statusLabel);
            
            return statusLabel;
        }
    }
}
