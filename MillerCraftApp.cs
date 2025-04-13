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

                // Add a button for "Audit Model"
                PushButtonData auditButtonData = new PushButtonData("AuditModelButton", "Audit Model", Assembly.GetExecutingAssembly().Location, "Miller_Craft_Tools.Commands.AuditModelCommand");
                auditButtonData.ToolTip = "Audit the model and display statistics like file size and element counts.";
                auditButtonData.LongDescription = "This tool analyzes the current Revit model and provides statistics such as file size, element count, family count, warnings, DWG imports, and schema sizes.";

                PushButton auditButton = panel.AddItem(auditButtonData) as PushButton;

                // Optionally, set an image for the button (place images in the same directory as the DLL)
                // auditButton.LargeImage = new BitmapImage(new Uri(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "AuditModelIcon.png")));

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
            // Cleanup if needed
            return Result.Succeeded;
        }
    }
}