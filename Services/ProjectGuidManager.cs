using System;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Miller_Craft_Tools.Utils;

namespace Miller_Craft_Tools.Services
{
    /// <summary>
    /// Handles the creation, retrieval, and storage of Project GUIDs for Miller Craft tools
    /// </summary>
    public class ProjectGuidManager
    {
        private readonly Document _document;
        private const string GuidParameterName = "sp.MC.ProjectGUID";
        private const string GuidMarker = "[MC_GUID:";

        public ProjectGuidManager(Document document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
        }

        /// <summary>
        /// Gets an existing project GUID or creates a new one and ensures it's properly stored
        /// </summary>
        public string GetOrCreateProjectGuid()
        {
            string projectId = null;
            ProjectInfo projectInfo = _document.ProjectInformation;
            bool parameterUpdated = false;

            // First check if we can find an existing GUID through various storage methods
            // 1. Look for existing parameter
            Parameter mcProjectGuidParam = projectInfo.LookupParameter(GuidParameterName);
            if (mcProjectGuidParam != null && !string.IsNullOrWhiteSpace(mcProjectGuidParam.AsString()))
            {
                projectId = mcProjectGuidParam.AsString();
                Logger.LogJson(new { Action = "Using existing ProjectGUID from parameter", GUID = projectId, Source = GuidParameterName }, "guid_usage");
                return projectId;
            }

            // 2. Try to extract from Project Name if embedded
            projectId = ExtractGuidFromProjectName();
            if (!string.IsNullOrWhiteSpace(projectId))
            {
                Logger.LogJson(new { Action = "Using existing ProjectGUID from project name", GUID = projectId }, "guid_usage");
                
                // Save it to the proper parameter if possible
                using (Transaction tx = new Transaction(_document, "Store MC Project GUID Parameter"))
                {
                    tx.Start();
                    
                    // Try to create or update the parameter
                    if (CreateOrUpdateProjectGuidParameter(projectId))
                    {
                        Logger.LogJson(new { Action = "Stored ProjectGUID to parameter", GUID = projectId, Success = true }, "guid_storage");
                    }
                    else
                    {
                        Logger.LogJson(new { Action = "Failed to store ProjectGUID to parameter", GUID = projectId }, "guid_storage");
                    }
                    
                    tx.Commit();
                }
                
                return projectId;
            }

            // 3. Check for a backup file
            string projectFolder = Path.GetDirectoryName(_document.PathName);
            string projectFile = Path.GetFileNameWithoutExtension(_document.PathName);
            string guidFilePath = Path.Combine(projectFolder, $"{projectFile}_MC_GUID.txt");
            
            if (File.Exists(guidFilePath))
            {
                try
                {
                    projectId = File.ReadAllText(guidFilePath).Trim();
                    if (!string.IsNullOrWhiteSpace(projectId))
                    {
                        Logger.LogJson(new { Action = "Using existing ProjectGUID from file", GUID = projectId, FilePath = guidFilePath }, "guid_usage");
                        
                        // Store it in the proper parameter
                        using (Transaction tx = new Transaction(_document, "Store MC Project GUID Parameter"))
                        {
                            tx.Start();
                            
                            // Try to create or update the parameter
                            if (CreateOrUpdateProjectGuidParameter(projectId))
                            {
                                Logger.LogJson(new { Action = "Stored ProjectGUID to parameter", GUID = projectId, Success = true }, "guid_storage");
                            }
                            else
                            {
                                Logger.LogJson(new { Action = "Failed to store ProjectGUID to parameter", GUID = projectId }, "guid_storage");
                            }
                            
                            tx.Commit();
                        }
                        
                        return projectId;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to read GUID from file: {ex.Message}");
                }
            }

            // If we're here, we need to generate a new GUID
            using (Transaction tx = new Transaction(_document, "Create MC Project GUID"))
            {
                tx.Start();
                
                // Generate a new GUID
                Guid guid = Guid.NewGuid();
                projectId = guid.ToString();
                
                // Log the new GUID
                Logger.LogJson(new { Action = "Generated new ProjectGUID", GUID = projectId }, "guid_generation");

                // Try to store it in the proper parameter and log the result
                bool paramCreated = CreateOrUpdateProjectGuidParameter(projectId);
                
                // If we couldn't create/set the parameter, store it somewhere else
                if (!paramCreated)
                {
                    Logger.LogJson(new { Action = "Failed to create ProjectGUID parameter", GUID = projectId }, "guid_storage");
                    projectId = StoreGuidInExistingParameter(projectId, projectInfo);
                }
                else
                {
                    Logger.LogJson(new { Action = "Successfully created ProjectGUID parameter", GUID = projectId }, "guid_storage");
                    Autodesk.Revit.UI.TaskDialog.Show("Project GUID Created", 
                        $"A new Miller Craft Project GUID has been created: {projectId}");
                }
                
                tx.Commit();
            }

            return projectId;
        }
        
        /// <summary>
        /// Attempts to create or update the sp.MC.ProjectGUID parameter
        /// </summary>
        /// <param name="projectId">The GUID to store</param>
        /// <returns>True if successful, false otherwise</returns>
        private bool CreateOrUpdateProjectGuidParameter(string projectId)
        {
            try
            {
                ProjectInfo projectInfo = _document.ProjectInformation;
                
                // First check if the parameter already exists
                Parameter mcProjectGuidParam = projectInfo.LookupParameter(GuidParameterName);
                
                if (mcProjectGuidParam != null)
                {
                    // Parameter exists, just update its value
                    if (!mcProjectGuidParam.IsReadOnly)
                    {
                        mcProjectGuidParam.Set(projectId);
                        return true;
                    }
                    else
                    {
                        Logger.LogError($"Parameter {GuidParameterName} exists but is read-only");
                        return false;
                    }
                }
                
                // If we're here, we need to try to create the parameter
                // Since creating a project parameter programmatically is complex and requires DefinitionFile access,
                // we'll first check if the project already has any custom parameter we can use
                
                // Try common Miller Craft parameter names that might already exist
                string[] possibleParamNames = new[] { "MC.ProjectGUID", "MCProjectGUID", "Miller Craft GUID" };
                
                foreach (string paramName in possibleParamNames)
                {
                    Parameter existingParam = projectInfo.LookupParameter(paramName);
                    if (existingParam != null && !existingParam.IsReadOnly)
                    {
                        existingParam.Set(projectId);
                        Logger.LogJson(new { Action = "Used existing parameter", ParameterName = paramName, GUID = projectId }, "guid_storage");
                        return true;
                    }
                }
                
                // If no suitable parameter exists, we can't create one directly without shared parameter file
                // We'll log this outcome and return false
                Logger.LogJson(new { Action = "No suitable parameter found and cannot create one", GUID = projectId }, "guid_storage");
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to create/update ProjectGUID parameter: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Stores the GUID in an existing parameter or alternative location when a dedicated parameter isn't available
        /// </summary>
        private string StoreGuidInExistingParameter(string projectId, ProjectInfo projectInfo)
        {
            try
            {
                // Try to store in Project Name parameter
                Parameter projectNameParam = projectInfo.LookupParameter("Project Name");
                
                if (projectNameParam != null && !projectNameParam.IsReadOnly)
                {
                    // Get the current project name
                    string currentName = projectNameParam.AsString() ?? string.Empty;
                    
                    // If the marker already exists, remove it
                    int markerIndex = currentName.IndexOf(GuidMarker);
                    if (markerIndex >= 0)
                    {
                        int endIndex = currentName.IndexOf("]", markerIndex);
                        if (endIndex >= 0)
                        {
                            currentName = currentName.Substring(0, markerIndex).TrimEnd() + 
                                        currentName.Substring(endIndex + 1);
                        }
                    }
                    
                    // Add the GUID to the project name
                    string newName = currentName.TrimEnd() + " " + GuidMarker + projectId + "]";
                    projectNameParam.Set(newName);
                    
                    Autodesk.Revit.UI.TaskDialog.Show("Project GUID Created", 
                        $"A new Miller Craft Project GUID has been created: {projectId}");
                    
                    return projectId;
                }

                // Try to store in any custom parameter if available
                Parameter customParam = projectInfo.GetParameters("MCProjectGUID").FirstOrDefault();
                if (customParam != null && !customParam.IsReadOnly)
                {
                    customParam.Set(projectId);
                    return projectId;
                }
                
                // Last resort - store in a file
                string projectFolder = Path.GetDirectoryName(_document.PathName);
                string projectFile = Path.GetFileNameWithoutExtension(_document.PathName);
                string guidFilePath = Path.Combine(projectFolder, $"{projectFile}_MC_GUID.txt");
                
                // Write the GUID to the file
                File.WriteAllText(guidFilePath, projectId);
                
                Autodesk.Revit.UI.TaskDialog.Show("Project GUID Created", 
                    $"A new Miller Craft Project GUID has been created: {projectId}\n\n" +
                    $"The GUID has been saved alongside your Revit file for future reference.");
                    
                return projectId;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to store project GUID: {ex.Message}");
                // Return the GUID even if storage fails, to allow the operation to continue
                return projectId;
            }
        }

        /// <summary>
        /// Tries to extract GUID from Project Name if it's embedded there
        /// </summary>
        public string ExtractGuidFromProjectName()
        {
            try
            {
                ProjectInfo projectInfo = _document.ProjectInformation;
                Parameter projectNameParam = projectInfo.LookupParameter("Project Name");
                
                if (projectNameParam != null)
                {
                    string projectName = projectNameParam.AsString() ?? string.Empty;
                    int markerIndex = projectName.IndexOf(GuidMarker);
                    
                    if (markerIndex >= 0)
                    {
                        int startIndex = markerIndex + GuidMarker.Length;
                        int endIndex = projectName.IndexOf("]", startIndex);
                        
                        if (endIndex >= 0)
                        {
                            string guid = projectName.Substring(startIndex, endIndex - startIndex);
                            if (!string.IsNullOrWhiteSpace(guid))
                            {
                                return guid;
                            }
                        }
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error extracting GUID from project name: {ex.Message}");
                return null;
            }
        }
    }
}
