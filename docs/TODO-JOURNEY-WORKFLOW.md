# TODO: Journey Workflow Implementation

**Priority**: HIGH  
**Target**: Make basic Journey lap counting with announcement workflow functional  
**Status**: Ready to start (TreeView cleanup complete)

---

## 🎯 User Goal

Enable the following workflow:
1. User creates a Journey with multiple stations (e.g., Bielefeld, Cologne, Frankfurt)
2. User sets the Journey's InPort (feedback port number)
3. When a train passes over the physical track feedback sensor:
   - Z21 sends feedback event
   - JourneyManager increments lap counter
   - When station threshold reached → trigger station workflow
   - Workflow plays announcement: "Nächster Halt Bielefeld Hauptbahnhof. Ausstieg in Fahrtrichtung rechts."

---

## ✅ Prerequisites (Completed)

- ✅ TreeView Explorer removed
- ✅ Clean navigation architecture (Frame-based)
- ✅ MainWindowViewModel simplified
- ✅ Build compiles successfully
- ✅ Typed selections available (SelectedJourney, etc.)

---

## 📋 Implementation Steps

### 1. EditorPage Enhancement
**File**: `WinUI/View/EditorPage.xaml` + `.cs`

**Add**:
- Master list of Journeys (ListView/DataGrid)
- Detail panel for selected Journey:
  - Journey name
  - InPort selection (ComboBox with available ports)
  - Station list with reordering (drag & drop or buttons)
- Station detail editor:
  - Name
  - NumberOfLapsToStop
  - Entry/Exit workflows (ComboBox)
  - FeedbackInPort (if different from Journey default)

**Actions**:
- Add Station button
- Remove Station button
- Move Station Up/Down
- Save Journey

### 2. JourneyManager Verification
**File**: `Backend/Manager/JourneyManager.cs`

**Verify**:
- ✅ Subscribes to Z21 feedback events (`IZ21.OnFeedbackChanged`)
- ✅ Increments lap counter on matching InPort
- ✅ Compares counter with `Station.NumberOfLapsToStop`
- ✅ Triggers workflow when threshold reached

**Check**:
```csharp
// Pseudo-code flow:
OnFeedbackReceived(int inPort) {
    var journey = FindJourneyByInPort(inPort);
    if (journey != null) {
        journey.CurrentLapCount++;
        CheckForStationArrival(journey);
    }
}

CheckForStationArrival(Journey journey) {
    var currentStation = journey.GetCurrentStation();
    if (journey.CurrentLapCount >= currentStation.NumberOfLapsToStop) {
        ExecuteWorkflow(currentStation.EntryWorkflow);
        AdvanceToNextStation();
    }
}
```

### 3. Workflow Execution Testing
**File**: `Backend/Services/WorkflowExecutor.cs` (or similar)

**Verify**:
- ✅ Workflow actions execute sequentially
- ✅ Announcement action calls TTS service
- ✅ TTS service uses Azure Speech (if configured) or fallback

**Test**:
1. Create simple workflow: Announcement("Nächster Halt Bielefeld")
2. Attach to station
3. Simulate feedback with `SimulateFeedbackCommand`
4. Verify announcement plays

### 4. OverviewPage (Lap Counter Dashboard)
**File**: `WinUI/View/OverviewPage.xaml`

**Display**:
- Current Journey (if any)
- Current lap count
- Current station
- Next station preview
- Last feedback timestamp
- Connection status

**Actions**:
- Reset counter
- Skip to next station (manual override)

### 5. Announcement Message Template
**Domain**: `Domain/WorkflowAction.cs` (Announcement type)

**Fields**:
- `MessageTemplate` (string with placeholders)
  - Example: "Nächster Halt {StationName}. Ausstieg in Fahrtrichtung {ExitSide}."
- Placeholder replacement at runtime:
  - `{StationName}` → current station name
  - `{ExitSide}` → "links" or "rechts" from station property

---

## 🧪 Testing Plan

### Manual Test Steps
1. **Setup**:
   - Load or create new solution
   - Add Journey: "Hamburg → Cologne → Frankfurt"
   - Set InPort = 1
   - Add stations with NumberOfLapsToStop = 2
   - Create workflow with Announcement action

2. **Connect Z21**:
   - Click "Connect Z21"
   - Enable Track Power
   - Verify counter shows 0

3. **Simulate Feedback**:
   - Enter InPort = 1 in text field
   - Click "Simulate" button
   - **Expected**: Counter increments to 1
   - Click "Simulate" again
   - **Expected**: Counter increments to 2, workflow triggers, announcement plays

4. **Verify**:
   - Check Debug output for workflow execution logs
   - Check speaker/headphones for TTS output
   - Verify next station advances in UI

### Unit Tests to Add
- `JourneyManagerTests.cs`:
  - `WhenFeedbackReceived_ThenCounterIncrements()`
  - `WhenThresholdReached_ThenWorkflowTriggered()`
  - `WhenWorkflowCompleted_ThenAdvanceToNextStation()`
  
- `WorkflowExecutorTests.cs`:
  - `ExecuteAnnouncement_WithTemplate_ReplacesPlaceholders()`

---

## 📚 Related Documentation

- **Architecture**: `docs/ARCHITECTURE.md` (check Clean Architecture compliance)
- **Z21 Protocol**: `docs/Z21-PROTOCOL.md` (feedback event format)
- **Threading**: `docs/THREADING.md` (ensure workflow executes on correct thread)
- **UX Guidelines**: `docs/UX-GUIDELINES.md` (announcement message best practices)

---

## 🚨 Known Issues to Address

1. **Azure Speech Configuration**: 
   - Check if API key is configured
   - Handle fallback to system TTS if Azure unavailable

2. **Thread Safety**:
   - Z21 callbacks run on UDP thread
   - Ensure UI updates dispatched correctly
   - Workflow execution should be async

3. **Error Handling**:
   - What happens if InPort doesn't match any Journey?
   - What if workflow execution fails?
   - How to recover from TTS errors?

---

## 🎯 Success Criteria

- [ ] User can create Journey with 3+ stations via EditorPage
- [ ] User can set InPort for Journey
- [ ] Simulate Feedback increments counter
- [ ] When counter reaches threshold, workflow executes
- [ ] Announcement message plays through speakers
- [ ] Next station advances automatically
- [ ] UI reflects all state changes in real-time
- [ ] No crashes or deadlocks during workflow execution

---

**Ready to Start**: The codebase is now clean and ready for functional implementation!  
**Estimated Time**: 2-3 hours for basic workflow  
**Next Session**: Start with EditorPage master-detail UI
