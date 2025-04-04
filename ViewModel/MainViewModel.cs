﻿using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Miller_Craft_Tools.Command;
using Miller_Craft_Tools.Controller;
using Miller_Craft_Tools.Views;
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
        public ICommand SetupStandardsCommand { get; } // New command

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
            RenumberWindowsCommand = new RelayCommand(RenumberWindowsExecute);
            RenumberViewsCommand = new RelayCommand(RenumberViewsExecute);
            SetupStandardsCommand = new RelayCommand(SetupStandardsExecute); // Initialize the new command

            // Subscribe to MainView events
            _view.SyncFilledRegionsClicked += (s, e) => SyncFilledRegionsExecute(null);
            _view.RenumberWindowsClicked += (s, e) => RenumberWindowsExecute(null);
            _view.RenumberViewsClicked += (s, e) => RenumberViewsExecute(null);
            _view.GroupElementsByLevelClicked += (s, e) => GroupElementsByLevelExecute(null);
            _view.ExportStandardsClicked += (s, e) => ExportStandardsExecute(null);
            _view.CopyToSheetsClicked += (s, e) => CopyToSheetsExecute(null);
            _view.SetupStandardsClicked += (s, e) => SetupStandardsExecute(null); // Subscribe to the new event
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

        private void RenumberWindowsExecute(object parameter)
        {
            _draftingController.RenumberWindows(_view);
        }

        private void RenumberViewsExecute(object parameter)
        {
            _draftingController.RenumberViewsOnSheet(_view);
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
    }
}