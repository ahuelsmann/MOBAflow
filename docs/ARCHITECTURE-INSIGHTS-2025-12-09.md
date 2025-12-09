## 🎯 MOBAflow Architecture Insights (2025-12-09)

### **Journey Execution Flow (Feedback-Driven)**

```
1. Z21 sends Feedback (InPort=5) → UDP arrives
2. JourneyManager checks: "Is there a Journey listening on InPort=5?"
3. If YES:
   - Counter++ (lap counter)
   - Check: Counter == Station.NumberOfLapsToStop?
   - If YES: 
     → CurrentPos++ (next station in array)
     → CurrentStationName = Stations[CurrentPos].Name
     → Execute Station.Workflow
       → Workflow.Actions execute (TTS, Turnout, Signal)
```

**Key Insight:** Journey "listens" on ONE InPort. Stations don't have individual feedback sensors for journey tracking!

---

### **JourneySessionState (Runtime Data)**

```csharp
public class JourneySessionState {
    public string CurrentStationName { get; set; }  // "Bielefeld Hbf"
    public int Counter { get; set; }                // Lap counter (0, 1, 2, ...)
    public int CurrentPos { get; set; }             // Index in Stations array
}
```

**Separation:**
- **Domain:** Static configuration (Stations list, NumberOfLapsToStop)
- **SessionState:** Runtime tracking (Counter, CurrentPos, CurrentStationName)

---

### **Station.NumberOfLapsToStop Pattern**

```csharp
// Example: Station requires 3 laps before stopping
Station.NumberOfLapsToStop = 3;

// Journey execution:
Lap 1: Counter=1 → Skip (1 < 3)
Lap 2: Counter=2 → Skip (2 < 3)
Lap 3: Counter=3 → STOP! (3 == 3) → Execute Workflow
```

**Use Case:** Circle layouts where train passes same sensor multiple times.

---

### **ViewModel Architecture (Wrapper Pattern)**

#### **Rule: 1:1 Property Mapping**

```csharp
// Domain.Station (POCO)
public class Station {
    public uint InPort { get; set; }
    public bool IsExitOnLeft { get; set; }
    public DateTime? Arrival { get; set; }
}

// StationViewModel (Wrapper) - ✅ MUST have ALL Domain properties!
public class StationViewModel {
    // ✅ Same name, same property (possibly different type for UI)
    public int InPort { 
        get => (int)_station.InPort; 
        set => SetProperty(...);
    }
    
    public bool IsExitOnLeft {
        get => _station.IsExitOnLeft;
        set => SetProperty(...);
    }
    
    public DateTime? Arrival {
        get => _station.Arrival;
        set => SetProperty(...);
    }
    
    // ✅ PLUS Runtime-only properties
    public int Position { get; set; }        // UI: "3rd station"
    public bool IsCurrentStation { get; set; }  // UI: "Train is HERE"
}
```

**Display Names:** Use `[Display(Name = "...")]` for UI-friendly names, NOT different property names!

---

### **Nested Stations Architecture (Permanent)**

```json
{
  "Journeys": [{
    "Name": "RE 78",
    "InPort": 5,
    "Stations": [  // ✅ Nested (not StationIds!)
      {
        "Id": "...",
        "Name": "Bielefeld",
        "InPort": 1,
        "IsExitOnLeft": true,  // ← Journey-specific!
        "NumberOfLapsToStop": 2
      }
    ]
  }]
}
```

**Reason:** Same physical station can have **different configuration** in different journeys:
- Journey A: "Exit left" + "Stop after 1 lap"
- Journey B: "Exit right" + "Stop after 3 laps"

---

### **Manager Architecture (Independent Feedback Processing)**

```
Z21 Feedback (InPort=5)
        ↓
┌───────┼────────┬────────────┐
│       │        │            │
Journey Station  Workflow  (Future)
Manager Manager  Manager
```

**Key Points:**
- All managers process **ALL** feedbacks independently
- JourneyManager: "Is this InPort relevant for MY journey?"
- Each manager has **independent logic**
- No inter-manager communication (yet)

---

### **Re-selection Pattern (UI Requirement)**

**Problem:**
```
1. Select Journey A → Properties shows Journey A ✅
2. Select Station B → Properties shows Station B ✅
3. Click Journey A AGAIN → Properties STILL shows Station B ❌
```

**Cause:** `SelectedJourney` doesn't change (already Journey A), so `OnPropertyChanged` doesn't fire!

**Solution:** Force PropertyChanged even when same object:
```csharp
partial void OnSelectedJourneyChanged(JourneyViewModel? value)
{
    CurrentSelectedEntityType = MobaType.Journey;
    OnPropertyChanged(nameof(CurrentSelectedObject));  // ✅ Always!
    RefreshCurrentSelectionCommand.Execute(null);      // ✅ Force refresh!
}
```

---

### **Workflow Execution Example**

**Scenario:** Train reaches "Bielefeld Hbf"

```
1. Feedback InPort=5 arrives
2. JourneyManager: Counter++ → Counter == NumberOfLapsToStop (3 == 3)
3. CurrentPos++ → CurrentPos = 1
4. CurrentStationName = "Bielefeld Hbf"
5. Station.WorkflowId → Resolve Workflow
6. Workflow.Actions execute:
   - Action 1 (TTS): "Nächster Halt Bielefeld Hauptbahnhof. Ausstieg in Fahrtrichtung rechts."
   - Action 2 (Turnout): Set turnout to position 2
   - Action 3 (Signal): Set signal to green
```

---

### **Properties Editability**

**Rule:** ALL properties should be **editable** (when possible)

**Exceptions (Read-Only):**
- `Position` (calculated from array index)
- `IsCurrentStation` (runtime state from JourneyManager)
- `CurrentStationName` (runtime state)
- `Counter`, `CurrentPos` (runtime state)

**Editable:**
- All Domain properties (Name, Description, InPort, NumberOfLapsToStop, etc.)

---

### **Future Features (Ignore for Now)**

- `Arrival`/`Departure` (DateTime) → For delay announcements (v2.0)
- `WorkflowManager` → Independent workflow triggers (v2.0)
- `StationManager` → Platform-specific tracking (v2.0)

---

**Last Updated:** 2025-12-09  
**Based on:** Q&A Session with Andreas
