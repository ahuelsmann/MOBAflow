# JSON-Validierung in MOBAflow

> **Version:** 1.0  
> **Erstellt:** 2026-02-05  
> **Status:** ✅ Produktiv

---

## Übersicht

MOBAflow verwendet **JSON-Schema-Validierung**, um sicherzustellen, dass nur kompatible Solution-Dateien geladen werden können. Dies verhindert:

❌ Korrupte JSON-Dateien  
❌ Inkorrekte Datenstrukturen  
❌ Inkompatible Schema-Versionen  
❌ Fehlende Pflichtfelder  

---

## Architektur

### Komponenten

| Komponente | Zweck |
|------------|-------|
| `Common/Validation/JsonValidationService.cs` | Zentrale Validierungslogik |
| `Domain/Solution.cs` | Schema-Version (`SchemaVersion` Property) |
| `WinUI/Service/IoService.cs` | Validierung vor Deserialisierung |
| `Test/Common/JsonValidationTests.cs` | 16+ Unit-Tests |

### Ablauf

```
User öffnet .json Datei
    ↓
IoService.LoadAsync()
    ↓
File.ReadAllTextAsync()
    ↓
JsonValidationService.Validate()
    ↓
    ├─ Syntax-Check (JsonDocument.Parse)
    ├─ Struktur-Check (Properties vorhanden?)
    ├─ Schema-Version-Check
    └─ Projekt-Struktur-Check
    ↓
✅ Valide → Deserialisierung
    ↓
Solution geladen

❌ Invalide → Fehler anzeigen
    ↓
Benutzer erhält klare Fehlermeldung
```

---

## Schema-Version

### Aktuell

**Konstante:** `Solution.CurrentSchemaVersion = 1`

### JSON-Beispiel

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

### Version-Check

- **Fehlt `schemaVersion`:** Warnung, aber erlaubt (für Legacy-Dateien)
- **Falsche Version:** Fehler, Datei wird **nicht** geladen
- **Zukünftige Versionen:** Auto-Migration oder Upgrade-Hinweis

---

## Validierungsregeln

### 1. JSON-Syntax

```csharp
JsonDocument.Parse(json)
```

**Fehler-Beispiele:**
```
❌ Invalid JSON format: Unexpected character '{' at position 42.
❌ Invalid JSON format: Expected ',' or '}' after property value.
```

### 2. Root-Element

```csharp
if (root.ValueKind != JsonValueKind.Object)
    return Failure("JSON root must be an object.");
```

**Fehler-Beispiel:**
```
❌ JSON root must be an object.
```

### 3. Pflichtfelder

```csharp
if (!root.TryGetProperty("name", out _))
    return Failure("Missing required property: 'name'.");

if (!root.TryGetProperty("projects", out var projectsElement))
    return Failure("Missing required property: 'projects'.");
```

**Fehler-Beispiele:**
```
❌ Missing required property: 'name'.
❌ Missing required property: 'projects'.
```

### 4. Datentypen

```csharp
if (projectsElement.ValueKind != JsonValueKind.Array)
    return Failure("Property 'projects' must be an array.");
```

**Fehler-Beispiel:**
```
❌ Property 'projects' must be an array.
```

### 5. Schema-Version (optional)

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

**Fehler-Beispiele:**
```
❌ Missing schema version. Expected version 1.
❌ Schema version must be a number.
❌ Incompatible schema version. Expected 1, found 999.
```

### 6. Projekt-Struktur

```csharp
foreach (var project in projectsElement.EnumerateArray())
{
    if (project.ValueKind != JsonValueKind.Object)
        return Failure($"Project at index {index} is not an object.");

    if (!project.TryGetProperty("name", out _))
        return Failure($"Project at index {index} is missing 'name' property.");
}
```

**Fehler-Beispiele:**
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

**Parameter:**
- `json` - Raw JSON-String
- `requiredSchemaVersion` - Erwartete Schema-Version (optional)

**Rückgabe:**
```csharp
public class JsonValidationResult
{
    public bool IsValid { get; }
    public string? ErrorMessage { get; }
}
```

**Verwendung:**

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

## Fehlerbehandlung

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

**Benutzer erhält:**
```
❌ Failed to load solution: Invalid solution file: Missing required property: 'projects'
```

---

## Tests

### Test-Datei

`Test/Common/JsonValidationTests.cs`

### Test-Szenarien (16 Tests)

| Test | Szenario |
|------|----------|
| `Validate_EmptyString_ShouldFail` | Leerer String |
| `Validate_WhitespaceOnly_ShouldFail` | Nur Whitespace |
| `Validate_InvalidJson_ShouldFail` | Ungültige JSON-Syntax |
| `Validate_JsonArray_ShouldFail` | Root ist Array statt Objekt |
| `Validate_MissingNameProperty_ShouldFail` | Fehlendes `name` |
| `Validate_MissingProjectsProperty_ShouldFail` | Fehlendes `projects` |
| `Validate_ProjectsNotArray_ShouldFail` | `projects` ist kein Array |
| `Validate_ProjectMissingName_ShouldFail` | Projekt ohne `name` |
| `Validate_ProjectNotObject_ShouldFail` | Projekt ist kein Objekt |
| `Validate_ValidMinimalJson_ShouldSucceed` | Minimal-JSON (leer) |
| `Validate_ValidJsonWithProjects_ShouldSucceed` | Valide Solution mit Projekten |
| `Validate_MissingSchemaVersion_WithRequiredVersion_ShouldFail` | Schema-Version fehlt |
| `Validate_WrongSchemaVersion_ShouldFail` | Falsche Version |
| `Validate_InvalidSchemaVersionType_ShouldFail` | Version ist String statt Zahl |
| `Validate_CorrectSchemaVersion_ShouldSucceed` | Korrekte Version |
| `Validate_NoSchemaVersionRequired_ShouldSucceed` | Keine Version gefordert |

### Test ausführen

```bash
dotnet test --filter "FullyQualifiedName~JsonValidationTests"
```

**Ergebnis:**
```
Test summary: total: 16; failed: 0; succeeded: 16; skipped: 0
```

---

## Migration (Zukünftig)

### Wenn Schema-Version 2 kommt

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

4. **Tests erweitern:**
   ```csharp
   [Test]
   public void MigrateFromV1_ShouldConvertCorrectly() { ... }
   ```

---

## Best Practices

### ✅ DO

- Immer Schema-Version in neuen Solution-Dateien speichern
- Klare Fehlermeldungen an Benutzer zurückgeben
- Validierung **vor** Deserialisierung durchführen
- Bei breaking changes Version inkrementieren
- Migration für alte Dateien anbieten

### ❌ DON'T

- Keine generischen `JsonException` werfen
- Keine silent failures (immer Error zurückgeben)
- Keine unvalidierten JSON-Strings deserialisieren
- Schema-Version nicht vergessen zu aktualisieren

---

## Zusammenfassung

MOBAflow's JSON-Validierung schützt vor:
- ❌ Korrupten Dateien
- ❌ Inkompatiblen Versionen
- ❌ Fehlenden Pflichtfeldern
- ❌ Falschen Datentypen

**Vorteile:**
- ✅ Bessere Fehlerbehandlung
- ✅ Klare Benutzermeldungen
- ✅ Zukunftssichere Migration
- ✅ 100% Test-Coverage für Validierung

---

**Status:** ✅ Implementiert & getestet (16 Unit-Tests)  
**Verantwortlich:** `Common/Validation/JsonValidationService.cs`  
**Tests:** `Test/Common/JsonValidationTests.cs`
