using System;

namespace Miller_Craft_Tools.Services.SyncUtilities
{
    /// <summary>
    /// Utility class for reporting progress during sync operations
    /// </summary>
    public class ProgressReporter
    {
        // Progress handler for reporting sync progress
        private readonly IProgress<Tuple<string, int>> _progressHandler;
        
        /// <summary>
        /// Creates a new instance of ProgressReporter
        /// </summary>
        /// <param name="progressHandler">Optional progress handler for reporting progress</param>
        public ProgressReporter(IProgress<Tuple<string, int>> progressHandler = null)
        {
            _progressHandler = progressHandler;
        }
        
        /// <summary>
        /// Reports progress through the progress handler if available
        /// </summary>
        /// <param name="message">Progress message</param>
        /// <param name="progressPercent">Progress percentage (0-100)</param>
        public void ReportProgress(string message, int progressPercent)
        {
            _progressHandler?.Report(new Tuple<string, int>(message, progressPercent));
        }
    }
}
