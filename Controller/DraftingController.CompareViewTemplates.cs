using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Miller_Craft_Tools.Controller
{
    public partial class DraftingController
    {
        // Constants for view template comparison
        private const string COMPARISON_REPORT_TITLE = "View Template Comparison Report";
        
        /// <summary>
        /// Compares two view templates and creates a drafting view with the comparison results
        /// </summary>
        public void CompareViewTemplates()
        {
            // Get all view templates in the document
            var viewTemplateCollector = new FilteredElementCollector(_doc)
                .OfClass(typeof(Autodesk.Revit.DB.View))
                .Cast<Autodesk.Revit.DB.View>()
                .Where(v => v.IsTemplate)
                .ToList();

            if (viewTemplateCollector.Count < 2)
            {
                Autodesk.Revit.UI.TaskDialog.Show("Error", "You need at least two view templates to compare.");
                return;
            }

            // Prompt user to select the first view template
            Autodesk.Revit.DB.View firstTemplate = PromptForViewTemplateSelection(viewTemplateCollector, "Select first view template");
            if (firstTemplate == null) return;

            // Prompt user to select the second view template
            Autodesk.Revit.DB.View secondTemplate = PromptForViewTemplateSelection(
                viewTemplateCollector.Where(v => v.Id != firstTemplate.Id).ToList(),
                "Select second view template"
            );
            if (secondTemplate == null) return;

            // Create a comparison report
            using (Transaction transaction = new Transaction(_doc, "Create View Template Comparison Report"))
            {
                transaction.Start();
                
                try
                {
                    // Create a new drafting view
                    ViewFamilyType draftingViewType = new FilteredElementCollector(_doc)
                        .OfClass(typeof(ViewFamilyType))
                        .Cast<ViewFamilyType>()
                        .FirstOrDefault(v => v.ViewFamily == ViewFamily.Drafting);
                    
                    if (draftingViewType == null)
                    {
                        Autodesk.Revit.UI.TaskDialog.Show("Error", "Could not find drafting view type.");
                        transaction.RollBack();
                        return;
                    }
                    
                    // Create the drafting view
                    ViewDrafting draftingView = ViewDrafting.Create(_doc, draftingViewType.Id);
                    draftingView.Name = $"{COMPARISON_REPORT_TITLE} - {firstTemplate.Name} vs {secondTemplate.Name}";
                    
                    // Generate and add the comparison report to the drafting view
                    CreateComparisonReport(draftingView, firstTemplate, secondTemplate);
                    
                    transaction.Commit();
                    
                    // Activate the newly created drafting view
                    _uidoc.ActiveView = draftingView;
                    
                    Autodesk.Revit.UI.TaskDialog.Show("Success", 
                        $"View template comparison report created successfully.\n\n" +
                        $"Comparing: {firstTemplate.Name} and {secondTemplate.Name}");
                }
                catch (Exception ex)
                {
                    transaction.RollBack();
                    Autodesk.Revit.UI.TaskDialog.Show("Error", $"Failed to create comparison report: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Creates a comparison report between two view templates in a drafting view
        /// </summary>
        private void CreateComparisonReport(ViewDrafting draftingView, Autodesk.Revit.DB.View firstTemplate, Autodesk.Revit.DB.View secondTemplate)
        {
            // Get all parameter groups to organize our report
            var parameterGroups = GetAllParametersInViewTemplates(firstTemplate, secondTemplate)
                .GroupBy(p => GetParameterGroupName(p))
                .OrderBy(g => g.Key);
            
            // Set up text dimensions
            double startX = 0.1;      // Starting X position (in feet)
            double startY = 0.8;      // Starting Y position (in feet)
            double lineHeight = 0.02; // Height between lines (in feet)
            double currentY = startY;
            double indentSize = 0.04;  // Indent size for parameter names
            
            // Add the header
            AddTextNote(draftingView, "VIEW TEMPLATE COMPARISON REPORT", startX, currentY, true);
            currentY -= lineHeight * 1.5;
            AddTextNote(draftingView, $"Template 1: {firstTemplate.Name}", startX, currentY);
            currentY -= lineHeight;
            AddTextNote(draftingView, $"Template 2: {secondTemplate.Name}", startX, currentY);
            currentY -= lineHeight * 2;
            
            // Add a legend
            AddTextNote(draftingView, "Legend:", startX, currentY, true);
            currentY -= lineHeight;
            AddTextNote(draftingView, "- Parameters marked with (*) have different values", startX + indentSize, currentY);
            currentY -= lineHeight * 2;
            
            // Process each parameter group
            foreach (var group in parameterGroups)
            {
                // Add the group name
                AddTextNote(draftingView, $"{group.Key}:", startX, currentY, true);
                currentY -= lineHeight;
                
                // Process each parameter in the group
                foreach (var parameter in group.OrderBy(p => p.Definition.Name))
                {
                    string paramName = parameter.Definition.Name;
                    string template1Value = GetParameterValueAsString(firstTemplate.GetParameters(paramName).FirstOrDefault());
                    string template2Value = GetParameterValueAsString(secondTemplate.GetParameters(paramName).FirstOrDefault());
                    
                    bool isDifferent = template1Value != template2Value;
                    string diffMarker = isDifferent ? "(*)" : "";
                    
                    // Add the parameter name
                    AddTextNote(draftingView, $"{diffMarker} {paramName}:", startX + indentSize, currentY);
                    currentY -= lineHeight;
                    
                    // Add the values
                    AddTextNote(draftingView, $"Template 1: {template1Value}", startX + indentSize * 2, currentY);
                    currentY -= lineHeight;
                    AddTextNote(draftingView, $"Template 2: {template2Value}", startX + indentSize * 2, currentY);
                    currentY -= lineHeight * 1.5;
                    
                    // Check if we need to start a new column or page
                    if (currentY < 0.1) // Near the bottom of the page
                    {
                        // Reset Y and move X position for a new column
                        currentY = startY;
                        startX += 0.8; // Move to the right for a new column
                    }
                }
            }
            
            // Add special section for visibility settings
            AddVisibilityComparison(draftingView, firstTemplate, secondTemplate, startX, currentY);
        }
        
        /// <summary>
        /// Adds visibility settings comparison between two templates
        /// </summary>
        private void AddVisibilityComparison(ViewDrafting draftingView, Autodesk.Revit.DB.View firstTemplate, Autodesk.Revit.DB.View secondTemplate, double startX, double startY)
        {
            // This method focuses on category visibility which is most relevant for troubleshooting visibility issues
            // Get visibility settings for both templates
            Categories categories = draftingView.Document.Settings.Categories;
            double currentY = startY - 0.1; // Add some spacing
            double lineHeight = 0.02;
            double indentSize = 0.04;
            
            // Add header for visibility section
            AddTextNote(draftingView, "CATEGORY VISIBILITY DIFFERENCES:", startX, currentY, true);
            currentY -= lineHeight * 2;
            
            bool foundDifferences = false;
            
            // Compare category visibility
            foreach (Category category in categories)
            {
                if (category.AllowsBoundParameters)
                {
                    // Try to get visibility for each template
                    bool template1Visible = IsCategoryVisible(firstTemplate, category.Id);
                    bool template2Visible = IsCategoryVisible(secondTemplate, category.Id);
                    
                    // If visibility differs, add to the report
                    if (template1Visible != template2Visible)
                    {
                        foundDifferences = true;
                        AddTextNote(draftingView, $"Category: {category.Name}", startX + indentSize, currentY);
                        currentY -= lineHeight;
                        AddTextNote(draftingView, $"Template 1: {(template1Visible ? "Visible" : "Hidden")}", startX + indentSize * 2, currentY);
                        currentY -= lineHeight;
                        AddTextNote(draftingView, $"Template 2: {(template2Visible ? "Visible" : "Hidden")}", startX + indentSize * 2, currentY);
                        currentY -= lineHeight * 1.5;
                        
                        // Check if we need to start a new column
                        if (currentY < 0.1)
                        {
                            // Reset Y and move X position for a new column
                            currentY = startY;
                            startX += 0.8; // Move to the right for a new column
                        }
                    }
                }
            }
            
            if (!foundDifferences)
            {
                AddTextNote(draftingView, "No category visibility differences found.", startX + indentSize, currentY);
            }
        }
        
        /// <summary>
        /// Determines if a category is visible in a view template
        /// </summary>
        private bool IsCategoryVisible(Autodesk.Revit.DB.View viewTemplate, ElementId categoryId)
        {
            try
            {
                // Try to get the category's visibility in the view
                return viewTemplate.GetCategoryHidden(categoryId) == false;
            }
            catch
            {
                // If the category isn't applicable to this view type, we'll assume it's not visible
                return false;
            }
        }
        
        /// <summary>
        /// Gets all parameters from both view templates for comparison
        /// </summary>
        private IEnumerable<Parameter> GetAllParametersInViewTemplates(Autodesk.Revit.DB.View firstTemplate, Autodesk.Revit.DB.View secondTemplate)
        {
            // Combine parameters from both templates, ensuring we don't have duplicates
            var allParameters = new Dictionary<string, Parameter>();
            
            foreach (Parameter param in firstTemplate.Parameters)
            {
                if (!allParameters.ContainsKey(param.Definition.Name))
                {
                    allParameters.Add(param.Definition.Name, param);
                }
            }
            
            foreach (Parameter param in secondTemplate.Parameters)
            {
                if (!allParameters.ContainsKey(param.Definition.Name))
                {
                    allParameters.Add(param.Definition.Name, param);
                }
            }
            
            return allParameters.Values;
        }
        
        /// <summary>
        /// Prompts the user to select a view template from a list
        /// </summary>
        private Autodesk.Revit.DB.View PromptForViewTemplateSelection(IEnumerable<Autodesk.Revit.DB.View> viewTemplates, string title)
        {
            using (var form = new System.Windows.Forms.Form())
            {
                form.Text = title;
                form.Width = 400;
                form.Height = 300;
                form.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
                form.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                
                // Create listbox for templates
                var listBox = new System.Windows.Forms.ListBox();
                listBox.Dock = System.Windows.Forms.DockStyle.Top;
                listBox.Height = 200;
                listBox.DataSource = viewTemplates.Select(v => v.Name).ToList();
                form.Controls.Add(listBox);
                
                // Create button panel
                var panel = new System.Windows.Forms.Panel();
                panel.Dock = System.Windows.Forms.DockStyle.Bottom;
                panel.Height = 50;
                form.Controls.Add(panel);
                
                // Create OK button
                var okButton = new System.Windows.Forms.Button();
                okButton.Text = "OK";
                okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
                okButton.Left = 100;
                okButton.Top = 10;
                okButton.Width = 80;
                panel.Controls.Add(okButton);
                
                // Create Cancel button
                var cancelButton = new System.Windows.Forms.Button();
                cancelButton.Text = "Cancel";
                cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
                cancelButton.Left = 200;
                cancelButton.Top = 10;
                cancelButton.Width = 80;
                panel.Controls.Add(cancelButton);
                
                form.AcceptButton = okButton;
                form.CancelButton = cancelButton;
                
                // Show the dialog and get result
                if (form.ShowDialog() == System.Windows.Forms.DialogResult.OK && listBox.SelectedIndex != -1)
                {
                    string selectedName = listBox.SelectedItem.ToString();
                    return viewTemplates.FirstOrDefault(v => v.Name == selectedName);
                }
                
                return null;
            }
        }
        
        /// <summary>
        /// Gets the parameter value as a formatted string
        /// </summary>
        private string GetParameterValueAsString(Parameter param)
        {
            if (param == null) return "(Not Set)";
            
            switch (param.StorageType)
            {
                case StorageType.Integer:
                    return param.AsInteger().ToString();
                
                case StorageType.Double:
                    return param.AsDouble().ToString("F4");
                
                case StorageType.String:
                    return param.AsString() ?? "(Empty String)";
                
                case StorageType.ElementId:
                    ElementId id = param.AsElementId();
                    if (id == null || id == ElementId.InvalidElementId)
                        return "(None)";
                        
                    Element elem = param.Element.Document.GetElement(id);
                    return elem != null ? elem.Name : id.ToString();
                    
                default:
                    return "(Unsupported Type)";
            }
        }
        
        /// <summary>
        /// Adds a text note to the drafting view
        /// </summary>
        private void AddTextNote(ViewDrafting view, string text, double x, double y, bool isBold = false)
        {
            // Create the position for the text
            XYZ position = new XYZ(x, y, 0);
            
            // Get the default text note type
            ElementId textTypeId = view.Document.GetDefaultElementTypeId(ElementTypeGroup.TextNoteType);
            
            // Create the text note in the view
            TextNote textNote = TextNote.Create(view.Document, view.Id, position, text, textTypeId);
            
            // Apply bold formatting if required (would need custom text style setup in a real implementation)
            if (isBold)
            {
                // In a complete implementation, we'd need to either:
                // 1. Find a bold text type and use that instead of the default
                // or
                // 2. Create a temporary bold text type to use
                // This is simplified for the demo
            }
        }
        
        /// <summary>
        /// Gets a consistent parameter group name for grouping parameters
        /// </summary>
        private string GetParameterGroupName(Parameter parameter)
        {
            // Try to categorize by parameter name prefixes
            try 
            {
                if (parameter == null || parameter.Definition == null)
                    return "Other";

                string name = parameter.Definition.Name;
                if (!string.IsNullOrEmpty(name))
                {
                    // Try to categorize by common prefixes
                    if (name.StartsWith("View ")) return "Views";
                    if (name.StartsWith("Graphics")) return "Graphics";
                    if (name.StartsWith("Visibility")) return "Visibility";
                    if (name.StartsWith("Analytical")) return "Analysis";
                    if (name.StartsWith("Model")) return "Model";
                    if (name.StartsWith("Data")) return "Data";
                    if (name.StartsWith("Identity")) return "Identity Data";
                    if (name.StartsWith("Constraints")) return "Constraints";
                    if (name.StartsWith("Materials")) return "Materials";
                    if (name.Contains("Section")) return "Sections";
                }
                
                // Use parameter name prefix as group if we can't categorize
                if (name.Contains("."))
                    return name.Split('.')[0];
            }
            catch
            {
                // Fallback for any errors
            }

            return "Other";
        }
    }
}
