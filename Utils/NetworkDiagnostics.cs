using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Miller_Craft_Tools.Utils
{
    /// <summary>
    /// Provides network diagnostic capabilities to help troubleshoot connection issues
    /// </summary>
    public class NetworkDiagnostics
    {
        /// <summary>
        /// Represents the result of a diagnostic test
        /// </summary>
        public class DiagnosticResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public string Details { get; set; }
            public TimeSpan? Duration { get; set; }
            
            public DiagnosticResult(bool success, string message, string details = null, TimeSpan? duration = null)
            {
                Success = success;
                Message = message;
                Details = details;
                Duration = duration;
            }
            
            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"[{(Success ? "SUCCESS" : "FAILED")}] {Message}");
                
                if (!string.IsNullOrEmpty(Details))
                {
                    sb.AppendLine(Details);
                }
                
                if (Duration.HasValue)
                {
                    sb.AppendLine($"Duration: {Duration.Value.TotalMilliseconds:F1}ms");
                }
                
                return sb.ToString();
            }
        }
        
        /// <summary>
        /// Comprehensive diagnostics report
        /// </summary>
        public class DiagnosticsReport
        {
            public string Url { get; set; }
            public string HostName { get; set; }
            public string IpAddress { get; set; }
            public DiagnosticResult DnsLookup { get; set; }
            public DiagnosticResult Ping { get; set; }
            public DiagnosticResult TcpConnection { get; set; }
            public DiagnosticResult HttpsConnection { get; set; }
            public DiagnosticResult ProxySettings { get; set; }
            public DiagnosticResult CertificateValidation { get; set; }
            public List<DiagnosticResult> AdditionalTests { get; set; }
            
            public DiagnosticsReport()
            {
                AdditionalTests = new List<DiagnosticResult>();
            }
            
            public string GetFormattedReport()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("===== NETWORK DIAGNOSTICS REPORT =====");
                sb.AppendLine($"URL: {Url}");
                sb.AppendLine($"Host: {HostName}");
                sb.AppendLine($"IP Address: {IpAddress}");
                sb.AppendLine();
                
                sb.AppendLine("--- DNS Lookup ---");
                sb.AppendLine(DnsLookup?.ToString() ?? "Not tested");
                sb.AppendLine();
                
                sb.AppendLine("--- Ping Test ---");
                sb.AppendLine(Ping?.ToString() ?? "Not tested");
                sb.AppendLine();
                
                sb.AppendLine("--- TCP Connection ---");
                sb.AppendLine(TcpConnection?.ToString() ?? "Not tested");
                sb.AppendLine();
                
                sb.AppendLine("--- HTTPS Connection ---");
                sb.AppendLine(HttpsConnection?.ToString() ?? "Not tested");
                sb.AppendLine();
                
                sb.AppendLine("--- Proxy Settings ---");
                sb.AppendLine(ProxySettings?.ToString() ?? "Not tested");
                sb.AppendLine();
                
                sb.AppendLine("--- Certificate Validation ---");
                sb.AppendLine(CertificateValidation?.ToString() ?? "Not tested");
                sb.AppendLine();
                
                if (AdditionalTests.Any())
                {
                    sb.AppendLine("--- Additional Tests ---");
                    foreach (var test in AdditionalTests)
                    {
                        sb.AppendLine(test.ToString());
                        sb.AppendLine();
                    }
                }
                
                sb.AppendLine("===== END OF REPORT =====");
                return sb.ToString();
            }
        }
        
        /// <summary>
        /// Runs a comprehensive network diagnostic test against the specified URL
        /// </summary>
        /// <param name="url">The URL to test, should be a complete URL including protocol</param>
        /// <param name="timeoutMs">Timeout for individual tests in milliseconds</param>
        /// <returns>A comprehensive diagnostics report</returns>
        public static async Task<DiagnosticsReport> RunDiagnosticsAsync(string url, int timeoutMs = 10000)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException("URL cannot be null or empty", nameof(url));
            }
            
            Logger.LogInfo($"Starting network diagnostics for {url}");
            
            var report = new DiagnosticsReport { Url = url };
            Uri uri;
            
            try
            {
                uri = new Uri(url);
                report.HostName = uri.Host;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Invalid URL format: {ex.Message}");
                report.AdditionalTests.Add(new DiagnosticResult(false, "URL Parsing", $"Failed to parse URL: {ex.Message}"));
                return report;
            }
            
            // Test DNS resolution
            try
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var ipAddresses = await Dns.GetHostAddressesAsync(uri.Host);
                watch.Stop();
                
                if (ipAddresses.Length > 0)
                {
                    report.IpAddress = string.Join(", ", ipAddresses.Select(ip => ip.ToString()));
                    report.DnsLookup = new DiagnosticResult(
                        true,
                        $"Successfully resolved {uri.Host} to {report.IpAddress}",
                        $"Found {ipAddresses.Length} IP address(es)",
                        watch.Elapsed
                    );
                }
                else
                {
                    report.DnsLookup = new DiagnosticResult(
                        false,
                        $"DNS lookup succeeded but no IP addresses were returned for {uri.Host}",
                        duration: watch.Elapsed
                    );
                }
            }
            catch (Exception ex)
            {
                report.DnsLookup = new DiagnosticResult(
                    false,
                    "DNS lookup failed",
                    $"Error: {ex.Message}"
                );
            }
            
            // Test ping
            try
            {
                var ping = new Ping();
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var reply = await ping.SendPingAsync(uri.Host, timeoutMs);
                watch.Stop();
                
                if (reply.Status == IPStatus.Success)
                {
                    report.Ping = new DiagnosticResult(
                        true,
                        $"Ping to {uri.Host} succeeded",
                        $"Round-trip time: {reply.RoundtripTime}ms, TTL: {reply.Options?.Ttl ?? 0}",
                        watch.Elapsed
                    );
                }
                else
                {
                    report.Ping = new DiagnosticResult(
                        false,
                        $"Ping to {uri.Host} failed",
                        $"Status: {reply.Status}",
                        watch.Elapsed
                    );
                }
            }
            catch (Exception ex)
            {
                report.Ping = new DiagnosticResult(
                    false,
                    $"Ping to {uri.Host} failed",
                    $"Error: {ex.Message}"
                );
            }
            
            // Test TCP connection
            int port = uri.Port > 0 ? uri.Port : (uri.Scheme.ToLower() == "https" ? 443 : 80);
            
            try
            {
                using (var tcpClient = new TcpClient())
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var connectTask = tcpClient.ConnectAsync(uri.Host, port);
                    
                    // Create a timeout task
                    var timeoutTask = Task.Delay(timeoutMs);
                    
                    // Wait for either connection or timeout
                    if (await Task.WhenAny(connectTask, timeoutTask) == connectTask)
                    {
                        watch.Stop();
                        report.TcpConnection = new DiagnosticResult(
                            true,
                            $"TCP connection to {uri.Host}:{port} succeeded",
                            $"Connected to {tcpClient.Client?.RemoteEndPoint}",
                            watch.Elapsed
                        );
                    }
                    else
                    {
                        report.TcpConnection = new DiagnosticResult(
                            false,
                            $"TCP connection to {uri.Host}:{port} timed out after {timeoutMs}ms",
                            "Connection attempt timed out"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                report.TcpConnection = new DiagnosticResult(
                    false,
                    $"TCP connection to {uri.Host}:{port} failed",
                    $"Error: {ex.Message}"
                );
            }
            
            // Test HTTPS connection
            if (uri.Scheme.ToLower() == "https")
            {
                try
                {
                    var handler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (sender, cert, chain, errors) =>
                        {
                            report.CertificateValidation = new DiagnosticResult(
                                errors == SslPolicyErrors.None,
                                errors == SslPolicyErrors.None 
                                    ? "SSL certificate validation succeeded" 
                                    : "SSL certificate validation failed",
                                errors == SslPolicyErrors.None
                                    ? $"Certificate: {cert.Subject}, Issued by: {cert.Issuer}, Valid from: {cert.NotBefore} to {cert.NotAfter}"
                                    : $"Validation errors: {errors}, Certificate: {cert.Subject}, Issued by: {cert.Issuer}"
                            );
                            
                            // For diagnostic purposes, we'll allow the connection even if there are certificate errors
                            return true;
                        }
                    };
                    
                    using (var httpClient = new HttpClient(handler))
                    {
                        httpClient.Timeout = TimeSpan.FromMilliseconds(timeoutMs);
                        var watch = System.Diagnostics.Stopwatch.StartNew();
                        var response = await httpClient.GetAsync(uri);
                        watch.Stop();
                        
                        report.HttpsConnection = new DiagnosticResult(
                            response.IsSuccessStatusCode,
                            response.IsSuccessStatusCode
                                ? $"HTTPS connection succeeded with status {(int)response.StatusCode} ({response.StatusCode})"
                                : $"HTTPS connection returned error status {(int)response.StatusCode} ({response.StatusCode})",
                            $"Response headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}:[{string.Join(", ", h.Value)}]"))}",
                            watch.Elapsed
                        );
                    }
                }
                catch (Exception ex)
                {
                    report.HttpsConnection = new DiagnosticResult(
                        false,
                        "HTTPS connection failed",
                        $"Error: {ex.Message}"
                    );
                }
            }
            
            // Check proxy settings
            try
            {
                var proxy = WebRequest.GetSystemWebProxy();
                var proxyUrl = proxy?.GetProxy(uri);
                bool usingProxy = proxyUrl != uri;
                
                report.ProxySettings = new DiagnosticResult(
                    true,  // Not a failure condition either way
                    usingProxy 
                        ? $"System is configured to use proxy {proxyUrl} for {uri}"
                        : "No proxy configured for this URL",
                    usingProxy
                        ? $"Proxy credentials type: {(proxy.Credentials == null ? "None" : proxy.Credentials.GetType().Name)}"
                        : null
                );
            }
            catch (Exception ex)
            {
                report.ProxySettings = new DiagnosticResult(
                    false,
                    "Failed to retrieve proxy settings",
                    $"Error: {ex.Message}"
                );
            }
            
            Logger.LogInfo($"Network diagnostics completed for {url}");
            return report;
        }
    }
}
