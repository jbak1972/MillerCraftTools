using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using Miller_Craft_Tools.UI.Dialogs;

namespace Miller_Craft_Tools.Command
{
    [Transaction(TransactionMode.Manual)]
    public class ManualApiTestCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Create and show the manual API test dialog
                if (commandData != null && commandData.Application != null && 
                    commandData.Application.MainWindowHandle != System.IntPtr.Zero)
                {
                    // Get owner form from Revit main window handle
                    System.Windows.Forms.Form ownerForm = System.Windows.Forms.Control.FromHandle(commandData.Application.MainWindowHandle) as System.Windows.Forms.Form;

                    // Create dialog and ensure it's fully initialized before showing
                    using (var dialog = new SimpleManualApiTestDialog())
                    {
                        // Make sure the dialog is created on the UI thread
                        System.Windows.Forms.Application.DoEvents();
                        
                        if (ownerForm != null)
                        {
                            // Show as modal to Revit window
                            dialog.ShowDialog(ownerForm);
                        }
                        else
                        {
                            dialog.ShowDialog();
                        }
                    }
                }
                else
                {
                    // Just show dialog without parent
                    using (var dialog = new SimpleManualApiTestDialog())
                    {
                        dialog.ShowDialog();
                    }
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Autodesk.Revit.UI.TaskDialog.Show("Error", $"An error occurred: {ex.Message}");
                return Result.Failed;
            }
        }
    }
}
