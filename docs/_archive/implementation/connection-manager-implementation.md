# Connection Manager Implementation

This document outlines the implementation details for the consolidated Connection Manager in Miller Craft Tools.

## Overview

The Connection Manager consolidates several previously separate functions:
- Authentication (login/logout)
- API token management
- Network connectivity diagnostics

## Ribbon Organization

### Previous Organization

The previous ribbon contained these separate buttons:
- Manual API Test - Opens dialog for manual API endpoint testing
- Test API Token - Runs detailed token validation tests against various endpoints
- API Token - Manages API tokens (add, remove, store)
- Network Test - Opens network diagnostics for connectivity troubleshooting
- Authentication - Manages login credentials and authentication settings
- Settings - General plugin settings management

### New Organization

The new ribbon organization consolidates these buttons:

1. **Connection Manager** - A single button that opens the tabbed ConnectionManagerDialog with:
   - Authentication tab (login credentials, status)
   - API Token tab (view, validate, manage tokens)
   - Diagnostics tab (connectivity tests, API tests)

2. **Connection Status Indicator** - A small colored indicator in the ribbon showing connection status:
   - Green: Connected with valid token
   - Yellow: Connected but token needs validation
   - Red: Not connected or token invalid

3. **Settings** - Remains as a separate button for non-connection related settings

### Buttons to Remove

The following buttons should be removed from the ribbon:
- Manual API Test
- Test API Token
- API Token
- Network Test
- Authentication

These functions are now available within the Connection Manager dialog.

## Implementation Details

### New Classes

1. `ConnectionManagerDialog` - Main tabbed dialog containing all connection functionality
2. `ConnectionStatusIndicator` - Small status indicator for the ribbon

### Modified Classes

None of the existing functionality needs to be modified, as it's being incorporated into the new dialog without changes to core business logic.

### Ribbon Definition

The ribbon should be updated to:
1. Add the ConnectionManagerCommand button
2. Add the ConnectionStatusIndicator
3. Remove the redundant buttons

## Migration Notes

- All functionality from the separate dialogs is preserved in the Connection Manager tabs
- Users should be notified of the UI changes in release notes
- The tabbed interface provides better organization while maintaining all capabilities
