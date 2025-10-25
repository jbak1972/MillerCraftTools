using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Miller_Craft_Tools.Model
{
    #region Initial Sync Request/Response Models
    
    /// <summary>
    /// Request model for initial sync (Revit to Web)
    /// Updated to match web app spec from REVIT_PLUGIN_INTEGRATION_PROMPT.md
    /// </summary>
    public class SyncRequest
    {
        [JsonProperty("revitProjectGuid")]
        public string RevitProjectGuid { get; set; }
        
        [JsonProperty("revitFileName")]
        public string RevitFileName { get; set; }
        
        [JsonProperty("parameters")]
        public List<ParameterData> Parameters { get; set; } = new List<ParameterData>();
        
        [JsonProperty("version")]
        public string Version { get; set; } = "1.0";
        
        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }
        
        [JsonProperty("command")]
        public string Command { get; set; }
    }
    
    /// <summary>
    /// Single parameter data for syncing
    /// Updated to match web app spec - value is now object type to support proper data types
    /// </summary>
    public class ParameterData
    {
        [JsonProperty("guid")]
        public string Guid { get; set; }
        
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("value")]
        public object Value { get; set; }
        
        [JsonProperty("group")]
        public string Group { get; set; }
        
        [JsonProperty("dataType")]
        public string DataType { get; set; }
        
        // Optional fields
        [JsonProperty("unit", NullValueHandling = NullValueHandling.Ignore)]
        public string Unit { get; set; }
        
        [JsonProperty("elementId", NullValueHandling = NullValueHandling.Ignore)]
        public string ElementId { get; set; }
    }
    
    /// <summary>
    /// Response from initial sync request
    /// Updated to match web app spec with action-based responses
    /// </summary>
    public class SyncResult
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
        
        [JsonProperty("action")]
        public string Action { get; set; }
        
        [JsonProperty("message")]
        public string Message { get; set; }
        
        // For action: "sync" (existing project)
        [JsonProperty("projectId")]
        public string ProjectId { get; set; }
        
        [JsonProperty("projectName")]
        public string ProjectName { get; set; }
        
        [JsonProperty("data")]
        public SyncData Data { get; set; }
        
        // For action: "queue" (new project)
        [JsonProperty("queueId")]
        public string QueueId { get; set; }
        
        [JsonProperty("queuePosition")]
        public int QueuePosition { get; set; }
        
        [JsonProperty("availableProjects")]
        public List<AvailableProject> AvailableProjects { get; set; }
        
        // For action: "error"
        [JsonProperty("error")]
        public string Error { get; set; }
        
        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }
        
        // Legacy fields - kept for backward compatibility during migration
        [JsonProperty("syncId")]
        public string SyncId { get; set; }
        
        [JsonProperty("status")]
        public string Status { get; set; }
        
        /// <summary>
        /// Formatted details about the sync result for UI display
        /// </summary>
        [JsonIgnore]
        public string FormattedDetails { get; set; }
        
        /// <summary>
        /// User-friendly explanation of the status for UI display
        /// </summary>
        [JsonIgnore]
        public string StatusExplanation { get; set; }
    }
    
    #endregion
    
    #region Sync Status Models
    
    /// <summary>
    /// Response from status check endpoint
    /// </summary>
    public class SyncStatus
    {
        [JsonProperty("syncId")]
        public string SyncId { get; set; }
        
        [JsonProperty("status")]
        public string Status { get; set; }
        
        [JsonProperty("message")]
        public string Message { get; set; }
        
        [JsonProperty("associatedProject")]
        public AssociatedProject AssociatedProject { get; set; }
        
        [JsonProperty("webChanges")]
        public List<WebParameterChange> WebChanges { get; set; }
        
        /// <summary>
        /// Human-readable explanation of the current sync status
        /// </summary>
        [JsonIgnore]
        public string StatusExplanation { get; set; }
        
        /// <summary>
        /// Formatted details about the sync status for UI display
        /// </summary>
        [JsonIgnore]
        public string FormattedDetails { get; set; }
        
        /// <summary>
        /// Helper method to check if status is in a terminal state
        /// </summary>
        public bool IsProcessingComplete
        {
            get
            {
                return Status == "processed" || Status == "error";
            }
        }
        
        /// <summary>
        /// Helper method to check if there are changes to apply
        /// </summary>
        public bool HasChangesToApply
        {
            get
            {
                return Status == "processed" && WebChanges != null && WebChanges.Count > 0;
            }
        }
    }
    
    /// <summary>
    /// Available project information for association
    /// </summary>
    public class AvailableProject
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("description")]
        public string Description { get; set; }
    }
    
    /// <summary>
    /// Associated project information (legacy - for SyncStatus)
    /// </summary>
    public class AssociatedProject
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("name")]
        public string Name { get; set; }
    }
    
    /// <summary>
    /// Sync data containing change information
    /// </summary>
    public class SyncData
    {
        [JsonProperty("changesApplied")]
        public int ChangesApplied { get; set; }
    }
    
    /// <summary>
    /// Parameter change data from web application
    /// </summary>
    public class WebParameterChange
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("category")]
        public string Category { get; set; }
        
        [JsonProperty("value")]
        public string Value { get; set; }
        
        [JsonProperty("dataType")]
        public string DataType { get; set; }
        
        [JsonProperty("modifiedBy")]
        public string ModifiedBy { get; set; }
        
        [JsonProperty("modifiedAt")]
        public string ModifiedAt { get; set; }
        
        [JsonProperty("elementId", NullValueHandling = NullValueHandling.Ignore)]
        public int ElementId { get; set; }
        
        [JsonProperty("elementUniqueId", NullValueHandling = NullValueHandling.Ignore)]
        public string ElementUniqueId { get; set; }
        
        // Additional properties for UI display
        [JsonIgnore]
        public bool IsSelected { get; set; } = true;
        
        [JsonIgnore]
        public string CurrentValue { get; set; }
        
        [JsonIgnore]
        public string FormattedModifiedAt
        {
            get
            {
                if (DateTime.TryParse(ModifiedAt, out DateTime date))
                {
                    return date.ToLocalTime().ToString("g");
                }
                return ModifiedAt;
            }
        }
    }
    
    #endregion
    
    #region Change Acknowledgment Models
    
    /// <summary>
    /// Request model for acknowledging applied changes
    /// </summary>
    public class ChangeAcknowledgment
    {
        [JsonProperty("syncId")]
        public string SyncId { get; set; }
        
        [JsonProperty("appliedChanges")]
        public List<AppliedChange> AppliedChanges { get; set; } = new List<AppliedChange>();
    }
    
    /// <summary>
    /// Information about an applied parameter change
    /// </summary>
    public class AppliedChange
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("category")]
        public string Category { get; set; }
        
        [JsonProperty("status")]
        public string Status { get; set; }
        
        /// <summary>
        /// Error message if application failed
        /// </summary>
        [JsonProperty("error")]
        public string Error { get; set; }

        /// <summary>
        /// Creates a new applied change with the specified status
        /// </summary>
        public static AppliedChange Create(WebParameterChange change, string status)
        {
            return new AppliedChange
            {
                Name = change.Name,
                Category = change.Category,
                Status = status
            };
        }

        /// <summary>
        /// Creates a new applied change with the specified status and error message
        /// </summary>
        public static AppliedChange Create(WebParameterChange change, string status, string errorMessage)
        {
            return new AppliedChange
            {
                Name = change.Name,
                Category = change.Category,
                Status = status,
                Error = errorMessage
            };
        }
    }
    
    /// <summary>
    /// Response from acknowledgment endpoint
    /// </summary>
    public class AcknowledgmentResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
        
        [JsonProperty("message")]
        public string Message { get; set; }
        
        /// <summary>
        /// Formatted details about the acknowledgment for UI display
        /// </summary>
        [JsonIgnore]
        public string FormattedDetails { get; set; }
    }
    
    #endregion
}