# MOBAflow Platform Wiki

**Willkommen im MOBAflow Platform Wiki!** 🚂

Hier findest du alle Informationen zu den drei Plattformen:

---

## 📚 Plattform-Übersicht

| Plattform | Technologie | Zielgruppe | Hauptfunktionen |
|-----------|-------------|------------|-----------------|
| **🖥️ MOBAflow** | WinUI 3 (Windows Desktop) | Power-User | Journey-Management, Workflow-Automation, Track Plan Editor |
| **📱 MOBAsmart** | .NET MAUI (Android) | Mobile Nutzer | Lap Counter, Z21 Monitoring, Feedback Statistics |
| **🌐 MOBAdash** | Blazor Server (Web) | Multi-Device | Dashboard, Real-time Monitoring, Statistics |

---

## 🗂️ Dokumentations-Index

> **📖 Benutzer-Dokumentation** - Für alle, die MOBAflow nutzen möchten
> 
> **👨‍💻 Entwickler-Dokumentation** - Am Ende dieser Seite, für Plugin-Entwickler und Contributors

### 🖥️ MOBAflow (Windows Desktop)

**Benutzerhandbuch:**
- [`wiki/MOBAFLOW-USER-GUIDE.md`](wiki/MOBAFLOW-USER-GUIDE.md) - Vollständige Anleitung

**Hauptthemen:**
- 🚂 Journey-Management (Zugfahrten mit Stationen)
- ⚡ Workflow-Automation (Event-driven Actions)
- 🎨 Track Plan Editor (Gleisplan-Visualisierung)
- 🎙️ Text-to-Speech (Azure Cognitive Services)
- 🗂️ Solution Management (Projekt-Verwaltung)

**Setup-Anleitungen:**
- [`wiki/AZURE-SPEECH-SETUP.md`](wiki/AZURE-SPEECH-SETUP.md) - Azure Speech Service einrichten (kostenlos!)

---

### 📱 MOBAsmart (Android)

**Benutzerhandbuch:**
- [`wiki/MOBASMART-USER-GUIDE.md`](wiki/MOBASMART-USER-GUIDE.md) - Vollständige Anleitung
- [`wiki/MOBASMART-WIKI.md`](wiki/MOBASMART-WIKI.md) - Erweiterte Dokumentation

**Hauptthemen:**
- 📊 Lap Counter (Rundenzähler mit Timer Filter)
- 🔌 Z21 Connection (UDP-Kommunikation)
- 📱 Display Management (App im Vordergrund halten)
- 🔋 Battery Optimization
- 🛠️ Troubleshooting

---

### 🌐 MOBAdash (Web)

**Benutzerhandbuch:**
- [`wiki/MOBADASH-USER-GUIDE.md`](wiki/MOBADASH-USER-GUIDE.md) - Vollständige Anleitung

**Hauptthemen:**
- 📊 Dashboard (Real-time Monitoring)
- 📈 Statistics (Auswertung & Diagramme)
- 🔄 SignalR (Live-Updates)
- 🔒 Security (HTTPS, VPN, Reverse Proxy)
- 📱 Progressive Web App (PWA)

---

## 🚀 Quick Start Guide

### Welche Plattform ist die richtige für mich?

#### 🖥️ **Wähle MOBAflow, wenn du...**
- ✅ ...einen **Windows-PC** nutzt
- ✅ ...komplexe **Automatisierungen** brauchst
- ✅ ...Zugfahrten mit **Stationen** definieren möchtest
- ✅ ...einen **Gleisplan** visualisieren willst
- ✅ ...**Text-to-Speech Ansagen** nutzen möchtest

#### 📱 **Wähle MOBAsmart, wenn du...**
- ✅ ...ein **Android-Gerät** nutzt
- ✅ ...**unterwegs** auf die Anlage zugreifen möchtest
- ✅ ...**Lap Counter** (Rundenzähler) brauchst
- ✅ ...eine **einfache, mobile Lösung** suchst
- ✅ ...**ohne PC** arbeiten möchtest

#### 🌐 **Wähle MOBAdash, wenn du...**
- ✅ ...**von mehreren Geräten** zugreifen möchtest
- ✅ ...ein **Dashboard** für Monitoring brauchst
- ✅ ...**Statistiken** auswerten möchtest
- ✅ ...eine **Browser-basierte Lösung** bevorzugst
- ✅ ...**Remote-Zugriff** (von außerhalb) brauchst

---

## 🔗 Plattform-Vergleich

### Funktions-Matrix

| Feature | MOBAflow<br>(Windows) | MOBAsmart<br>(Android) | MOBAdash<br>(Web) |
|---------|----------------------|----------------------|-------------------|
| **Z21 Verbindung** | ✅ | ✅ | ✅ |
| **Track Power Control** | ✅ | ✅ | ✅ |
| **Lap Counter** | ✅ | ✅ | ✅ |
| **Journey Management** | ✅ | ❌ | 🟡 (Read-only) |
| **Workflow Automation** | ✅ | ❌ | 🟡 (Monitor only) |
| **Track Plan Editor** | ✅ | ❌ | ❌ |
| **Text-to-Speech** | ✅ (Azure) | ❌ | ❌ |
| **Statistics** | ✅ | 🟡 (Basic) | ✅ (Advanced) |
| **Multi-User** | ❌ | ❌ | ✅ |
| **Remote Access** | ❌ | ❌ | ✅ |
| **Offline** | ✅ | ✅ | ❌ |

**Legende:**
- ✅ Voll unterstützt
- 🟡 Teilweise unterstützt
- ❌ Nicht verfügbar

---

## 🛠️ Gemeinsame Konzepte

### Z21 Verbindung

**Alle Plattformen nutzen das gleiche Protokoll:**

```
UDP Port: 21105
Protocol: Z21 LAN Protocol (Roco)
Connection: Direct (kein Cloud-Service)
```

**Einrichtung (identisch auf allen Plattformen):**

1. **Z21 IP-Adresse finden:**
   - Z21 App → Einstellungen → Z21-Informationen
   - Oder Router-Webinterface

2. **IP-Adresse in App eingeben:**
   - Beispiel: `192.168.0.111`

3. **Verbinden:**
   - Toggle/Button "Connect"
   - Grüner Status = Verbunden ✅

### Feedback Points (InPorts)

**Definition:**
- **InPort 1-16:** Rückmeldemodule an der Anlage
- **InPort 0:** Spezialwert (kein Feedback)

**Verwendung:**

| Plattform | Verwendung |
|-----------|-----------|
| **MOBAflow** | Journey-Trigger, Workflow-Trigger, Track Plan |
| **MOBAsmart** | Lap Counter (1 Counter pro InPort) |
| **MOBAdash** | Monitoring, Statistics |

**Beispiel:**
```
Anlage mit 3 Rückmeldemodule:
InPort 1: Bahnhof A
InPort 2: Streckenabschnitt
InPort 3: Bahnhof B
```

### Timer Filter (Anti-Doppelzählung)

**Problem:** Langer Zug löst Gleiskontakt mehrfach aus

**Lösung:** Timer Filter (in Settings konfigurierbar)

| Plattform | Einstellung |
|-----------|-------------|
| **MOBAflow** | Settings → Counter → Timer Interval |
| **MOBAsmart** | Main Page → Timer Filter (Checkbox + Intervall) |
| **MOBAdash** | Settings → Counter → Timer Interval |

**Empfohlene Werte:**
- Kurze Züge (2-3 Wagen): **5-8 Sekunden**
- Mittlere Züge (4-6 Wagen): **10-15 Sekunden**
- Lange Züge (>6 Wagen): **15-20 Sekunden**

---

## 🏗️ Architektur-Übersicht

### Schichtenmodell

```
┌─────────────────────────────────────────────────────┐
│ Presentation Layer                                  │
├─────────────────┬──────────────┬────────────────────┤
│ WinUI (Desktop) │ MAUI (Mobile)│ Blazor (Web)       │
│   MOBAflow      │  MOBAsmart   │  MOBAdash          │
├─────────────────┴──────────────┴────────────────────┤
│ SharedUI Layer (ViewModels, Common Logic)          │
├─────────────────────────────────────────────────────┤
│ Backend Layer (Business Logic, Z21 Communication)  │
├─────────────────────────────────────────────────────┤
│ Domain Layer (Entities: Journey, Workflow, Train)  │
└─────────────────────────────────────────────────────┘
```

**Vorteile:**
- ✅ **Code Sharing:** 80% gemeinsamer Code
- ✅ **Konsistenz:** Gleiche Logik auf allen Plattformen
- ✅ **Wartbarkeit:** Bugfixes gelten für alle Plattformen

### Datenfluss

```
Z21 Digital-Zentrale (UDP Port 21105)
    ↓
Backend.Z21 (UDP Client)
    ↓
Backend.FeedbackResult Event
    ↓
┌───────────┬──────────────┬──────────────┐
│ MOBAflow  │  MOBAsmart   │  MOBAdash    │
│ (WinUI)   │  (MAUI)      │  (Blazor)    │
└───────────┴──────────────┴──────────────┘
```

---

## 📖 Erweiterte Themen

### Solution Format (.mobaflow.json)

**MOBAflow speichert alle Daten in einer JSON-Datei:**

```json
{
  "Journeys": [
    {
      "Id": "guid-123",
      "Name": "ICE Hamburg → München",
      "InPort": 10,
      "TrainId": "guid-456",
      "Stations": [...]
    }
  ],
  "Workflows": [...],
  "Trains": [...],
  "Locomotives": [...],
  "TrackLayouts": [...]
}
```

**Kompatibilität:**
- ✅ **MOBAflow:** Voll bearbeitbar
- 🟡 **MOBAsmart:** Nicht unterstützt (fokussiert auf Lap Counter)
- 🟡 **MOBAdash:** Read-only Monitoring

### Settings-Persistierung

**Jede Plattform speichert Settings separat:**

| Plattform | Speicherort | Format |
|-----------|-------------|--------|
| **MOBAflow** | `%APPDATA%/MOBAflow/appsettings.json` | JSON |
| **MOBAsmart** | `/data/user/0/com.mobaflow.mobasmart/files/appsettings.json` | JSON |
| **MOBAdash** | `appsettings.json` (Server-Verzeichnis) | JSON |

**Gemeinsame Settings:**
```json
{
  "Z21": {
    "CurrentIpAddress": "192.168.0.111",
    "DefaultPort": "21105"
  },
  "Counter": {
    "CountOfFeedbackPoints": 3,
    "TargetLapCount": 10,
    "UseTimerFilter": true,
    "TimerIntervalSeconds": 10.0
  }
}
```

---

## 🤝 Multi-Platform Workflows

### Szenario 1: Desktop + Mobile

**Setup:**
1. **MOBAflow (PC):** Journey-Management + Workflows
2. **MOBAsmart (Handy):** Lap Counter Monitoring

**Workflow:**
- PC: Journeys/Workflows konfigurieren
- Handy: Runden zählen beim Fahren
- PC: Statistiken auswerten

### Szenario 2: Desktop + Web Dashboard

**Setup:**
1. **MOBAflow (PC):** Hauptsteuerung
2. **MOBAdash (Tablet):** Always-On Dashboard

**Workflow:**
- PC: Automation steuern
- Tablet: Live-Monitoring (neben der Anlage)
- Beide: Zugriff auf Z21

### Szenario 3: Pure Web (Server + Clients)

**Setup:**
1. **MOBAdash Server (Raspberry Pi):** Headless
2. **Browser (Laptop):** Hauptsteuerung
3. **Browser (Smartphone):** Mobile Monitoring

**Workflow:**
- Server: Läuft 24/7, verbindet Z21
- Clients: Greifen von überall zu
- Vorteil: Keine Installation auf Clients nötig

---

## 🛠️ Troubleshooting (Plattform-übergreifend)

### Problem: Z21 verbindet nicht (alle Plattformen)

**Checkliste:**

1. ✅ **Netzwerk:** Alle Geräte im gleichen WLAN?
2. ✅ **IP-Adresse:** Korrekt eingegeben? (z.B. 192.168.0.111)
3. ✅ **Z21 Status:** Eingeschaltet? LED leuchtet?
4. ✅ **Firewall:** Blockiert UDP Port 21105?
5. ✅ **Router:** "AP Isolation" deaktiviert?

**Test-Befehl (Windows):**
```bash
# Ping zur Z21
ping 192.168.0.111

# UDP Port testen (mit nmap)
nmap -sU -p 21105 192.168.0.111
```

### Problem: Feedbacks werden nicht empfangen

**Checkliste:**

1. ✅ **Rückmeldemodule:** Angeschlossen? (RBus an Z21)
2. ✅ **Gleiskontakte:** Verkabelt? Sauber?
3. ✅ **Z21 App Test:** Feedbacks dort sichtbar?
4. ✅ **InPort Mapping:** Korrekt konfiguriert?

**Test:**
1. Z21 App öffnen
2. Menü → Rückmeldungen
3. Zug über Gleiskontakt fahren
4. LED leuchtet auf? → Hardware OK
5. Wenn nein → Verkabelung prüfen

---

## 📚 Weitere Ressourcen

### Offizielle Dokumentation

- **Architecture:** [`ARCHITECTURE.md`](ARCHITECTURE.md)
- **Contributing:** [`CONTRIBUTING.md`](../CONTRIBUTING.md)
- **Third-Party Notices:** [`THIRD-PARTY-NOTICES.md`](../THIRD-PARTY-NOTICES.md)

### Technische Dokumentation

- **Z21 Protocol:** [Roco Z21 LAN Protocol](https://www.z21.eu/media/Kwc_Basic_DownloadTag_Component/47-2811-2411-downloadTag-47-2811-2411/default/fb4022/1606895562/z21-lan-protokoll.pdf)
- **AnyRail Integration:** [`ANYRAIL-INTEGRATION-LEGAL.md`](ANYRAIL-INTEGRATION-LEGAL.md)

### Community

- **Repository:** [Azure DevOps](https://dev.azure.com/ahuelsmann/MOBAflow)
- **Issues:** [Bug Reports & Feature Requests](https://dev.azure.com/ahuelsmann/MOBAflow/_workitems/create/Bug)

---

## 📜 Lizenz

**MOBAflow Platform** ist Open Source (MIT License).

- **Copyright:** © 2025-2026 Andreas Huelsmann
- **License:** MIT
- **Repository:** https://dev.azure.com/ahuelsmann/MOBAflow

Siehe [`LICENSE`](../LICENSE) für Details.

---

**Viel Spaß mit der MOBAflow Platform!** 🚂✨

*Letzte Aktualisierung: 05.02.2025*

---

# 👨‍💻 Entwickler-Dokumentation

> **Hinweis:** Die folgenden Abschnitte richten sich an **Software-Entwickler**, die MOBAflow erweitern oder Plugins entwickeln möchten.

---

## 🔌 Plugin Development

**Für Entwickler, die MOBAflow mit eigenen Plugins erweitern möchten.**

📖 **Vollständige Dokumentation:** [`wiki/PLUGIN-DEVELOPMENT.md`](wiki/PLUGIN-DEVELOPMENT.md)

### Überblick

Das Plugin-System ermöglicht es, eigene Seiten, Features und Integrationen hinzuzufügen, ohne den Core-Code zu modifizieren.

**Hauptmerkmale:**
- ✅ Auto-Discovery von Plugins im `Plugins/` Ordner
- ✅ Automatische Validierung beim Start
- ✅ Full Dependency Injection Support
- ✅ Lifecycle Hooks (OnInitialize, OnUnload)
- ✅ Robustheit - App läuft auch ohne/mit defekten Plugins

### Schnellstart

```bash
# 1. Template kopieren
cp -r Plugins/SamplePlugin Plugins/MeinPlugin

# 2. Klassen umbenennen
# 3. Plugin-Logik implementieren
# 4. Build & Test
dotnet build Plugins/MeinPlugin
```

### Entwickler-Ressourcen

| Ressource | Link |
|-----------|------|
| **Plugin-Entwickler-Handbuch** | [`wiki/PLUGIN-DEVELOPMENT.md`](wiki/PLUGIN-DEVELOPMENT.md) |
| **Plugin Interface** | [`Common/Plugins/IPlugin.cs`](../../Common/Plugins/IPlugin.cs) |
| **Plugin Base Class** | [`Common/Plugins/PluginBase.cs`](../../Common/Plugins/PluginBase.cs) |
| **Sample Plugin** | [`Plugins/SamplePlugin/`](../../Plugins/SamplePlugin/) |
| **Architektur-Übersicht** | [`docs/ARCHITECTURE.md`](../ARCHITECTURE.md) |

### Technologie-Stack

| Komponente | Technologie |
|------------|-------------|
| **MVVM** | CommunityToolkit.Mvvm |
| **DI** | Microsoft.Extensions.DependencyInjection |
| **UI** | WinUI 3 (XAML) |
| **Isolation** | AssemblyLoadContext (pro Plugin) |

---
