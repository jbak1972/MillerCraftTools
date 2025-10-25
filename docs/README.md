# Miller Craft Tools - Documentation

**Last Updated:** October 24, 2025

---

## 📁 Documentation Structure

### Active Documentation (Root Level)

These are the most frequently accessed documents:

- **`VERSION-HISTORY.md`** - Version tracking and release notes
- **`BIDIRECTIONAL_SYNC_IMPLEMENTATION.md`** - Current sync implementation details
- **`Automatic-Parameter-Creation.md`** - Auto-creation of sp.MC.ProjectGUID parameter
- **`ParameterHelper-Usage-Guide.md`** - Guide for using ParameterHelper utility
- **`REVIT_PLUGIN_DEVELOPER_GUIDE.md`** - Comprehensive developer guide
- **`CLEANUP-PLAN.md`** - This cleanup plan (can be archived after review)

### `/developer/` - Developer Resources

Tools and templates for developers (both AI and human):

- **`CODEMAP-TEMPLATE.md`** - Templates for documenting components in CODEMAP.md

### `/reference/` - Reference Materials

#### `/reference/images/`
Screenshots and diagrams:
- `Project-Parameters.png` - All 46 project parameters screenshot
- `ProjectGUID-no-shared-param.png` - Error when GUID parameter missing
- `Upload-error.png` - Sync upload error screenshot

#### `/reference/test-data/`
Test files and data for development:
- `project_info_*.json` - Test project info exports
- `revit-api-testing.md` - Revit API testing notes
- `revit-test-sample.cs` - Sample test code
- `token-authentication-guide.md` - Authentication testing guide

### `/_planning/` - Future Features

Documentation for features not yet implemented:

#### `/_planning/shared-parameters/`
**5-7 Week Project** - Web-based shared parameters management system:
- `SHARED_PARAMETERS_COORDINATION_SUMMARY.md` - Executive summary
- `SHARED_PARAMETERS_IMPLEMENTATION_PLAN.md` - Implementation plan
- `REVIT_SHARED_PARAMETERS_TECHNICAL_GUIDE.md` - Technical details
- `WEBAPP_SHARED_PARAMETERS_PROMPT.md` - Web app team specification
- `shared_parameter_management_outline.md` - Original outline

### `/_archive/` - Historical Documentation

Completed work and session summaries preserved for reference:

#### `/_archive/implementation/`
Completed implementation documentation:
- `IMPLEMENTATION-ROADMAP.md` - Original roadmap
- `Implementation-Complete-Summary.md` - Completion summary
- `Integration-Gap-Analysis.md` - Gap analysis (completed)
- `Phase-0-Implementation-Complete.md` - Phase 0 summary
- `Phase-1-UI-Consolidation-Complete.md` - Phase 1 summary
- `Ribbon-Cleanup-Plan.md` - Ribbon cleanup documentation
- `Services_Structure.md` - Services architecture
- `Network_Communication_Improvements.md` - Network improvements
- `connection-manager-implementation.md` - Connection manager work
- `web-login-integration.md` - Login integration
- `web-sync-consolidation-review.md` - Sync consolidation
- `Sync-Error-Fixes.md` - Sync error fixes

#### `/_archive/sessions/`
Session summaries and notes:
- `Session-Summary-Oct-23-2025.md` - Oct 23 session

---

## 🎯 Finding Documentation

### "I need to understand how sync works"
→ **`BIDIRECTIONAL_SYNC_IMPLEMENTATION.md`**

### "I need to understand the codebase structure"
→ **`../CODEMAP.md`** (in base directory)

### "I want to add a new feature"
1. Check **`../CODEMAP.md`** for existing patterns
2. Check **`developer/CODEMAP-TEMPLATE.md`** for documentation templates
3. Check **`REVIT_PLUGIN_DEVELOPER_GUIDE.md`** for guidelines

### "I need version history"
→ **`VERSION-HISTORY.md`**

### "I need test data or screenshots"
→ **`reference/`** folder

### "I want to see what's planned for the future"
→ **`_planning/`** folder

### "I want to see how something was implemented"
→ **`_archive/implementation/`** folder

---

## 📝 Documentation Conventions

### File Naming
- **Active docs:** Descriptive names in PascalCase or kebab-case
- **Archive docs:** Preserved with original names
- **Images:** Descriptive kebab-case names

### Markdown Style
- Use `#` for main title (once per document)
- Use `##` for major sections
- Use `###` for subsections
- Use `**bold**` for emphasis
- Use `code blocks` for code examples
- Include "Last Updated" date at top

### Updates
- Update relevant docs when features change
- Move completed implementation docs to `_archive/implementation/`
- Move session summaries to `_archive/sessions/`
- Keep active docs focused on current state

---

## 🧹 Maintenance

This structure was established on **October 24, 2025** to organize 38 files into a clear, maintainable structure.

### When to Archive
- Implementation is complete and documented in CODEMAP
- Session is finished
- Feature has been replaced

### When to Delete
- Document is truly outdated (check first!)
- Content is redundant with better documentation
- Planning document for cancelled feature

### When to Update Root Docs
- Feature implementation changes
- New patterns discovered
- API changes
- Version updates

---

**Questions?** Check **`../CODEMAP.md`** first - it's the authoritative reference for the current codebase.
