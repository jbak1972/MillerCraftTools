using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Miller_Craft_Tools.UI;

namespace Miller_Craft_Tools.Command
{
    /// <summary>
    /// Command to display the UI showcase dialog for visual consistency review
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class UIShowcaseCommand : IExternalCommand
    {
        /// <summary>
        /// Main execution point for the Revit command
        /// </summary>
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Create and show the UI showcase dialog
                using (var dialog = new UIShowcaseDialog())
                {
                    dialog.ShowDialog();
                }
                
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // Create error dialog with fully qualified type to avoid CS0104 error
                Autodesk.Revit.UI.TaskDialog errorDialog = new Autodesk.Revit.UI.TaskDialog("Error")
                {
                    MainInstruction = "Failed to open UI showcase",
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
