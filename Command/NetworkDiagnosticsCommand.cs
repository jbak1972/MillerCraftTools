using System;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Miller_Craft_Tools.UI.Dialogs;

namespace Miller_Craft_Tools.Command
{
    /// <summary>
    /// Command to open the Network Diagnostics dialog for troubleshooting connection issues
    /// </summary>
    [Transaction(TransactionMode.ReadOnly)]
    public class NetworkDiagnosticsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Show the network diagnostics dialog
                using (var dialog = new NetworkDiagnosticsDialog())
                {
                    dialog.ShowDialog();
                }
                
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Utils.Logger.LogError($"Error executing NetworkDiagnosticsCommand: {ex.Message}");
                
                // Show error to user
                Autodesk.Revit.UI.TaskDialog.Show("Error", $"An error occurred while opening the Network Diagnostics dialog: {ex.Message}");
                
                return Result.Failed;
            }
        }
    }
}
