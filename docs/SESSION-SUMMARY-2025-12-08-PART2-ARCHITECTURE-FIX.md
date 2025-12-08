# Architecture Fix Summary (Dec 8, 2025 - Part 2)

## ✅ **Korrekte Architektur jetzt implementiert:**

### **1. City (Library Pattern)**
```csharp
public class City : Station {
    public List<Station> Stations { get; set; }  // ✅ Library helper
}
```
- **Warum:** City ist Auswahlhilfe, NICHT Teil von Project aggregate
- **Usage:** Drag&Drop aus CityLibrary → erstellt JourneyStation

### **2. Project (Aggregate Root)**
```csharp
public class Project {
    // ❌ REMOVED: public List<Station> Stations  
    // ✅ Stations gehören NUR zu Journey!
    
    public List<Platform> Platforms { get; set; }  // ✅ Master list for Platforms
    public List<City> Cities { get; set; }         // ✅ City library
}
```

### **3. JourneyStation (NEW - Junction Entity)**
```csharp
public class JourneyStation {
    public Guid StationId { get; set; }              // Reference to Station
    public bool IsExitOnLeft { get; set; }           // ✅ Journey-specific!
    public uint NumberOfLapsToStop { get; set; }     // ✅ Journey-specific!
    public Guid? WorkflowId { get; set; }            // ✅ Journey-specific!
}
```
- **Warum:** Journey1 kann "Hauptbahnhof" mit IsExitOnLeft=true haben
- **Warum:** Journey2 kann gleiche Station mit IsExitOnLeft=false haben

### **4. Journey (Aggregate mit JourneyStations)**
```csharp
public class Journey {
    // ❌ REMOVED: public List<Guid> StationIds
    // ✅ NEW:
    public List<JourneyStation> JourneyStations { get; set; }
}
```

### **5. Station (Pure Entity - kein Journey-Kontext mehr)**
```csharp
public class Station {
    public Guid Id { get; set; }
    public string Name { get; set; }
    public List<Guid> PlatformIds { get; set; }  // ✅ Reference to Platforms
    public uint InPort { get; set; }
    
    // ❌ REMOVED: NumberOfLapsToStop (→ JourneyStation)
    // ❌ REMOVED: WorkflowId (→ JourneyStation)
    // ❌ REMOVED: IsExitOnLeft (→ JourneyStation)
}
```

---

## 🚧 **Bekannte Build-Errors: ~60**

### **Kategorien:**

1. **StationViewModel** (15 errors)
   - `Model.NumberOfLapsToStop` → Muss von JourneyStation kommen
   - `Model.WorkflowId` → Muss von JourneyStation kommen
   - `Model.Platforms` → Muss aus PlatformIds resolved werden

2. **JourneyViewModel** (10 errors)
   - `_journey.StationIds` → Muss zu `_journey.JourneyStations` werden
   - `RefreshStations()` → Muss JourneyStations iterieren

3. **MainWindowViewModel.Journey.cs** (8 errors)
   - `Model.StationIds` → Muss zu `Model.JourneyStations` werden
   - `CurrentProjectViewModel.Model.Stations` → Existiert nicht mehr

4. **JourneyManager** (4 errors)
   - `journey.StationIds` → Muss zu `journey.JourneyStations` werden
   - `_project.Stations` → Existiert nicht mehr

5. **StationManager** (6 errors)
   - `station.WorkflowId` → Muss aus Journey-Kontext kommen

6. **Tests** (10+ errors)
   - Station initialization mit NumberOfLapsToStop
   - Project.Stations usage
   - Journey.StationIds usage

7. **EditorPage.xaml.cs** (2 errors)
   - Commands noch nicht generiert (Rebuild SharedUI nötig)

---

## 🎯 **Nächste Session: Roadmap**

### **Phase 1: Fix ViewModels (30 min)**
1. **JourneyViewModel.cs**
   - `RefreshStations()`: `_journey.JourneyStations` statt `StationIds`
   - `AddStation()`: Create JourneyStation
   - `DeleteStation()`: Remove from JourneyStations

2. **StationViewModel.cs**
   - Constructor braucht `JourneyStation` zusätzlich zu `Station`
   - `NumberOfLapsToStop`, `WorkflowId`, `IsExitOnLeft` → von JourneyStation

### **Phase 2: Fix Backend (15 min)**
3. **JourneyManager.cs**
   - `journey.JourneyStations[pos].StationId` statt `journey.StationIds[pos]`
   - Station lookup aus City-Library oder separater Station-Liste

4. **StationManager.cs**
   - WorkflowId aus Journey-Kontext (nicht aus Station selbst)

### **Phase 3: Fix Tests (15 min)**
5. Update alle Test-Fixtures

### **Phase 4: Rebuild (5 min)**
6. `dotnet build` → Green! ✅

---

## 📖 **Architektur-Entscheidungen (Dokumentation)**

### **Warum JourneyStation?**
**Problem:**
```
Journey1: Hauptbahnhof → IsExitOnLeft = true
Journey2: Hauptbahnhof → IsExitOnLeft = false
```
Gleiche Station, aber Journey-spezifische Eigenschaften!

**Lösung:** Junction Entity Pattern
```csharp
// ✅ Journey hat JourneyStations (nicht StationIds)
journey.JourneyStations = [
    new JourneyStation { 
        StationId = hauptbahnhof.Id, 
        IsExitOnLeft = true,     // Journey1-specific
        WorkflowId = workflow1.Id 
    }
]
```

### **Warum City.Stations bleibt List<Station>?**
- City ist **NICHT Teil von Project** aggregate
- City ist **Library/Auswahlhilfe** (wie Locomotive Library)
- Wird geladen aus `cities.json` (readonly master data)
- Drag&Drop aus City → erstellt **neue JourneyStation**

### **Warum Project.Stations entfernt?**
- Stations sind **NICHT shared** zwischen Journeys
- Jede Journey hat **eigene JourneyStations** (mit eigenen Properties)
- Station selbst ist nur "Master Data" (Name, InPort, PlatformIds)

---

## 🔄 **Domain Model (Final)**

```
Project (Aggregate Root)
├── Journeys (List<Journey>)
│   └── JourneyStations (List<JourneyStation>)
│       ├── StationId (Guid) → resolves to Station (from City Library)
│       ├── IsExitOnLeft (bool) ← Journey-specific!
│       ├── WorkflowId (Guid?) ← Journey-specific!
│       └── NumberOfLapsToStop (uint) ← Journey-specific!
├── Platforms (List<Platform>)
├── Workflows (List<Workflow>)
├── Trains (List<Train>)
└── Cities (List<City>) ← Library (readonly)
    └── Stations (List<Station>) ← Master Data
        └── PlatformIds (List<Guid>) → resolves to Platforms
```

---

## ⚡ **Quick Fix Commands (Next Session)**

```powershell
cd "C:\Repos\ahuelsmann\MOBAflow"

# 1. Fix JourneyViewModel
code "SharedUI\ViewModel\JourneyViewModel.cs"
# Change: _journey.StationIds → _journey.JourneyStations
# Change: new Station(...) → new JourneyStation(...)

# 2. Fix StationViewModel
code "SharedUI\ViewModel\StationViewModel.cs"
# Add: private readonly JourneyStation _journeyStation
# Change: Model.WorkflowId → _journeyStation.WorkflowId

# 3. Fix JourneyManager
code "Backend\Manager\JourneyManager.cs"
# Change: journey.StationIds[pos] → journey.JourneyStations[pos].StationId

# 4. Rebuild
dotnet build
```

---

## 📊 **Status**

| Refactoring | Before | After | Status |
|-------------|--------|-------|--------|
| **Architecture** | Mixed patterns | JourneyStation pattern | ✅ Complete |
| **City.Stations** | `List<Guid>` (wrong) | `List<Station>` (correct) | ✅ Fixed |
| **Project.Stations** | Exists (wrong) | Removed | ✅ Fixed |
| **Journey.StationIds** | Simple refs | JourneyStations | ✅ Fixed |
| **Build Errors** | 20+ | ~60 | 🚧 Expected |

---

## 🎉 **Feierabend Notes**

**Heute erledigt:**
1. ✅ Architektur korrigiert (City, Project, JourneyStation)
2. ✅ Domain Layer vollständig
3. ✅ MainWindowViewModel.AddStationFromCity fixed

**Nächste Session (1h):**
- Fix 60 Build-Errors (ViewModels → Backend → Tests)
- Grüner Build! 🎯

---

**Session:** 2025-12-08 Part 2  
**Duration:** ~30 Minuten  
**Next:** Fix Build-Errors (60 min estimated)
