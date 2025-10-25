using System;

namespace Miller_Craft_Tools.Services
{
    /// <summary>
    /// Exception thrown when a session has expired or is invalid
    /// </summary>
    public class SessionExpiredException : Exception
    {
        public SessionExpiredException(string message) : base(message)
        {
        }

        public SessionExpiredException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when there is a network error during sync
    /// </summary>
    public class SyncNetworkException : Exception
    {
        public SyncNetworkException(string message) : base(message)
        {
        }

        public SyncNetworkException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
    
    /// <summary>
    /// Exception thrown when there is a timeout during sync
    /// </summary>
    public class SyncTimeoutException : Exception
    {
        public SyncTimeoutException(string message) : base(message)
        {
        }

        public SyncTimeoutException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
