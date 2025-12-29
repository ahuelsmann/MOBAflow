# Station Sounds

Place station-related sound effects here.

## Files

### arrival_bell.wav
- **Purpose:** Bell sound when train arrives at station
- **Duration:** 2-4 seconds
- **Freesound Search:** "station bell", "arrival bell"
- **Usage:** Audio Action in station arrival workflow

### departure_bell.wav
- **Purpose:** Bell sound when train departs from station
- **Duration:** 2-4 seconds
- **Freesound Search:** "departure bell", "railway bell"
- **Usage:** Audio Action in station departure workflow

### gong.wav
- **Purpose:** Generic gong/chime for announcement start
- **Duration:** 0.5-1 second
- **Freesound Search:** "gong", "chime", "ding"
- **Usage:** Play before TTS announcement

### platform_bell.wav
- **Purpose:** Platform warning bell (doors closing)
- **Duration:** 1-2 seconds
- **Freesound Search:** "platform bell", "warning bell"
- **Usage:** Warning before departure

## Example Workflow

```
Workflow: "Station Arrival"
├─ Audio: gong.wav (0.5s)
├─ Announcement: "Zug {TrainName} erreicht {StationName}" (TTS)
└─ Audio: arrival_bell.wav (3s)
```

## License Note

Ensure all sounds are either:
- CC0 (Public Domain) - no attribution needed
- CC-BY - add attribution to main README.md
