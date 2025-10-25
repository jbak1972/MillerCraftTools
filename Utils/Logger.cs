using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Miller_Craft_Tools.Utils
{
    public static class Logger
    {
        private static readonly string LogDirectoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Miller Craft Assistant");

        static Logger()
        {
            // Make sure the log directory exists
            if (!Directory.Exists(LogDirectoryPath))
            {
                try
                {
                    Directory.CreateDirectory(LogDirectoryPath);
                }
                catch (Exception ex)
                {
                    // Fall back to temp directory if we can't create in the home folder
                    LogDirectoryPath = Path.Combine(Path.GetTempPath(), "Miller Craft Assistant");
                    if (!Directory.Exists(LogDirectoryPath))
                    {
                        Directory.CreateDirectory(LogDirectoryPath);
                    }
                }
            }
        }

        /// <summary>
        /// Logs a JSON object to the user's home directory in the Miller Craft Assistant folder
        /// </summary>
        /// <param name="jsonObject">The object to serialize and log</param>
        /// <param name="prefix">Optional prefix for the filename</param>
        /// <returns>The path to the created file</returns>
        public static string LogJson(object jsonObject, string prefix = "json")
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string fileName = $"{prefix}_{timestamp}.json";
                string filePath = Path.Combine(LogDirectoryPath, fileName);

                // Format the JSON nicely
                var json = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);
                File.WriteAllText(filePath, json);

                return filePath;
            }
            catch (Exception ex)
            {
                LogError($"Failed to log JSON: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Logs an HTTP request to the user's home directory
        /// </summary>
        /// <param name="request">The HTTP request message</param>
        /// <param name="requestBody">The request body if available</param>
        /// <returns>The path to the created file</returns>
        /// <summary>
        /// Non-blocking version of LogHttpRequestAsync that won't cause UI thread issues
        /// </summary>
        public static void LogHttpRequestNonBlocking(HttpRequestMessage request, HttpContent requestBody = null)
        {
            // Fire and forget - don't block on logging
            Task.Run(() => LogHttpRequestAsync(request, requestBody));
        }

        public static async Task<string> LogHttpRequestAsync(HttpRequestMessage request, HttpContent requestBody = null)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string fileName = $"request_{timestamp}.txt";
                string filePath = Path.Combine(LogDirectoryPath, fileName);

                using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    writer.WriteLine($"REQUEST: {request.Method} {request.RequestUri}");
                    writer.WriteLine($"TIME: {DateTime.Now}");
                    writer.WriteLine("HEADERS:");
                    foreach (var header in request.Headers)
                    {
                        writer.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
                    }

                    writer.WriteLine();
                    writer.WriteLine("CONTENT:");
                    
                    if (requestBody != null)
                    {
                        var content = await requestBody.ReadAsStringAsync();
                        writer.WriteLine(content);
                    }
                    else if (request.Content != null)
                    {
                        var content = await request.Content.ReadAsStringAsync();
                        writer.WriteLine(content);
                    }
                    else
                    {
                        writer.WriteLine("[No content]");
                    }
                }

                return filePath;
            }
            catch (Exception ex)
            {
                LogError($"Failed to log HTTP request: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Logs an HTTP response to the user's home directory
        /// </summary>
        /// <param name="response">The HTTP response message</param>
        /// <returns>The path to the created file</returns>
        /// <summary>
        /// Non-blocking version of LogHttpResponseAsync that won't cause UI thread issues
        /// </summary>
        public static void LogHttpResponseNonBlocking(HttpResponseMessage response)
        {
            // Fire and forget - don't block on logging
            Task.Run(() => LogHttpResponseAsync(response));
        }

        public static async Task<string> LogHttpResponseAsync(HttpResponseMessage response)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string fileName = $"response_{timestamp}.txt";
                string filePath = Path.Combine(LogDirectoryPath, fileName);

                using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    writer.WriteLine($"RESPONSE: {(int)response.StatusCode} {response.StatusCode}");
                    writer.WriteLine($"TIME: {DateTime.Now}");
                    writer.WriteLine("HEADERS:");
                    foreach (var header in response.Headers)
                    {
                        writer.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
                    }

                    writer.WriteLine();
                    writer.WriteLine("CONTENT:");
                    
                    if (response.Content != null)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        writer.WriteLine(content);
                        
                        // Try to format JSON nicely if the content is JSON
                        if (response.Content.Headers.ContentType?.MediaType == "application/json")
                        {
                            try
                            {
                                var jsonObj = JObject.Parse(content);
                                string formattedJson = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
                                
                                // Also save as a separate JSON file
                                string jsonFileName = $"response_json_{timestamp}.json";
                                string jsonFilePath = Path.Combine(LogDirectoryPath, jsonFileName);
                                File.WriteAllText(jsonFilePath, formattedJson);
                            }
                            catch (JsonReaderException)
                            {
                                // Not valid JSON, ignore and continue
                            }
                        }
                    }
                    else
                    {
                        writer.WriteLine("[No content]");
                    }
                }

                return filePath;
            }
            catch (Exception ex)
            {
                LogError($"Failed to log HTTP response: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Logs an informational message to the info log file
        /// </summary>
        /// <param name="message">The informational message to log</param>
        public static void LogInfo(string message)
        {
            try
            {
                string filePath = Path.Combine(LogDirectoryPath, "info.log");
                string logEntry = $"{DateTime.Now}: {message}";
                
                // Append to the log file
                using (var writer = new StreamWriter(filePath, true, Encoding.UTF8))
                {
                    writer.WriteLine(logEntry);
                }
            }
            catch
            {
                // If we can't log the info, there's not much we can do
            }
        }
        
        /// <summary>
        /// Logs a debug message to the debug log file
        /// </summary>
        /// <param name="message">The debug message to log</param>
        public static void LogDebug(string message)
        {
            try
            {
                string filePath = Path.Combine(LogDirectoryPath, "debug.log");
                string logEntry = $"{DateTime.Now}: {message}";
                
                // Append to the log file
                using (var writer = new StreamWriter(filePath, true, Encoding.UTF8))
                {
                    writer.WriteLine(logEntry);
                }
                
                // Also write to debug output
                System.Diagnostics.Debug.WriteLine($"DEBUG: {message}");
            }
            catch
            {
                // If we can't log the debug info, there's not much we can do
            }
        }
        
        /// <summary>
        /// Logs a warning message to the warning log file
        /// </summary>
        /// <param name="message">The warning message to log</param>
        public static void LogWarning(string message)
        {
            try
            {
                string filePath = Path.Combine(LogDirectoryPath, "warnings.log");
                string logEntry = $"{DateTime.Now}: {message}";
                
                // Append to the log file
                using (var writer = new StreamWriter(filePath, true, Encoding.UTF8))
                {
                    writer.WriteLine(logEntry);
                }
                
                // Also write to debug output
                System.Diagnostics.Debug.WriteLine($"WARNING: {message}");
            }
            catch
            {
                // If we can't log the warning, there's not much we can do
            }
        }
        
        /// <summary>
        /// Logs an error message to the error log file
        /// </summary>
        /// <param name="message">The error message to log</param>
        /// <param name="severity">Optional severity level for the error</param>
        public static void LogError(string message, LogSeverity severity = LogSeverity.Error)
        {
            try
            {
                string filePath = Path.Combine(LogDirectoryPath, "errors.log");
                string logEntry = $"{DateTime.Now}: [{severity}] {message}";
                
                // Append to the log file
                using (var writer = new StreamWriter(filePath, true, Encoding.UTF8))
                {
                    writer.WriteLine(logEntry);
                }
            }
            catch
            {
                // If we can't log the error, there's not much we can do
            }
        }
        
        // Lock object to prevent multiple threads from writing to the consolidated log simultaneously
        private static readonly object _consolidatedLogLock = new object();
        
        /// <summary>
        /// Logs a JSON entry to a consolidated log file with timestamps, allowing multiple events to be stored in a single file
        /// </summary>
        /// <param name="jsonObject">The object to serialize and log</param>
        /// <param name="eventType">Event type identifier (e.g., "cmd_start", "cmd_execution")</param>
        /// <param name="logFileName">Name of the consolidated log file (default: "consolidated_log.json")</param>
        /// <returns>True if logging was successful</returns>
        public static bool LogJsonConsolidated(object jsonObject, string eventType, string logFileName = "consolidated_log.json")
        {
            try
            {
                // Create a wrapper with timestamp and event type
                var logEntry = new {
                    Timestamp = DateTime.Now,
                    EventType = eventType,
                    Data = jsonObject
                };
                
                // Format as a single line JSON string
                string jsonLine = JsonConvert.SerializeObject(logEntry);
                
                string filePath = Path.Combine(LogDirectoryPath, logFileName);
                
                // Use lock to prevent multiple threads from writing simultaneously
                lock (_consolidatedLogLock)
                {
                    // Create file with opening bracket if it doesn't exist
                    if (!File.Exists(filePath))
                    {
                        File.WriteAllText(filePath, "[\n");
                    }
                    else
                    {
                        // Make sure the file is valid JSON array format
                        string content = File.ReadAllText(filePath).Trim();
                        if (content.EndsWith("]"))
                        {
                            // Remove closing bracket to allow appending
                            content = content.Substring(0, content.Length - 1).TrimEnd();
                            File.WriteAllText(filePath, content);
                            
                            // Add comma if there are existing entries
                            if (!content.EndsWith("[") && !content.EndsWith("[\n"))
                            {
                                using (var writer = new StreamWriter(filePath, true, Encoding.UTF8))
                                {
                                    writer.Write(",\n");
                                }
                            }
                        }
                        else if (!content.EndsWith(",") && !content.EndsWith("[") && !content.EndsWith("[\n"))
                        {
                            // Add comma if needed
                            using (var writer = new StreamWriter(filePath, true, Encoding.UTF8))
                            {
                                writer.Write(",\n");
                            }
                        }
                    }
                    
                    // Append the new entry
                    using (var writer = new StreamWriter(filePath, true, Encoding.UTF8))
                    {
                        writer.Write("  " + jsonLine);
                        writer.Write("\n]");
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to log to consolidated JSON: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Non-blocking version of LogJsonConsolidated that won't cause UI thread issues
        /// </summary>
        public static void LogJsonConsolidatedNonBlocking(object jsonObject, string eventType, string logFileName = "consolidated_log.json")
        {
            // Fire and forget - don't block on logging
            Task.Run(() => LogJsonConsolidated(jsonObject, eventType, logFileName));
        }
    }
}
