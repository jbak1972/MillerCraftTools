using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic;
using System.Windows.Forms;

namespace Miller_Craft_Tools.Controller
{
    public partial class DraftingController
    {
        private Document _doc;
        private UIDocument _uidoc;
        
        // Public properties to access the document objects
        public Document Document { get { return _doc; } }
        public UIDocument UIDocument { get { return _uidoc; } }

        public DraftingController(Document doc, UIDocument uidoc)
        {
            _doc = doc;
            _uidoc = uidoc;
        }

        public void SetUIDocument(UIDocument uidoc)
        {
            _uidoc = uidoc;
            _doc = uidoc.Document;
        }

        public void UpdateDetailItems()
        {
            FilteredElementCollector collector = new FilteredElementCollector(_doc)
                .OfClass(typeof(FilledRegion));

            using (Transaction transaction = new Transaction(_doc, "Update Detail Items"))
            {
                try
                {
                    transaction.Start();

                    foreach (FilledRegion filledRegion in collector)
                    {
                        Parameter areaParam = filledRegion.LookupParameter("Area");
                        Parameter spAreaParam = filledRegion.LookupParameter("sp.Area");

                        if (areaParam != null && spAreaParam != null)
                        {
                            double areaValue = areaParam.AsDouble();
                            spAreaParam.Set(areaValue);
                        }
                    }

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.RollBack();
                    Autodesk.Revit.UI.TaskDialog.Show("Error", $"An error occurred while updating detail items: {ex.Message}");
                }
            }
        }

        public void RenumberWindows()
        {
            int startNumber = PromptForStartNumber();
            if (startNumber == -1) return;

            // Check per-user settings before opening 3D views
            var userSettings = Miller_Craft_Tools.Model.UserSettings.Load();
            if (userSettings.Open3DViewsForRenumbering)
            {
                try
                {
                    CreateMultiple3DViews();
                    Autodesk.Revit.UI.TaskDialog.Show(
                        "Tile Views",
                        "Tip: Press WT (Window > Tile) in Revit to tile all open views for easier selection."
                    );
                }
                catch (Exception ex)
                {
                    Autodesk.Revit.UI.TaskDialog.Show("3D View Error", $"Failed to open 3D views: {ex.Message}");
                }
            }

            RenumberWindows(startNumber);
        }

        private void RenumberWindows(int startNumber)
        {
            int currentNumber = startNumber;
            Selection sel = _uidoc.Selection;
            WindowSelectionFilter filter = new WindowSelectionFilter(_doc);

            while (true)
            {
                try
                {
                    Reference pickedRef = sel.PickObject(ObjectType.Element, filter, "Select a windoww to renumber or press ESC to finish.");
                    if (pickedRef == null)
                        continue;

                    Element window = _doc.GetElement(pickedRef);

                    using (Transaction transaction = new Transaction(_doc, "Renumber Window Mark"))
                    {
                        transaction.Start();

                        string markToAssign = currentNumber.ToString();
                        if (IsMarkUsed(markToAssign, window.Id))
                        {
                            AssignNewMark(markToAssign);
                        }

                        window.get_Parameter(BuiltInParameter.ALL_MODEL_MARK).Set(markToAssign);

                        transaction.Commit();
                    }

                    currentNumber++;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private bool IsMarkUsed(string mark, ElementId excludeElementId)
        {
            FilteredElementCollector collector = new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_Windows)
                .WhereElementIsNotElementType();

            foreach (Element element in collector)
            {
                if (element.Id == excludeElementId) continue;

                Parameter markParam = element.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
                if (markParam != null && markParam.AsString() == mark)
                {
                    return true;
                }
            }
            return false;
        }

        private void AssignNewMark(string currentMark)
        {
            int number = 1;
            while (true)
            {
                string newMark = currentMark + "-" + number;
                if (!IsMarkUsed(newMark, ElementId.InvalidElementId))
                {
                    foreach (Element element in new FilteredElementCollector(_doc)
                        .OfCategory(BuiltInCategory.OST_Windows)
                        .WhereElementIsNotElementType())
                    {
                        Parameter markParam = element.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
                        if (markParam != null && markParam.AsString() == currentMark)
                        {
                            markParam.Set(newMark);
                            return;
                        }
                    }
                }
                number++;
            }
        }

        public void RenumberViewsOnSheet()
        {
            Autodesk.Revit.DB.View activeView = _doc.ActiveView;
            ViewSheet selectedSheet;

            if (activeView is ViewSheet sheet)
            {
                selectedSheet = sheet;
            }
            else
            {
                // Use the public SheetFilter class instead of the internal one
                Reference sheetReference = _uidoc.Selection.PickObject(
                    ObjectType.Element,
                    new Miller_Craft_Tools.Command.SheetFilter(),
                    "Select a sheet"
                );
                if (sheetReference == null) return;

                selectedSheet = _doc.GetElement(sheetReference) as ViewSheet;
                if (selectedSheet == null) return;
            }

            List<ElementId> viewportIds = new FilteredElementCollector(_doc, selectedSheet.Id)
                .OfCategory(BuiltInCategory.OST_Viewports)
                .ToElementIds()
                .ToList();

            if (viewportIds.Count == 0)
            {
                Autodesk.Revit.UI.TaskDialog.Show("Error", "No views found on this sheet.");
                return;
            }

            int startNumber = PromptForStartNumber();
            if (startNumber == -1)
            {
                Autodesk.Revit.UI.TaskDialog.Show("Error", "Invalid number entered.");
                return;
            }

            int currentNumber = startNumber;
            List<ElementId> selectedViewports = new List<ElementId>();

            while (true)
            {
                try
                {
                    // Use the public ViewportFilter class instead of the internal one
                    Reference selectedReference = _uidoc.Selection.PickObject(
                        ObjectType.Element,
                        new Miller_Craft_Tools.Command.ViewportFilter(viewportIds),
                        $"Select view {currentNumber} or press ESC to finish"
                    );
                    if (selectedReference == null) break;

                    ElementId selectedViewId = selectedReference.ElementId;
                    if (!selectedViewports.Contains(selectedViewId))
                    {
                        selectedViewports.Add(selectedViewId);

                        using (Transaction transaction = new Transaction(_doc, "Renumber Viewport Detail"))
                        {
                            transaction.Start();

                            if (IsDetailNumberUsed(selectedSheet.Id, currentNumber.ToString()))
                            {
                                ShiftFollowingDetailNumbers(selectedSheet.Id, currentNumber);
                            }

                            Viewport viewport = _doc.GetElement(selectedViewId) as Viewport;
                            if (viewport != null)
                            {
                                viewport.get_Parameter(BuiltInParameter.VIEWPORT_DETAIL_NUMBER).Set(currentNumber.ToString());
                            }

                            transaction.Commit();
                        }

                        currentNumber++;
                    }
                    else
                    {
                        Autodesk.Revit.UI.TaskDialog.Show("Error", "This viewport has already been selected.");
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private bool IsDetailNumberUsed(ElementId sheetId, string detailNumber)
        {
            FilteredElementCollector collector = new FilteredElementCollector(_doc, sheetId)
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

        private void ShiftFollowingDetailNumbers(ElementId sheetId, int startNumber)
        {
            FilteredElementCollector collector = new FilteredElementCollector(_doc, sheetId)
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

        private int PromptForStartNumber()
        {
            // Simplify to avoid UI thread blocking issues - use Revit's native prompt
            try 
            {
                // Create a simple input form that works on the Revit UI thread
                using (var form = new System.Windows.Forms.Form())
                {
                    form.TopMost = true;
                    form.Width = 350;
                    form.Height = 150;
                    form.Text = "Window Renumbering";
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
            catch (Exception ex)
            {
                // Fall back to a simpler method if the form approach fails
                Autodesk.Revit.UI.TaskDialog.Show("Error", $"Could not show input dialog: {ex.Message}. Using fallback method.");
                
                // As a last resort, try the Interaction.InputBox but with proper error handling
                try
                {
                    string input = Microsoft.VisualBasic.Interaction.InputBox("Enter starting number:", "Starting Number", "1");
                    return string.IsNullOrEmpty(input) ? -1 : (int.TryParse(input, out int startNumber) ? startNumber : -1);
                }
                catch
                {
                    return 1; // Provide a default if all else fails
                }
            }
        }

        /// <summary>
        /// Creates multiple 3D views from different isometric angles for better window selection
        /// </summary>
        public void CreateMultiple3DViews()
        {
            var directions = new List<XYZ>
            {
                new XYZ(1, 1, 1),    // NE Isometric
                new XYZ(-1, 1, 1),   // NW Isometric
                new XYZ(-1, -1, 1),  // SW Isometric
                new XYZ(1, -1, 1)    // SE Isometric
            };

            var default3DView = new FilteredElementCollector(_doc)
                .OfClass(typeof(View3D))
                .Cast<View3D>()
                .FirstOrDefault(v => !v.IsTemplate && v.Name.Equals("{3D}", StringComparison.OrdinalIgnoreCase));

            if (default3DView == null)
                throw new InvalidOperationException("Default 3D view '{3D}' not found.");

            List<View3D> createdViews = new List<View3D>();
            using (Transaction tx = new Transaction(_doc, "Create Isometric 3D Views for Window Renumbering"))
            {
                tx.Start();
                foreach (var dir in directions)
                {
                    View3D new3DView = _doc.GetElement(default3DView.Duplicate(ViewDuplicateOption.Duplicate)) as View3D;
                    if (new3DView == null)
                        throw new InvalidOperationException("Failed to duplicate 3D view.");

                    ViewOrientation3D orientation = new ViewOrientation3D(
                        new XYZ(0, 0, 0),
                        dir,
                        XYZ.BasisZ
                    );
                    new3DView.SetOrientation(orientation);

                    string dirName = $"Isometric ({dir.X:+#;-#;0},{dir.Y:+#;-#;0},{dir.Z:+#;-#;0})";
                    new3DView.Name = $"Window Renumber - {dirName} - {DateTime.Now:HHmmss}";
                    createdViews.Add(new3DView);
                }
                tx.Commit();
            }

            foreach (var v in createdViews)
            {
                try
                {
                    _uidoc.ActiveView = v;
                }
                catch (Exception ex)
                {
                    Autodesk.Revit.UI.TaskDialog.Show("View Activation Error", $"Could not activate 3D view '{v.Name}': {ex.Message}");
                }
            }

            Autodesk.Revit.UI.TaskDialog.Show(
                "3D Views Opened",
                "Multiple 3D views from different isometric angles have been created and opened. You can now use them to select windows for renumbering more easily."
            );
        }
    }

        // Inner selection filter classes for DraftingController
        internal class WindowSelectionFilter : ISelectionFilter
        {
            private readonly Document _doc;

            public WindowSelectionFilter(Document document)
            {
                _doc = document;
            }

            public bool AllowElement(Element elem)
            {
                // Check if the element is a window
                return elem != null && elem.Category != null && elem.Category.Id.Equals(new ElementId(BuiltInCategory.OST_Windows));
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                // Get the element from the reference
                if (reference == null || _doc == null) return false;
                
                try
                {
                    Element elem = _doc.GetElement(reference);
                    // Check if it's a window
                    return AllowElement(elem);
                }
                catch
                {
                    // If we can't get the element for some reason, don't allow the reference
                    return false;
                }
            }
        }

        internal class SheetSelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem)
            {
                return elem is ViewSheet;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return true;
            }
        }

        internal class ViewportSelectionFilter : ISelectionFilter
        {
            private readonly List<ElementId> _validViewportIds;

            public ViewportSelectionFilter(List<ElementId> viewportIds)
            {
                _validViewportIds = viewportIds ?? new List<ElementId>();
            }

            public bool AllowElement(Element elem)
            {
                if (elem == null || _validViewportIds == null) return false;
                return _validViewportIds.Contains(elem.Id);
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return true;
            }
        }
}