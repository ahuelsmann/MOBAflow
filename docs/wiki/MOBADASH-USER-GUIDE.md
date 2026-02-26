# MOBAdash (Blazor) – User Guide

**Version:** 1.0  
**Platform:** Web (browser-based)  
**Status:** Production  
**Last Updated:** 2025-12-27

---

## 📱 What is MOBAdash?

**MOBAdash** is the web-based monitoring solution for your model railway layout. Access your Z21 from anywhere in your local network – from smartphone, tablet, or PC. No download, no installation – just open the browser and go.

---

## 🚀 Getting Started

### 1. System requirements

**Server (where MOBAdash runs):**
- PC/server in the same network as the Z21  
- .NET 10 runtime (ASP.NET Core)  
- Port `5000` (HTTP) or `5001` (HTTPS) available  

**Client (browser):**
- **Modern browser:** Chrome 90+, Firefox 88+, Edge 90+, Safari 14+  
- **JavaScript enabled**  
- **Network access** to the server  

### 2. Starting the server

#### Option 1: Visual Studio
```bash
1. Open solution (MOBAflow.sln)
2. Set WebApp as startup project
3. Press F5
4. Browser will open http://localhost:5000 automatically
```

#### Option 2: Command line
```bash
cd MOBAflow/WebApp
dotnet run
```

#### Option 3: Published build
```bash
cd MOBAflow/WebApp/bin/Release/net10.0/publish
dotnet WebApp.dll --urls "http://0.0.0.0:5000"
```

### 3. Accessing from other devices

**Find the server IP:**
```bash
# Windows
ipconfig

# Look for "IPv4 Address": e.g. 192.168.0.100
```

**Access from another device:**
```
http://192.168.0.100:5000
```

**⚠️ Important:** Windows Firewall must allow port `5000` (and/or `5001`).

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

## 🎯 Main Features

### 📊 Dashboard (home page)

**Central overview of all important information.**

#### Live monitoring
- **Z21 connection status:** 🟢 online / 🔴 offline  
- **Track power:** ⚡ ON / ⚫ OFF  
- **System stats:**
  - 🌡️ Temperature  
  - 🔋 Main current  
  - 🔌 Supply voltage  
  - ⚡ VCC voltage  

#### Lap counter dashboard
- **Real-time updates** for all feedback points  
- **Lap times** with averages  
- **Progress bars** for each track  
- **Responsive design** (mobile + desktop)  

---

### 🚂 Journeys monitor

**Track active journeys in real time.**

#### Display
```
Journey: ICE 1234 Hamburg → München
├─ Status: Active ✅
├─ Current Station: Bremen Hbf (InPort 3)
├─ Counter: 5 runs
└─ Last Update: 22:15:30
```

#### Features
- **List all journeys** (table)  
- **Highlight active journeys** (green badge)  
- **Counter statistics** (how often a journey has been completed)  
- **Station history** (last 10 stations)  

---

### ⚡ Workflows monitor

**Monitor running workflows.**

#### Display
```
Workflow: Bahnhofsansage Berlin
├─ Trigger: InPort 1
├─ Status: Waiting for feedback…
├─ Actions: 5 (Announcement, Delay, Audio, …)
└─ Last Execution: 22:10:15
```

#### Features
- **List all workflows**  
- Show **last execution time**  
- **Action preview** (expand on click)  
- **Execution log** (last 20 executions)  

---

### 📈 Statistics page

**Analyse your operating data.**

#### Available statistics

**1. Lap count statistics**
- Total number of laps per track  
- Average lap time per track  
- Fastest lap (record)  
- Slowest lap  

**2. Journey statistics**
- Most frequent journeys (Top 10)  
- Average duration per journey  
- Station distribution (which station is visited most often)  

**3. Workflow statistics**
- Executions per workflow  
- Average execution time  
- Error rate (failed executions)  

**4. Time-series charts**
- Lap count over time (line chart)  
- Main current over time (area chart)  
- Temperature over time (line chart)  

---

### ⚙️ Settings page

**Configure MOBAdash centrally.**

#### Z21 connection

| Setting | Description | Default |
|---------|-------------|---------|
| **IP Address** | Z21 IP address | `192.168.0.111` |
| **Port** | UDP port | `21105` |
| **Auto-reconnect** | Automatically reconnect | ✅ Enabled |
| **Polling Interval** | Status polling interval (seconds) | `5` |

#### Dashboard

| Setting | Description | Default |
|---------|-------------|---------|
| **Auto-refresh interval** | Page refresh interval (seconds) | `10` |
| **Show system stats** | Show system information | ✅ Enabled |
| **Dark Mode** | Dark theme | ⚙️ Auto (system) |

#### Counter

| Setting | Description | Default |
|---------|-------------|---------|
| **Count of Feedback Points** | Number of InPorts | `0` |
| **Target Lap Count** | Target number of laps | `10` |
| **Use Timer Filter** | Anti double-count filter | ✅ Enabled |
| **Timer Interval** | Filter interval (seconds) | `10.0` |

---

## 🔒 Security & Access

### 🔐 Configure HTTPS (recommended)

**Why HTTPS?**
- **Encrypted communication** (important when accessing over the internet)  
- **Modern browsers** prefer HTTPS  
- **Service workers** require HTTPS  

#### Create a self-signed certificate

```bash
# Windows PowerShell
dotnet dev-certs https --trust
```

#### Start MOBAdash with HTTPS

```bash
dotnet run --urls "https://0.0.0.0:5001"
```

**Access:**
```
https://192.168.0.100:5001
```

⚠️ **Browser warning:** Self-signed certificates cause a warning. Click “Advanced” → “Proceed anyway”.

### 🌐 Access from outside your network

**⚠️ Warning:** Do **NOT** expose MOBAdash directly to the internet without proper authentication!

**Safer options:**

#### Option 1: VPN (recommended)
```
Smartphone/Tablet
    ↓ VPN
Heimnetzwerk
    ↓
MOBAdash Server (192.168.0.100:5000)
```

**Benefits:**
- ✅ Secure (encrypted)  
- ✅ Access to the entire home network  
- ✅ No port forwarding on the router  

#### Option 2: Reverse proxy (e.g. ngrok)
```bash
# Install ngrok
ngrok http 5000

# A public URL is generated:
# https://abc123.ngrok.io → http://localhost:5000
```

**Benefits:**
- ✅ Quick to set up  
- ✅ HTTPS automatically  
- ⚠️ **Temporary URL** (changes on restart)  
- ⚠️ **Free tier** has limits  

#### Option 3: Cloudflare Tunnel
```bash
# Cloudflare Tunnel einrichten
cloudflared tunnel create mobaflow
cloudflared tunnel route dns mobaflow mobaflow.example.com
cloudflared tunnel run mobaflow
```

**Benefits:**
- ✅ Permanent URL  
- ✅ HTTPS automatically  
- ✅ DDoS protection  
- ⚠️ Cloudflare account required  

---

## 📱 Mobile optimisation

### Progressive Web App (PWA)

**MOBAdash can be installed like an app.**

#### Installation (Android/iOS)

**1. Open browser:**
```
https://192.168.0.100:5001
```

**2. Add to home screen:**
- **Android Chrome:** Menu → “Add to Home screen”  
- **iOS Safari:** Share → “Add to Home Screen”  

**3. App icon appears:**
- Open MOBAdash like a native app  
- ✅ Full screen mode  
- ✅ Fast startup  
- ✅ Limited offline support  

### Responsive design

**MOBAdash automatically adapts to the device:**

| Device | Layout |
|--------|--------|
| **Desktop** (>1200px) | 3-column layout, full detail |
| **Tablet** (768–1200px) | 2-column compact layout |
| **Smartphone** (<768px) | 1-column, touch-optimised |

---

## 🔄 Real-time updates (SignalR)

**MOBAdash uses SignalR for live updates.**

### How it works

```
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

**Benefits:**
- ✅ **Real-time:** No noticeable delay  
- ✅ **Efficient:** Only changes are sent  
- ✅ **Bidirectional:** Browser can also send commands  

### Check connection status

**Top right of the dashboard:**
- 🟢 **Green:** SignalR connected  
- 🟡 **Yellow:** Connecting…  
- 🔴 **Red:** No connection (auto‑reconnect running)  

---

## 🛠️ Troubleshooting

### Problem: “Page cannot be reached”

**Solution:**
1. **Is the server running?** Check console/Task Manager.  
2. **Port correct?** Default is `5000` (HTTP) or `5001` (HTTPS).  
3. **Firewall?** Windows Firewall must allow port `5000/5001`.  
4. **Network?** Client and server in the same WLAN?  

**Test:**
```bash
# On server PC (test localhost)
http://localhost:5000

# From another device (test IP)
http://192.168.0.100:5000
```

### Problem: No live updates

**Solution:**
1. **SignalR connection status:** Is it 🟢 green?  
2. **Browser supports WebSockets?** (All modern browsers do.)  
3. **Proxy/VPN active?** Some block WebSockets.  
4. **Hard reload:** Press `Ctrl + F5`.  

### Problem: “SSL/TLS error” when using HTTPS

**Solution:**
1. **Self‑signed certificate:** Accept the browser warning.  
2. Or use a **real certificate** (e.g. Let’s Encrypt).  
3. Or use **HTTP** (for local‑network only!).  

### Problem: High CPU usage

**Solution:**
1. **Increase polling interval:** Settings → Z21 → Polling: e.g. 10s.  
2. **Reduce auto‑refresh:** Settings → Dashboard → Auto‑refresh: e.g. 30s.  
3. **Fewer feedback points:** Settings → Counter → configure only what you need.  

---

## 💡 Tips & Tricks

### 🎨 Dark mode

**Automatic (based on system setting):**
```
Settings → Dashboard → Dark Mode: Auto
```

**Switch manually:**
```
Settings → Dashboard → Dark Mode: Light/Dark
```

### 📊 Exporting charts

**Right‑click a chart → “Save image as…”**

Formats:
- PNG (best quality)  
- SVG (vector graphic)  
- CSV (raw data)  

### 🔔 Browser notifications

**Enable notifications for important events:**

```javascript
Settings → Notifications:
✅ Track power changed
✅ Journey completed
✅ Workflow execution failed
❌ Feedback received (too noisy!)
```

**Note:** The browser must allow notifications.

### 📱 Kiosk mode (always‑on dashboard)

**Use an old tablet as a permanent dashboard:**

1. Keep the **tablet connected to power**.  
2. Install a **browser kiosk app** (e.g. “Fully Kiosk Browser”).  
3. Configure the **MOBAdash URL**.  
4. Enable **auto‑start on boot**.  
5. Disable **display timeout**.  

**Result:** A permanent dashboard next to your layout. 🖥️

---

## 🌐 Multi‑user access

**Multiple people can access MOBAdash at the same time:**

```
PC 1 (Desktop): http://192.168.0.100:5000
PC 2 (Laptop): http://192.168.0.100:5000
Tablet: http://192.168.0.100:5000
Smartphone: http://192.168.0.100:5000
```

**All users see the same live data.**

**⚠️ Caution:** 
- Only **one client** should control track power to avoid conflicts.  
- Workflows/journeys can be controlled by **any** client (first‑come‑first‑serve).  

---

## 📈 Performance optimisation

### Use the browser cache

**MOBAdash loads static resources only once:**

```
Erster Besuch: 5 MB Download
Zweiter Besuch: 50 KB Download (nur Updates)
```

**Clear cache (if issues occur):**
```
Ctrl + Shift + Delete → Cache leeren → Reload
```

### Service worker (offline capabilities)

**MOBAdash can partially work offline:**

**Works offline:**
- ✅ UI structure (pages load)  
- ✅ Static content (CSS, images)  

**Does NOT work offline:**
- ❌ Z21 connection (needs local network)  
- ❌ Live updates (SignalR requires connection)  
- ❌ API calls (saving settings, etc.)  

---

## 📜 License & Credits

**MOBAdash** is part of the **MOBAflow** project (MIT license).

- **Author:** Andreas Huelsmann  
- **Framework:** Blazor Server (.NET 10)  
- **UI library:** MudBlazor 7.0  
- **Charting:** Plotly.js  
- **Real-time:** SignalR  

See [`THIRD-PARTY-NOTICES.md`](../THIRD-PARTY-NOTICES.md) for details.

---

**Enjoy using MOBAdash!** 🚂📊✨
