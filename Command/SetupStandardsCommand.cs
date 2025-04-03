using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
                File.WriteAllText(tempPath, "");
                doc.Application.SharedParametersFilename = tempPath;
                defFile = doc.Application.OpenSharedParameterFile();
            }

            return defFile.Groups.Create(groupName);
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
                    TaskDialog.Show("Error", "ProjectStandards.json not found.");
                    return Result.Failed;
                }

                string jsonContent = File.ReadAllText(jsonPath);
                var standards = JsonConvert.DeserializeObject<ProjectStandards>(jsonContent);

                string standardsVersion = standards.StandardsVersion;
                var versionParameter = standards.ProjectParameters.FirstOrDefault(p => p.Name == "StandardsVersion");

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

                    // Create the parameter definition using SpecTypeId.String.Text
                    DefinitionGroup defGroup = doc.Application.OpenSharedParameterFile()?.Groups.get_Item(versionParameter.Group) ?? CreateDefinitionGroup(doc, versionParameter.Group);
                    ExternalDefinitionCreationOptions options = new ExternalDefinitionCreationOptions(versionParameter.Name, SpecTypeId.String.Text)
                    {
                        Description = "Tracks the version of the project standards applied"
                    };
                    ExternalDefinition definition = defGroup.Definitions.Create(options) as ExternalDefinition;

                    // Bind the parameter to Project Information using GroupTypeId.IdentityData
                    CategorySet categories = doc.Application.Create.NewCategorySet();
                    categories.Insert(projectInfoCategory);
                    Binding binding = doc.Application.Create.NewInstanceBinding(categories);
                    doc.ParameterBindings.Insert(definition, binding, GroupTypeId.IdentityData);

                    // Set the parameter value on the Project Information element
                    Element projectInfo = new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_ProjectInformation)
                        .FirstElement();
                    if (projectInfo != null)
                    {
                        Parameter param = projectInfo.LookupParameter(versionParameter.Name);
                        if (param != null)
                        {
                            param.Set(standardsVersion);
                        }
                    }

                    tx.Commit();
                }

                // Step 3: Apply StandardsVersion parameter to Families
                using (Transaction tx = new Transaction(doc, "Apply StandardsVersion to Families"))
                {
                    tx.Start();

                    // Get all family symbols (types) in the project
                    FilteredElementCollector familyCollector = new FilteredElementCollector(doc)
                        .OfClass(typeof(FamilySymbol));
                    List<FamilySymbol> familySymbols = familyCollector.Cast<FamilySymbol>().ToList();

                    foreach (FamilySymbol familySymbol in familySymbols)
                    {
                        Family family = familySymbol.Family;
                        if (family == null) continue;

                        // Open the family document for editing
                        Document familyDoc = doc.EditFamily(family);
                        if (familyDoc == null) continue;

                        using (Transaction familyTx = new Transaction(familyDoc, "Add StandardsVersion to Family"))
                        {
                            familyTx.Start();

                            // Add the parameter to the family using GroupTypeId.IdentityData
                            FamilyParameter familyParam = familyDoc.FamilyManager.AddParameter(
                                versionParameter.Name,
                                GroupTypeId.IdentityData,
                                SpecTypeId.String.Text,
                                versionParameter.IsInstance);

                            // Set the parameter value for all family symbols
                            foreach (FamilySymbol symbol in familyDoc.FamilyManager.Types)
                            {
                                familyDoc.FamilyManager.Set(familyParam, standardsVersion);
                            }

                            familyTx.Commit();
                        }

                        // Reload the family back into the project
                        familyDoc.LoadFamily(doc, new FamilyLoadOptions());
                        familyDoc.Close(false);
                    }

                    tx.Commit();
                }

                TaskDialog.Show("Success", $"Applied StandardsVersion {standardsVersion} to project and families.");
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