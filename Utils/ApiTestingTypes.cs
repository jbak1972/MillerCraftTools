using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Miller_Craft_Tools.Utils
{
    /// <summary>
    /// Shared types for API connectivity testing
    /// </summary>
    public class ApiTestingTypes
    {
        // Empty container class
    }
    
    /// <summary>
    /// Represents the overall result of an API connectivity test
    /// </summary>
    public class ApiTestResult
    {
        public string BaseUrl { get; set; }
        public string TestEndpoint { get; set; }
        public bool HasToken { get; set; }
        public DateTime Timestamp { get; set; }
        public EndpointTestResult UnauthenticatedTestResult { get; set; }
        public EndpointTestResult AuthenticatedTestResult { get; set; }
    }

    /// <summary>
    /// Represents the result of testing a specific endpoint
    /// </summary>
    public class EndpointTestResult
    {
        /// <summary>
        /// The URL that was tested
        /// </summary>
        public string Url { get; set; }
        
        /// <summary>
        /// Whether the request was successful
        /// </summary>
        public bool IsSuccessful { get; set; }
        
        /// <summary>
        /// Whether this request used authentication
        /// </summary>
        public bool IsAuthenticated { get; set; }
        
        /// <summary>
        /// HTTP status code if available
        /// </summary>
        public System.Net.HttpStatusCode? StatusCode { get; set; }
        
        /// <summary>
        /// Response time in milliseconds
        /// </summary>
        public long ResponseTimeMs { get; set; }
        
        /// <summary>
        /// Error message if any
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// Exception object if an error occurred
        /// </summary>
        public Exception Exception { get; set; }
        
        /// <summary>
        /// Response content as string
        /// </summary>
        public string ResponseContent { get; set; }
        
        /// <summary>
        /// Response headers
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// Token used for authentication (if any)
        /// </summary>
        public string TokenUsed { get; set; }
        
        /// <summary>
        /// Diagnostic information from the API response
        /// </summary>
        public ApiDiagnosticInfo DiagnosticInfo { get; set; }
    }
    
    /// <summary>
    /// Represents diagnostic information returned by the API test endpoint
    /// </summary>
    public class ApiDiagnosticInfo
    {
        public string ServerInfo { get; set; }
        public string ApiVersion { get; set; }
        public string AuthenticationStatus { get; set; }
        public string ClientIpAddress { get; set; }
        public Dictionary<string, string> RequestHeaders { get; set; }
        public Dictionary<string, string> ServerVariables { get; set; }
    }
}
