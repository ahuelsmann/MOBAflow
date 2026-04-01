# MOBAdash – Historical Guide

**Platform:** Historical reference
**Status:** Archived – not buildable from the current repository
**Last Updated:** 2026-03-31

---

## What is MOBAdash?

**MOBAdash** referred to an earlier web-based monitoring experiment.
The current repository does **not** contain a `WebApp`/Blazor host
anymore.

Use one of the current entry points instead:

* `MOBAflow/MOBAflow.csproj` for the Windows desktop app
* `MOBApi/MOBApi.csproj` for the REST/SignalR backend
* `MOBAsmart/MOBAsmart.csproj` for the Android app

---

## Getting Started

### 1. System requirements

#### Server (where MOBAdash runs)

* PC/server in the same network as the Z21
* .NET 10 runtime (ASP.NET Core)
* Port `5000` (HTTP) or `5001` (HTTPS) available

#### Client (browser)

* Modern browser: Chrome 90+, Firefox 88+, Edge 90+, Safari 14+
* JavaScript enabled
* Network access to the server

### 2. Current repository status

This document is retained for historical context only.

There is no `WebApp` or `WebApp.csproj` in the current repository.

For current setup and run instructions, use these documents instead:

* [INSTALLATION.md](INSTALLATION.md)
* [MOBAFLOW-USER-GUIDE.md](MOBAFLOW-USER-GUIDE.md)
* [MOBASMART-USER-GUIDE.md](MOBASMART-USER-GUIDE.md)

### 3. Accessing from other devices

#### Find the server IP

```bash
# Windows
ipconfig
```

#### Access from another device

```text
http://192.168.0.100:5000
```

### 4. Create a firewall rule

```powershell
# PowerShell (as Administrator)
New-NetFirewallRule -DisplayName "MOBAdash" `
  -Direction Inbound `
  -Protocol TCP `
  -LocalPort 5000 `
  -Action Allow
```

---

## Main Features

### Dashboard (home page)

Central overview of all important information.

#### Live monitoring

* Z21 connection status: online / offline
* Track power: ON / OFF
* System stats:
  * Temperature
  * Main current
  * Supply voltage
  * VCC voltage

#### Lap counter dashboard

* Real-time updates for all feedback points
* Lap times with averages
* Progress bars for each track
* Responsive design (mobile + desktop)

---

### Journeys monitor

Track active journeys in real time.

#### Journeys display

```text
Journey: ICE 1234 Hamburg → München
├─ Status: Active
├─ Current Station: Bremen Hbf (InPort 3)
├─ Counter: 5 runs
└─ Last Update: 22:15:30
```

#### Journeys features

* List all journeys (table)
* Highlight active journeys (green badge)
* Counter statistics (how often a journey has been completed)
* Station history (last 10 stations)

---

### Workflows monitor

Monitor running workflows.

#### Workflows display

```text
Workflow: Bahnhofsansage Berlin
├─ Trigger: InPort 1
├─ Status: Waiting for feedback…
├─ Actions: 5 (Announcement, Delay, Audio, …)
└─ Last Execution: 22:10:15
```

#### Workflows features

* List all workflows
* Show last execution time
* Action preview (expand on click)
* Execution log (last 20 executions)

---

### Statistics page

Analyse your operating data.

#### Available statistics

##### 1. Lap count statistics

* Total number of laps per track
* Average lap time per track
* Fastest lap (record)
* Slowest lap

##### 2. Journey statistics

* Most frequent journeys (Top 10)
* Average duration per journey
* Station distribution (which station is visited most often)

##### 3. Workflow statistics

* Executions per workflow
* Average execution time
* Error rate (failed executions)

##### 4. Time-series charts

* Lap count over time (line chart)
* Main current over time (area chart)
* Temperature over time (line chart)

---

### Settings page

Configure MOBAdash centrally.

#### Z21 connection

* IP Address: Z21 IP address. Default: `192.168.0.111`
* Port: UDP port. Default: `21105`
* Auto-reconnect: Automatically reconnect. Default: Enabled
* Polling Interval: Status polling interval (seconds). Default: `5`

#### Dashboard

* Auto-refresh interval: Page refresh interval (seconds). Default: `10`
* Show system stats: Show system information. Default: Enabled
* Dark Mode: Dark theme. Default: Auto (system)

#### Counter

* Count of Feedback Points: Number of InPorts. Default: `0`
* Target Lap Count: Target number of laps. Default: `10`
* Use Timer Filter: Anti double-count filter. Default: Enabled
* Timer Interval: Filter interval (seconds). Default: `10.0`

---

## Security & Access

### Configure HTTPS (recommended)

#### Why HTTPS?

* Encrypted communication (important when accessing over the internet)
* Modern browsers prefer HTTPS
* Service workers require HTTPS

#### Create a self-signed certificate

```bash
dotnet dev-certs https --trust
```

#### Start MOBAdash with HTTPS

```bash
dotnet run --urls "https://0.0.0.0:5001"
```

#### Access

```text
https://192.168.0.100:5001
```

#### Warning

Do NOT expose MOBAdash directly to the internet without proper authentication!

#### Safer options

#### Option 1: VPN (recommended)

```text
Smartphone/Tablet
    ↓ VPN
Heimnetzwerk
    ↓
MOBAdash Server (192.168.0.100:5000)
```

#### VPN benefits

* Secure (encrypted)
* Access to the entire home network
* No port forwarding on the router

#### Option 2: Reverse proxy (e.g. ngrok)

```bash
# Install ngrok
ngrok http 5000

# A public URL is generated:
# https://abc123.ngrok.io → http://localhost:5000
```

#### Reverse proxy benefits

* Quick to set up
* HTTPS automatically
* Temporary URL (changes on restart)
* Free tier has limits

#### Option 3: Cloudflare Tunnel

```bash
# Cloudflare Tunnel einrichten
cloudflared tunnel create mobaflow
cloudflared tunnel route dns mobaflow mobaflow.example.com
cloudflared tunnel run mobaflow
```

#### Cloudflare Tunnel benefits

* Permanent URL
* HTTPS automatically
* DDoS protection
* Cloudflare account required

---

## Mobile optimisation

### Progressive Web App (PWA)

**MOBAdash can be installed like an app.**

#### Installation (Android/iOS)

##### Open browser

```text
https://192.168.0.100:5001
```

##### Add to home screen

* **Android Chrome:** Menu → “Add to Home screen”
* **iOS Safari:** Share → “Add to Home Screen”

##### App icon appears

* Open MOBAdash like a native app
* Full screen mode
* Fast startup
* Limited offline support

### Responsive design

**MOBAdash automatically adapts to the device:**

* **Desktop** (>1200px): 3-column layout, full detail
* **Tablet** (768–1200px): 2-column compact layout
* **Smartphone** (<768px): 1-column, touch-optimised

---

## Real-time updates (SignalR)

**MOBAdash uses SignalR for live updates.**

### How it works

```text
Z21 sendet Feedback
    ↓
Backend receives (UDP)
    ↓
SignalR Hub pushed Update
    ↓
Browser receives (WebSocket)
    ↓
UI updates automatically
```

#### SignalR benefits

* **Real-time:** No noticeable delay
* **Efficient:** Only changes are sent
* **Bidirectional:** Browser can also send commands

### Check connection status

#### Top right of the dashboard

* **Green:** SignalR connected
* **Yellow:** Connecting…
* **Red:** No connection (auto‑reconnect running)

---

## Troubleshooting

### Problem: “Page cannot be reached”

#### No-page-reach solution

1. **Is the server running?** Check console/Task Manager.
2. **Port correct?** Default is `5000` (HTTP) or `5001` (HTTPS).
3. **Firewall?** Windows Firewall must allow port `5000/5001`.
4. **Network?** Client and server in the same WLAN?

#### Test

```bash
# On server PC (test localhost)
http://localhost:5000

# From another device (test IP)
http://192.168.0.100:5000
```

### Problem: No live updates

#### No-live-updates solution

1. **SignalR connection status:** Is it green?
2. **Browser supports WebSockets?** (All modern browsers do.)
3. **Proxy/VPN active?** Some block WebSockets.
4. **Hard reload:** Press `Ctrl + F5`.

### Problem: “SSL/TLS error” when using HTTPS

#### SSL/TLS solution

1. **Self‑signed certificate:** Accept the browser warning.
2. Or use a **real certificate** (e.g. Let’s Encrypt).
3. Or use **HTTP** (for local‑network only!).

### Problem: High CPU usage

#### High-CPU solution

1. **Increase polling interval:** Settings → Z21 → Polling: e.g. 10s.
2. **Reduce auto‑refresh:** Settings → Dashboard → Auto‑refresh: e.g. 30s.
3. **Fewer feedback points:** Settings → Counter → configure only what you need.

---

## Tips & Tricks

### Dark mode

**Automatic (based on system setting):**

```text
Settings → Dashboard → Dark Mode: Auto
```

**Switch manually:**

```text
Settings → Dashboard → Dark Mode: Light/Dark
```

### Exporting charts

Right‑click a chart → “Save image as…”.

#### Formats

* PNG (best quality)
* SVG (vector graphic)
* CSV (raw data)

### Browser notifications

**Enable notifications for important events:**

```text
Settings → Notifications:
Track power changed
Journey completed
Workflow execution failed
Feedback received (too noisy!)
```

**Note:** The browser must allow notifications.

### Kiosk mode (always‑on dashboard)

**Use an old tablet as a permanent dashboard:**

1. Keep the **tablet connected to power**.
2. Install a **browser kiosk app** (e.g. “Fully Kiosk Browser”).
3. Configure the **MOBAdash URL**.
4. Enable **auto‑start on boot**.
5. Disable **display timeout**.

**Result:** A permanent dashboard next to your layout.

---

## Multi‑user access

**Multiple people can access MOBAdash at the same time:**

```text
PC 1 (Desktop): http://192.168.0.100:5000
PC 2 (Laptop): http://192.168.0.100:5000
Tablet: http://192.168.0.100:5000
Smartphone: http://192.168.0.100:5000
```

**All users see the same live data.**

### Caution

* Only **one client** should control track power to avoid conflicts.
* Workflows/journeys can be controlled by **any** client (first‑come‑first‑serve).

---

## Performance optimisation

### Use the browser cache

**MOBAdash loads static resources only once:**

```text
Erster Besuch: 5 MB Download
Zweiter Besuch: 50 KB Download (nur Updates)
```

#### Clear cache (if issues occur)

```text
Ctrl + Shift + Delete → Cache leeren → Reload
```

### Service worker (offline capabilities)

**MOBAdash can partially work offline:**

#### Works offline

* UI structure (pages load)
* Static content (CSS, images)

#### Does NOT work offline

* Z21 connection (needs local network)
* Live updates (SignalR requires connection)
* API calls (saving settings, etc.)

---

## License & Credits

**MOBAdash** is part of the **MOBAflow** project (MIT license).

* **Author:** Andreas Huelsmann
* **Framework:** Blazor Server (.NET 10)
* **UI library:** MudBlazor 7.0
* **Charting:** Plotly.js
* **Real-time:** SignalR

See [`THIRD-PARTY-NOTICES.md`](../THIRD-PARTY-NOTICES.md) for details.

---

**Enjoy using MOBAdash!**
