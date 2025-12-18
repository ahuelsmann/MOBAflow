# Station Announcements - Quick Start Guide

**Purpose:** Get announcements working in 5 minutes  
**Target:** Developers testing the feature locally

---

## ⚡ 5-Minute Setup

### Step 1: Get Azure Speech Credentials (2 min)

1. Go to **Azure Portal** → Search "Speech"
2. Create "Speech Services" resource
3. Copy **Subscription Key** and **Region**

**Example:**
```
Key: YOUR_KEY_HERE
Region: germanywestcentral
```

### Step 2: Add to appsettings.json (1 min)

```json
{
  "Speech": {
    "Key": "YOUR_KEY_HERE",
    "Region": "germanywestcentral"
  }
}
```

### Step 3: Create Journey Template (1 min)

In MOBAflow UI:
1. Create Journey
2. Set Template Text:
   ```
   Nächster Halt {StationName}. Ausstieg in Fahrtrichtung {ExitDirection}.
   ```

### Step 4: Test (1 min)

1. Start app
2. Connect to Z21
3. Send feedback for station InPort
4. **Listen!** 🔊

---

## 🔊 What You Should Hear

**For Station "Berlin" with ExitDirection="rechts":**
```
"Nächster Halt Berlin. Ausstieg in Fahrtrichtung rechts."
```

**Speech Output:**
- Language: German (de-DE)
- Voice: Microsoft Azure Neural Voice
- Duration: ~3 seconds
- Output: System default speakers

---

## 🎯 Available Placeholders

| Placeholder | Example Value | Source |
|---|---|---|
| `{StationName}` | "Berlin Hauptbahnhof" | Station.Name |
| `{ExitDirection}` | "rechts" or "links" | Station.IsExitOnLeft |
| `{StationNumber}` | "1", "2", "3" | Position in journey |
| `{TrackNumber}` | "1", "5", "9" | Station.Track |

**Full Example:**
```
"Haltestelle Nummer {StationNumber}: {StationName}. 
Gleis {TrackNumber}. Ausstieg {ExitDirection}."
```

---

## 🧪 Testing Scenarios

### Scenario 1: Simple Announcement
```
Template: "Nächster Halt {StationName}."
Station: "Frankfurt"
Output: "Nächster Halt Frankfurt."
```

### Scenario 2: Full Information
```
Template: "Haltestelle {StationNumber}: {StationName}. 
            Gleis {TrackNumber}. Ausstieg {ExitDirection}."
Station: "Cologne" (position 2, track 1, exit left)
Output: "Haltestelle 2: Cologne. Gleis 1. Ausstieg links."
```

### Scenario 3: Multiple Stations
```
Journey stations:
1. Berlin (exit: rechts)
2. Munich (exit: links)
3. Hamburg (exit: rechts)

Send 3 feedbacks → Hear 3 announcements!
```

---

## 🐛 Troubleshooting

### ❌ "No audio output"

**Possible causes:**
1. Azure credentials wrong → Check `appsettings.json`
2. Speakers not connected → Test with system audio
3. Template empty → Set journey template text
4. Station not reached → Check InPort matching

**Fix:**
```json
// Check credentials
{
  "Speech": {
    "Key": "YOUR_ACTUAL_KEY_NOT_EMPTY",
    "Region": "germanywestcentral"  // Valid region
  }
}
```

---

### ❌ "Audio cuts off mid-speech"

**Possible causes:**
1. Network latency
2. App focus lost
3. Z21 feedback while speaking

**Fix:** Just wait, next station announcement will work

---

### ❌ "Speaking wrong language"

**Current:** German (de-DE)  
**Fix for other language:**
```csharp
// In AzureSpeechProvider or app startup:
_ttsProvider.SetLanguage("en-US");  // English
```

---

## 📊 Performance

- **Announcement generation:** <1ms
- **Azure Speech API call:** 1-3 seconds (depends on network)
- **Audio playback:** Duration of announcement (~3 seconds)
- **Total latency:** ~3-4 seconds from Z21 feedback to audio

**Acceptable?** ✅ Yes - matches real-world train announcements

---

## 🎤 Next Steps After Basic Testing

1. **Add multiple stations** and trigger in sequence
2. **Test with sound effects** (bell + announcement)
3. **Try different templates** with custom text
4. **Set speech rate** (faster/slower)
5. **Log announcements** (what was spoken when)

---

## 💾 Files You Modified

After setup, only two things changed:
1. ✅ `appsettings.json` - Added Azure credentials
2. ✅ Journey template text - In MOBAflow UI

**No code changes needed!**

---

## ✅ Checklist

- [ ] Azure subscription created
- [ ] Speech credentials copied
- [ ] `appsettings.json` updated
- [ ] App restarted
- [ ] Journey created with template
- [ ] Z21 feedback sent
- [ ] Audio heard! 🔊

---

**Status:** Ready to test! 🚀
