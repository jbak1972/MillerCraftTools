# Miller Craft Assistant - Revit Plugin Developer Guide

**Version:** 1.0  
**Last Updated:** January 24, 2025  
**Audience:** Revit Plugin Developers / AI Assistants working on Revit integration

---

## 📋 Table of Contents

1. [Quick Start](#quick-start)
2. [Authentication](#authentication)
3. [Sending Parameters to Web App](#sending-parameters-to-web-app)
4. [Handling Web App Responses](#handling-web-app-responses)
5. [User Feedback Messages](#user-feedback-messages)
6. [Complete Workflow Examples](#complete-workflow-examples)
7. [Error Handling](#error-handling)
8. [Best Practices](#best-practices)

---

## Quick Start

### What You're Building

The Revit plugin communicates with the Miller Craft Assistant web application to:
- **Send** parameter data from Revit models → Web App
- **Receive** project updates from Web App → Revit models
- Synchronize project information bidirectionally

### Base URL

```
Production: https://your-domain.com
Development: http://localhost:3000
```

### Required Headers

All API requests must include:

```http
Authorization: Bearer {USER_REVIT_TOKEN}
Content-Type: application/json
```

OR use the alternative header format:

```http
X-Revit-Token: {USER_REVIT_TOKEN}
Content-Type: application/json
```

---

## Authentication

### Step 1: User Gets Token from Web App

Users must first generate a Revit token in the web application:

1. User logs into Miller Craft Assistant web app
2. Goes to Profile page
3. Generates a Revit API token
4. Copies token for use in Revit plugin

### Step 2: Store Token in Revit Plugin

**Your plugin must:**
- Prompt user to paste their token (one-time setup)
- Store token securely between Revit sessions
- Include token in ALL API requests

**C# Example:**
```csharp
// Store token in user settings
Properties.Settings.Default.RevitApiToken = userInputToken;
Properties.Settings.Default.Save();

// Retrieve token for API calls
string token = Properties.Settings.Default.RevitApiToken;
```

### Step 3: Test Authentication

**Endpoint:** `POST /api/revit/test`

**Request:**
```http
POST /api/revit/test
Authorization: Bearer {token}
Content-Type: application/json
```

**Response (Success):**
```json
{
  "success": true,
  "message": "Connection successful",
  "timestamp": "2025-01-24T12:00:00.000Z"
}
```

**Response (Failure):**
```json
{
  "success": false,
  "message": "Invalid or revoked token",
  "error": "Authentication failed"
}
```

**User Feedback:**
- ✅ Success: "Connected to Miller Craft Assistant"
- ❌ Failure: "Invalid token. Please check your API token in the web app."

---

## Sending Parameters to Web App

### Primary Sync Endpoint

**Endpoint:** `POST /api/revit/sync`

This is the MAIN endpoint you'll use for all synchronization operations.

### Request Format

```json
{
  "revitProjectGuid": "84cfb8c4-8f22-4d9f-be6f-5df0ce7b359d",
  "revitFileName": "ProjectName.rvt",
  "command": "sync",
  "parameters": [
    {
      "guid": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "name": "Project Name",
      "value": "Miller Residence",
      "group": "Project Information",
      "dataType": "Text"
    },
    {
      "guid": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
      "name": "Project Address",
      "value": "123 Main St, Seattle, WA 98101",
      "group": "Project Information",
      "dataType": "Text"
    },
    {
      "guid": "c3d4e5f6-a7b8-9012-cdef-123456789012",
      "name": "Gross Floor Area",
      "value": "2500",
      "group": "Energy Analysis",
      "dataType": "Number"
    }
  ]
}
```

### Parameter Object Structure

Each parameter in the `parameters` array must include:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `guid` | string | **YES** | Revit parameter GUID (unique identifier) |
| `name` | string | **YES** | Parameter name as it appears in Revit |
| `value` | any | **YES** | Current parameter value |
| `group` | string | No | Parameter group (e.g., "Project Information") |
| `dataType` | string | No | Data type (Text, Number, YesNo, etc.) |

### Recommended Parameters to Send

**Project Information Parameters:**
```
- Project Name (sp.Project.Name)
- Project Number (sp.Project.Number)
- Project Address (sp.Project.Address)
- Client Name (sp.Client.Name)
- Building Type (sp.Building.Type)
```

**Location Parameters:**
```
- Project Address (sp.Project.Address)
- Jurisdiction (sp.Jurisdiction)
- Climate Zone (sp.Climate.Zone)
```

**Building Data Parameters:**
```
- Gross Floor Area (sp.Building.GrossFloorArea)
- Net Floor Area (sp.Building.NetFloorArea)
- Building Height (sp.Building.Height)
- Number of Stories (sp.Building.Stories)
- Occupancy Type (sp.Building.OccupancyType)
```

**Energy Analysis Parameters:**
```
- Envelope UA (sp.Energy.UA)
- Window Area (sp.Building.WindowArea)
- Wall Area (sp.Building.WallArea)
- Roof Area (sp.Building.RoofArea)
```

### How to Collect Parameters in Revit (C# Example)

```csharp
using Autodesk.Revit.DB;
using System.Collections.Generic;

public List<RevitParameter> CollectProjectParameters(Document doc)
{
    var parameters = new List<RevitParameter>();
    
    // Get project information
    ProjectInfo projectInfo = doc.ProjectInformation;
    
    // Iterate through project parameters
    foreach (Parameter param in projectInfo.Parameters)
    {
        if (param.HasValue && !param.IsReadOnly)
        {
            parameters.Add(new RevitParameter
            {
                Guid = param.GUID.ToString(),
                Name = param.Definition.Name,
                Value = GetParameterValue(param),
                Group = param.Definition.ParameterGroup.ToString(),
                DataType = param.Definition.ParameterType.ToString()
            });
        }
    }
    
    return parameters;
}

private object GetParameterValue(Parameter param)
{
    switch (param.StorageType)
    {
        case StorageType.String:
            return param.AsString();
        case StorageType.Integer:
            return param.AsInteger();
        case StorageType.Double:
            return param.AsDouble();
        case StorageType.ElementId:
            return param.AsElementId().IntegerValue;
        default:
            return param.AsValueString();
    }
}
```

---

## Handling Web App Responses

The web app will respond differently based on whether the Revit project is already associated with a web project.

### Response Type 1: New Project (Not Associated)

When syncing a Revit project for the FIRST time:

**Response:**
```json
{
  "success": true,
  "action": "queue",
  "message": "Project added to queue for association",
  "queueId": "683225c3ed3200113b8a60f7",
  "availableProjects": [],
  "timestamp": "2025-01-24T12:00:00.000Z"
}
```

**What This Means:**
- Your Revit project is NOT yet linked to a web app project
- Data has been queued for an administrator to review
- Admin must associate your Revit project with a web project

**User Feedback Message:**
```
✓ Data sent successfully!

Your Revit project has been queued for association.
An administrator will link it to a web project shortly.

You can continue working. Try syncing again later to see if 
the project has been associated.

Queue ID: 683225c3ed3200113b8a60f7
```

### Response Type 2: Existing Project (Associated)

When syncing a Revit project that's already linked:

**Response:**
```json
{
  "success": true,
  "action": "sync",
  "message": "Synchronization successful: 3 new parameters, 5 parameters updated",
  "projectId": "507f1f77bcf86cd799439011",
  "projectName": "Miller Residence",
  "data": {
    "totalParameters": 25,
    "newParameters": 3,
    "updatedParameters": 5,
    "unchangedParameters": 17,
    "changesApplied": 8
  },
  "timestamp": "2025-01-24T12:00:00.000Z"
}
```

**What This Means:**
- Sync successful!
- Your parameters have been updated in the web app
- The response tells you what changed

**User Feedback Message:**
```
✓ Sync Completed Successfully!

Project: Miller Residence
Synced: 25 parameters
Updated: 5 parameters
New: 3 parameters

All changes have been saved to the web application.
```

### Response Type 3: Already in Queue

If you sync again while still in queue:

**Response:**
```json
{
  "success": true,
  "action": "queue",
  "message": "This Revit project is already in the queue awaiting association",
  "queueId": "683225c3ed3200113b8a60f7",
  "availableProjects": [
    {
      "id": "507f1f77bcf86cd799439011",
      "name": "Miller Residence",
      "description": "Single family home renovation"
    }
  ],
  "timestamp": "2025-01-24T12:00:00.000Z"
}
```

**User Feedback Message:**
```
⏳ Waiting for Association

Your project is queued and waiting for an administrator
to link it with a web project.

Available projects:
  • Miller Residence
  
Please contact your administrator to complete the association.

Queue ID: 683225c3ed3200113b8a60f7
```

### Response Type 4: Error

**Response:**
```json
{
  "success": false,
  "action": "error",
  "message": "Failed to process Revit synchronization",
  "error": "Database connection failed",
  "timestamp": "2025-01-24T12:00:00.000Z"
}
```

**User Feedback Message:**
```
✗ Sync Failed

Error: Failed to process Revit synchronization
Details: Database connection failed

Please try again. If the problem persists, contact support.
```

---

## User Feedback Messages

### Design Principles

1. **Always show status** - User should know if sync succeeded or failed
2. **Be specific** - Show counts, project names, what changed
3. **Guide next steps** - Tell user what to do next
4. **Use visual indicators** - ✓ ✗ ⏳ symbols

### Message Templates

#### Success - First Sync
```
✓ Data Sent Successfully!

Your Revit project has been queued for association with a web project.

Next Steps:
1. An administrator will review your data
2. They will link it to a web project (or create a new one)
3. Try syncing again in a few minutes

Queue ID: {queueId}
```

#### Success - Regular Sync
```
✓ Sync Completed!

Project: {projectName}
Parameters Synced: {totalParameters}
Updated: {updatedParameters}
New: {newParameters}

Last sync: {timestamp}
```

#### Waiting for Association
```
⏳ Queued for Association

Your Revit project is waiting to be linked with a web project.

Status: Pending
Queue ID: {queueId}
Received: {timestamp}

Please contact your administrator or try again later.
```

#### Authentication Failed
```
✗ Authentication Failed

Your API token is invalid or has been revoked.

Please:
1. Log into Miller Craft Assistant web app
2. Go to your Profile
3. Generate a new Revit API token
4. Update the token in this plugin

Current token: {tokenPreview}...
```

#### Network Error
```
✗ Connection Failed

Could not connect to Miller Craft Assistant server.

Please check:
• Your internet connection
• The server URL is correct: {serverUrl}
• Firewall settings allow the connection

Error: {errorMessage}
```

---

## Complete Workflow Examples

### Workflow 1: Initial Sync (First Time)

```csharp
// 1. User clicks "Sync to Web" button in Revit
public void OnSyncButtonClick()
{
    try
    {
        // 2. Collect parameters from Revit
        var parameters = CollectProjectParameters(doc);
        
        // 3. Build request payload
        var request = new
        {
            revitProjectGuid = doc.ProjectInformation.UniqueId,
            revitFileName = doc.Title,
            command = "sync",
            parameters = parameters
        };
        
        // 4. Send to web app
        var response = await SendToWebApp("/api/revit/sync", request);
        
        // 5. Handle response
        if (response.success && response.action == "queue")
        {
            ShowMessage(
                "✓ Data Sent Successfully!",
                $"Your project has been queued for association.\n\n" +
                $"Queue ID: {response.queueId}\n\n" +
                $"An administrator will link it to a web project shortly.",
                MessageType.Success
            );
        }
        else if (response.success && response.action == "sync")
        {
            ShowMessage(
                "✓ Sync Completed!",
                $"Project: {response.projectName}\n" +
                $"Synced: {response.data.totalParameters} parameters\n" +
                $"Updated: {response.data.updatedParameters} parameters",
                MessageType.Success
            );
        }
    }
    catch (Exception ex)
    {
        ShowMessage(
            "✗ Sync Failed",
            $"Error: {ex.Message}\n\n" +
            $"Please try again or contact support.",
            MessageType.Error
        );
    }
}
```

### Workflow 2: Subsequent Sync

```csharp
public void OnSyncButtonClick()
{
    try
    {
        // Show progress
        ShowProgress("Collecting parameters from Revit...");
        
        var parameters = CollectProjectParameters(doc);
        
        ShowProgress($"Sending {parameters.Count} parameters to web app...");
        
        var request = new
        {
            revitProjectGuid = doc.ProjectInformation.UniqueId,
            revitFileName = doc.Title,
            command = "sync",
            parameters = parameters
        };
        
        var response = await SendToWebApp("/api/revit/sync", request);
        
        HideProgress();
        
        // Check if project is associated
        if (response.action == "sync")
        {
            // Project is associated - show detailed sync results
            var summary = BuildSyncSummary(response);
            ShowMessage("✓ Sync Completed!", summary, MessageType.Success);
            
            // Log sync for debugging
            LogSync(response);
        }
        else if (response.action == "queue")
        {
            // Still waiting for association
            ShowMessage(
                "⏳ Waiting for Association",
                $"Your project is queued. Queue ID: {response.queueId}\n\n" +
                $"Please contact your administrator.",
                MessageType.Warning
            );
        }
    }
    catch (HttpRequestException ex)
    {
        ShowMessage(
            "✗ Connection Failed",
            $"Could not connect to server.\n\n" +
            $"Error: {ex.Message}",
            MessageType.Error
        );
    }
}

private string BuildSyncSummary(ApiResponse response)
{
    return $"Project: {response.projectName}\n" +
           $"Total Parameters: {response.data.totalParameters}\n" +
           $"New: {response.data.newParameters}\n" +
           $"Updated: {response.data.updatedParameters}\n" +
           $"Unchanged: {response.data.unchangedParameters}\n\n" +
           $"Last sync: {DateTime.Now:g}";
}
```

---

## Error Handling

### Common Errors and Solutions

#### 401 Unauthorized

**Error Response:**
```json
{
  "success": false,
  "message": "Invalid or revoked token"
}
```

**User Message:**
```
✗ Authentication Failed

Your API token is invalid. Please generate a new token
in the web app and update it in this plugin.
```

**Code Example:**
```csharp
if (response.StatusCode == HttpStatusCode.Unauthorized)
{
    ShowTokenUpdatePrompt();
    return;
}
```

#### 400 Bad Request

**Causes:**
- Missing revitProjectGuid
- Invalid parameter format
- Missing required fields

**User Message:**
```
✗ Invalid Request

The data format is incorrect. This is likely a plugin bug.

Please report this error with the following details:
{errorDetails}
```

#### 500 Server Error

**User Message:**
```
✗ Server Error

The server encountered an error processing your request.

This is not your fault. Please try again later or contact support.

Error ID: {errorId}
```

#### Network Timeout

**Code Example:**
```csharp
try
{
    var response = await client.PostAsync(url, content);
}
catch (TaskCanceledException)
{
    ShowMessage(
        "✗ Request Timed Out",
        "The server did not respond in time.\n\n" +
        "This may be due to a large amount of data.\n" +
        "Please try again.",
        MessageType.Warning
    );
}
```

---

## Best Practices

### 1. Parameter Collection

**DO:**
- ✅ Send all shared parameters with the "sp." prefix
- ✅ Include parameter GUIDs for reliable matching
- ✅ Send only parameters that have values
- ✅ Exclude read-only calculated parameters
- ✅ Group related parameters together

**DON'T:**
- ❌ Send system parameters (unless specifically needed)
- ❌ Send empty/null parameters
- ❌ Modify parameter names (send as-is from Revit)

### 2. Sync Frequency

**Recommended:**
- User-initiated syncs (button click)
- After significant project changes
- Before closing Revit
- Daily automatic sync (optional background task)

**Avoid:**
- Syncing on every parameter change (too frequent)
- Syncing more than once per minute

### 3. User Experience

**Always:**
- Show clear progress indicators
- Provide specific error messages
- Log all sync operations for debugging
- Allow user to cancel long operations
- Store sync history locally

### 4. Performance

**Optimize:**
- Cache the project GUID (don't regenerate every sync)
- Send only changed parameters when possible
- Use async/await for all network calls
- Implement request timeouts (30-60 seconds)
- Batch parameters (don't send one at a time)

### 5. Error Recovery

**Implement:**
- Retry logic for network failures (max 3 attempts)
- Exponential backoff between retries
- Offline queue for failed syncs
- Clear error messages with actionable steps

---

## Testing Checklist

### Authentication Tests
- [ ] Valid token authenticates successfully
- [ ] Invalid token returns clear error
- [ ] Expired token is detected
- [ ] User can update token without restarting Revit

### Sync Tests
- [ ] First sync creates queue item
- [ ] Subsequent sync updates project
- [ ] Empty parameter list handled gracefully
- [ ] Large parameter lists (100+) sync successfully
- [ ] Special characters in values handled correctly

### Error Handling Tests
- [ ] Network offline shows clear message
- [ ] Server error shows clear message
- [ ] Timeout handled gracefully
- [ ] Malformed response handled gracefully

### User Experience Tests
- [ ] Progress indicator shows during sync
- [ ] Success message is clear and informative
- [ ] Error messages guide user to solutions
- [ ] Sync history is accessible to user

---

## Support & Resources

### Web App API Documentation
- Full API reference: `/docs/integration/revit-integration-master-document.md`
- Sync implementation guide: `/docs/integration/revit-sync-implementation-guide.md`

### Type Definitions
- See `/types/RevitSync.ts` for TypeScript interfaces
- Use these as reference for JSON structures

### Contact
- For API questions: Check web app documentation
- For plugin development: Consult Revit API documentation
- For integration issues: Review sync logs in web app admin panel

---

**End of Developer Guide**
