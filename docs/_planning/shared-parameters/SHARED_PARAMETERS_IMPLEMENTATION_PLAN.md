# Shared Parameters Implementation Plan
## Miller Craft Tools Revit Plugin

**Date:** October 22, 2025  
**Author:** Cascade AI Assistant  
**Status:** Planning Phase

---

## Executive Summary

This document outlines the comprehensive plan to migrate from Autodesk's Parameters Service to a centralized, web-based shared parameters management system. The new system will enable faster parameter management, centralized control, and seamless integration with our existing web app infrastructure.

### Goals
1. **Replace Autodesk Parameters Service** with a custom, web-based solution
2. **Centralize parameter definitions** in the web app with server-side storage
3. **Enable programmatic generation** of Revit shared parameters text files
4. **Migrate existing parameters** from Parameters Service to new system
5. **Maintain data integrity** during migration process
6. **Integrate with existing sync infrastructure** for seamless operation

---

## Current State Analysis

### Existing Parameter Management
- **Method:** Autodesk Parameters Service
- **Issues:** 
  - Very slow performance
  - No API access without Forge account
  - Cannot be modified programmatically
  - Centralized management limitations

### Current Implementation
- `SetupStandardsCommand.cs` manages shared parameters
- Creates temporary shared parameters files
- Uses `DefinitionFile`, `DefinitionGroup`, `ExternalDefinition`
- Parameters defined in `ProjectStandards.json`
- Parameter mapping system already in place (`ParameterMapping.cs`)

---

## Architecture Overview

### System Components

```
┌────────────────┐          ┌──────────────────┐         ┌─────────────────┐
│  Web App       │          │  Revit Plugin    │         │  Revit Project  │
│  (Server)      │          │                  │         │  /Families      │
│                │          │                  │         │                 │
│ - Store Params │◄─REST───►│ - Download File  │◄─API───►│ - Apply Params  │
│ - Generate TXT │   API    │ - Parse Params   │         │ - Bind to       │
│ - Version Ctrl │          │ - Apply to Revit │         │   Categories    │
└────────────────┘          └──────────────────┘         └─────────────────┘
```

### Data Flow
1. **Definition Storage:** Web app stores all parameter definitions in database
2. **File Generation:** Web app generates Revit shared parameters text file on demand
3. **Download:** Revit plugin downloads the file via REST API
4. **Application:** Plugin applies parameters to projects and families
5. **Migration:** Plugin handles migration from old Parameters Service GUIDs

---

## Revit Shared Parameters Format

### File Structure
```
# This is a Revit shared parameters file.
# Do not edit manually.
*META	VERSION	MINVERSION
META	2	1
*GROUP	ID	NAME
GROUP	1	Miller Craft Parameters
GROUP	2	Project Information
*PARAM	GUID	NAME	DATATYPE	DATACATEGORY	GROUP	VISIBLE	DESCRIPTION	USERMODIFIABLE
PARAM	a1b2c3d4-e5f6-7890-abcd-ef1234567890	Project Number	TEXT		1	1	Project tracking number	1
PARAM	b2c3d4e5-f6g7-8901-bcde-f12345678901	Client Name	TEXT		1	1	Name of the client	1
```

### File Sections
1. **META:** File version information
2. **GROUP:** Parameter groups/categories
3. **PARAM:** Individual parameter definitions with:
   - `GUID`: Unique identifier (critical for consistency)
   - `NAME`: Parameter display name
   - `DATATYPE`: Type (TEXT, INTEGER, NUMBER, YES_NO, etc.)
   - `GROUP`: Group ID reference
   - `VISIBLE`: 1=visible, 0=hidden
   - `DESCRIPTION`: User-facing description
   - `USERMODIFIABLE`: 1=editable, 0=locked

---

## Implementation Phases

### Phase 1: Web App Infrastructure (Week 1-2)
**Objective:** Build server-side parameter management system

#### Tasks:
1. Database schema design for shared parameters
2. API endpoints for parameter CRUD operations
3. Shared parameters file generation logic
4. Download endpoint for Revit plugin
5. Version control and audit trail
6. Admin UI for parameter management

#### Deliverables:
- Database models and migrations
- REST API documentation
- File generation service
- Admin interface for parameter management

### Phase 2: Revit Plugin Integration (Week 2-3)
**Objective:** Enable plugin to download and apply shared parameters

#### Tasks:
1. Create `SharedParametersService.cs`
2. Implement file download from web app
3. Parse shared parameters text file
4. Load parameters into Revit `DefinitionFile`
5. Bind parameters to appropriate categories
6. Add parameters to projects
7. Add parameters to families

#### Deliverables:
- `SharedParametersService.cs`
- `SharedParametersCommand.cs`
- Integration with existing sync infrastructure

### Phase 3: Migration Strategy (Week 3-4)
**Objective:** Migrate existing Parameters Service data to new system

#### Tasks:
1. Inventory all existing parameters from Parameters Service
2. Map old GUIDs to new GUIDs
3. Create migration mapping table
4. Implement parameter value transfer
5. Update project parameters
6. Update family parameters
7. Validate data integrity

#### Deliverables:
- `ParameterMigrationService.cs`
- Migration mapping documentation
- Validation reports
- Rollback procedures

### Phase 4: Integration & Testing (Week 4-5)
**Objective:** Ensure seamless operation with existing systems

#### Tasks:
1. Integrate with existing sync infrastructure
2. Update `ParameterMapping.cs` for new parameters
3. Test bidirectional sync with new parameters
4. Performance testing and optimization
5. Documentation updates
6. User acceptance testing

#### Deliverables:
- Integrated system
- Performance benchmarks
- Updated documentation
- Test reports

---

## Technical Specifications

### Database Schema (Web App)

```typescript
interface SharedParameter {
  id: string;
  guid: string;                    // Revit GUID
  name: string;                    // Parameter name
  dataType: string;                // TEXT, INTEGER, NUMBER, etc.
  dataCategory?: string;           // Optional data category
  group: string;                   // Parameter group
  groupId: number;                 // Group ID for file generation
  visible: boolean;                // Visibility flag
  description: string;             // User description
  userModifiable: boolean;         // Can users edit?
  categories: string[];            // Revit categories to bind to
  isInstance: boolean;             // Instance vs Type parameter
  createdAt: Date;
  updatedAt: Date;
  createdBy: string;
  version: number;                 // Version tracking
}

interface ParameterGroup {
  id: string;
  groupId: number;                 // Sequential ID for file
  name: string;
  description: string;
  sortOrder: number;
  createdAt: Date;
  updatedAt: Date;
}

interface ParameterMigration {
  id: string;
  oldGuid: string;                 // Parameters Service GUID
  newGuid: string;                 // New shared parameter GUID
  parameterName: string;
  migratedAt: Date;
  migratedBy: string;
  status: 'pending' | 'completed' | 'failed';
}
```

### API Endpoints (Web App)

#### 1. Download Shared Parameters File
```
GET /api/shared-parameters/download
Headers: Authorization: Bearer {token}
Response: text/plain (shared parameters file content)
```

#### 2. Get Parameter Definitions (JSON)
```
GET /api/shared-parameters/definitions
Headers: Authorization: Bearer {token}
Response: JSON array of SharedParameter objects
```

#### 3. Create/Update Parameter
```
POST /api/shared-parameters/parameters
PUT /api/shared-parameters/parameters/:id
Headers: Authorization: Bearer {token}
Body: SharedParameter object
```

#### 4. Get Migration Mapping
```
GET /api/shared-parameters/migration-map
Headers: Authorization: Bearer {token}
Response: JSON array of ParameterMigration objects
```

---

## Revit Plugin Architecture

### New Services

#### SharedParametersService.cs
```csharp
namespace Miller_Craft_Tools.Services
{
    /// <summary>
    /// Manages shared parameters integration with web app
    /// </summary>
    public class SharedParametersService
    {
        private readonly HttpClientHelper _httpClient;
        private readonly string _cacheDirectory;
        
        /// <summary>
        /// Downloads shared parameters file from web app
        /// </summary>
        public async Task<string> DownloadSharedParametersFileAsync();
        
        /// <summary>
        /// Loads shared parameters file into Revit
        /// </summary>
        public DefinitionFile LoadSharedParametersFile(Application app, string filePath);
        
        /// <summary>
        /// Gets parameter definition by name
        /// </summary>
        public ExternalDefinition GetParameterDefinition(DefinitionFile defFile, string paramName);
        
        /// <summary>
        /// Binds parameter to categories in project
        /// </summary>
        public bool BindParameterToCategories(Document doc, ExternalDefinition def, 
            List<BuiltInCategory> categories, bool isInstance);
    }
}
```

#### ParameterMigrationService.cs
```csharp
namespace Miller_Craft_Tools.Services
{
    /// <summary>
    /// Handles migration from Parameters Service to new system
    /// </summary>
    public class ParameterMigrationService
    {
        /// <summary>
        /// Gets migration mapping from web app
        /// </summary>
        public async Task<Dictionary<string, string>> GetMigrationMappingAsync();
        
        /// <summary>
        /// Migrates parameter values from old to new parameters
        /// </summary>
        public void MigrateProjectParameters(Document doc, Dictionary<string, string> guidMapping);
        
        /// <summary>
        /// Migrates family parameters
        /// </summary>
        public void MigrateFamilyParameters(Document familyDoc, Dictionary<string, string> guidMapping);
        
        /// <summary>
        /// Validates migration success
        /// </summary>
        public MigrationReport ValidateMigration(Document doc);
    }
}
```

### Updated Services

#### Update SetupStandardsCommand.cs
- Replace temporary shared parameters file creation
- Use `SharedParametersService` to download from web app
- Apply parameters from web-managed definitions

---

## Migration Strategy

### Phase 1: Inventory & Mapping
1. **Identify all current parameters** from Parameters Service
2. **Document GUIDs** of existing parameters
3. **Create mapping table** in web app database
4. **Generate new GUIDs** for new system (or reuse if possible)
5. **Map parameter names** between systems

### Phase 2: Parallel Operation
1. **Deploy web app** parameter management
2. **Keep Parameters Service active** initially
3. **Test new system** with non-production projects
4. **Validate file generation** and parameter application
5. **Build confidence** in new system

### Phase 3: Gradual Migration
1. **Project-by-project migration** approach
2. **Backup all projects** before migration
3. **Run migration service** to transfer values
4. **Validate data integrity** after each project
5. **Document any issues** for resolution

### Phase 4: Full Cutover
1. **Final validation** of all migrated projects
2. **Disable Parameters Service** integration
3. **Remove legacy code** after verification period
4. **Update all documentation**
5. **Train users** on new system

---

## Risk Mitigation

### Risk: Data Loss During Migration
**Mitigation:**
- Mandatory backup before any migration
- Dry-run validation mode
- Rollback procedures documented
- Parameter value comparison reports

### Risk: GUID Conflicts
**Mitigation:**
- Careful GUID management in web app
- Unique constraint on GUID field in database
- Validation before file generation
- Conflict detection and resolution

### Risk: Performance Issues
**Mitigation:**
- File caching in Revit plugin
- Incremental updates instead of full downloads
- Asynchronous operations where possible
- Progress indicators for long operations

### Risk: Network Failures
**Mitigation:**
- Offline fallback with cached file
- Retry logic with exponential backoff
- Clear error messages to users
- Manual file download option

---

## Testing Strategy

### Unit Tests
- Shared parameters file generation
- GUID uniqueness validation
- Parameter binding logic
- Migration mapping accuracy

### Integration Tests
- Download from web app
- File parsing and loading
- Parameter application to projects
- Family parameter updates

### End-to-End Tests
- Complete migration workflow
- Bidirectional sync with new parameters
- Multi-user scenarios
- Error recovery procedures

---

## Success Criteria

1. **Functional:**
   - All parameters successfully migrated
   - Zero data loss
   - Faster than Parameters Service
   - Reliable file generation

2. **Performance:**
   - < 5 seconds to download parameters file
   - < 30 seconds to apply all parameters to project
   - No UI freezing during operations

3. **Quality:**
   - 100% of parameter values preserved
   - All categories correctly bound
   - No GUID conflicts
   - Full audit trail

---

## Next Steps

1. Review this plan with stakeholders
2. Prioritize and schedule implementation phases
3. Set up development environment
4. Begin Phase 1: Web App Infrastructure
5. Coordinate between Revit plugin and web app teams

---

## Dependencies

### Revit Plugin
- Existing `HttpClientHelper.cs`
- Authentication infrastructure
- Project GUID management

### Web App
- MongoDB or SQL database
- Authentication system
- File storage/generation capability
- REST API framework

---

## Timeline Estimate

- **Phase 1 (Web App):** 2 weeks
- **Phase 2 (Plugin):** 1-2 weeks
- **Phase 3 (Migration):** 1-2 weeks
- **Phase 4 (Testing):** 1 week
- **Total:** 5-7 weeks

**Note:** Timeline assumes full-time development and may vary based on team availability and complexity of migration.

---

*This document will be updated as implementation progresses and new requirements are discovered.*
