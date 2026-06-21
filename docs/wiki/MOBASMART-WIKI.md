# MOBAsmart Wiki

> **Note:** This wiki supplements the main user guide
> [`MOBASMART-USER-GUIDE.md`](MOBASMART-USER-GUIDE.md). Start there for setup and
> daily use; use this page for FAQ, troubleshooting depth, and technical details.

**Platform:** Android  
**Status:** Production  
**Last Updated:** 2025-12-27

---

**Welcome to the MOBAsmart Wiki!** 🚂  

This documentation helps you get the most out of the MOBAsmart Android app.

---

## 📚 Table of Contents

- Getting Started
- Connecting to the Z21
- Lap Counter Settings
- Understanding Lap Counting
- Best Practices
- Troubleshooting
- FAQ
- Technical Details

---

## 🚀 Getting Started

### What you need

- **Android device:** Smartphone or tablet (`Android 7.0+`). Required:
  Yes
- **Roco Z21:** Digital command station (`Z21`, `Z21 start`, `z21`).
  Required: Yes
- **WLAN:** Z21 and Android device in the same network. Required: Yes
- **Feedback modules:** e.g. `Roco 10808`, `10787`. Required: Yes
- **Track contacts:** e.g. `Roco 42614`, `Märklin 74030`.
  Required: Yes

### Installation

#### Google Play Store *(planned)*

 1. Open Google Play Store.  
 2. Search for **"MOBAsmart"**.  
 3. Tap **Install**.  
 4. Open the app.  

#### Manual installation (APK)

 1. Download the APK file.  
 2. **Settings → Security → Unknown sources** → enable.  
 3. Tap the APK file to install.  
 4. Open **MOBAsmart**.  

### First launch

1. **Open the app** → you see the main screen.  
2. **Grant permissions** (network) → tap “Allow”.  
3. **Done!** → The app is ready.  

---

## 🔌 Connecting to the Z21

### Finding the Z21 IP address

#### Method 1: Z21 app (easiest)

 1. Open the **Z21 app** (Roco).  
 2. Go to **Menu → Settings → Z21 information**.  
 3. Note the **IP address** (e.g. `192.168.0.111`).  

#### Method 2: Router web interface

 1. Open your router UI (usually `192.168.0.1` or `192.168.1.1`).  
 2. Go to **Network → Connected devices**.  
 3. Look for **"Z21"** or **"ROCO"**.  
 4. Note the IP address.  

#### Method 3: Network scanner app

 1. Install **"Fing"** or **"Network Scanner"** from Google Play.  
 2. Scan your network.  
 3. Look for a device named **"Z21"**.  
 4. Note the IP address.  

### Establishing the connection

1. **Enter IP address:**
   - Tap into the input field at the top of the screen.  
   - Enter the Z21 IP address (e.g. `192.168.0.111`).  

2. **Connect:**
   - Tap the **connection switch** (next to "Disconnected").  
   - Wait 2–3 seconds.  

3. **Verify connection:**
   - **Green dot** in the top right → ✅ connected.  
   - **Red dot** in the top right → ❌ no connection.  
   - **System stats** (temperature, voltage) are shown.  

### Disconnecting

- Tap the **connection switch** again.  
- Wait until **"Disconnected"** is displayed.  

---

## ⚙️ Lap Counter Settings

### Feedback points (tracks)

#### What are feedback points?

**Feedback points** are the feedback modules on your layout that
detect when a train passes.

**Example:**

```text
Anlage mit 3 Gleiskontakten:
┌─────────────────────────────────────┐
│  Start/Ziel                         │
│     ↓                               │
│  [Track 1] ──────→                 │
│                  ↓                  │
│              [Track 2] ──────→     │
│                              ↓      │
│                          [Track 3] │
│                              ↓      │
│  ←──────────────────────────┘       │
└─────────────────────────────────────┘
```

**Setting:** `CountOfFeedbackPoints = 3`

#### How to configure feedback points

1. **Count your feedback modules:**
   - How many Roco 10808/10787 modules are connected?
   - Each module = 1 feedback point.

2. **Set in the app:**
   - **Tracks:** tap **−** or **+**
   - Example: 3 modules → set to **3**

3. **Result:**
   - The app creates 3 separate counters:
     - Track 1
     - Track 2
     - Track 3

**💡 Tip:** Start with **1 feedback point** for testing.

### Target lap count

#### What is it?

**Target Lap Count** is the number of laps you want to reach.

**Example:**

- **Racing:** 10 laps
- **Endurance test:** 100 laps
- **Short test:** 5 laps

#### How to configure the target lap count

1. **Define the goal:**
   - How many laps should the train run?

2. **Set in the app:**
   - **Target:** tap **−** or **+**
   - Example: 10 laps → set to **10**

3. **Result:**
   - The **progress bar** shows the progress
   - Example: 3 of 10 laps = 30% (`━━━━░░░░░░`)

### Timer filter (anti double-counting)

#### What is the timer filter?

The **timer filter** prevents a long train from being counted
multiple times when it slowly passes a track contact.

**Problem without timer filter:**

```text
Zug fährt über Gleiskontakt:
  Sekunde 0: Lok aktiviert Kontakt     → Count: 1
  Sekunde 2: Wagen 3 noch auf Kontakt  → Count: 2 ❌
  Sekunde 4: Wagen 6 noch auf Kontakt  → Count: 3 ❌
  Sekunde 6: Letzter Wagen verlässt    → Count: 4 ❌

Result: 4 counts, but only 1 pass!
```

**Solution with timer filter (10s):**

```text
Zug fährt über Gleiskontakt:
  Sekunde 0: Lok aktiviert Kontakt     → Count: 1 ✅
  Sekunde 2: Filter aktiv (noch 8s)    → Ignoriert
  Sekunde 4: Filter aktiv (noch 6s)    → Ignoriert
  Sekunde 6: Filter aktiv (noch 4s)    → Ignoriert
  
Nächster Durchgang (12 Sekunden später):
  Sekunde 12: Filter abgelaufen        → Count: 2 ✅

Result: 2 counts, 2 passes = correct!
```

#### How to configure the timer filter

**1. Enable/disable timer:**

- ✅ Checkbox checked → timer active
- ⬜ Checkbox empty → timer inactive

**2. Set interval:**

- Tap **− / +** next to the timer value
- **Values:** 1.0s to 60.0s (steps: 1.0s)

**3. Recommended values:**

- **Short trains** (`2–3 cars`): `5–8 seconds` — fast passes
- **Medium trains** (`4–6 cars`): `10–15 seconds` — standard length
- **Long trains** (`>6 cars`): `15–20 seconds` — long contact time
- **Very slow speed**: `20–30 seconds` — long time over contact

**💡 Tip:** Start with **10 seconds** (default) and adjust as needed.

---

## 📊 Understanding Lap Counting

### Counter display explained

```text
┌──────────────────────────────────────────────┐
│ [5]  Track 1                                 │
│      Lap: 00:12.5  @  22:15:30               │
│      Lap 5/10 ━━━━━━━━━━░░░░░░  50%         │
└──────────────────────────────────────────────┘
```

**Meaning of each element:**

- **`[5]`**: Current lap count. Example: 5 laps completed
- **`Track 1`**: Feedback point number. Example: Track contact no. 1
- **`Lap: 00:12.5`**: Last lap time. Example: 12.5 seconds
- **`@ 22:15:30`**: Timestamp. Example: today at `22:15:30`
- **`Lap 5/10`**: Progress. Example: 5 of 10 target laps
- **`━━━━━━━━━━`**: Progress bar. Example: 50% reached
- **`50%`**: Percentage. Example: half the target

### Badge colours

- **Blue (primary)**: Not active yet. When: no lap recorded
- **Green (accent)**: Active. When: at least 1 lap recorded

### Lap time calculation

**How is lap time calculated?**

```text
Zeit zwischen zwei aufeinanderfolgenden Feedbacks:
 
Durchgang 1: 22:15:30 (Erste Erfassung, keine Zeit)
Durchgang 2: 22:15:42 → Lap Time: 12 Sekunden
Durchgang 3: 22:15:55 → Lap Time: 13 Sekunden
Durchgang 4: 22:16:07 → Lap Time: 12 Sekunden
```

**💡 Note:**

- The **first lap** has no time (start point unknown).
- From the **second lap** onwards, time is measured.
- The app always shows the **last lap time**, not the average.

---

## ✅ Best Practices

### 🏁 Racing setup (3 trains, 10 laps)

**Scenario:** You want to run a race with 3 trains.

#### Hardware setup

```text
3 separate Gleiskontakte:
┌─────────────────────────────────────┐
│  [Track 1] ← Zug 1 (ICE)           │
│  [Track 2] ← Zug 2 (TGV)           │
│  [Track 3] ← Zug 3 (Railjet)       │
└─────────────────────────────────────┘
```

#### App settings

```yaml
Tracks: 3
Target: 10
Timer Filter: ✅ Aktiviert
Intervall: 8 Sekunden (schnelle Züge)
```

#### Racing workflow

1. Press **Reset** → counters to 0
2. **Track Power ON** → enable track power
3. **Start trains** (via Z21 app or handheld controller)
4. **Watch:** which train reaches 10/10 first?
5. **Winner:** train with 100% progress first 🏆

### 🔄 Automatic operation (1 train, continuous)

#### Automatic-operation scenario

One train runs automatically in a loop.

#### Automatic-operation hardware setup

```text
1 Gleiskontakt:
┌─────────────────────────────────────┐
│              ↓                      │
│  ←────── [Track 1] ──────→         │
│              ↑                      │
│  ────────────┘                      │
└─────────────────────────────────────┘
```

#### Automatic-operation app settings

```yaml
Tracks: 1
Target: 50 (lange Session)
Timer Filter: ✅ Aktiviert
Intervall: 15 Sekunden (langsamer Zug)
```

#### Automatic-operation workflow

1. **Track Power ON**
2. **Set train speed to 40–50%** (slow, constant speed)
3. **Watch the app** (keep the display on)
4. **After 50 laps:** stop the train and analyse the statistics

### 📱 Display management (long sessions)

#### Display-management problem

Battery drains and the display turns off.

#### Solutions

##### 1. Increase screen timeout

```text
Android Einstellungen
→ Display
→ Bildschirm-Timeout
→ 10 Minuten
```

##### 2. Developer options (with charger!)

```text
Android Einstellungen
→ Entwickleroptionen
→ Display bleibt an
→ ✅ Aktivieren
→ Ladegerät anschließen!
```

##### 3. Power bank

```text
USB-C Power Bank anschließen
→ Display auf 50% Helligkeit
→ Nachtmodus aktivieren (spart Energie)
```

---

## 🛠️ Troubleshooting

### Problem: No connection to Z21

#### Connection symptoms

- Red dot in the top right
- “Disconnected” is shown
- No system stats visible

#### Z21-connection solutions

##### 1. Check IP address

```text
Richtig: 192.168.0.111
Falsch:  192.168.0.1   (Router, nicht Z21!)
Falsch:  192.168.1.111 (falsches Subnetz)
```

##### 2. Check WLAN connection

- Is the **Android device** in the same WLAN as the **Z21**?
- Router setting: **“AP isolation”** should be disabled
  - Some routers isolate WLAN devices from each other.

##### 3. Restart Z21

```text
1. Stromversorgung der Z21 trennen
2. 10 Sekunden warten
3. Stromversorgung wieder anschließen
4. 30 Sekunden warten (Bootvorgang)
5. In MOBAsmart erneut verbinden
```

##### 4. Check firewall

- Are you using a **firewall app** on Android?
- MOBAsmart must be allowed to use **UDP port 21105**.

---

### Problem: Lap counters do not increase

#### Counter-increase symptoms

- Train passes a track contact
- Counter stays at 0 or does not increase

#### Counter-increase solutions

##### 1. Feedback points configured correctly?

```text
Anzahl Rückmeldemodule an deiner Anlage:
→ 3 Module = Tracks: 3 einstellen
 
Wenn falsch eingestellt:
→ Track 1, 2, 3 vorhanden, aber Track 4 wird erwartet
→ Feedbacks gehen verloren!
```

##### 2. Does the Z21 receive feedbacks?

```text
Test mit Z21 App:
1. Z21 App öffnen
2. Menü → Rückmeldungen
3. Zug über Gleiskontakt fahren
4. Leuchtet die LED auf? → Rückmeldung funktioniert
```

##### 3. Check wiring

```text
Rückmeldemodule (Roco 10808):
- Korrekt an Z21 angeschlossen? (RBus)
- Gleiskontakte richtig verkabelt?
- Plus/Minus vertauscht? → Funktioniert trotzdem!
- Kontakte sauber? (Oxidation verhindert Kontakt)
```

##### 4. App in foreground?

```text
⚠️ WICHTIG: App muss sichtbar sein!
- Display an?
- App nicht im Hintergrund?
- Andere App im Vordergrund? → MOBAsmart wieder öffnen!
```

---

### Problem: Double counting

#### Double-counting symptoms

- Train passes once  
- Counter increases by 2, 3 or 4  

#### Double-counting solutions

##### 1. Enable timer filter

```text
✅ Checkbox "Timer in s" anhaken
→ Intervall: 10 Sekunden (Standard)
→ Test: Zug langsam vorbeifahren lassen
→ Nur 1 Count? → Problem gelöst!
```

##### 2. Increase interval

```text
Langer Zug (>6 Wagen):
→ Intervall: 15-20 Sekunden

Sehr langsame Fahrt:
→ Intervall: 20-30 Sekunden
```

##### 3. Check track contacts

```text
Sind mehrere Gleiskontakte zu nah beieinander?
→ Zug aktiviert 2 Kontakte gleichzeitig
→ Lösung: Kontakte weiter auseinander platzieren
```

---

### Problem: App crashes or freezes

#### Crash/freeze solutions

##### 1. Restart the app

```text
1. Task-Switcher öffnen (Quadrat-Symbol)
2. MOBAsmart nach oben wischen (schließen)
3. App-Icon antippen (neu starten)
```

##### 2. Clear cache

```text
Android Einstellungen
→ Apps
→ MOBAsmart
→ Speicher
→ Cache leeren
```

##### 3. Clear app data (⚠️ settings will be lost!)

```text
Android Einstellungen
→ Apps
→ MOBAsmart
→ Speicher
→ Daten löschen
→ App neu starten
```

##### 4. Reinstall the app

```text
1. MOBAsmart deinstallieren
2. Gerät neu starten
3. MOBAsmart neu installieren (Google Play / APK)
```

---

## FAQ

### General questions

#### Does MOBAsmart work with all Z21 variants?

✅ **Yes.** All variants are supported:

- Z21 (black)
- Z21 start (white)
- z21 (small, white)

#### Do I need an internet connection?

❌ **No.** MOBAsmart communicates **locally** via UDP with the Z21.
No cloud and no internet connection required.

#### Are my data uploaded anywhere?

❌ **No.** All data stays **local** on your device. No cloud sync,
no telemetry.

#### Does the app cost anything?

✅ **Free.** MOBAsmart is open source (MIT license).

### Technical questions

#### Which Android version is required?

- **Minimum:** Android 7.0 (Nougat)
- **Recommended:** Android 10+ (better networking performance)

#### Does the app work in the background?

✅ **Yes, while connected.** MOBAsmart starts an Android **foreground
service** with a persistent notification when a Z21 or MOBAflow session
is active. This keeps UDP and SignalR connections alive when you switch
to another app.

**Requirements:**

- Grant **notification permission** on Android 13+ when prompted.
- On some devices, disable battery optimization for MOBAsmart when asked.
- Aggressive OEM power managers may still stop the app; keep MOBAsmart in
  the foreground as a fallback (see
  [Display management](#-display-management-long-sessions)).

**Note:** Higher battery use while the foreground service is running.
Use **Stop** in the notification to end background operation.

#### Can I monitor multiple Z21 units at once?

❌ **Not at the moment.** The app supports only **one Z21 connection**
at a time.

#### Why doesn’t the app offer loco control?

💡 **Design decision:** MOBAsmart focuses on **monitoring**
(lap counting, feedback events). For locomotive control, use the
official **Z21 app** or **MOBAflow (WinUI)**.

#### Can I export lap counts?

⏳ **Planned.** Export to **CSV** or **JSON** is planned for a future
version.

### Troubleshooting questions

#### Why doesn’t the app connect?

Most common causes:

1. **Wrong IP address** → verify in the Z21 app.  
2. **Wrong WLAN** → Android device in guest network?  
3. **AP isolation active** → check router settings.  
4. **Z21 powered off** → check power supply.

#### Why does only Track 1 count, but not Track 2/3?

Possible causes:

1. **Wrong number of tracks** → set `Tracks: 3` (not 1).  
2. **Feedback modules not connected** → check R-Bus wiring.  
3. **Faulty track contacts** → test with the Z21 app.

## Technical Details

### UDP communication

**Protocol:** Z21 LAN protocol (Roco)  
**Port:** 21105 (UDP)  
**Direction:** bidirectional (app ↔ Z21)

**Sent commands:**

- `LAN_GET_SERIAL_NUMBER` → query Z21 serial number
- `LAN_GET_HWINFO` → query hardware information
- `LAN_SYSTEMSTATE_GETDATA` → query system status (polling every 5s)
- `LAN_SET_TRACK_POWER_ON/OFF` → toggle track power on/off

**Received events:**

- `LAN_SYSTEMSTATE_DATACHANGED` → system status (current, temperature)
- `LAN_RMBUS_DATACHANGED` → feedback bus event (feedback!)
- `LAN_X_TURNOUT_INFO` → turnout state
  (currently not used in MOBAsmart)

### Feedback event processing

```csharp
// Pseudocode
OnFeedbackReceived(FeedbackResult feedback)
{
    // 1. Finde Zähler für InPort
    var stat = Statistics.FirstOrDefault(s => s.InPort == feedback.InPort);
    
    // 2. Timer Filter prüfen
    if (UseTimerFilter)
    {
        var elapsed = (DateTime.Now - lastFeedbackTime).TotalSeconds;
        if (elapsed < TimerIntervalSeconds)
            return; // Ignorieren (zu früh)
    }
    
    // 3. Rundenzeit berechnen
    if (stat.LastFeedbackTime != null)
        stat.LastLapTime = DateTime.Now - stat.LastFeedbackTime;
    
    // 4. Count erhöhen
    stat.Count++;
    stat.LastFeedbackTime = DateTime.Now;
}
```

### Data model

```csharp
public class InPortStatistic
{
    public int InPort { get; set; }              // 1, 2, 3, ...
    public string Name { get; set; }             // "Track 1", "Track 2", ...
    public int Count { get; set; }               // Rundenzahl
    public int TargetLapCount { get; set; }      // Ziel-Runden
    public DateTime? LastFeedbackTime { get; set; } // Letzter Durchgang
    public TimeSpan LastLapTime { get; set; }    // Letzte Rundenzeit
    public double Progress => (double)Count / TargetLapCount; // 0.0 - 1.0
    public bool HasReceivedFirstLap => Count > 0; // Badge-Farbe
}
```

### Settings persistence

**Storage location:** `/data/user/0/com.mobaflow.mobasmart/files/appsettings.json`

**Format:**

```json
{
  "Counter": {
    "CountOfFeedbackPoints": 3,
    "TargetLapCount": 10,
    "UseTimerFilter": true,
    "TimerIntervalSeconds": 10.0
  },
  "Z21": {
    "CurrentIpAddress": "192.168.0.111",
    "DefaultPort": "21105"
  }
}
```

**Auto-save:** Changes are saved **immediately** (after every `+`/`−` click).

---

## 📜 License & Credits

**MOBAsmart** is part of the **MOBAflow** project.

- **License:** MIT License
- **Author:** Andreas Huelsmann
- **Repository:** `https://dev.azure.com/ahuelsmann/MOBAflow`
- **Version:** 1.0 (December 2025)

### Third-party software

- **Roco Z21** – digital command station & protocol
- **.NET MAUI** – cross-platform framework (Microsoft)
- **CommunityToolkit.Mvvm** – MVVM framework (via SharedUI)
- **CommunityToolkit.Maui** – MAUI helpers and converters
- **AndroidX Startup** – Android initialization providers

See [`THIRD-PARTY-NOTICES.md`](../THIRD-PARTY-NOTICES.md) for full license information.

---

## 🤝 Contributing

**Found a bug? Want to request a feature?**

1. **Create an Azure DevOps work item (bug):**  
   `https://dev.azure.com/ahuelsmann/MOBAflow/_workitems/create/Bug`

2. **Submit a pull request:**  
   Fork → feature branch → Pull Request

3. **Send feedback via e-mail:**  
   `andreas.huelsmann@web.de`

---

## 📖 Further documentation

- **User Guide (compact):** [`MOBASMART-USER-GUIDE.md`](MOBASMART-USER-GUIDE.md)
- **Architecture:** [`../ARCHITECTURE.md`](../ARCHITECTURE.md)
- **Project overview & contributing:** [`../../README.md`](../../README.md)

---

**Enjoy using MOBAsmart!** 🚂✨
