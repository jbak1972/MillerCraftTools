# Documentation Cleanup Plan
**Date:** October 24, 2025

---

## Current State

**Base Directory:** 4 non-code files  
**docs/ Folder:** 38 files (too many!)

---

## Cleanup Strategy

### 🎯 Goals
1. Keep only **actively referenced** documentation
2. Archive **completed** implementation docs
3. Consolidate **overlapping** guides
4. Remove **truly outdated** files
5. Organize by **topic/purpose**

---

## Proposed Structure

```
docs/
├── _archive/                          # Completed/historical docs
│   ├── implementation/                # Completed implementation plans
│   └── sessions/                      # Session summaries
│
├── _planning/                         # Future features (not yet started)
│   └── shared-parameters/            # 5-7 week project docs
│
├── reference/                         # Current feature documentation
│   ├── images/                       # Screenshots and diagrams
│   └── test-data/                    # Test JSON files
│
├── developer/                         # For developers (AI & human)
│   ├── CODEMAP-TEMPLATE.md
│   └── guides/
│
└── [Active root-level docs]          # Frequently accessed
```

---

## File-by-File Analysis

### ✅ KEEP (Active - Root Level)

| File | Reason | Action |
|------|--------|--------|
| `CODEMAP.md` | **Core reference** - used every session | Keep at root |
| `VERSION-HISTORY.md` | Version tracking | Keep at root |
| `BIDIRECTIONAL_SYNC_IMPLEMENTATION.md` | Current feature doc | Keep at root |
| `Automatic-Parameter-Creation.md` | Just created today! | Keep at root |
| `ParameterHelper-Usage-Guide.md` | Active utility guide | Keep at root |

### 📁 ORGANIZE (Active - Move to Folders)

**To `developer/`:**
- `CODEMAP-TEMPLATE.md` → `developer/CODEMAP-TEMPLATE.md`

**To `reference/images/`:**
- `Project Parameters.png` → `reference/images/Project-Parameters.png`
- `ProjectGUID no shared param.png` → `reference/images/ProjectGUID-no-shared-param.png`
- `Upload error.png` → `reference/images/Upload-error.png`

**To `reference/test-data/`:**
- `project_info_d154d39c-6e12-4a81-8a6a-234a00ee0300.json` → `reference/test-data/`
- `test/` folder contents → `reference/test-data/`

**To `_planning/shared-parameters/`:**
- `SHARED_PARAMETERS_COORDINATION_SUMMARY.md`
- `SHARED_PARAMETERS_IMPLEMENTATION_PLAN.md`
- `REVIT_SHARED_PARAMETERS_TECHNICAL_GUIDE.md`
- `WEBAPP_SHARED_PARAMETERS_PROMPT.md`
- `shared_parameter_management_outline.md`

### 📦 ARCHIVE (Completed Work)

**To `_archive/implementation/`:**
- `IMPLEMENTATION-ROADMAP.md` - Original roadmap (now complete)
- `Implementation-Complete-Summary.md` - Completion summary
- `Integration-Gap-Analysis.md` - Gap analysis (gaps filled)
- `Phase-0-Implementation-Complete.md` - Phase 0 done
- `Phase-1-UI-Consolidation-Complete.md` - Phase 1 done
- `Ribbon-Cleanup-Plan.md` - Ribbon cleanup done
- `Services_Structure.md` - Services structure established
- `Network_Communication_Improvements.md` - Improvements done
- `connection-manager-implementation.md` - Connection manager done
- `web-login-integration.md` - Login integration done
- `web-sync-consolidation-review.md` - Consolidation done
- `Sync-Error-Fixes.md` - Errors fixed

**To `_archive/sessions/`:**
- `Session-Summary-Oct-23-2025.md` - Session summary from Oct 23

### 🗑️ DELETE (Outdated/Redundant)

**Consolidate into single guide:**
- ❌ `REVIT_PLUGIN_DEVELOPER_GUIDE.md` - Keep this one
- ❌ `REVIT_PLUGIN_INTEGRATION_PROMPT.md` - Redundant
- ❌ `miller-craft-revit-unified-integration-guide.md` - Redundant
- ❌ `revit-integration-master-document.md` - Redundant
- ❌ `revit-plugin-integration-guide.md` - Redundant
- ❌ `revit-plugin-integration-implementation-plan.md` - Plan complete
- ❌ `revit-web-integration-implementation.md` - Redundant
- ❌ `revit-synch-plan.md` - Plan complete
- ❌ `revit-ui-implementation-guidelines.md` - Covered in CODEMAP
- ❌ `revit-standards.md` - Info now in CODEMAP

**Truly outdated:**
- ❌ Base directory: `Claude_Outline.md` - Original planning, now outdated

---

## Consolidation: Developer Guide

**Keep:** `REVIT_PLUGIN_DEVELOPER_GUIDE.md`  
**Delete:** All other integration/developer guides  
**Action:** Ensure REVIT_PLUGIN_DEVELOPER_GUIDE.md has all essential info

---

## Final Structure

```
Miller Craft Tools/
├── CODEMAP.md                                    ← Core reference
├── LICENSE.txt
├── build-check.ps1
├── CopyToAddins.bat
├── CopyToAddins.ps1
│
└── docs/
    ├── VERSION-HISTORY.md
    ├── BIDIRECTIONAL_SYNC_IMPLEMENTATION.md
    ├── Automatic-Parameter-Creation.md
    ├── ParameterHelper-Usage-Guide.md
    ├── REVIT_PLUGIN_DEVELOPER_GUIDE.md
    │
    ├── developer/
    │   └── CODEMAP-TEMPLATE.md
    │
    ├── reference/
    │   ├── images/
    │   │   ├── Project-Parameters.png
    │   │   ├── ProjectGUID-no-shared-param.png
    │   │   └── Upload-error.png
    │   │
    │   └── test-data/
    │       ├── project_info_*.json (2 files)
    │       ├── revit-api-testing.md
    │       ├── revit-test-sample.cs
    │       └── token-authentication-guide.md
    │
    ├── _planning/
    │   └── shared-parameters/
    │       ├── SHARED_PARAMETERS_COORDINATION_SUMMARY.md
    │       ├── SHARED_PARAMETERS_IMPLEMENTATION_PLAN.md
    │       ├── REVIT_SHARED_PARAMETERS_TECHNICAL_GUIDE.md
    │       ├── WEBAPP_SHARED_PARAMETERS_PROMPT.md
    │       └── shared_parameter_management_outline.md
    │
    └── _archive/
        ├── implementation/
        │   ├── IMPLEMENTATION-ROADMAP.md
        │   ├── Implementation-Complete-Summary.md
        │   ├── Integration-Gap-Analysis.md
        │   ├── Phase-0-Implementation-Complete.md
        │   ├── Phase-1-UI-Consolidation-Complete.md
        │   ├── Ribbon-Cleanup-Plan.md
        │   ├── Services_Structure.md
        │   ├── Network_Communication_Improvements.md
        │   ├── connection-manager-implementation.md
        │   ├── web-login-integration.md
        │   ├── web-sync-consolidation-review.md
        │   └── Sync-Error-Fixes.md
        │
        └── sessions/
            └── Session-Summary-Oct-23-2025.md
```

---

## Impact Summary

### Before
- 38 files in docs/
- 4 non-code files in base directory
- Difficult to find current documentation
- Many overlapping/outdated guides

### After
- **5 active docs** at docs root (easy to find)
- **1 developer folder** (templates/guides)
- **1 reference folder** (images, test data)
- **1 planning folder** (future features)
- **1 archive folder** (historical docs)
- **Delete 11 redundant files**

**Total docs reduction: 38 → 27 files** (organized into clear structure)

---

## Execution Steps

1. ✅ Create folder structure
2. ✅ Move files to appropriate folders
3. ✅ Update any cross-references in remaining docs
4. ✅ Delete redundant files
5. ✅ Create README.md in docs/ explaining structure
6. ✅ Test that CODEMAP still references correct paths

---

**Ready to execute?** This will make the documentation **much** more maintainable!
