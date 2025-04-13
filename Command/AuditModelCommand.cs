using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Miller_Craft_Tools.ViewModel;
using Miller_Craft_Tools.Views;
using System.Windows;

namespace Miller_Craft_Tools.Command
{
    [Transaction(TransactionMode.ReadOnly)]
    public class AuditModelCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                Document doc = uiApp.ActiveUIDocument.Document;

                // Create and show the audit window
                AuditViewModel viewModel = new AuditViewModel(doc);
                AuditView auditView = new AuditView { DataContext = viewModel };
                auditView.ShowDialog();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Audit Model Error");
                return Result.Failed;
            }
        }
    }
}