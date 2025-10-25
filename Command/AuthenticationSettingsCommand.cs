using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Miller_Craft_Tools.UI;

namespace Miller_Craft_Tools.Command
{
    /// <summary>
    /// Command for managing Miller Craft Assistant authentication settings
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class AuthenticationSettingsCommand : IExternalCommand
    {
        /// <summary>
        /// Main execution point for the Revit command
        /// </summary>
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Create and show the authentication settings dialog
                using (var dialog = new AuthenticationSettingsDialog())
                {
                    dialog.ShowDialog();
                }
                
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // Create error dialog using full qualification to avoid CS0104 error
                Autodesk.Revit.UI.TaskDialog errorDialog = new Autodesk.Revit.UI.TaskDialog("Error")
                {
                    MainInstruction = "Failed to open authentication settings",
                    MainContent = ex.Message,
                    CommonButtons = TaskDialogCommonButtons.Ok
                };
                errorDialog.Show();
                
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
