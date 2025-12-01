# TreeView Migration zu Hierarchical ViewModels

**Datum**: 2025-11-29  
**Status**: ✅ Abgeschlossen

## 🎯 Ziel

Migration von flachem `TreeNodeViewModel`-Ansatz zu hierarchischen ViewModels für besseres MVVM und Two-Way Binding.

## ✅ Was wurde implementiert

### 1. Neue ViewModels

#### `SolutionViewModel.cs`
```csharp
public class SolutionViewModel : ObservableObject
{
    public Solution Model { get; }
    public ObservableCollection<ProjectViewModel> Projects { get; }
    
    public void Refresh() // Smart sync mit Model
}
```

**Features:**
- ✅ Wraps `Solution` Model
- ✅ `Projects` als ObservableCollection von ViewModels
- ✅ `Refresh()` synchronisiert intelligent (wiederverwendet existierende VMs)
- ✅ Helper-Methoden: `FindJourneyViewModel()`, `FindWorkflowViewModel()`, etc.

#### `ProjectViewModel.cs`
```csharp
public class ProjectViewModel : ObservableObject
{
    public Project Model { get; }
    public ObservableCollection<JourneyViewModel> Journeys { get; }
    public ObservableCollection<WorkflowViewModel> Workflows { get; }
    public ObservableCollection<TrainViewModel> Trains { get; }
    
    public void Refresh() // Smart sync mit Model
}
```

**Features:**
- ✅ Wraps `Project` Model
- ✅ Hierarchische Collections für TreeView
- ✅ Smart sync erhält Reihenfolge und wiederverwendet VMs

### 2. MainWindowViewModel Updates

**Neue Property:**
```csharp
[ObservableProperty]
private SolutionViewModel? solutionViewModel;
```

**OnSolutionChanged:**
```csharp
partial void OnSolutionChanged(Solution? value)
{
    // ...
    SolutionViewModel = new SolutionViewModel(value);
    // ...
}
```

**Nach Model-Änderungen:**
```csharp
Solution.Projects.Add(newProject);
SolutionViewModel?.Refresh(); // ← Synct ViewModels!
```

### 3. TreeViewBuilder Modernisierung

**Neue Methode:**
```csharp
public ObservableCollection<TreeNodeViewModel> BuildTreeView(SolutionViewModel solutionViewModel)
```

**Key Changes:**
- ✅ Akzeptiert `SolutionViewModel` statt `Solution`
- ✅ Verwendet existierende ViewModels aus `SolutionViewModel.Projects`
- ✅ Keine neuen ViewModels mehr erstellen (Performance!)
- ✅ Alte `BuildTreeView(Solution)` als `[Obsolete]` markiert

### 4. XAML

**Keine Änderung nötig!** 🎉
- ✅ TreeView bindet weiterhin an `ViewModel.TreeNodes`
- ✅ `TreeNodeViewModel.DataContext` enthält jetzt ViewModels statt Models
- ✅ Drag & Drop funktioniert weiterhin (nutzt ViewModels)

## 📊 Vorher vs. Nachher

| Aspekt | Vorher | Nachher |
|--------|--------|---------|
| **Model-Änderungen** | Manuell `BuildTreeView()` | Automatisch via `Refresh()` |
| **ViewModel-Erstellung** | Bei jedem Rebuild neu | Smart sync - Wiederverwendung |
| **Performance** | ⚠️ Langsam bei vielen Nodes | ✅ Schnell - nur Deltas |
| **Two-Way Binding** | ❌ Nicht möglich | ✅ Vorbereitet |
| **Code-Komplexität** | ⭐⭐⭐ Mittel | ⭐⭐ Einfacher |

## 🔄 Wann Refresh() aufrufen?

```csharp
// ✅ Nach Load
Solution.UpdateFrom(loadedSolution);
SolutionViewModel?.Refresh();

// ✅ Nach Add/Remove Project
Solution.Projects.Add(newProject);
SolutionViewModel?.Refresh();

// ✅ Nach New Solution
Solution.UpdateFrom(newSolution);
SolutionViewModel?.Refresh();

// ❌ NICHT nötig bei Änderungen innerhalb bestehender Journeys
// JourneyViewModel.Stations ist bereits ObservableCollection!
journeyVM.Stations.Add(stationVM); // ← Auto-Update!
```

## 🎯 Zukünftige Verbesserungen

### Phase 2: Direkte TreeView-Bindung (Optional)

Aktuell: `TreeViewBuilder` erstellt `TreeNodeViewModel` Wrapper  
Zukunft: Direkt an `SolutionViewModel.Projects` binden

**XAML (Zukunft):**
```xaml
<TreeView ItemsSource="{x:Bind ViewModel.SolutionViewModel.Projects}">
    <TreeView.ItemTemplate>
        <DataTemplate x:DataType="vm:ProjectViewModel">
            <!-- Direkt an ProjectViewModel binden -->
        </DataTemplate>
    </TreeView.ItemTemplate>
</TreeView>
```

**Vorteile:**
- ✅ Kein `TreeViewBuilder` mehr nötig
- ✅ Kein `BuildTreeView()` nach Änderungen
- ✅ 100% automatisches Update
- ✅ Weniger Code

**Warum nicht jetzt?**
- ⚠️ WinUI TreeView erfordert komplexe nested ItemTemplates
- ⚠️ Aktuelle Lösung funktioniert gut
- ⚠️ Migration kann schrittweise erfolgen

## 📝 Breaking Changes

### Für Backend Models

**Keine!** ✅
- Models bleiben POCOs mit `List`
- Keine `INotifyPropertyChanged` erforderlich
- JSON-Serialization unverändert

### Für ViewModels

**Minimal:**
- `TreeNodeViewModel.DataContext` enthält jetzt ViewModels statt Models
- Property-Checks müssen ViewModel-Typen erwarten:
  ```csharp
  // Vorher
  if (node.DataContext is Journey journey)
  
  // Nachher  
  if (node.DataContext is JourneyViewModel journeyVM)
  ```

## ✅ Abnahmekriterien

- [x] Build erfolgreich
- [x] TreeView zeigt Solution-Struktur
- [x] Expansion State bleibt erhalten
- [x] Selection funktioniert
- [x] Property Grid funktioniert
- [x] Drag & Drop funktioniert
- [x] Load Solution aktualisiert Tree
- [x] New Solution aktualisiert Tree
- [x] Add Project aktualisiert Tree
- [x] **Obsolete Code entfernt** ✅
- [x] **Factory-Abhängigkeiten entfernt** ✅
- [x] **Tests aktualisiert** ✅

## 🧹 Code Cleanup (29.11.2025)

### Entfernte Komponenten

1. **TreeViewBuilder.cs**
   - ❌ `[Obsolete] BuildTreeView(Solution)` Methode entfernt
   - ❌ 6 Factory-Felder entfernt:
     - `IJourneyViewModelFactory`
     - `IStationViewModelFactory`
     - `IWorkflowViewModelFactory`
     - `ILocomotiveViewModelFactory`
     - `ITrainViewModelFactory`
     - `IWagonViewModelFactory`
   - ❌ Constructor mit 6 Parametern entfernt
   - ❌ `using Moba.SharedUI.Interface` entfernt

2. **Tests aktualisiert**
   - ✅ `ViewModelTestBase.cs` - TreeViewBuilder ohne Parameter
   - ✅ `TreeViewBuilderTests.cs` - Verwendet jetzt `SolutionViewModel`

### Neue Simplicity

**Vorher:**
```csharp
public TreeViewBuilder(
    IJourneyViewModelFactory journeyViewModelFactory,
    IStationViewModelFactory stationViewModelFactory,
    IWorkflowViewModelFactory workflowViewModelFactory,
    ILocomotiveViewModelFactory locomotiveViewModelFactory,
    ITrainViewModelFactory trainViewModelFactory,
    IWagonViewModelFactory wagonViewModelFactory)
{
    // 6 factories injiziert, aber nie verwendet!
}
```

**Nachher:**
```csharp
public class TreeViewBuilder
{
    // No dependencies! ViewModels come from SolutionViewModel
    
    public ObservableCollection<TreeNodeViewModel> BuildTreeView(
        SolutionViewModel? solutionViewModel)
    {
        // Uses ViewModels directly from solutionViewModel.Projects
    }
}
```

### Vorteile der Vereinfachung

| Aspekt | Vorher | Nachher |
|--------|--------|---------|
| **Dependencies** | 6 Factories | 0 ✨ |
| **Constructor** | 6 Parameter | Parameterlos ✨ |
| **Code Lines** | ~250 | ~180 ✨ |
| **Complexity** | ⭐⭐⭐ | ⭐ ✨ |
| **Testability** | 6 Mocks nötig | Keine Mocks ✨ |
| **DI Setup** | 6 Zeilen | 1 Zeile ✨ |

## 🔗 Verwandte Dateien
