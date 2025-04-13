using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Miller_Craft_Tools.ViewModel;
using Miller_Craft_Tools.Views;
using System;

namespace Miller_Craft_Tools.Commands
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class AuditModelCommand : IExternalCommand
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
                AuditViewModel viewModel = new AuditViewModel(doc);
                AuditView auditView = new AuditView { DataContext = viewModel };
                auditView.ShowDialog();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = $"Failed to execute Audit Model: {ex.Message}";
                return Result.Failed;
            }
        }
    }
}