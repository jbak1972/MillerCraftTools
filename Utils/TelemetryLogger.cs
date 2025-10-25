using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Miller_Craft_Tools.Utils
{
    /// <summary>
    /// Enhanced logging system with telemetry capabilities
    /// </summary>
    public static class TelemetryLogger
    {
        // Enable anonymous error reporting with opt-out option
        public static bool EnableAnonymousReporting { get; set; } = true;
        
        // Configurable verbosity levels
        public enum LogLevel 
        { 
            Error, 
            Warning, 
            Info, 
            Debug, 
            Verbose 
        }
        
        // Current log level setting
        public static LogLevel CurrentLogLevel { get; set; } = LogLevel.Info;
        
        // Base path for logs
        private static string LogDirectory => 
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                        "Miller Craft Assistant", "Logs");
                        
        // Log file path
        private static string LogFilePath => 
            Path.Combine(LogDirectory, $"miller_craft_{DateTime.Now:yyyy-MM-dd}.log");
            
        // Telemetry endpoint (dummy value, replace with actual endpoint)
        private static string TelemetryEndpoint = "https://app.millercraftllc.com/api/telemetry";
        
        // Session ID for grouping log entries
        private static string _sessionId = Guid.NewGuid().ToString();
        
        // Track performance
        private static Dictionary<string, Stopwatch> _activeTimers = new Dictionary<string, Stopwatch>();
        
        /// <summary>
        /// Logs a message with the specified level
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="level">Log level</param>
        public static void Log(string message, LogLevel level = LogLevel.Info)
        {
            // Only log if the level is at or below the current log level
            if ((int)level > (int)CurrentLogLevel)
                return;
                
            try
            {
                // Ensure directory exists
                Directory.CreateDirectory(LogDirectory);
                
                // Format log entry
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] [{_sessionId}] {message}";
                
                // Write to log file
                File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
                
                // Also output to debug console
                Debug.WriteLine(logEntry);
            }
            catch (Exception ex)
            {
                // If logging fails, output to debug console
                Debug.WriteLine($"ERROR LOGGING: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Logs an error message
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="ex">Optional exception</param>
        public static void LogError(string message, Exception ex = null)
        {
            string errorMessage = message;
            
            if (ex != null)
            {
                errorMessage += $" | Exception: {ex.GetType().Name}: {ex.Message}";
                if (ex.StackTrace != null)
                {
                    errorMessage += $" | Stack: {ex.StackTrace.Split('\n')[0]}";
                }
            }
            
            Log(errorMessage, LogLevel.Error);
            
            // Send telemetry for errors if enabled
            if (EnableAnonymousReporting)
            {
                SendTelemetry(new Dictionary<string, object>
                {
                    ["level"] = "error",
                    ["message"] = message,
                    ["exceptionType"] = ex?.GetType().Name,
                    ["timestamp"] = DateTime.UtcNow
                });
            }
        }
        
        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="message">Warning message</param>
        public static void LogWarning(string message)
        {
            Log(message, LogLevel.Warning);
        }
        
        /// <summary>
        /// Logs an informational message
        /// </summary>
        /// <param name="message">Info message</param>
        public static void LogInfo(string message)
        {
            Log(message, LogLevel.Info);
        }
        
        /// <summary>
        /// Logs a debug message
        /// </summary>
        /// <param name="message">Debug message</param>
        public static void LogDebug(string message)
        {
            Log(message, LogLevel.Debug);
        }
        
        /// <summary>
        /// Logs a verbose message
        /// </summary>
        /// <param name="message">Verbose message</param>
        public static void LogVerbose(string message)
        {
            Log(message, LogLevel.Verbose);
        }
        
        /// <summary>
        /// Logs an object as JSON
        /// </summary>
        /// <param name="obj">Object to log</param>
        /// <param name="category">Optional category for the log</param>
        /// <param name="level">Log level</param>
        public static void LogJson(object obj, string category = null, LogLevel level = LogLevel.Debug)
        {
            try
            {
                string json = JsonConvert.SerializeObject(obj, Formatting.Indented);
                string message = string.IsNullOrEmpty(category) 
                    ? json 
                    : $"[{category}] {json}";
                    
                Log(message, level);
            }
            catch (Exception ex)
            {
                LogError($"Failed to serialize object for logging: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Logs with performance metrics
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="metrics">Performance metrics</param>
        /// <param name="level">Log level</param>
        public static void LogWithMetrics(string message, Dictionary<string, object> metrics, LogLevel level = LogLevel.Info)
        {
            try
            {
                // Add metrics to JSON
                string metricsJson = JsonConvert.SerializeObject(metrics);
                string logMessage = $"{message} | Metrics: {metricsJson}";
                
                Log(logMessage, level);
                
                // Send telemetry if enabled
                if (EnableAnonymousReporting)
                {
                    metrics["message"] = message;
                    metrics["level"] = level.ToString().ToLower();
                    metrics["timestamp"] = DateTime.UtcNow;
                    
                    SendTelemetry(metrics);
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to log with metrics: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Starts a performance timer
        /// </summary>
        /// <param name="operationName">Name of the operation to time</param>
        public static void StartTimer(string operationName)
        {
            var sw = new Stopwatch();
            sw.Start();
            _activeTimers[operationName] = sw;
            
            LogDebug($"Started timer for operation: {operationName}");
        }
        
        /// <summary>
        /// Stops a timer and records the elapsed time
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <returns>Elapsed time</returns>
        public static TimeSpan StopTimerAndRecord(string operationName)
        {
            if (_activeTimers.TryGetValue(operationName, out var sw))
            {
                sw.Stop();
                _activeTimers.Remove(operationName);
                
                // Log and return elapsed time
                var elapsed = sw.Elapsed;
                
                LogWithMetrics(
                    $"Operation {operationName} completed in {elapsed.TotalMilliseconds:0.00}ms",
                    new Dictionary<string, object> 
                    { 
                        ["operationName"] = operationName,
                        ["durationMs"] = elapsed.TotalMilliseconds 
                    },
                    LogLevel.Info);
                    
                return elapsed;
            }
            
            LogWarning($"No timer found for operation: {operationName}");
            return TimeSpan.Zero;
        }
        
        /// <summary>
        /// Sends telemetry data to the server
        /// </summary>
        /// <param name="data">Telemetry data</param>
        private static void SendTelemetry(Dictionary<string, object> data)
        {
            if (!EnableAnonymousReporting)
                return;
                
            // Add common telemetry fields
            data["sessionId"] = _sessionId;
            data["pluginVersion"] = typeof(TelemetryLogger).Assembly.GetName().Version.ToString();
            data["revitVersion"] = GetRevitVersion();
            data["hostname"] = Environment.MachineName;
            
            // Don't block on telemetry send
            Task.Run(() => SendTelemetryAsync(data));
        }
        
        /// <summary>
        /// Sends telemetry data asynchronously
        /// </summary>
        /// <param name="data">Telemetry data</param>
        private static async Task SendTelemetryAsync(Dictionary<string, object> data)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    // Set timeout to avoid blocking
                    client.Timeout = TimeSpan.FromSeconds(5);
                    
                    // Serialize the data
                    string json = JsonConvert.SerializeObject(data);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    
                    // Send the data
                    var response = await client.PostAsync(TelemetryEndpoint, content);
                    
                    // Log result at verbose level
                    LogVerbose($"Telemetry sent - Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                // Log failure but don't propagate exception
                Debug.WriteLine($"Failed to send telemetry: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Tracks an exception for telemetry purposes
        /// </summary>
        /// <param name="ex">Exception to track</param>
        /// <param name="category">Error category</param>
        /// <param name="additionalData">Additional contextual data</param>
        public static void TrackException(Exception ex, string category, object additionalData = null)
        {
            if (ex == null)
                return;
                
            try
            {
                // Log locally first
                string errorMessage = $"[{category}] Exception: {ex.GetType().Name}: {ex.Message}";
                Log(errorMessage, LogLevel.Error);
                
                // Only send telemetry if enabled
                if (!EnableAnonymousReporting)
                    return;
                    
                // Prepare telemetry data
                var data = new Dictionary<string, object>
                {
                    ["level"] = "error",
                    ["category"] = category,
                    ["exceptionType"] = ex.GetType().Name,
                    ["exceptionMessage"] = ex.Message,
                    ["timestamp"] = DateTime.UtcNow,
                    ["stackTrace"] = ex.StackTrace
                };
                
                // Add inner exception info if available
                if (ex.InnerException != null)
                {
                    data["innerExceptionType"] = ex.InnerException.GetType().Name;
                    data["innerExceptionMessage"] = ex.InnerException.Message;
                }
                
                // Add additional contextual data if provided
                if (additionalData != null)
                {
                    data["contextData"] = additionalData;
                }
                
                // Send the telemetry
                SendTelemetry(data);
            }
            catch (Exception logEx)
            {
                // Avoid recursion
                Debug.WriteLine($"Failed to track exception: {logEx.Message}");
            }
        }
        
        /// <summary>
        /// Gets the current Revit version
        /// </summary>
        /// <returns>Revit version string or "Unknown"</returns>
        private static string GetRevitVersion()
        {
            try
            {
                // Try to get Revit version from executing assembly
                string assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string directoryName = Path.GetDirectoryName(assemblyLocation);
                
                if (directoryName != null && directoryName.Contains("Revit"))
                {
                    // Extract version from path (e.g., "Revit 2022")
                    string[] parts = directoryName.Split(new[] { "Revit" }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1)
                    {
                        string versionPart = parts[1].Trim();
                        if (versionPart.StartsWith(" "))
                        {
                            return "Revit" + versionPart;
                        }
                    }
                }
                
                return "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}
