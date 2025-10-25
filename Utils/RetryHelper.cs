using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Miller_Craft_Tools.Utils
{
    /// <summary>
    /// Helper class for implementing retry logic with exponential backoff
    /// </summary>
    public static class RetryHelper
    {
        /// <summary>
        /// Default transient error detection strategy
        /// </summary>
        /// <param name="exception">The exception to check</param>
        /// <returns>True if the exception is considered transient</returns>
        public static bool DefaultTransientErrorDetectionStrategy(Exception exception)
        {
            // Network-related transient exceptions
            if (exception is TimeoutException || 
                exception is SocketException ||
                exception is TaskCanceledException)
            {
                return true;
            }

            // HTTP-related transient status codes
            if (exception is HttpRequestException httpEx)
            {
                // Check for specific transient HTTP errors
                string message = httpEx.Message.ToLowerInvariant();
                if (message.Contains("503") || // Service Unavailable
                    message.Contains("502") || // Bad Gateway
                    message.Contains("504") || // Gateway Timeout
                    message.Contains("429") || // Too Many Requests
                    message.Contains("408"))   // Request Timeout
                {
                    return true;
                }
            }

            // Consider connection errors transient
            if (exception.Message.Contains("connection") && 
               (exception.Message.Contains("timed out") ||
                exception.Message.Contains("refused") ||
                exception.Message.Contains("reset")))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Executes the specified action with retry logic
        /// </summary>
        /// <typeparam name="T">Return type of the action</typeparam>
        /// <param name="action">The action to execute</param>
        /// <param name="maxRetryCount">Maximum number of retry attempts</param>
        /// <param name="initialDelayMs">Initial delay in milliseconds</param>
        /// <param name="maxDelayMs">Maximum delay in milliseconds</param>
        /// <param name="transientErrorDetectionStrategy">Function to determine if an error is transient</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The result of the action</returns>
        public static async Task<T> ExecuteWithRetryAsync<T>(
            Func<Task<T>> action,
            int maxRetryCount = 3,
            int initialDelayMs = 100,
            int maxDelayMs = 5000,
            Func<Exception, bool> transientErrorDetectionStrategy = null,
            CancellationToken cancellationToken = default)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            transientErrorDetectionStrategy ??= DefaultTransientErrorDetectionStrategy;
            
            int retryCount = 0;
            int delay = initialDelayMs;
            List<Exception> exceptions = new List<Exception>();

            while (true)
            {
                try
                {
                    if (retryCount > 0)
                    {
                        Logger.LogInfo($"Retry attempt {retryCount}/{maxRetryCount} after {delay}ms delay");
                    }
                    
                    return await action();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);

                    if (retryCount >= maxRetryCount || !transientErrorDetectionStrategy(ex))
                    {
                        if (retryCount > 0)
                        {
                            Logger.LogError($"Operation failed after {retryCount} retries with exception: {ex.Message}");
                        }
                        throw;
                    }

                    Logger.LogWarning($"Transient error detected (attempt {retryCount + 1}/{maxRetryCount + 1}): {ex.Message}. Retrying in {delay}ms...");
                    
                    try
                    {
                        await Task.Delay(delay, cancellationToken);
                    }
                    catch (TaskCanceledException)
                    {
                        // If cancellation was requested during delay, rethrow the original exception
                        throw exceptions[0];
                    }
                    
                    // Exponential backoff with jitter
                    delay = Math.Min(delay * 2 + new Random().Next(50), maxDelayMs);
                    retryCount++;
                }
            }
        }

        /// <summary>
        /// Executes the specified action with retry logic (non-async version)
        /// </summary>
        /// <typeparam name="T">Return type of the action</typeparam>
        /// <param name="action">The action to execute</param>
        /// <param name="maxRetryCount">Maximum number of retry attempts</param>
        /// <param name="initialDelayMs">Initial delay in milliseconds</param>
        /// <param name="maxDelayMs">Maximum delay in milliseconds</param>
        /// <param name="transientErrorDetectionStrategy">Function to determine if an error is transient</param>
        /// <returns>The result of the action</returns>
        public static T ExecuteWithRetry<T>(
            Func<T> action,
            int maxRetryCount = 3,
            int initialDelayMs = 100,
            int maxDelayMs = 5000,
            Func<Exception, bool> transientErrorDetectionStrategy = null)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            transientErrorDetectionStrategy ??= DefaultTransientErrorDetectionStrategy;
            
            int retryCount = 0;
            int delay = initialDelayMs;
            List<Exception> exceptions = new List<Exception>();

            while (true)
            {
                try
                {
                    if (retryCount > 0)
                    {
                        Logger.LogInfo($"Retry attempt {retryCount}/{maxRetryCount} after {delay}ms delay");
                    }
                    
                    return action();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);

                    if (retryCount >= maxRetryCount || !transientErrorDetectionStrategy(ex))
                    {
                        if (retryCount > 0)
                        {
                            Logger.LogError($"Operation failed after {retryCount} retries with exception: {ex.Message}");
                        }
                        throw;
                    }

                    Logger.LogWarning($"Transient error detected (attempt {retryCount + 1}/{maxRetryCount + 1}): {ex.Message}. Retrying in {delay}ms...");
                    
                    Thread.Sleep(delay);
                    
                    // Exponential backoff with jitter
                    delay = Math.Min(delay * 2 + new Random().Next(50), maxDelayMs);
                    retryCount++;
                }
            }
        }
    }
}
