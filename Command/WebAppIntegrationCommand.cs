using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Miller_Craft_Tools.UI;

namespace Miller_Craft_Tools.Command
{
    /// <summary>
    /// Command to open the unified Web App Integration dialog
    /// Consolidates connection management, sync operations, and diagnostics
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class WebAppIntegrationCommand : IExternalCommand
    {
        /// <summary>
        /// Starting tab index (0=Connection, 1=Sync, 2=Diagnostics)
        /// </summary>
        private int _startingTab = 0;
        
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uidoc = uiApp.ActiveUIDocument;
                Document doc = uidoc?.Document;
                
                if (doc == null)
                {
                    Autodesk.Revit.UI.TaskDialog.Show("Error", "No active document found.");
                    return Result.Failed;
                }
                
                // Create and show the dialog
                using (var dialog = new WebAppIntegrationDialog(doc))
                {
                    dialog.SwitchToTab(_startingTab);
                    dialog.ShowDialog();
                }
                
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = $"Error opening Web App Integration: {ex.Message}";
                Autodesk.Revit.UI.TaskDialog.Show("Error", message);
                return Result.Failed;
            }
        }
        
        /// <summary>
        /// Sets the starting tab for the dialog
        /// </summary>
        /// <param name="tabIndex">0=Connection, 1=Sync, 2=Diagnostics</param>
        public void SetStartingTab(int tabIndex)
        {
            _startingTab = tabIndex;
        }
    }
    
    /// <summary>
    /// Opens the Web App Integration dialog to the Sync tab
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class WebAppSyncCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uidoc = uiApp.ActiveUIDocument;
                Document doc = uidoc?.Document;
                
                if (doc == null)
                {
                    Autodesk.Revit.UI.TaskDialog.Show("Error", "No active document found.");
                    return Result.Failed;
                }
                
                // Create and show the dialog starting at Sync tab
                using (var dialog = new WebAppIntegrationDialog(doc))
                {
                    dialog.SwitchToTab(1); // Sync tab
                    dialog.ShowDialog();
                }
                
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = $"Error opening Web App Sync: {ex.Message}";
                Autodesk.Revit.UI.TaskDialog.Show("Error", message);
                return Result.Failed;
            }
        }
    }
}
