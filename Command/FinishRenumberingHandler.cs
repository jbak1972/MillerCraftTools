using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;

namespace Miller_Craft_Tools.Command
{
    /// <summary>
    /// Command handler for the "Finish" button in the contextual ribbon tab
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class FinishRenumberingHandler : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Signal the command to succeed and finish
                RenumberViewsCommand.CommandController.IsFinished = true;
                RenumberViewsCommand.CommandController.IsActive = false;
                
                // Signal any waiting thread
                RenumberViewsCommand.CommandController.OnFinishCommand(this, EventArgs.Empty);

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
