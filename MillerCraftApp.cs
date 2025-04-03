using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Miller_Craft_Tools.Controller;
using Miller_Craft_Tools.ViewModel;
using Miller_Craft_Tools.Views;
using System;

namespace Miller_Craft_Tools
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class MillerCraftApp : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Store the ExternalCommandData in CommandDataHolder
                CommandDataHolder.CommandData = commandData;

                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                Document doc = uidoc.Document;

                // Instantiate controllers
                DraftingController draftingController = new DraftingController(doc, uidoc);
                InspectionController inspectionController = new InspectionController(doc, uidoc);
                SheetUtilitiesController sheetUtilitiesController = new SheetUtilitiesController(doc, uidoc);

                // Create view and view model
                MainView view = new MainView();
                MainViewModel viewModel = new MainViewModel(view, draftingController, inspectionController, sheetUtilitiesController);

                // Show the dialog via the view model
                viewModel.ShowDialog(uidoc);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Error", $"An error occurred: {ex.Message}");
                return Result.Failed;
            }
            finally
            {
                // Clear the CommandDataHolder to avoid holding onto the reference after the command finishes
                CommandDataHolder.CommandData = null;
            }
        }
    }
}