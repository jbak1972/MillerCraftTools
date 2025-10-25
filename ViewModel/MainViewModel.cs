using Miller_Craft_Tools.Views;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Miller_Craft_Tools.Command;
using Miller_Craft_Tools.Controller;
using Miller_Craft_Tools.ViewModel;
using System;
using System.Windows.Input;

namespace Miller_Craft_Tools.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private readonly DraftingController _draftingController;
        private readonly InspectionController _inspectionController;
       // private readonly SheetUtilitiesController _sheetUtilitiesController;
        private readonly MainView _view;

        public ICommand GroupElementsByLevelCommand { get; }
        public ICommand ExportStandardsCommand { get; }
        public ICommand CopyToSheetsCommand { get; }
        public ICommand SyncFilledRegionsCommand { get; }
        public ICommand RenumberWindowsCommand { get; }
        public ICommand RenumberViewsCommand { get; }
       // public ICommand SetupStandardsCommand { get; }
       // public ICommand AuditModelCommand { get; } // New command

        public MainViewModel(MainView view, DraftingController draftingController, InspectionController inspectionController)
        {
            _view = view;
            _draftingController = draftingController;
            _inspectionController = inspectionController;
            //_sheetUtilitiesController = sheetUtilitiesController;

            GroupElementsByLevelCommand = new RelayCommand(GroupElementsByLevelExecute);
            ExportStandardsCommand = new RelayCommand(ExportStandardsExecute);
            CopyToSheetsCommand = new RelayCommand(CopyToSheetsExecute);
            SyncFilledRegionsCommand = new RelayCommand(SyncFilledRegionsExecute);
           // SetupStandardsCommand = new RelayCommand(SetupStandardsExecute);
            //AuditModelCommand = new RelayCommand(AuditModelExecute); // Initialize the new command

            // Subscribe to MainAutodesk.Revit.DB.View events
            _view.SyncFilledRegionsClicked += (s, e) => SyncFilledRegionsExecute(null);
            _view.GroupElementsByLevelClicked += (s, e) => GroupElementsByLevelExecute(null);
            _view.ExportStandardsClicked += (s, e) => ExportStandardsExecute(null);
            _view.CopyToSheetsClicked += (s, e) => CopyToSheetsExecute(null);
            _view.ClearProjectInfoClicked += (s, e) => ClearProjectInfoExecute();
            //_view.SetupStandardsClicked += (s, e) => SetupStandardsExecute(null);
           // _view.AuditModelClicked += (s, e) => AuditModelExecute(null); // Subscribe to the new event
        }

        public void ShowDialog(UIDocument uidoc)
        {
            _draftingController.SetUIDocument(uidoc);
            _inspectionController.SetUIDocument(uidoc);
            //_sheetUtilitiesController.SetUIDocument(uidoc);
            _view.ShowDialog();
        }

        private void SyncFilledRegionsExecute(object parameter)
        {
            _draftingController.UpdateDetailItems();
        }


        private void GroupElementsByLevelExecute(object parameter)
        {
            var resultsView = new ResultsView(_inspectionController.GetUIDocument(), new List<LevelNode>()); // Pass empty list initially
            _inspectionController.GroupElementsByLevel(resultsView);
        }

        private void ExportStandardsExecute(object parameter)
        {
            var resultsView = new ResultsView(_inspectionController.GetUIDocument(), new List<LevelNode>()); // Pass empty list initially
            _inspectionController.ExportStandards(resultsView);
        }

        private void CopyToSheetsExecute(object parameter)
        {
            //_sheetUtilitiesController.CopyToSheets(_view);
        }

        private void ClearProjectInfoExecute()
        {
            var result = Autodesk.Revit.UI.TaskDialog.Show(
                "Clear Project Info",
                "This will clear all project-specific information, including the MC Project GUID. Are you sure you want to continue?",
                Autodesk.Revit.UI.TaskDialogCommonButtons.Yes | Autodesk.Revit.UI.TaskDialogCommonButtons.No);
            if (result == Autodesk.Revit.UI.TaskDialogResult.Yes)
            {
                _inspectionController.ClearProjectInformation();
                Autodesk.Revit.UI.TaskDialog.Show("Clear Project Info", "Project Information has been cleared.");
            }
        }
    }
}