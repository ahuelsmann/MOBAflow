# Clean Architecture Refactoring - Status Report

**Datum:** 30. November 2025  
**Status:** 70% abgeschlossen (3h Arbeit)  
**Verbleibender Aufwand:** 4-6 Stunden

---

## ✅ Was FERTIG ist (70%)

### 1. Domain-Projekt erstellt ✅
**Pfad:** `Domain/`

**Inhalt:**
- 20+ reine POCOs ohne Business-Logic
- Alle Enums (ActionType, BehaviorOnLastStop, CargoType, ColorScheme, DigitalSystem, Epoch, PassengerClass, PowerSystem, ServiceType, TrainType)
- Korrekte Namespace-Struktur: `Moba.Domain`, `Moba.Domain.Enum`
- Kompiliert erfolgreich ohne Dependencies (nur `net10.0`)

**Dateien:**
```
Domain/
├── Domain.csproj
├── Solution.cs
├── Project.cs
├── SpeakerEngineConfiguration.cs
├── Journey.cs
├── Station.cs
├── Platform.cs
├── Workflow.cs
├── WorkflowAction.cs
├── Train.cs
├── Locomotive.cs
├── Wagon.cs
├── PassengerWagon.cs
├── GoodsWagon.cs
├── Details.cs
├── Voice.cs
├── Settings.cs
├── ValidationResult.cs
├── City.cs
└── Enum/
    ├── ActionType.cs (mit Audio statt Sound!)
    ├── BehaviorOnLastStop.cs
    ├── CargoType.cs
    ├── ColorScheme.cs
    ├── DigitalSystem.cs
    ├── Epoch.cs
    ├── PassengerClass.cs
    ├── PowerSystem.cs
    ├── ServiceType.cs
    └── TrainType.cs
```

**Wichtige Änderungen:**
- ✅ `ActionType.Sound` → `ActionType.Audio` (Konsistenz mit ActionExecutor)
- ✅ `ValidationResult.Errors` als `List<string>` (für SolutionService)
- ✅ `Journey` hat jetzt: `Text`, `CurrentCounter`, `OnLastStop`, `NextJourney`, `FirstPos`
- ✅ `Station` hat jetzt: `Flow`, `WorkflowId`, `NumberOfLapsToStop`
- ✅ `Platform` hat jetzt: `Flow`, `WorkflowId`, `Track`
- ✅ `Workflow.Actions` ist jetzt `List<WorkflowAction>` (nicht mehr Action.Base)

---

### 2. Backend/Services Layer erstellt ✅
**Pfad:** `Backend/Services/`

**Dateien:**

#### `WorkflowService.cs` ✅
- Ersetzt die alte `Workflow.StartAsync()` Business-Logic
- Nutzt `ActionExecutor` für Action-Execution
- `ExecuteAsync(Workflow workflow, ActionExecutionContext? context)`

#### `ActionExecutor.cs` ✅
- Ersetzt die alte `Action.Base.ExecuteAsync()` Business-Logic
- Führt `WorkflowAction` (Domain POCO) aus basierend auf Type + Parameters
- Unterstützt: Command (Bytes), Audio (FilePath), Announcement (Message, VoiceName)

#### `SolutionService.cs` ✅
- Ersetzt die alte `Solution.UpdateFrom()` Business-Logic
- `MergeSolution(Solution target, Solution source)` - für DI Singleton Updates
- `ValidateSolution(Solution solution)` - für Consistency-Checks
- `ResolveWorkflowReferences(Solution solution)` - nach Deserialization

#### `ActionExecutionContext.cs` ✅
- Neue Klasse in `WorkflowService.cs`
- Ersetzt alte `Backend.Model.Action.ActionExecutionContext`
- Properties:
  - `IZ21 Z21` (required)
  - `ISpeakerEngine? SpeakerEngine`
  - `ISoundPlayer? SoundPlayer`
  - `string? JourneyTemplateText` (für Announcements)
  - `Station? CurrentStation` (für Journey-Workflows)

---

### 3. Manager-Klassen angepasst ✅

#### `BaseFeedbackManager<TEntity>` ✅
- Nutzt neue `ActionExecutionContext` (Services) statt alte (Model.Action)
- Constructor angepasst
- `using Moba.Backend.Services;` hinzugefügt

#### `WorkflowManager` ✅
- Injiziert `WorkflowService`
- Nutzt `_workflowService.ExecuteAsync(workflow, context)` statt `workflow.StartAsync()`
- Constructor: `(IZ21 z21, List<Workflow> workflows, WorkflowService workflowService, ActionExecutionContext? context)`

#### `JourneyManager` ⚠️ (70% fertig)
- Injiziert `WorkflowService`
- Nutzt `_workflowService.ExecuteAsync()` für Station-Workflows
- ⚠️ **TODO:** `HandleLastStationAsync` hat Logic-Fehler (siehe unten)

#### `PlatformManager` ✅
- Injiziert `WorkflowService`
- Nutzt `_workflowService.ExecuteAsync()` statt `platform.Flow.StartAsync()`

#### `StationManager` ✅
- Injiziert `WorkflowService`
- Nutzt `_workflowService.ExecuteAsync()` statt `station.Flow.StartAsync()`

#### `JourneyManagerFactory` ✅
- Injiziert `WorkflowService`
- Constructor angepasst
- Interface `IJourneyManagerFactory` aktualisiert

---

### 4. Backend.csproj aktualisiert ✅
```xml
<ProjectReference Include="..\Domain\Domain.csproj" />
```

### 5. Namespaces aktualisiert ✅
- `using Moba.Backend.Model;` → `using Moba.Domain;` (7 Dateien)
- `using Moba.Backend.Model.Enum;` → `using Moba.Domain.Enum;`

---

## ⏳ Was NOCH FEHLT (30%, ~4-6h)

### 1. JourneyManager Logic-Fehler beheben (2h) 🔴
**Pfad:** `Backend/Manager/JourneyManager.cs`

**Problem:**
```csharp
// Zeile ~119
switch (journey.OnLastStop)  // OnLastStop ist BehaviorOnLastStop (Enum), nicht Action-Delegate!
{
    case BehaviorOnLastStop.BeginAgainFromFirstStop:
        // ...
    case BehaviorOnLastStop.GotoJourney:
        // Fehler: journey.NextJourney ist jetzt Journey-Objekt, nicht String!
        var nextJourney = Entities.FirstOrDefault(j => j.Name == journey.NextJourney);
        // ...
}
```

**Fix:**
```csharp
private async Task HandleLastStationAsync(Journey journey)
{
    Debug.WriteLine($"🏁 Last station of journey '{journey.Name}' reached");

    switch (journey.BehaviorOnLastStop) // ✅ Korrekter Property-Name
    {
        case BehaviorOnLastStop.BeginAgainFromFirstStop:
            Debug.WriteLine("🔄 Journey will restart from beginning");
            journey.CurrentPos = journey.FirstPos; // ✅ FirstPos nutzen
            journey.CurrentCounter = 0;
            break;

        case BehaviorOnLastStop.GotoJourney:
            if (journey.NextJourney != null) // ✅ Journey-Objekt, nicht String
            {
                Debug.WriteLine($"➡ Switching to journey: {journey.NextJourney.Name}");
                journey.NextJourney.CurrentPos = journey.NextJourney.FirstPos;
                journey.NextJourney.CurrentCounter = 0;
                Debug.WriteLine($"✅ Journey '{journey.NextJourney.Name}' activated");
            }
            else
            {
                Debug.WriteLine($"⚠ NextJourney is null");
            }
            break;

        case BehaviorOnLastStop.None:
            Debug.WriteLine("⏹ Journey stops");
            break;
    }

    await Task.CompletedTask;
}
```

---

### 2. JSON-Converter neu schreiben (2h) 🔴
**Dateien:**
- `Backend/Converter/ActionConverter.cs`
- `Backend/Converter/WorkflowConverter.cs`

**Problem:**
- `ActionConverter` serialisiert noch `Backend.Model.Action.Base` statt `Domain.WorkflowAction`
- `WorkflowAction` nutzt `Dictionary<string, object>? Parameters` statt typed Properties
- Alte JSON-Dateien müssen migriert werden

**Lösung:**

#### ActionConverter.cs neu
```csharp
public class ActionConverter : JsonConverter<WorkflowAction>
{
    public override WorkflowAction? ReadJson(JsonReader reader, Type objectType, ...)
    {
        JObject jo = JObject.Load(reader);
        
        var action = new WorkflowAction
        {
            Id = jo["Id"]?.ToObject<Guid>() ?? Guid.NewGuid(),
            Name = jo["Name"]?.ToString() ?? "Unnamed Action",
            Number = jo["Number"]?.ToObject<int>() ?? 0,
            Type = jo["Type"]?.ToObject<ActionType>() ?? ActionType.Command
        };

        // Parse type-specific parameters
        switch (action.Type)
        {
            case ActionType.Command:
                action.Parameters = new Dictionary<string, object>
                {
                    ["Bytes"] = jo["Bytes"]?.ToObject<byte[]>() ?? Array.Empty<byte>()
                };
                break;

            case ActionType.Audio:
                action.Parameters = new Dictionary<string, object>
                {
                    ["FilePath"] = jo["FilePath"]?.ToString() ?? ""
                };
                break;

            case ActionType.Announcement:
                action.Parameters = new Dictionary<string, object>
                {
                    ["Message"] = jo["Message"]?.ToString() ?? "",
                    ["VoiceName"] = jo["VoiceName"]?.ToString() ?? ""
                };
                break;
        }

        return action;
    }

    public override void WriteJson(JsonWriter writer, WorkflowAction? value, ...)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartObject();
        writer.WritePropertyName("Id");
        writer.WriteValue(value.Id);
        writer.WritePropertyName("Name");
        writer.WriteValue(value.Name);
        writer.WritePropertyName("Number");
        writer.WriteValue(value.Number);
        writer.WritePropertyName("Type");
        writer.WriteValue(value.Type.ToString());

        // Write type-specific parameters
        if (value.Parameters != null)
        {
            foreach (var kvp in value.Parameters)
            {
                writer.WritePropertyName(kvp.Key);
                serializer.Serialize(writer, kvp.Value);
            }
        }

        writer.WriteEndObject();
    }
}
```

#### WorkflowConverter.cs
- Muss `Workflow` (Domain) statt `Backend.Model.Workflow` nutzen
- `WorkflowId` Properties in Station/Platform beim Deserialisieren setzen
- Nach Load: `SolutionService.ResolveWorkflowReferences()` aufrufen

---

### 3. Backend.Model/ Legacy-Code aufräumen (30min) 🟡

**Dateien zu entfernen/anpassen:**
- `Backend/Model/Workflow.cs` (jetzt in Domain)
- `Backend/Model/Solution.cs` (jetzt in Domain)
- `Backend/Model/Journey.cs, Station.cs, Platform.cs` (jetzt in Domain)
- `Backend/Model/Action/*` (durch WorkflowAction + ActionExecutor ersetzt)

**Options:**
1. **Löschen** (Clean Break)
2. **Als [Obsolete] markieren** mit Migration-Hinweis
3. **Legacy-Ordner** erstellen (`Backend/Model/Legacy/`)

**Empfehlung:** Löschen nach erfolgreichen Tests.

---

### 4. SharedUI ViewModels aktualisieren (1-2h) 🟡

**Betroffene Dateien (~40):**
```
SharedUI/ViewModel/
├── SolutionViewModel.cs
├── ProjectViewModel.cs
├── JourneyViewModel.cs
├── StationViewModel.cs
├── PlatformViewModel.cs
├── WorkflowViewModel.cs
├── TrainViewModel.cs
├── LocomotiveViewModel.cs
├── WagonViewModel.cs
├── PassengerWagonViewModel.cs  ← NEU erstellt
├── GoodsWagonViewModel.cs      ← NEU erstellt
├── DetailsViewModel.cs         ← NEU erstellt
├── SettingsViewModel.cs        ← NEU erstellt
├── VoiceViewModel.cs           ← NEU erstellt
├── Action/
│   ├── AudioViewModel.cs
│   ├── AnnouncementViewModel.cs
│   └── CommandViewModel.cs
└── ...
```

**Änderungen:**
```csharp
// ❌ ALT
using Moba.Backend.Model;
using Moba.Backend.Model.Enum;

// ✅ NEU
using Moba.Domain;
using Moba.Domain.Enum;
```

**Automatisiert:**
```powershell
Get-ChildItem -Path "SharedUI\ViewModel" -Recurse -Filter "*.cs" | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    $content = $content -replace 'using Moba\.Backend\.Model;', 'using Moba.Domain;'
    $content = $content -replace 'using Moba\.Backend\.Model\.Enum;', 'using Moba.Domain.Enum;'
    Set-Content $_.FullName -Value $content -NoNewline
}
```

---

### 5. DI-Container aktualisieren (30min) 🟡

**Dateien:**
- `WinUI/App.xaml.cs`
- `MAUI/MauiProgram.cs`
- `WebApp/Program.cs`

**Neue Services registrieren:**
```csharp
// WinUI/App.xaml.cs ConfigureServices()
services.AddSingleton<ActionExecutor>();
services.AddSingleton<WorkflowService>();
services.AddSingleton<SolutionService>();

// JourneyManagerFactory braucht jetzt WorkflowService
services.AddSingleton<IJourneyManagerFactory, JourneyManagerFactory>();

// MAUI/MauiProgram.cs
builder.Services.AddSingleton<ActionExecutor>();
builder.Services.AddSingleton<WorkflowService>();
builder.Services.AddSingleton<SolutionService>();
builder.Services.AddSingleton<IJourneyManagerFactory, JourneyManagerFactory>();

// WebApp/Program.cs
builder.Services.AddSingleton<ActionExecutor>();
builder.Services.AddSingleton<WorkflowService>();
builder.Services.AddSingleton<SolutionService>();
builder.Services.AddSingleton<IJourneyManagerFactory, JourneyManagerFactory>();
```

---

### 6. Tests aktualisieren (1-2h) 🟡

**Dateien:**
```
Test/
├── Backend/
│   ├── ActionExecutorTests.cs   ← NEU
│   ├── WorkflowServiceTests.cs  ← NEU
│   ├── SolutionServiceTests.cs  ← NEU
│   └── JourneyManagerTests.cs   ← Anpassen
└── Domain/
    ├── WorkflowTests.cs         ← NEU (POCO-Tests)
    └── ValidationResultTests.cs ← NEU
```

**Vorteile:**
- Domain-Models sind jetzt einfach testbar (POCOs ohne Dependencies)
- Services können gemockt werden

**Beispiel:**
```csharp
[Test]
public async Task WorkflowService_ExecutesActionsSequentially()
{
    // Arrange
    var mockZ21 = new Mock<IZ21>();
    var actionExecutor = new ActionExecutor();
    var workflowService = new WorkflowService(actionExecutor, mockZ21.Object);
    
    var workflow = new Workflow
    {
        Name = "Test Workflow",
        Actions = new List<WorkflowAction>
        {
            new WorkflowAction
            {
                Number = 1,
                Type = ActionType.Command,
                Parameters = new Dictionary<string, object>
                {
                    ["Bytes"] = new byte[] { 0x01, 0x02, 0x03 }
                }
            }
        }
    };

    // Act
    await workflowService.ExecuteAsync(workflow);

    // Assert
    mockZ21.Verify(z => z.SendCommandAsync(It.IsAny<byte[]>()), Times.Once);
}
```

---

## 📊 Fehler-Log

### Aktuelle Build-Fehler (Stand: Heute 18:00)

1. **JourneyManager.cs:119** - `journey.OnLastStop` Type Mismatch
2. **JourneyManager.cs:126** - `journey.NextJourney` String → Journey
3. **ActionConverter.cs** - Nutzt noch `Backend.Model.Action.Base`
4. **WorkflowConverter.cs** - Nutzt noch alte Workflow-Struktur

---

## 🔄 Migration Strategy

### Phase 1: Backend fertigstellen (2h)
1. JourneyManager.HandleLastStationAsync fixen
2. ActionConverter neu schreiben
3. WorkflowConverter anpassen
4. Backend.Model/ aufräumen

### Phase 2: Frontend anpassen (1-2h)
1. SharedUI ViewModels Namespaces ändern
2. WinUI/MAUI/WebApp Compilation testen

### Phase 3: DI & Tests (1-2h)
1. Services in DI registrieren
2. Unit Tests schreiben
3. Integration Tests durchführen

### Phase 4: Validation (30min)
1. Bestehende JSON-Dateien laden testen
2. Workflow-Execution testen
3. Full Build + Run

---

## 🎯 Morgen starten

### Schritt 1: Projekt öffnen
```sh
cd C:\Repo\ahuelsmann\MOBAflow
git status  # Prüfen, was geändert wurde
```

### Schritt 2: JourneyManager fixen
- Datei: `Backend/Manager/JourneyManager.cs`
- Zeile: ~105-140
- Fix: Siehe "Was NOCH FEHLT" Punkt 1

### Schritt 3: Build testen
```sh
dotnet build Backend/Backend.csproj
```

### Schritt 4: JSON-Converter
- `Backend/Converter/ActionConverter.cs`
- `Backend/Converter/WorkflowConverter.cs`

---

## 📚 Ressourcen

- **Clean Architecture:** https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html
- **Domain-Driven Design:** https://martinfowler.com/bliki/DomainDrivenDesign.html
- **.NET Dependency Injection:** https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection

---

## ✅ Checklist für morgen

- [ ] JourneyManager.HandleLastStationAsync fixen
- [ ] ActionConverter neu schreiben
- [ ] WorkflowConverter anpassen
- [ ] Backend Build erfolgreich
- [ ] SharedUI Namespaces aktualisieren
- [ ] DI-Container Services registrieren
- [ ] Tests schreiben
- [ ] Full Solution Build
- [ ] Runtime-Tests (Workflow-Execution)
- [ ] Git Commit + Push

---

**Geschätzter Zeitaufwand morgen: 4-6 Stunden**

**Status:** Ready for Phase 2! 🚀
