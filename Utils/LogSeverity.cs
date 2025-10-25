using System;

namespace Miller_Craft_Tools.Utils
{
    /// <summary>
    /// Defines severity levels for log messages
    /// </summary>
    public enum LogSeverity
    {
        /// <summary>
        /// Debug information, useful for development
        /// </summary>
        Debug = 0,
        
        /// <summary>
        /// Informational messages about normal operation
        /// </summary>
        Info = 1,
        
        /// <summary>
        /// Warning conditions that should be addressed
        /// </summary>
        Warning = 2,
        
        /// <summary>
        /// Error conditions that affect functionality but don't stop operation
        /// </summary>
        Error = 3,
        
        /// <summary>
        /// Critical conditions that require immediate attention
        /// </summary>
        Critical = 4
    }
}
