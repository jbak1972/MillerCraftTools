# Miller Craft Tools API Testing Guide

This document provides comprehensive instructions for testing the connection between the Revit plugin and the Miller Craft web app.

## API Test Endpoint

A dedicated test endpoint has been set up at `https://app.millercraftllc.com/api/revit/test` to help diagnose connection issues. Key features:

- Accepts both authenticated and unauthenticated requests
- Returns detailed diagnostic information
- Helps isolate network, authentication, and configuration issues

## Testing Methods

### 1. Command-Line Testing with PowerShell

#### Basic Connectivity Test (No Authentication)

```powershell
# Basic test without authentication
Invoke-RestMethod -Uri "https://app.millercraftllc.com/api/revit/test" -Method GET -ContentType "application/json"
```

#### Authenticated Test

```powershell
# Test with authentication token
$token = "your-token-here"
$headers = @{
    "Authorization" = "Bearer $token"
}
Invoke-RestMethod -Uri "https://app.millercraftllc.com/api/revit/test" -Method GET -Headers $headers -ContentType "application/json"
```

### 2. Testing with Curl

#### Basic Connectivity Test

```
curl -X GET "https://app.millercraftllc.com/api/revit/test" -H "Content-Type: application/json"
```

#### Authenticated Test

```
curl -X GET "https://app.millercraftllc.com/api/revit/test" -H "Content-Type: application/json" -H "Authorization: Bearer 10d0eb1bfc11f1db99810a295fd5c17c684db2b64d76475bf1a3eaab9056fdf7"
```

### 3. Testing with Postman

1. Create a new GET request to `https://app.millercraftllc.com/api/revit/test`
2. Under Headers, add:
   - `Content-Type: application/json`
   - For authenticated tests: `Authorization: Bearer your-token-here`
3. Send the request and review the response

## Integrating with Revit Plugin

### Quick Integration Steps

1. Add the `revit-test-sample.cs` file to your project
2. Call the testing methods from your diagnostic code:

```csharp
// For basic testing
var result = await ApiConnectivityTester.TestApiConnectivityAsync();
string report = ApiConnectivityTester.GenerateTestReport(result);
// Display report to user

// For testing with authentication
string token = apiTokenService.GetToken(); // Get token from your token service
var result = await ApiConnectivityTester.TestApiConnectivityAsync(DEFAULT_BASE_URL, token);
string report = ApiConnectivityTester.GenerateTestReport(result);
```

### Using the Manual API Test Dialog

The Manual API Test dialog we've implemented in the plugin provides a user-friendly interface for testing:

1. Launch the dialog using the "Manual API Test" button from the ribbon
2. Select the `/api/revit/test` endpoint
3. Choose your authentication method:
   - No Authentication: Tests basic connectivity
   - Use Stored Token: Tests with the token stored in the plugin
   - Use Custom Token: Tests with a manually entered token
4. Click "Test Endpoint" to run the test
5. Review the results in the dialog

## Troubleshooting Common Issues

### Network Connectivity Issues

- **Symptoms**: 
  - Timeout errors
  - Connection refused
  - Host not found
- **Solutions**:
  - Verify internet connectivity
  - Check firewall settings
  - Confirm URL is correct
  - Ensure SSL/TLS handshake is working properly

### Authentication Issues

- **Symptoms**: 
  - 401 Unauthorized responses
  - 403 Forbidden responses
- **Solutions**:
  - Verify token is valid and not expired
  - Check token format (should be a valid JWT)
  - Regenerate the token if necessary
  - Ensure you're using the correct token type (Bearer)

### Server Configuration Issues

- **Symptoms**: 
  - 500 Internal Server Error
  - 404 Not Found on known endpoints
- **Solutions**:
  - Check with server administrators
  - Verify API versioning
  - Confirm endpoint paths are correct

## Obtaining and Managing Authentication Tokens

### Manually Obtaining a Token (For Testing Only)

1. Use the API Token Management dialog in the plugin
2. Enter your credentials
3. Click "Get New Token"
4. Copy the token for manual testing

### Programmatically Managing Tokens

The plugin uses `ApiTokenService` to manage tokens:

```csharp
// Get current token
var tokenService = new ApiTokenService();
string token = tokenService.GetToken();

// Store a new token
tokenService.SaveToken(newToken);
```

## Testing with Hardcoded Tokens (Development Only)

For development and debugging only, you can temporarily use hardcoded tokens:

1. In the Manual API Test dialog, select "Use Custom Token"
2. Paste your token in the text field
3. Run your tests

**Important**: Never commit code with hardcoded tokens or use this approach in production.

---

## Testing API Endpoints

You can test API connectivity using different methods:

1. Using the Manual API Test dialog in the Revit plugin
2. Using direct HTTP requests via tools like Postman or curl
3. Using browser developer tools

All tests should check for proper HTTP status codes and JSON response formats.

### API Endpoint Test Results

Below are test results for different API endpoints:

#### `/api/revit/test` Endpoint (Successful Response)

```json
{
  "success": true,
  "message": "Revit connectivity test endpoint reached successfully",
  "auth": {
    "hasAuthHeader": true,
    "tokenFormat": "Bearer format detected"
  },
  "request": {
    "method": "GET",
    "path": "/api/revit/test",
    "query": {},
    "headers": {
      "contentType": "application/json",
      "accept": "*/*",
      "userAgent": "curl/8.13.0",
      "hasAuthorization": true
    }
  },
  "serverInfo": {
    "timestamp": "2025-08-28T20:52:38.782Z",
    "environment": "development"
  }
}
```

#### `/api/health` Endpoint (Successful Response)

```json
{
  "status": "ok",
  "timestamp": "2025-08-28T21:05:06.049Z",
  "message": "Miller Craft Assistant is healthy"
}
```

#### `/api/tokens/validate` Endpoint (Failed Response - 404)

This endpoint returns a 404 Not Found error with HTML content for the error page.

#### `/api/parameter-mappings` Endpoint (Failed Response - 401)

```json
{
  "error": "Authentication required"
}
```

### Response Analysis

From these test results, we can determine:

- Basic health check endpoints are working (`/api/health`)  
- The test endpoint correctly recognizes authentication (`/api/revit/test`)  
- Token validation endpoint may have moved or changed (`/api/tokens/validate`)  
- Parameter mappings require proper authentication (`/api/parameter-mappings`)  

For API troubleshooting, focus on:
1. Verifying API endpoints are correct and up-to-date
2. Ensuring authentication tokens are valid and properly formatted
3. Checking that you have appropriate permissions for protected endpoints

## Expected Response Format

The `/api/revit/test` endpoint returns JSON in the following format:

```json
{
  "serverInfo": "Miller Craft API v2.1",
  "apiVersion": "2.1.0",
  "authenticationStatus": "Authenticated as username@example.com",
  "clientIpAddress": "192.168.1.100",
  "requestHeaders": {
    "Authorization": "Bearer eyJ***[truncated]***",
    "User-Agent": "MillerCraftTools/1.0",
    "Content-Type": "application/json"
  },
  "serverVariables": {
    "SERVER_NAME": "app.millercraftllc.com",
    "REQUEST_TIME": "2025-08-28T10:49:15-07:00"
  }
}
```

For unauthenticated requests, the `authenticationStatus` will show "Not authenticated".
