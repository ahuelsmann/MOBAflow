# 🎯 Refactoring: SessionState Pattern Implementation

**Status:** 🟡 In Progress  
**Started:** 2025-12-05  
**Thread:** Session 1  
**Goal:** Separate Domain (user data) from Runtime State (session data)

---

## 📋 Overview

### Problem
Domain classes (Journey, Station, Train) contain runtime state:
- `Journey.CurrentPos` / `Journey.CurrentCounter` → Runtime state, should NOT be saved
- Mixing user project data with execution state
- Violates Clean Architecture (Domain should be pure POCOs)

### Solution
**SessionState Pattern:**
1. **Domain:** Pure POCOs (only user project data)
2. **Backend/Services:** SessionState classes (runtime state)
3. **Backend/Manager:** Managers own SessionState instances
4. **SharedUI/ViewModel:** Wraps Domain + SessionState reference

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    WinUI / Blazor (View)                     │
└──────────────────────────┬──────────────────────────────────┘
                           │ binds to
┌──────────────────────────▼──────────────────────────────────┐
│              SharedUI/ViewModel/JourneyViewModel             │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ - _journey: Journey         (Domain reference)         │ │
│  │ - _state: JourneySessionState (SessionState reference) │ │
│  │                                                          │ │
│  │ Properties:                                              │ │
│  │ • Name => _journey.Name                                  │ │
│  │ • CurrentStation => _state.CurrentStationName            │ │
│  │ • Counter => _state.Counter                              │ │
│  └────────────────────────────────────────────────────────┘ │
└──────────────────────────┬──────────────────────────────────┘
                           │ subscribes to events
┌──────────────────────────▼──────────────────────────────────┐
│              Backend/Manager/JourneyManager                  │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ - _states: Dictionary<Guid, JourneySessionState>       │ │
│  │ - _inPortToJourneys: Dictionary<uint, List<Journey>>   │ │
│  │                                                          │ │
│  │ Event: StationChanged(JourneyId, Station, SessionState)│ │
│  │                                                          │ │
│  │ Methods:                                                 │ │
│  │ • RegisterJourney(Journey)                              │ │
│  │ • GetState(Guid journeyId)                              │ │
│  │ • ProcessFeedback(uint inPort)                          │ │
│  └────────────────────────────────────────────────────────┘ │
└──────────────────────────┬──────────────────────────────────┘
                           │ manages
┌──────────────────────────▼──────────────────────────────────┐
│            Backend/Services/JourneySessionState              │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ + JourneyId: Guid                                        │ │
│  │ + CurrentStationName: string                             │ │
│  │ + Counter: int                                           │ │
│  │ + LastFeedbackTime: DateTime?                            │ │
│  │ + IsActive: bool                                         │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

---

## 📁 File Changes

### New Files
| File | Purpose |
|------|---------|
| `Backend/Services/JourneySessionState.cs` | Runtime state for Journey execution |
| `Backend/Services/TrainSessionState.cs` | Runtime state for Train execution |
| `Backend/Manager/JourneyManager.cs` | Manages Journey SessionStates + Events |
| `Backend/Manager/StationChangedEventArgs.cs` | Event args for station changes |
| `Test/Fakes/FakeJourneyManager.cs` | Test fake for JourneyManager |

### Modified Files
| File | Changes |
|------|---------|
| `Domain/Journey.cs` | ❌ Remove: CurrentPos, CurrentCounter, StateChanged |
| `Backend/Manager/StationManager.cs` | ✅ Add: JourneyManager integration |
| `SharedUI/ViewModel/JourneyViewModel.cs` | ✅ Add: SessionState wrapping |
| `WinUI/App.xaml.cs` | ✅ Add: DI registrations |
| `WebApp/Program.cs` | ✅ Add: DI registrations (scoped) |
| `.github/copilot-instructions.md` | ✅ Add: SessionState Pattern documentation |

---

## 🔧 Implementation Steps

### ✅ Step 1: Create JourneySessionState
**File:** `Backend/Services/JourneySessionState.cs`

```csharp
namespace Moba.Backend.Services;

public class JourneySessionState
{
    public Guid JourneyId { get; set; }
    public string CurrentStationName { get; set; } = string.Empty;
    public int Counter { get; set; }
    public DateTime? LastFeedbackTime { get; set; }
    public bool IsActive { get; set; }
}
```

**Why:**
- Pure data class (no INotifyPropertyChanged - that's for ViewModels)
- Lives in Backend (not Domain, not SharedUI)
- Managed by JourneyManager

---

### ✅ Step 2: Create TrainSessionState
**File:** `Backend/Services/TrainSessionState.cs`

```csharp
namespace Moba.Backend.Services;

public class TrainSessionState
{
    public Guid TrainId { get; set; }
    public int CurrentSpeed { get; set; }
    public bool IsRunning { get; set; }
    public DateTime? LastCommandTime { get; set; }
}
```

---

### ✅ Step 3: Create JourneyManager
**File:** `Backend/Manager/JourneyManager.cs`

```csharp
namespace Moba.Backend.Manager;

public class JourneyManager
{
    private readonly Dictionary<Guid, JourneySessionState> _states = [];
    private readonly Dictionary<uint, List<Journey>> _inPortToJourneys = [];
    
    public event EventHandler<StationChangedEventArgs>? StationChanged;
    
    public void RegisterJourney(Journey journey)
    {
        // Create SessionState
        _states[journey.Id] = new JourneySessionState
        {
            JourneyId = journey.Id,
            IsActive = true
        };
        
        // Map InPort → Journey
        if (!_inPortToJourneys.ContainsKey(journey.InPort))
            _inPortToJourneys[journey.InPort] = [];
        
        _inPortToJourneys[journey.InPort].Add(journey);
    }
    
    public JourneySessionState? GetState(Guid journeyId)
    {
        return _states.GetValueOrDefault(journeyId);
    }
    
    public void ProcessFeedback(uint inPort, Station station)
    {
        if (!_inPortToJourneys.TryGetValue(inPort, out var journeys))
            return;
        
        foreach (var journey in journeys)
        {
            var state = _states[journey.Id];
            state.Counter++;
            state.CurrentStationName = station.Name;
            state.LastFeedbackTime = DateTime.Now;
            
            StationChanged?.Invoke(this, new StationChangedEventArgs
            {
                JourneyId = journey.Id,
                Station = station,
                SessionState = state
            });
        }
    }
}
```

---

### ✅ Step 4: Create StationChangedEventArgs
**File:** `Backend/Manager/StationChangedEventArgs.cs`

```csharp
namespace Moba.Backend.Manager;

public class StationChangedEventArgs : EventArgs
{
    public Guid JourneyId { get; init; }
    public Station Station { get; init; } = null!;
    public JourneySessionState SessionState { get; init; } = null!;
}
```

---

### ✅ Step 5: Clean Domain/Journey.cs
**Remove:**
```csharp
// ❌ DELETE THESE:
private uint _currentPos;
private uint _currentCounter;
public uint CurrentPos { get; set; }
public uint CurrentCounter { get; set; }
public event Action? StateChanged;
```

**Keep:**
```csharp
// ✅ KEEP THESE (User project data):
public string Name { get; set; }
public List<Station> Stations { get; set; }
public uint InPort { get; set; }
public BehaviorOnLastStop BehaviorOnLastStop { get; set; }
public Journey? NextJourney { get; set; }
public uint FirstPos { get; set; }
```

---

### ✅ Step 6: Update StationManager
**Add dependency:**
```csharp
private readonly JourneyManager _journeyManager;

public StationManager(Z21 z21, JourneyManager journeyManager, ...)
{
    _journeyManager = journeyManager;
}
```

**Fire events:**
```csharp
protected override async Task ProcessFeedbackAsync(FeedbackResult feedback)
{
    // ... existing logic ...
    
    // Fire JourneyManager event
    _journeyManager.ProcessFeedback(feedback.InPort, station);
}
```

---

### ✅ Step 7: Update JourneyViewModel
**Add SessionState reference:**
```csharp
public partial class JourneyViewModel : ObservableObject
{
    private readonly Journey _journey;
    private readonly JourneySessionState _state;
    private readonly JourneyManager _journeyManager;
    
    public JourneyViewModel(Journey journey, JourneySessionState state, JourneyManager manager)
    {
        _journey = journey;
        _state = state;
        _journeyManager = manager;
        
        // Subscribe to events
        _journeyManager.StationChanged += OnStationChanged;
    }
    
    // Domain properties
    public string Name => _journey.Name;
    
    // SessionState properties
    public string CurrentStation => _state.CurrentStationName;
    public int Counter => _state.Counter;
    
    private void OnStationChanged(object? sender, StationChangedEventArgs e)
    {
        if (e.JourneyId != _journey.Id) return;
        
        OnPropertyChanged(nameof(CurrentStation));
        OnPropertyChanged(nameof(Counter));
    }
}
```

---

### ✅ Step 8: DI Registration (WinUI)
**File:** `WinUI/App.xaml.cs`

```csharp
// Backend Services
services.AddSingleton<Backend.Manager.JourneyManager>();

// Update StationManager (add JourneyManager dependency)
services.AddSingleton<Backend.Manager.StationManager>(sp =>
{
    var z21 = sp.GetRequiredService<Backend.Interface.IZ21>();
    var journeyManager = sp.GetRequiredService<Backend.Manager.JourneyManager>();
    var stations = /* get stations */;
    return new Backend.Manager.StationManager(z21, stations, journeyManager);
});

// ViewModels (Transient with factory)
services.AddTransient<SharedUI.ViewModel.JourneyViewModel>(sp =>
{
    var journey = /* from context */;
    var journeyManager = sp.GetRequiredService<Backend.Manager.JourneyManager>();
    var state = journeyManager.GetState(journey.Id) 
                ?? throw new InvalidOperationException("Journey not registered");
    return new SharedUI.ViewModel.JourneyViewModel(journey, state, journeyManager);
});
```

---

### ✅ Step 9: DI Registration (Blazor)
**File:** `WebApp/Program.cs`

```csharp
// Backend Services (SCOPED for Blazor!)
builder.Services.AddScoped<Backend.Manager.JourneyManager>();

// StationManager
builder.Services.AddScoped<Backend.Manager.StationManager>();

// ViewModels
builder.Services.AddTransient<SharedUI.ViewModel.JourneyViewModel>();
```

---

### ✅ Step 10-14: Testing & Documentation
- Update all `new JourneyViewModel(...)` calls
- Create `FakeJourneyManager` for tests
- Run tests + fix failures
- Update `copilot-instructions.md`

---

## 🔑 Key Principles

| Principle | Rule |
|-----------|------|
| **Domain** | Pure POCOs - NO runtime state, NO events, NO INotifyPropertyChanged |
| **SessionState** | Pure data classes in Backend/Services - NO UI dependencies |
| **Managers** | Backend/Manager owns SessionState instances - fires events |
| **ViewModels** | Wrap Domain + SessionState reference - subscribe to Manager events |
| **DI** | Singleton Managers (WinUI), Scoped Managers (Blazor) |

---

## 🚨 Common Pitfalls

### ❌ DON'T: Put SessionState in Domain
```csharp
// ❌ WRONG
public class Journey
{
    public int Counter { get; set; } // Runtime state in Domain!
}
```

### ✅ DO: Keep Domain Pure
```csharp
// ✅ CORRECT
public class Journey
{
    public string Name { get; set; } // Only user data
}
```

### ❌ DON'T: Duplicate State in ViewModel
```csharp
// ❌ WRONG
public class JourneyViewModel
{
    [ObservableProperty]
    private int counter; // Duplicates SessionState!
}
```

### ✅ DO: Reference SessionState
```csharp
// ✅ CORRECT
public class JourneyViewModel
{
    private readonly JourneySessionState _state;
    public int Counter => _state.Counter; // Delegates to SessionState
}
```

---

## 📝 Session Checklist

- [ ] Step 1: Create JourneySessionState
- [ ] Step 2: Create TrainSessionState
- [ ] Step 3: Create JourneyManager
- [ ] Step 4: Create StationChangedEventArgs
- [ ] Step 5: Clean Domain/Journey.cs
- [ ] Step 6: Update StationManager
- [ ] Step 7: Update JourneyViewModel
- [ ] Step 8: DI registration WinUI
- [ ] Step 9: DI registration Blazor
- [ ] Step 10: Update ViewModel constructor calls
- [ ] Step 11: Update tests
- [ ] Step 12: Build solution
- [ ] Step 13: Run tests
- [ ] Step 14: Update copilot-instructions.md
- [ ] Commit changes
- [ ] Push to Azure DevOps

---

## 🔄 For Next Session

If this session ends before completion:

1. **Check progress:** Read this file + plan file in `.temp/`
2. **Find last completed step:** Check checkboxes above
3. **Resume from next step:** Follow implementation details
4. **DI is critical:** Don't forget registrations in App.xaml.cs + Program.cs
5. **Test thoroughly:** Run all tests before committing

---

**Last Updated:** 2025-12-05  
**Next Review:** After Step 14 completion
