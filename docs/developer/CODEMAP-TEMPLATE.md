# CODEMAP Entry Templates

This file contains templates for consistently documenting components in CODEMAP.md

---

## Command Template

```markdown
#### [CommandName]Command (X,XXX bytes)

**Purpose**: [One-line description of what the command does]

**Features**:
- [Key feature 1]
- [Key feature 2]
- [Key feature 3]

**Implementation Notes**:
- [Important pattern used]
- [Integration with other components]
- [UI type: WinForms/WPF/TaskDialog]

**Related Components**: [ServiceName], [UtilityName]
```

---

## Service Template

```markdown
### [ServiceName] (X,XXX bytes)

**Purpose**: [One-line description of service responsibility]

**Key Responsibilities**:
- [Responsibility 1]
- [Responsibility 2]
- [Responsibility 3]

**Key Methods**:
```csharp
ReturnType MethodName(paramType param)
ReturnType AnotherMethod(params)
```

**Dependencies**:
- [Other service/utility it depends on]

**Used By**: [Commands that use this service]

**Patterns/Notes**:
- [Important implementation pattern]
- [Known issues or gotchas]
- [Error handling approach]
```

---

## Utility/Helper Template

```markdown
### [UtilityName] (X,XXX bytes)

**Purpose**: [One-line description]

**Key Methods**:
```csharp
ReturnType MethodName(params)
```

**Critical Pattern**: [If there's a MUST-FOLLOW pattern, highlight it]

**Usage Example**:
```csharp
// Show how to use it correctly
var result = UtilityName.Method(param);
```

**⚠️ Common Mistakes**:
- [What NOT to do]
- [Why it matters]
```

---

## Controller Template

```markdown
### [ControllerName] (X,XXX bytes)

**Purpose**: [Controller responsibility]

**Partial Class Files** (if split):
- `[ControllerName].cs` - Main implementation (X,XXX bytes)
- `[ControllerName].FeatureA.cs` - Feature A implementation (X,XXX bytes)
- `[ControllerName].FeatureB.cs` - Feature B implementation (X,XXX bytes)

**Key Operations**:
- [Operation 1]
- [Operation 2]

**Integration Points**:
- [Service/component it integrates with]

**Patterns**:
- [Transaction handling approach]
- [Error recovery strategy]
```

---

## UI Component Template

```markdown
### [DialogName] (X,XXX bytes)

**Type**: [WinForms/WPF]

**Purpose**: [What the dialog does]

**Key UI Elements**:
- [Major control 1]
- [Major control 2]
- [Major control 3]

**Data Binding** (if MVVM):
- ViewModel: [ViewModelName]
- Key Properties: [prop1, prop2]

**User Actions**:
- [Action 1] → [What happens]
- [Action 2] → [What happens]

**Validation**:
- [What's validated]
- [How errors are shown]

**Related Components**: [ViewModel], [Service]
```

---

## Bug Fix Documentation Template

When documenting a bug fix in CODEMAP, add this section to the relevant component:

```markdown
**⚠️ Known Issues Fixed**:
- **[Issue description]** (Fixed: YYYY-MM-DD)
  - Root Cause: [What caused it]
  - Solution: [How it was fixed]
  - Pattern to Follow: [How to avoid it in future]
  
  ```csharp
  // ❌ WRONG - Don't do this
  BadCode();
  
  // ✅ CORRECT - Do this instead
  GoodCode();
  ```
```

---

## New Pattern Documentation Template

When discovering a new pattern that should be followed:

```markdown
**🎯 Established Pattern**:
- **Pattern Name**: [Short name]
- **When to Use**: [Situation/context]
- **Implementation**:
  ```csharp
  // Code example showing the pattern
  ```
- **Why**: [Explanation of benefits]
- **Related**: [Similar patterns or components]
```

---

## Namespace Conflict Documentation

For components with namespace issues:

```markdown
**⚠️ Namespace Conflicts**:
This component requires fully qualified types for:
- `Autodesk.Revit.ApplicationServices.Application` (not just `Application`)
- `System.Threading.Timer` (not just `Timer`)
- [Add others as needed]

**Reason**: [Explain why - e.g., "Conflicts with System.Windows.Forms.Application"]
```

---

## API Endpoint Documentation (for web-integrated services)

```markdown
**API Endpoints**:

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/path` | GET | [What it does] |
| `/api/path` | POST | [What it does] |

**Authentication**: [Bearer token / OAuth2 / etc.]

**Request Format**:
```json
{
  "field": "value"
}
```

**Response Format**:
```json
{
  "result": "data"
}
```
```

---

## Dependency Documentation

For components with important dependencies:

```markdown
**Dependencies**:
- **NuGet**: [Package name] v[version] - [Why needed]
- **Revit API**: [Specific version requirements]
- **External**: [Third-party services/APIs]

**Build Requirements**:
- [Special build configuration]
- [Platform requirements]
```

---

## Update Frequency Guide

| Change Type | Update Priority | When |
|-------------|----------------|------|
| New command added | **IMMEDIATE** | After implementation |
| New service created | **IMMEDIATE** | After implementation |
| New utility added | **IMMEDIATE** | After implementation |
| Bug fix with pattern | **HIGH** | After fix verified |
| File split/refactor | **HIGH** | After completion |
| Minor method change | **LOW** | Batch with other updates |
| Comment updates | **SKIP** | Not needed in CODEMAP |

---

## Quick Checklist

Before closing a session, verify:
- [ ] All new files documented in CODEMAP
- [ ] File sizes updated if changed significantly
- [ ] New patterns documented
- [ ] Bug fixes recorded with solutions
- [ ] Cross-references added
- [ ] Namespace conflicts noted
- [ ] Critical methods listed

---

## File Size Reference

Always include byte count for components:
- Find in Windows Explorer → Right-click → Properties → Size
- Or use: `(Get-Item "filepath").Length` in PowerShell
- Update when file size changes significantly (>20%)

---

## Linking Guidelines

Cross-reference related components:
```markdown
**Related Components**: 
- Uses: [ServiceName](#servicename)
- Called by: [CommandName](#commandname)
- Similar to: [OtherComponent](#othercomponent)
```

---

**Last Updated**: October 24, 2025

*This is a living document - update templates as patterns evolve*
