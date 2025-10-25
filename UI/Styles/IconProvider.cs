using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.IO;
using Miller_Craft_Tools.Utils;

namespace Miller_Craft_Tools.UI.Styles
{
    /// <summary>
    /// Provides consistent icons that match or closely resemble those in the web application
    /// </summary>
    public static class IconProvider
    {
        private static Dictionary<string, Bitmap> _iconCache = new Dictionary<string, Bitmap>();
        
        /// <summary>
        /// Gets an icon by name, loading it from embedded resources
        /// </summary>
        /// <param name="iconName">The name of the icon (without extension)</param>
        /// <returns>The icon as a bitmap, or null if not found</returns>
        public static Bitmap GetIcon(string iconName)
        {
            if (_iconCache.ContainsKey(iconName))
                return _iconCache[iconName];
                
            try
            {
                // Load from embedded resources - use same icon naming as web app
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = $"Miller_Craft_Tools.Resources.{iconName}.png";
                
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        Logger.LogError($"Icon not found: {iconName}");
                        return null;
                    }
                        
                    var bitmap = new Bitmap(stream);
                    _iconCache[iconName] = bitmap;
                    return bitmap;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error loading icon {iconName}: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Checks if an icon exists in the embedded resources
        /// </summary>
        /// <param name="iconName">The name of the icon (without extension)</param>
        /// <returns>True if the icon exists</returns>
        public static bool IconExists(string iconName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"Miller_Craft_Tools.Resources.{iconName}.png";
            
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                return stream != null;
            }
        }
        
        /// <summary>
        /// Gets all available icon names
        /// </summary>
        /// <returns>A list of available icon names</returns>
        public static List<string> GetAvailableIcons()
        {
            var icons = new List<string>();
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();
            
            string prefix = "Miller_Craft_Tools.Resources.";
            string extension = ".png";
            
            foreach (var resourceName in resourceNames)
            {
                if (resourceName.StartsWith(prefix) && resourceName.EndsWith(extension))
                {
                    // Extract just the icon name without prefix or extension
                    string iconName = resourceName.Substring(prefix.Length, resourceName.Length - prefix.Length - extension.Length);
                    icons.Add(iconName);
                }
            }
            
            return icons;
        }
        
        /// <summary>
        /// Common icons used throughout the application
        /// </summary>
        public static class IconNames
        {
            public const string Sync = "sync_icon";
            public const string Settings = "settings_icon";
            public const string Success = "success_icon";
            public const string Warning = "warning_icon";
            public const string Error = "error_icon";
            public const string Info = "info_icon";
            public const string StatusIdle = "status_idle";
            public const string StatusUploading = "status_uploading";
            public const string StatusPending = "status_pending";
            public const string StatusProcessing = "status_processing";
            public const string StatusComplete = "status_complete";
            public const string StatusError = "status_error";
            public const string Logo = "millercraft_logo";
            public const string LogoWhite = "millercraft_logo_white";
        }
    }
}
