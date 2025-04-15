using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Miller_Craft_Tools.Command;
using Miller_Craft_Tools.Controller;
using Miller_Craft_Tools.Views;
using Miller_Craft_Tools.ViewModel;
using System;
using System.Windows.Input;

namespace Miller_Craft_Tools.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private readonly DraftingController _draftingController;
        private readonly InspectionController _inspectionController;
        private readonly SheetUtilitiesController _sheetUtilitiesController;
        private readonly MainView _view;

        public ICommand GroupElementsByLevelCommand { get; }
        public ICommand ExportStandardsCommand { get; }
        public ICommand CopyToSheetsCommand { get; }
        public ICommand SyncFilledRegionsCommand { get; }
        public ICommand RenumberWindowsCommand { get; }
        public ICommand RenumberViewsCommand { get; }
        public ICommand SetupStandardsCommand { get; }
        public ICommand AuditModelCommand { get; } // New command

        public MainViewModel(MainView view, DraftingController draftingController, InspectionController inspectionController, SheetUtilitiesController sheetUtilitiesController)
        {
            _view = view;
            _draftingController = draftingController;
            _inspectionController = inspectionController;
            _sheetUtilitiesController = sheetUtilitiesController;

            GroupElementsByLevelCommand = new RelayCommand(GroupElementsByLevelExecute);
            ExportStandardsCommand = new RelayCommand(ExportStandardsExecute);
            CopyToSheetsCommand = new RelayCommand(CopyToSheetsExecute);
            SyncFilledRegionsCommand = new RelayCommand(SyncFilledRegionsExecute);
            SetupStandardsCommand = new RelayCommand(SetupStandardsExecute);
            AuditModelCommand = new RelayCommand(AuditModelExecute); // Initialize the new command

            // Subscribe to MainView events
            _view.SyncFilledRegionsClicked += (s, e) => SyncFilledRegionsExecute(null);
            _view.GroupElementsByLevelClicked += (s, e) => GroupElementsByLevelExecute(null);
            _view.ExportStandardsClicked += (s, e) => ExportStandardsExecute(null);
            _view.CopyToSheetsClicked += (s, e) => CopyToSheetsExecute(null);
            _view.SetupStandardsClicked += (s, e) => SetupStandardsExecute(null);
            _view.AuditModelClicked += (s, e) => AuditModelExecute(null); // Subscribe to the new event
        }

        public void ShowDialog(UIDocument uidoc)
        {
            _draftingController.SetUIDocument(uidoc);
            _inspectionController.SetUIDocument(uidoc);
            _sheetUtilitiesController.SetUIDocument(uidoc);
            _view.ShowDialog();
        }

        private void SyncFilledRegionsExecute(object parameter)
        {
            _draftingController.UpdateDetailItems();
        }


        private void GroupElementsByLevelExecute(object parameter)
        {
            _inspectionController.GroupElementsByLevel(_view);
        }

        private void ExportStandardsExecute(object parameter)
        {
            _inspectionController.ExportStandards(_view);
        }

        private void CopyToSheetsExecute(object parameter)
        {
            _sheetUtilitiesController.CopyToSheets(_view);
        }

        private void SetupStandardsExecute(object parameter)
        {
            try
            {
                ExternalCommandData commandData = CommandDataHolder.CommandData;
                if (commandData == null)
                {
                    TaskDialog.Show("Error", "ExternalCommandData is not available. Please run this command from a Revit ribbon button.");
                    return;
                }

                var command = new SetupStandardsCommand();
                string message = string.Empty;
                ElementSet elements = new ElementSet();
                Result result = command.Execute(commandData, ref message, elements);

                if (result != Result.Succeeded)
                {
                    TaskDialog.Show("Error", message);
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Failed to execute Setup Standards: {ex.Message}");
            }
        }

        private void AuditModelExecute(object parameter)
        {
            try
            {
                ExternalCommandData commandData = CommandDataHolder.CommandData;
                if (commandData == null)
                {
                    TaskDialog.Show("Error", "ExternalCommandData is not available.");
                    return;
                }

                Document doc = commandData.Application.ActiveUIDocument.Document;
                AuditViewModel viewModel = new AuditViewModel(doc);
                AuditView auditView = new AuditView { DataContext = viewModel };
                auditView.ShowDialog();
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Failed to execute Audit Model: {ex.Message}");
            }
        }
    }
}