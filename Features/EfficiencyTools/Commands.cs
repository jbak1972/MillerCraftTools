using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Miller_Craft_Tools.Core.Application;
using Miller_Craft_Tools.Core.Infrastructure.Events;
using Miller_Craft_Tools.Core.Infrastructure.Logging;
using System;

namespace Miller_Craft_Tools.Features.EfficiencyTools.Commands
{
    [Transaction(TransactionMode.Manual)]
    [CommandAttribute(
        CommandId = "ShowEfficiencyDashboard",
        ButtonText = "Efficiency Dashboard",
        ToolTip = "Opens the efficiency dashboard",
        PanelName = "Efficiency Tools",
        IconName = "Dashboard")]
    public class ShowEfficiencyDashboardCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                // Log the command execution
                LogManager.LogInfo("ShowEfficiencyDashboard command executed");

                // Get application and document
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;

                // Show the dashboard dialog
                var dialog = new EfficiencyDashboardDialog(uiDoc);
                dialog.ShowDialog();

                // Publish event
                EventManager.Publish(EventNames.CommandExecuted, "ShowEfficiencyDashboard");

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // Log the exception
                LogManager.LogError($"Error executing ShowEfficiencyDashboard: {ex.Message}");
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}