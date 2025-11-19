# 🎯 MVP Implementation Plan - MOBAflow Complete CRUD & Modern Editor UI

**Status**: 🟡 In Planning  
**Last Updated**: 2025-01-19  
**Estimated Effort**: ~15-20 hours  

---

## 📋 Executive Summary

This plan implements **complete CRUD functionality** for all model entities in MOBAflow, replacing the current read-only TreeView + PropertyGrid with a modern **Tab-based Editor UI** featuring master-detail layouts.

### Goals:
1. ✅ **Complete MVP CRUD** - Create, Read, Update, Delete for all entities
2. ✅ **Modern Editor UI** - Tab-based master-detail layouts
3. ✅ **Validation** - Prevent invalid deletes (referential integrity)
4. ✅ **UX Improvements** - Rename navigation, fix layouts, remove solution requirements

---

## 🎨 New EditorPage - Tab-Based UI Architecture

### Tab Structure Overview

```
┌─────────────────────────────────────────────────────────┐
│  EditorPage                                              │
├─────────────────────────────────────────────────────────┤
│  [Journeys] [Workflows] [Trains] [Locomotives] [Wagons] [Settings] │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  TAB CONTENT (Master-Detail Layout)                     │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

---

### 1️⃣ Journeys Tab (Master-Detail Layout)

**Layout**: Vertical split with master (Journeys) on top, detail (Stations) on bottom

```
┌──────────────────────────────────────────────────────────┐
│  Journeys & Stations                                      │
├──────────────────────────────────────────────────────────┤
│  ┌────────────────────────────────────────────────────┐  │
│  │ Master: Journeys (DataGrid)                        │  │
│  │ ─────────────────────────────────────────────────  │  │
│  │ Name         | InPort | Train        | Stations   │  │
│  │ Berlin Tour  | 1      | ICE 123      | 5          │  │ ← Selected
│  │ Hamburg Loop | 2      | Regional 456 | 3          │  │
│  └────────────────────────────────────────────────────┘  │
│                                                           │
│  ┌────────────────────────────────────────────────────┐  │
│  │ Detail: Stations of "Berlin Tour" (DataGrid)      │  │
│  │ ─────────────────────────────────────────────────  │  │
│  │ # | Name       | Track | Platforms | Workflow     │  │
│  │ 1 | Berlin Hbf | 5     | 2         | Departure    │  │
│  │ 2 | Potsdam    | 3     | 1         | (None)       │  │
│  │ 3 | Dresden    | 7     | 3         | Arrival      │  │
│  └────────────────────────────────────────────────────┘  │
│                                                           │
│  [➕ Add Journey] [➖ Delete Journey]                     │
│  [➕ Add Station] [➖ Delete Station] [↑] [↓]            │
└──────────────────────────────────────────────────────────┘
```

**Features**:
- ✅ Select Journey → Stations auto-populate below
- ✅ No ComboBox needed - direct master-detail binding
- ✅ Drag & Drop for Station reordering
- ✅ Inline editing in DataGrid
- ✅ Expandable rows for Platforms (sub-grid)

**CRUD Operations**:
- **Journey**: Add, Edit (inline), Delete (with validation - check NextJourney references)
- **Station**: Add to selected Journey, Edit, Delete, Reorder (↑↓ or drag-drop)
- **Platform**: Add to selected Station (expandable row), Edit, Delete

---

### 2️⃣ Workflows Tab (Master-Detail Layout)

**Layout**: Vertical split with master (Workflows) on top, detail (Actions) on bottom

```
┌──────────────────────────────────────────────────────────┐
│  Workflows & Actions                                      │
├──────────────────────────────────────────────────────────┤
│  ┌────────────────────────────────────────────────────┐  │
│  │ Master: Workflows (DataGrid)                       │  │
│  │ ─────────────────────────────────────────────────  │  │
│  │ Name       | InPort | Actions | Used By           │  │
│  │ Departure  | 0      | 3       | Station: Berlin   │  │ ← Selected
│  │ Arrival    | 0      | 2       | Station: Dresden  │  │
│  │ Platform 1 | 5      | 4       | Platform: Gleis 1 │  │
│  └────────────────────────────────────────────────────┘  │
│                                                           │
│  ┌────────────────────────────────────────────────────┐  │
│  │ Detail: Actions of "Departure" (DataGrid)         │  │
│  │ ─────────────────────────────────────────────────  │  │
│  │ # | Type         | Name                  | Preview│  │
│  │ 1 | Announcement | Welcome announcement  | "Wil..." │
│  │ 2 | Command      | Set signal to green   | [0x4...]│
│  │ 3 | Audio        | Door closing sound    | bell.wav│
│  └────────────────────────────────────────────────────┘  │
│                                                           │
│  [➕ Add Workflow] [➖ Delete Workflow]                   │
│  [➕ Announcement] [➕ Command] [➕ Audio] [➖] [↑] [↓]   │
└──────────────────────────────────────────────────────────┘
```

**Features**:
- ✅ Select Workflow → Actions auto-populate below
- ✅ Three Action types: Announcement, Command, Audio
- ✅ Preview column shows content preview
- ✅ "Used By" shows where Workflow is referenced (validation info)

**CRUD Operations**:
- **Workflow**: Add, Edit (inline), Delete (validate not referenced by Station/Platform)
- **Action**: Add (type-specific buttons), Edit, Delete, Reorder (↑↓ or drag-drop)
- **Action Types**:
  - **Announcement**: TextToSpeak property
  - **Command**: Bytes[] property (hex editor)
  - **Audio**: WaveFile path (file picker)

---

### 3️⃣ Trains Tab (Composition Editor)

**Layout**: Vertical split with master (Trains) on top, detail (Locomotives + Wagons) on bottom

```
┌──────────────────────────────────────────────────────────┐
│  Trains & Composition                                     │
├──────────────────────────────────────────────────────────┤
│  ┌────────────────────────────────────────────────────┐  │
│  │ Master: Trains (DataGrid)                          │  │
│  │ ─────────────────────────────────────────────────  │  │
│  │ Name    | Type | Service    | Locs | Wagons       │  │
│  │ ICE 123 | ICE  | Passenger  | 2    | 8            │  │ ← Selected
│  │ RE 456  | RE   | Regional   | 1    | 4            │  │
│  └────────────────────────────────────────────────────┘  │
│                                                           │
│  ┌────────────────────────────────────────────────────┐  │
│  │ Detail: Composition of "ICE 123"                   │  │
│  │ ─────────────────────────────────────────────────  │  │
│  │ Locomotives:                                       │  │
│  │ ┌──────────────────────────────────────────────┐  │  │
│  │ │ Pos | Name      | Address | Manufacturer    │  │  │
│  │ │ 1   | BR 401.1  | 3       | Roco            │  │  │
│  │ │ 2   | BR 401.2  | 4       | Roco (Pushing)  │  │  │
│  │ └──────────────────────────────────────────────┘  │  │
│  │                                                    │  │
│  │ Wagons:                                            │  │
│  │ ┌──────────────────────────────────────────────┐  │  │
│  │ │ Pos | Type      | Class | Manufacturer      │  │  │
│  │ │ 1   | Passenger | 1st   | Märklin           │  │  │
│  │ │ 2   | Passenger | 2nd   | Märklin           │  │  │
│  │ │ 3   | Passenger | 2nd   | Märklin           │  │  │
│  │ └──────────────────────────────────────────────┘  │  │
│  └────────────────────────────────────────────────────┘  │
│                                                           │
│  [➕ Add Train] [➖ Delete Train]                         │
│  [➕ Add Locomotive] [➕ Add Passenger] [➕ Add Goods]    │
│  [➖ Remove] [↑] [↓]                                     │
└──────────────────────────────────────────────────────────┘
```

**Features**:
- ✅ Select Train → Locomotives + Wagons auto-populate below
- ✅ Separate sub-grids for Locomotives and Wagons
- ✅ ComboBox to select from global Locomotive/Wagon library
- ✅ Drag & Drop for composition reordering

**CRUD Operations**:
- **Train**: Add, Edit (inline), Delete (validate not referenced by Journey.Train)
- **Train.Locomotives**: Add from library, Remove, Reorder
- **Train.Wagons**: Add from library (Passenger/Goods), Remove, Reorder

---

### 4️⃣ Locomotives Tab (Grid Editor)

**Layout**: Simple DataGrid with all locomotives (global library)

```
┌──────────────────────────────────────────────────────────┐
│  Locomotives (Global Library)                             │
├──────────────────────────────────────────────────────────┤
│  ┌────────────────────────────────────────────────────┐  │
│  │ Name      | Address | Manufacturer | Series | Color│  │
│  │ BR 401.1  | 3       | Roco         | BR 401 | Red  │  │
│  │ BR 401.2  | 4       | Roco         | BR 401 | Red  │  │
│  │ BR 185    | 5       | Märklin      | BR 185 | Gray │  │
│  └────────────────────────────────────────────────────┘  │
│                                                           │
│  [➕ Add Locomotive] [➖ Delete] [📝 Edit Details]       │
└──────────────────────────────────────────────────────────┘
```

**Features**:
- ✅ Global library of all locomotives (Project.Locomotives)
- ✅ Can be referenced by multiple Trains
- ✅ Inline editing for quick changes
- ✅ Detail dialog for advanced properties (Details object)

**CRUD Operations**:
- **Locomotive**: Add, Edit (inline or dialog), Delete (validate not in any Train)

---

### 5️⃣ Wagons Tab (Grid Editor with Sub-Tabs)

**Layout**: Tab control with two sub-tabs (Passenger, Goods)

```
┌──────────────────────────────────────────────────────────┐
│  Wagons (Global Library)                                  │
├──────────────────────────────────────────────────────────┤
│  [Passenger Wagons] [Goods Wagons]                       │
│  ┌────────────────────────────────────────────────────┐  │
│  │ Passenger Wagons                                   │  │
│  │ ─────────────────────────────────────────────────  │  │
│  │ Name      | Class | Manufacturer | Series | Color │  │
│  │ Apmz 121  | 1st   | Märklin      | Apmz   | Red   │  │
│  │ Bpmz 291  | 2nd   | Märklin      | Bpmz   | Red   │  │
│  └────────────────────────────────────────────────────┘  │
│                                                           │
│  [➕ Add Passenger Wagon] [➖ Delete]                     │
└──────────────────────────────────────────────────────────┘
```

**Features**:
- ✅ Two separate grids: PassengerWagons and GoodsWagons
- ✅ Global library (Project.PassengerWagons, Project.GoodsWagons)
- ✅ Type-specific properties (WagonClass vs. Cargo)

**CRUD Operations**:
- **PassengerWagon**: Add, Edit, Delete (validate not in any Train)
- **GoodsWagon**: Add, Edit, Delete (validate not in any Train)

---

### 6️⃣ Settings Tab (Form Editor)

**Layout**: Form-based editor for Project.Settings

```
┌──────────────────────────────────────────────────────────┐
│  Project Settings                                         │
├──────────────────────────────────────────────────────────┤
│  Azure Speech Service                                     │
│  ┌────────────────────────────────────────────────────┐  │
│  │ Speech Key:      [********************]            │  │
│  │ Speech Region:   [westeurope ▼]                    │  │
│  │ Speaker Engine:  [en-US-Neural ▼]                  │  │
│  │ Voice:           [en-US-JennyNeural ▼]             │  │
│  │ Volume:          [90] ━━━━━━━━━○━━ 100             │  │
│  │ Rate:            [-1] ━━━━━○━━━━━━ +3              │  │
│  └────────────────────────────────────────────────────┘  │
│                                                           │
│  Default Journey                                          │
│  ┌────────────────────────────────────────────────────┐  │
│  │ Journey Name:    [Berlin Tour ▼]                   │  │
│  └────────────────────────────────────────────────────┘  │
│                                                           │
│  UI Settings                                              │
│  ┌────────────────────────────────────────────────────┐  │
│  │ ☑ Reset Window Layout On Start                     │  │
│  └────────────────────────────────────────────────────┘  │
│                                                           │
│  [Save Settings]                                          │
└──────────────────────────────────────────────────────────┘
```

**Features**:
- ✅ Form-based editing (no grid needed)
- ✅ Validation for required fields (SpeechKey, SpeechRegion)
- ✅ ComboBoxes for enum values and references

---

## 📊 Complete Entity CRUD Matrix

| Entity | Create | Read | Update | Delete | Validation |
|--------|--------|------|--------|--------|------------|
| **Solution** | ✅ (New) | ✅ | ✅ | ❌ (Root) | N/A |
| **Project** | ✅ | ✅ | ✅ | ✅ | Can delete if not last |
| **Settings** | ❌ (1:1) | ✅ | ✅ | ❌ | N/A |
| **Journey** | ✅ | ✅ | ✅ | ✅ | Check NextJourney refs |
| **Station** | ✅ | ✅ | ✅ | ✅ | Part of Journey |
| **Platform** | ✅ | ✅ | ✅ | ✅ | Part of Station |
| **Workflow** | ✅ | ✅ | ✅ | ✅ | Check Station/Platform refs |
| **Action.Announcement** | ✅ | ✅ | ✅ | ✅ | Part of Workflow |
| **Action.Command** | ✅ | ✅ | ✅ | ✅ | Part of Workflow |
| **Action.Audio** | ✅ | ✅ | ✅ | ✅ | Part of Workflow |
| **Train** | ✅ | ✅ | ✅ | ✅ | Check Journey.Train refs |
| **Locomotive** | ✅ | ✅ | ✅ | ✅ | Check Train usage |
| **PassengerWagon** | ✅ | ✅ | ✅ | ✅ | Check Train usage |
| **GoodsWagon** | ✅ | ✅ | ✅ | ✅ | Check Train usage |
| **SpeakerEngine** | ✅ | ✅ | ✅ | ✅ | Azure TTS |
| **Voice** | ✅ | ✅ | ✅ | ✅ | Azure TTS |

---

## 📝 Implementation Steps

### Phase 1: Quick Wins (Steps 1-3)
**Estimated Time**: 1-2 hours

#### Step 1: Rename Navigation Items in MainWindow
- [ ] WinUI/View/MainWindow.xaml
  - Change "Solution Explorer" → "Explorer"
  - Change "Project Configuration" → "Editor"
- [ ] Update comments/documentation referencing old names

#### Step 2: Fix Overview Page Layout Scrolling Issue
- [ ] WinUI/View/OverviewPage.xaml
  - Ensure ScrollViewer allows full content visibility when maximized
  - Test with different window sizes
  - Fix any MaxWidth/MaxHeight constraints

#### Step 3: Remove Solution Requirement for Page Navigation
- [ ] WinUI/View/MainWindow.xaml.cs
  - Remove `HasSolution` checks in `NavigationView_ItemInvoked`
  - Allow navigation to Overview and Editor without loaded solution
  - Show empty state in Editor if no solution loaded

---

### Phase 2: Editor Infrastructure (Steps 4-10)
**Estimated Time**: 6-8 hours

#### Step 4: Create EditorPage with TabView
- [ ] WinUI/View/EditorPage.xaml
  - Create TabView with 6 tabs (Journeys, Workflows, Trains, Locomotives, Wagons, Settings)
  - Setup data binding to EditorPageViewModel
- [ ] WinUI/View/EditorPage.xaml.cs
  - Minimal code-behind (MVVM pattern)
- [ ] SharedUI/ViewModel/EditorPageViewModel.cs
  - ObservableCollection for each entity type
  - SelectedTab property
  - Navigation between tabs

#### Step 5: Implement Journeys Tab (Master-Detail)
- [ ] SharedUI/ViewModel/JourneyEditorViewModel.cs
  - ObservableCollection<Journey> Journeys
  - Journey? SelectedJourney
  - ObservableCollection<Station> CurrentStations (filtered by SelectedJourney)
  - AddJourneyCommand, DeleteJourneyCommand
  - AddStationCommand, DeleteStationCommand
  - MoveStationUpCommand, MoveStationDownCommand
- [ ] WinUI/View/EditorPage.xaml (Journeys Tab)
  - DataGrid for Journeys (master)
  - DataGrid for Stations (detail)
  - Buttons for CRUD operations
  - Expandable rows for Platforms

#### Step 6: Implement Workflows Tab (Master-Detail)
- [ ] SharedUI/ViewModel/WorkflowEditorViewModel.cs
  - ObservableCollection<Workflow> Workflows
  - Workflow? SelectedWorkflow
  - ObservableCollection<Action.Base> CurrentActions
  - "Used By" calculation (scan all Stations/Platforms)
  - AddWorkflowCommand, DeleteWorkflowCommand (with validation)
  - AddAnnouncementCommand, AddCommandCommand, AddAudioCommand
  - DeleteActionCommand, MoveActionUpCommand, MoveActionDownCommand
- [ ] WinUI/View/EditorPage.xaml (Workflows Tab)
  - DataGrid for Workflows (master)
  - DataGrid for Actions (detail) with type-specific columns
  - Type-specific add buttons

#### Step 7: Implement Trains Tab (Composition Editor)
- [ ] SharedUI/ViewModel/TrainEditorViewModel.cs
  - ObservableCollection<Train> Trains
  - Train? SelectedTrain
  - ObservableCollection<Locomotive> CurrentLocomotives
  - ObservableCollection<Wagon> CurrentWagons
  - AddTrainCommand, DeleteTrainCommand
  - AddLocomotiveToTrainCommand (ComboBox from Project.Locomotives)
  - AddWagonToTrainCommand (ComboBox from Project.PassengerWagons/GoodsWagons)
  - RemoveLocomotiveCommand, RemoveWagonCommand
  - Reordering commands
- [ ] WinUI/View/EditorPage.xaml (Trains Tab)
  - DataGrid for Trains (master)
  - Two sub-grids: Locomotives and Wagons (detail)

#### Step 8: Implement Locomotives Tab (Grid Editor)
- [ ] SharedUI/ViewModel/LocomotiveEditorViewModel.cs
  - ObservableCollection<Locomotive> Locomotives (from Project.Locomotives)
  - AddLocomotiveCommand, DeleteLocomotiveCommand
  - EditLocomotiveCommand (dialog for Details)
- [ ] WinUI/View/EditorPage.xaml (Locomotives Tab)
  - DataGrid for Locomotives
  - CRUD buttons

#### Step 9: Implement Wagons Tab (Grid Editor with Sub-Tabs)
- [ ] SharedUI/ViewModel/WagonEditorViewModel.cs
  - ObservableCollection<PassengerWagon> PassengerWagons
  - ObservableCollection<GoodsWagon> GoodsWagons
  - AddPassengerWagonCommand, DeletePassengerWagonCommand
  - AddGoodsWagonCommand, DeleteGoodsWagonCommand
- [ ] WinUI/View/EditorPage.xaml (Wagons Tab)
  - Sub-TabView: Passenger | Goods
  - DataGrids for each type

#### Step 10: Implement Settings Tab (Form Editor)
- [ ] SharedUI/ViewModel/SettingsEditorViewModel.cs
  - Bindings to Project.Settings properties
  - SaveSettingsCommand
  - Validation for required fields
- [ ] WinUI/View/EditorPage.xaml (Settings Tab)
  - Form layout with TextBoxes, ComboBoxes, Sliders
  - Save button

---

### Phase 3: Commands & Validation (Steps 11-13)
**Estimated Time**: 4-5 hours

#### Step 11: Add CRUD Commands to MainWindowViewModel
- [ ] SharedUI/ViewModel/WinUI/MainWindowViewModel.cs
  - Expose EditorPageViewModel
  - Wire up commands for TreeView context menus
  - Ensure TreeView updates when entities change in Editor

#### Step 12: Implement Add Commands for All Entity Types
- [ ] Factories for creating new instances with defaults
  - New Journey (default name, empty stations list)
  - New Workflow (default name, empty actions list)
  - New Action (type-specific defaults)
  - New Train, Locomotive, Wagon, etc.
- [ ] Add commands in each ViewModel
  - AddJourneyCommand, AddStationCommand, AddPlatformCommand
  - AddWorkflowCommand, AddActionCommand (type-specific)
  - AddTrainCommand, AddLocomotiveCommand, AddWagonCommand

#### Step 13: Implement Delete Commands with Validation
- [ ] SharedUI/Service/ValidationService.cs
  - `CanDeleteJourney(Journey)` → Check NextJourney refs
  - `CanDeleteWorkflow(Workflow)` → Check Station/Platform.Flow refs
  - `CanDeleteTrain(Train)` → Check Journey.Train refs
  - `CanDeleteLocomotive(Locomotive)` → Check Train usage
  - `CanDeleteWagon(Wagon)` → Check Train usage
  - Return ValidationResult with message if deletion blocked
- [ ] Backend/Model/ValidationResult.cs
  - IsValid, ErrorMessage, BlockingReferences (list of entities)
- [ ] Delete commands in each ViewModel
  - Call ValidationService before delete
  - Show dialog if blocked: "Cannot delete X because it is used by Y"
  - Proceed with delete if valid

---

### Phase 4: TreeView Integration (Steps 14-16)
**Estimated Time**: 2-3 hours

#### Step 14: Add Context Menus to TreeView Nodes
- [ ] WinUI/View/MainWindow.xaml
  - Enhance TreeView.ItemTemplate with dynamic ContextFlyout
  - Node type detection (Journey, Station, Workflow, etc.)
  - Context menu items per type:
    - **Journey**: Add Station, Delete Journey
    - **Station**: Add Platform, Delete Station, Move Up, Move Down
    - **Workflow**: Add Announcement/Command/Audio, Delete Workflow
    - **Train**: Add Locomotive, Add Wagon, Delete Train
    - **Locomotive/Wagon**: Remove from Train, Delete (global)

#### Step 15: Implement Drag-and-Drop Reordering
- [ ] WinUI/View/MainWindow.xaml
  - Enable drag-drop for Station (within Journey)
  - Enable drag-drop for Action (within Workflow)
  - Enable drag-drop for Locomotive/Wagon (within Train)
- [ ] ViewModels
  - Handle DragItemsCompleted event
  - Update model order (Station.Number, Action.Number, Locomotive.Pos, Wagon.Pos)
  - Trigger save

#### Step 16: Update TreeViewBuilder for Real-Time Sync
- [ ] SharedUI/Service/TreeViewBuilder.cs
  - Subscribe to ObservableCollection changes in EditorPageViewModel
  - Rebuild TreeView nodes when entities added/deleted/reordered
  - Preserve expand/collapse state during refresh
  - Highlight newly added nodes

---

### Phase 5: Testing & Verification (Steps 17-19)
**Estimated Time**: 3-4 hours

#### Step 17: Add Validation for Delete Operations
- [ ] Test all delete validation scenarios:
  - Try deleting Journey referenced by NextJourney → Blocked ✅
  - Try deleting Workflow used by Station/Platform → Blocked ✅
  - Try deleting Train used by Journey → Blocked ✅
  - Try deleting Locomotive in Train → Blocked ✅
  - Delete orphaned entities → Allowed ✅

#### Step 18: Create Unit Tests for CRUD Operations
- [ ] Test/SharedUI/EditorPageViewModelTests.cs
  - Test Add commands create entities correctly
  - Test Delete commands respect validation
  - Test reordering updates positions correctly
- [ ] Test/SharedUI/ValidationServiceTests.cs
  - Test each validation rule (CanDeleteJourney, CanDeleteWorkflow, etc.)
  - Test blocking scenarios return correct error messages
  - Test allowed scenarios return IsValid = true

#### Step 19: Verify Complete MVP Functionality
- [ ] **Manual Test Checklist**:
  - [ ] Create new Solution/Project via UI ✅
  - [ ] Add Journey, add Stations to Journey ✅
  - [ ] Add Workflow, add Actions (all 3 types) ✅
  - [ ] Assign Workflow to Station/Platform ✅
  - [ ] Add Train, add Locomotives + Wagons ✅
  - [ ] Assign Train to Journey ✅
  - [ ] Delete entity with references → Blocked with clear message ✅
  - [ ] Delete orphaned entity → Allowed ✅
  - [ ] Reorder Stations/Actions/Composition via drag-drop ✅
  - [ ] Save solution, reload, verify all data persists ✅
  - [ ] Navigate between tabs, no crashes ✅
  - [ ] TreeView reflects Editor changes in real-time ✅
- [ ] **Build & Run Tests**:
  - [ ] All unit tests pass ✅
  - [ ] No warnings ✅
  - [ ] Clean build on Release configuration ✅

---

## 🚨 Known Challenges & Mitigations

### Challenge 1: WinUI 3 DataGrid Limitations
**Problem**: WinUI 3 doesn't have a built-in editable DataGrid like WPF  
**Solution**: Use CommunityToolkit.WinUI.UI.Controls.DataGrid (already referenced)

### Challenge 2: Master-Detail Binding Performance
**Problem**: Large collections (e.g., 100+ Journeys with 1000+ Stations) may lag  
**Solution**: 
- Use virtualization in DataGrid
- Debounce selection changes
- Load details on-demand

### Challenge 3: Workflow Reference Tracking
**Problem**: Finding all references to a Workflow (Station.Flow, Platform.Flow) is expensive  
**Solution**:
- Cache "Used By" info in ValidationService
- Rebuild cache when solution reloads
- Incremental updates on add/delete

### Challenge 4: Action Type-Specific Editing
**Problem**: Different Action types (Announcement, Command, Audio) need different editors  
**Solution**:
- Use DataTemplateSelector in DataGrid
- Show type-specific columns (TextToSpeak, Bytes, WaveFile)
- Action row expansion for complex editing (e.g., byte array hex editor)

---

## 📚 References

- **Main Instructions**: `.copilot-instructions.md` (see section "MVP Goals & Product Vision")
- **Architecture Docs**: `docs/ARCHITECTURE.md`
- **Model Classes**: `Backend/Model/*.cs`
- **Existing UI**: `WinUI/View/ProjectConfigurationPage.xaml` (reference for DataGrid patterns)

---

## ✅ Definition of Done

**MVP is complete when**:
1. ✅ All 16 entity types support full CRUD (Create, Read, Update, Delete)
2. ✅ EditorPage has 6 functional tabs (Journeys, Workflows, Trains, Locomotives, Wagons, Settings)
3. ✅ Delete validation prevents invalid operations with clear error messages
4. ✅ TreeView reflects Editor changes in real-time (two-way sync)
5. ✅ Context menus provide quick CRUD access in TreeView
6. ✅ Drag-and-drop reordering works for Stations, Actions, Composition
7. ✅ All unit tests pass (CRUD operations, validation)
8. ✅ Manual test checklist completed without issues
9. ✅ No regressions (existing features like Solution load/save still work)
10. ✅ User can define complete Journey with Stations, Workflows with Actions, and Trains with Composition **entirely via UI** (no JSON editing needed)

---

## 🎯 Next Steps After MVP

**Future Enhancements** (not part of MVP):
- [ ] Undo/Redo for CRUD operations (currently only for PropertyGrid)
- [ ] Import/Export entities (e.g., export Journey as template)
- [ ] Advanced search/filter in DataGrids
- [ ] Batch operations (e.g., delete multiple Stations at once)
- [ ] Workflow visual designer (drag-drop actions, flow chart view)
- [ ] Journey timeline view (visualize timetable graphically)

---

**End of Plan** 🚀
