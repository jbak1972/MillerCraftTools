using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic;

namespace Miller_Craft_Tools.Controller
{
    public class DraftingController
    {
        private Document _doc;
        private UIDocument _uidoc;

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
                    TaskDialog.Show("Error", $"An error occurred while updating detail items: {ex.Message}");
                }
            }
        }

        public void RenumberWindows()
        {
            int startNumber = PromptForStartNumber();
            if (startNumber == -1) return;

            RenumberWindows(startNumber);
        }

        private void RenumberWindows(int startNumber)
        {
            int currentNumber = startNumber;

            while (true)
            {
                try
                {
                    Reference selectedReference = _uidoc.Selection.PickObject(
                        ObjectType.Element,
                        new WindowSelectionFilter(),
                        "Select a window to renumber or press ESC to finish."
                    );
                    if (selectedReference == null) break;

                    Element selectedElement = _doc.GetElement(selectedReference);

                    using (Transaction transaction = new Transaction(_doc, "Renumber Window Mark"))
                    {
                        transaction.Start();

                        string markToAssign = currentNumber.ToString();
                        if (IsMarkUsed(markToAssign, selectedElement.Id))
                        {
                            AssignNewMark(markToAssign);
                        }

                        selectedElement.get_Parameter(BuiltInParameter.ALL_MODEL_MARK).Set(markToAssign);

                        transaction.Commit();
                    }

                    currentNumber++;
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
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
            View activeView = _doc.ActiveView;
            ViewSheet selectedSheet;

            if (activeView is ViewSheet sheet)
            {
                selectedSheet = sheet;
            }
            else
            {
                Reference sheetReference = _uidoc.Selection.PickObject(
                    ObjectType.Element,
                    new SheetSelectionFilter(),
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
                TaskDialog.Show("Error", "No views found on this sheet.");
                return;
            }

            int startNumber = PromptForStartNumber();
            if (startNumber == -1)
            {
                TaskDialog.Show("Error", "Invalid number entered.");
                return;
            }

            int currentNumber = startNumber;
            List<ElementId> selectedViewports = new List<ElementId>();

            while (true)
            {
                try
                {
                    Reference selectedReference = _uidoc.Selection.PickObject(
                        ObjectType.Element,
                        new ViewportSelectionFilter(viewportIds),
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
                        TaskDialog.Show("Error", "This viewport has already been selected.");
                    }
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
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
            string input = Interaction.InputBox("Enter starting number:", "Starting Number", "1");
            return int.TryParse(input, out int startNumber) ? startNumber : -1;
        }
    }

    internal class WindowSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem.Category?.Id.IntegerValue == (int)BuiltInCategory.OST_Windows;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
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
            _validViewportIds = viewportIds;
        }

        public bool AllowElement(Element elem)
        {
            return _validViewportIds.Contains(elem.Id);
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }
}