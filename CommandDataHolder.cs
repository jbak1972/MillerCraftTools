using Autodesk.Revit.UI;
using Miller_Craft_Tools.UI.Controls;

namespace Miller_Craft_Tools
{
    /// <summary>
    /// Static holder class for shared command data and UI references across the application
    /// </summary>
    public static class CommandDataHolder
    {
        /// <summary>
        /// Shared Revit command data
        /// </summary>
        public static ExternalCommandData? CommandData { get; set; } = null;
        
        /// <summary>
        /// Reference to the connection status button in the ribbon
        /// </summary>
        public static PushButton? ConnectionStatusButton { get; set; } = null;
        
        /// <summary>
        /// Reference to the connection status indicator control
        /// </summary>
        public static ConnectionStatusIndicator? ConnectionStatusIndicator { get; set; } = null;
    }
}