# MainWindowViewModel Partial Classes Refactoring

## 🎯 Ziel
Aufteilen von MainWindowViewModel.cs (1360 Zeilen) in 7 logische Partial Classes.

## 📁 Neue Datei-Struktur

```
SharedUI/ViewModel/
├── MainWindowViewModel.cs              (~200 Zeilen) - Core: Fields, Constructor, Properties
├── MainWindowViewModel.Selection.cs     (~150 Zeilen) - Selection Commands & Handlers
├── MainWindowViewModel.Solution.cs      (~100 Zeilen) - Solution/Project Management
├── MainWindowViewModel.Journey.cs       (~120 Zeilen) - Journey & Station CRUD
├── MainWindowViewModel.Workflow.cs      (~100 Zeilen) - Workflow CRUD
├── MainWindowViewModel.Train.cs         (~150 Zeilen) - Train, Locomotive, Wagon CRUD
├── MainWindowViewModel.Z21.cs           (~200 Zeilen) - Z21 Connection & Commands
└── MainWindowViewModel.Settings.cs      (~150 Zeilen) - Settings Properties & Commands
```

---

## 🔧 Automatisches Refactoring-Script

### PowerShell Script: `Refactor-MainWindowViewModel.ps1`

```powershell
# MOBAflow - MainWindowViewModel Partial Classes Refactoring Script
# Copyright (c) 2025 Andreas Huelsmann

$baseDir = "C:\Repo\ahuelsmann\MOBAflow\SharedUI\ViewModel"
$sourceFile = Join-Path $baseDir "MainWindowViewModel.cs"
$backupFile = "$sourceFile.backup"

# Backup erstellen
Copy-Item $sourceFile $backupFile -Force
Write-Host "✅ Backup erstellt: $backupFile" -ForegroundColor Green

# Header für alle Partial Classes
$header = @"
// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Moba.Backend.Interface;
using Moba.Backend.Manager;
using Moba.Common.Configuration;
using Moba.Domain;
using Moba.Domain.Enum;
using Moba.SharedUI.Enum;
using Moba.SharedUI.Helper;
using Moba.SharedUI.Interface;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

"@

# ============================================================================
# 1. MainWindowViewModel.Selection.cs
# ============================================================================
$selectionContent = $header + @"
/// <summary>
/// MainWindowViewModel - Selection Management
/// Handles all entity selection commands and change handlers.
/// </summary>
public partial class MainWindowViewModel
{
    #region Selection Commands

    [RelayCommand]
    private void SelectSolution() =>
        ClearOtherSelections(MobaType.Solution);

    [RelayCommand]
    private void SelectProject(ProjectViewModel? project) =>
        _selectionManager.SelectEntity(project, MobaType.Project, SelectedProject, v => SelectedProject = v);

    [RelayCommand]
    private void SelectJourney(JourneyViewModel? journey) =>
        _selectionManager.SelectEntity(journey, MobaType.Journey, SelectedJourney, v => SelectedJourney = v);

    [RelayCommand]
    private void SelectStation(StationViewModel? station) =>
        _selectionManager.SelectEntity(station, MobaType.Station, SelectedStation, v => SelectedStation = v);

    [RelayCommand]
    private void SelectWorkflow(WorkflowViewModel? workflow) =>
        _selectionManager.SelectEntity(workflow, MobaType.Workflow, SelectedWorkflow, v => SelectedWorkflow = v);

    [RelayCommand]
    private void SelectTrain(TrainViewModel? train) =>
        _selectionManager.SelectEntity(train, MobaType.Train, SelectedTrain, v => SelectedTrain = v);

    [RelayCommand]
    private void SelectLocomotive(LocomotiveViewModel? locomotive) =>
        _selectionManager.SelectEntity(locomotive, MobaType.Locomotive, SelectedLocomotive, v => SelectedLocomotive = v);

    [RelayCommand]
    private void SelectWagon(WagonViewModel? wagon) =>
        _selectionManager.SelectEntity(wagon, MobaType.Wagon, SelectedWagon, v => SelectedWagon = v);

    #endregion

    #region Selection Change Handlers

    partial void OnSelectedProjectChanged(ProjectViewModel? value)
    {
        if (value != null)
        {
            ClearOtherSelections(MobaType.Project);
            CurrentSelectedEntityType = MobaType.Project;
        }
        
        NotifySelectionPropertiesChanged();
    }

    partial void OnSelectedJourneyChanged(JourneyViewModel? value)
    {
        AddStationToJourneyCommand.NotifyCanExecuteChanged();

        if (value != null)
        {
            ClearOtherSelections(MobaType.Journey);
            CurrentSelectedEntityType = MobaType.Journey;
        }
        
        NotifySelectionPropertiesChanged();
    }

    partial void OnSelectedStationChanged(StationViewModel? value)
    {
        if (value != null)
        {
            ClearOtherSelections(MobaType.Station);
            CurrentSelectedEntityType = MobaType.Station;
        }
        
        NotifySelectionPropertiesChanged();
    }

    partial void OnSelectedWorkflowChanged(WorkflowViewModel? value)
    {
        if (value != null)
        {
            ClearOtherSelections(MobaType.Workflow);
            CurrentSelectedEntityType = MobaType.Workflow;
        }
        
        NotifySelectionPropertiesChanged();
    }

    partial void OnSelectedTrainChanged(TrainViewModel? value)
    {
        if (value != null)
        {
            ClearOtherSelections(MobaType.Train);
            CurrentSelectedEntityType = MobaType.Train;
        }
        
        NotifySelectionPropertiesChanged();
    }

    partial void OnSelectedLocomotiveChanged(LocomotiveViewModel? value)
    {
        if (value != null)
        {
            ClearOtherSelections(MobaType.Locomotive);
            CurrentSelectedEntityType = MobaType.Locomotive;
        }
        
        NotifySelectionPropertiesChanged();
    }

    partial void OnSelectedWagonChanged(WagonViewModel? value)
    {
        if (value != null)
        {
            ClearOtherSelections(MobaType.Wagon);
            CurrentSelectedEntityType = MobaType.Wagon;
        }
        
        NotifySelectionPropertiesChanged();
    }

    #endregion

    #region Helper Methods

    private void ClearOtherSelections(MobaType keepType)
    {
        // Keep Project selected when Journey or Station is selected (parent context)
        // Keep Journey selected when Station is selected (parent context)
        if (keepType != MobaType.Project && keepType != MobaType.Journey && keepType != MobaType.Station)
        {
            if (SelectedProject != null) SelectedProject = null;
        }
        
        if (keepType != MobaType.Journey && keepType != MobaType.Station)
        {
            if (SelectedJourney != null) SelectedJourney = null;
        }
        
        if (keepType != MobaType.Station)
        {
            if (SelectedStation != null) SelectedStation = null;
        }
        
        if (keepType != MobaType.Workflow)
        {
            if (SelectedWorkflow != null) SelectedWorkflow = null;
        }
        
        if (keepType != MobaType.Train)
        {
            if (SelectedTrain != null) SelectedTrain = null;
        }
        
        if (keepType != MobaType.Locomotive)
        {
            if (SelectedLocomotive != null) SelectedLocomotive = null;
        }
        
        if (keepType != MobaType.Wagon)
        {
            if (SelectedWagon != null) SelectedWagon = null;
        }
    }

    private void NotifySelectionPropertiesChanged()
    {
        OnPropertyChanged(nameof(HasSelectedEntity));
        OnPropertyChanged(nameof(ShowSolutionProperties));
        OnPropertyChanged(nameof(ShowProjectProperties));
        OnPropertyChanged(nameof(ShowJourneyProperties));
        OnPropertyChanged(nameof(ShowStationProperties));
        OnPropertyChanged(nameof(ShowWorkflowProperties));
        OnPropertyChanged(nameof(ShowTrainProperties));
    }

    #endregion
}
"@

$selectionFile = Join-Path $baseDir "MainWindowViewModel.Selection.cs"
Set-Content -Path $selectionFile -Value $selectionContent -Encoding UTF8
Write-Host "✅ Created: MainWindowViewModel.Selection.cs" -ForegroundColor Green

Write-Host "`n📝 Weitere Partial Classes müssen manuell aus der Original-Datei extrahiert werden." -ForegroundColor Yellow
Write-Host "📖 Siehe vollständige Anleitung in: docs/PARTIAL-CLASSES-GUIDE.md" -ForegroundColor Cyan
```

---

## 📖 Manuelle Schritte (Empfohlen)

Da die automatische Extraktion komplex ist, empfehle ich **manuelle Aufteilung mit Visual Studio**:

### **Schritt 1: Selection Logic (bereits im Script)**
✅ Script erstellt `MainWindowViewModel.Selection.cs`

### **Schritt 2-7: Weitere Partial Classes**

Öffnen Sie `MainWindowViewModel.cs` und:

1. **Kopieren** Sie die relevanten Regions
2. **Erstellen** Sie neue `.cs` Dateien
3. **Ersetzen** Sie `public partial class MainWindowViewModel : ObservableObject` als Header
4. **Löschen** Sie die kopierten Teile aus der Original-Datei
5. **Kompilieren** und **testen** Sie nach jeder Datei

---

## ✅ Checkliste

```
[ ] MainWindowViewModel.Selection.cs     (Script erledigt das)
[ ] MainWindowViewModel.Solution.cs      (AddProject, LoadSolution, SaveSolution, NewSolution)
[ ] MainWindowViewModel.Journey.cs       (AddJourney, DeleteJourney, AddStation, DeleteStation, AddStationFromCity)
[ ] MainWindowViewModel.Workflow.cs      (AddWorkflow, DeleteWorkflow, AddAnnouncement, AddCommand, AddAudio)
[ ] MainWindowViewModel.Train.cs         (AddTrain, DeleteTrain, AddLoco, DeleteLoco, AddWagon, DeleteWagon, Composition)
[ ] MainWindowViewModel.Z21.cs           (ConnectToZ21, DisconnectFromZ21, SetTrackPower, SimulateFeedback, OnConnectionLost)
[ ] MainWindowViewModel.Settings.cs      (Z21IpAddress, CityLibraryPath, SpeechSettings, etc.)
[ ] MainWindowViewModel.cs               (Nur: Fields, Constructor, Core Properties)
```

---

## 🚀 Vorteile nach Refactoring

- ✅ Jede Datei <200 Zeilen (statt 1360)
- ✅ Logische Gruppierung
- ✅ Einfach zu navigieren (Ctrl+, → Dateiname)
- ✅ Bessere Code-Review-Möglichkeiten
- ✅ Paralleles Arbeiten im Team möglich
- ✅ **KEINE funktionalen Änderungen**

---

## 🎯 Nächste Schritte

1. **PowerShell Script ausführen**: Erstellt Selection.cs automatisch
2. **Manuell weitere Dateien** erstellen (oder warten auf vollständiges Script)
3. **Build & Test** nach jeder Datei
4. **Commit** mit Message: "refactor: Split MainWindowViewModel into partial classes"

**Soll ich das PowerShell-Script jetzt ausführen?**
