# JSON Validation in MOBAflow

**Scope:** Solution JSON file validation  
**Status:** Production  
**Last Updated:** 2026-07-20

---

## Overview

MOBAflow uses **JSON schema-style validation** to ensure that only
compatible solution files can be loaded. This prevents:

❌ Corrupted JSON files  
❌ Incorrect data structures  
❌ Incompatible schema versions  
❌ Missing required properties  

---

## Architecture

### Components

- **`Common/Validation/JsonValidationService.cs`:** Central validation
  logic
- **`Domain/Solution.cs`:** Schema version (`SchemaVersion` property)
- **`MOBAflow/Service/IoService.cs`:** Validation before deserialization in the WinUI host
- **`Test/Common/JsonValidationServiceTests.cs`:** unit tests

### Flow

```text
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

**Constant:** `Solution.CurrentSchemaVersion = 4`

### JSON example

```json
{
  "name": "My Model Railroad",
  "schemaVersion": 4,
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

- **Missing `schemaVersion`:** Error, file will **not** be loaded
- **Wrong version:** Error, file will **not** be loaded

---

## Validation rules

### 1. JSON syntax

```csharp
JsonDocument.Parse(json)
```

**Error examples:**

```text
❌ Invalid JSON format: Unexpected character '{' at position 42.
❌ Invalid JSON format: Expected ',' or '}' after property value.
```

### 2. Root element

```csharp
if (root.ValueKind != JsonValueKind.Object)
    return Failure("JSON root must be an object.");
```

**Error example:**

```text
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

```text
❌ Missing required property: 'name'.
❌ Missing required property: 'projects'.
```

### 4. Data types

```csharp
if (projectsElement.ValueKind != JsonValueKind.Array)
    return Failure("Property 'projects' must be an array.");
```

**Error example:**

```text
❌ Property 'projects' must be an array.
```

### 5. Schema version (optional)

```csharp
if (requiredSchemaVersion.HasValue)
{
    if (!root.TryGetProperty("schemaVersion", out var versionElement))
        return Failure(
            $"Missing schema version. Expected version {requiredSchemaVersion.Value}.");

    if (!versionElement.TryGetInt32(out var actualVersion))
        return Failure("Schema version must be a number.");

    if (actualVersion != requiredSchemaVersion.Value)
        return Failure(
            $"Incompatible schema version. Expected " +
            $"{requiredSchemaVersion.Value}, found {actualVersion}.");
}
```

**Error examples:**

```text
❌ Missing required property: 'schemaVersion'.
❌ Schema version must be a number.
❌ Incompatible schema version. Expected 4, found 999.
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

```text
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

```text
❌ Failed to load solution: Invalid solution file: Missing required property: 'projects'
```

---

## Tests

### Test file

`Test/Common/JsonValidationServiceTests.cs`

### Covered test scenarios

- **`Validate_EmptyString_ShouldFail`:** Empty string
- **`Validate_WhitespaceOnly_ShouldFail`:** Whitespace only
- **`Validate_InvalidJson_ShouldFail`:** Invalid JSON syntax
- **`Validate_JsonArray_ShouldFail`:** Root is array instead of object
- **`Validate_MissingNameProperty_ShouldFail`:** Missing `name`
- **`Validate_MissingProjectsProperty_ShouldFail`:** Missing `projects`
- **`Validate_ProjectsNotArray_ShouldFail`:** `projects` is not an
  array
- **`Validate_ProjectMissingName_ShouldFail`:** Project without `name`
- **`Validate_ProjectNotObject_ShouldFail`:** Project is not an object
- **`Validate_ValidMinimalJson_ShouldSucceed`:** Minimal JSON (empty)
- **`Validate_ValidJsonWithProjects_ShouldSucceed`:** Valid solution
  with projects
- **`Validate_MissingSchemaVersion_WithRequiredVersion_ShouldFail`:**
  Schema version missing
- **`Validate_WrongSchemaVersion_ShouldFail`:** Wrong version
- **`Validate_InvalidSchemaVersionType_ShouldFail`:** Version is
  string instead of number
- **`Validate_CorrectSchemaVersion_ShouldSucceed`:** Correct version
- **`Validate_NoSchemaVersionRequired_ShouldSucceed`:** No version
  required

### Running tests

```bash
dotnet test Test/Test.csproj --filter "FullyQualifiedName~JsonValidationServiceTests"
```

**Result:**

```text
Test summary: total: 16; failed: 0; succeeded: 16; skipped: 0
```

---

## Best practices

### ✅ DO

- Always persist the schema version in new solution files
- Return clear error messages to the user
- Validate **before** deserialization
- Increment the version on breaking changes

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
- ✅ Explicit current-schema enforcement
- ✅ High test coverage for validation

---

**Status:** Implemented and covered by automated tests
**Owner:** `Common/Validation/JsonValidationService.cs`  
**Tests:** `Test/Common/JsonValidationServiceTests.cs`

## Related current model notes

The current sample solution also contains rolling-stock and display data that
is not shown in the minimal examples above:

- `Project.Locomotives`, `Project.PassengerWagons`, and `Project.GoodsWagons`
  store the vehicle libraries.
- `Project.Trains` uses `Train.Vehicles` as the canonical, ordered, mixed
  consist model. Legacy split lists such as locomotive IDs and wagon IDs are
  not the canonical representation.
- Workflow actions use typed payload objects such as `announcement`, `audio`,
  `command`, `executeScript`, `selectSignalAspect`, `changeJourneyStop`, and
  `trainDestinationDisplay`.
- `Project.TimetableServices` stores dated service definitions and references
  existing journeys, trains, stations, platforms and journey stops. Mutable
  operator decisions are persisted separately by the timetable state store.

See [`PROJECT-REFERENCE.md`](PROJECT-REFERENCE.md) for the full current data
model overview.
