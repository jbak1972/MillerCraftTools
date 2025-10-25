using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Miller_Craft_Tools.Model;
using Miller_Craft_Tools.Utils;

namespace Miller_Craft_Tools.Services.SyncUtilities
{
    /// <summary>
    /// Handles tracking and checking the status of sync operations
    /// </summary>
    public class SyncStatusTracker
    {
        // Default status check interval (5 minutes)
        private const int DefaultStatusCheckIntervalMs = 5 * 60 * 1000;
        
        private readonly int _statusCheckIntervalMs;
        private readonly CancellationToken _cancellationToken;
        private readonly HttpRequestHelper _httpHelper;
        private readonly ApiEndpointManager _endpointManager;
        private readonly ProgressReporter _progressReporter;
        
        // Active timer for status checking - using fully qualified name to avoid ambiguity
        private System.Threading.Timer _statusCheckTimer;
        
        /// <summary>
        /// Creates a new instance of SyncStatusTracker
        /// </summary>
        /// <param name="httpHelper">Helper for HTTP requests</param>
        /// <param name="endpointManager">Manager for API endpoints</param>
        /// <param name="progressReporter">Reporter for progress updates</param>
        /// <param name="statusCheckIntervalMs">Interval for status checking in milliseconds, defaults to 5 minutes</param>
        /// <param name="cancellationToken">Cancellation token for cancelling operations</param>
        public SyncStatusTracker(
            HttpRequestHelper httpHelper,
            ApiEndpointManager endpointManager,
            ProgressReporter progressReporter,
            int statusCheckIntervalMs = DefaultStatusCheckIntervalMs,
            CancellationToken cancellationToken = default)
        {
            _httpHelper = httpHelper;
            _endpointManager = endpointManager;
            _progressReporter = progressReporter;
            _statusCheckIntervalMs = statusCheckIntervalMs;
            _cancellationToken = cancellationToken;
        }
        
        /// <summary>
        /// Checks the status of a sync operation
        /// </summary>
        /// <param name="syncId">The ID of the sync operation to check</param>
        /// <returns>Current status of the sync operation</returns>
        public async Task<SyncStatus> CheckSyncStatusAsync(string syncId)
        {
            try
            {
                // Get valid authentication token
                string token = await _httpHelper.GetValidTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    throw new UnauthorizedAccessException("Authentication token is not available. Please log in.");
                }
                
                // Report progress
                _progressReporter.ReportProgress("Checking sync status...", 50);
                
                // Try primary status endpoint first
                string statusUrl = _endpointManager.GetStatusEndpoint(syncId, true);
                
                // Send the request to the server
                using (var httpClient = AuthenticationService.CreateAuthenticatedHttpClient(token))
                {
                    // Set a reasonable timeout to prevent hanging
                    httpClient.Timeout = TimeSpan.FromMinutes(1); // 1 minute is enough for status checks
                    
                    // Add logging to help diagnose issues
                    Logger.LogJson(new { Action = "Checking Status", SyncId = syncId, Endpoint = statusUrl }, "status_check");
                    
                    HttpResponseMessage response;
                    try
                    {
                        response = await httpClient.GetAsync(statusUrl, _cancellationToken);
                    }
                    catch (HttpRequestException ex) when (ex.Message.Contains("404"))
                    {
                        // If we got a 404, try the fallback endpoint
                        TelemetryLogger.LogInfo("Primary status endpoint returned 404, trying fallback endpoint");
                        statusUrl = _endpointManager.GetStatusEndpoint(syncId, false);
                        Logger.LogJson(new { Action = "Fallback Status Check", SyncId = syncId, Endpoint = statusUrl }, "status_check");
                        response = await httpClient.GetAsync(statusUrl, _cancellationToken);
                    }
                    
                    if (response.IsSuccessStatusCode)
                    {
                        string responseJson = await response.Content.ReadAsStringAsync();
                        var status = JsonConvert.DeserializeObject<SyncStatus>(responseJson);
                        
                        // Log the status check
                        Logger.LogJson(new { Action = "Status Check", SyncId = syncId, Status = status.Status }, "sync_status");
                        
                        // Report progress based on status
                        string progressMessage = $"Sync status: {status.Status}";
                        if (status.AssociatedProject != null)
                        {
                            progressMessage += $" - Project: {status.AssociatedProject.Name}";
                        }
                        _progressReporter.ReportProgress(progressMessage, status.IsProcessingComplete ? 100 : 75);
                        
                        return status;
                    }
                    
                    // Handle error responses
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        throw new InvalidOperationException($"Sync ID {syncId} not found");
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        throw new UnauthorizedAccessException("Authentication failed. Please log in again.");
                    }
                    
                    throw new HttpRequestException($"Status check failed with status code: {response.StatusCode}");
                }
            }
            catch (Exception ex) when (!(ex is UnauthorizedAccessException || ex is InvalidOperationException || 
                                         ex is HttpRequestException || ex is OperationCanceledException))
            {
                Logger.LogError($"Status check failed: {ex.Message}");
                throw new InvalidOperationException("Failed to check sync status. Please try again later.", ex);
            }
        }
        
        /// <summary>
        /// Starts periodic status checking for a sync operation
        /// </summary>
        /// <param name="syncId">The ID of the sync operation to check</param>
        /// <param name="statusCallback">Callback to receive status updates</param>
        public void StartStatusChecking(string syncId, Action<SyncStatus> statusCallback)
        {
            // Stop any existing timer
            StopStatusChecking();
            
            // Create a new timer for status checking - using fully qualified name to avoid ambiguity
            _statusCheckTimer = new System.Threading.Timer(
                async state => await CheckStatusAndNotify(syncId, statusCallback),
                null,
                0, // Start immediately
                _statusCheckIntervalMs);
        }
        
        /// <summary>
        /// Stops periodic status checking
        /// </summary>
        public void StopStatusChecking()
        {
            if (_statusCheckTimer != null)
            {
                _statusCheckTimer.Dispose();
                _statusCheckTimer = null;
            }
        }
        
        /// <summary>
        /// Checks sync status and notifies callback
        /// </summary>
        private async Task CheckStatusAndNotify(string syncId, Action<SyncStatus> statusCallback)
        {
            try
            {
                var status = await CheckSyncStatusAsync(syncId);
                
                // Invoke the callback with the status
                statusCallback?.Invoke(status);
                
                // If processing is complete, stop the timer
                if (status.IsProcessingComplete)
                {
                    StopStatusChecking();
                }
            }
            catch (Exception ex)
            {
                // Log the error but continue checking
                Logger.LogError($"Error checking sync status: {ex.Message}");
            }
        }
    }
}
