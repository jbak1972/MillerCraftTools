using System;

namespace Miller_Craft_Tools.Services.SyncUtilities
{
    /// <summary>
    /// Manages API endpoints for the Miller Craft Tools sync service
    /// Updated to match REVIT_PLUGIN_INTEGRATION_PROMPT.md specification
    /// </summary>
    public class ApiEndpointManager
    {
        // Base API URL
        private const string BaseApiUrl = "https://app.millercraftllc.com";
        
        // API endpoints per web app spec
        private const string TestEndpoint = BaseApiUrl + "/api/revit/test";
        private const string SyncEndpoint = BaseApiUrl + "/api/revit/sync";
        
        // Legacy endpoint for backward compatibility (deprecated)
        private const string LegacyUploadEndpoint = BaseApiUrl + "/api/revit-sync/upload";
        
        // Flag to control endpoint preference
        private readonly bool _useNewEndpoints;
        
        /// <summary>
        /// Creates a new instance of ApiEndpointManager
        /// </summary>
        public ApiEndpointManager()
        {
            _useNewEndpoints = true;
        }
        
        /// <summary>
        /// Creates a new instance of ApiEndpointManager with endpoint preference
        /// </summary>
        /// <param name="useNewEndpoints">Whether to prefer new endpoints over legacy ones</param>
        public ApiEndpointManager(bool useNewEndpoints)
        {
            _useNewEndpoints = useNewEndpoints;
        }
        
        /// <summary>
        /// Gets the test endpoint for connectivity testing
        /// GET /api/revit/test - works with or without authentication
        /// </summary>
        /// <returns>The test endpoint URL</returns>
        public string GetTestEndpoint()
        {
            return TestEndpoint;
        }
        
        /// <summary>
        /// Gets the sync endpoint
        /// POST /api/revit/sync - primary endpoint for synchronization
        /// </summary>
        /// <returns>The sync endpoint URL</returns>
        public string GetSyncEndpoint()
        {
            return SyncEndpoint;
        }
        
        /// <summary>
        /// Gets the legacy upload endpoint (deprecated)
        /// POST /api/revit-sync/upload - redirects to /api/revit/sync
        /// </summary>
        /// <returns>The legacy upload endpoint URL</returns>
        [Obsolete("This endpoint is deprecated. Use GetSyncEndpoint() instead.")]
        public string GetLegacyUploadEndpoint()
        {
            return LegacyUploadEndpoint;
        }
        
        /// <summary>
        /// Gets the base API URL for constructing custom endpoints
        /// </summary>
        /// <returns>The base API URL</returns>
        public string GetBaseApiUrl()
        {
            return BaseApiUrl;
        }
        
        /// <summary>
        /// Gets the status check endpoint for a specific sync operation
        /// </summary>
        /// <param name="syncId">The sync ID to check status for</param>
        /// <param name="usePrimary">Whether to use primary endpoint (default true)</param>
        /// <returns>The status endpoint URL</returns>
        public string GetStatusEndpoint(string syncId, bool usePrimary = true)
        {
            if (usePrimary)
            {
                return $"{BaseApiUrl}/api/revit/sync/{syncId}/status";
            }
            else
            {
                // Legacy fallback endpoint if needed
                return $"{BaseApiUrl}/api/revit-sync/{syncId}/status";
            }
        }
        
        /// <summary>
        /// Gets the apply/acknowledge endpoint for applying changes from web to Revit
        /// </summary>
        /// <param name="syncId">The sync ID to apply changes for</param>
        /// <param name="usePrimary">Whether to use primary endpoint (default true)</param>
        /// <returns>The apply endpoint URL</returns>
        public string GetApplyEndpoint(string syncId, bool usePrimary = true)
        {
            if (usePrimary)
            {
                return $"{BaseApiUrl}/api/revit/sync/{syncId}/apply";
            }
            else
            {
                // Legacy fallback endpoint if needed
                return $"{BaseApiUrl}/api/revit-sync/{syncId}/apply";
            }
        }
    }
}
