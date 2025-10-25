using System;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Windows.Forms;

namespace Miller_Craft_Tools
{
    // DEPRECATED: Use Miller_Craft_Tools.Model.UserSettings for all settings and tokens.
    public static class PluginSettings
    {
        private static string SettingsPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MillerCraftTools", "settings.json");

        // Deprecated: Use UserSettings.Load().ApiToken and Save() instead.
        public static string GetToken()
        {
            return Miller_Craft_Tools.Model.UserSettings.Load().ApiToken;
        }

        public static void SetToken(string token)
        {
            var settings = Miller_Craft_Tools.Model.UserSettings.Load();
            settings.ApiToken = token;
            settings.Save();
        }
        
        /// <summary>
        /// Gets the OAuth2 refresh token
        /// </summary>
        public static string GetRefreshToken()
        {
            return Miller_Craft_Tools.Model.UserSettings.Load().RefreshToken;
        }
        
        /// <summary>
        /// Sets the OAuth2 refresh token
        /// </summary>
        public static void SetRefreshToken(string refreshToken)
        {
            var settings = Miller_Craft_Tools.Model.UserSettings.Load();
            settings.RefreshToken = refreshToken;
            settings.Save();
        }
        
        /// <summary>
        /// Gets the token expiration time as ISO 8601 string
        /// </summary>
        public static string GetTokenExpiration()
        {
            return Miller_Craft_Tools.Model.UserSettings.Load().TokenExpiration;
        }
        
        /// <summary>
        /// Sets the token expiration time as ISO 8601 string
        /// </summary>
        public static void SetTokenExpiration(string expirationTime)
        {
            var settings = Miller_Craft_Tools.Model.UserSettings.Load();
            settings.TokenExpiration = expirationTime;
            settings.Save();
        }

        private static string PromptForToken()
        {
            string token = "";
            var prompt = new Form()
            {
                Width = 500,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Miller Craft Tools - Token Required",
                StartPosition = FormStartPosition.CenterScreen
            };
            var textLabel = new Label() { Left = 50, Top = 20, Text = "Enter your Revit API token (copy from your profile page):", Width = 400 };
            var textBox = new TextBox() { Left = 50, Top = 50, Width = 400 };
            var confirmation = new Button() { Text = "OK", Left = 350, Width = 100, Top = 80, DialogResult = DialogResult.OK };
            confirmation.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;
            if (prompt.ShowDialog() == DialogResult.OK)
            {
                token = textBox.Text;
            }
            return token;
        }
    }
}
