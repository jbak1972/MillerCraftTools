using System;
using System.IO;
using Autodesk.Revit.DB;
using Autodesk.Revit.ApplicationServices;

namespace Miller_Craft_Tools.Utils
{
    /// <summary>
    /// Helper class for creating required shared parameters if they don't exist
    /// This is a temporary solution until the full shared parameters system is implemented
    /// </summary>
    public static class ParameterCreationHelper
    {
        private const string SharedParamFileName = "MillerCraftTools_Temp.txt";
        private const string ProjectGuidParamName = "sp.MC.ProjectGUID";
        private const string ProjectGuidParamGuid = "8a7f6e5d-4c3b-2a1e-9f8d-7c6b5a4e3d2c"; // Fixed GUID for this parameter
        
        /// <summary>
        /// Ensures the sp.MC.ProjectGUID parameter exists in the document
        /// Creates it as a shared parameter if it doesn't exist
        /// </summary>
        /// <param name="doc">The Revit document</param>
        /// <returns>True if parameter exists or was created successfully, false otherwise</returns>
        public static bool EnsureProjectGuidParameterExists(Document doc)
        {
            try
            {
                // Check if parameter already exists
                ProjectInfo projInfo = doc.ProjectInformation;
                Parameter existingParam = projInfo?.LookupParameter(ProjectGuidParamName);
                
                if (existingParam != null)
                {
                    Logger.LogInfo($"Parameter '{ProjectGuidParamName}' already exists");
                    return true;
                }
                
                Logger.LogInfo($"Parameter '{ProjectGuidParamName}' not found. Creating it...");
                
                // Get the application
                Autodesk.Revit.ApplicationServices.Application app = doc.Application;
                
                // Get or create temporary shared parameters file
                string sharedParamFilePath = CreateTemporarySharedParametersFile();
                
                // Store the current shared parameters file path
                string originalSharedParamFile = app.SharedParametersFilename;
                
                try
                {
                    // Set our temporary file as the shared parameters file
                    app.SharedParametersFilename = sharedParamFilePath;
                    
                    // Open the shared parameters file
                    DefinitionFile defFile = app.OpenSharedParameterFile();
                    if (defFile == null)
                    {
                        Logger.LogError("Failed to open shared parameters file");
                        return false;
                    }
                    
                    // Get or create the parameter group
                    DefinitionGroup defGroup = defFile.Groups.get_Item("Miller Craft Parameters") 
                        ?? defFile.Groups.Create("Miller Craft Parameters");
                    
                    // Get or create the parameter definition
                    Definition paramDef = defGroup.Definitions.get_Item(ProjectGuidParamName);
                    if (paramDef == null)
                    {
                        // Create new external definition options
                        ExternalDefinitionCreationOptions options = new ExternalDefinitionCreationOptions(
                            ProjectGuidParamName,
                            SpecTypeId.String.Text);
                        options.Description = "Unique GUID identifying this project in Miller Craft Assistant";
                        options.GUID = new Guid(ProjectGuidParamGuid);
                        
                        paramDef = defGroup.Definitions.Create(options);
                    }
                    
                    if (paramDef == null)
                    {
                        Logger.LogError("Failed to create parameter definition");
                        return false;
                    }
                    
                    // Bind the parameter to Project Information category
                    CategorySet catSet = app.Create.NewCategorySet();
                    Category projectInfoCategory = doc.Settings.Categories.get_Item(BuiltInCategory.OST_ProjectInformation);
                    catSet.Insert(projectInfoCategory);
                    
                    // Create instance binding
                    InstanceBinding binding = app.Create.NewInstanceBinding(catSet);
                    
                    // Get the binding map
                    BindingMap bindingMap = doc.ParameterBindings;
                    
                    // Check if binding already exists
                    if (bindingMap.Contains(paramDef))
                    {
                        Logger.LogInfo("Parameter definition exists, updating binding");
                        bindingMap.ReInsert(paramDef, binding, GroupTypeId.IdentityData);
                    }
                    else
                    {
                        Logger.LogInfo("Adding new parameter binding");
                        bindingMap.Insert(paramDef, binding, GroupTypeId.IdentityData);
                    }
                    
                    Logger.LogInfo($"Successfully created parameter '{ProjectGuidParamName}'");
                    return true;
                }
                finally
                {
                    // Restore original shared parameters file
                    if (!string.IsNullOrEmpty(originalSharedParamFile))
                    {
                        app.SharedParametersFilename = originalSharedParamFile;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error ensuring ProjectGUID parameter exists: {ex.Message}");
                Logger.LogError($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }
        
        /// <summary>
        /// Creates a temporary shared parameters file with the Miller Craft parameters
        /// </summary>
        /// <returns>Path to the temporary shared parameters file</returns>
        private static string CreateTemporarySharedParametersFile()
        {
            try
            {
                // Create in user's temp folder
                string tempFolder = Path.GetTempPath();
                string filePath = Path.Combine(tempFolder, SharedParamFileName);
                
                // Create the shared parameters file content
                // Format MUST be exact - TAB delimited
                string fileContent = 
                    "# This is a Revit shared parameters file.\r\n" +
                    "# Do not edit manually.\r\n" +
                    "*META\tVERSION\tMINVERSION\r\n" +
                    "META\t2\t1\r\n" +
                    "*GROUP\tID\tNAME\r\n" +
                    "GROUP\t1\tMiller Craft Parameters\r\n" +
                    "*PARAM\tGUID\tNAME\tDATATYPE\tDATACATEGORY\tGROUP\tVISIBLE\tDESCRIPTION\tUSERMODIFIABLE\r\n" +
                    $"PARAM\t{ProjectGuidParamGuid}\t{ProjectGuidParamName}\tTEXT\t\t1\t1\tUnique GUID identifying this project in Miller Craft Assistant\t1\r\n";
                
                // Write the file
                File.WriteAllText(filePath, fileContent);
                
                Logger.LogInfo($"Created temporary shared parameters file: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error creating temporary shared parameters file: {ex.Message}");
                throw;
            }
        }
    }
}
