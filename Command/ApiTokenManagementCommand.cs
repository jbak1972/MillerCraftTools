using System;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Miller_Craft_Tools.UI.Dialogs;

namespace Miller_Craft_Tools.Command
{
    /// <summary>
    /// Command to manage API tokens for Miller Craft API access
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class ApiTokenManagementCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Show the API token management dialog
                using (var dialog = new ApiTokenDialog())
                {
                    dialog.ShowDialog();
                }
                
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // Log the error
                Utils.Logger.LogError($"Error in API token management: {ex.Message}", Utils.LogSeverity.Error);
                
                // Show error message to user
                Autodesk.Revit.UI.TaskDialog.Show(
                    "API Token Management Error",
                    $"An error occurred while managing API tokens: {ex.Message}");
                
                return Result.Failed;
            }
        }
    }
}
