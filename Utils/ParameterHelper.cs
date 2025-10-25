using Autodesk.Revit.DB;
using System;

namespace Miller_Craft_Tools.Utils
{
    /// <summary>
    /// Helper utilities for safely working with Revit parameters
    /// Prevents common pitfalls like trusting HasValue for empty strings
    /// </summary>
    public static class ParameterHelper
    {
        /// <summary>
        /// Safely gets a non-empty string parameter value from an element
        /// </summary>
        /// <param name="element">The element containing the parameter</param>
        /// <param name="parameterName">Name of the parameter to retrieve</param>
        /// <returns>Non-empty string value, or null if parameter doesn't exist or is empty</returns>
        /// <remarks>
        /// CRITICAL: Revit's Parameter.HasValue returns TRUE even for empty strings!
        /// This method properly checks for empty strings after retrieving the value.
        /// </remarks>
        public static string GetParameterStringValue(Element element, string parameterName)
        {
            if (element == null)
                return null;

            var param = element.LookupParameter(parameterName);
            string value = param?.AsString();

            // Check for null parameter, missing value, AND empty string
            if (param != null && param.HasValue && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return null;
        }

        /// <summary>
        /// Safely gets a string parameter value from ProjectInformation
        /// </summary>
        /// <param name="doc">The Revit document</param>
        /// <param name="parameterName">Name of the parameter to retrieve</param>
        /// <returns>Non-empty string value, or null if parameter doesn't exist or is empty</returns>
        public static string GetProjectInfoStringValue(Document doc, string parameterName)
        {
            if (doc?.ProjectInformation == null)
                return null;

            return GetParameterStringValue(doc.ProjectInformation, parameterName);
        }

        /// <summary>
        /// Checks if a parameter exists and has a non-empty value
        /// </summary>
        /// <param name="element">The element containing the parameter</param>
        /// <param name="parameterName">Name of the parameter to check</param>
        /// <returns>True if parameter exists and has non-empty value</returns>
        public static bool HasValidStringValue(Element element, string parameterName)
        {
            return !string.IsNullOrWhiteSpace(GetParameterStringValue(element, parameterName));
        }

        /// <summary>
        /// Safely sets a string parameter value with validation
        /// </summary>
        /// <param name="element">The element containing the parameter</param>
        /// <param name="parameterName">Name of the parameter to set</param>
        /// <param name="value">Value to set (must be non-empty)</param>
        /// <returns>True if successfully set, false otherwise</returns>
        /// <remarks>Requires an active transaction</remarks>
        public static bool SetParameterStringValue(Element element, string parameterName, string value)
        {
            if (element == null || string.IsNullOrWhiteSpace(value))
                return false;

            var param = element.LookupParameter(parameterName);
            if (param == null || param.IsReadOnly)
                return false;

            try
            {
                param.Set(value);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to set parameter '{parameterName}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets a parameter value with type checking and conversion
        /// </summary>
        /// <param name="element">The element containing the parameter</param>
        /// <param name="parameterName">Name of the parameter</param>
        /// <returns>Parameter value as object, or null if invalid</returns>
        public static object GetParameterTypedValue(Element element, string parameterName)
        {
            if (element == null)
                return null;

            var param = element.LookupParameter(parameterName);
            if (param == null || !param.HasValue)
                return null;

            switch (param.StorageType)
            {
                case StorageType.String:
                    string strValue = param.AsString();
                    // Don't return empty strings
                    return string.IsNullOrWhiteSpace(strValue) ? null : strValue;

                case StorageType.Integer:
                    return param.AsInteger();

                case StorageType.Double:
                    return param.AsDouble();

                case StorageType.ElementId:
                    ElementId elemId = param.AsElementId();
                    return elemId != null && elemId != ElementId.InvalidElementId ? elemId : null;

                default:
                    return null;
            }
        }

        /// <summary>
        /// Logs parameter retrieval for debugging
        /// </summary>
        /// <param name="element">The element</param>
        /// <param name="parameterName">Parameter name</param>
        /// <param name="logPrefix">Optional prefix for log message</param>
        public static void LogParameterStatus(Element element, string parameterName, string logPrefix = "")
        {
            if (element == null)
            {
                Logger.LogInfo($"{logPrefix}Element is null");
                return;
            }

            var param = element.LookupParameter(parameterName);
            if (param == null)
            {
                Logger.LogInfo($"{logPrefix}Parameter '{parameterName}' not found on {element.GetType().Name}");
                return;
            }

            string value = param.AsString();
            Logger.LogInfo($"{logPrefix}Parameter '{parameterName}': " +
                          $"HasValue={param.HasValue}, " +
                          $"IsEmpty={string.IsNullOrWhiteSpace(value)}, " +
                          $"Value='{value}'");
        }
    }
}
