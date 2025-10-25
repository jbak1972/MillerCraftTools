using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Miller_Craft_Tools.Command
{
    [Transaction(TransactionMode.Manual)]
    public class SettingsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Open the new WPF Settings dialog for user settings
                var settingsView = new Miller_Craft_Tools.Views.SettingsView();
                settingsView.ShowDialog();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Autodesk.Revit.UI.TaskDialog.Show("Settings", $"Error: {ex.Message}");
                return Result.Failed;
            }
        }
    }
}
