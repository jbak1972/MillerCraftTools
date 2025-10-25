using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Miller_Craft_Tools.UI.Styles
{
    /// <summary>
    /// Defines brand colors that match exactly with the web application
    /// </summary>
    public static class BrandColors
    {
        // Match these exactly to web app colors
        public static System.Windows.Media.Color Primary = System.Windows.Media.Color.FromRgb(59, 130, 246);  // Blue-500
        public static System.Windows.Media.Color Secondary = System.Windows.Media.Color.FromRgb(107, 114, 128);  // Gray-500
        public static System.Windows.Media.Color Success = System.Windows.Media.Color.FromRgb(34, 197, 94);  // Green-500
        public static System.Windows.Media.Color Warning = System.Windows.Media.Color.FromRgb(234, 179, 8);  // Yellow-500
        public static System.Windows.Media.Color Error = System.Windows.Media.Color.FromRgb(239, 68, 68);  // Red-500
        
        // Convert to System.Drawing.Color for Windows Forms
        public static System.Drawing.Color GetSystemDrawingColor(System.Windows.Media.Color color)
        {
            return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
        }
        
        // Convert to Brush for WPF elements
        public static System.Windows.Media.SolidColorBrush GetBrush(System.Windows.Media.Color color)
        {
            return new System.Windows.Media.SolidColorBrush(color);
        }
        
        // Convenience methods for commonly used colors
        public static System.Drawing.Color PrimaryColor => GetSystemDrawingColor(Primary);
        public static System.Drawing.Color SecondaryColor => GetSystemDrawingColor(Secondary);
        public static System.Drawing.Color SuccessColor => GetSystemDrawingColor(Success);
        public static System.Drawing.Color WarningColor => GetSystemDrawingColor(Warning);
        public static System.Drawing.Color ErrorColor => GetSystemDrawingColor(Error);
        
        /// <summary>
        /// Applies the Miller Craft brand theme to a Windows Forms component
        /// </summary>
        /// <param name="form">The form or control to apply the theme to</param>
        public static void ApplyTheme(Form form)
        {
            if (form == null) return;
            
            // Set form colors
            form.BackColor = System.Drawing.Color.White;
            form.ForeColor = System.Drawing.SystemColors.ControlText;
            
            // Apply to all contained buttons
            foreach (Control control in form.Controls)
            {
                ApplyThemeToControl(control);
            }
        }
        
        /// <summary>
        /// Recursively applies theme to controls and their children
        /// </summary>
        private static void ApplyThemeToControl(Control control)
        {
            if (control is Button button)
            {
                button.BackColor = PrimaryColor;
                button.ForeColor = System.Drawing.Color.White;
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderColor = PrimaryColor;
                button.FlatAppearance.BorderSize = 1;
            }
            else if (control is Panel || control is GroupBox || control is TableLayoutPanel || control is FlowLayoutPanel)
            {
                // Container controls should be recursively processed
                foreach (Control childControl in control.Controls)
                {
                    ApplyThemeToControl(childControl);
                }
            }
        }
    }
}
