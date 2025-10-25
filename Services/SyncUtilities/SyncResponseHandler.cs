using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Miller_Craft_Tools.Model;
using Miller_Craft_Tools.Utils;

namespace Miller_Craft_Tools.Services.SyncUtilities
{
    /// <summary>
    /// Handles processing, formatting, and interpreting responses from the sync web application
    /// to provide clear feedback to users about synchronization status and results.
    /// </summary>
    public class SyncResponseHandler
    {
        /// <summary>
        /// Processes a sync result and returns a formatted user-friendly message
        /// </summary>
        /// <param name="result">The sync result to process</param>
        /// <returns>A formatted message with detailed information about the sync response</returns>
        public static string FormatSyncInitiationResult(SyncResult result)
        {
            if (result == null)
            {
                return "Error: No response received from server.";
            }

            StringBuilder sb = new StringBuilder();

            // Add basic status info
            sb.AppendLine($"Sync ID: {result.SyncId}");
            sb.AppendLine($"Status: {result.Status}");

            if (!string.IsNullOrEmpty(result.Message))
            {
                sb.AppendLine($"Message: {result.Message}");
            }

            // Add project information if available
            if (!string.IsNullOrEmpty(result.ProjectId))
            {
                sb.AppendLine($"Project ID: {result.ProjectId}");
            }

            // Add queue information if applicable
            if (result.QueuePosition > 0)
            {
                sb.AppendLine($"Queue Position: {result.QueuePosition}");
            }

            // Add available projects if any
            if (result.AvailableProjects != null && result.AvailableProjects.Count > 0)
            {
                sb.AppendLine("\nAvailable Projects:");
                foreach (var project in result.AvailableProjects)
                {
                    sb.AppendLine($"- {project.Name} (ID: {project.Id})");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Processes a sync status and returns a formatted user-friendly message
        /// </summary>
        /// <param name="status">The sync status to process</param>
        /// <returns>A formatted message with detailed information about the sync status</returns>
        public static string FormatSyncStatus(SyncStatus status)
        {
            if (status == null)
            {
                return "Error: No status information available.";
            }

            StringBuilder sb = new StringBuilder();

            // Add basic status info
            sb.AppendLine($"Sync ID: {status.SyncId}");
            sb.AppendLine($"Status: {status.Status}");

            if (!string.IsNullOrEmpty(status.Message))
            {
                sb.AppendLine($"Message: {status.Message}");
            }

            // Add associated project info if available
            if (status.AssociatedProject != null)
            {
                sb.AppendLine($"Associated Project: {status.AssociatedProject.Name} (ID: {status.AssociatedProject.Id})");
            }

            // Add info about parameter changes if available
            if (status.HasChangesToApply)
            {
                sb.AppendLine($"\nParameter Changes Available: {status.WebChanges.Count} change(s)");
                
                // List the first few changes as a preview
                int previewCount = Math.Min(status.WebChanges.Count, 3);
                if (previewCount > 0)
                {
                    sb.AppendLine("\nPreview of Changes:");
                    for (int i = 0; i < previewCount; i++)
                    {
                        var change = status.WebChanges[i];
                        sb.AppendLine($"- {change.Name}: {change.Value} (modified by {change.ModifiedBy})");
                    }
                    
                    if (status.WebChanges.Count > previewCount)
                    {
                        sb.AppendLine($"... and {status.WebChanges.Count - previewCount} more change(s).");
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Processes an acknowledgment response and returns a formatted user-friendly message
        /// </summary>
        /// <param name="response">The acknowledgment response to process</param>
        /// <param name="changes">The list of changes that were acknowledged</param>
        /// <returns>A formatted message with detailed information about the acknowledgment</returns>
        public static string FormatAcknowledgmentResponse(AcknowledgmentResponse response, List<AppliedChange> changes)
        {
            if (response == null)
            {
                return "Error: No acknowledgment response received from server.";
            }

            StringBuilder sb = new StringBuilder();

            // Add basic response info
            sb.AppendLine($"Success: {response.Success}");
            
            if (!string.IsNullOrEmpty(response.Message))
            {
                sb.AppendLine($"Message: {response.Message}");
            }

            // Add information about the changes
            if (changes != null && changes.Count > 0)
            {
                // Count changes by status
                var appliedCount = changes.Count(c => c.Status == "applied");
                var rejectedCount = changes.Count(c => c.Status == "rejected");
                var errorCount = changes.Count(c => c.Status == "error");

                sb.AppendLine($"\nChanges Summary:");
                sb.AppendLine($"- Applied: {appliedCount}");
                sb.AppendLine($"- Rejected: {rejectedCount}");
                sb.AppendLine($"- Errors: {errorCount}");
                
                // If there were errors, provide more detail
                if (errorCount > 0)
                {
                    sb.AppendLine("\nParameters with errors:");
                    foreach (var change in changes.Where(c => c.Status == "error"))
                    {
                        sb.AppendLine($"- {change.Name} (Category: {change.Category})");
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets a human-readable interpretation of a sync status code
        /// </summary>
        /// <param name="statusCode">The status code from the sync operation</param>
        /// <returns>A user-friendly explanation of the status</returns>
        public static string GetStatusExplanation(string statusCode)
        {
            if (string.IsNullOrEmpty(statusCode))
            {
                return "Unknown status";
            }

            switch (statusCode.ToLower())
            {
                case "initiated":
                    return "Sync has been successfully initiated and is awaiting processing.";
                case "queued":
                    return "Sync request is in the processing queue. It will be processed as soon as possible.";
                case "processing":
                    return "Server is actively processing your sync request.";
                case "processed":
                    return "Sync has been successfully processed. Changes may be available to apply.";
                case "acknowledged":
                    return "Changes have been acknowledged and the sync process is complete.";
                case "partial":
                    return "Sync was partially successful. Some parameters may not have been processed correctly.";
                case "error":
                    return "An error occurred during the sync process. Please check the error message for details.";
                case "rejected":
                    return "Sync request was rejected by the server. Please check your parameters and try again.";
                case "expired":
                    return "Sync request has expired. Please initiate a new sync.";
                default:
                    return $"Status: {statusCode}";
            }
        }

        /// <summary>
        /// Determines if the sync was successful based on the status
        /// </summary>
        /// <param name="status">The status string returned from the sync operation</param>
        /// <returns>True if the sync was successful, false otherwise</returns>
        public static bool IsSuccessfulStatus(string status)
        {
            if (string.IsNullOrEmpty(status))
            {
                return false;
            }

            // Consider these statuses as successful
            string[] successStatuses = new[] { 
                "initiated", "queued", "processing", "processed", "acknowledged"
            };

            return successStatuses.Contains(status.ToLower());
        }

        /// <summary>
        /// Logs detailed information about a sync response for diagnostics
        /// </summary>
        /// <param name="result">The sync result to log</param>
        public static void LogSyncResult(SyncResult result)
        {
            if (result == null)
            {
                Logger.LogError("Null SyncResult received");
                return;
            }

            Logger.LogJson(new { 
                Action = "Sync Result Received",
                Success = result.Success,
                SyncId = result.SyncId,
                Status = result.Status,
                Message = result.Message,
                ProjectId = result.ProjectId,
                QueuePosition = result.QueuePosition
            }, "sync_response");
        }

        /// <summary>
        /// Logs detailed information about a sync status for diagnostics
        /// </summary>
        /// <param name="status">The sync status to log</param>
        public static void LogSyncStatus(SyncStatus status)
        {
            if (status == null)
            {
                Logger.LogError("Null SyncStatus received");
                return;
            }

            Logger.LogJson(new {
                Action = "Sync Status Check",
                SyncId = status.SyncId,
                Status = status.Status,
                Message = status.Message,
                HasChanges = status.HasChangesToApply,
                ChangeCount = status.WebChanges?.Count ?? 0
            }, "sync_status");
        }
    }
}
