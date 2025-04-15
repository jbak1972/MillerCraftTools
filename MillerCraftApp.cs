using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Miller_Craft_Tools.ViewModel;
using Miller_Craft_Tools.Views;
using System;
using System.IO;
using System.Reflection;

namespace Miller_Craft_Tools
{
    public class MillerCraftApp : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // Create a custom ribbon tab
                string tabName = "Miller Craft Tools";
                application.CreateRibbonTab(tabName);

                // Create a ribbon panel
                RibbonPanel panel = application.CreateRibbonPanel(tabName, "Project Maintenance");

                // Add "Audit Model" button (already exists)
                PushButtonData auditButtonData = new PushButtonData("AuditModelButton", "Audit Model", Assembly.GetExecutingAssembly().Location, "Miller_Craft_Tools.Command.AuditModelCommand");
                auditButtonData.ToolTip = "Audit the model and display statistics like file size and element counts.";
                auditButtonData.LongDescription = "This tool analyzes the current Revit model and provides statistics such as file size, element count, family count, warnings, DWG imports, and schema sizes.";
                PushButton auditButton = panel.AddItem(auditButtonData) as PushButton;

                // Add "Renumber Views on Sheet" button
                PushButtonData renumberViewsButtonData = new PushButtonData("RenumberViewsButton", "Renumber Views on Sheet", Assembly.GetExecutingAssembly().Location, "Miller_Craft_Tools.Command.RenumberViewsCommand");
                renumberViewsButtonData.ToolTip = "Renumber views on a selected sheet.";
                renumberViewsButtonData.LongDescription = "This tool allows you to renumber the detail numbers of viewports on a sheet by selecting them in sequence.";
                PushButton renumberViewsButton = panel.AddItem(renumberViewsButtonData) as PushButton;

                // Add "Renumber Windows" button
                PushButtonData renumberWindowsButtonData = new PushButtonData("RenumberWindowsButton", "Renumber Windows", Assembly.GetExecutingAssembly().Location, "Miller_Craft_Tools.Command.RenumberWindowsCommand");
                renumberWindowsButtonData.ToolTip = "Renumber windows in the model.";
                renumberWindowsButtonData.LongDescription = "This tool allows you to renumber windows by assigning new mark values, resolving conflicts automatically.";
                PushButton renumberWindowsButton = panel.AddItem(renumberWindowsButtonData) as PushButton;

                // Add "Sync sp.Area" button
                PushButtonData syncAreaButtonData = new PushButtonData("SyncAreaButton", "Sync sp.Area", Assembly.GetExecutingAssembly().Location, "Miller_Craft_Tools.Command.SyncFilledRegionsCommand");
                syncAreaButtonData.ToolTip = "Sync sp.Area parameter with Area for filled regions.";
                syncAreaButtonData.LongDescription = "This tool updates the sp.Area parameter of filled regions to match their Area parameter.";
                PushButton syncAreaButton = panel.AddItem(syncAreaButtonData) as PushButton;

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Failed to initialize Miller Craft Tools: {ex.Message}");
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}