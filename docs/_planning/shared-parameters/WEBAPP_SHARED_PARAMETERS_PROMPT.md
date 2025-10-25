# Web App Development Prompt: Shared Parameters Management System

---

## Project Context

You are implementing a **Shared Parameters Management System** for the Miller Craft Assistant web application. This system will serve as the **central source of truth** for Revit shared parameters, replacing our current reliance on Autodesk's Parameters Service which is slow and cannot be modified via API.

---

## System Overview

### Purpose
Create a server-side system that:
1. Stores and manages Revit shared parameter definitions
2. Generates Revit shared parameters text files on-demand
3. Provides REST API access for the Revit plugin
4. Enables versioning and audit trails
5. Offers an admin UI for parameter management

### Integration Point
The Revit plugin will:
- Download the shared parameters file via REST API
- Apply parameters to Revit projects and families
- Sync parameter values bidirectionally with the web app

---

## Revit Shared Parameters File Format

Revit uses a specific text file format for shared parameters. Your system must generate files in this **exact format**:

```
# This is a Revit shared parameters file.
# Do not edit manually.
*META	VERSION	MINVERSION
META	2	1
*GROUP	ID	NAME
GROUP	1	Miller Craft Parameters
GROUP	2	Project Information
GROUP	3	Identity Data
*PARAM	GUID	NAME	DATATYPE	DATACATEGORY	GROUP	VISIBLE	DESCRIPTION	USERMODIFIABLE
PARAM	a1b2c3d4-e5f6-7890-abcd-ef1234567890	Project Number	TEXT		2	1	Project tracking number	1
PARAM	b2c3d4e5-f6g7-8901-bcde-f12345678901	Client Name	TEXT		2	1	Name of the client	1
PARAM	c3d4e5f6-g7h8-9012-cdef-012345678902	StandardsVersion	TEXT		3	1	Version of project standards	1
```

### Format Specifications

#### Line Structure
- Fields are **TAB-delimited** (ASCII character 9)
- Lines starting with `*` are section headers
- Lines starting with `#` are comments

#### META Section
```
*META	VERSION	MINVERSION
META	2	1
```
- `VERSION`: File format version (always 2)
- `MINVERSION`: Minimum compatible version (always 1)

#### GROUP Section
```
*GROUP	ID	NAME
GROUP	1	Miller Craft Parameters
GROUP	2	Project Information
```
- `ID`: Sequential integer (1, 2, 3...)
- `NAME`: Display name for the group

#### PARAM Section
```
*PARAM	GUID	NAME	DATATYPE	DATACATEGORY	GROUP	VISIBLE	DESCRIPTION	USERMODIFIABLE
PARAM	{guid}	{name}	{type}	{category}	{groupId}	{visible}	{description}	{modifiable}
```

**Field Definitions:**
- `GUID`: UUID in format `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` (lowercase)
- `NAME`: Parameter display name (no tabs, careful with special characters)
- `DATATYPE`: One of:
  - `TEXT` - String values
  - `INTEGER` - Whole numbers
  - `NUMBER` - Decimal numbers
  - `LENGTH` - Distance measurements
  - `AREA` - Area measurements  
  - `VOLUME` - Volume measurements
  - `ANGLE` - Angular measurements
  - `URL` - Web links
  - `MATERIAL` - Material references
  - `YES_NO` - Boolean values
  - `MULTILINETEXT` - Large text blocks
- `DATACATEGORY`: Usually empty (use empty string between tabs)
- `GROUP`: Integer referencing GROUP.ID
- `VISIBLE`: `1` = visible, `0` = hidden
- `DESCRIPTION`: User-facing description text
- `USERMODIFIABLE`: `1` = editable, `0` = read-only

---

## Database Schema

Implement these MongoDB collections (or equivalent SQL tables):

### Collection: `sharedParameterGroups`
```typescript
{
  _id: ObjectId,
  groupId: Number,           // Sequential ID (1, 2, 3...)
  name: String,              // Display name
  description: String,       // Internal description
  sortOrder: Number,         // For ordering in UI
  createdAt: Date,
  updatedAt: Date,
  createdBy: ObjectId        // User reference
}
```

**Indexes:**
- `groupId`: unique
- `name`: unique

### Collection: `sharedParameters`
```typescript
{
  _id: ObjectId,
  guid: String,              // UUID format, must be unique and stable
  name: String,              // Parameter name
  dataType: String,          // TEXT, INTEGER, NUMBER, etc.
  dataCategory: String,      // Usually empty
  groupId: Number,           // References sharedParameterGroups.groupId
  visible: Boolean,          // Show in Revit UI
  description: String,       // User-facing description
  userModifiable: Boolean,   // Can users edit this parameter
  
  // Additional metadata
  categories: [String],      // Revit categories to bind to
  isInstance: Boolean,       // Instance vs Type parameter
  isBuiltIn: Boolean,        // Is this a Revit built-in parameter mapping
  
  // Versioning and audit
  version: Number,           // Increment on each change
  createdAt: Date,
  updatedAt: Date,
  createdBy: ObjectId,       // User reference
  lastModifiedBy: ObjectId   // User reference
}
```

**Indexes:**
- `guid`: unique
- `name`: unique
- `groupId`: non-unique
- `updatedAt`: for versioning queries

### Collection: `sharedParametersHistory` (Optional but recommended)
```typescript
{
  _id: ObjectId,
  parameterId: ObjectId,     // References sharedParameters._id
  changeType: String,        // 'created', 'updated', 'deleted'
  previousValues: Object,    // Full previous state
  newValues: Object,         // Full new state
  changedBy: ObjectId,       // User reference
  changedAt: Date,
  reason: String             // Optional change reason
}
```

### Collection: `parameterMigrations`
```typescript
{
  _id: ObjectId,
  oldGuid: String,           // Previous GUID (from Parameters Service)
  newGuid: String,           // New GUID in our system
  parameterName: String,     // Parameter name for reference
  migratedAt: Date,
  migratedBy: ObjectId,
  status: String,            // 'pending', 'completed', 'failed'
  notes: String              // Migration notes
}
```

---

## REST API Endpoints

### 1. Download Shared Parameters File
**Purpose:** Revit plugin downloads the current shared parameters text file

```
GET /api/shared-parameters/download
```

**Authentication:** Required (Bearer token)

**Response:**
- **Content-Type:** `text/plain; charset=utf-8`
- **Body:** The complete shared parameters file content

**Example Response:**
```
# This is a Revit shared parameters file.
# Do not edit manually.
*META	VERSION	MINVERSION
META	2	1
*GROUP	ID	NAME
GROUP	1	Miller Craft Parameters
*PARAM	GUID	NAME	DATATYPE	DATACATEGORY	GROUP	VISIBLE	DESCRIPTION	USERMODIFIABLE
PARAM	a1b2c3d4-e5f6-7890-abcd-ef1234567890	Project Number	TEXT		1	1	Project number	1
```

**Implementation Notes:**
- Generate file dynamically from database
- Use proper TAB characters (\t)
- Ensure Unix line endings (\n) or Windows (\r\n) - Revit accepts both
- Cache generated file for performance (invalidate on parameter changes)
- Include ETag header for caching

---

### 2. Get Parameter Definitions (JSON)
**Purpose:** Get structured parameter data (for plugin or admin UI)

```
GET /api/shared-parameters/definitions
```

**Authentication:** Required

**Query Parameters:**
- `groupId` (optional): Filter by group ID
- `includeHidden` (optional, default=false): Include hidden parameters
- `version` (optional): Get specific version

**Response:**
```json
{
  "success": true,
  "version": 1,
  "generatedAt": "2025-10-22T22:00:00Z",
  "groups": [
    {
      "groupId": 1,
      "name": "Miller Craft Parameters",
      "description": "Standard Miller Craft parameters"
    }
  ],
  "parameters": [
    {
      "id": "...",
      "guid": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "name": "Project Number",
      "dataType": "TEXT",
      "dataCategory": "",
      "groupId": 1,
      "groupName": "Miller Craft Parameters",
      "visible": true,
      "description": "Project tracking number",
      "userModifiable": true,
      "categories": ["Project Information"],
      "isInstance": true,
      "version": 1,
      "updatedAt": "2025-10-22T20:00:00Z"
    }
  ]
}
```

---

### 3. Get Groups
**Purpose:** List all parameter groups

```
GET /api/shared-parameters/groups
```

**Authentication:** Required

**Response:**
```json
{
  "success": true,
  "groups": [
    {
      "id": "...",
      "groupId": 1,
      "name": "Miller Craft Parameters",
      "description": "Standard parameters",
      "sortOrder": 1,
      "parameterCount": 15
    }
  ]
}
```

---

### 4. Create Parameter Group
**Purpose:** Add a new parameter group

```
POST /api/shared-parameters/groups
```

**Authentication:** Required (Admin role)

**Request Body:**
```json
{
  "name": "Custom Group",
  "description": "Custom parameters",
  "sortOrder": 10
}
```

**Response:**
```json
{
  "success": true,
  "group": {
    "id": "...",
    "groupId": 4,
    "name": "Custom Group",
    ...
  }
}
```

---

### 5. Create Parameter
**Purpose:** Add a new shared parameter

```
POST /api/shared-parameters/parameters
```

**Authentication:** Required (Admin role)

**Request Body:**
```json
{
  "name": "New Parameter",
  "dataType": "TEXT",
  "groupId": 1,
  "visible": true,
  "description": "Description of parameter",
  "userModifiable": true,
  "categories": ["Project Information"],
  "isInstance": true
}
```

**Response:**
```json
{
  "success": true,
  "parameter": {
    "id": "...",
    "guid": "newly-generated-uuid",
    "name": "New Parameter",
    ...
  }
}
```

**Implementation Notes:**
- **Auto-generate GUID** on creation (UUID v4)
- Validate uniqueness of name and GUID
- Ensure groupId references valid group
- Increment version number
- Create history entry

---

### 6. Update Parameter
**Purpose:** Modify existing parameter

```
PUT /api/shared-parameters/parameters/:id
```

**Authentication:** Required (Admin role)

**Request Body:** (partial update supported)
```json
{
  "description": "Updated description",
  "visible": false
}
```

**Response:**
```json
{
  "success": true,
  "parameter": {
    "id": "...",
    "guid": "unchanged-guid",
    "name": "Parameter Name",
    "description": "Updated description",
    "visible": false,
    ...
  }
}
```

**Implementation Notes:**
- **DO NOT allow GUID changes** once created
- Create history entry before update
- Increment version number
- Invalidate file cache

---

### 7. Delete Parameter
**Purpose:** Remove a parameter (soft delete recommended)

```
DELETE /api/shared-parameters/parameters/:id
```

**Authentication:** Required (Admin role)

**Response:**
```json
{
  "success": true,
  "message": "Parameter deleted successfully"
}
```

**Implementation Notes:**
- Consider soft delete (add `deletedAt` field)
- Create history entry
- Warn if parameter is in use in projects
- Invalidate file cache

---

### 8. Get Migration Mapping
**Purpose:** Revit plugin retrieves GUID migration map

```
GET /api/shared-parameters/migration-map
```

**Authentication:** Required

**Response:**
```json
{
  "success": true,
  "mappings": [
    {
      "oldGuid": "old-parameters-service-guid",
      "newGuid": "new-shared-param-guid",
      "parameterName": "Project Number",
      "status": "completed"
    }
  ]
}
```

---

## File Generation Logic

### Core Algorithm

```typescript
async function generateSharedParametersFile(): Promise<string> {
  // 1. Fetch groups (sorted by sortOrder)
  const groups = await SharedParameterGroup.find()
    .sort({ sortOrder: 1 })
    .lean();
  
  // 2. Fetch parameters (sorted by group, then name)
  const parameters = await SharedParameter.find()
    .sort({ groupId: 1, name: 1 })
    .lean();
  
  // 3. Build file content
  let content = '';
  
  // Header
  content += '# This is a Revit shared parameters file.\n';
  content += '# Do not edit manually.\n';
  
  // META section
  content += '*META\tVERSION\tMINVERSION\n';
  content += 'META\t2\t1\n';
  
  // GROUP section
  content += '*GROUP\tID\tNAME\n';
  for (const group of groups) {
    content += `GROUP\t${group.groupId}\t${group.name}\n`;
  }
  
  // PARAM section
  content += '*PARAM\tGUID\tNAME\tDATATYPE\tDATACATEGORY\tGROUP\tVISIBLE\tDESCRIPTION\tUSERMODIFIABLE\n';
  for (const param of parameters) {
    const visible = param.visible ? '1' : '0';
    const modifiable = param.userModifiable ? '1' : '0';
    content += `PARAM\t${param.guid}\t${param.name}\t${param.dataType}\t${param.dataCategory || ''}\t${param.groupId}\t${visible}\t${param.description}\t${modifiable}\n`;
  }
  
  return content;
}
```

### Caching Strategy

```typescript
// Cache generated file in Redis or memory
const CACHE_KEY = 'shared_parameters_file';
const CACHE_VERSION_KEY = 'shared_parameters_version';

async function getSharedParametersFile(): Promise<string> {
  // Get current version
  const currentVersion = await SharedParameter.find()
    .sort({ version: -1 })
    .limit(1)
    .select('version');
  
  // Check cache
  const cachedVersion = await cache.get(CACHE_VERSION_KEY);
  if (cachedVersion === currentVersion) {
    const cachedFile = await cache.get(CACHE_KEY);
    if (cachedFile) return cachedFile;
  }
  
  // Generate new file
  const fileContent = await generateSharedParametersFile();
  
  // Cache it
  await cache.set(CACHE_KEY, fileContent, 3600); // 1 hour TTL
  await cache.set(CACHE_VERSION_KEY, currentVersion);
  
  return fileContent;
}

// Invalidate cache when parameters change
async function invalidateParametersCache() {
  await cache.del(CACHE_KEY);
  await cache.del(CACHE_VERSION_KEY);
}
```

---

## Admin UI Requirements

### Parameter Management Page

**Features:**
1. **List View**
   - Display all parameters in a table
   - Columns: Name, Type, Group, Visible, Modified Date
   - Search and filter capabilities
   - Sort by column
   - Pagination

2. **Create/Edit Form**
   - All fields from schema
   - Validation
   - Group dropdown
   - Data type dropdown
   - Category multi-select
   - GUID display (read-only after creation)

3. **Group Management**
   - Create/edit/delete groups
   - Reorder groups
   - View parameter count per group

4. **Bulk Operations**
   - Export to CSV
   - Import from CSV
   - Bulk visibility toggle
   - Bulk group assignment

5. **Version History**
   - View change history for each parameter
   - Compare versions
   - Rollback capability (optional)

6. **Migration Tools**
   - Upload old Parameters Service export
   - Create migration mappings
   - Track migration status

---

## Security Considerations

1. **Authentication:**
   - All endpoints require valid JWT token
   - Admin-only operations (create/update/delete)

2. **Authorization:**
   - Role-based access control
   - Audit log for all changes

3. **Validation:**
   - Validate all inputs
   - Prevent SQL injection / NoSQL injection
   - Sanitize descriptions and names

4. **Rate Limiting:**
   - Apply rate limits to prevent abuse
   - Especially on file download endpoint

---

## Testing Requirements

### Unit Tests
- File generation produces valid format
- GUID uniqueness validation
- Group ID auto-increment
- Parameter CRUD operations

### Integration Tests
- API endpoint responses
- Database operations
- Cache invalidation
- Authentication/authorization

### Format Validation Tests
- Generated file opens in Revit
- Parameters are correctly parsed
- Special characters handled properly
- Unicode support

---

## Initial Data Setup

### Default Groups
Create these groups on initial setup:

```javascript
[
  { groupId: 1, name: "Miller Craft Parameters", sortOrder: 1 },
  { groupId: 2, name: "Project Information", sortOrder: 2 },
  { groupId: 3, name: "Identity Data", sortOrder: 3 }
]
```

### Initial Parameters
Migrate these from the current `ProjectStandards.json`:

```javascript
[
  {
    name: "StandardsVersion",
    dataType: "TEXT",
    groupId: 3,
    visible: true,
    description: "Version of project standards applied",
    userModifiable: true,
    categories: ["Project Information"],
    isInstance: true
  },
  {
    name: "Project Number",
    dataType: "TEXT",
    groupId: 2,
    visible: true,
    description: "Project tracking number",
    userModifiable: true,
    categories: ["Project Information"],
    isInstance: true
  }
  // Add more as needed
]
```

---

## Performance Considerations

1. **File Generation:**
   - Cache generated file (Redis or in-memory)
   - Invalidate cache only on parameter changes
   - Set appropriate ETag headers

2. **Database Queries:**
   - Index frequently queried fields
   - Use lean() for read operations
   - Batch operations where possible

3. **API Response Times:**
   - Target < 200ms for file download
   - < 100ms for JSON endpoints
   - Use compression for large responses

---

## Migration Support

### Export Existing Parameters
Provide an endpoint to export current Parameters Service data:

```
POST /api/shared-parameters/import-legacy
```

**Request Body:**
```json
{
  "parameters": [
    {
      "name": "Old Parameter",
      "guid": "old-guid-here",
      "type": "TEXT",
      "group": "Custom"
    }
  ]
}
```

**Process:**
1. Create new parameter with new GUID
2. Store migration mapping (old GUID → new GUID)
3. Return mapping for Revit plugin to use

---

## Documentation Deliverables

1. **API Documentation:**
   - OpenAPI/Swagger specification
   - Example requests and responses
   - Authentication guide

2. **Database Documentation:**
   - Schema diagrams
   - Field descriptions
   - Index strategy

3. **Admin Guide:**
   - How to add parameters
   - Best practices
   - Troubleshooting

---

## Questions to Consider

1. **Should we support importing existing Revit shared parameters files?**
2. **Do we need parameter templates or presets?**
3. **Should we version the entire parameter set (snapshot versioning)?**
4. **Do we need conflict resolution if multiple admins edit simultaneously?**
5. **Should we support parameter dependencies or validation rules?**

---

## Success Criteria

- [ ] File generation produces valid Revit shared parameters format
- [ ] All CRUD operations work correctly
- [ ] File download is fast (< 5 seconds)
- [ ] Admin UI is intuitive and responsive
- [ ] Caching improves performance significantly
- [ ] Migration mapping is accurate
- [ ] Comprehensive audit trail exists
- [ ] API documentation is complete

---

## Next Steps

1. Review this specification
2. Set up database collections
3. Implement file generation logic
4. Build REST API endpoints
5. Create admin UI
6. Test with actual Revit plugin
7. Perform migration dry run

---

**This specification should provide everything needed to implement the server-side shared parameters management system. If you need clarification on any Revit-specific details, refer back to the Cascade AI assistant working on the Revit plugin side.**
