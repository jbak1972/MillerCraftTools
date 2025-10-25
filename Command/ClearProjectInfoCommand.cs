using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Miller_Craft_Tools.Controller;

namespace Miller_Craft_Tools.Command
{
    [Transaction(TransactionMode.Manual)]
    public class ClearProjectInfoCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            var controller = new InspectionController(doc, uidoc);
            var result = Autodesk.Revit.UI.TaskDialog.Show(
                "Clear Project Info",
                "This will clear all project-specific information, including the MC Project GUID. Are you sure you want to continue?",
                Autodesk.Revit.UI.TaskDialogCommonButtons.Yes | Autodesk.Revit.UI.TaskDialogCommonButtons.No);
            if (result == Autodesk.Revit.UI.TaskDialogResult.Yes)
            {
                controller.ClearProjectInformation();
                Autodesk.Revit.UI.TaskDialog.Show("Clear Project Info", "Project Information has been cleared.");
            }
            return Result.Succeeded;
        }
    }
}
