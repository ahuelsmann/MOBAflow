# 🔄 Session Continuation - SessionState Pattern Refactoring

**Datum:** 2025-12-05  
**Thread:** 2 (Fortsetzung)  
**Vorheriger Commit:** a2116fe - "WIP: SessionState Pattern Phase 1"

---

## ⚡ SCHNELLSTART für neuen Thread

**Sage einfach:**
> "Bitte lies docs/SESSION-CONTINUATION-2025-12-05.md und setze das Refactoring fort"

---

## 📊 Aktueller Status

### ✅ Was ist bereits erledigt (Commit a2116fe):

1. **SessionState Klassen erstellt:**
   - ✅ `Backend/Services/JourneySessionState.cs` (mit Id, Counter, CurrentPos, CurrentStationName, LastFeedbackTime, IsActive)
   - ✅ `Backend/Services/TrainSessionState.cs` (für später)
   - ✅ `Backend/Manager/StationChangedEventArgs.cs` (Event args mit JourneyId, Station, SessionState)

2. **Domain bereinigt:**
   - ✅ `Domain/Journey.cs` - Entfernt: CurrentPos, CurrentCounter, StateChanged event
   - ✅ `Domain/Journey.cs` - Hinzugefügt: `Guid Id { get; set; }` property

3. **Dokumentation:**
   - ✅ `docs/REFACTORING-SESSIONSTATE-PATTERN.md` - Vollständige Anleitung mit Code-Beispielen

4. **Backup:**
   - ✅ `Backend/Manager/JourneyManager.cs.backup` - Original vor Refactoring

### ⚠️ Was noch NICHT funktioniert:

- ❌ **Build schlägt fehl** (51 Errors)
- ❌ `Backend/Manager/JourneyManager.cs` - Verwendet noch `journey.CurrentCounter/CurrentPos`
- ❌ `SharedUI/ViewModel/JourneyViewModel.cs` - Referenziert noch `Model.CurrentCounter/CurrentPos/StateChanged`
- ❌ DI-Registrierung fehlt für JourneyManager SessionState
- ❌ Tests anpassen

---

## 🎯 NÄCHSTE SCHRITTE (in dieser Reihenfolge!)

### Step 1: Refactor `Backend/Manager/JourneyManager.cs` ⬅️ **START HIER!**

**Datei:** `Backend/Manager/JourneyManager.cs`

**Ziel:** Ersetze alle Domain-Modifikationen durch SessionState-Nutzung.

**Änderungen:**

#### 1.1 Constructor: SessionState initialisieren
```csharp
public JourneyManager(
    IZ21 z21, 
    List<Journey> journeys, 
    WorkflowService workflowService,
    ActionExecutionContext? executionContext = null)
: base(z21, journeys, executionContext)
{
    _workflowService = workflowService;
    
    // ✅ ADD: Initialize SessionState for all journeys
    foreach (var journey in journeys)
    {
        _states[journey.Id] = new JourneySessionState
        {
            JourneyId = journey.Id,
            CurrentPos = (int)journey.FirstPos,
            Counter = 0,
            IsActive = true
        };
    }
}
```

#### 1.2 Methode: `HandleFeedbackAsync` anpassen
```csharp
private async Task HandleFeedbackAsync(Journey journey)
{
    // ✅ ADD: Get state
    var state = _states[journey.Id];
    
    // ❌ REMOVE: journey.CurrentCounter++;
    // ✅ ADD:
    state.Counter++;
    state.LastFeedbackTime = DateTime.Now;
    
    // ❌ REMOVE: journey.CurrentCounter, journey.CurrentPos
    // ✅ USE: state.Counter, state.CurrentPos
    Debug.WriteLine($"🔄 Journey '{journey.Name}': Round {state.Counter}, Position {state.CurrentPos}");
    
    if (state.CurrentPos >= journey.Stations.Count)
    {
        Debug.WriteLine($"⚠️ CurrentPos out of Stations list bounds");
        return;
    }
    
    var currentStation = journey.Stations[state.CurrentPos];
    
    if (state.Counter >= currentStation.NumberOfLapsToStop)
    {
        Debug.WriteLine($"🚉 Station reached: {currentStation.Name}");
        
        // ✅ ADD: Update SessionState
        state.CurrentStationName = currentStation.Name;
        
        // ✅ ADD: Fire StationChanged event
        StationChanged?.Invoke(this, new StationChangedEventArgs
        {
            JourneyId = journey.Id,
            Station = currentStation,
            SessionState = state
        });
        
        // Execute workflow (existing code)
        if (currentStation.Flow != null)
        {
            ExecutionContext.JourneyTemplateText = journey.Text;
            ExecutionContext.CurrentStation = currentStation;
            await _workflowService.ExecuteAsync(currentStation.Flow, ExecutionContext);
            ExecutionContext.JourneyTemplateText = null;
            ExecutionContext.CurrentStation = null;
        }
        
        state.Counter = 0;
        bool isLastStation = state.CurrentPos == journey.Stations.Count - 1;
        
        if (isLastStation)
        {
            await HandleLastStationAsync(journey);
        }
        else
        {
            state.CurrentPos++;
        }
    }
}
```

#### 1.3 Methode: `HandleLastStationAsync` anpassen
```csharp
private async Task HandleLastStationAsync(Journey journey)
{
    // ✅ ADD: Get state
    var state = _states[journey.Id];
    
    Debug.WriteLine($"🏁 Last station of journey '{journey.Name}' reached");
    
    switch (journey.BehaviorOnLastStop)
    {
        case BehaviorOnLastStop.BeginAgainFromFistStop:
            Debug.WriteLine("🔄 Journey will restart from beginning");
            state.CurrentPos = 0; // ✅ Use state instead of journey.CurrentPos
            break;
            
        case BehaviorOnLastStop.GotoJourney:
            if (journey.NextJourney != null)
            {
                // ✅ Get next journey's state
                var nextState = _states[journey.NextJourney.Id];
                Debug.WriteLine($"➡️ Switching to journey: {journey.NextJourney.Name}");
                nextState.CurrentPos = (int)journey.NextJourney.FirstPos;
                Debug.WriteLine($"✅ Journey '{journey.NextJourney.Name}' activated at position {nextState.CurrentPos}");
            }
            else
            {
                Debug.WriteLine($"⚠️ NextJourney not set");
            }
            break;
            
        case BehaviorOnLastStop.None:
            Debug.WriteLine("ℹ️ Journey stops");
            state.IsActive = false; // ✅ ADD
            break;
    }
    
    await Task.CompletedTask;
}
```

#### 1.4 Methode: `Reset` anpassen
```csharp
// ❌ REMOVE: public static void Reset(Journey journey)
// ✅ ADD: public void Reset(Journey journey) - make non-static
public void Reset(Journey journey)
{
    if (_states.TryGetValue(journey.Id, out var state))
    {
        state.Counter = 0;
        state.CurrentPos = (int)journey.FirstPos;
        state.CurrentStationName = string.Empty;
        state.LastFeedbackTime = null;
        state.IsActive = true;
        Debug.WriteLine($"🔄 Journey '{journey.Name}' reset");
    }
}
```

#### 1.5 Methode: `GetState` hinzufügen
```csharp
/// <summary>
/// Gets the runtime state for a journey.
/// Returns null if journey is not registered.
/// </summary>
public JourneySessionState? GetState(Guid journeyId)
{
    return _states.GetValueOrDefault(journeyId);
}
```

**Wichtig:** Nach diesen Änderungen sollte `JourneyManager.cs` kompilieren! ✅

---

### Step 2: Build prüfen

```bash
dotnet build Backend/Backend.csproj
```

**Erwartete Errors nach Step 1:**
- JourneyViewModel referenziert noch `Model.CurrentCounter/CurrentPos` ❌
- Aber JourneyManager sollte kompilieren! ✅

---

### Step 3: Refactor `SharedUI/ViewModel/JourneyViewModel.cs`

**Datei:** `SharedUI/ViewModel/JourneyViewModel.cs`

**Änderungen:**

#### 3.1 Constructor: JourneySessionState + JourneyManager hinzufügen
```csharp
public partial class JourneyViewModel : ObservableObject, IEntityViewModel
{
    private readonly Journey _journey; // Rename Model → _journey
    private readonly JourneySessionState _state; // ADD
    private readonly JourneyManager _journeyManager; // ADD
    private readonly IUiDispatcher _dispatcher;
    
    public JourneyViewModel(
        Journey journey, 
        JourneySessionState state, // ADD
        JourneyManager journeyManager, // ADD
        IUiDispatcher dispatcher)
    {
        _journey = journey;
        _state = state; // ADD
        _journeyManager = journeyManager; // ADD
        _dispatcher = dispatcher;
        
        // ❌ REMOVE: Model.StateChanged subscription
        // ✅ ADD: Subscribe to JourneyManager event
        _journeyManager.StationChanged += OnStationChanged;
        
        // Stations ViewModel collection (existing code)
        Stations = new ObservableCollection<StationViewModel>(
            journey.Stations.Select(s => new StationViewModel(s, _dispatcher))
        );
    }
    
    // ✅ ADD: Event handler
    private void OnStationChanged(object? sender, StationChangedEventArgs e)
    {
        if (e.JourneyId != _journey.Id) return; // Only react to THIS journey
        
        _dispatcher.InvokeOnUi(() =>
        {
            OnPropertyChanged(nameof(CurrentStation));
            OnPropertyChanged(nameof(CurrentCounter));
            OnPropertyChanged(nameof(CurrentPos));
        });
    }
}
```

#### 3.2 Properties: Delegieren zu Domain oder SessionState
```csharp
// Domain properties (delegate to _journey)
public string Name
{
    get => _journey.Name;
    set => SetProperty(_journey.Name, value, _journey, (m, v) => m.Name = v);
}

public string Description
{
    get => _journey.Description;
    set => SetProperty(_journey.Description, value, _journey, (m, v) => m.Description = v);
}

// ❌ REMOVE old properties:
// public uint CurrentCounter { get => Model.CurrentCounter; set => ... }
// public uint CurrentPos { get => Model.CurrentPos; set => ... }

// ✅ ADD: SessionState properties (read-only from ViewModel perspective)
public string CurrentStation => _state.CurrentStationName;
public int CurrentCounter => _state.Counter;
public int CurrentPos => _state.CurrentPos;
```

#### 3.3 Property: Model → expose _journey (für Serialization)
```csharp
// Expose domain object for serialization
public Journey Model => _journey;
```

**Nach diesen Änderungen:** JourneyViewModel kompiliert! ✅

---

### Step 4: DI Registrierung

**Datei:** `WinUI/App.xaml.cs`

**Problem:** JourneyViewModel braucht jetzt `JourneySessionState` + `JourneyManager`

**Lösung:** Factory Pattern

```csharp
// ConfigureServices()
services.AddSingleton<Backend.Manager.JourneyManager>(sp =>
{
    var z21 = sp.GetRequiredService<Backend.Interface.IZ21>();
    var solution = sp.GetRequiredService<Domain.Solution>();
    var workflowService = sp.GetRequiredService<Backend.Services.WorkflowService>();
    
    // Get all journeys from all projects
    var journeys = solution.Projects.SelectMany(p => p.Journeys).ToList();
    
    return new Backend.Manager.JourneyManager(z21, journeys, workflowService);
});

// JourneyViewModel wird NICHT mehr im DI registriert!
// Stattdessen: Factory-Methode in MainWindowViewModel oder wo benötigt
```

**MainWindowViewModel:** Factory für JourneyViewModel

```csharp
private JourneyViewModel CreateJourneyViewModel(Journey journey)
{
    var state = _journeyManager.GetState(journey.Id) 
                ?? throw new InvalidOperationException($"Journey {journey.Id} not registered");
    
    return new JourneyViewModel(journey, state, _journeyManager, _dispatcher);
}
```

---

### Step 5: Tests anpassen

**Datei:** `Test/SharedUI/JourneyViewModelTests.cs` (falls existiert)

**Änderungen:**
- Mock `JourneyManager`
- Erstelle `JourneySessionState` für Tests
- Passe Constructor-Calls an

---

### Step 6: Build & Test

```bash
dotnet build
dotnet test
```

**Erwartung:** ✅ Alles kompiliert, Tests laufen!

---

## 🔍 Häufige Probleme & Lösungen

### Problem 1: "Journey does not contain Id"
**Lösung:** Domain/Journey.cs muss `public Guid Id { get; set; }` haben (bereits committed ✅)

### Problem 2: "JourneySessionState does not contain CurrentPos"
**Lösung:** JourneySessionState.cs muss `public int CurrentPos { get; set; }` haben (bereits committed ✅)

### Problem 3: Unicode-Emojis in Fehlermeldungen
**Lösung:** Ignorieren - sind nur Debug.WriteLine Ausgaben, kein Compile-Problem

### Problem 4: "SharedUI.Interface namespace not found"
**Lösung:** Separates Problem - wird in anderem Thread behoben. Für jetzt: Ignorieren oder Interface-Dateien prüfen.

---

## 📚 Referenz-Dateien

| Datei | Zweck |
|-------|-------|
| `docs/REFACTORING-SESSIONSTATE-PATTERN.md` | Vollständige Anleitung mit Architektur-Diagrammen |
| `Backend/Manager/JourneyManager.cs.backup` | Original-Zustand vor Refactoring |
| `Backend/Services/JourneySessionState.cs` | SessionState Klasse (FERTIG ✅) |
| `Backend/Manager/StationChangedEventArgs.cs` | Event Args (FERTIG ✅) |

---

## ✅ Definition of Done

Die Refactoring ist abgeschlossen wenn:

1. ✅ `dotnet build` erfolgreich (0 Errors)
2. ✅ `dotnet test` erfolgreich (alle Tests grün)
3. ✅ Keine `journey.CurrentCounter` / `journey.CurrentPos` Referenzen mehr im Code
4. ✅ JourneyViewModel nutzt SessionState statt Domain-Properties
5. ✅ DI korrekt registriert (JourneyManager als Singleton)
6. ✅ `.github/copilot-instructions.md` aktualisiert mit SessionState Pattern

---

## 🚀 Nach Abschluss

1. **Commit:**
   ```bash
   git add .
   git commit -m "Refactor: Complete SessionState Pattern - JourneyManager + JourneyViewModel"
   git push
   ```

2. **Instructions aktualisieren:**
   - Ergänze `.github/copilot-instructions.md` mit SessionState Pattern
   - Füge Beispiel-Code hinzu
   - Dokumentiere DI-Registrierung

3. **Cleanup:**
   - Lösche `Backend/Manager/JourneyManager.cs.backup`
   - Archive `docs/SESSION-CONTINUATION-2025-12-05.md` nach `docs/archive/`

---

**Viel Erfolg! 🎯**

Wenn du im neuen Thread anfängst, sage einfach:
> "Bitte lies docs/SESSION-CONTINUATION-2025-12-05.md und setze das Refactoring fort"
