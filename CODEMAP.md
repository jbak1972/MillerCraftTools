# Miller Craft Tools - Revit Plugin Codemap

**Version:** 1.1.1  
**Target Framework:** .NET 8.0-windows  
**Revit API:** 2026  
**Last Updated:** Oct 24, 2025

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Project Structure](#project-structure)
4. [Commands (Ribbon Buttons)](#commands-ribbon-buttons)
5. [Services Layer](#services-layer)
6. [UI Components](#ui-components)
7. [Controllers](#controllers)
8. [Models & Data](#models--data)
9. [Utilities](#utilities)
10. [Key Workflows](#key-workflows)
11. [Important Patterns](#important-patterns)
12. [Configuration Files](#configuration-files)

---

## Overview

Miller Craft Tools is a Revit plugin that provides utilities for project maintenance, element renumbering, material management, and bidirectional synchronization with the Miller Craft Assistant web application.

### Core Capabilities

- **Project Maintenance**: Audit models, clear project info, compare view templates
- **Element Renumbering**: Windows, views on sheets with intelligent conflict resolution
- **Material Management**: Sync fenestration materials, purge non-English materials, wall assembly standardization
- **Web Synchronization**: OAuth2-authenticated bidirectional parameter sync with Miller Craft web app
- **Drafting Tools**: Sync filled region areas, manage view templates

---

## Architecture

### Design Patterns

- **MVVM**: Used for WPF-based dialogs (Audit, Settings, Material Sync)
- **Service Layer**: Authentication, Sync, Project GUID management separated from UI
- **Command Pattern**: Each ribbon button maps to an `IExternalCommand` implementation
- **Singleton**: AppTalker, logging utilities
- **Partial Classes**: Large controllers/commands split across multiple files

### Key Architectural Decisions

1. **Separation of Concerns**: Sync logic split into utility classes (ApiEndpointManager, ParameterManager, etc.)
2. **Async/Await**: All network operations use async patterns with cancellation support
3. **Transaction Management**: All Revit modifications wrapped in proper transactions
4. **Logging**: JSON-based structured logging to user's home directory
5. **Error Handling**: Graceful degradation with user-friendly error messages

---

## Project Structure

```
Miller Craft Tools/
├── Command/              # IExternalCommand implementations (ribbon buttons)
├── Controller/           # Business logic controllers (Drafting, Inspection, Family, Sheet)
├── Core/                 # Core utilities (currently empty, reserved for future use)
├── Features/             # Feature-specific modules (ProjectSetup, StandardManagement - empty)
├── Model/                # Data models and DTOs
├── Properties/           # Assembly info and resources
├── Resources/            # Icons and images for ribbon buttons
├── Services/             # Service layer (Auth, Sync, GUID management)
│   └── SyncUtilities/    # Sync-specific helper classes
├── Standards/            # Project standards configuration (JSON)
├── UI/                   # User interface components
│   ├── Dialogs/          # Dialog windows (WinForms & WPF)
│   ├── Panels/           # Custom panels (empty)
│   └── Styles/           # UI styling, branding, themes
├── Utils/                # Utility classes (logging, HTTP, testing, retry logic)
├── ViewModel/            # MVVM view models for WPF dialogs
└── Views/                # WPF XAML views
```

---

## Commands (Ribbon Buttons)

All commands implement `IExternalCommand` and are registered in `MillerCraftApp.cs` during ribbon creation.

### Project Maintenance Panel

| Command | Class | Icon | Description |
|---------|-------|------|-------------|
| **Audit Model** | `AuditModelCommand` | Audit_Model_32.png | Displays model statistics: file size, element counts, family counts, warnings, DWG imports, schema sizes |
| **# Views** | `RenumberViewsCommand` | RenumViews32.png | Renumber detail numbers of viewports on a sheet by selection sequence |
| **Finish #** | `FinishRenumberingHandler` | check.png | Commits renumbering changes (enabled during renumbering session) |
| **Cancel #** | `CancelRenumberingHandler` | cancel.png | Cancels renumbering and discards changes (enabled during renumbering session) |
| **# Windows** | `RenumberWindowsCommand` | RenumWindows32.png | Renumber window marks with automatic conflict resolution |
| **Sync sp.Area** | `SyncFilledRegionsCommand` | Synch_Area_32.png | Syncs sp.Area parameter with Area for all filled regions |
| **MatSynch** | `MaterialSyncCommand` | Material_Synch_32.png | Synchronizes fenestration materials from Global Parameters to window/door type parameters |
| **Mat Manage** | `MaterialManagementCommand` | Material_Manage_32.png | Purges materials with non-English characters and standardizes material naming |
| **Wall Std** | `WallAssemblyStandardizerCommand` | Wall_Standard_32.png | Renames wall types to standard conventions and creates standard assemblies |
| **Web App** | `WebAppSyncCommand` | Globe_Synch_32.png | Opens unified Web App Integration dialog for sync, connection, and diagnostics |
| **Clr Info** | `ClearProjectInfoCommand` | Clean32.png | Clears all editable project info parameters including MC Project GUID |
| **Compare Templates** | `CompareViewTemplatesCommand` | CompareTemplate32.png | Compares settings between two view templates and generates difference report |

### Command Details

#### RenumberViewsCommand (18,566 bytes)
- **Pattern**: Sequential selection with conflict resolution
- **Features**:
  - Prompts for starting number
  - Allows viewport selection in desired order
  - Automatically shifts conflicting detail numbers
  - Uses CommandController for managing Finish/Cancel button states
  - Supports both active sheet and sheet selection modes

#### RenumberWindowsCommand (11,919 bytes)
- **Pattern**: Direct selection loop with ISelectionFilter
- **Features**:
  - Optional 3D view creation from multiple isometric angles (user setting)
  - Mark conflict resolution with -1, -2, etc. suffixes
  - Simple selection pattern avoiding threading complexity
  - Uses `ElementId.Equals()` for proper API compatibility

#### MaterialSyncCommand (7,310 bytes)
- **Global Parameters Read**:
  - `Fenestration - Exterior Finish Material`
  - `Fenestration - Exterior Frame Material`
  - `Fenestration - Interior Finish Material`
  - `Fenestration - Interior Frame Material`
- **Target Parameters** (on window/door types):
  - `sp.Ext Finish Material`, `sp.Ext Frame Material`
  - `sp.Int Finish Material`, `sp.Int Frame Material`

#### MaterialManagementCommand (6,827 bytes)
- **Operations**:
  1. Purge materials with non-English characters
  2. Standardize material names (fix spacing issues)
- **UI**: Custom dialog with operation selection

#### WallAssemblyStandardizerCommand (16,536 bytes)
- **Operations**:
  1. Rename existing wall types to standard format
  2. Create new standard wall assemblies from "ZOOT - " prefixed materials
- **UI**: WallAssemblyConfigDialog for configuration

#### CompareViewTemplatesCommand (1,232 bytes)
- **Delegates to**: `DraftingController.CompareViewTemplates()`
- **Output**: Generates detailed comparison report of template differences

#### SyncWithWebCommand (40,702 bytes)
**Type**: Legacy command (replaced by WebAppIntegrationCommand)
**Purpose**: Original bidirectional sync implementation with threading and timer-based status checking

**Features**:
- Async sync initiation with cancellation support
- Timer-based status polling
- Idling event handling for UI thread safety
- Progress reporting
- Change review and application

**Note**: This command has been superseded by `WebAppIntegrationCommand` which provides a more robust dialog-based approach.

#### WebAppIntegrationCommand (3,481 bytes)
**Purpose**: Opens the unified Web App Integration dialog

**Features**:
- Single entry point for all web sync operations
- Delegates to `WebAppIntegrationDialog` for all functionality
- Replaces older separate sync/connection commands

#### SetupStandardsCommand (14,242 bytes)
**Purpose**: Applies project standards from `ProjectStandards.json` to the Revit document

**Operations**:
1. Read `ProjectStandards.json` from Standards folder
2. Create shared parameters if they don't exist
3. Apply StandardsVersion parameter to Project Information
4. Configure project parameters based on standards

**Dependencies**: Requires `Standards/ProjectStandards.json` file

#### RenumberViewsContextHandler (18,469 bytes)
**Purpose**: Context-aware view renumbering with ribbon button state management

**Features**:
- Alternative implementation to `RenumberViewsCommand`
- Manages transaction groups for undo/redo
- Coordinates with Finish/Cancel buttons
- Sheet selection or active view detection

#### ConnectionManagerCommand (1,392 bytes)
**Purpose**: Opens the Connection Manager dialog

**Features**:
- Manage web application connection
- OAuth2 login interface
- Token management
- Session handling

#### AuthenticationSettingsCommand (1,549 bytes)
**Purpose**: Opens authentication and connection settings dialog

**Features**:
- Configure authentication options
- Manage connection preferences
- View/edit stored credentials

#### NetworkDiagnosticsCommand (1,289 bytes)
**Purpose**: Opens network diagnostics dialog

**Features**:
- Test API endpoint connectivity
- Diagnose network issues
- Verify SSL/TLS configuration
- Check proxy settings

#### ManualApiTestCommand (2,289 bytes)
**Purpose**: Opens manual API testing dialog

**Features**:
- Test custom API endpoints
- Send custom payloads
- View raw responses
- Debugging tool for API integration

#### TestApiTokenCommand (11,766 bytes)
**Purpose**: Comprehensive API token testing and validation

**Features**:
- Token validation endpoint testing
- Multi-endpoint testing
- Detailed error reporting
- Token refresh testing

#### ApiTokenManagementCommand (1,347 bytes)
**Purpose**: Opens API token management dialog

**Features**:
- Manual token entry
- Token validation
- Legacy token management (pre-OAuth2)

#### UIShowcaseCommand (1,506 bytes)
**Purpose**: Opens UI showcase dialog demonstrating branded components

**Features**:
- Display all branded UI components
- Color palette demonstration
- Design reference for developers

#### SettingsCommand (833 bytes)
**Purpose**: Opens user settings dialog

**Features**:
- User preferences
- Application configuration
- Feature toggles

#### RelayCommand (947 bytes)
**Type**: WPF command implementation
**Purpose**: Generic command implementation for MVVM pattern

**Usage**: Used by ViewModels to bind UI actions to methods

#### RenumberingControlForm (3,594 bytes in Command/UI/)
**Type**: WinForms control
**Purpose**: UI control for managing renumbering operations

**Features**:
- Finish/Cancel button interface
- Operation status display
- Embedded in CommandController

---

## Services Layer

### AuthenticationService (21,887 bytes)

**Purpose**: OAuth2 authentication with Miller Craft Assistant web application

**Endpoints**:
- `/api/revit/tokens` - Token generation
- `/api/revit/tokens/refresh` - Refresh token
- `/api/tokens/validate` - Validate token

**Key Methods**:
```csharp
Task<string> Authenticate(username, password, cancellationToken)
Task<bool> RefreshToken(cancellationToken)
Task<bool> ValidateToken(token, cancellationToken)
string GetValidToken()  // Synchronous wrapper
```

**Token Storage**:
- Stores in `UserSettings` (preferred) and `PluginSettings` (legacy)
- Tracks expiration time (ISO 8601 UTC)
- Automatic refresh when token expires

### SyncServiceV2 (17,987 bytes)

**Purpose**: Bidirectional parameter synchronization with web application

**Architecture**: Uses modular utility classes for separation of concerns
- `ApiEndpointManager` - Endpoint URL management
- `ParameterManager` - Parameter collection and application
- `HttpRequestHelper` - HTTP request handling with auth
- `SyncStatusTracker` - Async status polling
- `ProgressReporter` - Progress event handling
- `SyncResponseHandler` - Response formatting and logging

**Key Methods**:
```csharp
Task<SyncResult> InitiateSyncAsync(doc, projectGuid)
Task<SyncStatus> CheckSyncStatusAsync(syncId)
Task<bool> ApplyParameterChangesAsync(doc, status, progress, cancellationToken)
Task<bool> AcknowledgeChangesAsync(syncId, appliedChanges)
void StartStatusChecking(syncId, statusCallback)
void StopStatusChecking()
```

**Workflow**:
1. Collect parameters from Revit → `SyncRequest`
2. POST to `/api/revit/sync` → returns `SyncResult` with action type
3. If action = "queue": Display available projects for selection
4. If action = "sync": Apply changes immediately
5. Poll status via `/api/revit/sync/{syncId}/status`
6. Apply web changes to Revit document
7. Acknowledge applied changes via `/api/revit/sync/{syncId}/apply`

### ProjectGuidManager (13,944 bytes)

**Purpose**: Manage unique project GUID for web sync correlation

**GUID Storage Priority**:
1. `sp.MC.ProjectGUID` parameter (preferred)
2. Embedded in "Project Name" parameter with `[MC_GUID:...]` marker
3. Backup file: `{ProjectName}_MC_GUID.txt`
4. Generate new GUID if none found

**Key Methods**:
```csharp
string GetOrCreateProjectGuid()
string ExtractGuidFromProjectName()
bool CreateOrUpdateProjectGuidParameter(projectId)
```

**Important**: Always checks for empty strings with `!string.IsNullOrWhiteSpace()` due to Revit API quirk where `HasValue = true` even for empty strings

### SyncUtilities (6 classes)

#### ApiEndpointManager (4,266 bytes)
- Manages primary and fallback endpoint URLs
- Configurable via `useNewEndpoints` flag
- Methods: `GetSyncEndpoint()`, `GetStatusEndpoint()`, `GetApplyEndpoint()`

#### ParameterManager (22,913 bytes)
- Collects parameters from Revit document → `SyncRequest`
- Applies web parameter changes to Revit
- Uses `ParameterMappingConfiguration` for field mapping
- **Critical Pattern**: Always validates with `!string.IsNullOrWhiteSpace()` after `AsString()`

### AuthenticationUIHelper (10,896 bytes)

**Purpose**: Helper class for authentication UI operations

**Features**:
- Coordinates between `AuthenticationService` and `AuthStatusControl`
- Provides async authentication with UI feedback
- Updates status controls during auth operations
- Telemetry logging for auth performance

**Key Methods**:
```csharp
Task<bool> AuthenticateAsync(username, password)
Task<bool> RefreshTokenAsync()
void UpdateStatusControl(status, message)
```

### SyncExceptions (1,220 bytes)

**Purpose**: Custom exception types for sync operations

**Exception Types**:
- `SessionExpiredException` - Thrown when session expires
- `SyncNetworkException` - Thrown for network errors during sync
- `SyncTimeoutException` - Thrown when sync operations timeout

**Usage**: Used throughout sync services for specific error handling

#### HttpRequestHelper (8,309 bytes)
- Handles all HTTP requests with authentication
- Automatic token refresh on 401 responses
- Implements retry logic
- JSON serialization/deserialization

#### SyncStatusTracker (8,702 bytes)
- Periodic status polling (default 5 minutes)
- Uses `System.Threading.Timer` for async checks
- Prevents UI thread blocking

#### SyncResponseHandler (10,587 bytes)
- Formats sync responses for display
- Logs sync results with structured JSON
- Handles different action types (sync, queue, error)

#### ProgressReporter (1,155 bytes)
- Wraps `IProgress<Tuple<string, int>>` for progress reporting
- Simple abstraction for progress updates

### ApiTokenService (6,766 bytes)

**Purpose**: Legacy token management (being replaced by AuthenticationService)

**Methods**:
```csharp
Task<bool> ValidateTokenAsync(token)
Task<string> GenerateTokenAsync()
```

---

## UI Components

### Dialogs (WinForms & WPF)

#### WebAppIntegrationDialog (38,821 bytes)
**Type**: WinForms  
**Purpose**: Unified dialog for web sync, connection management, and diagnostics

**Features**:
- Tab-based interface: Sync, Connection, Diagnostics
- Real-time connection status indicator
- Progress tracking for sync operations
- Change review before applying web changes
- Manual/automatic sync mode selection
- OAuth2 login flow

#### ConnectionManagerDialog (28,747 bytes)
**Type**: WinForms  
**Purpose**: Manage web application connection and authentication

**Features**:
- Login with username/password
- Token validation and refresh
- Connection status display
- Session management

#### UIShowcaseDialog (20,485 bytes)
**Type**: WinForms  
**Purpose**: Demonstrates branded UI components and styles

**Features**:
- Shows BrandColors palette
- Demonstrates StatusIndicator component
- Example layouts and controls

#### ChangeReviewDialog (13,263 bytes)
**Type**: WinForms  
**Purpose**: Review and approve/reject web parameter changes before applying

**Features**:
- Parameter-by-parameter review
- Side-by-side comparison (current vs. new)
- Selective apply/reject
- Conflict resolution

#### ApiTokenDialog (16,256 bytes)
**Type**: WinForms  
**Purpose**: Manual API token entry (legacy, replaced by OAuth2)

#### NetworkDiagnosticsDialog (17,401 bytes)
**Type**: WinForms  
**Purpose**: Network connectivity and API endpoint testing

**Features**:
- Endpoint reachability tests
- SSL/TLS verification
- Proxy detection
- DNS resolution diagnostics
- Firewall detection

#### SimpleManualApiTestDialog (20,248 bytes)
**Type**: WinForms  
**Purpose**: Manual API endpoint testing tool

#### ManualApiTestDialog (13,653 bytes + 11,472 Designer)
**Type**: WinForms  
**Purpose**: Advanced API testing with custom payloads

#### ApiTestProgressDialog (8,462 bytes)
**Type**: WinForms  
**Purpose**: Shows progress during API connectivity tests

#### WallAssemblyConfigDialog (9,864 bytes)
**Type**: WinForms  
**Purpose**: Configuration for wall assembly standardization

#### AuthenticationSettingsDialog (9,906 bytes)
**Type**: WinForms  
**Purpose**: Authentication and connection settings

### WPF Views

#### AuditView.xaml / AuditView.xaml.cs
**Purpose**: Display model audit statistics
**ViewModel**: `AuditViewModel`

#### MaterialSyncProgress.xaml
**Purpose**: Show progress during material sync operations
**Pattern**: Simple progress bar with status text

#### ResultsView.xaml
**Purpose**: Display operation results
**Pattern**: DataGrid-based result presentation

#### SettingsView.xaml
**Purpose**: User preferences and settings
**ViewModel**: `SettingsViewModel`

### Reusable Controls

#### ConnectionStatusIndicator (5,420 bytes)
**Type**: WinForms UserControl  
**Features**:
- Visual connection status (Connected, Disconnected, Checking, Error)
- Color-coded indicators (green, red, yellow, orange)
- Status text display
- Automatic status updates

#### AuthStatusControl (4,009 bytes)
**Type**: WinForms UserControl  
**Features**:
- Displays authentication status
- Shows logged-in user
- Token expiration warning

#### LoginCredentialsControl (4,287 bytes)
**Type**: WinForms UserControl  
**Features**:
- Username/password input
- Login button
- Password masking

### Styles & Branding

#### BrandColors (3,595 bytes)
**Colors**:
- Primary: `#1E3A8A` (deep blue)
- Secondary: `#F59E0B` (amber)
- Success: `#10B981` (green)
- Error: `#EF4444` (red)
- Warning: `#F59E0B` (amber)
- Info: `#3B82F6` (blue)

#### BrandedForm (5,718 bytes)
**Base class** for consistent form styling:
- Standard font (Segoe UI, 9pt)
- Branded colors
- Consistent padding and spacing

#### StatusIndicator (5,018 bytes)
**Component**: Visual status indicator with color coding
**States**: Success, Error, Warning, Info, Unknown

#### IconProvider (4,566 bytes)
**Purpose**: Centralized icon resource management
**Methods**: `GetIcon(iconName, size)`

#### UISettings (4,582 bytes)
**Purpose**: Centralized UI configuration (fonts, sizes, colors)

#### Terms (2,944 bytes)
**Purpose**: Standardized terminology and labels for consistent UI

### ViewModels

#### ViewModelBase (458 bytes)
**Purpose**: Base class for all MVVM view models

**Features**:
- Implements `INotifyPropertyChanged`
- Provides `OnPropertyChanged` helper method
- Foundation for all WPF view models

#### MainViewModel (4,407 bytes)
**Purpose**: Main view model for primary application interface

**Commands**:
- `GroupElementsByLevelCommand`
- `ExportStandardsCommand`
- `CopyToSheetsCommand`
- `SyncFilledRegionsCommand`
- `RenumberWindowsCommand`
- `RenumberViewsCommand`

**Controllers**: 
- Uses `DraftingController` and `InspectionController`

#### SettingsViewModel (1,205 bytes)
**Purpose**: View model for settings view

**Features**:
- User preference management
- Setting persistence
- Data binding for settings UI

#### CopyToSheetsViewModel (1,503 bytes)
**Purpose**: View model for copy to sheets functionality

**Features**:
- Sheet selection management
- Copy operation coordination
- Progress tracking

#### LevelNode (790 bytes)
**Purpose**: Data structure for hierarchical level representation

**Usage**: Used in UI for displaying project levels in tree structures

---

## Controllers

### DraftingController (19,892 bytes + 18,720 bytes partial)

**Purpose**: Business logic for drafting and view-related operations

**Partial Class Structure**:
- `DraftingController.cs` - Main class with window/view renumbering
- `DraftingController.CompareViewTemplates.cs` - View template comparison

**Key Methods**:
```csharp
void UpdateDetailItems()                    // Sync filled region sp.Area
void RenumberWindows()                      // Window renumbering workflow
void RenumberViewsOnSheet()                 // View renumbering workflow
void CreateMultiple3DViews()                // Create isometric views for selection
void CompareViewTemplates(template1, template2)  // Template comparison
```

**Selection Filters** (inner classes):
- `WindowSelectionFilter` - Filters windows only
- `SheetSelectionFilter` - Filters sheets only
- `ViewportSelectionFilter` - Filters viewports on specific sheet

### InspectionController (27,131 bytes)

**Purpose**: Project inspection and audit operations

**Key Methods**:
```csharp
void AuditModel()                           // Collect model statistics
int CountElements()
int CountFamilies()
int CountWarnings()
double GetFileSize()
```

### FamilyController (12,469 bytes)

**Purpose**: Family-related operations (currently not compiled - see .csproj excludes)

### SheetUtilitiesController (10,308 bytes)

**Purpose**: Sheet management utilities (currently not compiled - see .csproj excludes)

---

## Models & Data

### SyncApiModels (10,285 bytes)

**Request Models**:
- `SyncRequest` - Revit → Web parameter sync request
- `ChangeAcknowledgment` - Applied changes acknowledgment
- `ProjectSelectionRequest` - Project selection for queued syncs

**Response Models**:
- `SyncResult` - Sync operation result with action type
- `WebParameterChange` - Individual parameter change from web
- `AppliedChange` - Change application result
- `SyncStatus` - Current sync operation status
- `AvailableProject` - Project available for selection

**Auth Models**:
- `AuthResult` - Authentication response with tokens

### SyncResponseModels (3,036 bytes)

- `AcknowledgmentResponse` - Applied change acknowledgment response
- `ErrorResponse` - API error response
- `ValidationError` - Field validation errors

### UserSettings (3,961 bytes)

**Purpose**: User preferences and authentication data

**Fields**:
```csharp
bool Open3DViewsForRenumbering
string Username
string ApiToken
string RefreshToken
string TokenExpiration  // ISO 8601 UTC
string WebSessionCookie
List<string> SyncHistory  // Last 10 syncs
```

**Storage**: `%APPDATA%/Miller Craft Assistant/settings.json`

**Methods**:
```csharp
static UserSettings Load()
void Save()
bool HasValidToken()
void ClearAuthData()
void AddSyncHistoryEntry(entry)
```

### ParameterMapping (23,855 bytes)

**Purpose**: Configure which Revit parameters map to web fields

**Classes**:
- `ParameterMappingConfiguration` - Full mapping config
- `FieldMapping` - Individual field mapping rules
- `ParameterLocation` - Where to find parameter (ProjectInfo, Type, Instance)

**Note**: File has grown significantly, likely includes additional mapping configurations and helper methods.

### ProjectInfoExportModel (551 bytes)

**Purpose**: Model for exporting project information

**Classes**:
- `ProjectInfoExportModel` - Contains project ID, filename, and parameters
- `ProjectParameterExport` - Individual parameter export data (name, value, type, update flag)

**Usage**: Used for exporting project data to external formats

### ProjectData (2,452 bytes)

**Purpose**: Collected project data for sync

### ProjectStandards (3,211 bytes)

**Purpose**: Project standards configuration loaded from JSON

---

## Utilities

### Logger (15,809 bytes)

**Purpose**: Structured JSON logging to user's home directory

**Log Location**: `%USERPROFILE%/Miller Craft Assistant/`

**Key Methods**:
```csharp
string LogJson(object, prefix)
Task<string> LogHttpRequestAsync(request, body)
void LogHttpRequestNonBlocking(request, body)
Task<string> LogHttpResponseAsync(response)
void LogError(message)
void LogInfo(message)
void LogWarning(message)
```

**Log Types**:
- `json_{timestamp}.json` - Generic JSON logs
- `request_{timestamp}.txt` - HTTP request logs
- `response_{timestamp}.txt` - HTTP response logs
- `sync_initiation_*.json` - Sync start logs
- `sync_acknowledgment_*.json` - Sync completion logs
- `parameter_changes_*.json` - Parameter change logs
- `guid_*.json` - GUID operations

### TelemetryLogger (14,225 bytes)

**Purpose**: Application telemetry and usage tracking

**Methods**:
```csharp
void LogInfo(message)
void LogWarning(message)
void LogError(message)
void LogException(exception, context)
```

### HttpClientHelper (20,708 bytes)

**Purpose**: HTTP client with retry logic, timeout management, and proxy support

**Features**:
- Automatic retry with exponential backoff
- Configurable timeouts
- Proxy detection and configuration
- SSL/TLS verification
- Request/response logging

**Key Methods**:
```csharp
Task<HttpResponseMessage> SendAsync(request, cancellationToken)
Task<string> GetStringAsync(url)
Task<string> PostJsonAsync(url, json, token)
```

### RetryHelper (7,619 bytes)

**Purpose**: Retry logic with exponential backoff

**Configuration**:
- Max retries: 3
- Initial delay: 1 second
- Backoff multiplier: 2x
- Max delay: 10 seconds

**Methods**:
```csharp
Task<T> ExecuteWithRetry<T>(operation, cancellationToken)
```

### NetworkDiagnostics (14,240 bytes)

**Purpose**: Comprehensive network diagnostics

**Tests**:
- Internet connectivity check
- DNS resolution
- Endpoint reachability
- SSL/TLS verification
- Proxy detection
- Firewall detection
- API endpoint testing

### ApiConnectivityTester (11,577 bytes)

**Purpose**: Test API endpoint connectivity

**Test Types**:
- Simple GET request
- Authenticated request
- Token validation
- Full sync test

### NetworkErrorLogger (5,343 bytes)

**Purpose**: Detailed network error logging with context

### ParameterHelper (6,281 bytes)

**Purpose**: Helper methods for Revit parameter operations

**Key Methods**:
```csharp
string GetParameterValue(element, parameterName)
bool SetParameterValue(element, parameterName, value)
Parameter GetParameter(element, parameterName)
```

**Critical Pattern**: Always validates with `!string.IsNullOrWhiteSpace()` after `AsString()`

### ProxyHelper (3,343 bytes)

**Purpose**: Proxy detection and configuration

**Methods**:
```csharp
WebProxy GetSystemProxy()
bool IsProxyConfigured()
```

### SimpleProgressReporter (730 bytes)

**Purpose**: Simple implementation of `IProgress<T>`

### ParameterCreationHelper (7,733 bytes)

**Purpose**: Helper for creating required shared parameters

**Key Features**:
- Creates `sp.MC.ProjectGUID` parameter if missing
- Manages temporary shared parameter files
- Uses fixed GUID for parameter consistency
- Transaction-based parameter creation

**Methods**:
```csharp
bool EnsureProjectGuidParameterExists(Document doc)
string CreateTemporarySharedParametersFile()
```

**Note**: Temporary solution until full shared parameters system is implemented

### SimpleApiTester (12,614 bytes)

**Purpose**: Simplified API testing utilities

**Features**:
- Test API endpoints with/without authentication
- Health endpoint testing
- GET/POST request testing
- Detailed result reporting

**Methods**:
```csharp
Task<SimpleEndpointTestResult> TestSimpleEndpointAsync(token, baseUrl)
Task<SimpleEndpointTestResult> TestHealthEndpointAsync(baseUrl)
Task<SimpleEndpointTestResult> TestEndpointGetAsync(endpoint, token, baseUrl)
```

### ManualTokenTester (11,967 bytes)

**Purpose**: Manual testing of API tokens

**Features**:
- Direct token validation
- Custom endpoint testing
- Raw response inspection
- Debugging tool for authentication issues

### TokenTester (9,728 bytes)

**Purpose**: Automated token testing and validation

**Features**:
- Batch token testing
- Expiration checking
- Refresh token validation
- Token performance testing

### ApiTestingTypes (3,090 bytes)

**Purpose**: Type definitions for API testing

**Types**:
- Test result structures
- Endpoint configurations
- Test status enums
- Response wrappers

### ApiTestingResult (621 bytes)

**Purpose**: Simple data structure for API test results

**Properties**:
- Success flag
- Status code
- Response time
- Error messages

### LogSeverity (852 bytes)

**Purpose**: Enumeration for log severity levels

**Levels**:
- Debug
- Info
- Warning
- Error
- Critical

---

## Key Workflows

### 1. Web Synchronization Workflow

```
User clicks "Web App" button
   ↓
WebAppIntegrationDialog opens
   ↓
[If not authenticated]
   → Login via OAuth2
   → Store tokens in UserSettings
   ↓
User clicks "Sync Now"
   ↓
ProjectGuidManager.GetOrCreateProjectGuid()
   → Checks sp.MC.ProjectGUID parameter
   → Fallback to project name marker
   → Fallback to file backup
   → Generate new GUID if needed
   ↓
SyncServiceV2.InitiateSyncAsync(doc, guid)
   → ParameterManager collects parameters
   → POST to /api/revit/sync
   → Returns SyncResult with action type
   ↓
[If action = "queue"]
   → Display available projects
   → User selects project
   → POST selection
   ↓
[If action = "sync"]
   → Status polling begins
   → GET /api/revit/sync/{syncId}/status
   ↓
[If HasChangesToApply]
   → ChangeReviewDialog shows changes
   → User approves/rejects
   → Apply changes in transaction
   → POST acknowledgment
   ↓
Complete
```

### 2. Window Renumbering Workflow

```
User clicks "# Windows"
   ↓
Prompt for starting number
   ↓
[If user setting enabled]
   → Create 4 isometric 3D views
   → Open all views
   ↓
Enter selection loop:
   while (true)
      → PickObject with WindowSelectionFilter
      → Check if mark conflicts
      → [If conflict] Rename existing to mark-1, mark-2, etc.
      → Set new mark
      → Increment counter
   until ESC pressed
   ↓
Complete
```

### 3. View Renumbering Workflow

```
User clicks "# Views"
   ↓
Determine active sheet or select sheet
   ↓
Collect viewports on sheet
   ↓
Prompt for starting number
   ↓
Enable "Finish #" and "Cancel #" buttons
   ↓
Enter selection loop:
   while (true)
      → PickObject with ViewportFilter
      → Check if detail number conflicts
      → [If conflict] Shift all following numbers up
      → Set new detail number
      → Increment counter
   until ESC pressed
   ↓
User clicks "Finish #" or "Cancel #"
   ↓
Disable buttons
   ↓
Complete
```

### 4. Material Sync Workflow

```
User clicks "MatSynch"
   ↓
Read 4 Global Parameters:
   - Fenestration - Exterior Finish Material
   - Fenestration - Exterior Frame Material
   - Fenestration - Interior Finish Material
   - Fenestration - Interior Frame Material
   ↓
Get all window and door types
   ↓
For each type:
   → Set sp.Ext Finish Material
   → Set sp.Ext Frame Material
   → Set sp.Int Finish Material
   → Set sp.Int Frame Material
   ↓
Display progress
   ↓
Show results summary
```

---

## Important Patterns

### 1. Namespace Conflict Resolution

**Problem**: Name collisions between Autodesk.Revit and System.Windows.Forms/System.Drawing

**Solution**: Always use fully qualified names

```csharp
// CORRECT
Autodesk.Revit.UI.TaskDialog.Show("Title", "Message");
System.Threading.Timer timer = new System.Threading.Timer(...);
System.Windows.Forms.Form form = new System.Windows.Forms.Form();

// INCORRECT - Will cause CS0104 errors
TaskDialog.Show("Title", "Message");
Timer timer = new Timer(...);
Form form = new Form();
```

**Common Conflicts**:
- `Form`, `Panel`, `TaskDialog`, `Timer`, `Point`, `Size`, `View`, `Color`, `Padding`

### 2. Revit Parameter Empty String Check

**Problem**: `Parameter.HasValue` returns `true` for empty strings

**Solution**: Always validate with `!string.IsNullOrWhiteSpace()`

```csharp
// CORRECT
var param = element.LookupParameter("ParameterName");
string value = param?.AsString();
if (param != null && param.HasValue && !string.IsNullOrWhiteSpace(value))
{
    // Use value
}

// INCORRECT - Value might be empty string
if (param != null && param.HasValue)
{
    string value = param.AsString(); // Could be ""!
}
```

### 3. ElementId Comparison

**Problem**: `ElementId.IntegerValue` is deprecated/incompatible

**Solution**: Use `ElementId.Equals()` method

```csharp
// CORRECT
if (elem.Category.Id.Equals(new ElementId(BuiltInCategory.OST_Windows)))

// INCORRECT
if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Windows)
```

### 4. Transaction Management

**Pattern**: All Revit modifications must be in transactions

```csharp
using (Transaction tx = new Transaction(doc, "Description"))
{
    tx.Start();
    try
    {
        // Modify document
        tx.Commit();
    }
    catch (Exception ex)
    {
        tx.RollBack();
        throw;
    }
}
```

### 5. Async HTTP with Authentication

**Pattern**: All HTTP requests use async/await with token refresh

```csharp
string token = await _authService.GetValidTokenAsync();
string response = await _httpHelper.SendJsonRequestAsync(url, json, token);
```

### 6. Selection Filters

**Pattern**: Implement `ISelectionFilter` for filtered element selection

```csharp
private class WindowSelectionFilter : ISelectionFilter
{
    public bool AllowElement(Element elem)
    {
        if (elem == null || elem.Category == null) return false;
        return elem.Category.Id.Equals(new ElementId(BuiltInCategory.OST_Windows));
    }
    
    public bool AllowReference(Reference reference, XYZ position)
    {
        return true; // Let AllowElement decide
    }
}
```

### 7. Partial Classes for Large Files

**Pattern**: Split files over 500 lines into partial classes

```csharp
// DraftingController.cs
public partial class DraftingController
{
    // Main functionality
}

// DraftingController.CompareViewTemplates.cs
public partial class DraftingController
{
    // View template comparison
}
```

---

## Future Enhancements

### Planned Features (Features/ folder structure)

- `ProjectSetup/` - Automated project setup workflows
- `StandardManagement/` - Enhanced standards management

### Excluded Files (see .csproj)

The following files are currently excluded from compilation:
- `Controller/FamilyController.cs`
- `Controller/SheetUtilitiesController.cs`
- `DataConnection.cs`

These may be reintegrated in future versions.

---

## Configuration Files

### ProjectStandards.json (Standards/)

**Purpose**: Defines project standards and parameter configurations

**Contents**:
- `StandardsVersion` - Version identifier
- `ProjectParameters` - List of parameters to create/apply
- Parameter definitions (name, type, location, description)

**Usage**: Read by `SetupStandardsCommand` to apply standards to projects

### Miller_Craft_Tools.addin

**Purpose**: Revit add-in manifest

**Defines**:
- Plugin assembly location
- External command registrations
- Application class entry point
- Vendor information

---

## Summary Statistics

### Component Counts
- **Commands**: 24 IExternalCommand implementations
- **Controllers**: 4 (2 active, 2 excluded)
- **Services**: 6 core services + 6 sync utilities
- **Models**: 7 data models
- **UI Dialogs**: 12 dialogs (WinForms + WPF)
- **UI Controls**: 3 reusable controls
- **ViewModels**: 6 MVVM view models
- **Utilities**: 16 helper classes
- **Views**: 6 WPF XAML views

### Code Organization
- Total Commands: ~190KB
- Total Services: ~90KB  
- Total UI Components: ~170KB
- Total Utilities: ~120KB
- Total Controllers: ~60KB

### Key File Sizes (Largest Components)
1. `SyncWithWebCommand.cs` - 40,702 bytes
2. `WebAppIntegrationDialog.cs` - 38,821 bytes
3. `ConnectionManagerDialog.cs` - 28,747 bytes
4. `ParameterMapping.cs` - 23,855 bytes
5. `AuthenticationService.cs` - 21,887 bytes

---

**End of Codemap**

*This document should be updated whenever significant architectural changes or new features are added to the codebase.*
