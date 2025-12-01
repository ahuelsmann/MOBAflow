# TODO: End-to-End Journey Workflow Testing

**Priority**: HIGH  
**Status**: Ready for Manual Testing  
**Prerequisites**: ✅ All completed (UI + Backend verified)

---

## 🎯 Test Scenario: "Hamburg → Cologne → Frankfurt"

### Goal
Verify complete workflow from Journey creation → Lap counting → Announcement playback

---

## 📋 Test Steps

### Setup Phase

#### 1. Create Workflow with Announcement
1. Navigate to **Editor → Workflows** tab
2. Click `+` to add new workflow
3. Name: "Station Arrival Announcement"
4. Click "Announcement" button to add action
5. Configure announcement:
   - **Message**: "Nächster Halt {StationName}. Ausstieg in Fahrtrichtung rechts."
   - **Voice**: (default or configure in Settings)
6. Save workflow

#### 2. Configure Azure Speech (Optional but Recommended)
1. Navigate to **Editor → Settings** tab
2. Enter Azure Speech credentials:
   - **Speech Key**: Your Azure API key
   - **Speech Region**: e.g., `westeurope`
   - **Voice Name**: `de-DE-KatjaNeural`
   - **Speech Rate**: `0` (normal speed)
   - **Speech Volume**: `80`
3. Save settings (Ctrl+S)

#### 3. Create Journey
1. Navigate to **Editor → Journeys** tab
2. Click `+` to add new journey
3. Configure Journey:
   - **Name**: "Hamburg → Cologne → Frankfurt"
   - **InPort**: `1` (feedback port number)
   - **BehaviorOnLastStop**: "Loop" (restart from beginning)

#### 4. Add Stations
1. With journey selected, add stations:

**Station 1: Hamburg**
- Click `+` in Stations column
- **Name**: "Hamburg Hauptbahnhof"
- **Description**: "Norddeutschlands größter Bahnhof"
- **NumberOfLapsToStop**: `2`
- **FeedbackInPort**: `0` (use Journey InPort)
- **Station Workflow**: Select "Station Arrival Announcement"

**Station 2: Cologne**
- Click `+` in Stations column
- **Name**: "Köln Hauptbahnhof"
- **Description**: "Am Dom"
- **NumberOfLapsToStop**: `2`
- **FeedbackInPort**: `0`
- **Station Workflow**: Select "Station Arrival Announcement"

**Station 3: Frankfurt**
- Click `+` in Stations column
- **Name**: "Frankfurt Hauptbahnhof"
- **Description**: "Finanzmetropole"
- **NumberOfLapsToStop**: `3`
- **FeedbackInPort**: `0`
- **Station Workflow**: Select "Station Arrival Announcement"

5. **Save Solution** (Ctrl+S or File → Save)

---

### Execution Phase

#### 1. Connect to Z21
1. Click **"Connect Z21"** button in toolbar
2. **Expected**: Status changes to "Connected to [IP]:[Port]"
3. If connection fails:
   - Verify Z21 is powered on
   - Check IP address in **Editor → Settings**
   - Ensure network connectivity

#### 2. Enable Track Power
1. Toggle **"Track Power"** button
2. **Expected**: Button shows "ON" state
3. **Expected**: Status bar shows "Track power ON"

#### 3. Simulate Feedback (Test Lap Counting)

**Round 1 (Hamburg arrival)**
1. Enter `1` in InPort text field
2. Click **"Simulate"** button
3. **Expected**: Debug output: `Journey 'Hamburg → Cologne → Frankfurt': Round 1, Position 0`
4. Click **"Simulate"** again
5. **Expected**: 
   - Debug output: `Station reached: Hamburg Hauptbahnhof`
   - Announcement plays: "Nächster Halt Hamburg Hauptbahnhof. Ausstieg in Fahrtrichtung rechts."
   - Counter resets to 0
   - Position advances to 1 (Cologne)

**Round 2 (Cologne arrival)**
6. Click **"Simulate"** twice more
7. **Expected**:
   - After 2nd click: Announcement for Cologne
   - Position advances to 2 (Frankfurt)

**Round 3 (Frankfurt arrival)**
8. Click **"Simulate"** three times
9. **Expected**:
   - After 3rd click: Announcement for Frankfurt
   - Position resets to 0 (Hamburg) due to Loop behavior

**Round 4 (Verify Loop)**
10. Click **"Simulate"** twice
11. **Expected**: Hamburg announcement plays again (loop confirmed)

---

## ✅ Success Criteria

### Functional Requirements
- [ ] Journey created with 3 stations
- [ ] InPort configured (visible in Journey list)
- [ ] NumberOfLapsToStop configured per station
- [ ] Workflow assigned to each station
- [ ] Z21 connection established
- [ ] Track power enabled

### Execution Requirements
- [ ] Feedback simulation increments counter
- [ ] Counter value displayed in Debug output
- [ ] Workflow executes when threshold reached
- [ ] Announcement message plays through speakers/headphones
- [ ] Announcement contains correct station name
- [ ] Next station advances after workflow execution
- [ ] Last station triggers loop behavior
- [ ] Second loop confirms journey restarts

### Quality Requirements
- [ ] No crashes or exceptions
- [ ] No UI freezing during workflow execution
- [ ] Debug output readable and informative
- [ ] Announcement audio quality acceptable
- [ ] Timing between actions appropriate

---

## 🚨 Troubleshooting

### Z21 Connection Fails
- **Check**: Z21 powered on and connected to network
- **Check**: IP address correct in Settings
- **Check**: Firewall allows UDP port 21105
- **Action**: Try pinging Z21 IP from command prompt

### No Announcement Audio
- **Check**: Azure Speech API key configured
- **Check**: Volume not muted (system and app)
- **Check**: Speakers/headphones connected
- **Check**: Health status icon in status bar (should be green)
- **Action**: Test Azure Speech separately

### Counter Doesn't Increment
- **Check**: Z21 connection active (green icon)
- **Check**: InPort number matches simulation input
- **Check**: Debug window shows feedback received
- **Action**: Check Debug output for errors

### Workflow Doesn't Execute
- **Check**: Workflow assigned to station
- **Check**: NumberOfLapsToStop reached
- **Check**: Workflow has actions (not empty)
- **Check**: Debug output for workflow execution logs
- **Action**: Verify workflow in Workflows tab

### Wrong Station Announced
- **Check**: Template uses `{StationName}` placeholder
- **Check**: Current station index in Debug output
- **Check**: Station order in Journey
- **Action**: Verify template in Workflow action

---

## 📊 Test Results Template

```markdown
## Test Results - [Date]

**Environment**:
- OS: Windows [version]
- Z21: [Physical / Simulated]
- Azure Speech: [Enabled / Disabled]

**Test Case**: Hamburg → Cologne → Frankfurt Loop

| Step | Expected | Actual | Status |
|------|----------|--------|--------|
| Create Journey | Journey with InPort=1 | | ⏳ |
| Add 3 Stations | Hamburg (2), Cologne (2), Frankfurt (3) | | ⏳ |
| Assign Workflows | All stations have workflow | | ⏳ |
| Connect Z21 | "Connected to..." | | ⏳ |
| Track Power ON | Toggle ON | | ⏳ |
| Hamburg Round 1 | Counter = 1 | | ⏳ |
| Hamburg Round 2 | Announcement plays, advance to Cologne | | ⏳ |
| Cologne Round 1 | Counter = 1 | | ⏳ |
| Cologne Round 2 | Announcement plays, advance to Frankfurt | | ⏳ |
| Frankfurt Round 1 | Counter = 1 | | ⏳ |
| Frankfurt Round 2 | Counter = 2 | | ⏳ |
| Frankfurt Round 3 | Announcement plays, loop to Hamburg | | ⏳ |
| Hamburg Loop Test | Announcement plays again | | ⏳ |

**Overall Result**: ⏳ Pending / ✅ Pass / ❌ Fail

**Notes**:
- [Any observations]
- [Issues encountered]
- [Performance comments]
```

---

## 🎥 Debug Output Reference

### Expected Debug Messages

```
📡 Feedback received: InPort 1
🔄 Journey 'Hamburg → Cologne → Frankfurt': Round 1, Position 0
📡 Feedback received: InPort 1
🔄 Journey 'Hamburg → Cologne → Frankfurt': Round 2, Position 0
🚉 Station reached: Hamburg Hauptbahnhof
🎤 Executing Workflow: Station Arrival Announcement
🔊 Announcement: Nächster Halt Hamburg Hauptbahnhof. Ausstieg in Fahrtrichtung rechts.
✅ Workflow execution complete
📡 Feedback received: InPort 1
🔄 Journey 'Hamburg → Cologne → Frankfurt': Round 1, Position 1
[... continue for Cologne and Frankfurt ...]
🏁 Last station of journey 'Hamburg → Cologne → Frankfurt' reached
🔄 Journey will restart from beginning
```

---

## 📝 Post-Test Actions

### If Test Passes ✅
1. Document results in test report
2. Create sample solution file for demo
3. Update user documentation
4. Consider adding automated tests

### If Test Fails ❌
1. Document failure details
2. Check Debug output for error messages
3. Verify configuration steps
4. File issue in TODO list with:
   - Steps to reproduce
   - Expected vs Actual behavior
   - Debug output
   - System environment

---

## 🔄 Next Test Scenarios

Once basic scenario passes, test:
1. **Multiple Journeys**: Two journeys with different InPorts running simultaneously
2. **Physical Z21**: Replace simulation with real hardware
3. **Rapid Feedback**: Test debouncing (multiple rapid clicks)
4. **Edge Cases**: Empty workflows, missing workflows, invalid InPorts
5. **Chain Journeys**: Test `GotoJourney` behavior
6. **Error Recovery**: Disconnect Z21 mid-journey, reconnect

---

**Ready to Test!** 🚂  
Follow the steps above and document your results.
