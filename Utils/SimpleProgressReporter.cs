using System;

namespace Miller_Craft_Tools.Utils
{
    /// <summary>
    /// Simple progress reporter for background operations
    /// Implements IProgress to report sync progress
    /// </summary>
    public class SimpleProgressReporter : IProgress<Tuple<string, int>>
    {
        /// <summary>
        /// Reports progress with a message and percentage
        /// </summary>
        /// <param name="value">Tuple containing (message, percentComplete)</param>
        public void Report(Tuple<string, int> value)
        {
            if (value != null)
            {
                // Log progress for debugging
                Logger.LogInfo($"Progress ({value.Item2}%): {value.Item1}");
            }
        }
    }
}
