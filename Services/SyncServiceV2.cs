using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Miller_Craft_Tools.Model;
using Miller_Craft_Tools.Utils;
using Miller_Craft_Tools.Services.SyncUtilities;

namespace Miller_Craft_Tools.Services
{
    /// <summary>
    /// V2 implementation of the SyncService using the new REST API endpoints
    /// for bidirectional synchronization with the Miller Craft Assistant web application
    /// </summary>
    public class SyncServiceV2
    {
        // Default status check interval (5 minutes)
        private const int DefaultStatusCheckIntervalMs = 5 * 60 * 1000;
        
        // Utility classes for improved separation of concerns
        private readonly ApiEndpointManager _endpointManager;
        private readonly ParameterManager _parameterManager;
        private readonly HttpRequestHelper _httpHelper;
        private readonly SyncStatusTracker _statusTracker;
        private readonly ProgressReporter _progressReporter;
        
        // Additional services
        private readonly AuthenticationService _authService;
        private readonly CancellationToken _cancellationToken;
        
        /// <summary>
        /// Creates a new instance of the SyncServiceV2 class
        /// </summary>
        /// <param name="progressHandler">Optional progress handler for reporting progress</param>
        /// <param name="statusCheckIntervalMs">Interval for status checking in milliseconds, defaults to 5 minutes</param>
        /// <param name="cancellationToken">Cancellation token for cancelling operations</param>
        /// <param name="mappingConfig">Optional parameter mapping configuration</param>
        /// <param name="useNewEndpoints">Whether to use new endpoints as primary</param>
        public SyncServiceV2(
            IProgress<Tuple<string, int>> progressHandler = null,
            int statusCheckIntervalMs = DefaultStatusCheckIntervalMs,
            CancellationToken cancellationToken = default,
            ParameterMappingConfiguration mappingConfig = null,
            bool useNewEndpoints = true)
        {
            _cancellationToken = cancellationToken;
            _authService = new AuthenticationService();
            
            // Initialize utility classes
            _progressReporter = new ProgressReporter(progressHandler);
            _endpointManager = new ApiEndpointManager(useNewEndpoints);
            _parameterManager = new ParameterManager(mappingConfig ?? new ParameterMappingConfiguration());
            _httpHelper = new HttpRequestHelper(_authService, cancellationToken);
            _statusTracker = new SyncStatusTracker(
                _httpHelper,
                _endpointManager,
                _progressReporter,
                statusCheckIntervalMs,
                cancellationToken);
        }
        
        // Endpoint methods moved to ApiEndpointManager class
        
        /// <summary>
        /// Initiates a sync operation from Revit to the web application
        /// </summary>
        /// <param name="doc">Revit document to sync</param>
        /// <param name="projectGuid">Unique GUID for the Revit project</param>
        /// <returns>Result of the sync operation including SyncId for status checking</returns>
        public async Task<SyncResult> InitiateSyncAsync(Document doc, string projectGuid)
        {
            try
            {
                // Report progress
                _progressReporter.ReportProgress("Collecting project parameters...", 10);
                
                // Collect parameters from the Revit model using ParameterManager
                SyncRequest request = _parameterManager.CollectParametersForSync(doc, projectGuid);
                
                // Get valid authentication token
                string token = await _httpHelper.GetValidTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    throw new UnauthorizedAccessException("Authentication token is not available. Please log in.");
                }
                
                // Report progress
                _progressReporter.ReportProgress("Sending parameters to Miller Craft...", 30);
                
                // Convert request to JSON
                string requestJson = JsonConvert.SerializeObject(request);
                
                // Log the request for debugging
                Logger.LogInfo($"Sync Request JSON (first 500 chars): {requestJson.Substring(0, Math.Min(500, requestJson.Length))}");
                Logger.LogInfo($"Request RevitProjectGuid: {request.RevitProjectGuid}");
                
                // Send the request to the server - POST /api/revit/sync
                string responseJson = await _httpHelper.SendJsonRequestAsync(
                    _endpointManager.GetSyncEndpoint(),
                    requestJson,
                    token);
                    
                // Parse the response
                var result = JsonConvert.DeserializeObject<SyncResult>(responseJson);
                
                // Check for success
                if (result == null || !result.Success)
                {
                    string errorMessage = result?.Message ?? "Unknown error occurred during sync";
                    string errorDetails = result?.Error ?? "";
                    Logger.LogError($"Sync failed: {errorMessage}. {errorDetails}");
                    throw new InvalidOperationException($"Sync failed: {errorMessage}");
                }
                
                // Report progress
                _progressReporter.ReportProgress("Sync complete!", 100);
                
                // Log the successful sync with new action-based format
                if (result.Action == "queue")
                {
                    Logger.LogJson(new { 
                        Action = "Sync Queued", 
                        QueueId = result.QueueId, 
                        ProjectGuid = projectGuid,
                        AvailableProjectsCount = result.AvailableProjects?.Count ?? 0
                    }, "sync_initiation");
                }
                else if (result.Action == "sync")
                {
                    Logger.LogJson(new { 
                        Action = "Sync Successful", 
                        ProjectId = result.ProjectId,
                        ProjectName = result.ProjectName,
                        ChangesApplied = result.Data?.ChangesApplied ?? 0
                    }, "sync_initiation");
                }
                
                // Use the SyncResponseHandler to log the sync result
                SyncResponseHandler.LogSyncResult(result);
                
                return result;
            }
            catch (HttpRequestException ex)
            {
                Logger.LogError($"Network error during sync: {ex.Message}");
                throw new SyncNetworkException("Unable to connect to Miller Craft Assistant. Please check your network connection.", ex);
            }
            catch (TaskCanceledException ex)
            {
                if (_cancellationToken.IsCancellationRequested)
                {
                    Logger.LogError("Sync operation was canceled by user");
                    throw new OperationCanceledException("Sync was canceled", ex, _cancellationToken);
                }
                
                Logger.LogError("Sync operation timed out");
                throw new TimeoutException("Sync operation timed out. Please try again later.", ex);
            }
            catch (Exception ex) when (!(ex is UnauthorizedAccessException || ex is InvalidOperationException || 
                                           ex is SyncNetworkException || ex is OperationCanceledException))
            {
                Logger.LogError($"Sync operation failed: {ex.Message}");
                throw new InvalidOperationException("Sync failed. Please try again later.", ex);
            }
        }
        
        /// <summary>
        /// Checks the status of a sync operation
        /// </summary>
        /// <param name="syncId">The ID of the sync operation to check</param>
        /// <returns>Current status of the sync operation</returns>
        public async Task<SyncStatus> CheckSyncStatusAsync(string syncId)
        {
            // Delegate to the SyncStatusTracker
            return await _statusTracker.CheckSyncStatusAsync(syncId);
        }
        
        /// <summary>
        /// Applies parameter changes from the web application to the Revit model
        /// </summary>
        /// <param name="doc">Revit document to apply changes to</param>
        /// <param name="changes">List of parameter changes to apply</param>
        /// <returns>List of applied changes with their status</returns>
        public async Task<bool> ApplyParameterChangesAsync(Document doc, SyncStatus status, IProgress<string> progress, CancellationToken cancellationToken)
        {
            if (doc == null || status == null || !status.HasChangesToApply)
            {
                return false;
            }

            // Use the SyncResponseHandler to format the status message
            string statusDetails = SyncResponseHandler.FormatSyncStatus(status);
            progress?.Report($"Processing {status.WebChanges.Count} parameter changes...\n{statusDetails}");

            // Apply the changes to the document
            List<AppliedChange> appliedChanges = new List<AppliedChange>();
            
            try
            {
                // Must use a transaction to modify the Revit document
                using (Transaction tx = new Transaction(doc))
                {
                    tx.Start("Apply Web Parameter Changes");

                    foreach (var change in status.WebChanges)
                    {
                        try
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                tx.RollBack();
                                throw new OperationCanceledException("Parameter application was cancelled.");
                            }

                            var appliedChange = _parameterManager.ApplyParameterChange(doc, change);
                            appliedChanges.Add(appliedChange);
                            
                            progress?.Report($"Applied change to parameter: {change.Name} (Value: {change.Value})");
                            
                            // Log the change
                            Logger.LogJson(
                                new { Action = "Applied Web Change", Parameter = change.Name, Value = change.Value },
                                "parameter_changes");
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"Failed to apply change to {change.Name}: {ex.Message}");
                            appliedChanges.Add(AppliedChange.Create(change, "error"));
                        }
                    }
                    tx.Commit();
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to apply parameter changes: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Applies parameter changes synchronously - compatibility method for legacy code
        /// </summary>
        /// <param name="doc">Revit document to apply changes to</param>
        /// <param name="changes">List of parameter changes to apply</param>
        /// <returns>List of applied changes with their status</returns>
        public List<AppliedChange> ApplyParameterChanges(Document doc, List<WebParameterChange> changes)
        {
            if (doc == null || changes == null || changes.Count == 0)
            {
                return new List<AppliedChange>();
            }

            List<AppliedChange> appliedChanges = new List<AppliedChange>();

            try
            {
                using (Transaction tx = new Transaction(doc))
                {
                    tx.Start("Apply Web Parameter Changes");

                    foreach (var change in changes)
                    {
                        try
                        {
                            var appliedChange = _parameterManager.ApplyParameterChange(doc, change);
                            appliedChanges.Add(appliedChange);
                            
                            Logger.LogJson(
                                new { Action = "Applied Web Change", Parameter = change.Name, Value = change.Value },
                                "parameter_changes");
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"Failed to apply change to {change.Name}: {ex.Message}");
                            appliedChanges.Add(AppliedChange.Create(change, "error", ex.Message));
                        }
                    }

                    tx.Commit();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to apply parameter changes: {ex.Message}");
            }

            return appliedChanges;
        }

        /// <summary>
        /// Acknowledges applied changes back to the server
        /// </summary>
        /// <param name="syncId">The ID of the sync operation</param>
        /// <param name="appliedChanges">List of changes and their application status</param>
        /// <returns>True if acknowledgment was successful</returns>
        public async Task<bool> AcknowledgeChangesAsync(string syncId, List<AppliedChange> appliedChanges)
        {
            try
            {
                // Get valid authentication token using HttpRequestHelper
                string token = await _httpHelper.GetValidTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    throw new UnauthorizedAccessException("Authentication token is not available. Please log in.");
                }
                
                // Create acknowledgment request
                var acknowledgment = new ChangeAcknowledgment
                {
                    SyncId = syncId,
                    AppliedChanges = appliedChanges
                };
                
                // Convert to JSON
                string requestJson = JsonConvert.SerializeObject(acknowledgment);
                
                // Send the request to the server - try primary endpoint first
                string responseJson;
                try
                {
                    responseJson = await _httpHelper.SendJsonRequestAsync(
                        _endpointManager.GetApplyEndpoint(syncId, true), // Try primary endpoint first
                        requestJson,
                        token);
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("404"))
                {
                    // If we got a 404, try the fallback endpoint
                    TelemetryLogger.LogInfo("Primary apply/acknowledge endpoint returned 404, trying fallback endpoint");
                    responseJson = await _httpHelper.SendJsonRequestAsync(
                        _endpointManager.GetApplyEndpoint(syncId, false), // Use fallback endpoint
                        requestJson,
                        token);
                }
                    
                // Parse the response
                var result = JsonConvert.DeserializeObject<AcknowledgmentResponse>(responseJson);
                
                // Check for success
                if (result == null || !result.Success)
                {
                    string errorMessage = result?.Message ?? "Unknown error occurred during acknowledgment";
                    throw new InvalidOperationException($"Acknowledgment failed: {errorMessage}");
                }
                
                // Log the successful acknowledgment
                Logger.LogJson(
                    new { Action = "Changes Acknowledged", SyncId = syncId, AppliedCount = appliedChanges.Count },
                    "sync_acknowledgment");
                
                return true;
            }
            catch (Exception ex) when (!(ex is UnauthorizedAccessException || ex is InvalidOperationException || 
                                           ex is HttpRequestException || ex is OperationCanceledException))
            {
                Logger.LogError($"Acknowledgment failed: {ex.Message}");
                throw new InvalidOperationException("Failed to acknowledge changes. Please try again later.", ex);
            }
        }
        
        /// <summary>
        /// Starts periodic status checking for a sync operation
        /// </summary>
        /// <param name="syncId">The ID of the sync operation to check</param>
        /// <param name="statusCallback">Callback to receive status updates</param>
        public void StartStatusChecking(string syncId, Action<SyncStatus> statusCallback)
        {
            // Delegate to the SyncStatusTracker
            _statusTracker.StartStatusChecking(syncId, statusCallback);
        }
        
        /// <summary>
        /// Stops periodic status checking
        /// </summary>
        public void StopStatusChecking()
        {
            // Delegate to the SyncStatusTracker
            _statusTracker.StopStatusChecking();
        }
        
        // All private helper methods have been moved to their respective utility classes
    }
}