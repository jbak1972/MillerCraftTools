using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Miller_Craft_Tools.Command
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class SetupStandardsCommand : IExternalCommand
    {
        // Helper method to create a definition group if it doesn't exist
        private DefinitionGroup CreateDefinitionGroup(Document doc, string groupName)
        {
            DefinitionFile defFile = doc.Application.OpenSharedParameterFile();
            if (defFile == null)
            {
                // Create a temporary shared parameter file if none exists
                string tempPath = Path.Combine(Path.GetTempPath(), "TempSharedParameters.txt");
                try
                {
                    File.WriteAllText(tempPath, "");
                    doc.Application.SharedParametersFilename = tempPath;
                    defFile = doc.Application.OpenSharedParameterFile();
                    if (defFile == null)
                    {
                        throw new Exception("Failed to create or open a temporary shared parameter file.");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to create temporary shared parameter file at {tempPath}: {ex.Message}");
                }
            }

            DefinitionGroup group = defFile.Groups.get_Item(groupName) ?? defFile.Groups.Create(groupName);
            if (group == null)
            {
                throw new Exception($"Failed to create or find definition group '{groupName}'.");
            }

            return group;
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // Step 1: Read ProjectStandards.json
                string jsonPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Standards", "ProjectStandards.json");
                if (!File.Exists(jsonPath))
                {
                    TaskDialog.Show("Error", $"ProjectStandards.json not found at {jsonPath}.");
                    return Result.Failed;
                }

                string jsonContent = File.ReadAllText(jsonPath);
                var standards = JsonSerializer.Deserialize<ProjectStandards>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (standards == null)
                {
                    TaskDialog.Show("Error", "Failed to deserialize ProjectStandards.json.");
                    return Result.Failed;
                }

                string standardsVersion = standards.StandardsVersion;
                var versionParameter = standards.ProjectParameters?.FirstOrDefault(p => p.Name == "StandardsVersion");
                if (versionParameter == null)
                {
                    TaskDialog.Show("Error", "StandardsVersion parameter not found in ProjectStandards.json.");
                    return Result.Failed;
                }

                // Step 2: Apply StandardsVersion parameter to Project Information
                using (Transaction tx = new Transaction(doc, "Apply StandardsVersion to Project"))
                {
                    tx.Start();

                    // Get the Project Information category
                    Category projectInfoCategory = doc.Settings.Categories.get_Item(BuiltInCategory.OST_ProjectInformation);
                    if (projectInfoCategory == null)
                    {
                        TaskDialog.Show("Error", "Project Information category not found.");
                        tx.RollBack();
                        return Result.Failed;
                    }

                    // Create the parameter definition using ForgeTypeId for SpecTypeId.String.Text
                    DefinitionGroup defGroup = CreateDefinitionGroup(doc, versionParameter.Group);
                    ExternalDefinitionCreationOptions options = new ExternalDefinitionCreationOptions(versionParameter.Name, SpecTypeId.String.Text)
                    {
                        Description = "Tracks the version of the project standards applied"
                    };
                    ExternalDefinition definition = defGroup.Definitions.get_Item(versionParameter.Name) as ExternalDefinition ?? defGroup.Definitions.Create(options) as ExternalDefinition;
                    if (definition == null)
                    {
                        TaskDialog.Show("Error", $"Failed to create or find shared parameter definition for '{versionParameter.Name}'.");
                        tx.RollBack();
                        return Result.Failed;
                    }

                    // Bind the parameter to Project Information using ForgeTypeId for Identity Data
                    CategorySet categories = doc.Application.Create.NewCategorySet();
                    categories.Insert(projectInfoCategory);
                    Binding binding = doc.Application.Create.NewInstanceBinding(categories);
                    if (!doc.ParameterBindings.ReInsert(definition, binding, GroupTypeId.IdentityData))
                    {
                        // If ReInsert fails, try Insert (in case the binding doesn't exist)
                        if (!doc.ParameterBindings.Insert(definition, binding, GroupTypeId.IdentityData))
                        {
                            TaskDialog.Show("Error", $"Failed to bind parameter '{versionParameter.Name}' to Project Information.");
                            tx.RollBack();
                            return Result.Failed;
                        }
                    }

                    // Set the parameter value on the Project Information element
                    Element projectInfo = new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_ProjectInformation)
                        .FirstElement();
                    if (projectInfo == null)
                    {
                        TaskDialog.Show("Error", "Project Information element not found.");
                        tx.RollBack();
                        return Result.Failed;
                    }

                    Parameter param = projectInfo.LookupParameter(versionParameter.Name);
                    if (param == null)
                    {
                        TaskDialog.Show("Error", $"Parameter '{versionParameter.Name}' not found on Project Information element after binding.");
                        tx.RollBack();
                        return Result.Failed;
                    }

                    if (!param.Set(standardsVersion))
                    {
                        TaskDialog.Show("Error", $"Failed to set value '{standardsVersion}' for parameter '{versionParameter.Name}' on Project Information.");
                        tx.RollBack();
                        return Result.Failed;
                    }

                    tx.Commit();
                }

                // Step 3: Apply StandardsVersion parameter to Families (limited to Detail Items with names starting with "D.ANNO")
                // Removed the outer transaction to avoid conflict with EditFamily
                // Get family symbols in the Detail Item category (OST_DetailComponents)
                FilteredElementCollector familyCollector = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .OfCategory(BuiltInCategory.OST_DetailComponents);
                List<FamilySymbol> familySymbols = familyCollector
                    .Cast<FamilySymbol>()
                    .Where(fs => fs.Family != null && fs.Family.Name.StartsWith("D.ANNO", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (familySymbols.Count == 0)
                {
                    TaskDialog.Show("Info", "No Detail Item families starting with 'D.ANNO' were found in the project.");
                }
                else
                {
                    foreach (FamilySymbol familySymbol in familySymbols)
                    {
                        Family family = familySymbol.Family;
                        if (family == null) continue;

                        // Open the family document for editing
                        Document familyDoc = doc.EditFamily(family);
                        if (familyDoc == null)
                        {
                            TaskDialog.Show("Warning", $"Failed to open family '{family.Name}' for editing. Skipping.");
                            continue;
                        }

                        try
                        {
                            using (Transaction familyTx = new Transaction(familyDoc, "Add StandardsVersion to Family"))
                            {
                                familyTx.Start();

                                // Check if the parameter already exists
                                FamilyParameter familyParam = familyDoc.FamilyManager.get_Parameter(versionParameter.Name);
                                if (familyParam == null)
                                {
                                    // Add the parameter to the family using ForgeTypeId for Identity Data
                                    familyParam = familyDoc.FamilyManager.AddParameter(
                                        versionParameter.Name,
                                        GroupTypeId.IdentityData,
                                        SpecTypeId.String.Text,
                                        versionParameter.IsInstance);
                                    if (familyParam == null)
                                    {
                                        TaskDialog.Show("Warning", $"Failed to add parameter '{versionParameter.Name}' to family '{family.Name}'. Skipping.");
                                        familyTx.RollBack();
                                        continue;
                                    }
                                }

                                // Set the parameter value for all family types
                                foreach (FamilyType familyType in familyDoc.FamilyManager.Types)
                                {
                                    familyDoc.FamilyManager.CurrentType = familyType;

                                    // Reload the family back into the project
                                    try
                                    {
                                        familyDoc.LoadFamily(doc, new FamilyLoadOptions());
                                    }
                                    catch (Exception ex)
                                    {
                                        TaskDialog.Show("Warning", $"Failed to reload family '{family.Name}' into the project: {ex.Message}. Changes may not be applied.");
                                    }
                                }

                                familyTx.Commit();
                            }

                            // Reload the family back into the project
                            try
                            {
                                familyDoc.LoadFamily(doc, new FamilyLoadOptions());
                            }
                            catch (Exception ex)
                            {
                                TaskDialog.Show("Warning", $"Failed to reload family '{family.Name}' into the project: {ex.Message}. Changes may not be applied.");
                            }
                        }
                        catch (Exception ex)
                        {
                            TaskDialog.Show("Warning", $"Error processing family '{family.Name}': {ex.Message}. Skipping.");
                        }
                        finally
                        {
                            familyDoc.Close(false);
                        }
                    }
                }

                TaskDialog.Show("Success", $"Applied StandardsVersion '{standardsVersion}' to project and selected families.");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Error", $"Failed to apply standards: {ex.Message}");
                return Result.Failed;
            }
        }
    }

    // Helper class to deserialize ProjectStandards.json
    public class ProjectStandards
    {
        public string StandardsVersion { get; set; }
        public List<ProjectParameter> ProjectParameters { get; set; }
        public List<object> SharedParameters { get; set; }
    }

    public class ProjectParameter
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Group { get; set; }
        public List<string> Categories { get; set; }
        public bool IsInstance { get; set; }
    }

    // Helper class for family loading options
    public class FamilyLoadOptions : IFamilyLoadOptions
    {
        public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
        {
            overwriteParameterValues = true;
            return true;
        }

        public bool OnSharedFamilyFound(Family sharedFamily, bool familyInUse, out FamilySource source, out bool overwriteParameterValues)
        {
            source = FamilySource.Project;
            overwriteParameterValues = true;
            return true;
        }
    }
}