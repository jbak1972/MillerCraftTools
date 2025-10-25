# Miller Craft Tools - Services Directory Structure

## Overview

The Services directory contains classes responsible for handling various backend services including authentication, synchronization with the Miller Craft Assistant web application, and project GUID management. The code has been organized using modular architecture to separate concerns and improve maintainability.

## Main Service Classes

| File | Description |
|------|-------------|
| `AuthenticationService.cs` | Handles authentication with the Miller Craft Assistant web application, including token management and refresh logic. |
| `AuthenticationUIHelper.cs` | Provides UI-related functionality for authentication workflows. |
| `ProjectGuidManager.cs` | Manages Revit project GUIDs for synchronization with web application. |
| `SyncExceptions.cs` | Contains custom exception types for sync-related errors. |
| `SyncService.cs` | Original sync service implementation (legacy). |
| `SyncServiceV2.cs` | Improved V2 implementation of sync service using the new REST API endpoints for bidirectional synchronization with the Miller Craft Assistant web application. |

## SyncUtilities Subdirectory

The `SyncUtilities` subdirectory contains refactored components from the original monolithic `SyncServiceV2.cs` class, following the Single Responsibility Principle to improve maintainability, testability, and code organization.

| File | Description |
|------|-------------|
| `ApiEndpointManager.cs` | Manages API endpoints with support for both new and legacy endpoints, including fallback logic. |
| `HttpRequestHelper.cs` | Handles HTTP requests to the Miller Craft Assistant API, including JSON serialization and authentication. |
| `ParameterManager.cs` | Manages parameter operations including collecting parameters from Revit documents, finding parameters by category and name, and applying parameter values. |
| `ProgressReporter.cs` | Utility for reporting progress during sync operations. |
| `SyncStatusTracker.cs` | Manages tracking and checking the status of sync operations, including periodic status checking. |

## Class Relationships

- `SyncServiceV2` is the main coordinator class that uses utility classes from the `SyncUtilities` subdirectory.
- `ApiEndpointManager` is used by both `SyncServiceV2` and `SyncStatusTracker` to determine the appropriate endpoints to use.
- `HttpRequestHelper` relies on `AuthenticationService` for authentication token management.
- `SyncStatusTracker` uses `HttpRequestHelper`, `ApiEndpointManager`, and `ProgressReporter` to check sync status.
- `ParameterManager` is used by `SyncServiceV2` to handle parameter-related operations.

## Important Implementation Notes

1. **Namespace Conflicts**: Due to ambiguity between various class names across different namespaces, always use fully qualified names for potentially ambiguous classes:
   - Use `Autodesk.Revit.UI.TaskDialog` (not just `TaskDialog`)
   - Use `System.Threading.Timer` (not just `Timer`)

2. **Authentication Flow**: Authentication is handled by `AuthenticationService` which manages token refreshing.

3. **Parameter Mapping**: The `ParameterManager` handles mapping between Revit parameters and web application fields based on configuration rules.

4. **Error Handling**: Custom exceptions in `SyncExceptions.cs` provide specific error types for different sync-related failures.

5. **Synchronization Process**:
   - `InitiateSyncAsync`: Initiates sync from Revit to web
   - `CheckSyncStatusAsync`: Checks sync status
   - `ApplyParameterChanges`: Applies changes from web to Revit
   - `AcknowledgeChangesAsync`: Acknowledges applied changes back to server

6. **Periodic Status Checking**: The `SyncStatusTracker` handles periodic status checking using `System.Threading.Timer`.
