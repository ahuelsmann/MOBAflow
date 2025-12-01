# Clean Architecture - Quick Start für Morgen

**Ziel:** Die restlichen 30% des Refactorings abschließen (4-6h)

---

## 🚀 Start: JourneyManager fixen (2h)

### Problem
`HandleLastStationAsync` hat Logic-Fehler wegen neuer Domain-Models.

### Fix

Öffne: `Backend/Manager/JourneyManager.cs`

**Ersetze Zeilen ~105-140:**

```csharp
private async Task HandleLastStationAsync(Journey journey)
{
    Debug.WriteLine($"🏁 Last station of journey '{journey.Name}' reached");

    switch (journey.BehaviorOnLastStop)
    {
        case BehaviorOnLastStop.BeginAgainFromFirstStop:
            Debug.WriteLine("🔄 Journey will restart from beginning");
            journey.CurrentPos = journey.FirstPos;
            journey.CurrentCounter = 0;
            break;

        case BehaviorOnLastStop.GotoJourney:
            if (journey.NextJourney != null)
            {
                Debug.WriteLine($"➡ Switching to journey: {journey.NextJourney.Name}");
                journey.NextJourney.CurrentPos = journey.NextJourney.FirstPos;
                journey.NextJourney.CurrentCounter = 0;
                Debug.WriteLine($"✅ Journey '{journey.NextJourney.Name}' activated at position {journey.NextJourney.FirstPos}");
            }
            else
            {
                Debug.WriteLine($"⚠ NextJourney is null - no journey to switch to");
            }
            break;

        case BehaviorOnLastStop.None:
            Debug.WriteLine("⏹ Journey stops - no further action");
            break;

        default:
            Debug.WriteLine($"⚠ Unknown BehaviorOnLastStop: {journey.BehaviorOnLastStop}");
            break;
    }

    await Task.CompletedTask;
}
```

### Test
```sh
dotnet build Backend/Backend.csproj
```

---

## 📝 ActionConverter neu schreiben (1h)

### Problem
Converter nutzt noch alte `Backend.Model.Action.Base` statt `Domain.WorkflowAction`.

### Lösung

Öffne: `Backend/Converter/ActionConverter.cs`

**Ersetze komplett:**

```csharp
// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Converter;

using Moba.Domain;
using Moba.Domain.Enum;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// JSON Converter for WorkflowAction (Domain POCO).
/// Handles polymorphic serialization via Type + Parameters.
/// </summary>
public class ActionConverter : JsonConverter<WorkflowAction>
{
    public override WorkflowAction? ReadJson(
        JsonReader reader, 
        Type objectType, 
        WorkflowAction? existingValue, 
        bool hasExistingValue, 
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        JObject jo = JObject.Load(reader);

        var action = new WorkflowAction
        {
            Id = jo["Id"]?.ToObject<Guid>() ?? Guid.NewGuid(),
            Name = jo["Name"]?.ToString() ?? "Unnamed Action",
            Number = jo["Number"]?.ToObject<int>() ?? 0,
            Type = jo["Type"]?.ToObject<ActionType>() ?? ActionType.Command
        };

        // Parse type-specific parameters
        action.Parameters = new Dictionary<string, object>();

        switch (action.Type)
        {
            case ActionType.Command:
                var bytes = jo["Bytes"]?.ToObject<byte[]>();
                if (bytes != null)
                    action.Parameters["Bytes"] = bytes;
                break;

            case ActionType.Audio:
                var filePath = jo["FilePath"]?.ToString();
                if (!string.IsNullOrEmpty(filePath))
                    action.Parameters["FilePath"] = filePath;
                break;

            case ActionType.Announcement:
                var message = jo["Message"]?.ToString() ?? "";
                var voiceName = jo["VoiceName"]?.ToString();
                
                action.Parameters["Message"] = message;
                if (!string.IsNullOrEmpty(voiceName))
                    action.Parameters["VoiceName"] = voiceName;
                break;
        }

        return action;
    }

    public override void WriteJson(
        JsonWriter writer, 
        WorkflowAction? value, 
        JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartObject();

        // Write common properties
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

### Test
```sh
dotnet build Backend/Backend.csproj
```

---

## 🔄 WorkflowConverter anpassen (30min)

### Problem
Converter muss WorkflowId Properties in Station/Platform setzen.

### Lösung

Öffne: `Backend/Converter/WorkflowConverter.cs`

**Ändere ReadJson:**

```csharp
public override Workflow? ReadJson(...)
{
    if (reader.TokenType == JsonToken.Null)
        return null;

    if (reader.TokenType == JsonToken.String)
    {
        string? workflowIdString = reader.Value?.ToString();

        if (!string.IsNullOrEmpty(workflowIdString) && Guid.TryParse(workflowIdString, out Guid workflowId))
        {
            // Create temporary workflow with only ID (will be resolved later)
            return new Workflow { Id = workflowId };
        }
    }

    return null;
}
```

**Nach Solution Load:**

In `Backend/Model/Solution.cs` (oder wo LoadAsync ist):

```csharp
var solution = JsonConvert.DeserializeObject<Solution>(json, settings);

if (solution != null)
{
    // ✅ Resolve workflow references using SolutionService
    var solutionService = new SolutionService();
    solutionService.ResolveWorkflowReferences(solution);
}
```

### Test
```sh
dotnet build Backend/Backend.csproj
```

---

## 🧹 Backend.Model/ Legacy aufräumen (30min)

### Option 1: Löschen (Clean Break)
```sh
Remove-Item Backend/Model/Workflow.cs
Remove-Item Backend/Model/Solution.cs
Remove-Item Backend/Model/Journey.cs
Remove-Item Backend/Model/Station.cs
Remove-Item Backend/Model/Platform.cs
Remove-Item Backend/Model/Action -Recurse
```

### Option 2: [Obsolete] markieren
```csharp
// Backend/Model/Workflow.cs
[Obsolete("Use Moba.Domain.Workflow instead. This class will be removed in v2.0")]
public class Workflow
{
    // ...
}
```

### Empfehlung
Löschen nach erfolgreichem Build.

---

## 🎨 SharedUI ViewModels aktualisieren (1h)

### Automatisiert

```powershell
# Alle ViewModels Namespaces anpassen
Get-ChildItem -Path "SharedUI\ViewModel" -Recurse -Filter "*.cs" | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    $content = $content -replace 'using Moba\.Backend\.Model;', 'using Moba.Domain;'
    $content = $content -replace 'using Moba\.Backend\.Model\.Enum;', 'using Moba.Domain.Enum;'
    $content = $content -replace 'using Moba\.Backend\.Model\.Action;', 'using Moba.Domain;'
    Set-Content $_.FullName -Value $content -NoNewline
}
Write-Output "SharedUI ViewModels aktualisiert"
```

### Test
```sh
dotnet build SharedUI/SharedUI.csproj
```

---

## 💉 DI-Container Services registrieren (30min)

### WinUI

Öffne: `WinUI/App.xaml.cs`

**In ConfigureServices() hinzufügen:**

```csharp
// ✅ Clean Architecture Services
services.AddSingleton<ActionExecutor>();
services.AddSingleton<WorkflowService>();
services.AddSingleton<SolutionService>();

// JourneyManagerFactory braucht jetzt WorkflowService
// (bereits registriert, aber stell sicher dass es NACH WorkflowService kommt)
services.AddSingleton<Backend.Interface.IJourneyManagerFactory, Backend.Manager.JourneyManagerFactory>();
```

### MAUI

Öffne: `MAUI/MauiProgram.cs`

**Hinzufügen:**

```csharp
// Clean Architecture Services
builder.Services.AddSingleton<Backend.Services.ActionExecutor>();
builder.Services.AddSingleton<Backend.Services.WorkflowService>();
builder.Services.AddSingleton<Backend.Services.SolutionService>();
builder.Services.AddSingleton<Backend.Interface.IJourneyManagerFactory, Backend.Manager.JourneyManagerFactory>();
```

### Blazor

Öffne: `WebApp/Program.cs`

**Hinzufügen:**

```csharp
// Clean Architecture Services
builder.Services.AddSingleton<Moba.Backend.Services.ActionExecutor>();
builder.Services.AddSingleton<Moba.Backend.Services.WorkflowService>();
builder.Services.AddSingleton<Moba.Backend.Services.SolutionService>();
builder.Services.AddSingleton<Moba.Backend.Interface.IJourneyManagerFactory, Moba.Backend.Manager.JourneyManagerFactory>();
```

### Test
```sh
dotnet build WinUI/WinUI.csproj
dotnet build MAUI/MAUI.csproj
dotnet build WebApp/WebApp.csproj
```

---

## ✅ Full Solution Build

```sh
dotnet build
```

**Erwartung:** 0 Fehler, 0 Warnungen

---

## 🧪 Runtime-Tests

### 1. WinUI starten
```sh
dotnet run --project WinUI/WinUI.csproj
```

### 2. Workflow testen
1. Solution laden
2. Workflow erstellen
3. Workflow ausführen (via Feedback oder manuell)
4. Debugger: Breakpoint in `ActionExecutor.ExecuteAsync`

### 3. JSON-Serialisierung testen
1. Solution speichern
2. Solution neu laden
3. Prüfen: Workflows korrekt geladen?

---

## 📦 Git Commit

```sh
git add .
git status

git commit -m "feat: Clean Architecture Refactoring

✅ Domain project with 20+ POCOs
✅ Backend/Services (WorkflowService, ActionExecutor, SolutionService)
✅ Manager classes use Services instead of Model business logic
✅ JSON converters for WorkflowAction
✅ ViewModels use Domain namespace
✅ DI registration for new services

BREAKING CHANGE: Backend.Model.Action.* replaced by Domain.WorkflowAction + Services
"

git push origin main
```

---

## 🎓 Was du gelernt hast

1. **Clean Architecture** - Domain ist innerste Schicht
2. **Services Pattern** - Business Logic von Models trennen
3. **POCO Models** - Testbar, wiederverwendbar
4. **Dependency Inversion** - Services injizieren

---

## 📚 Nächste Schritte (optional)

1. **Unit Tests** für Services schreiben
2. **Integration Tests** für Manager-Klassen
3. **Performance-Tests** (Workflow-Execution)
4. **Documentation** aktualisieren

---

**Geschätzter Zeitaufwand: 4-6 Stunden**

**Viel Erfolg morgen! 🚀**
