# Miller Craft Tools - Implementation Roadmap

**Date:** October 22, 2025  
**Status:** Ready to Begin

---

## 📋 Quick Reference

**Three Key Documents Created:**

1. **`web-sync-consolidation-review.md`** - Original consolidation plan (UI-focused)
2. **`Integration-Gap-Analysis.md`** - Critical API misalignments discovered ⚠️
3. **`IMPLEMENTATION-ROADMAP.md`** (this file) - Complete execution plan

---

## 🎯 The Problem (Summary)

You asked for a review of the web app synchronization implementation. I found:

### UI/UX Issues (Original Request)
- ✅ 5 redundant commands
- ✅ 8 overlapping dialogs  
- ✅ Scattered functionality
- ✅ No sync history/status dashboard

### API Issues (Discovered During Review) ⚠️
- ❌ **Authentication completely wrong** - OAuth2 vs. token paste
- ❌ **Wrong endpoints** - `/api/sync/initiate` vs `/api/revit/sync`
- ❌ **Request/response models don't match** web app spec
- ❌ **Bidirectional sync implemented prematurely** - web app doesn't support it yet
- ❌ **Wrong header format priority** - should use `X-Revit-Token`

---

## 🚨 Critical Decision Required

**We must fix the API integration BEFORE consolidating the UI.**

### Why?
1. Current implementation won't work with the actual web app API
2. UI consolidation would just organize broken functionality
3. Users would be confused by non-functional features
4. Fixing API after UI work means rewriting twice

### The Plan:
```
Phase 0 (NEW): Fix API Integration        [2-3 days] ← START HERE
    ↓
Phase 1: Consolidate UI                   [2-3 days]
    ↓
Phase 2: Remove Redundancy                [1 day]
    ↓
Phase 3: Bidirectional Sync (Future)      [TBD - when web app ready]
```

---

## 📊 Phase 0: API Integration Fix (CRITICAL)

**Goal:** Make the plugin work with the actual web app API

**Duration:** 2-3 days  
**Priority:** 🔴 **MUST DO FIRST**

### Changes Required

#### 1. Authentication (High Impact)

**Remove:**
```csharp
// DELETE THIS
public async Task<string> Authenticate(string username, string password)
{
    // OAuth2 login that doesn't exist
}

public async Task<bool> RefreshToken()
{
    // Token refresh that doesn't exist
}
```

**Replace With:**
```csharp
// SIMPLIFIED AUTH
public async Task<bool> ValidateTokenAsync(string token)
{
    using (var client = new HttpClient())
    {
        client.DefaultRequestHeaders.Add("X-Revit-Token", token);
        var response = await client.GetAsync("https://app.millercraftllc.com/api/revit/test");
        return response.IsSuccessStatusCode;
    }
}
```

**UI Change:**
- Remove: Username/password login form
- Add: Simple "Paste Token" textbox with "Validate" button
- Add: "Get Token" link that opens web app

#### 2. Update API Models

**File:** `Model/SyncApiModels.cs`

**Current Parameter:**
```csharp
public class Parameter
{
    public string Name { get; set; }
    public string Value { get; set; }  // Always string
}
```

**New Parameter:**
```csharp
public class RevitParameter
{
    [JsonProperty("guid")]
    public string Guid { get; set; }  // ADD
    
    [JsonProperty("name")]
    public string Name { get; set; }
    
    [JsonProperty("value")]
    public object Value { get; set; }  // CHANGE: proper type
    
    [JsonProperty("group")]
    public string Group { get; set; }  // ADD
    
    [JsonProperty("dataType")]
    public string DataType { get; set; }  // ADD
}
```

**Current Request:**
```csharp
public class SyncRequest
{
    public string ProjectGuid { get; set; }  // WRONG name
    public List<Parameter> Parameters { get; set; }
    public string RevitFileName { get; set; }
}
```

**New Request:**
```csharp
public class SyncRequest
{
    [JsonProperty("revitProjectGuid")]  // CHANGED
    public string RevitProjectGuid { get; set; }
    
    [JsonProperty("parameters")]
    public List<RevitParameter> Parameters { get; set; }
    
    [JsonProperty("revitFileName")]
    public string RevitFileName { get; set; }
    
    [JsonProperty("version")]  // ADD
    public string Version { get; set; } = "1.0";
    
    [JsonProperty("timestamp")]  // ADD
    public string Timestamp { get; set; }
    
    [JsonProperty("command")]  // ADD (optional)
    public string Command { get; set; }
}
```

**Current Response:**
```csharp
public class SyncResult
{
    public bool Success { get; set; }
    public string SyncId { get; set; }
    public string Status { get; set; }
}
```

**New Response:**
```csharp
public class SyncResponse
{
    [JsonProperty("success")]
    public bool Success { get; set; }
    
    [JsonProperty("action")]  // ADD: "sync" or "queue"
    public string Action { get; set; }
    
    [JsonProperty("message")]
    public string Message { get; set; }
    
    // For existing projects (action: "sync")
    [JsonProperty("projectId")]
    public string ProjectId { get; set; }
    
    [JsonProperty("projectName")]
    public string ProjectName { get; set; }
    
    [JsonProperty("data")]
    public SyncData Data { get; set; }
    
    // For new projects (action: "queue")
    [JsonProperty("queueId")]
    public string QueueId { get; set; }
    
    [JsonProperty("availableProjects")]
    public List<AvailableProject> AvailableProjects { get; set; }
    
    [JsonProperty("timestamp")]
    public string Timestamp { get; set; }
}

public class AvailableProject
{
    [JsonProperty("id")]
    public string Id { get; set; }
    
    [JsonProperty("name")]
    public string Name { get; set; }
    
    [JsonProperty("description")]
    public string Description { get; set; }
}

public class SyncData
{
    [JsonProperty("changesApplied")]
    public int ChangesApplied { get; set; }
}
```

#### 3. Update Endpoints

**File:** `Services/SyncUtilities/ApiEndpointManager.cs`

**Change:**
```csharp
// OLD
public string GetSyncEndpoint() => "/api/sync/initiate";
public string GetStatusEndpoint(string syncId) => $"/api/sync/status/{syncId}";
public string GetApplyEndpoint(string syncId) => $"/api/sync/apply/{syncId}";

// NEW
public string GetTestEndpoint() => "/api/revit/test";
public string GetSyncEndpoint() => "/api/revit/sync";
// Remove GetStatusEndpoint
// Remove GetApplyEndpoint
```

#### 4. Update ParameterManager

**File:** `Services/SyncUtilities/ParameterManager.cs`

**Add methods:**
```csharp
private string GetParameterDataType(Parameter param)
{
    switch (param.StorageType)
    {
        case StorageType.Integer:
            return "Integer";
        case StorageType.Double:
            return "Number";
        case StorageType.String:
            return "Text";
        default:
            return "Unknown";
    }
}

private object GetParameterValue(Parameter param)
{
    // Return proper type, not always string
    switch (param.StorageType)
    {
        case StorageType.Integer:
            return param.AsInteger();
        case StorageType.Double:
            return param.AsDouble();
        case StorageType.String:
            return param.AsString() ?? "";
        default:
            return param.AsValueString() ?? "";
    }
}
```

**Update collection method:**
```csharp
public SyncRequest CollectParametersForSync(Document doc, string projectGuid)
{
    var parameters = new List<RevitParameter>();
    
    var projectInfo = doc.ProjectInformation;
    foreach (Parameter param in projectInfo.Parameters)
    {
        if (param.HasValue)
        {
            parameters.Add(new RevitParameter
            {
                Guid = param.GUID.ToString(),
                Name = GetParameterName(param),
                Value = GetParameterValue(param),  // Proper type now
                Group = "Project Information",
                DataType = GetParameterDataType(param)
            });
        }
    }
    
    return new SyncRequest
    {
        RevitProjectGuid = projectGuid,  // Changed field name
        RevitFileName = doc.Title,
        Parameters = parameters,
        Version = "1.0",
        Timestamp = DateTime.UtcNow.ToString("o")  // ISO 8601
    };
}
```

#### 5. Update SyncServiceV2

**File:** `Services/SyncServiceV2.cs`

**Change endpoint call:**
```csharp
public async Task<SyncResponse> InitiateSyncAsync(Document doc, string projectGuid)
{
    // ... parameter collection ...
    
    // OLD:
    // responseJson = await _httpHelper.SendJsonRequestAsync(
    //     _endpointManager.GetSyncEndpoint(true),  
    //     requestJson, token);
    
    // NEW:
    responseJson = await _httpHelper.SendJsonRequestAsync(
        _endpointManager.GetSyncEndpoint(),  // Just one endpoint now
        requestJson, token);
    
    var response = JsonConvert.DeserializeObject<SyncResponse>(responseJson);
    
    // Handle new response format
    if (response.Action == "queue")
    {
        // New project - needs association
        Logger.LogInfo($"Project queued for association. QueueId: {response.QueueId}");
    }
    else if (response.Action == "sync")
    {
        // Existing project - synced successfully
        Logger.LogInfo($"Sync successful. Changes: {response.Data?.ChangesApplied}");
    }
    
    return response;
}
```

**Remove these methods (not needed yet):**
```csharp
// COMMENT OUT OR DELETE:
// public async Task<SyncStatus> CheckSyncStatusAsync(string syncId)
// public async Task<bool> AcknowledgeChangesAsync(string syncId, ...)
// public void StartStatusChecking(string syncId, Action<SyncStatus> callback)
// public void StopStatusChecking()
```

#### 6. Update SyncWithWebCommand

**File:** `Command/SyncWithWebCommand.cs`

**Handle new response types:**
```csharp
private async Task HandleSyncResponse(SyncResponse response)
{
    if (response.Action == "queue")
    {
        // New project - show message and open web app
        string message = $"{response.Message}\n\n" +
                        $"Queue ID: {response.QueueId}\n\n" +
                        "Click OK to open the web app and associate this project.";
        
        Autodesk.Revit.UI.TaskDialog td = new Autodesk.Revit.UI.TaskDialog("Project Association Required");
        td.MainInstruction = "New Project Detected";
        td.MainContent = message;
        td.CommonButtons = Autodesk.Revit.UI.TaskDialogCommonButtons.Ok;
        
        if (td.Show() == Autodesk.Revit.UI.TaskDialogResult.Ok)
        {
            System.Diagnostics.Process.Start("https://app.millercraftllc.com/revit/queue");
        }
    }
    else if (response.Action == "sync")
    {
        // Existing project - show success
        string message = $"Synchronization successful!\n\n" +
                        $"Project: {response.ProjectName}\n" +
                        $"Parameters Updated: {response.Data?.ChangesApplied ?? 0}";
        
        Autodesk.Revit.UI.TaskDialog.Show("Sync Complete", message);
    }
}
```

#### 7. Update HttpRequestHelper

**File:** `Services/SyncUtilities/HttpRequestHelper.cs`

**Change header:**
```csharp
private HttpClient CreateAuthenticatedClient(string token)
{
    var client = new HttpClient();
    client.Timeout = TimeSpan.FromSeconds(30);
    
    // PRIMARY: X-Revit-Token header (per web app spec)
    client.DefaultRequestHeaders.Add("X-Revit-Token", token);
    
    // OPTIONAL: Also add Authorization as fallback
    // client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
    
    return client;
}
```

**Add test method:**
```csharp
public async Task<bool> TestConnectivityAsync()
{
    using (var client = new HttpClient())
    {
        try
        {
            var response = await client.GetAsync($"{_baseUrl}/api/revit/test");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
```

### Phase 0 Checklist

- [ ] Update `RevitParameter` class with guid, group, dataType
- [ ] Update `SyncRequest` class with revitProjectGuid, version, timestamp
- [ ] Update `SyncResponse` class with action, queueId, availableProjects
- [ ] Create `AvailableProject` and `SyncData` classes
- [ ] Update `ApiEndpointManager` to use `/api/revit/sync` and `/api/revit/test`
- [ ] Update `ParameterManager.GetParameterValue()` to return proper types
- [ ] Update `ParameterManager.CollectParametersForSync()` with new fields
- [ ] Update `HttpRequestHelper` to use `X-Revit-Token` header
- [ ] Add `HttpRequestHelper.TestConnectivityAsync()` method
- [ ] Update `SyncServiceV2.InitiateSyncAsync()` to handle new responses
- [ ] Comment out/remove status checking methods in `SyncServiceV2`
- [ ] Update `SyncWithWebCommand` to handle queue vs sync responses
- [ ] Remove OAuth2 login code from `AuthenticationService`
- [ ] Update `AuthenticationService.ValidateTokenAsync()` to use test endpoint
- [ ] Test against real web app API

---

## 📊 Phase 1: UI Consolidation

**After Phase 0 is complete and tested**

See `web-sync-consolidation-review.md` for full details.

### Create `WebAppIntegrationDialog`

**Connection Tab:**
- Token paste textbox (masked)
- Validate button
- Connection status indicator
- "Get Token from Web App" link

**Sync Tab:**
- Project GUID display
- **Sync Now** button
- Last sync result display
- Sync history (last 10)
- Handle queue/sync responses appropriately

**Diagnostics Tab:**
- Test connectivity (GET /api/revit/test)
- Network diagnostics
- View logs

### Update Commands
- `SyncWithWebCommand` → Opens dialog to Sync tab
- Rename `ConnectionManagerCommand` → `WebAppIntegrationCommand`

---

## 📊 Phase 2: Cleanup

**After Phase 1 is complete**

### Delete Redundant Files
- `UI/AuthenticationSettingsDialog.cs`
- `UI/Dialogs/ApiTokenDialog.cs`
- `UI/Dialogs/ManualApiTestDialog.cs`
- `UI/Dialogs/SimpleManualApiTestDialog.cs`
- `UI/Dialogs/NetworkDiagnosticsDialog.cs`
- `Command/AuthenticationSettingsCommand.cs`
- `Command/ApiTokenManagementCommand.cs`
- `Command/ManualApiTestCommand.cs`
- `Command/NetworkDiagnosticsCommand.cs`
- `Command/TestApiTokenCommand.cs`

### Consolidate Services
- Merge `ApiTokenService` into `AuthenticationService`
- Remove legacy storage code

---

## 📊 Phase 3: Bidirectional Sync (Future)

**When web app implements web-to-Revit sync**

### Re-enable Features
- Status checking endpoint
- `ChangeReviewDialog` usage
- Acknowledgment flow
- Polling or webhook support

### Keep For Now (Don't Delete)
- `ChangeReviewDialog.cs` - Will be needed
- `ApplyParameterChangesAsync()` method
- Status checking infrastructure

---

## 🧪 Testing Strategy

### Phase 0 Testing (Critical)

**Before moving to Phase 1, verify:**

1. **Authentication:**
   - [ ] GET `/api/revit/test` works without token (returns success)
   - [ ] GET `/api/revit/test` works with valid token
   - [ ] GET `/api/revit/test` fails with invalid token (401)
   - [ ] Token stored in UserSettings persists

2. **Sync - New Project:**
   - [ ] POST `/api/revit/sync` with new project GUID
   - [ ] Response has `action: "queue"`
   - [ ] Response includes `queueId`
   - [ ] Response includes `availableProjects[]` array
   - [ ] Browser opens to association queue

3. **Sync - Existing Project:**
   - [ ] POST `/api/revit/sync` with associated project GUID
   - [ ] Response has `action: "sync"`
   - [ ] Response includes `projectId` and `projectName`
   - [ ] Response includes `data.changesApplied` count
   - [ ] Success message displays correctly

4. **Parameters:**
   - [ ] Parameters have `guid` field populated
   - [ ] Parameters have `group` field populated
   - [ ] Parameters have `dataType` field populated
   - [ ] Integer parameters send as numbers, not strings
   - [ ] Double parameters send as numbers, not strings
   - [ ] Text parameters send as strings

5. **Headers:**
   - [ ] `X-Revit-Token` header present in requests
   - [ ] `Content-Type: application/json` header present
   - [ ] Request body is valid JSON

6. **Error Handling:**
   - [ ] 401 Unauthorized shows "Invalid token" message
   - [ ] 400 Bad Request shows validation error
   - [ ] Network errors show connection message
   - [ ] Retry logic works for transient failures

---

## 📈 Success Metrics

### Phase 0 Complete When:
- ✅ All API calls use correct endpoints
- ✅ Request/response models match web app spec
- ✅ Authentication uses token paste (no OAuth2)
- ✅ Parameters include all required fields
- ✅ Queue response opens browser to web app
- ✅ Sync response shows change count
- ✅ All Phase 0 tests pass

### Phase 1 Complete When:
- ✅ Single consolidated dialog works
- ✅ Token management simple and clear
- ✅ Sync workflow intuitive
- ✅ Sync history visible

### Phase 2 Complete When:
- ✅ All redundant dialogs removed
- ✅ All redundant commands removed
- ✅ Code cleanup complete
- ✅ Documentation updated

---

## 🚀 Getting Started

### Immediate Next Steps:

1. **Review these documents:**
   - `Integration-Gap-Analysis.md` - Understand the problems
   - `IMPLEMENTATION-ROADMAP.md` (this file) - Understand the solution
   - `REVIT_PLUGIN_INTEGRATION_PROMPT.md` - Reference spec

2. **Make decision:**
   - Approve Phase 0 approach
   - Or discuss modifications needed

3. **When ready, I can:**
   - Start implementing Phase 0 changes
   - Update model classes first
   - Then update services
   - Then update commands
   - Test each component

4. **Coordination needed:**
   - Verify web app API hasn't changed from spec
   - Get test token from web app
   - Plan rollout to users (breaking change)

---

## ❓ Questions to Resolve

1. **Should we maintain backward compatibility?**
   - Recommendation: No - clean break, version the plugin

2. **Do we have access to test environment?**
   - Need: Test token from web app
   - Need: Access to web app queue UI

3. **What about existing users?**
   - Communication plan needed
   - Migration guide needed

4. **Should we implement Phase 0 incrementally or all at once?**
   - Recommendation: All at once (it's interconnected)
   - But can test models separately first

---

## 📞 Ready When You Are

**I'm ready to start implementing Phase 0 as soon as you approve the approach.**

The code changes are clear, well-documented, and I have all the information needed from the web app spec.

**Estimated time:** 2-3 days for Phase 0 (fixing API integration)

---

**End of Roadmap**
