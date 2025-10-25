using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Windows.Forms;

namespace Miller_Craft_Tools.Command
{
    [Transaction(TransactionMode.Manual)]
    public class WallAssemblyStandardizerCommand : IExternalCommand
    {
        #region Enums
        
        public enum WallAssemblyCategory
        {
            Exterior,
            ExteriorFinish,
            Interior,
            InteriorFinish,
            Structural
        }
        
        public enum WallFunction
        {
            Interior,
            Exterior,
            Foundation,
            Retaining,
            Core,
            Shaft
        }
        
        public enum LayerFunction
        {
            Structure,
            Finish,
            Membrane,
            Substrate,
            Thermal,
            Air
        }
        
        #endregion
        
        #region Classes
        
        public class WallLayerTemplate
        {
            // Material name (must start with "ZOOT - ")
            public string MaterialName { get; set; }
            
            // Layer function (Structure, Finish, Thermal, etc.)
            public LayerFunction Function { get; set; }
            
            // Layer thickness in feet
            public double Thickness { get; set; }
        }
        
        public class WallAssemblyTemplate
        {
            // Category identifier
            public WallAssemblyCategory Category { get; set; }
            
            // Unique name for this template (without prefix, e.g. "Wood_Stud_16OC")
            public string Name { get; set; }
            
            // Description of this wall assembly
            public string Description { get; set; }
            
            // Wall function (Interior, Exterior, Foundation, etc.)
            public WallFunction Function { get; set; }
            
            // Wall type (Basic, Stacked, Curtain, etc.)
            public WallFamily Family { get; set; }
            
            // Default width in feet
            public double Width { get; set; }
            
            // List of layers in order from exterior to interior
            public List<WallLayerTemplate> Layers { get; set; }
            
            // Additional parameters to set on the wall type
            public Dictionary<string, string> Parameters { get; set; }
            
            // Gets the full standardized name including prefix
            public string GetStandardName() 
            {
                string prefix = Category switch
                {
                    WallAssemblyCategory.Exterior => "E_",
                    WallAssemblyCategory.ExteriorFinish => "EF_",
                    WallAssemblyCategory.Interior => "I_",
                    WallAssemblyCategory.InteriorFinish => "IF_",
                    WallAssemblyCategory.Structural => "S_",
                    _ => "X_"
                };
                
                return prefix + Name;
            }
        }
        
        public enum WallFamily
        {
            Basic,
            Stacked,
            Curtain
        }
        
        #endregion
        
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Get all wall types in the project
            var wallTypeCollector = new FilteredElementCollector(doc)
                .OfClass(typeof(WallType))
                .Cast<WallType>()
                .ToList();

            if (!wallTypeCollector.Any())
            {
                Autodesk.Revit.UI.TaskDialog.Show("Wall Assembly Standardizer", "No wall types found in the model.");
                return Result.Succeeded;
            }

            // Get all materials with "ZOOT - " prefix
            var materialCollector = new FilteredElementCollector(doc)
                .OfClass(typeof(Material))
                .Cast<Material>()
                .Where(m => m.Name.StartsWith("ZOOT - "))
                .ToList();

            if (!materialCollector.Any())
            {
                Autodesk.Revit.UI.TaskDialog.Show("Wall Assembly Standardizer", 
                    "No materials with prefix 'ZOOT - ' were found. Please create these materials first.");
                return Result.Succeeded;
            }

            // Load templates (in a real implementation, these might come from config files or UI)
            List<WallAssemblyTemplate> standardTemplates = GetStandardTemplates();

            // Show dialog to let user select templates to create
            var configDialog = new Miller_Craft_Tools.UI.Dialogs.WallAssemblyConfigDialog(doc, standardTemplates, materialCollector);
            if (configDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                // User canceled
                return Result.Cancelled;
            }
            
            // Get selected templates from dialog
            standardTemplates = configDialog.SelectedTemplates;
            if (standardTemplates.Count == 0)
            {
                Autodesk.Revit.UI.TaskDialog.Show("Wall Assembly Standardizer", "No templates were selected.");
                return Result.Succeeded;
            }

            try
            {
                using (Transaction tx = new Transaction(doc, "Wall Assembly Standardization"))
                {
                    TransactionStatus tStatus = tx.Start();
                    if (tStatus != TransactionStatus.Started)
                    {
                        Autodesk.Revit.UI.TaskDialog.Show("Wall Assembly Standardizer", 
                            $"ERROR starting transaction: {tStatus}");
                        return Result.Failed;
                    }

                    int renamed = 0;
                    int created = 0;
                    List<string> results = new List<string>();

                    // Step 1: Find and rename existing wall types that match our templates
                    foreach (var template in standardTemplates)
                    {
                        string standardName = template.GetStandardName();
                        
                        // Look for existing wall types that match this template
                        foreach (var wallType in wallTypeCollector)
                        {
                            // If the wall type already has the correct name, skip it
                            if (wallType.Name == standardName)
                                continue;

                            // Check if this wall type matches our template
                            if (MatchesTemplate(wallType, template))
                            {
                                string oldName = wallType.Name;
                                wallType.Name = standardName;
                                renamed++;
                                results.Add($"Renamed: {oldName} → {standardName}");
                            }
                        }
                    }

                    // Step 2: Create any missing standard wall types
                    foreach (var template in standardTemplates)
                    {
                        string standardName = template.GetStandardName();
                        
                        // Check if this wall type already exists
                        bool exists = wallTypeCollector.Any(wt => wt.Name == standardName);
                        if (!exists)
                        {
                            // Create a new wall type from the template
                            WallType newWallType = CreateWallType(doc, template, materialCollector);
                            if (newWallType != null)
                            {
                                created++;
                                results.Add($"Created: {standardName}");
                            }
                        }
                    }

                    tx.Commit();

                    // Display results
                    string summary = 
                        $"Wall Assembly Standardization complete.\n\n" +
                        $"Wall types renamed: {renamed}\n" +
                        $"Wall types created: {created}\n\n";
                    
                    if (results.Count > 0)
                    {
                        summary += "Results:\n" + string.Join("\n", results.Take(20));
                        if (results.Count > 20)
                            summary += $"\n... and {results.Count - 20} more";
                    }
                    
                    Autodesk.Revit.UI.TaskDialog.Show("Wall Assembly Standardizer", summary);
                }
            }
            catch (Exception ex)
            {
                Autodesk.Revit.UI.TaskDialog.Show("Wall Assembly Standardizer Error", ex.Message);
                return Result.Failed;
            }

            return Result.Succeeded;
        }

        /// <summary>
        /// Check if a wall type matches a template
        /// </summary>
        private bool MatchesTemplate(WallType wallType, WallAssemblyTemplate template)
        {
            // This is a placeholder for the real matching logic
            // In a full implementation, you would compare:
            // 1. Layer structure
            // 2. Materials
            // 3. Functions
            // 4. Thickness
            
            // For now, just do a simple check based on name similarity
            string nameWithoutPrefix = wallType.Name;
            
            // Strip any existing prefix
            if (nameWithoutPrefix.StartsWith("E_") || 
                nameWithoutPrefix.StartsWith("EF_") || 
                nameWithoutPrefix.StartsWith("I_") || 
                nameWithoutPrefix.StartsWith("IF_") || 
                nameWithoutPrefix.StartsWith("S_"))
            {
                nameWithoutPrefix = nameWithoutPrefix.Substring(3);
            }
            
            // Check if the name contains the template name (simple fuzzy match)
            return nameWithoutPrefix.Contains(template.Name) || 
                   template.Name.Contains(nameWithoutPrefix);
        }

        /// <summary>
        /// Create a new wall type from a template
        /// </summary>
        private WallType CreateWallType(Document doc, WallAssemblyTemplate template, List<Material> availableMaterials)
        {
            // This is a placeholder for the real creation logic
            // In a full implementation, you would:
            // 1. Create a duplicate of an existing wall type
            // 2. Modify its structure (layers, materials)
            // 3. Set parameters
            // 4. Set name
            
            try
            {
                // Find a suitable wall type to duplicate
                // For basic example, just get the first wall type of the same family
                ElementId basicWallTypeId = GetDefaultWallTypeId(doc, template.Family);
                if (basicWallTypeId == null || basicWallTypeId == ElementId.InvalidElementId)
                    return null;
                    
                // Duplicate the wall type
                WallType existingWallType = doc.GetElement(basicWallTypeId) as WallType;
                ElementType newElementType = existingWallType.Duplicate(template.GetStandardName());
                WallType newWallType = newElementType as WallType;
                
                // TODO: Configure wall type layers and materials
                // This requires more detailed Revit API work with CompoundStructure
                
                return newWallType;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WallAssemblyStandardizer] ERROR creating wall type {template.GetStandardName()}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get the ID of a default wall type for a given wall family
        /// </summary>
        private ElementId GetDefaultWallTypeId(Document doc, WallFamily family)
        {
            // For Basic wall, return the first Basic wall type found
            var collector = new FilteredElementCollector(doc)
                .OfClass(typeof(WallType))
                .Cast<WallType>().ToList();
                
            switch (family)
            {
                case WallFamily.Basic:
                    // Use name-based approach - basic walls typically have "Basic Wall" in their family name
                    return collector.FirstOrDefault(wt => wt.FamilyName?.Contains("Basic Wall") == true)?.Id;
                case WallFamily.Stacked:
                    // Use name-based approach - stacked walls typically have "Stacked Wall" in their family name
                    return collector.FirstOrDefault(wt => wt.FamilyName?.Contains("Stacked Wall") == true)?.Id;
                case WallFamily.Curtain:
                    // Use name-based approach - curtain walls typically have "Curtain Wall" in their family name
                    return collector.FirstOrDefault(wt => wt.FamilyName?.Contains("Curtain Wall") == true)?.Id;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Get standard wall templates for the organization
        /// </summary>
        private List<WallAssemblyTemplate> GetStandardTemplates()
        {
            // This is a placeholder - in a real implementation, these templates would come from:
            // 1. A configuration file (JSON, XML)
            // 2. A database
            // 3. User input via a dialog
            
            // For now, just create a few example templates
            List<WallAssemblyTemplate> templates = new List<WallAssemblyTemplate>
            {
                // Example: Exterior Wall - Wood Framed
                new WallAssemblyTemplate
                {
                    Category = WallAssemblyCategory.Exterior,
                    Name = "Wood_Stud_16OC",
                    Description = "2x6 Wood studs @ 16\" O.C. with exterior insulation",
                    Function = WallFunction.Exterior,
                    Family = WallFamily.Basic,
                    Width = 0.7, // 8.4 inches
                    Layers = new List<WallLayerTemplate>
                    {
                        // Layers will be specified here
                    }
                },
                
                // Example: Interior Wall - Metal Stud Partition
                new WallAssemblyTemplate
                {
                    Category = WallAssemblyCategory.Interior,
                    Name = "Metal_Stud_3-5_8",
                    Description = "3-5/8\" Metal studs with single layer gypsum board each side",
                    Function = WallFunction.Interior,
                    Family = WallFamily.Basic,
                    Width = 0.475, // 5.7 inches
                    Layers = new List<WallLayerTemplate>
                    {
                        // Layers will be specified here
                    }
                },
                
                // Example: Structural Wall
                new WallAssemblyTemplate
                {
                    Category = WallAssemblyCategory.Structural,
                    Name = "Concrete_12in",
                    Description = "12\" Cast-in-place concrete wall",
                    Function = WallFunction.Core,
                    Family = WallFamily.Basic,
                    Width = 1.0, // 12 inches
                    Layers = new List<WallLayerTemplate>
                    {
                        // Layers will be specified here
                    }
                }
            };
            
            return templates;
        }
    }
}
