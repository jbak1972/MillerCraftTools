# Revit Plugin: Shared Parameters Technical Implementation Guide

**Date:** October 22, 2025  
**Component:** Miller Craft Tools Revit Plugin  
**Author:** Cascade AI Assistant

---

## Overview

This document provides technical implementation details for integrating web-based shared parameters management into the Miller Craft Tools Revit plugin.

---

## Architecture

### Components to Implement

1. **SharedParametersService.cs** - Core service for parameter management
2. **ParameterMigrationService.cs** - Handles migration from Parameters Service
3. **SyncSharedParametersCommand.cs** - User command to sync parameters
4. **ParameterApplicationService.cs** - Applies parameters to projects/families

### Integration Points

- **Existing:** `HttpClientHelper.cs`, `AuthenticationService.cs`
- **Updates:** `SetupStandardsCommand.cs`, `ParameterMapping.cs`
- **New:** Services listed above

---

## Implementation Details

### 1. SharedParametersService.cs

**Location:** `Services/SharedParametersService.cs`

**Purpose:** Manages download, caching, and loading of shared parameters file

**Key Methods:**

```csharp
public class SharedParametersService
{
    private readonly HttpClientHelper _httpClient;
    private readonly string _cacheDirectory;
    private readonly string _cacheFilePath;
    
    public SharedParametersService()
    {
        _cacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Miller Craft Tools",
            "SharedParameters"
        );
        _cacheFilePath = Path.Combine(_cacheDirectory, "SharedParameters.txt");
        
        if (!Directory.Exists(_cacheDirectory))
            Directory.CreateDirectory(_cacheDirectory);
    }
    
    /// <summary>
    /// Downloads latest shared parameters file from web app
    /// </summary>
    public async Task<string> DownloadSharedParametersFileAsync(string authToken)
    {
        string url = "/api/shared-parameters/download";
        string content = await _httpClient.GetAsync(url, authToken);
        
        // Save to cache
        File.WriteAllText(_cacheFilePath, content);
        
        return _cacheFilePath;
    }
    
    /// <summary>
    /// Gets cached file path, downloads if needed
    /// </summary>
    public async Task<string> GetSharedParametersFileAsync(string authToken, bool forceDownload = false)
    {
        if (forceDownload || !File.Exists(_cacheFilePath))
        {
            return await DownloadSharedParametersFileAsync(authToken);
        }
        return _cacheFilePath;
    }
    
    /// <summary>
    /// Loads shared parameters file into Revit application
    /// </summary>
    public DefinitionFile LoadIntoRevit(Autodesk.Revit.ApplicationServices.Application app, string filePath)
    {
        app.SharedParametersFilename = filePath;
        return app.OpenSharedParameterFile();
    }
    
    /// <summary>
    /// Gets parameter definition by name
    /// </summary>
    public ExternalDefinition GetDefinition(DefinitionFile defFile, string parameterName)
    {
        foreach (DefinitionGroup group in defFile.Groups)
        {
            Definition def = group.Definitions.get_Item(parameterName);
            if (def != null && def is ExternalDefinition extDef)
                return extDef;
        }
        return null;
    }
}
```

### 2. ParameterApplicationService.cs

**Location:** `Services/ParameterApplicationService.cs`

**Purpose:** Applies shared parameters to projects and families

**Key Methods:**

```csharp
public class ParameterApplicationService
{
    /// <summary>
    /// Binds shared parameter to project categories
    /// </summary>
    public bool BindParameterToProject(
        Document doc,
        ExternalDefinition definition,
        List<BuiltInCategory> categories,
        bool isInstance,
        ForgeTypeId parameterGroup = null)
    {
        if (parameterGroup == null)
            parameterGroup = GroupTypeId.IdentityData;
        
        CategorySet categorySet = doc.Application.Create.NewCategorySet();
        
        foreach (var builtInCategory in categories)
        {
            Category category = doc.Settings.Categories.get_Item(builtInCategory);
            if (category != null)
                categorySet.Insert(category);
        }
        
        if (categorySet.IsEmpty)
            return false;
        
        Binding binding = isInstance 
            ? doc.Application.Create.NewInstanceBinding(categorySet) as Binding
            : doc.Application.Create.NewTypeBinding(categorySet) as Binding;
        
        // Try ReInsert first (for existing), then Insert
        if (!doc.ParameterBindings.ReInsert(definition, binding, parameterGroup))
        {
            return doc.ParameterBindings.Insert(definition, binding, parameterGroup);
        }
        
        return true;
    }
    
    /// <summary>
    /// Adds parameter to family
    /// </summary>
    public FamilyParameter AddParameterToFamily(
        Document familyDoc,
        string parameterName,
        ForgeTypeId parameterGroup,
        ForgeTypeId parameterType,
        bool isInstance)
    {
        // Check if parameter already exists
        FamilyParameter existingParam = familyDoc.FamilyManager.get_Parameter(parameterName);
        if (existingParam != null)
            return existingParam;
        
        // Add new parameter
        return familyDoc.FamilyManager.AddParameter(
            parameterName,
            parameterGroup,
            parameterType,
            isInstance
        );
    }
}
```

---

## Migration Strategy

### ParameterMigrationService.cs

**Location:** `Services/ParameterMigrationService.cs`

**Purpose:** Migrates parameters from Parameters Service to new shared parameters

```csharp
public class ParameterMigrationService
{
    private readonly HttpClientHelper _httpClient;
    private readonly ParameterApplicationService _applicationService;
    
    public class MigrationMapping
    {
        public string OldGuid { get; set; }
        public string NewGuid { get; set; }
        public string ParameterName { get; set; }
    }
    
    /// <summary>
    /// Downloads migration mapping from web app
    /// </summary>
    public async Task<List<MigrationMapping>> GetMigrationMappingsAsync(string authToken)
    {
        string url = "/api/shared-parameters/migration-map";
        string json = await _httpClient.GetAsync(url, authToken);
        // Deserialize JSON to List<MigrationMapping>
        return JsonConvert.DeserializeObject<List<MigrationMapping>>(json);
    }
    
    /// <summary>
    /// Migrates project parameters from old to new GUIDs
    /// </summary>
    public MigrationReport MigrateProjectParameters(Document doc, List<MigrationMapping> mappings)
    {
        var report = new MigrationReport();
        
        using (Transaction tx = new Transaction(doc, "Migrate Parameters"))
        {
            tx.Start();
            
            foreach (var mapping in mappings)
            {
                try
                {
                    // Get old parameter value
                    Element projectInfo = new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_ProjectInformation)
                        .FirstElement();
                    
                    Parameter oldParam = projectInfo.LookupParameter(mapping.ParameterName);
                    if (oldParam == null)
                    {
                        report.Skipped.Add($"{mapping.ParameterName}: Not found");
                        continue;
                    }
                    
                    string value = GetParameterValueAsString(oldParam);
                    
                    // Apply new parameter (must be bound already via SharedParametersService)
                    Parameter newParam = projectInfo.LookupParameter(mapping.ParameterName);
                    if (newParam != null)
                    {
                        SetParameterValueFromString(newParam, value);
                        report.Succeeded.Add(mapping.ParameterName);
                    }
                    else
                    {
                        report.Failed.Add($"{mapping.ParameterName}: New parameter not bound");
                    }
                }
                catch (Exception ex)
                {
                    report.Failed.Add($"{mapping.ParameterName}: {ex.Message}");
                }
            }
            
            tx.Commit();
        }
        
        return report;
    }
    
    private string GetParameterValueAsString(Parameter param)
    {
        switch (param.StorageType)
        {
            case StorageType.String:
                return param.AsString() ?? "";
            case StorageType.Integer:
                return param.AsInteger().ToString();
            case StorageType.Double:
                return param.AsDouble().ToString();
            case StorageType.ElementId:
                return param.AsElementId().ToString();
            default:
                return "";
        }
    }
    
    private void SetParameterValueFromString(Parameter param, string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        
        switch (param.StorageType)
        {
            case StorageType.String:
                param.Set(value);
                break;
            case StorageType.Integer:
                if (int.TryParse(value, out int intVal))
                    param.Set(intVal);
                break;
            case StorageType.Double:
                if (double.TryParse(value, out double dblVal))
                    param.Set(dblVal);
                break;
        }
    }
}

public class MigrationReport
{
    public List<string> Succeeded { get; set; } = new List<string>();
    public List<string> Failed { get; set; } = new List<string>();
    public List<string> Skipped { get; set; } = new List<string>();
    
    public string GenerateSummary()
    {
        return $"Migration Complete:\n" +
               $"  Succeeded: {Succeeded.Count}\n" +
               $"  Failed: {Failed.Count}\n" +
               $"  Skipped: {Skipped.Count}";
    }
}
```

---

## Command Implementation

### SyncSharedParametersCommand.cs

**Location:** `Command/SyncSharedParametersCommand.cs`

```csharp
[Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
public class SyncSharedParametersCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIDocument uidoc = commandData.Application.ActiveUIDocument;
        Document doc = uidoc.Document;
        
        try
        {
            // 1. Get authentication token
            var authService = new AuthenticationService();
            string token = authService.GetStoredToken();
            
            if (string.IsNullOrEmpty(token))
            {
                Autodesk.Revit.UI.TaskDialog.Show("Error", "Not authenticated. Please log in first.");
                return Result.Failed;
            }
            
            // 2. Download shared parameters file
            var sharedParamService = new SharedParametersService();
            string filePath = await sharedParamService.GetSharedParametersFileAsync(token, forceDownload: true);
            
            // 3. Load into Revit
            DefinitionFile defFile = sharedParamService.LoadIntoRevit(commandData.Application.Application, filePath);
            
            if (defFile == null)
            {
                Autodesk.Revit.UI.TaskDialog.Show("Error", "Failed to load shared parameters file.");
                return Result.Failed;
            }
            
            // 4. Apply parameters to project
            ApplyParametersToProject(doc, defFile, sharedParamService);
            
            Autodesk.Revit.UI.TaskDialog.Show("Success", "Shared parameters synchronized successfully.");
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            Autodesk.Revit.UI.TaskDialog.Show("Error", $"Failed to sync parameters: {ex.Message}");
            return Result.Failed;
        }
    }
    
    private void ApplyParametersToProject(Document doc, DefinitionFile defFile, SharedParametersService service)
    {
        var applicationService = new ParameterApplicationService();
        
        // Example: Apply StandardsVersion parameter
        ExternalDefinition def = service.GetDefinition(defFile, "StandardsVersion");
        if (def != null)
        {
            using (Transaction tx = new Transaction(doc, "Apply Shared Parameters"))
            {
                tx.Start();
                
                bool success = applicationService.BindParameterToProject(
                    doc,
                    def,
                    new List<BuiltInCategory> { BuiltInCategory.OST_ProjectInformation },
                    isInstance: true,
                    GroupTypeId.IdentityData
                );
                
                tx.Commit();
            }
        }
    }
}
```

---

## Integration with Existing Code

### Update SetupStandardsCommand.cs

Replace the temporary shared parameters file creation with:

```csharp
// OLD CODE (remove):
// string tempPath = Path.Combine(Path.GetTempPath(), "TempSharedParameters.txt");
// File.WriteAllText(tempPath, "");
// doc.Application.SharedParametersFilename = tempPath;

// NEW CODE:
var authService = new AuthenticationService();
string token = authService.GetStoredToken();

var sharedParamService = new SharedParametersService();
string filePath = await sharedParamService.GetSharedParametersFileAsync(token);
DefinitionFile defFile = sharedParamService.LoadIntoRevit(doc.Application, filePath);
```

---

## Testing Strategy

### Unit Tests
1. File download and caching
2. Parameter definition lookup
3. Binding to categories
4. Migration mapping application

### Integration Tests
1. End-to-end parameter sync
2. Project parameter application
3. Family parameter addition
4. Migration with real project data

### Manual Testing
1. Test in Revit 2024, 2025, 2026
2. Verify parameters appear in UI
3. Check parameter values persist
4. Validate family parameter functionality

---

## Error Handling

### Network Errors
- Retry logic with exponential backoff
- Fallback to cached file
- Clear user messaging

### File Parsing Errors
- Validate file format before loading
- Log detailed errors
- Provide recovery options

### Permission Errors
- Check document is not read-only
- Verify user has edit permissions
- Handle transaction failures gracefully

---

## Performance Optimization

1. **Caching:** Keep downloaded file cached locally
2. **Async Operations:** Use async/await for network calls
3. **Batch Operations:** Apply multiple parameters in single transaction
4. **Progress Reporting:** Show progress for long operations

---

## Next Steps

1. Implement `SharedParametersService.cs`
2. Implement `ParameterApplicationService.cs`
3. Implement `ParameterMigrationService.cs`
4. Update `SetupStandardsCommand.cs`
5. Create `SyncSharedParametersCommand.cs`
6. Test with web app integration
7. Perform migration dry run

---

*Refer to SHARED_PARAMETERS_IMPLEMENTATION_PLAN.md for overall project plan and WEBAPP_SHARED_PARAMETERS_PROMPT.md for web app coordination.*
