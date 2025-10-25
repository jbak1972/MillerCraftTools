using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Miller_Craft_Tools.Controller;
using System;

namespace Miller_Craft_Tools.Command
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class CompareViewTemplatesCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                if (uidoc == null)
                {
                    message = "No active document found.";
                    return Result.Failed;
                }

                Document doc = uidoc.Document;
                
                // Create a controller instance and call the compare method
                DraftingController controller = new DraftingController(doc, uidoc);
                controller.CompareViewTemplates();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = $"Failed to execute Compare View Templates: {ex.Message}";
                return Result.Failed;
            }
        }
    }
}
