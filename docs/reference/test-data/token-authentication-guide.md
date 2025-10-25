# Token Authentication Guide for Miller Craft Tools

This guide explains how to use the updated token authentication system with the Miller Craft Tools Revit plugin.

## Overview of the Updated Token System

The token authentication system has been updated with several improvements:

1. Tokens are now stored in a dedicated `UserToken` collection instead of directly on the `User` document
2. Tokens have proper expiration dates, type classification, and revocation support
3. The validation endpoint is located at `/api/tokens/validate`
4. All authentication is handled through the unified `api-token-auth.ts` adapter

## Using Tokens in the Revit Plugin

### Obtaining a Token

There are two ways to obtain a token for use with the Revit plugin:

1. **Web Application (Recommended)**:
   - Log in to the Miller Craft Assistant web application
   - Navigate to your profile page
   - Click the "Generate New Revit API Token" button
   - Copy the generated token to use in the plugin

2. **API Request**:
   - Make a POST request to the token generation endpoint
   - Requires an existing session authentication
   - See the API examples below

### Setting Up Authentication in the Plugin

1. Open the Miller Craft Tools Revit plugin
2. Click the "API Token" button in the ribbon
3. Enter or paste your token in the dialog
4. Click "Save Token" to store the token

Alternatively, use the "Manual API Test" dialog:
1. Select "Use Custom Token" from the authentication dropdown
2. Enter your token in the text field
3. Test your API endpoints

## API Endpoints

### Token Generation

```
POST /api/user/revit-token
```

**Request Body**:
```json
{
  "name": "My Revit Token",
  "expirationDays": 30
}
```

**Response**:
```json
{
  "token": "your-token-string",
  "expiresAt": "2025-09-27T21:19:45.000Z",
  "tokenId": "token-unique-id"
}
```

### Token Validation

```
GET /api/tokens/validate
```

**Headers**:
```
Authorization: Bearer YOUR_TOKEN_HERE
```

**Successful Response**:
```json
{
  "valid": true,
  "user": {
    "id": "user-id",
    "email": "user@example.com",
    "name": "User Name"
  },
  "permissions": ["read:projects", "write:parameters"],
  "expiresAt": "2025-09-27T21:19:45.000Z"
}
```

## Token Management

### Token Expiration

Tokens now have expiration dates. When a token expires:
- It will no longer authenticate API requests
- The validation endpoint will return a 401 error
- You'll need to generate a new token from the web application

### Multiple Tokens

The new system supports multiple active tokens per user:
- Each token can have a different name, expiration date, and permission set
- Tokens can be revoked individually without affecting other tokens
- You can create specialized tokens for different Revit workstations

### Token Security Best Practices

1. **Do not share tokens** between users
2. Use a reasonable expiration period (30-90 days recommended)
3. Revoke tokens when they are no longer needed
4. Use descriptive token names to track their usage

## Troubleshooting

### Common Error Codes

- **401 Unauthorized**: Invalid, expired, or revoked token
- **403 Forbidden**: Token valid but insufficient permissions
- **404 Not Found**: Endpoint doesn't exist or URL typo

### API Response Messages

The API now returns more descriptive error messages:

```json
{
  "error": "Authentication required",
  "message": "The token has expired",
  "code": "TOKEN_EXPIRED"
}
```

### Testing Token Validity

Use the Manual API Test dialog in the Revit plugin to test your token:
1. Select the `/api/tokens/validate` endpoint
2. Choose "Use Custom Token" authentication
3. Enter your token
4. Click "Test Selected Endpoint"

## Migration Notes

For users migrating from the previous token system:

1. Previous tokens stored in `User.revitToken` field are no longer valid
2. You must generate new tokens using the web application
3. Update any saved tokens in configuration files or settings

## Differences from Previous Token System

The previous token system stored tokens directly on the User document as `revitToken`. The new system:

1. Uses a dedicated `UserToken` collection for better token management
2. Supports multiple tokens per user
3. Includes token expiration, revocation, and usage tracking
4. Works with the unified authentication adapter for both session and token auth
