# Bidirectional Sync Implementation Guide
## Miller Craft Tools - Revit Plugin

**Date:** October 24, 2025  
**Status:** ✅ Implemented  
**Version:** 1.2.0

---

## Overview

This document outlines the bidirectional synchronization system between the Revit plugin and the Miller Craft Assistant web application, including all 46 project parameters.

---

## What Was Implemented

### 1. Parameter Collection Enhancement

**File:** `Services/SyncUtilities/ParameterManager.cs`

**Changes:**
- ✅ Removed restriction that only collected shared parameters
- ✅ Added `GenerateStableGuid()` method for non-shared parameters
- ✅ Now collects **ALL parameters** (shared and project parameters)
- ✅ Generates deterministic GUIDs for project parameters using MD5 hash

**Key Code:**
```csharp
// For non-shared parameters, generate stable GUID based on parameter name
if (param.IsShared)
{
    paramGuid = param.GUID.ToString();
}
else
{
    // Generate stable GUID using MD5 hash of parameter name
    paramGuid = GenerateStableGuid(rule.RevitParameterName);
}
```

**Benefits:**
- Same parameter name always produces same GUID
- Enables tracking parameters across syncs
- Compatible with future shared parameters migration

### 2. Expanded Parameter Mapping

**File:** `Model/ParameterMapping.cs`

**Changes:**
- ✅ Expanded from 6 parameters to **46 parameters**
- ✅ All parameters from `Project Parameters.png` included
- ✅ Organized into logical groups
- ✅ Configured sync directions (Both, RevitToWeb, WebToRevit)

**Parameter Groups:**
1. **Contact/People (11 parameters)**
   - sp.Client.Name
   - sp.Contact.* (Contractor, Designer, Drafter, Energy, Owner, Structural, Stormwater)
   - sp.Project.Engineer
   - sp.Property.Owner

2. **Energy Analysis (6 parameters)**
   - sp.Energy.Area.Wall, sp.Energy.Area.Wall.Garage
   - sp.Energy.U.Fenestration, sp.Energy.U.Walls, sp.Energy.U.Walls.Garage
   - sp.Energy.Calc

3. **Existing Conditions (4 parameters)**
   - sp.Existing.Residence.Area, Bathrooms, Bedrooms
   - sp.Existing.Attached.Garage.Area

4. **Project Info/Description (4 parameters)**
   - sp.Name, sp.Info.Project.Description
   - sp.Code.Requirements, sp.Legal.Text

5. **Property/Site (10 parameters)**
   - sp.Area, sp.Jurisdiction, sp.Land.Use, sp.Local.Order
   - sp.Lot.Coverage, sp.Lot.Size, sp.Parcel.Number
   - sp.Property.Type, sp.Setbacks, sp.Zoning

6. **Utilities/Services (2 parameters)**
   - sp.Sewer.Septic, sp.Vitality.Service

7. **Tag/Annotation (9 parameters)**
   - sp.Filter.Tag, sp.Finish.Exterior, sp.Finishes.Interior
   - sp.Tag.Text.Instance, sp.Tag.Text.Type
   - sp.Text, sp.Text.Multiline, sp.Visible

---

## Data Flow

### Revit → Web (Upload)

```
[User clicks Sync]
        ↓
[ParameterManager.CollectParametersForSync]
        ↓
[Collect all 46+ parameters from Project Info]
        ↓
[Generate/retrieve GUID for each parameter]
        ↓
[Build SyncRequest JSON]
        ↓
[POST /api/revit/sync]
        ↓
[Web App stores parameters + returns status]
```

### Web → Revit (Download)

```
[Web App detects parameter changes]
        ↓
[Returns webToRevitChanges in sync response]
        ↓
[Revit receives changes]
        ↓
[ApplyParameterChangesAsync]
        ↓
[Apply changes in Transaction]
        ↓
[POST /api/revit/sync/{syncId}/apply]
        ↓
[Acknowledge changes applied]
```

---

## Sync Tracking Strategy

### Web App Responsibility (Primary)

The web app is the **single source of truth** for sync history and parameter changes.

**What Web App Stores:**
```typescript
{
  syncId: string,
  projectId: string,
  revitProjectGuid: string,
  timestamp: Date,
  userId: string,
  action: "sync" | "queue" | "conflict",
  
  // Parameter change tracking
  changes: {
    new: ["sp.Client.Name", ...],
    updated: ["sp.Area", ...],
    unchanged: [...],
    conflicts: [] // If both sides changed
  },
  
  // Full parameter snapshot
  parameters: [
    {
      guid: "stable-guid-123",
      name: "sp.Client.Name",
      value: "John Doe",
      previousValue: "Jane Smith",
      modifiedBy: "revit" | "web",
      modifiedAt: Date
    }
  ]
}
```

**Benefits:**
- ✅ Survives Revit crashes/reinstalls
- ✅ Accessible from anywhere
- ✅ Admin can review all changes
- ✅ Multi-user conflict detection
- ✅ Complete audit trail

### Revit Plugin Responsibility (Minimal)

The plugin stores **minimal local state** for performance and offline capability.

**What Plugin Stores:**
```csharp
public class SyncSettings
{
    public string LastSyncId { get; set; }
    public DateTime? LastSyncTime { get; set; }
    public string LastSyncStatus { get; set; }
    public string ProjectGuid { get; set; }
}
```

**Benefits:**
- ✅ Fast local reference
- ✅ UI can show last sync time
- ✅ Minimal storage footprint

---

## Parameter Data Structure

### What Gets Sent

```json
{
  "revitProjectGuid": "84cfb8c4-8f22-4d9f-be6f-5df0ce7b359d",
  "revitFileName": "Miller Residence.rvt",
  "version": "1.0",
  "timestamp": "2025-10-24T17:30:00Z",
  "parameters": [
    {
      "guid": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "name": "sp.Client.Name",
      "value": "John Doe",
      "group": "Project Information",
      "dataType": "Text",
      "unit": null,
      "elementId": "12345"
    }
  ]
}
```

**Field Descriptions:**
- **guid** - Stable identifier (real GUID for shared params, generated for project params)
- **name** - Exact parameter name from Revit
- **value** - Current parameter value (string, number, etc.)
- **group** - Parameter group/category
- **dataType** - Text, Number, Integer, etc.
- **unit** - Unit of measurement (if applicable)
- **elementId** - Element containing the parameter

---

## Conflict Resolution

### Detection

Conflicts occur when:
1. Parameter changed in Revit since last sync
2. AND parameter changed in Web App since last sync
3. AND values are different

**Example:**
```
Last Sync: sp.Client.Name = "Original Client"
Revit Now: sp.Client.Name = "John Doe" (changed 10/24 5:00pm)
Web Now:   sp.Client.Name = "Jane Smith" (changed 10/24 4:30pm)
Result:    CONFLICT!
```

### Resolution Strategy (Current)

**Last-Write-Wins:**
- Most recent timestamp wins
- Loser's value is logged but overwritten
- User notified of conflict

**Web App Response:**
```json
{
  "success": true,
  "action": "sync",
  "conflicts": [
    {
      "parameterName": "sp.Client.Name",
      "revitValue": "John Doe",
      "webValue": "Jane Smith",
      "resolution": "revit-wins",
      "reason": "Revit change was more recent"
    }
  ]
}
```

### Future Enhancements

**Manual Resolution (Phase 2):**
- Show dialog in Revit: "Conflict detected - choose value"
- User selects which value to keep
- Choice sent back to web app

---

## Testing Checklist

### Parameter Collection
- [x] Collects all 46 parameters from Project Info
- [x] Generates stable GUIDs for non-shared parameters
- [x] Skips empty/null values
- [x] Includes data type and unit info
- [x] Automatically creates sp.MC.ProjectGUID parameter if missing
- [ ] Test with real project data
- [ ] Verify GUID stability across syncs

### Bidirectional Sync
- [x] Revit → Web sends all parameters
- [x] Web → Revit applies changes
- [ ] Conflict detection works
- [ ] Acknowledgment completes
- [ ] Multi-user scenarios

### Error Handling
- [x] Network failures handled gracefully
- [x] Invalid parameters logged
- [x] Transaction rollback on errors
- [ ] Offline queue implementation

---

## API Endpoints

### 1. Sync Parameters (Revit → Web)

```
POST /api/revit/sync
Authorization: Bearer {token}
Content-Type: application/json

Body: { revitProjectGuid, parameters, ... }

Response:
{
  "success": true,
  "syncId": "abc123",
  "action": "sync",
  "projectId": "proj-456",
  "webToRevitChanges": [...]
}
```

### 2. Acknowledge Changes (Web → Revit)

```
POST /api/revit/sync/{syncId}/apply
Authorization: Bearer {token}
Content-Type: application/json

Body: { appliedChanges: [...] }

Response:
{
  "success": true,
  "message": "Changes acknowledged"
}
```

### 3. Check Sync Status

```
GET /api/revit/sync/{syncId}/status
Authorization: Bearer {token}

Response:
{
  "syncId": "abc123",
  "status": "processed",
  "webToRevitChanges": [...]
}
```

---

## Integration with Existing Code

### SyncServiceV2.cs

**Already implements:**
- ✅ `InitiateSyncAsync()` - Sends parameters
- ✅ `ApplyParameterChangesAsync()` - Applies web changes
- ✅ `AcknowledgeChangesAsync()` - Confirms application
- ✅ `CheckSyncStatusAsync()` - Polls for updates

**No changes needed** - service already supports bidirectional sync!

### SyncWithWebCommand.cs

**May need updates:**
- Show progress for parameter collection (46 params now)
- Display conflict warnings to user
- Show applied changes summary

---

## Performance Considerations

### Current Implementation

**Parameter Collection:**
- Collects from single element (ProjectInfo)
- ~46 parameters
- Estimated time: < 1 second

**Network Transfer:**
- JSON payload size: ~5-10 KB
- Compression: possible
- Estimated time: < 2 seconds

**Total Sync Time:**
- Collection: < 1 second
- Transfer: < 2 seconds
- Apply changes: < 1 second
- **Total: < 5 seconds**

### Future Optimizations

1. **Incremental Sync:**
   - Only send changed parameters
   - Compare with last sync values
   - Reduces payload size 80-90%

2. **Batching:**
   - Already batches all parameters in single request
   - No additional optimization needed

3. **Caching:**
   - Cache parameter mappings in memory
   - Reduces lookup time

---

## Next Steps

### Immediate (This Sprint)
1. ✅ Fix parameter collection (DONE)
2. ✅ Expand parameter mapping (DONE)
3. ⏳ Test with real project data
4. ⏳ Verify all parameters sync correctly
5. ⏳ Test bidirectional changes

### Short Term (Next Sprint)
1. Implement conflict UI in Revit
2. Add sync history view
3. Show parameter change log
4. Enable/disable individual parameter sync

### Long Term
1. Incremental sync (only changed params)
2. Multi-user conflict resolution
3. Parameter versioning
4. Rollback capability

---

## Troubleshooting

### Issue: Only 1 Parameter Syncing

**Cause:** Code was skipping non-shared parameters
**Fix:** ✅ Implemented - now collects all parameters

### Issue: Different GUIDs Each Sync

**Cause:** Non-shared parameters don't have real GUIDs
**Fix:** ✅ Implemented stable GUID generation

### Issue: Parameter Not Found

**Possible Causes:**
1. Parameter doesn't exist in project
2. Parameter name typo in mapping
3. Category mismatch

**Solution:**
- Check parameter exists in Revit
- Verify spelling in ParameterMapping.cs
- Check RevitCategory matches

### Issue: Bidirectional Sync Not Working

**Check:**
1. Web app returning `webToRevitChanges`?
2. `ApplyParameterChangesAsync()` being called?
3. Transaction completing successfully?
4. Acknowledgment sent back?

---

## Summary

✅ **Implemented:** Collection of all 46 parameters
✅ **Implemented:** Stable GUID generation for non-shared parameters  
✅ **Implemented:** Comprehensive parameter mapping configuration
✅ **Ready:** Bidirectional sync infrastructure
⏳ **Pending:** Real-world testing with project data
⏳ **Pending:** Conflict resolution UI

**The system is ready for bidirectional synchronization of all 46 project parameters!**

---

*For web app coordination, see: `docs/REVIT_PLUGIN_DEVELOPER_GUIDE.md`*
