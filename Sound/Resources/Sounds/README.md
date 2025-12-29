# MOBAflow Audio Library

This directory contains sound effects for workflow actions in MOBAflow.

## 📁 Directory Structure

### 🚉 Station/
Sounds for station-related events:
- `arrival_bell.wav` - Bell sound when train arrives at station
- `departure_bell.wav` - Bell sound when train departs
- `gong.wav` - Generic gong/chime for announcements
- `platform_bell.wav` - Platform warning bell
- `ticket_machine.wav` - Optional: Background ambience

**Recommended Freesound searches:**
- "station bell"
- "railway bell"
- "gong chime"
- "platform warning"

---

### 🚂 Train/
Sounds for train-related actions:
- `whistle_short.wav` - Short steam whistle
- `whistle_long.wav` - Long steam whistle
- `horn_diesel.wav` - Diesel horn
- `horn_electric.wav` - Electric train horn
- `brake_squeal.wav` - Brake sound (optional)
- `door_close.wav` - Train door closing (optional)

**Recommended Freesound searches:**
- "train whistle"
- "steam whistle"
- "diesel horn"
- "train horn"
- "train brake"

---

### 🚦 Signals/
Sounds for railway signals and warnings:
- `warning_beep.wav` - Generic warning beep
- `signal_ding.wav` - Signal confirmation sound
- `crossing_bell.wav` - Level crossing bell
- `emergency_horn.wav` - Emergency signal

**Recommended Freesound searches:**
- "warning signal"
- "railway crossing"
- "alert beep"
- "emergency horn"

---

### 🌆 Ambient/
Background sounds (optional):
- `station_ambient.wav` - Station background noise
- `crowd_murmur.wav` - People talking
- `rain.wav` - Rain on platform
- `wind.wav` - Wind sound

**Recommended Freesound searches:**
- "train station ambience"
- "railway ambience"
- "crowd ambience"

---

## 🎵 Audio File Requirements

**Format Specifications:**
- ✅ **File Format:** `.wav` (PCM)
- ✅ **Sample Rate:** 44100 Hz (CD quality) or 48000 Hz
- ✅ **Bit Depth:** 16-bit
- ✅ **Channels:** Mono or Stereo
- ❌ **Not Supported:** .mp3, .ogg, .flac (must convert to .wav)

**Duration Recommendations:**
- Station bells: 2-4 seconds
- Train whistles: 1-3 seconds
- Warning signals: 1-2 seconds
- Gongs/Chimes: 0.5-1 seconds
- Ambient loops: 10-30 seconds (optional)

---

## 📥 How to Add Sounds

### Step 1: Download from Freesound.org
1. Search for sound (e.g., "train bell")
2. Filter by License: **Creative Commons 0** (no attribution required)
3. Download as `.wav` format
4. Save to appropriate subfolder

### Step 2: Copy to Project
```powershell
# Example: Copy downloaded sound to Station folder
copy C:\Downloads\arrival_bell.wav Sound\Resources\Sounds\Station\
```

### Step 3: Use in Workflow
1. Create Audio Action in Workflow
2. Click **Browse...** button in properties panel
3. Navigate to sound file in output directory
4. Select sound file
5. FilePath will be automatically set

**Or manually enter path:**
```
Resources\Sounds\Station\arrival_bell.wav
```

---

## 🎧 Testing Sounds

### Test Audio Playback in Windows:
1. Open File Explorer → Navigate to sound file
2. Double-click .wav file
3. Windows Media Player should play it
4. Verify volume is audible

### Test in MOBAflow:
1. Create Workflow with Audio Action
2. Set FilePath to sound file
3. Trigger workflow via feedback
4. Sound should play immediately

---

## ⚖️ Licensing Notes

**Recommended License Types:**
- ✅ **CC0 (Public Domain)** - No attribution required, free for all uses
- ✅ **CC-BY 4.0** - Attribution required (mention author in README)
- ❌ **CC-BY-NC** - Non-commercial only (avoid for flexibility)

**Attribution Template (if using CC-BY):**
```
arrival_bell.wav by [Username] on Freesound.org (CC-BY 4.0)
https://freesound.org/people/[Username]/sounds/[ID]/
```

---

## 🔊 Volume Control

**Note:** Audio volume is controlled by:
- ✅ **Windows System Volume** (Settings → Sound)
- ✅ **Application Volume Mixer** (Volume icon → App volume settings)
- ❌ **Not controlled in MOBAflow** (uses system volume)

**Tip:** Normalize all sounds to similar volume levels using Audacity before adding to library.

---

## 📝 Sound Naming Conventions

Use descriptive, lowercase names with underscores:
- ✅ `arrival_bell.wav`
- ✅ `whistle_short.wav`
- ✅ `crossing_warning.wav`
- ❌ `sound1.wav` (not descriptive)
- ❌ `ArrivalBell.wav` (use lowercase)

---

## 🚀 Quick Start Example

**Workflow: "Station Arrival Sequence"**
```
Action #1: Audio
  ├─ FilePath: Resources\Sounds\Train\whistle_short.wav
  └─ (Train approaches station)

Action #2: Announcement
  ├─ Message: "{TrainName} erreicht {StationName}"
  └─ (TTS announcement)

Action #3: Audio
  ├─ FilePath: Resources\Sounds\Station\arrival_bell.wav
  └─ (Station bell confirms arrival)
```

---

## 📚 Recommended Sound Packs

**Freesound.org Collections:**
1. Search: "train sound pack"
2. Search: "railway sound effects"
3. Search: "station ambience pack"

**Individual High-Quality Sounds:**
- "station-bell" by InspectorJ (CC-BY 4.0)
- "train-whistle" by Benboncan (CC-BY 3.0)
- "gong-chime" by kwahmah_02 (CC0)

---

**Last Updated:** 2025-12-24  
**MOBAflow Version:** 3.9
