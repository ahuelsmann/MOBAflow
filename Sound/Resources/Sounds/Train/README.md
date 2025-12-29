# Train Sounds

Place train-related sound effects here.

## Files

### whistle_short.wav
- **Purpose:** Short steam locomotive whistle
- **Duration:** 1-2 seconds
- **Freesound Search:** "train whistle short", "steam whistle"
- **Usage:** Signal departure or arrival

### whistle_long.wav
- **Purpose:** Long steam locomotive whistle
- **Duration:** 2-3 seconds
- **Freesound Search:** "train whistle long", "steam horn"
- **Usage:** Journey start signal

### horn_diesel.wav
- **Purpose:** Diesel locomotive horn
- **Duration:** 1-2 seconds
- **Freesound Search:** "diesel horn", "train horn"
- **Usage:** Modern train signal

### horn_electric.wav
- **Purpose:** Electric train horn
- **Duration:** 1-2 seconds
- **Freesound Search:** "electric train horn", "modern train horn"
- **Usage:** High-speed train signal

### brake_squeal.wav (optional)
- **Purpose:** Train brake sound
- **Duration:** 1-2 seconds
- **Freesound Search:** "train brake", "brake squeal"
- **Usage:** Train stopping at station

### door_close.wav (optional)
- **Purpose:** Train door closing sound
- **Duration:** 1 second
- **Freesound Search:** "train door", "metro door close"
- **Usage:** Before departure

## Example Workflows

```
Workflow: "Departure Sequence"
├─ Audio: door_close.wav
├─ Audio: whistle_short.wav
├─ Command: Lok Speed=50, Direction=Forward
└─ Announcement: "Abfahrt von {StationName}"
```

```
Workflow: "Journey Start"
├─ Audio: whistle_long.wav
├─ Announcement: "Gute Fahrt mit {TrainName}"
└─ Command: Lok Speed=80
```
