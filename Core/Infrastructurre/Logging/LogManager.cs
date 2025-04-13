using System;
using System.IO;
using System.Text;

namespace Miller_Craft_Tools.Core.Infrastructure.Logging
{
    public static class LogManager
    {
        // Log file path
        private static string _logFilePath;
        private static bool _loggingEnabled;
        private static LogLevel _logLevel;

        // Initialize the log manager
        public static void Initialize(string pluginPath)
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var logDir = Path.Combine(appDataPath, "Miller_Craft_Tools", "Logs");

            // Create the directory if it doesn't exist
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            // Create a log file for this session
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _logFilePath = Path.Combine(logDir, $"MillerCraftTools_{timestamp}.log");

            // Get logging settings
            _loggingEnabled = true; // Default to enabled, will be overridden by config
            _logLevel = LogLevel.Info; // Default to Info, will be overridden by config

            // Initial log entry
            LogInfo("Logging initialized");
        }

        // Set logging configuration
        public static void Configure(bool enabled, string logLevel)
        {
            _loggingEnabled = enabled;

            if (Enum.TryParse<LogLevel>(logLevel, true, out var level))
            {
                _logLevel = level;
            }

            LogInfo($"Logging configured: Enabled={_loggingEnabled}, Level={_logLevel}");
        }

        // Log methods for different levels
        public static void LogDebug(string message) => Log(LogLevel.Debug, message);
        public static void LogInfo(string message) => Log(LogLevel.Info, message);
        public static void LogWarning(string message) => Log(LogLevel.Warning, message);
        public static void LogError(string message) => Log(LogLevel.Error, message);

        // Main logging method
        private static void Log(LogLevel level, string message)
        {
            // Check if logging is enabled and if the level is sufficient
            if (!_loggingEnabled || level < _logLevel)
                return;

            try
            {
                // Format the log entry
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var logEntry = $"{timestamp} [{level}] {message}";

                // Write to the log file
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine, Encoding.UTF8);

                // Also output to debug console
                System.Diagnostics.Debug.WriteLine(logEntry);
            }
            catch
            {
                // Silently fail if logging fails
            }
        }
    }

    // Log level enum
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3
    }
}