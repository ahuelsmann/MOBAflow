# MOBAdash (Blazor) - Benutzerhandbuch

**Version:** 1.0  
**Plattform:** Web (Browser-basiert)  
**Letzte Aktualisierung:** 27.12.2025

---

## 📱 Was ist MOBAdash?

**MOBAdash** ist die webbasierte Monitoring-Lösung für deine Modellbahnanlage. Greife von überall auf deine Z21 zu – egal ob vom Smartphone, Tablet oder PC. Kein Download, keine Installation – einfach Browser öffnen und loslegen!

---

## 🚀 Erste Schritte

### 1. Systemvoraussetzungen

**Server (wo MOBAdash läuft):**
- PC/Server im gleichen Netzwerk wie die Z21
- .NET 10 Runtime (ASP.NET Core)
- Port 5000 (HTTP) oder 5001 (HTTPS) verfügbar

**Client (Browser):**
- **Moderne Browser:** Chrome 90+, Firefox 88+, Edge 90+, Safari 14+
- **JavaScript aktiviert**
- **Netzwerk-Zugriff** zum Server

### 2. Server starten

#### Option 1: Visual Studio
```bash
1. Solution öffnen (MOBAflow.sln)
2. WebApp als Startprojekt setzen
3. F5 drücken
4. Browser öffnet automatisch http://localhost:5000
```

#### Option 2: Kommandozeile
```bash
cd MOBAflow/WebApp
dotnet run
```

#### Option 3: Published Version
```bash
cd MOBAflow/WebApp/bin/Release/net10.0/publish
dotnet WebApp.dll --urls "http://0.0.0.0:5000"
```

### 3. Zugriff von anderen Geräten

**Server-IP herausfinden:**
```bash
# Windows
ipconfig

# Suche nach "IPv4-Adresse": z.B. 192.168.0.100
```

**Von anderem Gerät zugreifen:**
```
http://192.168.0.100:5000
```

**⚠️ Wichtig:** Windows Firewall muss Port 5000 erlauben!

### 4. Firewall-Regel erstellen

```powershell
# PowerShell als Admin
New-NetFirewallRule -DisplayName "MOBAdash" `
  -Direction Inbound `
  -Protocol TCP `
  -LocalPort 5000 `
  -Action Allow
```

---

## 🎯 Hauptfunktionen

### 📊 Dashboard (Home Page)

**Zentrale Übersicht aller wichtigen Informationen.**

#### Live-Monitoring:
- **Z21 Connection Status:** 🟢 Online / 🔴 Offline
- **Track Power:** ⚡ ON / ⚫ OFF
- **System Stats:**
  - 🌡️ Temperatur
  - 🔋 Main Current (Stromaufnahme)
  - 🔌 Supply Voltage
  - ⚡ VCC Voltage

#### Lap Counter Dashboard:
- **Echtzeit-Updates** aller Feedback Points
- **Rundenzeiten** mit Durchschnitt
- **Fortschrittsbalken** pro Track
- **Responsive Design** (Mobile + Desktop)

---

### 🚂 Journeys Monitor

**Verfolge aktive Zugfahrten in Echtzeit.**

#### Anzeige:
```
Journey: ICE 1234 Hamburg → München
├─ Status: Aktiv ✅
├─ Current Station: Bremen Hbf (InPort 3)
├─ Counter: 5 Durchläufe
└─ Last Update: 22:15:30
```

#### Funktionen:
- **Alle Journeys auflisten** (Tabelle)
- **Aktive Journeys hervorheben** (grüne Badge)
- **Counter-Statistik** (Wie oft wurde Journey durchlaufen?)
- **Station History** (Letzte 10 Stationen)

---

### ⚡ Workflows Monitor

**Überwache laufende Workflows.**

#### Anzeige:
```
Workflow: Bahnhofsansage Berlin
├─ Trigger: InPort 1
├─ Status: Waiting for Feedback...
├─ Actions: 5 (Announcement, Delay, Audio, ...)
└─ Last Execution: 22:10:15
```

#### Funktionen:
- **Alle Workflows auflisten**
- **Letzte Ausführungszeit** anzeigen
- **Actions-Vorschau** (Klick zum Aufklappen)
- **Execution Log** (letzte 20 Ausführungen)

---

### 📈 Statistics Page

**Analysiere deine Fahrbetrieb-Daten.**

#### Verfügbare Statistiken:

**1. Lap Count Statistics**
- **Gesamt-Runden** pro Track
- **Durchschnittliche Rundenzeit** pro Track
- **Schnellste Runde** (Rekord)
- **Langsamste Runde**

**2. Journey Statistics**
- **Häufigste Journeys** (Top 10)
- **Durchschnittliche Durchlaufzeit** pro Journey
- **Stations-Verteilung** (Welche Station wird am häufigsten angefahren?)

**3. Workflow Statistics**
- **Ausführungen pro Workflow**
- **Durchschnittliche Ausführungszeit**
- **Fehlerrate** (gescheiterte Ausführungen)

**4. Zeitreihen-Diagramme**
- **Lap Count über Zeit** (Line Chart)
- **Stromaufnahme über Zeit** (Area Chart)
- **Temperatur über Zeit** (Line Chart)

---

### ⚙️ Settings Page

**Konfiguriere MOBAdash zentral.**

#### Z21 Connection:

| Einstellung | Beschreibung | Default |
|-------------|--------------|---------|
| **IP Address** | Z21 IP-Adresse | 192.168.0.111 |
| **Port** | UDP-Port | 21105 |
| **Auto-reconnect** | Automatische Wiederverbindung | ✅ Aktiviert |
| **Polling Interval** | Status-Abfrage (Sekunden) | 5 |

#### Dashboard:

| Einstellung | Beschreibung | Default |
|-------------|--------------|---------|
| **Auto-refresh interval** | Seite neu laden (Sekunden) | 10 |
| **Show system stats** | System-Infos anzeigen | ✅ Aktiviert |
| **Dark Mode** | Dunkles Theme | ⚙️ Auto (System) |

#### Counter:

| Einstellung | Beschreibung | Default |
|-------------|--------------|---------|
| **Count of Feedback Points** | Anzahl InPorts | 0 |
| **Target Lap Count** | Ziel-Rundenzahl | 10 |
| **Use Timer Filter** | Anti-Doppelzählung | ✅ Aktiviert |
| **Timer Interval** | Filter-Intervall (Sekunden) | 10.0 |

---

## 🔒 Sicherheit & Zugriff

### 🔐 HTTPS einrichten (empfohlen)

**Warum HTTPS?**
- **Verschlüsselte Kommunikation** (wichtig bei Zugriff übers Internet)
- **Moderne Browser** bevorzugen HTTPS
- **Service Workers** erfordern HTTPS

#### Selbstsigniertes Zertifikat erstellen:

```bash
# Windows PowerShell
dotnet dev-certs https --trust
```

#### MOBAdash mit HTTPS starten:

```bash
dotnet run --urls "https://0.0.0.0:5001"
```

**Zugriff:**
```
https://192.168.0.100:5001
```

⚠️ **Browser-Warnung:** Selbstsignierte Zertifikate erzeugen eine Warnung. Klicke "Erweitert" → "Trotzdem fortfahren".

### 🌐 Zugriff von außerhalb des Netzwerks

**⚠️ Vorsicht:** Öffne MOBAdash **NICHT** direkt ins Internet ohne Authentifizierung!

**Sichere Optionen:**

#### Option 1: VPN (EMPFOHLEN)
```
Smartphone/Tablet
    ↓ VPN
Heimnetzwerk
    ↓
MOBAdash Server (192.168.0.100:5000)
```

**Vorteile:**
- ✅ Sicher (verschlüsselt)
- ✅ Zugriff auf gesamtes Heimnetzwerk
- ✅ Keine Port-Freigabe am Router

#### Option 2: Reverse Proxy (z.B. ngrok)
```bash
# ngrok installieren
ngrok http 5000

# Öffentliche URL wird generiert
# https://abc123.ngrok.io → http://localhost:5000
```

**Vorteile:**
- ✅ Schnell eingerichtet
- ✅ HTTPS automatisch
- ⚠️ **Temporäre URL** (ändert sich bei Neustart)
- ⚠️ **Kostenlose Version** hat Limits

#### Option 3: Cloudflare Tunnel
```bash
# Cloudflare Tunnel einrichten
cloudflared tunnel create mobaflow
cloudflared tunnel route dns mobaflow mobaflow.example.com
cloudflared tunnel run mobaflow
```

**Vorteile:**
- ✅ Permanente URL
- ✅ HTTPS automatisch
- ✅ DDoS-Schutz
- ⚠️ Cloudflare Account erforderlich

---

## 📱 Mobile Optimierung

### Progressive Web App (PWA)

**MOBAdash kann als App installiert werden!**

#### Installation (Android/iOS):

**1. Browser öffnen:**
```
https://192.168.0.100:5001
```

**2. "Zum Startbildschirm hinzufügen":**
- **Android Chrome:** Menü → "Zum Startbildschirm hinzufügen"
- **iOS Safari:** Teilen → "Zum Home-Bildschirm"

**3. App-Icon erscheint:**
- Öffne MOBAdash wie eine native App
- ✅ Vollbild-Modus
- ✅ Schneller Start
- ✅ Offline-Funktionalität (begrenzt)

### Responsive Design

**MOBAdash passt sich automatisch an:**

| Gerät | Layout |
|-------|--------|
| **Desktop** (>1200px) | 3-Spalten-Layout, alle Details |
| **Tablet** (768-1200px) | 2-Spalten-Layout, kompakte Ansicht |
| **Smartphone** (<768px) | 1-Spalte, Touch-optimiert |

---

## 🔄 Echtzeit-Updates (SignalR)

**MOBAdash nutzt SignalR für Live-Updates.**

### Wie funktioniert es?

```
Z21 sendet Feedback
    ↓
Backend empfängt (UDP)
    ↓
SignalR Hub pushed Update
    ↓
Browser empfängt (WebSocket)
    ↓
UI aktualisiert automatisch
```

**Vorteile:**
- ✅ **Echtzeit:** Keine Verzögerung
- ✅ **Effizient:** Nur Änderungen werden gesendet
- ✅ **Bidirektional:** Browser kann auch Befehle senden

### Connection Status prüfen:

**Oben rechts im Dashboard:**
- 🟢 **Grün:** SignalR verbunden
- 🟡 **Gelb:** Verbindung wird hergestellt...
- 🔴 **Rot:** Keine Verbindung (Auto-Reconnect läuft)

---

## 🛠️ Problemlösung

### Problem: "Seite nicht erreichbar"

**Lösung:**
1. **Server läuft?** Prüfe Kommandozeile/Task-Manager
2. **Port korrekt?** Standard ist 5000 (HTTP) oder 5001 (HTTPS)
3. **Firewall?** Windows Firewall erlaubt Port 5000/5001?
4. **Netzwerk?** Client und Server im gleichen WLAN?

**Test:**
```bash
# Auf Server-PC (localhost funktioniert?)
http://localhost:5000

# Von anderem Gerät (IP funktioniert?)
http://192.168.0.100:5000
```

### Problem: Keine Live-Updates

**Lösung:**
1. **SignalR Connection Status:** 🟢 Grün?
2. **Browser unterstützt WebSockets?** (Moderne Browser: Ja)
3. **Proxy/VPN aktiv?** Manche blockieren WebSockets
4. **Seite neu laden:** Ctrl + F5 (Hard Reload)

### Problem: "SSL/TLS Fehler" bei HTTPS

**Lösung:**
1. **Selbstsigniertes Zertifikat:** Browser-Warnung akzeptieren
2. **Oder:** Echtes Zertifikat verwenden (Let's Encrypt)
3. **Oder:** HTTP nutzen (nur im lokalen Netzwerk!)

### Problem: Hohe CPU-Last

**Lösung:**
1. **Polling-Intervall erhöhen:** Settings → Z21 → Polling: 10s
2. **Auto-refresh reduzieren:** Settings → Dashboard → Auto-refresh: 30s
3. **Weniger Feedback Points:** Settings → Counter → Count: nur benötigte

---

## 💡 Tipps & Tricks

### 🎨 Dark Mode

**Automatisch basierend auf Systemeinstellung:**
```
Settings → Dashboard → Dark Mode: Auto
```

**Manuell umschalten:**
```
Settings → Dashboard → Dark Mode: Light/Dark
```

### 📊 Diagramme exportieren

**Rechtsklick auf Diagramm → "Als Bild speichern"**

Formate:
- PNG (beste Qualität)
- SVG (Vektorgrafik)
- CSV (Rohdaten)

### 🔔 Browser-Benachrichtigungen

**Aktiviere Benachrichtigungen für wichtige Events:**

```javascript
Settings → Notifications:
✅ Track Power changed
✅ Journey completed
✅ Workflow execution failed
❌ Feedback received (zu viele!)
```

**Hinweis:** Browser muss Benachrichtigungen erlauben!

### 📱 Kiosk-Modus (Always-On-Display)

**Nutze ein altes Tablet als permanentes Dashboard:**

1. **Tablet dauerhaft mit Strom versorgen**
2. **Browser-Kiosk-App installieren** (z.B. "Fully Kiosk Browser")
3. **MOBAdash URL einstellen**
4. **Auto-Start bei Boot aktivieren**
5. **Display-Timeout deaktivieren**

**Ergebnis:** Permanentes Dashboard neben der Anlage! 🖥️

---

## 🌐 Multi-User Zugriff

**Mehrere Personen können gleichzeitig zugreifen:**

```
PC 1 (Desktop): http://192.168.0.100:5000
PC 2 (Laptop): http://192.168.0.100:5000
Tablet: http://192.168.0.100:5000
Smartphone: http://192.168.0.100:5000
```

**Alle sehen die gleichen Live-Daten!**

**⚠️ Achtung:** 
- Nur **ein Client** sollte Track Power steuern (Konflikte vermeiden!)
- Workflows/Journeys können von **allen** gesteuert werden (First-Come-First-Serve)

---

## 📈 Performance-Optimierung

### Browser-Cache nutzen

**MOBAdash lädt statische Ressourcen nur einmal:**

```
Erster Besuch: 5 MB Download
Zweiter Besuch: 50 KB Download (nur Updates)
```

**Cache leeren (falls Probleme):**
```
Ctrl + Shift + Delete → Cache leeren → Reload
```

### Service Worker (Offline-Funktionalität)

**MOBAdash kann teilweise offline funktionieren:**

**Was funktioniert offline?**
- ✅ UI-Struktur (Seiten laden)
- ✅ Statische Inhalte (CSS, Bilder)

**Was funktioniert NICHT offline?**
- ❌ Z21-Verbindung (Internet/WLAN erforderlich)
- ❌ Live-Updates (SignalR benötigt Verbindung)
- ❌ API-Calls (Settings speichern, etc.)

---

## 📜 Lizenz & Credits

**MOBAdash** ist Teil des **MOBAflow**-Projekts (MIT License).

- **Entwickler:** Andreas Hülsmann
- **Framework:** Blazor Server (.NET 10)
- **UI-Library:** MudBlazor 7.0
- **Charting:** Plotly.js
- **Real-time:** SignalR

Siehe [`THIRD-PARTY-NOTICES.md`](../THIRD-PARTY-NOTICES.md).

---

**Viel Spaß mit MOBAdash!** 🚂📊✨
