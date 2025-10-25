using System;

namespace Miller_Craft_Tools.Utils
{
    /// <summary>
    /// Represents the result of an API connectivity test
    /// </summary>
    public class ApiTestingResult
    {
        /// <summary>
        /// The name or description of the test
        /// </summary>
        public string TestName { get; set; }
        
        /// <summary>
        /// Whether the test was successful
        /// </summary>
        public bool IsSuccess { get; set; }
        
        /// <summary>
        /// Additional details or error message
        /// </summary>
        public string Message { get; set; }
    }
}
