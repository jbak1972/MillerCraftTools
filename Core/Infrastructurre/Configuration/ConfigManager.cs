using System;
using System.IO;
using System.Xml.Serialization;

namespace Miller_Craft_Tools.Core.Infrastructure.Configuration
{
    public static class ConfigManager
    {
        // Configuration instance
        private static PluginConfig _config;
        private static string _configPath;

        // Initialize the configuration manager
        public static void Initialize(string pluginPath)
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var configDir = Path.Combine(appDataPath, "Miller_Craft_Tools");

            // Create the directory if it doesn't exist
            if (!Directory.Exists(configDir))
                Directory.CreateDirectory(configDir);

            _configPath = Path.Combine(configDir, "config.xml");

            // Load or create the configuration
            LoadConfiguration();
        }

        // Get the configuration
        public static PluginConfig GetConfig()
        {
            return _config;
        }

        // Save the configuration
        public static void SaveConfig()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(PluginConfig));
                using (var writer = new StreamWriter(_configPath))
                {
                    serializer.Serialize(writer, _config);
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                LogManager.LogError($"Error saving configuration: {ex.Message}");
            }
        }

        // Load the configuration
        private static void LoadConfiguration()
        {
            if (File.Exists(_configPath))
            {
                try
                {
                    var serializer = new XmlSerializer(typeof(PluginConfig));
                    using (var reader = new StreamReader(_configPath))
                    {
                        _config = (PluginConfig)serializer.Deserialize(reader);
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception and create a default config
                    LogManager.LogError($"Error loading configuration: {ex.Message}");
                    _config = new PluginConfig();
                }
            }
            else
            {
                // Create a default configuration
                _config = new PluginConfig();
                SaveConfig();
            }
        }
    }

    // Configuration class
    [Serializable]
    public class PluginConfig
    {
        // General settings
        public string UserName { get; set; }
        public bool EnableLogging { get; set; } = true;
        public string LogLevel { get; set; } = "Info";

        // Feature-specific settings
        public EfficiencyToolsConfig EfficiencyTools { get; set; } = new EfficiencyToolsConfig();
        public StandardsManagementConfig StandardsManagement { get; set; } = new StandardsManagementConfig();
        public ProjectSetupConfig ProjectSetup { get; set; } = new ProjectSetupConfig();
    }

    // Feature-specific configuration classes
    [Serializable]
    public class EfficiencyToolsConfig
    {
        public bool EnableJournalAnalysis { get; set; } = true;
        public string JournalDirectory { get; set; }
    }

    [Serializable]
    public class StandardsManagementConfig
    {
        public string TemplateDirectory { get; set; }
        public string FamilyLibraryDirectory { get; set; }
    }

    [Serializable]
    public class ProjectSetupConfig
    {
        public string DefaultProjectTemplate { get; set; }
    }
}