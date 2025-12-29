# Signal Sounds

Place railway signal and warning sounds here.

## Files

### warning_beep.wav
- **Purpose:** Generic warning beep
- **Duration:** 1 second
- **Freesound Search:** "warning beep", "alert beep"
- **Usage:** General warnings, emergency stop

### signal_ding.wav
- **Purpose:** Signal confirmation sound
- **Duration:** 0.5 seconds
- **Freesound Search:** "signal ding", "confirmation beep"
- **Usage:** Confirm workflow execution

### crossing_bell.wav
- **Purpose:** Level crossing warning bell
- **Duration:** 2-3 seconds (loopable)
- **Freesound Search:** "railway crossing", "level crossing bell"
- **Usage:** Crossing approach warning

### emergency_horn.wav
- **Purpose:** Emergency signal horn
- **Duration:** 1-2 seconds
- **Freesound Search:** "emergency horn", "alarm horn"
- **Usage:** Emergency stop workflow

## Example Workflows

```
Workflow: "Emergency Stop"
├─ Audio: emergency_horn.wav
├─ Command: Lok Speed=0 (all locos)
└─ Announcement: "Notbremse aktiviert"
```

```
Workflow: "Crossing Approach"
├─ Audio: crossing_bell.wav
├─ Command: Signal Red=ON
└─ Audio: warning_beep.wav
```

```
Workflow: "Action Confirmed"
├─ Audio: signal_ding.wav
└─ Announcement: "Aktion ausgeführt"
```
