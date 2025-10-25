using System;
using System.Windows.Forms;
using System.Drawing;

namespace Miller_Craft_Tools.UI.Styles
{
    /// <summary>
    /// Defines consistent typography and layout settings to match the web application style
    /// </summary>
    public static class UISettings
    {
        // Font sizes
        public static float HeadingSize = 16f;
        public static float SubheadingSize = 14f;
        public static float BodySize = 12f;
        public static float SmallSize = 10f;
        
        // Standard padding (in pixels)
        public static int StandardPadding = 8;
        public static int TightPadding = 4;
        public static int WidePadding = 16;
        
        // Default font family - using system fonts since we can't load custom fonts in Revit
        public static FontFamily DefaultFontFamily = FontFamily.GenericSansSerif;
        
        // Apply consistent font styling for different text roles
        public static void ApplyHeadingStyle(Label label)
        {
            label.Font = new Font(DefaultFontFamily, HeadingSize, FontStyle.Bold);
            label.ForeColor = Color.Black;
        }
        
        public static void ApplySubheadingStyle(Label label)
        {
            label.Font = new Font(DefaultFontFamily, SubheadingSize, FontStyle.Bold);
            label.ForeColor = Color.Black;
        }
        
        public static void ApplyBodyStyle(Label label)
        {
            label.Font = new Font(DefaultFontFamily, BodySize, FontStyle.Regular);
            label.ForeColor = Color.Black;
        }
        
        public static void ApplySmallStyle(Label label)
        {
            label.Font = new Font(DefaultFontFamily, SmallSize, FontStyle.Regular);
            label.ForeColor = Color.DarkGray;
        }
        
        // Button styling
        public static void ApplyPrimaryButtonStyle(Button button)
        {
            button.BackColor = BrandColors.PrimaryColor;
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
            button.Font = new Font(DefaultFontFamily, BodySize, FontStyle.Bold);
            button.Padding = new Padding(TightPadding);
            button.UseVisualStyleBackColor = false;
        }
        
        public static void ApplySecondaryButtonStyle(Button button)
        {
            button.BackColor = BrandColors.SecondaryColor;
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
            button.Font = new Font(DefaultFontFamily, BodySize, FontStyle.Regular);
            button.Padding = new Padding(TightPadding);
            button.UseVisualStyleBackColor = false;
        }
        
        public static void ApplyOutlineButtonStyle(Button button)
        {
            button.BackColor = Color.White;
            button.ForeColor = BrandColors.PrimaryColor;
            button.FlatStyle = FlatStyle.Flat;
            button.Font = new Font(DefaultFontFamily, BodySize, FontStyle.Regular);
            button.FlatAppearance.BorderColor = BrandColors.PrimaryColor;
            button.Padding = new Padding(TightPadding);
            button.UseVisualStyleBackColor = false;
        }
        
        // Panel styling
        public static void ApplyCardStyle(Panel panel)
        {
            panel.BorderStyle = BorderStyle.FixedSingle;
            panel.BackColor = Color.White;
            panel.Padding = new Padding(StandardPadding);
        }
        
        // TextBox styling
        public static void ApplyTextBoxStyle(TextBox textBox)
        {
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.Font = new Font(DefaultFontFamily, BodySize);
            textBox.BackColor = System.Drawing.Color.White;
            textBox.ForeColor = System.Drawing.Color.Black;
        }
        
        // Form defaults
        public static Size DefaultFormSize = new Size(600, 400);
        public static FormStartPosition DefaultStartPosition = FormStartPosition.CenterScreen;
        public static FormBorderStyle DefaultFormBorderStyle = FormBorderStyle.FixedDialog;
        
        // Apply standard form styling
        public static void ApplyFormDefaults(Form form)
        {
            form.BackColor = Color.White;
            form.Font = new Font(DefaultFontFamily, BodySize);
            form.Padding = new Padding(StandardPadding);
            form.FormBorderStyle = DefaultFormBorderStyle;
            form.StartPosition = DefaultStartPosition;
            form.MinimizeBox = true;
            form.MaximizeBox = false;
        }
    }
}
