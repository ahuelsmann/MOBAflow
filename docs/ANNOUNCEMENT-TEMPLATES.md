# Announcement Message Templates - User Guide

**Feature**: Template-based TTS Announcements  
**Status**: ✅ Implemented  
**Version**: 1.0

---

## 🎤 Overview

Announcement actions in workflows support **template placeholders** that are automatically replaced with contextual information during execution.

This allows you to create **dynamic announcements** that adapt to the current journey state without manually editing workflows.

---

## 📝 Supported Placeholders

### `{StationName}`
**Replaced with**: Current station name  
**Context Required**: Station must be set in ActionExecutionContext  
**Example**:
```
Template: "Nächster Halt {StationName}."
Result:   "Nächster Halt Hamburg Hauptbahnhof."
```

### `{JourneyName}`
**Replaced with**: Journey template text (from `Journey.Text` property)  
**Context Required**: JourneyTemplateText must be set in ActionExecutionContext  
**Example**:
```
Template: "Willkommen auf der Fahrt {JourneyName}."
Result:   "Willkommen auf der Fahrt nach Berlin."
```

### `{ExitSide}`
**Replaced with**: "links" or "rechts" based on `Station.IsExitOnLeft`  
**Context Required**: Station must be set in ActionExecutionContext  
**Example**:
```
Template: "Ausstieg in Fahrtrichtung {ExitSide}."
Result:   "Ausstieg in Fahrtrichtung rechts."
```

---

## 🚂 Example: Station Arrival Announcement

### Template Message
```
Nächster Halt {StationName}. Ausstieg in Fahrtrichtung {ExitSide}.
```

### Execution Context
```csharp
ActionExecutionContext context = new()
{
    CurrentStation = new Station 
    { 
        Name = "Köln Hauptbahnhof",
        IsExitOnLeft = false // Exit on right side
    }
};
```

### Rendered Message
```
Nächster Halt Köln Hauptbahnhof. Ausstieg in Fahrtrichtung rechts.
```

### Audio Output (TTS)
```
🔊 "Nächster Halt Köln Hauptbahnhof. Ausstieg in Fahrtrichtung rechts."
```

---

## 🛠️ Creating Announcement Workflows

### Via Editor UI (Future Implementation)
1. Navigate to **Editor → Workflows**
2. Click `+` to add new workflow
3. Name: "Station Arrival Announcement"
4. Click **"Announcement"** button
5. Configure:
   - **Message**: `Nächster Halt {StationName}. Ausstieg in Fahrtrichtung {ExitSide}.`
   - **Voice**: `de-DE-KatjaNeural` (or leave default)
   - **Rate**: `0` (normal speed)
   - **Volume**: `80`
6. Save workflow
7. Assign to station in Journey Editor

### Via JSON (Current Method)
```json
{
  "name": "Station Arrival Announcement",
  "actions": [
    {
      "number": 1,
      "name": "Announce Station",
      "type": "Announcement",
      "parameters": {
        "Message": "Nächster Halt {StationName}. Ausstieg in Fahrtrichtung {ExitSide}.",
        "VoiceName": "de-DE-KatjaNeural",
        "Rate": 0,
        "Volume": 0.8
      }
    }
  ]
}
```

---

## 🔄 How Template Replacement Works

### Execution Flow

```
1. User clicks "Simulate Feedback" or train passes sensor
        ↓
2. JourneyManager.ProcessFeedbackAsync()
        ↓
3. Increment journey.CurrentCounter
        ↓
4. Compare with station.NumberOfLapsToStop
        ↓
5. [If threshold reached]
        ↓
6. Set ActionExecutionContext:
   - context.CurrentStation = currentStation
   - context.JourneyTemplateText = journey.Text
        ↓
7. WorkflowService.ExecuteAsync(station.Flow, context)
        ↓
8. ActionExecutor.ExecuteAnnouncementAsync(action, context)
        ↓
9. ReplaceTemplatePlaceholders(message, context)
   - Replace {StationName} → "Hamburg Hauptbahnhof"
   - Replace {ExitSide} → "rechts"
        ↓
10. SpeakerEngine.AnnouncementAsync(processedMessage, voice)
        ↓
11. Azure Speech TTS or Fallback synthesizes audio
        ↓
12. 🔊 Audio plays through speakers
```

---

## 📋 Template Best Practices

### ✅ Do
- Use clear, natural language
- Include context (station name, direction)
- Test with Azure Speech preview
- Use German locale for German messages
- Keep messages concise (< 20 seconds)

### ❌ Don't
- Use placeholders that won't be set (will remain as `{PlaceholderName}`)
- Mix languages in one message
- Use special characters that TTS struggles with
- Make messages too long (causes delays)

---

## 🧪 Testing Announcements

### Manual Test Steps
1. **Create Journey** with station
2. **Create Workflow** with announcement action
3. **Set Template Message**:
   ```
   Nächster Halt {StationName}. Ausstieg in Fahrtrichtung {ExitSide}.
   ```
4. **Assign Workflow** to station
5. **Connect Z21** and enable track power
6. **Simulate Feedback** until threshold reached
7. **Listen** for announcement
8. **Verify**:
   - ✅ Station name correct
   - ✅ Exit side correct
   - ✅ Audio quality acceptable
   - ✅ Timing appropriate

### Debug Output
```
▶ Starting workflow 'Station Arrival Announcement' (ID: ...) with 1 action(s)
  ▶ Executing action #1: Announce Station (Type: Announcement)
    → Replaced {StationName} with 'Hamburg Hauptbahnhof'
    → Replaced {ExitSide} with 'rechts'
    ✓ Announcement: Nächster Halt Hamburg Hauptbahnhof. Ausstieg in Fahrtrichtung rechts. (Voice: de-DE-KatjaNeural)
✅ Workflow 'Station Arrival Announcement' completed successfully
```

---

## 🎨 Advanced Examples

### Multi-Station Journey
```
Journey: Hamburg → Cologne → Frankfurt

Station 1 (Hamburg):
  Message: "Nächster Halt {StationName}. Erste Station unserer Reise."

Station 2 (Cologne):
  Message: "Nächster Halt {StationName}. Wir erreichen in Kürze den Kölner Dom."

Station 3 (Frankfurt):
  Message: "Nächster Halt {StationName}. Endstation. Bitte alle aussteigen."
```

### Bilingual Announcement
```
Workflow: "Bilingual Announcement"

Action 1 (German):
  Message: "Nächster Halt {StationName}. Ausstieg in Fahrtrichtung {ExitSide}."
  VoiceName: "de-DE-KatjaNeural"

Action 2 (English):
  Message: "Next stop {StationName}. Exit on the {ExitSide} side."
  VoiceName: "en-US-JennyNeural"
```

### Express Train
```
Message: "Durchfahrt {StationName}. Bitte nicht aussteigen."
```
(Assign to stations where train doesn't stop)

---

## 🚨 Troubleshooting

### Placeholder Not Replaced
**Symptom**: Announcement says "Nächster Halt {StationName}" literally  
**Cause**: Context not set correctly  
**Fix**: Verify workflow is assigned to station and triggered via JourneyManager

### Wrong Station Name
**Symptom**: Announces previous station or wrong name  
**Cause**: Journey position not updated correctly  
**Fix**: Check JourneyManager lap counting logic

### No Audio Output
**Symptom**: No sound plays  
**Cause**: SpeakerEngine not configured or Azure Speech not available  
**Fix**: 
- Check Azure Speech API key in Settings
- Verify system volume not muted
- Check Health Status icon in status bar

### Garbled Audio
**Symptom**: Speech sounds distorted or robotic  
**Cause**: Network issues or wrong voice name  
**Fix**:
- Check internet connection
- Verify voice name (e.g., "de-DE-KatjaNeural")
- Try different voice

---

## 📚 Related Documentation

- **Journey Manager**: `docs/JOURNEY-MANAGER.md` (lap counting logic)
- **Workflow System**: `docs/WORKFLOW-SYSTEM.md` (execution flow)
- **Azure Speech**: `docs/AZURE-SPEECH-SETUP.md` (TTS configuration)
- **End-to-End Test**: `docs/TODO-END-TO-END-TEST.md` (complete test guide)

---

## 🔄 Future Enhancements

### Planned Features
- [ ] UI editor for announcement templates
- [ ] More placeholders: `{Time}`, `{Date}`, `{Platform}`, `{Track}`
- [ ] Preview/test button in workflow editor
- [ ] Template validation (warn about unused placeholders)
- [ ] Multiple language support per workflow
- [ ] SSML support for advanced prosody control

---

**Status**: Ready for use! 🎤  
**Last Updated**: 2025-12-01  
**Implemented By**: Journey Workflow Implementation
