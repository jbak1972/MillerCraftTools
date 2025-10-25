using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using Miller_Craft_Tools.Model;
using Miller_Craft_Tools.Utils;

namespace Miller_Craft_Tools.Services.SyncUtilities
{
    /// <summary>
    /// Manages parameter operations for sync service including collection, finding, and value application
    /// </summary>
    public class ParameterManager
    {
        // Parameter mapping configuration
        private readonly ParameterMappingConfiguration _mappingConfig;
        
        /// <summary>
        /// Creates a new instance of ParameterManager
        /// </summary>
        /// <param name="mappingConfig">Optional parameter mapping configuration</param>
        public ParameterManager(ParameterMappingConfiguration mappingConfig = null)
        {
            _mappingConfig = mappingConfig ?? new ParameterMappingConfiguration();
        }
        
        /// <summary>
        /// Collects parameters from the Revit document for syncing based on mapping rules
        /// </summary>
        
        /// <summary>
        /// Applies a parameter change from the web application to the Revit document
        /// </summary>
        /// <param name="doc">The Revit document</param>
        /// <param name="change">The parameter change to apply</param>
        /// <returns>An AppliedChange object with the result</returns>
        public AppliedChange ApplyParameterChange(Document doc, WebParameterChange change)
        {
            try
            {
                if (doc == null || change == null)
                {
                    return AppliedChange.Create(change, "error", "Document or change is null");
                }

                // Find the element
                Element element = null;
                
                // Try to find by unique ID first
                if (!string.IsNullOrEmpty(change.ElementUniqueId))
                {
                    element = doc.GetElement(change.ElementUniqueId);
                }
                
                // If not found and we have an element ID, try that
                if (element == null && change.ElementId > 0)
                {
                    element = doc.GetElement(new ElementId(change.ElementId));
                }
                
                if (element == null)
                {
                    return AppliedChange.Create(change, "error", "Element not found");
                }
                
                // Find the parameter
                Parameter param = element.LookupParameter(change.Name);
                if (param == null)
                {
                    return AppliedChange.Create(change, "error", $"Parameter '{change.Name}' not found on element");
                }
                
                if (param.IsReadOnly)
                {
                    return AppliedChange.Create(change, "error", $"Parameter '{change.Name}' is read-only");
                }
                
                // Apply the value based on parameter type
                bool success = false;
                switch (param.StorageType)
                {
                    case StorageType.String:
                        success = param.Set(change.Value);
                        break;
                        
                    case StorageType.Integer:
                        if (int.TryParse(change.Value, out int intValue))
                        {
                            success = param.Set(intValue);
                        }
                        break;
                        
                    case StorageType.Double:
                        if (double.TryParse(change.Value, out double doubleValue))
                        {
                            success = param.Set(doubleValue);
                        }
                        break;
                        
                    case StorageType.ElementId:
                        if (int.TryParse(change.Value, out int idValue))
                        {
                            success = param.Set(new ElementId(idValue));
                        }
                        break;
                        
                    default:
                        return AppliedChange.Create(change, "error", $"Unsupported parameter type: {param.StorageType}");
                }
                
                if (success)
                {
                    return AppliedChange.Create(change, "applied");
                }
                else
                {
                    return AppliedChange.Create(change, "error", "Failed to set parameter value");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error applying parameter change: {ex.Message}");
                return AppliedChange.Create(change, "error", ex.Message);
            }
        }

        /// <summary>
        /// Collects parameters from the Revit document for syncing based on mapping rules
        /// </summary>
        /// <param name="doc">Revit document to collect parameters from</param>
        /// <param name="projectGuid">Unique GUID for the Revit project</param>
        /// <returns>SyncRequest with collected parameters</returns>
        public SyncRequest CollectParametersForSync(Document doc, string projectGuid)
        {
            // Get file name - handle case where document hasn't been saved
            string fileName = "Unsaved Project";
            if (!string.IsNullOrEmpty(doc.PathName))
            {
                fileName = Path.GetFileNameWithoutExtension(doc.PathName);
            }
            else if (doc.Title != null)
            {
                fileName = doc.Title; // Use document title as fallback
            }
            
            SyncRequest request = new SyncRequest
            {
                RevitProjectGuid = projectGuid,
                RevitFileName = fileName,
                Version = "1.0",
                Timestamp = DateTime.UtcNow.ToString("o"), // ISO 8601 format
                Command = null // Don't send command unless specifically needed
            };
            
            ProjectInfo projInfo = doc.ProjectInformation;
            
            // Add the ProjectGUID parameter explicitly (always include this)
            request.Parameters.Add(new ParameterData
            {
                Guid = Guid.NewGuid().ToString(),  // Generate a unique ID for this parameter
                Name = "sp.MC.ProjectGUID",
                Value = projectGuid,
                Group = "Project Information",
                DataType = "Text",
                Unit = null,
                ElementId = projInfo?.Id?.ToString()
            });
            
            // Track existing parameters to avoid duplicates
            var addedParameterNames = new HashSet<string>();
            addedParameterNames.Add("sp.MC.ProjectGUID"); // Already added this one
            
            // First process Project Information parameters according to mapping rules
            CollectCategoryParameters(doc, projInfo, "Project Information", addedParameterNames, request);
            
            // Process Energy Analysis parameters if available
            // For now, we'll have to look in Project Info, but in the future this could be in a different element
            CollectCategoryParameters(doc, projInfo, "Energy Analysis", addedParameterNames, request);
            
            // Collect from other categories based on mapping rules
            // TODO: Implement collection from other elements beyond ProjectInfo when needed
            
            return request;
        }
        
        /// <summary>
        /// Collects parameters for a specific category based on mapping rules
        /// </summary>
        /// <param name="doc">Revit document</param>
        /// <param name="element">Element to collect parameters from</param>
        /// <param name="category">Category name</param>
        /// <param name="addedParameterNames">Set of already added parameter names</param>
        /// <param name="request">SyncRequest to add parameters to</param>
        public void CollectCategoryParameters(
            Document doc, 
            Element element, 
            string category, 
            HashSet<string> addedParameterNames, 
            SyncRequest request)
        {
            // Get all mapping rules for this category that should be synced to web
            var categoryRules = _mappingConfig.RevitToWebRules
                .Where(r => string.Equals(r.RevitCategory, category, StringComparison.OrdinalIgnoreCase))
                .ToList();
                
            if (categoryRules.Count == 0)
                return; // No rules for this category
                
            // First handle the specific mapped parameters
            foreach (var rule in categoryRules)
            {
                // Skip if we've already added this parameter
                if (addedParameterNames.Contains(rule.RevitParameterName))
                    continue;
                    
                // Find the parameter
                Parameter param = element.LookupParameter(rule.RevitParameterName);
                if (param == null)
                    continue; // Parameter not found
                
                // Get parameter value with proper type
                object value = GetParameterTypedValue(param);
                if (value == null)
                    continue; // Skip null/empty values
                    
                string dataType = GetParameterDataType(param);
                
                // Get parameter GUID - use actual GUID for shared parameters,
                // generate stable GUID for project parameters
                string paramGuid;
                if (param.IsShared)
                {
                    paramGuid = param.GUID.ToString();
                }
                else
                {
                    // For non-shared parameters, generate a stable GUID based on the parameter name
                    // This ensures the same parameter always gets the same GUID across syncs
                    paramGuid = GenerateStableGuid(rule.RevitParameterName);
                    Logger.LogInfo($"Generated stable GUID for non-shared parameter: {rule.RevitParameterName}");
                }
                
                request.Parameters.Add(new ParameterData
                {
                    Guid = paramGuid,
                    Name = rule.RevitParameterName,
                    Value = value,
                    Group = category,
                    DataType = dataType,
                    Unit = GetParameterUnitString(param),
                    ElementId = element?.Id?.ToString()
                });
                
                // Mark as added
                addedParameterNames.Add(rule.RevitParameterName);
                
                // Log for debugging
                Logger.LogJson(
                    new { Action = "Mapped Parameter", Parameter = rule.RevitParameterName, WebField = rule.WebAppField, Value = value },
                    "parameter_mapping");
            }
            
            // Now add any other parameters from this category not explicitly mapped but that might be useful
            foreach (Parameter param in element.Parameters)
            {
                string name = param.Definition.Name;
                
                // Skip if we've already added this parameter or if it has an empty value
                if (addedParameterNames.Contains(name))
                    continue;
                    
                // Only add parameters whose names start with "MC." or "sp.MC." for custom parameters
                if (!name.StartsWith("MC.") && !name.StartsWith("sp.MC."))
                    continue;
                    
                addedParameterNames.Add(name);
                
                object value = GetParameterTypedValue(param);
                if (value == null)
                    continue; // Skip empty values for unmapped parameters
                    
                string dataType = GetParameterDataType(param);
                
                // Get GUID - shared parameters have real GUIDs, others get stable generated ones
                string unmappedGuid;
                if (param.IsShared)
                {
                    unmappedGuid = param.GUID.ToString();
                }
                else
                {
                    unmappedGuid = GenerateStableGuid(name);
                }
                
                // Add parameter data
                request.Parameters.Add(new ParameterData
                {
                    Guid = unmappedGuid,
                    Name = name,
                    Value = value,
                    Group = category,
                    DataType = dataType,
                    Unit = GetParameterUnitString(param),
                    ElementId = element?.Id?.ToString()
                });
                
                // Log for debugging
                Logger.LogJson(
                    new { Action = "Additional Parameter", Parameter = name, Value = value },
                    "parameter_mapping");
            }
        }
        
        /// <summary>
        /// Finds a parameter in the Revit document by category and name, using mapping rules if needed
        /// </summary>
        /// <param name="doc">Revit document</param>
        /// <param name="category">Category name</param>
        /// <param name="name">Parameter name</param>
        /// <returns>Found parameter or null if not found</returns>
        public Parameter FindParameter(Document doc, string category, string name)
        {
            if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(name))
                return null;
                
            // Check if this is a web parameter that needs mapping to Revit
            var mappingRule = _mappingConfig.GetRuleForWebField(name);
            if (mappingRule != null)
            {
                // Use the mapped Revit parameter name and category
                category = mappingRule.RevitCategory;
                name = mappingRule.RevitParameterName;
                
                // Log the mapping for debugging
                Logger.LogJson(
                    new { Action = "WebToRevit Parameter Mapping", WebField = name, RevitCategory = category, RevitParameter = name },
                    "parameter_mapping");
            }
            
            // Find the element based on category
            Element element = null;
            if (category.Equals("Project Information", StringComparison.OrdinalIgnoreCase))
            {
                element = doc.ProjectInformation;
            }
            else if (category.Equals("Energy Analysis", StringComparison.OrdinalIgnoreCase))
            {
                // For now, we'll use Project Info for Energy Analysis parameters too
                // In a future implementation, this could be more specific
                element = doc.ProjectInformation;
            }
            // TODO: Add support for other parameter categories and element types
            
            // If we found the element, look for the parameter
            if (element != null)
            {
                return element.LookupParameter(name);
            }
            
            return null;
        }
        
        /// <summary>
        /// Applies a parameter value based on its data type
        /// </summary>
        /// <param name="parameter">Parameter to apply value to</param>
        /// <param name="value">Value to apply</param>
        /// <param name="dataType">Data type of the value</param>
        public void ApplyParameterValue(Parameter parameter, string value, string dataType)
        {
            if (parameter == null || parameter.IsReadOnly)
                return;
                
            switch (dataType?.ToLower())
            {
                case "integer":
                    if (int.TryParse(value, out int intValue))
                        parameter.Set(intValue);
                    break;
                    
                case "double":
                case "number":
                    if (double.TryParse(value, out double doubleValue))
                        parameter.Set(doubleValue);
                    break;
                    
                case "boolean":
                case "bool":
                    if (bool.TryParse(value, out bool boolValue))
                        parameter.Set(boolValue ? 1 : 0);
                    break;
                    
                case "string":
                case "text":
                default:
                    parameter.Set(value);
                    break;
            }
        }
        
        /// <summary>
        /// Gets the unit information for a parameter as a string
        /// </summary>
        /// <param name="param">Parameter to get unit for</param>
        /// <returns>Unit as string or empty string if not applicable</returns>
        private string GetParameterUnitString(Parameter param)
        {
            if (param == null)
                return string.Empty;
                
            try
            {
                // Try to get the unit information in a version-compatible way
                switch (param.StorageType)
                {
                    case StorageType.Double:
                        // For numeric parameters, try to use the AsValueString which includes the unit
                        string valueWithUnit = param.AsValueString();
                        if (!string.IsNullOrEmpty(valueWithUnit))
                        {
                            // Try to extract unit from the value string
                            // Common format is "value unit" like "10.5 ft" or "23.4 m²"
                            string[] parts = valueWithUnit.Split(new[] { ' ' }, 2);
                            if (parts.Length > 1)
                            {
                                return parts[1].Trim(); // Return just the unit portion
                            }
                        }
                        
                        // Fallback: use a basic classification based on parameter name
                        string paramName = param.Definition?.Name?.ToLower() ?? string.Empty;
                        if (paramName.Contains("area"))
                            return "m²";
                        else if (paramName.Contains("volume"))
                            return "m³";
                        else if (paramName.Contains("length") || paramName.Contains("width") || 
                                 paramName.Contains("height") || paramName.Contains("depth"))
                            return "mm";
                        else if (paramName.Contains("temp") || paramName.Contains("temperature"))
                            return "°C";
                        
                        return "Number"; // Generic numeric indicator
                        
                    case StorageType.Integer:
                        // For integer parameters, just identify them as Integer type
                        return "Integer";
                        
                    case StorageType.String:
                        // For string parameters, no unit is applicable
                        return "Text";
                        
                    case StorageType.ElementId:
                        // For element id parameters, indicate it's a reference
                        return "ElementRef";
                        
                    default:
                        return string.Empty;
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the entire operation
                Utils.Logger.LogError($"Error getting parameter unit: {ex.Message}");
            }
            
            return string.Empty;
        }
        
        /// <summary>
        /// Gets the data type of a parameter as a string
        /// </summary>
        /// <param name="param">Parameter to get data type for</param>
        /// <returns>Data type as string</returns>
        private string GetParameterDataType(Parameter param)
        {
            if (param == null)
                return "Text";
                
            switch (param.StorageType)
            {
                case StorageType.Integer:
                    return "Integer";
                case StorageType.Double:
                    return "Number";
                case StorageType.String:
                    return "Text";
                case StorageType.ElementId:
                    return "ElementId";
                default:
                    return "Text";
            }
        }
        
        /// <summary>
        /// Gets the parameter value with proper type (int, double, or string)
        /// Returns null if the parameter has no value
        /// </summary>
        /// <param name="param">Parameter to get value from</param>
        /// <returns>Typed value or null</returns>
        private object GetParameterTypedValue(Parameter param)
        {
            if (param == null || !param.HasValue)
                return null;
                
            try
            {
                switch (param.StorageType)
                {
                    case StorageType.Integer:
                        int intValue = param.AsInteger();
                        return intValue;
                        
                    case StorageType.Double:
                        double doubleValue = param.AsDouble();
                        // Skip if value is essentially zero or invalid
                        if (double.IsNaN(doubleValue) || double.IsInfinity(doubleValue))
                            return null;
                        return doubleValue;
                        
                    case StorageType.String:
                        string stringValue = param.AsString();
                        // Skip empty or whitespace strings
                        if (string.IsNullOrWhiteSpace(stringValue) || stringValue.Trim() == "-")
                            return null;
                        return stringValue;
                        
                    case StorageType.ElementId:
                        ElementId elementId = param.AsElementId();
                        if (elementId == null || elementId.Equals(ElementId.InvalidElementId))
                            return null;
                        // Return the element ID as a string to avoid API version incompatibility
                        return elementId.ToString();
                        
                    default:
                        // Fallback to string representation
                        string fallbackValue = param.AsValueString() ?? param.AsString();
                        if (string.IsNullOrWhiteSpace(fallbackValue))
                            return null;
                        return fallbackValue;
                }
            }
            catch (Exception ex)
            {
                Utils.Logger.LogError($"Error getting typed parameter value for '{param.Definition?.Name}': {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Generates a stable GUID for a parameter based on its name
        /// This ensures the same parameter name always produces the same GUID
        /// </summary>
        /// <param name="parameterName">The parameter name</param>
        /// <returns>A stable GUID string</returns>
        private string GenerateStableGuid(string parameterName)
        {
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                return Guid.NewGuid().ToString(); // Fallback to random GUID
            }
            
            // Use a deterministic hash to generate a stable GUID
            // This ensures the same parameter name always produces the same GUID
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes($"MillerCraft_{parameterName}");
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                
                // Convert the hash to a GUID format
                return new Guid(hashBytes).ToString();
            }
        }
    }
}
