# Station Announcements - CORRECTED Architecture

**Date:** 2025-12-18 (Updated)  
**Fix:** Announcement is ActionType in Workflow, NOT automatic in JourneyManager  
**Status:** ✅ CORRECTED

---

## 🎯 Corrected Architecture

### **Before (WRONG):**
```
Z21 Feedback → Station reached → JourneyManager calls AnnouncementService DIRECTLY ❌
```

### **After (CORRECT):** ✅
```
Z21 Feedback 
    ↓
Station reached
    ↓
Workflow "Arrival Main Station" executes
    ├─ Action #0: "Arrival Announcement" (Type: 0 = ActionType.Announcement)
    │   └─ ActionExecutor calls ExecuteAnnouncementAsync()
    │       └─ Uses AnnouncementService with Journey template
    │           └─ Generates announcement with placeholder replacement
    │               └─ Speaks via Azure Speech Service 🔊
    │
    └─ Action #1: Other actions (Commands, Audio, etc.)
```

---

## 📋 Flow Details

### **1. Station Reached**
```csharp
// JourneyManager.HandleFeedbackAsync()
if (state.Counter >= currentStation.NumberOfLapsToStop)
{
    // Update state
    state.CurrentStationName = currentStation.Name;
    
    // Execute workflow (if configured for this station)
    await _workflowService.ExecuteAsync(workflow, ExecutionContext);
}
```

### **2. Workflow Executes Actions**
```csharp
// WorkflowService.ExecuteAsync()
foreach (var action in workflow.Actions.OrderBy(a => a.Number))
{
    await _actionExecutor.ExecuteAsync(action, context);
}
```

### **3. Action Type 0 = Announcement**
```csharp
// ActionExecutor.ExecuteAsync()
switch (action.Type)
{
    case ActionType.Announcement:  // Type: 0
        await ExecuteAnnouncementAsync(action, context);
        break;
    // ...other types...
}
```

### **4. Announcement Action Executes**
```csharp
// ActionExecutor.ExecuteAnnouncementAsync()
var announcementText = _announcementService.GenerateAnnouncementText(
    new Journey { Text = context.JourneyTemplateText },  // From Journey.Text
    context.CurrentStation,
    stationIndex
);

await _announcementService.GenerateAndSpeakAnnouncementAsync(
    new Journey { Text = context.JourneyTemplateText },
    context.CurrentStation,
    stationIndex,
    CancellationToken.None
);
```

### **5. Template Replacement**
```
Journey.Text = "Nächster Halt {StationName}. Ausstieg in Fahrtrichtung {ExitDirection}."
Current Station = "Berlin" (IsExitOnLeft = false)

Result: "Nächster Halt Berlin. Ausstieg in Fahrtrichtung rechts."
```

### **6. Azure Speech**
```
Text → SSML → Azure Cognitive Services → Audio Stream → Speakers 🔊
```

---

## ✅ Your Configuration is CORRECT!

```json
{
  "Workflows": [
    {
      "Id": "b2c3d4e5-f6a7-8901-2345-678901bcdef0",
      "Name": "Arrival Main Station",
      "Actions": [
        {
          "Id": "c3d4e5f6-a7b8-9012-cdef-123456789012",
          "Name": "Arrival Announcement",
          "Number": 0,
          "Type": 0,              // ✅ ActionType.Announcement
          "Parameters": {}        // ✅ Empty (uses Journey.Text)
        }
      ]
    }
  ]
}
```

---

## 🔧 How It Works

### **1. Parameters is Empty**
- ✅ **Correct!** Announcement uses `Journey.Text` as template
- ❌ Would be wrong if: Parameters had "Message" key (for SpeakerEngine, old system)

### **2. Type: 0 is ActionType.Announcement**
- ✅ **Correct!** Triggers announcement logic
- Maps to: `ActionType.Announcement` enum

### **3. Execution Order**
- Actions execute in order of `Number` (0, 1, 2, ...)
- "Arrival Announcement" is Number 0 → runs first
- Other actions (Commands, Audio) run after

---

## 📊 Complete Data Flow

```
Journey Configuration:
├─ Journey.Text = "Nächster Halt {StationName}. {ExitDirection}."
├─ Stations
│  ├─ Station 1: "Berlin" (IsExitOnLeft=false)
│  ├─ Station 2: "Munich" (IsExitOnLeft=true)
│  └─ Each station has WorkflowId = "Arrival Main Station"
│
└─ Workflows
   └─ "Arrival Main Station"
      └─ Action 0: "Arrival Announcement" (Type: 0)

Runtime:
1. Z21 sends InPort 1 feedback
2. Station 1 reached ("Berlin")
3. Workflow "Arrival Main Station" executes
4. Action "Arrival Announcement" (Type: 0) executes:
   - GenerateAnnouncementText():
     * Replace {StationName} with "Berlin"
     * Replace {ExitDirection} with "rechts"
     * Result: "Nächster Halt Berlin. rechts."
   - GenerateAndSpeakAnnouncementAsync():
     * Send to Azure Speech Service
     * Play audio on speakers 🔊
```

---

## 🎯 What Changed in Code

### **JourneyManager.cs**
```diff
- // Removed: Direct AnnouncementService call
- if (_announcementService != null)
- {
-     await _announcementService.GenerateAndSpeakAnnouncementAsync(...);
- }

✅ Now: Announcement runs ONLY via Workflow Action
```

### **ActionExecutor.cs**
```diff
+ public ActionExecutor(Interface.IZ21? z21 = null, AnnouncementService? announcementService = null)

+ private async Task ExecuteAnnouncementAsync(WorkflowAction action, ActionExecutionContext context)
+ {
+     // Uses AnnouncementService with Journey template
+ }
```

### **WinUI/App.xaml.cs**
```diff
+ // Register AnnouncementService FIRST
+ services.AddSingleton<Backend.Service.AnnouncementService>(...)
+ 
+ // Register ActionExecutor WITH AnnouncementService
+ services.AddSingleton(sp => new ActionExecutor(z21, announcementService))
```

---

## ✨ Key Points

1. ✅ **Announcement is a Workflow Action**
   - Type: 0 = ActionType.Announcement
   - Executes during Workflow execution
   - Only when Workflow is triggered

2. ✅ **Not Automatic in JourneyManager**
   - JourneyManager only detects station arrival
   - Executes configured Workflow
   - ActionExecutor handles the Announcement action

3. ✅ **Flexible**
   - Can disable Announcement by removing Action from Workflow
   - Can combine with other Actions (Commands, Audio)
   - Reusable across multiple workflows

4. ✅ **Template-Driven**
   - Uses Journey.Text as template
   - Supports placeholders: {StationName}, {ExitDirection}, etc.
   - No need for hardcoded parameters

---

## 🚀 Summary

**Your JSON config is PERFECT!** ✅

Type: 0 (Announcement) means:
- ✅ Will execute announcement action
- ✅ Uses Journey template text
- ✅ Replaces placeholders with current station data
- ✅ Speaks via Azure Speech Service
- ✅ Part of Workflow, not automatic

**Ready to test!** 🔊
