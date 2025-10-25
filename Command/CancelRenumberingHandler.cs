using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;

namespace Miller_Craft_Tools.Command
{
    /// <summary>
    /// Command handler for the "Cancel" button in the contextual ribbon tab
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class CancelRenumberingHandler : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Signal the command to cancel and finish
                RenumberViewsCommand.CommandController.IsCancelled = true;
                RenumberViewsCommand.CommandController.IsActive = false;
                
                // Signal any waiting thread
                RenumberViewsCommand.CommandController.OnCancelCommand(this, EventArgs.Empty);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
