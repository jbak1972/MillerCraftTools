﻿using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Miller_Craft_Tools.Model;
using Miller_Craft_Tools.ViewModel;
using Miller_Craft_Tools.Views;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Miller_Craft_Tools.Controller
{
    public class SheetUtilitiesController
    {
        private Document _doc;
        private UIDocument _uidoc;

        public SheetUtilitiesController(Document doc, UIDocument uidoc)
        {
            _doc = doc;
            _uidoc = uidoc;
        }

        public void SetUIDocument(UIDocument uidoc)
        {
            _uidoc = uidoc;
            _doc = uidoc.Document;
        }

        public void CopyToSheets(MainView view)
        {
            view.HideDialog();
            try
            {
                // Show the dialog to select what to copy
                CopyToSheetsView dialog = new CopyToSheetsView();
                CopyToSheetsViewModel dialogViewModel = new CopyToSheetsViewModel(dialog);
                dialog.DataContext = dialogViewModel;

                bool? dialogResult = dialog.ShowDialog();
                if (dialogResult != true)
                {
                    TaskDialog.Show("Canceled", "Operation canceled by the user.");
                    return;
                }

                // Validate that the active view is a sheet
                if (!(_uidoc.ActiveView is ViewSheet sourceSheet))
                {
                    TaskDialog.Show("Error", "The active view must be a sheet view.");
                    return;
                }

                // Collect elements to copy based on user selection
                RevisionCloud revisionCloud = null;
                ElementId tagTypeId = null;
                XYZ tagPosition = null;
                Viewport legendViewport = null;
                XYZ legendPosition = null;
                ElementId legendViewId = null;

                if (dialogViewModel.CopyRevision)
                {
                    try
                    {
                        IList<Reference> selectedRefs = _uidoc.Selection.PickObjects(
                            ObjectType.Element,
                            "Select a revision cloud and its associated tag."
                        );

                        if (selectedRefs.Count != 2)
                        {
                            TaskDialog.Show("Error", "Please select exactly one revision cloud and one revision tag.");
                            return;
                        }

                        foreach (Reference reference in selectedRefs)
                        {
                            Element element = _doc.GetElement(reference);
                            if (element is RevisionCloud cloud)
                            {
                                if (revisionCloud != null)
                                {
                                    TaskDialog.Show("Error", "Only one revision cloud should be selected.");
                                    return;
                                }
                                revisionCloud = cloud;
                            }
                            else if (element is IndependentTag tag)
                            {
                                if (tagPosition != null)
                                {
                                    TaskDialog.Show("Error", "Only one revision tag should be selected.");
                                    return;
                                }
                                tagTypeId = tag.GetTypeId();
                                tagPosition = tag.TagHeadPosition;
                            }
                        }

                        if (revisionCloud == null || tagPosition == null || tagTypeId == null)
                        {
                            TaskDialog.Show("Error", "Please select exactly one revision cloud and one revision tag.");
                            return;
                        }
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                        TaskDialog.Show("Canceled", "Selection canceled by the user.");
                        return;
                    }
                }

                if (dialogViewModel.CopyLegend)
                {
                    try
                    {
                        Reference viewportRef = _uidoc.Selection.PickObject(ObjectType.Element, "Select a legend to copy.");
                        legendViewport = _doc.GetElement(viewportRef) as Viewport;
                        if (legendViewport == null)
                        {
                            TaskDialog.Show("Error", "Selected element is not a viewport.");
                            return;
                        }

                        View legendView = _doc.GetElement(legendViewport.ViewId) as View;
                        if (legendView == null || legendView.ViewType != ViewType.Legend)
                        {
                            TaskDialog.Show("Error", "Selected viewport does not reference a legend view.");
                            return;
                        }

                        legendPosition = legendViewport.GetBoxCenter();
                        legendViewId = legendViewport.ViewId;
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                        TaskDialog.Show("Canceled", "Selection canceled by the user.");
                        return;
                    }
                }

                // Collect all sheets except the source sheet
                FilteredElementCollector sheetCollector = new FilteredElementCollector(_doc)
                    .OfClass(typeof(ViewSheet));
                List<ViewSheet> targetSheets = sheetCollector
                    .Cast<ViewSheet>()
                    .Where(s => s.Id != sourceSheet.Id)
                    .ToList();

                if (targetSheets.Count == 0)
                {
                    TaskDialog.Show("Warning", "No other sheets found in the project.");
                    return;
                }

                // Copy revision clouds and tags
                if (dialogViewModel.CopyRevision && revisionCloud != null && tagTypeId != null && tagPosition != null)
                {
                    using (Transaction tx = new Transaction(_doc, "Copy Revision Clouds"))
                    {
                        tx.Start();
                        foreach (ViewSheet targetSheet in targetSheets)
                        {
                            ElementId[] elementsToCopy = new ElementId[] { revisionCloud.Id };
                            IList<ElementId> copiedElementIds = ElementTransformUtils.CopyElements(
                                sourceSheet,
                                elementsToCopy,
                                targetSheet,
                                Transform.Identity,
                                new CopyPasteOptions()
                            ).ToList();

                            ElementId copiedRevisionCloudId = copiedElementIds.FirstOrDefault();
                            if (copiedRevisionCloudId == null)
                            {
                                TaskDialog.Show("Error", "Failed to copy the revision cloud to one of the sheets.");
                                continue;
                            }

                            RevisionCloud copiedRevisionCloud = _doc.GetElement(copiedRevisionCloudId) as RevisionCloud;
                            if (copiedRevisionCloud == null)
                            {
                                TaskDialog.Show("Error", "Failed to retrieve the copied revision cloud.");
                                continue;
                            }

                            Reference cloudReference = new Reference(copiedRevisionCloud);

                            IndependentTag newTag = IndependentTag.Create(
                                _doc,
                                tagTypeId,
                                targetSheet.Id,
                                cloudReference,
                                false,
                                TagOrientation.Horizontal,
                                tagPosition
                            );
                        }
                        tx.Commit();
                    }
                }

                // Copy legends
                if (dialogViewModel.CopyLegend && legendViewport != null && legendPosition != null && legendViewId != null)
                {
                    using (Transaction tx = new Transaction(_doc, "Copy Legends"))
                    {
                        tx.Start();
                        foreach (ViewSheet targetSheet in targetSheets)
                        {
                            Viewport.Create(_doc, targetSheet.Id, legendViewId, legendPosition);
                        }
                        tx.Commit();
                    }
                }

                TaskDialog.Show("Success", $"Copied to {targetSheets.Count} sheets.");
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Failed to copy elements: {ex.Message}");
            }
            finally
            {
                view.ShowDialogAgain();
            }
        }
    }
}