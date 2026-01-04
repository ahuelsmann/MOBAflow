# 📋 TODO-Liste: MOBAflow Modernisierung

**Erstellt:** 2025-01-XX  
**Status:** In Planung  
**Geschätzte Gesamtzeit:** 6-8 Stunden

---

## ✅ Abgeschlossen

- [x] **COMException-Fix**: `OnSelectedProjectChanged()` mit `_uiDispatcher.InvokeOnUi()` verzögert
- [x] **Selection-Reset**: `ClearAllSelections()` in `NewSolutionAsync()`, `ApplyLoadedSolution()`, `DeleteProject()`
- [x] **IoService modernisiert**: 
  - [x] Ungenutztes `_uiDispatcher` Feld entfernt
  - [x] `EnsureInitialized()` Helper-Methode
  - [x] Atomic File Writes bei `SaveAsync()`
  - [x] `NormalizePhotoCategory()` mit Validierung
  - [x] Plattform-agnostische Pfad-Separatoren
- [x] **Legacy-Pattern-Analyse** durchgeführt

---

## 🔴 KRITISCH - Hohe Priorität

### 1. Newtonsoft.Json → System.Text.Json Migration
**Aufwand:** 3-4 Stunden | **Impact:** HOCH | **Labels:** `performance`, `modernization`, `breaking-change`

**Erwarteter Gewinn:**
- ✅ Bundle Size: -13 MB
- ✅ Performance: +50-80%
- ✅ AOT-Kompatibilität

**Betroffene Dateien (7):**

#### Backend/Converter/ActionConverter.cs
- [ ] `ActionJsonConverter` für System.Text.Json erstellen
- [ ] `WriteJson()` → `Write(Utf8JsonWriter, WorkflowAction, JsonSerializerOptions)`
- [ ] `ReadJson()` → `Read(ref Utf8JsonReader, Type, JsonSerializerOptions)`
- [ ] Testen mit allen ActionTypes (Command, Announcement, Audio)

**Beispiel-Code:**
```csharp
public class ActionJsonConverter : JsonConverter<WorkflowAction>
{
    public override WorkflowAction Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        var root = jsonDoc.RootElement;
        
        var action = new WorkflowAction
        {
            Id = root.GetProperty("Id").GetGuid(),
            Name = root.GetProperty("Name").GetString() ?? string.Empty,
            // ...
        };
        return action;
    }
    
    public override void Write(Utf8JsonWriter writer, WorkflowAction value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("Id", value.Id);
        writer.WriteString("Name", value.Name);
        // ...
        writer.WriteEndObject();
    }
}
```

#### Backend/Data/DataManager.cs
- [ ] `using Newtonsoft.Json` → `using System.Text.Json`
- [ ] `JsonConvert.SerializeObject()` → `JsonSerializer.Serialize()`
- [ ] `JsonConvert.DeserializeObject()` → `JsonSerializer.Deserialize()`

#### Domain/Solution.cs
- [ ] JSON Attribute migrieren: `[JsonProperty("name")]` → `[JsonPropertyName("name")]`
- [ ] `[JsonIgnore]` bleibt gleich (existiert in beiden Libraries)

#### WinUI/Service/IoService.cs
- [ ] `JsonSerializerSettings` → `JsonSerializerOptions`
- [ ] Shared Options als Konstante:
```csharp
private static readonly JsonSerializerOptions JsonOptions = new()
{
    Converters = { new ActionJsonConverter() },
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};
```
- [ ] `LoadAsync()`, `LoadFromPathAsync()`, `SaveAsync()` aktualisieren

#### WinUI/Service/SettingsService.cs
- [ ] AppSettings Serialization umstellen
- [ ] Error-Handling für ungültige JSON-Dateien

#### WinUI/Service/CityService.cs
- [ ] City Library JSON-Parsing migrieren

#### Testing
- [ ] Unit Tests für `ActionJsonConverter`
- [ ] Integration Tests: Solution Load/Save
- [ ] Workflow Actions serialisieren/deserialisieren testen
- [ ] Performance-Benchmarks (vorher/nachher)

---

### 2. Debug.WriteLine → ILogger Migration
**Aufwand:** 2-3 Stunden | **Impact:** MITTEL | **Labels:** `logging`, `modernization`

**Statistik:** 371 Vorkommen in 51+ Dateien

**Top 10 Dateien:**

| Datei | Anzahl | Priorität |
|-------|--------|-----------|
| `MAUI/Service/SettingsService.cs` | 51 | ⭐⭐⭐ |
| `WinUI/App.xaml.cs` | 51 | ⭐⭐⭐ |
| `SharedUI/ViewModel/MainWindowViewModel.Train.cs` | 28 | ⭐⭐ |
| `SharedUI/ViewModel/MauiViewModel.cs` | 25 | ⭐⭐ |
| `Backend/Manager/JourneyManager.cs` | 18 | ⭐⭐ |
| `SharedUI/ViewModel/WebAppViewModel.cs` | 16 | ⭐ |
| `WinUI/Service/FirewallHelper.cs` | 15 | ⭐ |
| `SharedUI/ViewModel/MainWindowViewModel.Z21.cs` | 12 | ⭐ |
| `Backend/Service/ActionExecutor.cs` | 12 | ⭐ |
| `SharedUI/ViewModel/MainWindowViewModel.Solution.cs` | 11 | ⭐ |

#### MAUI/Service/SettingsService.cs (51×)
- [ ] `ILogger<SettingsService>` via Constructor Injection hinzufügen
- [ ] Systematischer Replace:
  - `Debug.WriteLine($"Text {var}")` → `_logger.LogInformation("Text {Var}", var)`
  - `Debug.WriteLine($"⚠️ Warning")` → `_logger.LogWarning("Warning")`
  - `Debug.WriteLine($"❌ Error")` → `_logger.LogError("Error")`

#### WinUI/App.xaml.cs (51×)
- [ ] `ILogger<App>` hinzufügen
- [ ] Startup-Logging strukturieren
- [ ] DI-Container-Logging verbessern

#### SharedUI/ViewModel/MainWindowViewModel.Train.cs (28×)
- [ ] Photo-Upload-Logging mit strukturierten Properties
- [ ] Error-Logging für SignalR-Fehler

#### Backend/Manager/JourneyManager.cs (18×)
- [ ] Journey-State-Changes loggen
- [ ] Feedback-Events strukturiert loggen

**PowerShell-Script für Batch-Replace:**
```powershell
# Replace-DebugWithLogger.ps1
$files = Get-ChildItem -Path ".\SharedUI\ViewModel" -Filter "*.cs" -Recurse

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    
    # Pattern 1: Debug.WriteLine($"Text {var}")
    $content = $content -replace 'Debug\.WriteLine\(\$"([^"]*)\{([^}]+)\}([^"]*)"\);', 
                                 '_logger.LogInformation("$1{$2}$3", $2);'
    
    # Pattern 2: Debug.WriteLine("Simple text")
    $content = $content -replace 'Debug\.WriteLine\("([^"]*)"\);', 
                                 '_logger.LogInformation("$1");'
    
    Set-Content $file.FullName -Value $content
}
```

---

## 🟡 MITTEL - Mittlere Priorität

### 3. TrackPlan.Import.AnyRail modernisieren
**Aufwand:** 1-2 Stunden | **Impact:** MITTEL | **Labels:** `code-quality`, `modernization`

#### TrackPlan.Import.AnyRail/AnyRail.cs
- [ ] **Obsolete Methode entfernen:**
  ```csharp
  [Obsolete("Use ParseAsync instead")]
  public static AnyRail Parse(string xmlPath) => 
      ParseAsync(xmlPath).GetAwaiter().GetResult(); // ❌ Thread-blocking!
  ```
  → Komplett entfernen, alle Aufrufer auf `ParseAsync` migrieren

- [ ] **Parsing-Validierung:**
  ```csharp
  private static (double X, double Y) ParsePoint(string? s)
  {
      var parts = (s ?? "0,0").Split(',');
      if (parts.Length < 2)
          throw new FormatException($"Invalid point format: '{s}'. Expected 'X,Y'.");
      
      return (double.Parse(parts[0], CultureInfo.InvariantCulture),
              double.Parse(parts[1], CultureInfo.InvariantCulture));
  }
  ```

- [ ] **Magic Numbers → Constants:**
  ```csharp
  private static class AnyRailConstants
  {
      // Tolerances
      public const double CoordinateTolerance = 1.0;
      public const double AngleTolerance = 2.0;
      public const double RadiusTolerance = 20.0;
      
      // Piko Radii (centerline measurements)
      public const double PikoR1Radius = 454.0;
      public const double PikoR2Radius = 515.0;
      public const double PikoR3Radius = 577.0;
      public const double PikoR4Radius = 639.0;
      
      // Special angles
      public const double WeichengegenBogenAngle = 15.0;
  }
  ```

- [ ] **StringBuilder Performance:**
  ```csharp
  public string ToPathData()
  {
      var estimatedSize = (Lines.Count + Arcs.Count) * 50;
      var sb = new StringBuilder(estimatedSize);
      // ...
  }
  ```

- [ ] **LINQ Multi-Enumeration optimieren:**
  ```csharp
  // Vorher:
  var avgX = allPoints.Average(p => p.X);
  var avgY = allPoints.Average(p => p.Y);  // ❌ 2× enumerate
  
  // Nachher:
  var (sumX, sumY, count) = (0.0, 0.0, 0);
  foreach (var p in allPoints)
  {
      sumX += p.X;
      sumY += p.Y;
      count++;
  }
  return count > 0 ? (sumX / count, sumY / count) : (0, 0);
  ```

- [ ] **ILogger statt Debug.WriteLine** (11 Vorkommen)

---

### 4. Magic Strings → Enums
**Aufwand:** 1 Stunde | **Impact:** NIEDRIG-MITTEL | **Labels:** `code-quality`, `type-safety`

#### WinUI/Service/IoService.cs
- [ ] Enum definieren:
  ```csharp
  public enum NewSolutionResult
  {
      Success,
      Cancelled,
      SaveRequested
  }
  ```

- [ ] Interface `IIoService` anpassen:
  ```csharp
  Task<(NewSolutionResult result, string? error)> NewSolutionAsync(bool hasUnsavedChanges);
  ```

- [ ] Magic String `"SAVE_REQUESTED"` ersetzen:
  ```csharp
  if (result == ContentDialogResult.Primary)
      return (NewSolutionResult.SaveRequested, null);
  ```

- [ ] `NullIoService` aktualisieren

---

### 5. TrackPlanEditorViewModel modernisieren
**Aufwand:** 1 Stunde | **Impact:** NIEDRIG | **Labels:** `error-handling`, `code-quality`

#### SharedUI/ViewModel/TrackPlanEditorViewModel.cs
- [ ] **Constructor-Dependencies dokumentieren:**
  ```csharp
  /// <summary>
  /// Initializes a new instance of TrackPlanEditorViewModel.
  /// </summary>
  /// <param name="mainViewModel">Main window view model for global state</param>
  /// <param name="ioService">File I/O service for import/export</param>
  /// <param name="renderer">Topology renderer for visual output</param>
  /// <param name="geometryLibrary">Track geometry definitions</param>
  /// <param name="feedbackStateManager">Feedback point state management</param>
  /// <param name="topologySolver">Topology solver for track connections</param>
  /// <param name="logger">Logger for diagnostics</param>
  public TrackPlanEditorViewModel(
      MainWindowViewModel mainViewModel,
      IIoService ioService,
      // ...
  ```

- [ ] **RenderLayout() Error-Handling:**
  ```csharp
  private void RenderLayout()
  {
      try
      {
          _topologySolver.Solve(Segments, Connections);
          // ...
      }
      catch (Exception ex)
      {
          _logger.LogError(ex, "Failed to render track layout");
          // Optional: Show user-friendly error message
      }
  }
  ```

- [ ] **ImportFromAnyRailXml() Error-Handling verbessern:**
  ```csharp
  [RelayCommand]
  private async Task ImportFromAnyRailXml()
  {
      try
      {
          var file = await _ioService.BrowseForXmlFileAsync();
          if (file == null) return;
          
          var anyRailLayout = await AnyRail.ParseAsync(file);
          // ...
      }
      catch (FormatException ex)
      {
          _logger.LogError(ex, "Invalid AnyRail XML format: {FilePath}", file);
          // Show error dialog
      }
      catch (Exception ex)
      {
          _logger.LogError(ex, "Failed to import AnyRail layout");
          // Show error dialog
      }
  }
  ```

---

## 🟢 NIEDRIG - Nice-to-Have

### 6. Code-Quality Verbesserungen
**Aufwand:** 2-3 Stunden | **Impact:** NIEDRIG | **Labels:** `code-quality`, `c#12`

#### Nullable Reference Types aktivieren
- [ ] In allen `.csproj` Dateien:
  ```xml
  <PropertyGroup>
      <Nullable>enable</Nullable>
  </PropertyGroup>
  ```
- [ ] Warnings beheben (schrittweise pro Projekt)

#### File-Scoped Namespaces (C# 10+)
- [ ] Automatischer Replace:
  ```csharp
  // Vorher:
  namespace Moba.Backend.Service
  {
      public class MyClass { }
  }
  
  // Nachher:
  namespace Moba.Backend.Service;
  
  public class MyClass { }
  ```

#### Primary Constructors (C# 12+)
- [ ] ViewModels mit vielen Dependencies:
  ```csharp
  public partial class MainWindowViewModel(
      IZ21 z21,
      WorkflowService workflowService,
      ILogger<MainWindowViewModel> logger) : ObservableObject
  {
      // Felder werden automatisch generiert
  }
  ```

#### Collection Expressions (C# 12+)
- [ ] Arrays und Lists vereinfachen:
  ```csharp
  // Vorher:
  List<string> items = new List<string> { "item1", "item2" };
  
  // Nachher:
  List<string> items = ["item1", "item2"];
  ```

---

## 📊 Metriken & Testing

### 7. Performance-Benchmarks
**Aufwand:** 2 Stunden | **Labels:** `performance`, `testing`

- [ ] **BenchmarkDotNet** NuGet-Package hinzufügen
- [ ] **JSON Serialization Benchmark:**
  ```csharp
  [Benchmark]
  public void NewtonsoftJson_Serialize() { }
  
  [Benchmark]
  public void SystemTextJson_Serialize() { }
  ```

- [ ] **Startup Time** messen:
  - WinUI App-Start (vor/nach)
  - MAUI App-Start (vor/nach)

- [ ] **Memory Profiling:**
  - ObservableCollection-Änderungen
  - Photo-Upload Memory-Leaks prüfen

---

### 8. Test-Coverage verbessern
**Aufwand:** 3 Stunden | **Labels:** `testing`, `quality`

#### Unit Tests
- [ ] `IoService.SaveAsync()` - Atomic File Writes
- [ ] `ClearAllSelections()` - Property Reset
- [ ] `ActionJsonConverter` - Alle ActionTypes

#### Integration Tests
- [ ] Solution Load/Save mit verschiedenen Projekttypen
- [ ] Photo Upload → SignalR → ViewModel Update
- [ ] AnyRail Import → Topology Solver → Renderer

---

## 📚 Dokumentation

### 9. Dokumentation aktualisieren
**Aufwand:** 1 Stunde | **Labels:** `documentation`

#### README.md
- [ ] "Modernisierungen" Abschnitt hinzufügen:
  ```markdown
  ## Recent Modernizations
  
  - ✅ Migrated from Newtonsoft.Json to System.Text.Json (+50% performance)
  - ✅ Replaced Debug.WriteLine with ILogger (structured logging)
  - ✅ Added atomic file writes to prevent data corruption
  - ✅ Fixed COMException when changing selected project
  ```

#### CHANGELOG.md
- [ ] Erstellen mit Versionierung:
  ```markdown
  # Changelog
  
  ## [Unreleased]
  ### Changed
  - Migrated from Newtonsoft.Json to System.Text.Json
  - Replaced Debug.WriteLine with ILogger
  - Added atomic file writes to IoService
  
  ### Fixed
  - COMException when changing selected project
  - Stale data in property panels after solution operations
  - Photo category validation in SavePhotoAsync
  
  ### Performance
  - JSON serialization ~60% faster
  - Bundle size reduced by 13 MB
  ```

---

## 🎯 Empfohlene Reihenfolge

**Session 1 (3-4h):**
1. ✅ Newtonsoft.Json → System.Text.Json (Backend/Converter/ActionConverter.cs)
2. ✅ IoService JSON-Migration
3. ⚡ Testing der JSON-Migration

**Session 2 (2-3h):**
4. 📝 Debug.WriteLine → ILogger (Top 5 Dateien)
5. 🧹 AnyRail.cs modernisieren
6. ⚡ Testing & Code Review

**Session 3 (1-2h):**
7. 📊 Performance-Benchmarks
8. 📚 Dokumentation
9. ✅ Abschluss & Deployment

---

## 📝 Notizen

- ✅ Build ist erfolgreich nach IoService-Modernisierung
- ✅ Compiler-Warnung (`_uiDispatcher` nicht verwendet) behoben
- ⚠️ 7 Dateien verwenden noch Newtonsoft.Json
- ⚠️ 371 Debug.WriteLine-Aufrufe über die gesamte Solution
- 📊 TrackPlanEditorViewModel aktuell geöffnet

**Wichtige Links:**
- System.Text.Json Migration Guide: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/migrate-from-newtonsoft
- ILogger Best Practices: https://learn.microsoft.com/en-us/dotnet/core/extensions/logging

---

## 🏷️ Labels/Tags

- `modernization` - Code-Modernisierung auf .NET 9/10 Standards
- `performance` - Performance-Optimierungen
- `breaking-change` - API-Änderungen die Breaking Changes sind
- `logging` - Logging-Infrastruktur
- `code-quality` - Code-Qualität & Wartbarkeit
- `testing` - Unit/Integration Tests
- `documentation` - Dokumentation
- `type-safety` - Type Safety (Enums statt Magic Strings)
- `error-handling` - Error Handling & Resilience

---

**Letzte Aktualisierung:** 2025-01-XX  
**Erstellt von:** GitHub Copilot  
**Repository:** https://dev.azure.com/ahuelsmann/MOBAflow/_git/MOBAflow
