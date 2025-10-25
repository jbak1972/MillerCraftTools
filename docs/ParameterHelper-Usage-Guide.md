# ParameterHelper Usage Guide

## Overview

`ParameterHelper` is a utility class that provides safe methods for working with Revit parameters, preventing common bugs related to empty string handling.

## The Problem

Revit's `Parameter.HasValue` returns `TRUE` even when the string value is empty (`""`), which causes logic errors:

```csharp
// ❌ DANGEROUS CODE - Don't do this!
var param = element.LookupParameter("ParameterName");
if (param != null && param.HasValue)
{
    string value = param.AsString(); // Could be ""!
    UseValue(value); // BUG: Might use empty string
}
```

## The Solution

Use `ParameterHelper` methods which properly check for empty strings:

---

## Common Methods

### 1. Get String Value from Element

```csharp
// Returns null if parameter doesn't exist, is empty, or contains only whitespace
string value = ParameterHelper.GetParameterStringValue(element, "ParameterName");

if (value != null)
{
    // Safe to use - guaranteed non-empty
}
```

### 2. Get String Value from Project Info

```csharp
// Convenient shortcut for project-level parameters
string projectNumber = ParameterHelper.GetProjectInfoStringValue(doc, "Project Number");

if (projectNumber != null)
{
    // Use project number
}
```

### 3. Check if Parameter Has Valid Value

```csharp
// Returns true only if parameter exists and has non-empty value
if (ParameterHelper.HasValidStringValue(element, "ParameterName"))
{
    // Parameter exists and has a valid value
}
```

### 4. Set Parameter Value

```csharp
// Safely set parameter value (requires active transaction)
using (Transaction tx = new Transaction(doc, "Set Parameter"))
{
    tx.Start();
    
    bool success = ParameterHelper.SetParameterStringValue(
        element, 
        "ParameterName", 
        "New Value");
    
    if (success)
    {
        tx.Commit();
    }
    else
    {
        tx.RollBack();
        // Parameter doesn't exist or is read-only
    }
}
```

### 5. Get Typed Parameter Value

```csharp
// Returns typed value (string, int, double, ElementId) or null
object value = ParameterHelper.GetParameterTypedValue(element, "ParameterName");

if (value is string strVal)
{
    // Use string value
}
else if (value is int intVal)
{
    // Use integer value
}
```

### 6. Debug Parameter Status

```csharp
// Logs detailed parameter information for debugging
ParameterHelper.LogParameterStatus(element, "ParameterName", "[DEBUG] ");

// Output:
// [DEBUG] Parameter 'ParameterName': HasValue=true, IsEmpty=true, Value=''
```

---

## Real-World Examples

### Example 1: Project GUID Handling

```csharp
// Get existing GUID or detect empty
string projectGuid = ParameterHelper.GetProjectInfoStringValue(doc, "sp.MC.ProjectGUID");

if (string.IsNullOrWhiteSpace(projectGuid))
{
    // No valid GUID - generate new one
    projectGuid = Guid.NewGuid().ToString();
    
    using (Transaction tx = new Transaction(doc, "Set Project GUID"))
    {
        tx.Start();
        ParameterHelper.SetParameterStringValue(doc.ProjectInformation, "sp.MC.ProjectGUID", projectGuid);
        tx.Commit();
    }
}

// Use projectGuid - guaranteed non-empty
```

### Example 2: Element Property Validation

```csharp
// Check multiple parameters before processing
var elements = new FilteredElementCollector(doc)
    .OfClass(typeof(FamilyInstance))
    .ToElements();

foreach (var elem in elements)
{
    // Only process if all required parameters have values
    if (ParameterHelper.HasValidStringValue(elem, "Mark") &&
        ParameterHelper.HasValidStringValue(elem, "Type Name") &&
        ParameterHelper.HasValidStringValue(elem, "Level"))
    {
        // Process element
    }
}
```

### Example 3: Safe Parameter Collection

```csharp
public List<string> CollectParameterValues(Document doc, string paramName)
{
    var values = new List<string>();
    
    var elements = new FilteredElementCollector(doc)
        .WhereElementIsNotElementType()
        .ToElements();
    
    foreach (var elem in elements)
    {
        // GetParameterStringValue returns null for empty/missing parameters
        string value = ParameterHelper.GetParameterStringValue(elem, paramName);
        
        if (value != null)
        {
            values.Add(value); // Only add valid non-empty values
        }
    }
    
    return values;
}
```

---

## Migration Guide

### Before (Old Pattern):

```csharp
var param = element.LookupParameter("ParameterName");
if (param != null && param.HasValue)
{
    string value = param.AsString();
    if (!string.IsNullOrEmpty(value))
    {
        UseValue(value);
    }
}
```

### After (Using ParameterHelper):

```csharp
string value = ParameterHelper.GetParameterStringValue(element, "ParameterName");
if (value != null)
{
    UseValue(value);
}
```

Much cleaner and safer! ✅

---

## Testing Checklist

When testing parameter-related code, always verify with:

- ✅ **Parameter doesn't exist** (null)
- ✅ **Parameter exists but is empty** (`""`)
- ✅ **Parameter exists with whitespace only** (`"   "`)
- ✅ **Parameter exists with valid value**

`ParameterHelper` handles all these cases correctly.

---

## Benefits

1. **Prevents Empty String Bugs** - Properly checks for empty values
2. **Consistent API** - All parameter operations use same pattern
3. **Better Error Handling** - Built-in validation and logging
4. **Cleaner Code** - Less boilerplate in business logic
5. **Type Safety** - Typed value retrieval with proper conversion

---

## When NOT to Use

For non-string parameters where empty values are valid (integers, booleans, etc.), you may still use the standard Revit API directly. However, `GetParameterTypedValue()` provides a safer alternative for mixed types.

---

## Additional Notes

- All methods handle null inputs gracefully (return null/false)
- Set methods require an active transaction
- Logging methods use the project's Logger utility
- Read-only parameters are detected and handled in Set methods

---

**Created:** October 23, 2025  
**Version:** 1.1.1  
**Author:** Miller Craft Tools Team
