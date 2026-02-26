# MOBAsmart – User Guide

**Platform:** Android  
**Status:** Production  
**Last Updated:** 2025-12-27

---

## 📱 What is MOBAsmart?

**MOBAsmart** is the Android app for monitoring your model railway layout. It connects directly via UDP to your **Roco Z21 digital command station** and automatically counts train laps based on feedback events.

---

## 🚀 Getting Started

### 1. Requirements

- **Android device** (Android 7.0 or newer)
- **Roco Z21 digital command station** in the same WLAN network
- **Feedback modules** (e.g. Roco 10808) connected to your layout

### 2. Install the app

1. Download the app from the Google Play Store *(or install the APK manually)*.
2. Open **MOBAsmart**.
3. Grant network permissions (if requested).

### 3. Connect to Z21

1. Enter the **IP address** of your Z21 (e.g. `192.168.0.111`).
   - **Tip:** You can find it in the Z21 app under **Settings**.
2. Tap the **connection toggle**.
3. When connected, a **green dot** appears in the top-right corner.

✅ **Successfully connected** if you see the Z21 system status data:
- 🌡️ **Temperature** (e.g. `28°C`)
- 🔌 **Supply voltage** (e.g. `16500mV`)
- ⚡ **VCC voltage** (e.g. `5000mV`)

---

## 🎯 Main Features

### ⚙️ Settings

#### Feedback points (tracks)
- **What is this?** Number of feedback modules on your layout.
- **Example:** If you have 3 track contacts → set it to **3**.
- **How to change:**
  - Tap **−** or **+** next to “Tracks”.
  - The app automatically creates 3 separate counters (Track 1, Track 2, Track 3).

#### Target lap count
- **What is this?** Target number of laps for all tracks.
- **Example:** If you want to drive 10 laps → set it to **10**.
- **How to change:**
  - Tap **−** or **+** next to “Target”.
  - The **progress bar** shows the progress (e.g. 3/10 = 30%).

#### Timer filter
- **What is this?** Prevents double counting for long trains.
- **Why is it important?** A long train can keep a track contact active for several seconds.
- **Recommendation:**
  - ✅ **Enabled** (checkbox checked).
  - **Interval:** 10 seconds (default).
  - **Meaning:** Within 10 seconds, a feedback is only counted once.

**Example:**
```
Without timer filter:
  Train passes Track 1 → Count: 1
  (2 seconds later, train still on Track 1) → Count: 2 ❌ (double count!)

With timer filter (10s):
  Train passes Track 1 → Count: 1
  (2 seconds later, train still on Track 1) → Ignored ✅
  (12 seconds later, next lap) → Count: 2 ✅
```

---

## 📊 Lap Counter

### Understanding the display

Each feedback point has its own counter:

```
┌─────────────────────────────────────────┐
│ [5]  Track 1                            │
│      Lap: 00:12.5  @  22:15:30          │
│      Lap 5/10 ━━━━━━━━━━━░░░░░░  50%   │
└─────────────────────────────────────────┘
```

**Legend:**
- **[5]** → Current lap count
- **Track 1** → Feedback point number
- **Lap: 00:12.5** → Last lap time (12.5 seconds)
- **@ 22:15:30** → Timestamp of last detection
- **Lap 5/10** → 5 of 10 target laps
- **━━━━━━━━━━━** → Progress bar (50%)
- **50%** → Percentage

### Badge colours

- **🟦 Blue (primary):** No lap recorded yet
- **🟢 Green (accent):** At least one lap recorded

### Reset counters

1. Tap **↻ Reset** (top right of the lap counter area).
2. All counters are reset to **0**.
3. All progress bars are reset.

---

## 🔋 Important: Keep the app in the foreground

### ⚠️ Why must the app stay open?

**Android restricts background activity:**
- After ~10 minutes in the background, Android may cut the network connection.
- UDP packets from the Z21 are no longer received.
- **Result:** Lap counts will **not** be updated.

### ✅ How to use MOBAsmart correctly

#### Option 1: Keep app in the foreground (recommended)
1. Start **MOBAsmart**.
2. Connect to the Z21.
3. **Keep the display on** (or use the system “keep screen on” option).
4. Place the phone next to the layout.

**Benefits:**
- ✅ Reliable lap counting
- ✅ Real-time updates
- ✅ No missed events

**Tip:** Use a stand so you can easily see the counters.

#### Option 2: Increase display timeout
1. **Android Settings → Display**
2. **Screen timeout** → set to **10 minutes** or more
3. Place the phone where you can see the app

#### Option 3: “Stay awake” (developer options)
1. **Android Settings → Developer options**
   - If not visible: **About phone** → tap **Build number** 7 times
2. In **Developer options** enable **Stay awake**
3. Connect a charger (high battery usage)

**⚠️ Warning:** High battery consumption – only use with charger connected.

---

## 🔌 Track Power

### Switch on/off

1. Use the **Track Power toggle** to switch Z21 track power on/off
2. **Status:**
   - 🟡 **Yellow (warning):** Track power is **ON** (trains can move)
   - ⚫ **Grey:** Track power is **OFF** (trains stopped)

### When to switch off

- ✅ After running sessions (saves energy)
- ✅ During maintenance (safety)
- ✅ During long pauses

---

## 🛠️ Troubleshooting

### Problem: No connection to Z21

**Solution:**
1. **Check IP address:**
   - Open the Z21 app → Settings → note the IP address
   - Enter the same address in MOBAsmart (e.g. `192.168.0.111`)
2. **Check WLAN:**
   - Is the phone in the **same network** as the Z21?
   - Router settings: “AP isolation” should be disabled
3. **Restart Z21:**
   - Briefly disconnect power (wait ~10 seconds) and reconnect

### Problem: Lap counters do not increase

**Solution:**
1. **Feedback points configured correctly?**
   - `Tracks` = number of feedback modules?
2. **Does the Z21 receive feedbacks?**
   - Test with the Z21 app: show “Feedbacks”
3. **Timer filter interval too short?**
   - Increase interval to **15 seconds**
4. **App in foreground?**
   - See [Important: Keep the app in the foreground](#-important-keep-the-app-in-the-foreground)

### Problem: Double counting

**Solution:**
1. **Enable timer filter:**
   - ✅ Check the “Timer in s” checkbox
2. **Increase interval:**
   - Long trains → **15–20 seconds**
   - Short trains → **5–10 seconds**
3. **Check feedback modules:**
   - Are contacts too close together?
   - Are contacts wired correctly?

---

## 📸 Photo upload to MOBAflow (Windows)

MOBAsmart can send photos directly to the MOBAflow desktop app. To make this work, the phone and PC must be in the **same network** and **Windows Firewall** must be configured correctly.

### Network prerequisites

| Requirement | Details |
|------------|---------|
| **Same network** | Phone and Windows PC must be in the same WLAN |
| **No active VPN** | Corporate/VPN can block the connection |
| **No “AP isolation”** | Router must allow device-to-device communication |

### Configure Windows Firewall

MOBAflow needs two firewall rules:

| Service | Protocol | Port | Purpose |
|---------|----------|------|---------|
| REST API | **TCP** | 5001 | Photo upload |
| Discovery | **UDP** | 21106 | Automatic discovery |

#### Create firewall rules (PowerShell as Administrator):

```powershell
# TCP for REST API (photo upload)
New-NetFirewallRule -DisplayName "MOBAflow REST API" -Direction Inbound -Protocol TCP -LocalPort 5001 -Action Allow -Profile Private,Public

# UDP for discovery (automatic detection)
New-NetFirewallRule -DisplayName "MOBAflow Discovery" -Direction Inbound -Protocol UDP -LocalPort 21106 -Action Allow -Profile Private,Public
```

#### Alternative: Configure via Windows 11 settings

**Step 1: Open Windows Defender Firewall**
1. Press `Win + I` to open **Settings**
2. Go to **Privacy & Security → Windows Security**
3. Click **Firewall & network protection**
4. Scroll down and click **Advanced settings**
   - *(Alternatively: `Win + R`, then type `wf.msc`.)*

**Step 2: Create new inbound rule (TCP 5001)**
1. Click **Inbound Rules** on the left
2. Click **New Rule…** on the right
3. Rule type: choose **Port** → **Next**
4. Protocol: **TCP**
5. Ports: **Specific local ports** → enter `5001` → **Next**
6. Action: **Allow the connection** → **Next**
7. Profile: ☑️ **Domain**, ☑️ **Private**, ☑️ **Public** → **Next**
8. Name: `MOBAflow REST API` → **Finish**

**Step 3: Create second rule (UDP 21106)**
1. Repeat step 2, but choose:
   - Protocol: **UDP**
   - Port: `21106`
   - Name: `MOBAflow Discovery`

**Verify result:**
Afterwards you should see two new rules:
```
✅ MOBAflow REST API      (TCP 5001)
✅ MOBAflow Discovery     (UDP 21106)
```

> **💡 Tip:** Rules are effective immediately – no reboot required.

### Troubleshooting photo upload

#### Discovery does not work (phone cannot find PC)

**Causes:**
- VPN/corporate network active → **disconnect VPN**
- Router blocks multicast → **disable AP isolation**
- Wrong network profile → create firewall rules for both “Private” and “Public”

**Test:** Can the phone ping the PC’s IP?

#### Upload timeout / connection failed

**Causes:**
- Missing or wrong firewall rule → must allow **TCP** (not UDP) on port 5001
- MOBAflow not running → WinUI app must be running
- Wrong port → REST API listens on port **5001**

**Test on the PC (PowerShell):**
```powershell
# Check if port 5001 is listening
netstat -an | Select-String ":5001"

# Should show: TCP 0.0.0.0:5001 LISTENING
```

#### Phone and PC in different networks

**Symptom:** Discovery fails, manual upload does not work.

**Check:**
- PC: `ipconfig` → note IPv4 address (e.g. 192.168.1.100)
- Phone: Android Settings → Wi-Fi → IP address (e.g. 192.168.1.xxx)
- **Same network ID?** (192.168.1.x vs 192.168.1.x = OK)

**Typical problems:**
- PC via Ethernet (192.168.0.x), phone via WLAN (192.168.1.x) → **different subnets**
- PC connected to VPN → VPN has its own subnet

---

### Problem: App crashes or freezes

**Solution:**
1. **Restart the app:**
   - Open task switcher → close MOBAsmart → open again
2. **Clear cache:**
   - Android Settings → Apps → MOBAsmart → Storage → Clear cache
3. **Reinstall app:**
   - Uninstall → reinstall (settings are usually kept)

---

## 💡 Tips & Tricks

### 🎯 Recommended settings for racing

**Scenario:** 3 trains racing for 10 laps

```
✅ Tracks: 3
✅ Target: 10
✅ Timer Filter: Aktiviert
✅ Intervall: 8 Sekunden (schnelle Züge)
```

**Why?**
- 3 separate counters (one train per track)
- 10 laps → good progress visibility (10%, 20%, …)
- 8 seconds → prevents double counting on fast passes

### 🚂 Recommended settings for automatic running

**Scenario:** 1 train runs automatically in a loop

```
✅ Tracks: 1 (nur ein Gleiskontakt)
✅ Target: 50 (lange Session)
✅ Timer Filter: Aktiviert
✅ Intervall: 15 Sekunden (langsamer Zug)
```

**Why?**
- 1 counter is enough (single contact)
- 50 laps → can run for hours
- 15 seconds → robust against double counting

### 📱 Display management

**Problem:** Battery drains too fast

**Solution:**
1. Reduce **display brightness** to ~50%
2. Use **dark mode** (saves energy on OLED)
3. Connect a **charger** for long sessions

---

## 📊 Example scenario: race with 3 trains

### Setup
- **3 feedback modules** (Roco 10808) along the track
- **3 trains** (ICE, TGV, Railjet)
- **Goal:** Which train reaches 10 laps first?

### Configuration in MOBAsmart

1. **Connect to Z21**
   - Enter IP → enable toggle
2. **Settings:**
   - Tracks: **3**
   - Target: **10**
   - Timer: **enabled**, **10 seconds**
3. Press **Reset** → reset counters
4. Turn **Track Power** on
5. **Start trains** (via Z21 app or handheld controller)

### Watch the race

```
[3]  Track 1  (ICE)
     Lap: 00:15.2  @  22:30:45
     Lap 3/10 ━━━━━░░░░░░░░░░░  30%

[5]  Track 2  (TGV)
     Lap: 00:14.8  @  22:30:50
     Lap 5/10 ━━━━━━━━━━░░░░░  50%  ← Führend!

[2]  Track 3  (Railjet)
     Lap: 00:16.1  @  22:30:40
     Lap 2/10 ━━░░░░░░░░░░░░░░  20%
```

**Winner:** Track 2 (TGV) is the first to reach 10/10! 🏆

---

## 🔒 Privacy & Permissions

### Required permissions

| Permission | Reason |
|-----------|--------|
| **Internet** | UDP communication with Z21 |
| **Network state** | Check WLAN connection |

### What is **not** collected

- ❌ No personal data
- ❌ No location data
- ❌ No usage analytics
- ❌ No cloud connection

**All data stays locally on your device.**

---

## 📞 Support & Feedback

### Reporting issues

**GitHub Issues:**  
`https://dev.azure.com/ahuelsmann/MOBAflow/_git/MOBAflow`

**E-mail:**  
`andreas.huelsmann@web.de`

### Feature requests

We appreciate feedback! 🎉

Tell us:
- What is missing in the app?
- Which features would you like to see?
- What could be improved?

---

## 📜 License & Credits

**MOBAsmart** is part of the **MOBAflow** project.

- **License:** MIT License
- **Author:** Andreas Huelsmann

**Third-party software:**
- Roco Z21 digital command station (communication protocol)
- .NET MAUI (Microsoft)

See [`THIRD-PARTY-NOTICES.md`](../THIRD-PARTY-NOTICES.md) for details.

---

## 🎯 Summary

**MOBAsmart** makes lap counting effortless. 🚂

**Enjoy your running sessions!** 🎉
