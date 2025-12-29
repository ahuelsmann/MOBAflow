# Ambient Sounds

Place background/ambient sound effects here (optional).

## Files

### station_ambient.wav
- **Purpose:** Station background atmosphere
- **Duration:** 10-30 seconds (loopable)
- **Freesound Search:** "train station ambience", "railway ambience"
- **Usage:** Background loop during station operations

### crowd_murmur.wav
- **Purpose:** People talking/crowd noise
- **Duration:** 10-30 seconds (loopable)
- **Freesound Search:** "crowd murmur", "people talking"
- **Usage:** Busy station atmosphere

### rain.wav
- **Purpose:** Rain on platform roof
- **Duration:** 10-30 seconds (loopable)
- **Freesound Search:** "rain ambience", "rain on roof"
- **Usage:** Weather atmosphere

### wind.wav
- **Purpose:** Wind sound effect
- **Duration:** 10-30 seconds (loopable)
- **Freesound Search:** "wind ambience", "outdoor wind"
- **Usage:** Weather atmosphere

## Note on Ambient Sounds

⚠️ **Ambient sounds are optional and require special handling:**

- **Loop Support:** Current `WindowsSoundPlayer` does NOT support looping
- **Manual Loop:** You would need to trigger the sound repeatedly via workflow
- **Alternative:** Use background music player (not implemented yet)

**For now, focus on Station/Train/Signal sounds which are one-shot effects.**

## Future Enhancement

If ambient looping is needed, consider:
```csharp
// Future: MusicPlayer with loop support
services.AddSingleton<IMusicPlayer, WindowsMusicPlayer>();

// Supports:
await musicPlayer.PlayLoopAsync("station_ambient.wav");
await musicPlayer.StopAsync();
```

Currently not implemented - ambient sounds are **optional placeholders**.
