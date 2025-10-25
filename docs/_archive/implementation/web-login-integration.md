# Web Login Integration for Miller Craft Tools (Revit Plugin)

## Overview
This document describes the approach for integrating secure web login into the Miller Craft Tools Revit plugin. The goal is to allow users to authenticate via the web app and have the plugin securely retrieve and store their API token for future API calls.

---

## User Flow
1. User clicks “Login to Web App” in the plugin’s settings dialog.
2. A browser window opens to the web app’s login page (OAuth2/SSO endpoint).
3. User logs in using their credentials (credentials are never handled by the plugin).
4. On success, the web app redirects to a special URL (e.g., `https://yourapp.com/auth/success?token=XYZ`).
5. The plugin detects this redirect, extracts the API token (or session cookie), and stores it securely in per-user settings.
6. The plugin uses this token for all future API calls. No need for the user to manually manage or see the token.

---

## Technical Steps
- Add a WPF window with an embedded browser (`WebBrowser` control or `CefSharp`).
- Navigate to the web app’s login page.
- Listen for navigation events; when the browser hits the “success” redirect URL, extract the token.
- Store the token in `UserSettings`.
- Use the token for all API calls from the plugin.
- If the token expires, prompt the user to log in again.

---

## Security Benefits
- User never sees or handles the raw token.
- Credentials are only entered on the secure web app.
- Supports advanced login options (SSO, MFA) without plugin code changes.

---

## Web App Requirements
- Endpoint for login (OAuth2 is ideal).
- Redirect URL for plugin to detect (e.g., `https://yourapp.com/auth/success?token=...`).
- (Optional) Ability to invalidate tokens (logout).

---

## Summary
- The plugin will authenticate, retrieve the user’s API token, and use that token for all future API calls.
- This is more secure and user-friendly than manual token entry.

---

## Next Steps
- Await decision to implement.
- (Optional) Choose browser control: WPF `WebBrowser` (easier) or `CefSharp` (more robust).
- Coordinate with web app team for endpoint and redirect details.
