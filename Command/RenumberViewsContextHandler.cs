using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Miller_Craft_Tools.Controller;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Miller_Craft_Tools.Command
{
    /// <summary>
    /// Handles the renumber views functionality with contextual ribbon support
    /// </summary>
    public class RenumberViewsContextHandler
    {
        private DraftingController _controller;
        private bool _isExecuting = false;
        private TransactionGroup _transGroup = null;
        
        public RenumberViewsContextHandler(DraftingController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        public void Execute()
        {
            if (_isExecuting)
                return;

            _isExecuting = true;
            
            // Get the document and UIDocument using the properties we added to DraftingController
            Document doc = _controller.Document;
            UIDocument uidoc = _controller.UIDocument;
            
            if (doc == null || uidoc == null)
            {
                Autodesk.Revit.UI.TaskDialog.Show("Error", 
                    "Could not access required Revit document objects.");
                _isExecuting = false;
                return;
            }

            try
            {
                // Get the active view or have user select a sheet
                Autodesk.Revit.DB.View activeView = doc.ActiveView;
                ViewSheet selectedSheet;

                if (activeView is ViewSheet sheet)
                {
                    selectedSheet = sheet;
                }
                else
                {
                    ISelectionFilter sheetFilter = new SheetFilter();
                    Reference sheetReference = uidoc.Selection.PickObject(
                        ObjectType.Element,
                        sheetFilter,
                        "Select a sheet"
                    );
                    
                    selectedSheet = doc.GetElement(sheetReference) as ViewSheet;
                    if (selectedSheet == null) 
                    {
                        Autodesk.Revit.UI.TaskDialog.Show("Error", "Selected element is not a sheet.");
                        _isExecuting = false;
                        return;
                    }
                }

                // Get viewports on the sheet
                List<ElementId> viewportIds = new FilteredElementCollector(doc, selectedSheet.Id)
                    .OfCategory(BuiltInCategory.OST_Viewports)
                    .ToElementIds()
                    .ToList();

                if (viewportIds.Count == 0)
                {
                    Autodesk.Revit.UI.TaskDialog.Show("Error", "No views found on this sheet.");
                    _isExecuting = false;
                    return;
                }

                // Get starting number from user
                int startNumber = PromptForStartNumber();
                if (startNumber < 0)
                {
                    _isExecuting = false;
                    return;
                }

                // Create transaction group to allow for cancellation
                _transGroup = new TransactionGroup(doc, "Renumber Views");
                _transGroup.Start();

                int currentNumber = startNumber;
                List<ElementId> selectedViewports = new List<ElementId>();

                // Show instructions
                Autodesk.Revit.UI.TaskDialog.Show(
                    "View Renumbering",
                    "Select views in the order you want to renumber them.\n\n" +
                    "When finished, click the 'Finish' button on the ribbon tab.\n" +
                    "To cancel, click the 'Cancel' button."
                );

                while (true)
                {
                    // Exit the loop if the command has been cancelled or completed via the ribbon
                    if (!RenumberViewsCommand.CommandController.IsActive)
                    {
                        break;
                    }

                    try
                    {
                        Reference selectedReference = uidoc.Selection.PickObject(
                            ObjectType.Element,
                            new ViewportFilter(viewportIds),
                            $"Select view {currentNumber} or use ribbon buttons to finish/cancel"
                        );

                        if (!RenumberViewsCommand.CommandController.IsActive)
                        {
                            break; // Check again after selection in case user clicked ribbon during selection
                        }

                        ElementId selectedViewId = selectedReference.ElementId;
                        if (!selectedViewports.Contains(selectedViewId))
                        {
                            selectedViewports.Add(selectedViewId);

                            using (Transaction transaction = new Transaction(doc, $"Renumber Viewport {currentNumber}"))
                            {
                                transaction.Start();

                                try
                                {
                                    // Check if this detail number is already used
                                    if (IsDetailNumberUsed(selectedSheet.Id, currentNumber.ToString(), doc))
                                    {
                                        ShiftFollowingDetailNumbers(selectedSheet.Id, currentNumber, doc);
                                    }

                                    Viewport viewport = doc.GetElement(selectedViewId) as Viewport;
                                    if (viewport != null)
                                    {
                                        viewport.get_Parameter(BuiltInParameter.VIEWPORT_DETAIL_NUMBER).Set(currentNumber.ToString());
                                        transaction.Commit();
                                        currentNumber++;
                                    }
                                    else
                                    {
                                        transaction.RollBack();
                                        Autodesk.Revit.UI.TaskDialog.Show("Error", "Could not set viewport detail number.");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    transaction.RollBack();
                                    Autodesk.Revit.UI.TaskDialog.Show("Error", $"Error during renumbering: {ex.Message}");
                                }
                            }
                        }
                        else
                        {
                            Autodesk.Revit.UI.TaskDialog.Show("Warning", "This viewport has already been selected.");
                        }
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                        // User pressed ESC - create a custom dialog with clear options
                        Autodesk.Revit.UI.TaskDialog dialog = new Autodesk.Revit.UI.TaskDialog("Renumbering Paused");
                        dialog.MainInstruction = "What would you like to do?"; 
                        dialog.MainContent = $"You have renumbered {selectedViewports.Count} view(s) so far.";
                        dialog.CommonButtons = Autodesk.Revit.UI.TaskDialogCommonButtons.None;
                        
                        // Add custom buttons with clear labels
                        dialog.AddCommandLink(Autodesk.Revit.UI.TaskDialogCommandLinkId.CommandLink1, 
                            "Continue Renumbering", 
                            "Return to selection mode and continue renumbering more views.");
                        dialog.AddCommandLink(Autodesk.Revit.UI.TaskDialogCommandLinkId.CommandLink2, 
                            "Save & Finish", 
                            "Save all changes and exit renumbering mode.");
                        dialog.AddCommandLink(Autodesk.Revit.UI.TaskDialogCommandLinkId.CommandLink3, 
                            "Cancel & Discard Changes", 
                            "Discard all changes and exit renumbering mode.");
                        
                        dialog.DefaultButton = Autodesk.Revit.UI.TaskDialogResult.CommandLink1;
                        
                        // Show the dialog and get the result
                        Autodesk.Revit.UI.TaskDialogResult result = dialog.Show();

                        // Handle each of our command link options
                        if (result == Autodesk.Revit.UI.TaskDialogResult.CommandLink1)
                        {
                            // User chose to continue renumbering - just continue the loop
                            continue;
                        }
                        else if (result == Autodesk.Revit.UI.TaskDialogResult.CommandLink2)
                        {
                            // User chose "Save & Finish" - assimilate the transaction group
                            if (_transGroup.HasStarted() && _transGroup.GetStatus() == TransactionStatus.Started)
                            {
                                _transGroup.Assimilate();
                            }
                            
                            // Signal successful completion through the command controller
                            RenumberViewsCommand.CommandController.OnFinishCommand(this, EventArgs.Empty);
                            break;
                        }
                        else if (result == Autodesk.Revit.UI.TaskDialogResult.CommandLink3)
                        {
                            // User chose "Cancel & Discard Changes" - roll back the transaction group
                            if (_transGroup.HasStarted() && _transGroup.GetStatus() == TransactionStatus.Started)
                            {
                                _transGroup.RollBack();
                            }
                            
                            // Signal cancellation through the command controller
                            RenumberViewsCommand.CommandController.OnCancelCommand(this, EventArgs.Empty);
                            break;
                        }
                        else
                        {
                            // Dialog was dismissed in some other way (X button, etc.)
                            // Default to continuing the loop
                            continue;
                        }
                    }
                }

                // Finalize based on command controller state
                if (RenumberViewsCommand.CommandController.IsFinished && !RenumberViewsCommand.CommandController.IsCancelled)
                {
                    if (_transGroup.HasStarted() && _transGroup.GetStatus() == TransactionStatus.Started)
                    {
                        _transGroup.Assimilate();
                    }
                    Autodesk.Revit.UI.TaskDialog.Show("Success", $"{selectedViewports.Count} view(s) renumbered successfully.");
                }
                else
                {
                    if (_transGroup.HasStarted() && _transGroup.GetStatus() == TransactionStatus.Started)
                    {
                        _transGroup.RollBack();
                    }
                }
            }
            catch (Exception ex)
            {
                Autodesk.Revit.UI.TaskDialog.Show("Error", $"An error occurred: {ex.Message}");
                if (_transGroup != null && _transGroup.HasStarted() && _transGroup.GetStatus() == TransactionStatus.Started)
                {
                    _transGroup.RollBack();
                }
            }
            finally
            {
                _isExecuting = false;
            }
        }

        private int PromptForStartNumber()
        {
            using (var form = new System.Windows.Forms.Form())
            {
                form.TopMost = true;
                form.Width = 350;
                form.Height = 150;
                form.Text = "View Renumbering";
                form.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
                form.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                
                // Create the label
                var label = new System.Windows.Forms.Label();
                label.Text = "Enter starting number:";
                label.Left = 20;
                label.Top = 20;
                label.Width = 200;
                form.Controls.Add(label);
                
                // Create the text box
                var textBox = new System.Windows.Forms.TextBox();
                textBox.Text = "1";
                textBox.Left = 20;
                textBox.Top = 50;
                textBox.Width = 100;
                form.Controls.Add(textBox);
                
                // Create the OK button
                var okButton = new System.Windows.Forms.Button();
                okButton.Text = "OK";
                okButton.Left = 130;
                okButton.Top = 80;
                okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
                form.Controls.Add(okButton);
                
                // Create the Cancel button
                var cancelButton = new System.Windows.Forms.Button();
                cancelButton.Text = "Cancel";
                cancelButton.Left = 230;
                cancelButton.Top = 80;
                cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
                form.Controls.Add(cancelButton);
                
                // Set the accept and cancel buttons
                form.AcceptButton = okButton;
                form.CancelButton = cancelButton;
                
                // Show the form and get the result
                if (form.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    return int.TryParse(textBox.Text, out int startNumber) ? startNumber : -1;
                }
                
                return -1; // User cancelled
            }
        }

        private bool IsDetailNumberUsed(ElementId sheetId, string detailNumber, Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc, sheetId)
                .OfCategory(BuiltInCategory.OST_Viewports);

            foreach (Viewport vp in collector)
            {
                string currentNumber = vp.get_Parameter(BuiltInParameter.VIEWPORT_DETAIL_NUMBER).AsString();
                if (currentNumber == detailNumber)
                {
                    return true;
                }
            }
            return false;
        }

        private void ShiftFollowingDetailNumbers(ElementId sheetId, int startNumber, Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc, sheetId)
                .OfCategory(BuiltInCategory.OST_Viewports);

            List<Viewport> viewportsToShift = new List<Viewport>();
            foreach (Viewport vp in collector)
            {
                if (int.TryParse(vp.get_Parameter(BuiltInParameter.VIEWPORT_DETAIL_NUMBER).AsString(), out int vpNumber) && vpNumber >= startNumber)
                {
                    viewportsToShift.Add(vp);
                }
            }

            viewportsToShift.Sort((x, y) =>
                int.Parse(y.get_Parameter(BuiltInParameter.VIEWPORT_DETAIL_NUMBER).AsString())
                - int.Parse(x.get_Parameter(BuiltInParameter.VIEWPORT_DETAIL_NUMBER).AsString()));

            foreach (Viewport vp in viewportsToShift)
            {
                int currentNumber = int.Parse(vp.get_Parameter(BuiltInParameter.VIEWPORT_DETAIL_NUMBER).AsString());
                vp.get_Parameter(BuiltInParameter.VIEWPORT_DETAIL_NUMBER).Set((currentNumber + 1).ToString());
            }
        }

        private class SheetSelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem)
            {
                if (elem == null) return false;
                return elem is ViewSheet;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }

        private class ViewportSelectionFilter : ISelectionFilter
        {
            private readonly ICollection<ElementId> _viewportIds;

            public ViewportSelectionFilter(ICollection<ElementId> viewportIds)
            {
                _viewportIds = viewportIds;
            }

            public bool AllowElement(Element elem)
            {
                if (elem == null) return false;
                return _viewportIds.Contains(elem.Id);
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }
    }
    
    /// <summary>
    /// Selection filter that allows only ViewSheet elements to be picked
    /// </summary>
    public class SheetFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem is ViewSheet;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
    
    /// <summary>
    /// Selection filter that allows only specific Viewport elements to be picked
    /// </summary>
    public class ViewportFilter : ISelectionFilter
    {
        private List<ElementId> _viewportIds;
        
        public ViewportFilter(List<ElementId> viewportIds)
        {
            _viewportIds = viewportIds;
        }
        
        public bool AllowElement(Element elem)
        {
            if (elem == null) return false;
            return _viewportIds.Contains(elem.Id);
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
