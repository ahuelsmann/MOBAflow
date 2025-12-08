# Refactoring Plan: Reference-Based Domain Architecture

**Status**: IN PROGRESS (72% complete)  
**Started**: 2025-12-08  
**Last Updated**: 2025-12-08 06:30

---

## 🎯 Goal

Refactor MOBAflow domain model from **nested object trees** to **flat aggregates with GUID references**.

### Why?
- ❌ **Before**: Circular references (Journey.NextJourney), redundant storage, complex JSON serialization
- ✅ **After**: Single source of truth in Project, simple GUID references, standard JSON serialization

---

## ✅ Completed (Steps 1-8)

### 1️⃣ Domain Layer - GUID Identity
- [x] `Station.Id` (Guid) added
- [x] `Locomotive.Id` (Guid) added
- [x] `Wagon.Id` (Guid) added (base class, inherited by PassengerWagon/GoodsWagon)
- [x] `Train.Id` (Guid) added

### 2️⃣ Domain Layer - Reference Properties
- [x] `Journey.Stations` → `Journey.StationIds` (List<Guid>)
- [x] `Journey.NextJourney` → `Journey.NextJourneyId` (Guid?)
- [x] `Train.Locomotives` → `Train.LocomotiveIds` (List<Guid>)
- [x] `Train.Wagons` → `Train.WagonIds` (List<Guid>)
- [x] `Station.Flow` removed (only `WorkflowId` remains)

### 3️⃣ Project Aggregate Root
- [x] `Project.Stations` list added (Single Source of Truth)

**Result**: 
```csharp
public class Project
{
    public List<Locomotives> Locomotives { get; set; }      // ✅ Master list
    public List<Wagon> PassengerWagons { get; set; }        // ✅ Master list
    public List<Wagon> GoodsWagons { get; set; }            // ✅ Master list
    public List<Station> Stations { get; set; }             // ✅ Master list (NEW!)
    public List<Workflow> Workflows { get; set; }           // ✅ Master list
    public List<Journey> Journeys { get; set; }             // ✅ Contains StationIds
    public List<Train> Trains { get; set; }                 // ✅ Contains LocomotiveIds/WagonIds
}
```

### 4️⃣ Backend Layer
- [x] **StationConverter.cs** deleted (no longer needed!)
- [x] **IoService.cs** updated (removed StationConverter references, 4 locations)
- [x] **SolutionService.RestoreWorkflowReferences()** deleted (obsolete)
- [x] **JourneyManager** refactored:
  - Constructor now takes `Project` instead of `List<Journey>`
  - Resolves references via `_project.Stations`, `_project.Workflows`, `_project.Journeys`
  - `HandleFeedbackAsync()` uses `journey.StationIds[pos]` → lookup in `_project.Stations`
  - `HandleLastStationAsync()` uses `journey.NextJourneyId` → lookup in `_project.Journeys`

---

## ⏸️ Remaining Work (Steps 9-11)

### 9️⃣ ViewModels - Reference Resolution (64+ errors)

#### A. TrainViewModel.cs (16 errors)
**Current problem**: Uses `Model.Locomotives` and `Model.Wagons` (no longer exist)

**Solution**:
```csharp
public class TrainViewModel
{
    private readonly Train _train;
    private readonly Project _project;  // ✅ NEW: Need Project reference
    
    // Resolved at runtime
    public ObservableCollection<LocomotiveViewModel> Locomotives =>
        _train.LocomotiveIds
            .Select(id => _project.Locomotives.FirstOrDefault(l => l.Id == id))
            .Where(l => l != null)
            .Select(l => new LocomotiveViewModel(l))
            .ToObservableCollection();
    
    public void AddLocomotive(Locomotive loco)
    {
        _train.LocomotiveIds.Add(loco.Id);  // ✅ Add ID only
        _project.Locomotives.Add(loco);     // ✅ Add to master list
        OnPropertyChanged(nameof(Locomotives));
    }
}
```

**Files to update**:
- `SharedUI/ViewModel/TrainViewModel.cs` (constructor, Locomotives/Wagons properties, Add/Remove methods)
- `SharedUI/ViewModel/MainWindowViewModel.Train.cs` (CreateTrainViewModel factory)

---

#### B. JourneyViewModel.cs (8 errors)
**Current problem**: Uses `Model.Stations` (no longer exists)

**Solution**:
```csharp
public class JourneyViewModel
{
    private readonly Journey _journey;
    private readonly Project _project;  // ✅ NEW
    
    public ObservableCollection<StationViewModel> Stations =>
        _journey.StationIds
            .Select(id => _project.Stations.FirstOrDefault(s => s.Id == id))
            .Where(s => s != null)
            .Select(s => new StationViewModel(s, _project))
            .ToObservableCollection();
    
    public void AddStation(Station station)
    {
        _journey.StationIds.Add(station.Id);  // ✅ Add ID
        _project.Stations.Add(station);       // ✅ Add to master list
        OnPropertyChanged(nameof(Stations));
    }
}
```

**Files to update**:
- `SharedUI/ViewModel/JourneyViewModel.cs`
- `SharedUI/ViewModel/MainWindowViewModel.Journey.cs` (CreateJourneyViewModel factory)

---

#### C. StationViewModel.cs (3 errors)
**Current problem**: Uses `Model.Flow` (no longer exists)

**Solution**:
```csharp
public class StationViewModel
{
    private readonly Station _station;
    private readonly Project _project;  // ✅ NEW
    
    public Workflow? Flow
    {
        get => _station.WorkflowId.HasValue
            ? _project.Workflows.FirstOrDefault(w => w.Id == _station.WorkflowId.Value)
            : null;
        set
        {
            _station.WorkflowId = value?.Id;
            OnPropertyChanged();
        }
    }
}
```

**Files to update**:
- `SharedUI/ViewModel/StationViewModel.cs`

---

#### D. MainWindowViewModel (6 errors)
**Files to update**:
- `SharedUI/ViewModel/MainWindowViewModel.cs` (AddStation command)
- `SharedUI/ViewModel/MainWindowViewModel.Journey.cs` (Stations management)

---

#### E. ValidationService.cs (6 errors)
**Current problem**: Checks `journey.NextJourney`, `journey.Stations`, `train.Locomotives`, `train.Wagons`, `station.Flow`

**Solution**: Update validation logic to use IDs + lookups:
```csharp
// Before:
var referencingJourney = journeys.FirstOrDefault(j => j.NextJourney == journey);

// After:
var referencingJourney = journeys.FirstOrDefault(j => j.NextJourneyId == journey.Id);
```

**Files to update**:
- `SharedUI/Service/ValidationService.cs`

---

#### F. EditorPage.xaml.cs (14 errors)
**Current problem**: Drag & Drop code uses `Model.Locomotives.Add(loco)`

**Solution**: Update to use IDs + master lists:
```csharp
// Before:
ViewModel.SelectedTrain.Model.Locomotives.Add(locomotiveCopy);

// After:
ViewModel.SelectedTrain.Model.LocomotiveIds.Add(locomotiveCopy.Id);
ViewModel.CurrentProjectViewModel.Model.Locomotives.Add(locomotiveCopy);
ViewModel.SelectedTrain.RefreshLocomotives();
```

**Files to update**:
- `WinUI/View/EditorPage.xaml.cs` (Drag & Drop handlers)

---

### 🔟 Tests Update (11+ errors)

#### Test Files to Update:
1. **Test/SharedUI/JourneyViewModelTests.cs**
   - Change `Stations = new List<Station>` → `StationIds = new List<Guid>`

2. **Test/SharedUI/ValidationServiceTests.cs**
   - Update all test cases using `NextJourney`, `Stations`, `Flow`, `Locomotives`, `Wagons`

3. **Test/Backend/JourneyManagerTests.cs**
   - Update constructor calls (now needs `Project` instead of `List<Journey>`)
   - Update test setup to use `StationIds`

---

### 1️⃣1️⃣ Build & Verify
- [ ] Run `run_build` and fix any remaining errors
- [ ] Run all tests (`dotnet test`)
- [ ] Manual smoke test in WinUI app

---

## 🛠️ Implementation Pattern

### For each ViewModel:
1. **Add Project reference** to constructor
2. **Replace object collections** with computed properties using LINQ + lookup
3. **Update Add/Remove methods** to modify IDs + master lists
4. **Update factory methods** in MainWindowViewModel

### Example Template:
```csharp
public class SomeViewModel
{
    private readonly SomeEntity _entity;
    private readonly Project _project;
    
    public SomeViewModel(SomeEntity entity, Project project, IUiDispatcher dispatcher)
    {
        _entity = entity;
        _project = project;
        _dispatcher = dispatcher;
    }
    
    // Resolved collection
    public ObservableCollection<ChildViewModel> Children =>
        _entity.ChildIds
            .Select(id => _project.ChildList.FirstOrDefault(c => c.Id == id))
            .Where(c => c != null)
            .Select(c => new ChildViewModel(c, _project))
            .ToObservableCollection();
}
```

---

## 📚 Key Principles

1. **Single Source of Truth**: All entities live in `Project.XXX` lists
2. **References via GUID**: Domain objects only store IDs, never nested objects
3. **Resolve at Runtime**: ViewModels resolve references via LINQ lookups
4. **Standard JSON**: No custom converters needed (GUIDs serialize natively)
5. **No Circular References**: GUIDs prevent circular serialization issues

---

## 🚦 Next Session Checklist

1. Start with **TrainViewModel** (biggest block, 16 errors)
2. Then **JourneyViewModel** (8 errors)
3. Then **StationViewModel** (3 errors)
4. Then **ValidationService** (6 errors)
5. Then **EditorPage** code-behind (14 errors)
6. Finally **Tests** (11+ errors)
7. Build & verify

---

## 📝 Notes

- **JourneyManager** already updated (Step 8) - serves as reference implementation
- **City Library** (germany-stations.json) remains unchanged (master data)
- **Existing .mobaflow files** will still load (JSON deserializer handles missing properties gracefully)
- Consider adding **migration logic** later if old file format support is critical

---

**Estimated Time Remaining**: 3-4 hours  
**Difficulty**: Medium (repetitive pattern, well-defined steps)

---

## 🔗 Related Files
- Domain models: `Domain/*.cs`
- ViewModels: `SharedUI/ViewModel/*.cs`
- Managers: `Backend/Manager/*.cs`
- Tests: `Test/**/*.cs`
