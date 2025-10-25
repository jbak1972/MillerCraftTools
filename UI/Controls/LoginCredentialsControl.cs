using System;
using System.Windows.Forms;
using System.Drawing;
using Miller_Craft_Tools.UI.Styles;
using Miller_Craft_Tools.Utils;

namespace Miller_Craft_Tools.UI.Controls
{
    /// <summary>
    /// Reusable control for login credentials input
    /// </summary>
    public class LoginCredentialsControl : UserControl
    {
        private TextBox _usernameTextBox;
        private TextBox _passwordTextBox;
        private Label _usernameLabel;
        private Label _passwordLabel;

        /// <summary>
        /// Gets or sets the username value
        /// </summary>
        public string Username
        {
            get => _usernameTextBox.Text.Trim();
            set => _usernameTextBox.Text = value;
        }

        /// <summary>
        /// Gets or sets the password value
        /// </summary>
        public string Password
        {
            get => _passwordTextBox.Text;
            set => _passwordTextBox.Text = value;
        }

        /// <summary>
        /// Gets or sets whether the control is enabled
        /// </summary>
        public new bool Enabled
        {
            get => base.Enabled;
            set
            {
                base.Enabled = value;
                _usernameTextBox.Enabled = value;
                _passwordTextBox.Enabled = value;
            }
        }

        /// <summary>
        /// Event raised when input values change
        /// </summary>
        public event EventHandler InputChanged;

        /// <summary>
        /// Creates a new LoginCredentialsControl
        /// </summary>
        public LoginCredentialsControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // Username label and textbox
            _usernameLabel = new Label
            {
                Text = Terms.UsernameText + ":",
                Location = new Point(UISettings.StandardPadding, UISettings.StandardPadding),
                AutoSize = true
            };
            UISettings.ApplyBodyStyle(_usernameLabel);
            Controls.Add(_usernameLabel);
            
            _usernameTextBox = new TextBox
            {
                Location = new Point(UISettings.StandardPadding, _usernameLabel.Bottom + UISettings.StandardPadding),
                Size = new Size(350, 20)
            };
            UISettings.ApplyTextBoxStyle(_usernameTextBox);
            _usernameTextBox.TextChanged += (s, e) => InputChanged?.Invoke(this, EventArgs.Empty);
            Controls.Add(_usernameTextBox);
            
            // Password label and textbox
            _passwordLabel = new Label
            {
                Text = Terms.PasswordText + ":",
                Location = new Point(UISettings.StandardPadding, _usernameTextBox.Bottom + UISettings.WidePadding),
                AutoSize = true
            };
            UISettings.ApplyBodyStyle(_passwordLabel);
            Controls.Add(_passwordLabel);
            
            _passwordTextBox = new TextBox
            {
                Location = new Point(UISettings.StandardPadding, _passwordLabel.Bottom + UISettings.StandardPadding),
                Size = new Size(350, 20),
                PasswordChar = '*'
            };
            UISettings.ApplyTextBoxStyle(_passwordTextBox);
            _passwordTextBox.TextChanged += (s, e) => InputChanged?.Invoke(this, EventArgs.Empty);
            Controls.Add(_passwordTextBox);

            // Set control size
            Height = _passwordTextBox.Bottom + UISettings.StandardPadding;
            Width = 400;
        }

        /// <summary>
        /// Validates that both username and password have values
        /// </summary>
        /// <returns>True if both fields have values</returns>
        public bool Validate()
        {
            return !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password);
        }

        /// <summary>
        /// Clears the password field
        /// </summary>
        public void ClearPassword()
        {
            _passwordTextBox.Text = string.Empty;
        }

        /// <summary>
        /// Sets focus to the username field
        /// </summary>
        public void FocusUsername()
        {
            _usernameTextBox.Focus();
        }
    }
}
