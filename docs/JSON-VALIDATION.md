# JSON Validation in MOBAflow

**Scope:** Solution JSON file validation  
**Status:** Production  
**Last Updated:** 2026-02-05

---

## Overview

MOBAflow uses **JSON schema-style validation** to ensure that only compatible solution files can be loaded. This prevents:

❌ Corrupted JSON files  
❌ Incorrect data structures  
❌ Incompatible schema versions  
❌ Missing required properties  

---

## Architecture

### Components

| Component | Purpose |
|----------|---------|
| `Common/Validation/JsonValidationService.cs` | Central validation logic |
| `Domain/Solution.cs` | Schema version (`SchemaVersion` property) |
| `WinUI/Service/IoService.cs` | Validation before deserialization |
| `Test/Common/JsonValidationTests.cs` | 16+ unit tests |

### Flow

```
User opens .json file
    ↓
IoService.LoadAsync()
    ↓
File.ReadAllTextAsync()
    ↓
JsonValidationService.Validate()
    ↓
    ├─ Syntax check (JsonDocument.Parse)
    ├─ Structure check (required properties present?)
    ├─ Schema version check
    └─ Project structure check
    ↓
✅ Valid → Deserialization
    ↓
Solution loaded

❌ Invalid → Show error
    ↓
User gets a clear error message
```

---

## Schema version

### Current

**Constant:** `Solution.CurrentSchemaVersion = 1`

### JSON example

```json
{
  "name": "My Model Railroad",
  "schemaVersion": 1,
  "projects": [
    {
      "name": "Main Project",
      "workflows": [],
      "trains": []
    }
  ]
}
```

### Version checks

- **Missing `schemaVersion`:** Warning, but allowed (for legacy files)
- **Wrong version:** Error, file will **not** be loaded
- **Future versions:** Auto-migration or upgrade hint (planned)

---

## Validation rules

### 1. JSON syntax

```csharp
JsonDocument.Parse(json)
```

**Error examples:**
```
❌ Invalid JSON format: Unexpected character '{' at position 42.
❌ Invalid JSON format: Expected ',' or '}' after property value.
```

### 2. Root element

```csharp
if (root.ValueKind != JsonValueKind.Object)
    return Failure("JSON root must be an object.");
```

**Error example:**
```
❌ JSON root must be an object.
```

### 3. Required properties

```csharp
if (!root.TryGetProperty("name", out _))
    return Failure("Missing required property: 'name'.");

if (!root.TryGetProperty("projects", out var projectsElement))
    return Failure("Missing required property: 'projects'.");
```

**Error examples:**
```
❌ Missing required property: 'name'.
❌ Missing required property: 'projects'.
```

### 4. Data types

```csharp
if (projectsElement.ValueKind != JsonValueKind.Array)
    return Failure("Property 'projects' must be an array.");
```

**Error example:**
```
❌ Property 'projects' must be an array.
```

### 5. Schema version (optional)

```csharp
if (requiredSchemaVersion.HasValue)
{
    if (!root.TryGetProperty("schemaVersion", out var versionElement))
        return Failure($"Missing schema version. Expected version {requiredSchemaVersion.Value}.");

    if (!versionElement.TryGetInt32(out var actualVersion))
        return Failure("Schema version must be a number.");

    if (actualVersion != requiredSchemaVersion.Value)
        return Failure($"Incompatible schema version. Expected {requiredSchemaVersion.Value}, found {actualVersion}.");
}
```

**Error examples:**
```
❌ Missing schema version. Expected version 1.
❌ Schema version must be a number.
❌ Incompatible schema version. Expected 1, found 999.
```

### 6. Project structure

```csharp
foreach (var project in projectsElement.EnumerateArray())
{
    if (project.ValueKind != JsonValueKind.Object)
        return Failure($"Project at index {index} is not an object.");

    if (!project.TryGetProperty("name", out _))
        return Failure($"Project at index {index} is missing 'name' property.");
}
```

**Error examples:**
```
❌ Project at index 0 is not an object.
❌ Project at index 1 is missing 'name' property.
```

---

## API

### JsonValidationService.Validate()

```csharp
public static JsonValidationResult Validate(
    string json, 
    int? requiredSchemaVersion = null)
```

**Parameters:**
- `json` - Raw JSON string
- `requiredSchemaVersion` - Expected schema version (optional)

**Return type:**
```csharp
public class JsonValidationResult
{
    public bool IsValid { get; }
    public string? ErrorMessage { get; }
}
```

**Usage:**

```csharp
var json = await File.ReadAllTextAsync(filePath);

var result = JsonValidationService.Validate(json, Solution.CurrentSchemaVersion);

if (!result.IsValid)
{
    return (null, null, $"Invalid solution file: {result.ErrorMessage}");
}

var solution = JsonSerializer.Deserialize<Solution>(json);
```

---

## Error handling

### In `IoService`

```csharp
public async Task<(Solution? solution, string? path, string? error)> LoadAsync()
{
    // ...
    var json = await File.ReadAllTextAsync(result.Path);
    
    // ✅ Validation BEFORE deserialization
    var validationResult = JsonValidationService.Validate(json, Solution.CurrentSchemaVersion);
    if (!validationResult.IsValid)
    {
        return (null, null, $"Invalid solution file: {validationResult.ErrorMessage}");
    }

    try
    {
        var sol = JsonSerializer.Deserialize<Solution>(json, JsonOptions.Default);
        return (sol, result.Path, null);
    }
    catch (JsonException ex)
    {
        return (null, null, $"Failed to parse JSON: {ex.Message}");
    }
}
```

### In `MainWindowViewModel`

```csharp
var (loadedSolution, path, error) = await _ioService.LoadAsync();

if (!string.IsNullOrEmpty(error))
{
    throw new InvalidOperationException($"Failed to load solution: {error}");
}
```

**User sees:**
```
❌ Failed to load solution: Invalid solution file: Missing required property: 'projects'
```

---

## Tests

### Test file

`Test/Common/JsonValidationTests.cs`

### Test scenarios (16 tests)

| Test | Scenario |
|------|----------|
| `Validate_EmptyString_ShouldFail` | Empty string |
| `Validate_WhitespaceOnly_ShouldFail` | Whitespace only |
| `Validate_InvalidJson_ShouldFail` | Invalid JSON syntax |
| `Validate_JsonArray_ShouldFail` | Root is array instead of object |
| `Validate_MissingNameProperty_ShouldFail` | Missing `name` |
| `Validate_MissingProjectsProperty_ShouldFail` | Missing `projects` |
| `Validate_ProjectsNotArray_ShouldFail` | `projects` is not an array |
| `Validate_ProjectMissingName_ShouldFail` | Project without `name` |
| `Validate_ProjectNotObject_ShouldFail` | Project is not an object |
| `Validate_ValidMinimalJson_ShouldSucceed` | Minimal JSON (empty) |
| `Validate_ValidJsonWithProjects_ShouldSucceed` | Valid solution with projects |
| `Validate_MissingSchemaVersion_WithRequiredVersion_ShouldFail` | Schema version missing |
| `Validate_WrongSchemaVersion_ShouldFail` | Wrong version |
| `Validate_InvalidSchemaVersionType_ShouldFail` | Version is string instead of number |
| `Validate_CorrectSchemaVersion_ShouldSucceed` | Correct version |
| `Validate_NoSchemaVersionRequired_ShouldSucceed` | No version required |

### Running tests

```bash
dotnet test --filter "FullyQualifiedName~JsonValidationTests"
```

**Result:**
```
Test summary: total: 16; failed: 0; succeeded: 16; skipped: 0
```

---

## Migration (future)

### When schema version 2 is introduced

1. **Update `Solution.CurrentSchemaVersion`:**
   ```csharp
   public const int CurrentSchemaVersion = 2;
   ```

2. **Migration implementieren:**
   ```csharp
   public static Solution MigrateFromV1(Solution oldSolution)
   {
       // Add new fields, transform data
       return newSolution;
   }
   ```

3. **In `IoService`:**
   ```csharp
   if (solution.SchemaVersion == 1)
   {
       solution = Solution.MigrateFromV1(solution);
   }
   ```

4. **Extend tests:**
   ```csharp
   [Test]
   public void MigrateFromV1_ShouldConvertCorrectly() { ... }
   ```

---

## Best practices

### ✅ DO

- Always persist the schema version in new solution files
- Return clear error messages to the user
- Validate **before** deserialization
- Increment the version on breaking changes
- Offer migration paths for legacy files

### ❌ DON'T

- Do not throw generic `JsonException` without context
- Do not silently ignore validation failures
- Do not deserialize unvalidated JSON strings
- Do not forget to update the schema version constant

---

## Summary

MOBAflow's JSON validation protects against:
- ❌ Corrupted files
- ❌ Incompatible versions
- ❌ Missing required properties
- ❌ Wrong data types

**Benefits:**
- ✅ Better error handling
- ✅ Clear user-facing error messages
- ✅ Future-proof migration support
- ✅ High test coverage for validation

---

**Status:** Implemented & tested (16 unit tests)  
**Owner:** `Common/Validation/JsonValidationService.cs`  
**Tests:** `Test/Common/JsonValidationTests.cs`
