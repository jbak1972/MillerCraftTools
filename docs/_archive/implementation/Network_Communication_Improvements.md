# Miller Craft Tools Network Communication Improvements

This document outlines the network communication improvements implemented in the Miller Craft Tools Revit plugin to address connectivity issues with the Miller Craft Assistant web application.

## Overview of Improvements

The following features have been implemented to ensure secure, reliable, and seamless plugin-to-web app communication:

1. **Explicit TLS Configuration**
2. **Proxy Support**
3. **Enhanced Error Logging**
4. **Network Diagnostics**
5. **Retry Logic**

## 1. Explicit TLS Configuration

**File:** `MillerCraftApp.cs`

Modern server environments often require TLS 1.2 or higher for security. We've enforced TLS 1.2 and 1.3 as follows:

```csharp
// Configure TLS 1.2 and TLS 1.3 for all HTTPS connections
System.Net.ServicePointManager.SecurityProtocol = 
    System.Net.SecurityProtocolType.Tls12 | 
    System.Net.SecurityProtocolType.Tls13;
```

This ensures all plugin communications use secure modern protocols.

## 2. Proxy Support

**File:** `Utils/ProxyHelper.cs`

Many corporate environments use proxies which can interfere with web connections. We've added automatic proxy detection and configuration:

- Auto-detects system proxy settings
- Configures HttpClientHandler with the appropriate proxy settings
- Handles proxy authentication using default credentials
- Falls back gracefully when proxy detection fails

## 3. Enhanced Error Logging

**File:** `Utils/NetworkErrorLogger.cs`

To better diagnose connection issues, we've created detailed error logging for network-related exceptions:

- SSL/TLS handshake failures and certificate trust issues
- Socket and DNS errors
- HTTP request failures and timeouts
- Structured error details for telemetry and support

Example log message from `NetworkErrorLogger`:
```
[ERROR] SSL/TLS Authentication Error when connecting to https://app.millercraftllc.com/api/sync
Details: The remote certificate is invalid according to the validation procedure
Error Type: Certificate Trust
```

## 4. Network Diagnostics

**Files:**
- `Utils/NetworkDiagnostics.cs` 
- `UI/Dialogs/NetworkDiagnosticsDialog.cs`
- `Command/NetworkDiagnosticsCommand.cs`

We've added comprehensive network diagnostics to help troubleshoot connectivity issues:

- **Tests performed:**
  - DNS resolution
  - ICMP ping
  - TCP connection
  - HTTPS connection with certificate validation
  - Proxy detection

- **User Interface:**
  - Windows Forms dialog accessible from the ribbon
  - Select endpoints to test or enter custom URLs
  - View detailed test results
  - Copy results to clipboard for support

## 5. Retry Logic

**Files:**
- `Utils/RetryHelper.cs`
- `Services/SyncUtilities/HttpRequestHelper.cs`

We've implemented intelligent retry logic with exponential backoff for handling transient network failures:

- Automatically retries operations on transient errors
- Uses exponential backoff with random jitter to prevent thundering herd problems
- Specifically handles network timeouts, socket exceptions, and HTTP 5xx errors
- Configurable max retry count, initial delay, and max delay
- Supports both synchronous and asynchronous operations

Example usage in `HttpRequestHelper`:
```csharp
public async Task<string> SendJsonRequestAsync(string endpoint, string jsonContent, string token)
{
    return await RetryHelper.ExecuteWithRetryAsync(
        async () => await SendJsonRequestInternalAsync(endpoint, jsonContent, token),
        maxRetryCount: 3,             // Try up to 3 times
        initialDelayMs: 1000,         // Start with 1 second delay
        maxDelayMs: 10000,            // Maximum 10 second delay
        cancellationToken: _cancellationToken);
}
```

## Future Recommendations

1. **Certificate Pinning**: For additional security, consider implementing certificate pinning for critical API endpoints.
2. **Connection Pooling Optimization**: Review and optimize connection pooling settings for high-volume operations.
3. **Advanced Telemetry**: Add detailed network performance metrics to telemetry to identify slow endpoints or operations.
4. **Network Policy Configuration**: Add ability for administrators to configure network policies (timeouts, retry attempts, etc.) via settings.

## Testing and Validation

To validate these improvements:
1. Test the plugin in various network environments (direct connection, corporate proxy, VPN)
2. Use the Network Diagnostics tool to verify connectivity to the Miller Craft Assistant web application
3. Monitor error logs and telemetry to identify any remaining connectivity issues
