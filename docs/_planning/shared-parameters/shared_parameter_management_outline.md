# Miller Craft Shared Parameter Management Utility — Design Outline

## Purpose
Enable robust, centralized, and standardized management of Revit shared parameters without relying on Autodesk Parameter Service or the default shared parameters text file. Ensure all projects and families use parameters with consistent GUIDs, names, and types, supporting reliable reporting and automation.

---

## Background
- **Revit Shared Parameters** are identified by GUIDs and can be used in both families and projects.
- The default workflow uses a shared parameters text file, but this is error-prone and hard to govern.
- Autodesk Parameter Service is slow and not widely adopted.
- Teams often suffer from "parameter drift" (same-named parameters with different GUIDs) due to lack of central management.

---

## Goals
- **Central Source of Truth:** Store all shared parameter definitions (name, GUID, type, group, description) in a version-controlled JSON file or web database.
- **Automated Sync:** Generate the shared parameters file and bind parameters to projects/families programmatically, using the master definitions.
- **Governance:** Prevent accidental creation of duplicate parameters and ensure all models/families use the correct GUIDs.
- **Auditing:** Provide tools to detect and repair drift or missing parameters.

---

## Key Concepts
- **GUID:** The unique identifier for a shared parameter. Must be consistent across all uses.
- **Definition Source:** Instead of editing the shared parameters text file directly, use a JSON or DB schema as the master source.
- **Revit API:** Use `Application.SharedParametersFilename`, `DefinitionFile`, `DefinitionGroup`, `ExternalDefinition`, and `BindingMap` to create and bind parameters programmatically.

---

## Proposed Workflow
1. **Define Parameters in Central Source**
    - Store all parameter definitions in a JSON file or database (fields: Name, GUID, DataType, Group, Description).
2. **Sync Utility (Revit Addin):**
    - Reads the central definitions.
    - Generates a temporary shared parameters file for Revit API operations.
    - Adds/binds parameters to projects and families using correct GUIDs.
3. **Audit & Repair:**
    - Scan projects/families for required parameters.
    - Report and optionally fix missing or mismatched GUIDs.
4. **Governance:**
    - UI for admins to add/edit parameters in the master source.
    - Versioning and change tracking.

---

## Implementation Notes
- **No Dependency on Parameter Service:** All management is local or via web API.
- **No Manual Editing:** Users never edit the shared parameters text file directly.
- **Extensible:** Easy to add new parameters or update definitions centrally.

---

## Revit API References
- `Application.SharedParametersFilename`
- `DefinitionFile`, `DefinitionGroup`, `ExternalDefinition`
- `BindingMap` for binding parameters to categories
- Ability to specify GUID when creating shared parameters

---

## Next Steps
- Prototype JSON schema for parameter definitions
- Prototype Revit addin to sync parameters from JSON to project/family
- Design UI for admins to manage shared parameters
- Implement auditing/reporting tools

---

## Quick Recap for Future Work
- The goal is to **standardize shared parameter management** by using a single source of truth (JSON/DB), programmatically syncing to Revit, and providing tools for auditing and repair.
- Avoid all manual editing of the shared parameters file and do not use Autodesk Parameter Service.
- Use Revit API to create, bind, and audit parameters by GUID.

---

*For any future work, review this outline and the referenced Revit API classes. Begin with the JSON schema and addin prototype to validate feasibility and workflow.*
