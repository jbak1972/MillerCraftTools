using System;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
// Using LogSeverity enum for detailed error categorization

namespace Miller_Craft_Tools.Utils
{
    /// <summary>
    /// Enhanced error logging specifically for network and SSL/TLS related issues
    /// </summary>
    public static class NetworkErrorLogger
    {
        /// <summary>
        /// Logs detailed information about network exceptions with specific handling for SSL/TLS issues
        /// </summary>
        /// <param name="ex">The exception to analyze</param>
        /// <param name="url">The URL that was being accessed when the exception occurred</param>
        /// <param name="operationName">Name of the operation being performed</param>
        public static void LogNetworkException(Exception ex, string url, string operationName)
        {
            StringBuilder detailedError = new StringBuilder();
            detailedError.AppendLine($"Network Error in operation '{operationName}' to URL: {url}");
            
            // Start with the most specific exception types
            if (ex is AuthenticationException authEx)
            {
                detailedError.AppendLine("SSL/TLS AUTHENTICATION ERROR");
                detailedError.AppendLine($"Authentication failed: {authEx.Message}");
                detailedError.AppendLine("Possible causes: Certificate validation failed, protocol mismatch, or certificate trust issues.");
                
                Logger.LogError(detailedError.ToString(), Miller_Craft_Tools.Utils.LogSeverity.Critical);
                TelemetryLogger.TrackException(ex, "ssl_authentication_error", new { url, details = detailedError.ToString() });
            }
            else if (ex is HttpRequestException reqEx)
            {
                if (reqEx.InnerException is AuthenticationException || 
                    reqEx.Message.Contains("SSL") || reqEx.Message.Contains("TLS") || 
                    reqEx.Message.Contains("certificate") || reqEx.Message.Contains("trust"))
                {
                    detailedError.AppendLine("SSL/TLS CONNECTION ERROR");
                    detailedError.AppendLine($"Details: {reqEx.Message}");
                    detailedError.AppendLine("Possible causes: Certificate validation failed, protocol mismatch, or certificate trust issues.");
                    
                    Logger.LogError(detailedError.ToString(), Miller_Craft_Tools.Utils.LogSeverity.Critical);
                    TelemetryLogger.TrackException(ex, "ssl_connection_error", new { url, details = detailedError.ToString() });
                }
                else
                {
                    detailedError.AppendLine("HTTP REQUEST ERROR");
                    detailedError.AppendLine($"Details: {reqEx.Message}");
                    detailedError.AppendLine($"Inner Exception: {reqEx.InnerException?.Message ?? "None"}");
                    
                    Logger.LogError(detailedError.ToString());
                    TelemetryLogger.TrackException(ex, "http_request_error", new { url, details = detailedError.ToString() });
                }
            }
            else if (ex is SocketException sockEx)
            {
                detailedError.AppendLine("NETWORK SOCKET ERROR");
                detailedError.AppendLine($"Error Code: {sockEx.ErrorCode}, Native Error: {sockEx.NativeErrorCode}");
                detailedError.AppendLine($"Details: {sockEx.Message}");
                detailedError.AppendLine("Possible causes: Firewall blocking, DNS resolution failure, or network connectivity issues.");
                
                Logger.LogError(detailedError.ToString());
                TelemetryLogger.TrackException(ex, "socket_error", new { url, errorCode = sockEx.ErrorCode, details = detailedError.ToString() });
            }
            else if (ex is TimeoutException)
            {
                detailedError.AppendLine("CONNECTION TIMEOUT");
                detailedError.AppendLine($"Details: {ex.Message}");
                detailedError.AppendLine("Possible causes: Server not responding, network congestion, or firewall blocking with no rejection.");
                
                Logger.LogError(detailedError.ToString());
                TelemetryLogger.TrackException(ex, "timeout_error", new { url, details = detailedError.ToString() });
            }
            else
            {
                detailedError.AppendLine("GENERAL NETWORK ERROR");
                detailedError.AppendLine($"Exception Type: {ex.GetType().Name}");
                detailedError.AppendLine($"Details: {ex.Message}");
                
                if (ex.InnerException != null)
                {
                    detailedError.AppendLine($"Inner Exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                }
                
                Logger.LogError(detailedError.ToString());
                TelemetryLogger.TrackException(ex, "network_error", new { url, details = detailedError.ToString() });
            }
            
            // Always log the stack trace for all network errors
            Logger.LogDebug($"Stack Trace for {operationName} error:\n{ex.StackTrace}");
        }
    }
}
