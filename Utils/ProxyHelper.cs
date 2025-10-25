using System;
using System.Net;
using System.Net.Http;

namespace Miller_Craft_Tools.Utils
{
    /// <summary>
    /// Helper class for proxy configuration in the Miller Craft Tools plugin
    /// </summary>
    public static class ProxyHelper
    {
        /// <summary>
        /// Creates an HttpClientHandler configured with system proxy settings
        /// </summary>
        /// <param name="bypassProxyOnLocal">Whether to bypass proxy for local addresses</param>
        /// <returns>HttpClientHandler configured with proxy settings</returns>
        public static HttpClientHandler CreateProxyEnabledHandler(bool bypassProxyOnLocal = true)
        {
            var handler = new HttpClientHandler();
            
            try
            {
                // Get system proxy
                var proxy = WebRequest.GetSystemWebProxy();
                
                if (proxy != null)
                {
                    // Configure proxy with default credentials (current user)
                    proxy.Credentials = CredentialCache.DefaultCredentials;
                    handler.Proxy = proxy;
                    handler.UseProxy = true;
                    handler.UseDefaultCredentials = true;
                    handler.PreAuthenticate = true;
                    
                    // Bypass proxy for local addresses if specified
                    if (bypassProxyOnLocal)
                    {
                        handler.UseDefaultCredentials = true;
                    }
                    
                    // Log proxy configuration
                    string proxyUri = proxy.GetProxy(new Uri("https://app.millercraftllc.com"))?.ToString() ?? "No proxy";
                    Logger.LogInfo($"System proxy configured: {proxyUri}");
                }
                else
                {
                    Logger.LogInfo("No system proxy detected");
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - continue without proxy if there's an error
                Logger.LogError($"Failed to configure proxy: {ex.Message}");
            }
            
            return handler;
        }
        
        /// <summary>
        /// Gets the system proxy address as a string, for diagnostic purposes
        /// </summary>
        /// <returns>String representation of proxy address or "Direct connection"</returns>
        public static string GetProxyAddressForDiagnostics()
        {
            try
            {
                var proxy = WebRequest.GetSystemWebProxy();
                if (proxy != null)
                {
                    Uri targetUri = new Uri("https://app.millercraftllc.com");
                    Uri proxyUri = proxy.GetProxy(targetUri);
                    
                    // If proxy URI is different from target URI, a proxy is in use
                    if (proxyUri != targetUri)
                    {
                        return proxyUri.ToString();
                    }
                }
                
                return "Direct connection";
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error getting proxy information: {ex.Message}");
                return "Error detecting proxy";
            }
        }
    }
}
