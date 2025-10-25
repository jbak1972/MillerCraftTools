using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Miller_Craft_Tools.UI;

namespace Miller_Craft_Tools.Command
{
    /// <summary>
    /// Command to open the consolidated Connection Manager dialog
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class ConnectionManagerCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Show the Connection Manager dialog
                using (var dialog = new ConnectionManagerDialog())
                {
                    dialog.ShowDialog();
                }
                
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // Log the error
                Utils.Logger.LogError($"Error opening Connection Manager: {ex.Message}", Utils.LogSeverity.Error);
                
                // Show error message to user - using fully qualified type reference to avoid CS0104 error
                Autodesk.Revit.UI.TaskDialog.Show(
                    "Connection Manager Error",
                    $"An error occurred while opening the Connection Manager: {ex.Message}");
                
                return Result.Failed;
            }
        }
    }
}
