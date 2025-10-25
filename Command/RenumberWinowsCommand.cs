using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Miller_Craft_Tools.Command.UI; // Added for the control form
using Miller_Craft_Tools.Controller;
using Miller_Craft_Tools.Model;
using System;
using System.Collections.Generic;
using System.Linq; // Added for .Any()
using System.Threading.Tasks; // Added for TaskCompletionSource
using System.Windows.Forms; // Full namespace for clarity, though already used

namespace Miller_Craft_Tools.Command
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class RenumberWindowsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            if (doc == null)
            {
                message = "No active document found.";
                return Result.Failed;
            }

            int startNumber = PromptForStartNumber();
            if (startNumber < 0) return Result.Cancelled; // User cancelled initial prompt

            var userSettings = UserSettings.Load();
            if (userSettings.Open3DViewsForRenumbering)
            {
                try
                {
                    DraftingController controller = new DraftingController(doc, uidoc);
                    controller.CreateMultiple3DViews();
                    Autodesk.Revit.UI.TaskDialog.Show("Tile Views", "Tip: Press WT (Window > Tile) to tile views.");
                }
                catch (Exception ex)
                {
                    Autodesk.Revit.UI.TaskDialog.Show("3D View Error", $"Failed to open 3D views: {ex.Message}");
                    // Continue without 3D views if this fails
                }
            }

            RenumberingControlForm controlForm = null;
            NativeWindow revitMainWindowOwner = new NativeWindow(); // For owning the form
            TransactionGroup tg = new TransactionGroup(doc, "Renumber Windows Group"); 

            try
            {
                controlForm = new RenumberingControlForm();
                var tcs = new TaskCompletionSource<bool>(); // True for finish, False for cancel

                controlForm.FinishClicked += (s, e) => tcs.TrySetResult(true);
                controlForm.FormCancelled += (s, e) => tcs.TrySetResult(false);
                // Also handle direct form closing if not covered by FormCancelled
                controlForm.FormClosed += (s, e) => tcs.TrySetResult(false); 

                revitMainWindowOwner.AssignHandle(commandData.Application.MainWindowHandle);
                controlForm.Show(revitMainWindowOwner); // Show modelessly, owned by Revit

                tg.Start();

                int currentNumber = startNumber;
                List<Tuple<Element, string>> windowsToUpdate = new List<Tuple<Element, string>>();
                WindowSelectionFilter windowFilter = new WindowSelectionFilter();

                while (true)
                {
                    if (tcs.Task.IsCompleted) break; // Check if form actioned before picking

                    controlForm.SetStatus($"Select window for #{currentNumber}, or click Finish. {windowsToUpdate.Count} selected.");
                    Reference pickedRef = null;
                    bool shouldBreak = false;
                    try
                    {
                        pickedRef = uidoc.Selection.PickObject(
                            ObjectType.Element,
                            windowFilter,
                            $"Select window for #{currentNumber}. Or, use dialog to Finish. ESC cancels current pick.");
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException) // User pressed ESC during PickObject
                    {
                        if (tcs.Task.IsCompleted) break; // Form actioned during pick, exit loop

                        Autodesk.Revit.UI.TaskDialog td = new Autodesk.Revit.UI.TaskDialog("Selection Cancelled");
                        td.MainInstruction = "Current selection attempt cancelled.";
                        td.MainContent = "Do you want to select another window, or finish renumbering?";
                        td.CommonButtons = Autodesk.Revit.UI.TaskDialogCommonButtons.None;
                        td.AddCommandLink(Autodesk.Revit.UI.TaskDialogCommandLinkId.CommandLink1, "Select another window");
                        td.AddCommandLink(Autodesk.Revit.UI.TaskDialogCommandLinkId.CommandLink2, "Finish and save changes");
                        td.DefaultButton = Autodesk.Revit.UI.TaskDialogResult.CommandLink1;
                        Autodesk.Revit.UI.TaskDialogResult tdResult = td.Show();

                        if (tdResult == Autodesk.Revit.UI.TaskDialogResult.CommandLink1)
                        {
                            continue; // Go back to PickObject
                        }
                        else // CommandLink2 or dialog closed (treat as finish)
                        {
                            tcs.TrySetResult(true); // Signal to finish
                            break; // Exit pick loop
                        }
                    }
                    catch (Exception ex)
                    {
                        message = $"Error during selection: {ex.Message}";
                        if (tg.HasStarted() && tg.GetStatus() == TransactionStatus.Started) tg.RollBack();
                        return Result.Failed;
                    }
                    finally
                    {
                        // Check if form was closed or finish clicked while PickObject was active
                        if (tcs.Task.IsCompleted) shouldBreak = true;
                    }
                    if (shouldBreak) break;

                    if (pickedRef == null) continue; // Should not happen if no exception and not cancelled
                    
                    Element window = doc.GetElement(pickedRef);
                    if (window == null || window.Category == null || !window.Category.Id.Equals(new ElementId(BuiltInCategory.OST_Windows)))
                    {
                        Autodesk.Revit.UI.TaskDialog.Show("Warning", "Selected element is not a window. Please select a window.");
                        continue;
                    }

                    windowsToUpdate.Add(Tuple.Create(window, currentNumber.ToString()));
                    currentNumber++;
                } // End of while loop for picking

                bool shouldCommit = tcs.Task.IsCompleted && tcs.Task.Result;

                if (shouldCommit && windowsToUpdate.Any())
                {
                    using (Transaction tx = new Transaction(doc, "Renumber Selected Windows"))
                    {
                        tx.Start();
                        foreach (var item in windowsToUpdate)
                        {
                            Parameter markParam = item.Item1.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
                            if (markParam != null && !markParam.IsReadOnly)
                            {
                                markParam.Set(item.Item2);
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"Could not set mark for window {item.Item1.Id} - parameter issue.");
                            }
                        }
                        tx.Commit();
                    }
                    tg.Assimilate();
                    Autodesk.Revit.UI.TaskDialog.Show("Success", $"{windowsToUpdate.Count} window(s) renumbered successfully.");
                    return Result.Succeeded;
                }
                else
                {
                    tg.RollBack();
                    if (windowsToUpdate.Any() && !shouldCommit)
                    {
                        Autodesk.Revit.UI.TaskDialog.Show("Cancelled", "Window renumbering cancelled. No changes were made.");
                    }
                    else if (!windowsToUpdate.Any())
                    {
                        Autodesk.Revit.UI.TaskDialog.Show("Information", "No windows were selected for renumbering.");
                    }
                    return Result.Cancelled;
                }
            }
            catch (Exception ex)
            {
                message = $"Unhandled error: {ex.Message}";
                if (tg.HasStarted() && tg.GetStatus() == TransactionStatus.Started) tg.RollBack();
                return Result.Failed;
            }
            finally
            {
                controlForm?.Close(); // Ensure form is closed
                if (revitMainWindowOwner.Handle != IntPtr.Zero) revitMainWindowOwner.ReleaseHandle();
            }
        }
        
        private int PromptForStartNumber()
        {
            using (System.Windows.Forms.Form form = new System.Windows.Forms.Form())
            {
                form.Text = "Window Renumbering";
                form.Width = 300;
                form.Height = 150;
                form.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
                form.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                
                System.Windows.Forms.Label label = new System.Windows.Forms.Label();
                label.Text = "Enter starting number:";
                label.Location = new System.Drawing.Point(20, 20);
                label.AutoSize = true;
                form.Controls.Add(label);
                
                System.Windows.Forms.TextBox textBox = new System.Windows.Forms.TextBox();
                textBox.Text = "1";
                textBox.Location = new System.Drawing.Point(20, 50);
                textBox.Width = 100;
                form.Controls.Add(textBox);
                
                System.Windows.Forms.Button okButton = new System.Windows.Forms.Button();
                okButton.Text = "OK";
                okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
                okButton.Location = new System.Drawing.Point(70, 80);
                form.Controls.Add(okButton);
                
                System.Windows.Forms.Button cancelButton = new System.Windows.Forms.Button();
                cancelButton.Text = "Cancel";
                cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
                cancelButton.Location = new System.Drawing.Point(150, 80);
                form.Controls.Add(cancelButton);
                
                form.AcceptButton = okButton;
                form.CancelButton = cancelButton;
                
                if (form.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    return int.TryParse(textBox.Text, out int result) ? result : 1;
                }
                
                return -1; // User cancelled
            }
        }
        
        /// <summary>
        /// Window selection filter for Revit
        /// </summary>
        private class WindowSelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem)
            {
                if (elem == null || elem.Category == null) return false;
                // Use the proper API method to check category
                return elem.Category.Id.Equals(new ElementId(BuiltInCategory.OST_Windows));
            }
            
            public bool AllowReference(Reference reference, XYZ position)
            {
                return true; // Let AllowElement make the determination
            }
        }
    }
}