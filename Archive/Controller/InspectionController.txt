using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Miller_Craft_Tools.Model;
using Miller_Craft_Tools.ViewModel;
using Miller_Craft_Tools.Views;
using Microsoft.Win32;
using Autodesk.Revit.UI.Selection;

namespace Miller_Craft_Tools.Controller
{
    public class InspectionController
    {
        private Document _doc;
        private UIDocument _uidoc;

        public InspectionController(Document doc, UIDocument uidoc)
        {
            _doc = doc;
            _uidoc = uidoc;
        }

        public void SetUIDocument(UIDocument uidoc)
        {
            _uidoc = uidoc;
            _doc = uidoc.Document;
        }

        public void GroupElementsByLevel(MainView view)
        {
            view.HideDialog();
            try
            {
                List<LevelNode> levelNodes = GetElementsGroupedBySelectedLevels();
                DisplayGroupedElements(levelNodes);
            }
            finally
            {
                view.ShowDialogAgain();
            }
        }

        private List<LevelNode> GetElementsGroupedBySelectedLevels()
        {
            Selection selection = _uidoc.Selection;
            IList<Reference> selectedRefs = selection.PickObjects(
                ObjectType.Element,
                new LevelSelectionFilter(),
                "Please select one or more levels"
            );

            List<Level> selectedLevels = selectedRefs.Select(r => _doc.GetElement(r) as Level).Where(l => l != null).ToList();

            if (selectedLevels.Count == 0)
            {
                TaskDialog.Show("Error", "No levels were selected.");
                return new List<LevelNode>();
            }

            List<LevelNode> levelNodes = new List<LevelNode>();

            foreach (Level level in selectedLevels)
            {
                ICollection<ElementId> dependentElementIds = level.GetDependentElements(null);
                List<Element> elementsForLevel = dependentElementIds
                    .Select(id => _doc.GetElement(id))
                    .Where(e => e != null)
                    .Where(e =>
                    {
                        if (e.Category == null) return true;
                        return e.Category.Id.IntegerValue != (int)BuiltInCategory.OST_Sun &&
                               e.Category.Id.IntegerValue != (int)BuiltInCategory.OST_Constraints;
                    })
                    .ToList();

                LevelNode levelNode = new LevelNode
                {
                    LevelName = $"Level: {level.Name} - {elementsForLevel.Count} elements",
                    ElementCount = elementsForLevel.Count
                };

                var elementsByCategory = elementsForLevel
                    .GroupBy(e =>
                    {
                        if (e.Category == null) return "Unknown Category";
                        int categoryId = e.Category.Id.IntegerValue;
                        if (categoryId == (int)BuiltInCategory.OST_DetailComponents) return "Detail Items";
                        return e.Category.Name;
                    })
                    .OrderBy(g => g.Key);

                foreach (var categoryGroup in elementsByCategory)
                {
                    CategoryNode categoryNode = new CategoryNode
                    {
                        CategoryName = $"{categoryGroup.Key}: {categoryGroup.Count()}",
                        ElementCount = categoryGroup.Count()
                    };

                    foreach (var element in categoryGroup.OrderBy(e => e.Name))
                    {
                        string name = element.Name ?? "Unnamed";
                        categoryNode.Elements.Add(new Miller_Craft_Tools.ViewModel.ElementNode
                        {
                            ElementName = $"{name} (ID: {element.Id})",
                            ElementId = element.Id.ToString()
                        });
                    }

                    levelNode.Categories.Add(categoryNode);
                }

                if (levelNode.ElementCount > 0)
                {
                    levelNodes.Add(levelNode);
                }
            }

            return levelNodes.OrderBy(node => node.LevelName).ToList();
        }

        private void DisplayGroupedElements(List<LevelNode> levelNodes)
        {
            if (levelNodes.Count == 0)
            {
                TaskDialog.Show("Results", "No elements found for the selected levels.");
                return;
            }

            ResultsView resultsView = new ResultsView(_uidoc, levelNodes);
            resultsView.ShowDialog();
        }

        public void ExportStandards(MainView view)
        {
            view.HideDialog();
            try
            {
                ProjectStandards standards = CollectProjectStandards();

                // Populate IdentityInformation
                standards.IdentityInformation = new IdentityInformation
                {
                    FilePath = _doc.PathName,
                    FileName = Path.GetFileName(_doc.PathName),
                    ExportDate = DateTime.Now.ToString("yyyy-MM-dd"),
                    ExportTime = DateTime.Now.ToString("HH:mm:ss")
                };

                // Use SaveFileDialog to let the user choose the file path
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Title = "Export Project Standards",
                    Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                    DefaultExt = "json",
                    FileName = "standards.json",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                };

                bool? result = saveFileDialog.ShowDialog();
                if (result != true)
                {
                    // User canceled the dialog
                    TaskDialog.Show("Export Canceled", "Export was canceled by the user.");
                    return;
                }

                string filePath = saveFileDialog.FileName;
                string jsonString = JsonSerializer.Serialize(standards, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, jsonString);

                TaskDialog.Show("Success", $"Standards exported to {filePath}");
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Failed to export standards: {ex.Message}");
            }
            finally
            {
                view.ShowDialogAgain();
            }
        }

        private ProjectStandards CollectProjectStandards()
        {
            ProjectStandards standards = new ProjectStandards();

            // Collect families, grouped by category
            FilteredElementCollector familyCollector = new FilteredElementCollector(_doc)
                .OfClass(typeof(Family));
            foreach (Family family in familyCollector.Cast<Family>())
            {
                string category = family.FamilyCategory?.Name ?? "Unknown Category";
                if (!standards.Families.ContainsKey(category))
                {
                    standards.Families[category] = new List<FamilyStandard>();
                }

                FamilyStandard familyStandard = new FamilyStandard
                {
                    Name = family.Name
                };

                foreach (ElementId symbolId in family.GetFamilySymbolIds())
                {
                    FamilySymbol symbol = _doc.GetElement(symbolId) as FamilySymbol;
                    if (symbol != null)
                    {
                        FamilyTypeStandard typeStandard = new FamilyTypeStandard
                        {
                            Name = symbol.Name
                        };

                        foreach (Parameter param in symbol.Parameters)
                        {
                            if (param.Definition.Name == "Width" || param.Definition.Name == "Height")
                            {
                                string value = param.AsValueString() ?? param.AsString() ?? "N/A";
                                typeStandard.Parameters.Add(new ParameterStandard
                                {
                                    Name = param.Definition.Name,
                                    Value = value
                                });
                            }
                        }

                        familyStandard.Types.Add(typeStandard);
                    }
                }

                standards.Families[category].Add(familyStandard);
            }

            // Collect Object Styles (split into Model and Annotation)
            Categories categories = _doc.Settings.Categories;
            foreach (Category category in categories)
            {
                if (category.CategoryType == CategoryType.Model)
                {
                    CollectObjectStyle(category, standards.ModelObjectStyles);
                }
                else if (category.CategoryType == CategoryType.Annotation)
                {
                    CollectObjectStyle(category, standards.AnnotationObjectStyles);
                }
                // Skip AnalyticalModel categories for now
            }

            // Collect fill styles (fill patterns)
            FilteredElementCollector fillPatternCollector = new FilteredElementCollector(_doc)
                .OfClass(typeof(FillPatternElement));
            foreach (FillPatternElement fillPattern in fillPatternCollector.Cast<FillPatternElement>())
            {
                FillStyleStandard fillStyle = new FillStyleStandard
                {
                    Name = fillPattern.Name,
                    ForegroundPattern = fillPattern.GetFillPattern().Name,
                    BackgroundPattern = "None", // Revit API doesn't directly expose background patterns; we can expand this later
                    Color = "N/A" // We'll need to associate fill patterns with materials or regions to get colors
                };
                standards.FillStyles.Add(fillStyle);
            }

            // List of known built-in project parameter names to exclude
            HashSet<string> builtInParameterNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Occupant",
                "Building Height",
                "Flux Id",
                // Add other known built-in parameters as needed
                "Energy Analysis Space Type", // Common in energy analysis
                "Energy Analysis Building Type",
                "Space Type", // Common for Spaces
                "Number of People" // Common for Rooms
            };

            // Collect project parameters (filter and group by shared/non-shared)
            BindingMap bindings = _doc.ParameterBindings;
            DefinitionBindingMapIterator iterator = bindings.ForwardIterator();
            while (iterator.MoveNext())
            {
                Definition definition = iterator.Key;
                ElementBinding binding = iterator.Current as ElementBinding;

                if (definition != null && binding != null)
                {
                    // Skip known built-in parameters by name
                    if (builtInParameterNames.Contains(definition.Name))
                    {
                        continue;
                    }

                    // Additional heuristic: If it's an InternalDefinition and not following a user-defined naming convention (e.g., "sp."), consider excluding
                    // This can be refined based on your naming conventions
                    if (definition is InternalDefinition && !definition.Name.StartsWith("sp.", StringComparison.OrdinalIgnoreCase))
                    {
                        // Optionally, exclude InternalDefinitions in certain groups unless they match your naming convention
                        string groupName = GetParameterGroupDisplayName(definition.GetGroupTypeId());
                        if (groupName == "Identity Data" && binding.Categories.Contains(_doc.Settings.Categories.get_Item(BuiltInCategory.OST_Rooms)))
                        {
                            continue; // Exclude parameters like "Occupant" in "Identity Data" for Rooms
                        }
                    }

                    ProjectParameterStandard paramStandard = new ProjectParameterStandard
                    {
                        Name = definition.Name,
                        Type = LabelUtils.GetLabelForSpec(definition.GetDataType()), // Use LabelUtils for localized type name
                        Group = GetParameterGroupDisplayName(definition.GetGroupTypeId()),
                        IsInstance = binding is InstanceBinding,
                        IsShared = definition is ExternalDefinition // Shared if it's an ExternalDefinition
                    };

                    foreach (Category category in binding.Categories)
                    {
                        paramStandard.Categories.Add(category.Name);
                    }

                    // Add to the appropriate list based on IsShared
                    if (paramStandard.IsShared)
                    {
                        standards.SharedProjectParameters.Add(paramStandard);
                    }
                    else
                    {
                        standards.NonSharedProjectParameters.Add(paramStandard);
                    }
                }
            }

            // Collect line patterns
            FilteredElementCollector linePatternCollector = new FilteredElementCollector(_doc)
                .OfClass(typeof(LinePatternElement));
            foreach (LinePatternElement linePattern in linePatternCollector.Cast<LinePatternElement>())
            {
                LinePatternStandard patternStandard = new LinePatternStandard
                {
                    Name = linePattern.Name
                };
                standards.LinePatterns.Add(patternStandard);
            }

            // Collect line styles
            Category linesCategory = _doc.Settings.Categories.get_Item(BuiltInCategory.OST_Lines);
            if (linesCategory != null)
            {
                foreach (Category lineStyle in linesCategory.SubCategories)
                {
                    LineStyleStandard styleStandard = new LineStyleStandard
                    {
                        Name = lineStyle.Name,
                        LineWeight = lineStyle.GetLineWeight(GraphicsStyleType.Projection),
                        LineColor = lineStyle.LineColor != null ? $"RGB {lineStyle.LineColor.Red}-{lineStyle.LineColor.Green}-{lineStyle.LineColor.Blue}" : "Black",
                        LinePattern = lineStyle.GetGraphicsStyle(GraphicsStyleType.Projection)?.GraphicsStyleCategory.Name ?? "Solid"
                    };
                    standards.LineStyles.Add(styleStandard);
                }
            }

            return standards;
        }

        private string GetParameterGroupDisplayName(ForgeTypeId group)
        {
            // Use LabelUtils to get the localized display name of the parameter group
            return LabelUtils.GetLabelForGroup(group);
        }

        private void CollectObjectStyle(Category category, List<ObjectStyle> objectStyles)
        {
            // Skip categories that don't support line weights and have no sub-categories
            if (category.GetLineWeight(GraphicsStyleType.Projection) == null && category.SubCategories.Size == 0)
            {
                return;
            }

            ObjectStyle style = new ObjectStyle
            {
                Category = category.Name,
                ProjectionLineWeight = category.GetLineWeight(GraphicsStyleType.Projection),
                CutLineWeight = category.CategoryType == CategoryType.Model ? category.GetLineWeight(GraphicsStyleType.Cut) : null,
                LineColor = category.LineColor != null ? $"RGB {category.LineColor.Red}-{category.LineColor.Green}-{category.LineColor.Blue}" : "Black",
                LinePattern = category.GetGraphicsStyle(GraphicsStyleType.Projection)?.GraphicsStyleCategory.Name ?? "Solid",
                Material = category.Material?.Name ?? "None"
            };

            // Collect sub-categories (only for Model Categories)
            if (category.CategoryType == CategoryType.Model)
            {
                foreach (Category subCategory in category.SubCategories)
                {
                    CollectObjectStyle(subCategory, style.SubCategories);
                }
            }

            objectStyles.Add(style);
        }

        internal class LevelSelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem)
            {
                return elem is Level;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return true;
            }
        }
    }
}