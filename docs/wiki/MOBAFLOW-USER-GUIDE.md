# MOBAflow (WinUI) – User Guide

**Version:** 1.0  
**Platform:** Windows 10/11 (Desktop)  
**Status:** Production  
**Last Updated:** 2025-12-29

---

## 📱 What is MOBAflow?

**MOBAflow** is the Windows desktop application for advanced control
and automation of your model railway layout. It connects directly via
UDP to your **Roco Z21 digital command station** and provides features
such as journey management, workflow automation, and a visual track
plan editor.

---

## 🚀 Getting Started

### 1. System requirements

- **Operating system:** Windows 10 (version 1809+) or Windows 11  
- **Runtime:** .NET 10 Desktop Runtime  
- **Hardware:**  
  - Roco Z21 digital command station  
  - Feedback modules (Roco 10808, 10787, etc.)  
  - WLAN router (Z21 and PC in the same network)

### 2. Installation

1. **Read the installation guide:** Follow the step-by-step
   instructions in [`INSTALLATION.md`](INSTALLATION.md).  
2. **Build the project:** Build MOBAflow with .NET 10 as described
   in the installation guide.  
3. **Start the app:** Launch the desktop application as described
   there (for example via `dotnet run --project
   MOBAflow/MOBAflow.csproj`).

### 3. First launch

1. **Welcome screen:** Overview of the main features.  
2. **Connect to Z21:** Enter the IP address and click
   **Connect**.  
3. **Create a solution:** Create a new solution or open an
   existing one.

---

## 🎯 Main Features

### 📊 Overview page

**The central control dashboard for your layout.**

#### Features

- **Z21 connection status:** Green = connected, Red = disconnected  
- **Track power control:** Turn track power on/off  
- **System stats:**  
  - 🌡️ Z21 temperature  
  - 🔌 Supply voltage  
  - ⚡ VCC voltage  
  - 🔋 Main current

#### Lap counter

- **Real-time monitoring** of all feedback points  
- **Lap times** with basic statistics  
- **Progress bars** per track  
- **Export function** (CSV, JSON – where supported)

---

### 🚂 Journeys

**Define complex train runs with multiple stations.**

#### What is a journey?

A **journey** is a predefined route with multiple stations. At each
station, actions can be executed automatically (announcements,
commands, sounds).

#### Creating a journey

1. Open the **Journeys** page (left navigation).  
2. Click **Add Journey**.  
3. Configure the **properties**:  
   - **Name:** e.g. `ICE Berlin → Munich`  
   - **InPort:** Feedback point used to detect the train (e.g. InPort 5)  
   - **Train:** Select your train from the list  

4. **Add stations:**  
   - Click **Add Station**.  
   - **Name:** e.g. `Berlin Hbf`  
   - **InPort:** Feedback point of the station (e.g. InPort 1)  
   - **Workflow:** Action to execute on arrival (optional)  

#### Example journey

```text
Journey: "ICE 1234 Hamburg → Frankfurt"
├─ Station 1: "Hamburg Hbf" (InPort 1)
│  └─ Workflow: Announcement "The train is departing"
├─ Station 2: "Bremen Hbf" (InPort 3)
│  └─ Workflow: Announcement "Next stop: Hanover"
└─ Station 3: "Frankfurt Hbf" (InPort 5)
   └─ Workflow: Announcement "Final destination reached"
```

#### Starting a journey

1. Select the **journey** in the list.  
2. Click **Start Journey**.  
3. Run the train – on each feedback event the matching station is detected.  
4. The **counter** increases to show how many times the journey has been completed.

---

### ⚡ Workflows

**Automate actions using event‑driven workflows.**

#### What is a workflow?

A **workflow** is a sequence of actions that is executed automatically
when a trigger fires (for example a feedback event, a timer, or a
button click).

#### Creating a workflow

1. Open the **Workflows** page.  
2. Click **Add Workflow**.  
3. Configure the **properties**:  
   - **Name:** e.g. `Station announcement Berlin`  
   - **InPort:** Trigger feedback point (e.g. InPort 1)  
   - **Execution mode:** Sequential (one after another) or Parallel
     (staggered/overlapping)  
   - **Actions:** List of actions to execute  

#### Execution modes

- **Sequential:** Actions run one after another. `DelayAfterMs`
  means a pause **after** the action finishes before the next one starts.
- **Parallel:** Actions start with offsets (overlap in time).
  `DelayAfterMs` means the start offset relative to the previous action.

**Sequential example:**

```text
Action 1: Play "gong.wav" → waits until finished → 1000ms pause → Action 2 starts
Action 2: Announcement → waits until finished → Action 3 starts
```

**Parallel example (staggered start):**

```text
t=0ms:    Action 1: Gong (DelayAfterMs=0)           → starts immediately
t=500ms:  Action 2: Announcement (DelayAfterMs=500)
           → starts after 500ms (gong still playing)
t=2500ms: Action 3: Lights (DelayAfterMs=2000)      → starts 2s after previous action
```

#### Available action types

- **Announcement:** Text-to-speech station announcement. Parameters:
  text, voice, rate, volume
- **Command:** Send Z21 command. Parameters: command bytes
- **Audio:** Play a WAV file. Parameters: file path

**All actions support:**

- **`DelayAfterMs`:** Time delay whose meaning depends on the execution mode

#### Example workflow (sequential)

```yaml
Workflow: "Bahnhofsansage Berlin Hbf"
Trigger: InPort 1
Execution Mode: Sequential

Actions:
1. Audio: `"gong.wav"` (DelayAfterMs: 1000)
   → gong + 1s pause afterwards  
2. Announcement: `"ICE 1234 is arriving"`
   → first announcement  
3. Announcement: `"Please stand back from the edge"`
   → second announcement  
```

#### Example workflow (parallel)

```yaml
Workflow: "Bahnhof mit Effekten"
Trigger: InPort 1
Execution Mode: Parallel

Actions:
1. Audio: `"gong.wav"` (DelayAfterMs: 0)               → t=0ms: gong starts  
2. Announcement: `"Train is arriving"` (DelayAfterMs: 500)
   → t=500ms: announcement starts (gong still playing)  
3. Command: lights on (DelayAfterMs: 2000)           → t=2500ms: lights switch  
```

---

### 🎨 Track plan editor

**Visualise and edit your track plan.**

#### Track-plan features

- **AnyRail import:** Import track plans from AnyRail XML  
- **Drag & drop:** Place track pieces on the canvas  
- **Feedback points:** Assign InPorts to track segments  
- **Zoom & pan:** Navigate using mouse/touchpad  

#### AnyRail import

1. Open **AnyRail** and create your layout.  
2. **Export:** `File → Export → XML`.  
3. In **MOBAflow:** open the track plan page and use **Import** to
   select the XML file.  
4. The track plan is converted automatically.

#### Manual editing

1. **Track library** (left): Available tracks (e.g. Piko A‑Gleis).  
2. **Canvas** (center): Working area.  
3. **Properties** (right): Properties of the selected track item.  

**Placing tracks:**

- Drag & drop from the library.  
- Double‑click a track to rotate it.  
- Right‑click to delete.

---

### Display page

**Design and test LED matrix output for remote display scenarios.**

#### 5x5 matrix editor

The Display page contains a color palette and a 5x5 LED matrix preview.

- **Choose a color:** Use the color picker on the left.
- **Paint a cell:** Left-click or tap a matrix cell.
- **Clear a cell:** Right-click a matrix cell to switch it back to the off state.
- **Off state:** Cleared cells use the default light-gray off color.

#### Interaction model

The page uses ViewModel commands for painting and clearing cells. This keeps the interaction consistent with the MOBAflow MVVM pattern and makes the matrix state testable outside the WinUI runtime.

---

### Solution management

**Organise your layout into projects and solutions.**

#### What is a solution?

A **solution** is a file (`.mobaflow.json`) that contains all your data:

- Journeys  
- Workflows  
- Track plans  
- Trains and locomotives  
- Feedback points  

#### Creating a new solution

1. Go to **File → New Solution**.  
2. **Name:** e.g. `My Layout 2025`.  
3. **Location:** Choose a folder.  
4. Click **Save**.  

#### Opening an existing solution

1. Go to **File → Open Solution**.  
2. Select your `.mobaflow.json` file.  
3. All data is loaded into the app.  

#### Auto‑load on startup

1. Go to **Settings → Auto-load last solution**.  
2. Enable the option.  
3. On the next start, the last solution will be loaded automatically.

---

## 🎙️ Text‑to‑Speech (Azure Cognitive Services)

**Professional announcements using Azure Speech.**

### Setup

1. **Azure account:** Create a free Azure account.  
2. **Speech service:** Create a Speech resource.  
3. **Copy API key:** Note down key and region.  

### Configure in MOBAflow

1. Open **Settings → Speech**.  
2. Paste your **API key**.  
3. Set the **region**, e.g. `germanywestcentral`.  
4. Choose a **voice**, e.g. `de-DE-ConradNeural` (male) or
   `de-DE-KatjaNeural` (female).  

### Test

1. Go to **Workflows → Add Workflow → Add Announcement Action**.  
2. Enter some test text, for example `"This is a test"`.  
3. Click **Play** – the announcement should be spoken.  

### Free quota

- **5 million characters/month** for free (Azure free tier).  
- More than enough for private model railway usage.

---

## 🔧 Settings page

### General

- **Auto-load last solution:** Load the last solution file on startup.
  Default: ✅ Enabled
- **Reset window layout on start:** Reset window size/position on
  startup. Default: ❌ Disabled

### Z21

- **Current IP Address:** Z21 IP address. Default: `192.168.0.111`
- **Default Port:** UDP port. Default: `21105`
- **Auto-connect retry interval:** Reconnect interval (seconds).
  Default: `10`
- **System state polling interval:** Status polling interval
  (seconds). Default: `5`

### Speech

- **API Key:** Azure Speech API key. Default: (empty)
- **Region:** Azure region. Default: `germanywestcentral`
- **Voice:** Default voice. Default: `de-DE-ConradNeural`
- **Rate:** Speaking rate (`-10` to `+10`). Default: `-1`
- **Volume:** Volume (0–100). Default: `90`

### Counter

- **Count of Feedback Points:** Number of InPorts. Default: `0`
- **Target Lap Count:** Target number of laps. Default: `10`
- **Use Timer Filter:** Anti-double-count filter. Default: ✅ Enabled
- **Timer Interval:** Filter interval (seconds). Default: `10.0`

---

## 🛠️ Troubleshooting

### Problem: Z21 does not connect

#### Z21-connection solution

1. **Check IP address:** Does it match your Z21 IP?  
2. **Firewall:** Does Windows Firewall allow MOBAflow on UDP port `21105`?  
3. **Network:** Are PC and Z21 in the same LAN/WLAN?  
4. **Restart Z21:** Power cycle the Z21 and wait 10 seconds before
   reconnecting.  

### Problem: Azure Speech does not work

#### Azure-Speech solution

1. **API key correct?** Verify in the Azure Portal.  
2. **Region correct?** Must match the key’s region.  
3. **Internet connection?** Azure Speech requires internet access.  
4. **Quota exceeded?** Check your Azure usage and quotas.  

### Problem: Journeys are not counted

#### Journey-counting solution

1. **InPort correct?** `Journey.InPort` must match the feedback point.  
2. **Feedback received?** Check on the overview page (lap counter).  
3. **Journey started?** Make sure you clicked **Start Journey**.  

### Problem: Workflow is not executed

#### Workflow-execution solution

1. **InPort correct?** `Workflow.InPort` must be the trigger feedback point.  
2. **Any actions defined?** At least one action is required.  
3. **Error in action?** Check the log output (View → Logs).  

---

## 💡 Tips & Tricks

### 🚂 Best practice: journey structure

**Good journey:**

```text
Journey: "ICE Hamburg → München"
InPort: 10 (Lok-Decoder Feedback)
Stations:
  1. Hamburg (InPort 1)
  2. Bremen (InPort 3)
  3. Hannover (InPort 5)
  4. Frankfurt (InPort 7)
  5. München (InPort 9)
```

**Poor journey:**

```text
Journey: "Alle Züge"
InPort: 0 (kein spezifischer Zug)
Stations:
  1. Irgendwo (InPort 1)
```

### ⚡ Performance optimisation

**Problem:** The app becomes slow with many feedback events.

#### Performance-optimisation solution

1. **Increase polling interval:** Settings → Z21 → Polling interval: e.g. 10s.  
2. **Reduce active workflows:** Disable unused workflows.  
3. **Lower log level:** Settings → Logging → level `Warning`.  

### 🎨 Track plan import

**Tip:** AnyRail track plans are often more precise than drawing everything manually.

**Typical workflow:**

1. **AnyRail:** Design your exact layout with measurements.  
2. **Export XML:** Export with full geometry information.  
3. **MOBAflow import:** Import the XML and let MOBAflow convert it.  
4. **Assign feedback points:** Link InPorts to track segments.  

---

## 📋 Keyboard shortcuts

- **Ctrl + N:** New solution
- **Ctrl + O:** Open solution
- **Ctrl + S:** Save solution
- **Ctrl + Q:** Quit app
- **F1:** Open help
- **F5:** Refresh Z21 connection
- **Ctrl + T:** Toggle track power

---

## 📜 License & Credits

**MOBAflow** is open source (MIT license).

- **Author:** Andreas Huelsmann  
- **Repository:** [Azure DevOps](https://dev.azure.com/ahuelsmann/MOBAflow)  
- **App version:** 3.9 (December 2025)  

**Third‑party components:**

- Roco Z21 protocol  
- Azure Cognitive Services (Speech)  
- AnyRail (import format)  
- Microsoft WinUI 3 (UI framework)  

See [`THIRD-PARTY-NOTICES.md`](../THIRD-PARTY-NOTICES.md) for full details.

---

**Enjoy using MOBAflow!** 🚂✨
