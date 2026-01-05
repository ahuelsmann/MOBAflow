# MOBAflow (WinUI) - Benutzerhandbuch

**Version:** 1.0  
**Plattform:** Windows 10/11 (Desktop)  
**Letzte Aktualisierung:** 29.12.2025

---

## 📱 Was ist MOBAflow?

**MOBAflow** ist die Desktop-Anwendung für die umfassende Steuerung und Automatisierung deiner Modellbahnanlage. Sie verbindet sich direkt per UDP mit deiner **Roco Z21 Digital-Zentrale** und bietet erweiterte Funktionen wie Journey-Management, Workflow-Automatisierung und Track Plan Editor.

---

## 🚀 Erste Schritte

### 1. Systemvoraussetzungen

- **Betriebssystem:** Windows 10 (Version 1809+) oder Windows 11
- **Runtime:** .NET 10 Desktop Runtime (wird automatisch installiert)
- **Hardware:** 
  - Roco Z21 Digital-Zentrale
  - Rückmeldemodule (Roco 10808, 10787, etc.)
  - WLAN-Router (Z21 und PC im gleichen Netzwerk)

### 2. Installation

1. **Download:** Lade MOBAflow von [Releases](https://dev.azure.com/ahuelsmann/MOBAflow/_git/MOBAflow) herunter
2. **Installation:** Starte `MOBAflow-Setup.exe`
3. **Runtime:** Falls .NET 10 fehlt, wird es automatisch heruntergeladen
4. **Start:** Öffne MOBAflow über das Desktop-Icon

### 3. Erster Start

1. **Willkommensbildschirm:** Übersicht über die Hauptfunktionen
2. **Z21 verbinden:** IP-Adresse eingeben und "Connect" klicken
3. **Solution erstellen:** Neue Solution anlegen oder existierende öffnen

---

## 🎯 Hauptfunktionen

### 📊 Overview Page

**Die zentrale Steuerung deiner Anlage.**

#### Funktionen:
- **Z21 Connection Status:** Grün = Verbunden, Rot = Getrennt
- **Track Power Control:** Gleisspannung ein/aus
- **System Stats:** 
  - 🌡️ Temperatur der Z21
  - 🔌 Versorgungsspannung
  - ⚡ VCC-Spannung
  - 🔋 Stromaufnahme (Main Current)

#### Lap Counter (Rundenzähler):
- **Echtzeitüberwachung** aller Feedback Points
- **Rundenzeiten** mit Durchschnittsberechnung
- **Fortschrittsbalken** pro Track
- **Export-Funktion** (CSV, JSON)

---

### 🚂 Journeys (Fahrten)

**Definiere komplexe Zugfahrten mit Stationen.**

#### Was ist eine Journey?
Eine **Journey** ist eine vordefinierte Route mit mehreren Stationen. Bei jeder Station können automatisch Aktionen ausgeführt werden (Ansagen, Befehle, Sounds).

#### Journey erstellen:

1. **Journeys Page** öffnen (Seitenleiste)
2. **Add Journey** klicken
3. **Properties:**
   - **Name:** z.B. "ICE Berlin → München"
   - **InPort:** Feedback Point für Zugerkennung (z.B. InPort 5)
   - **Train:** Wähle deinen Zug aus der Liste

4. **Stations hinzufügen:**
   - **Add Station** klicken
   - **Name:** z.B. "Berlin Hbf"
   - **InPort:** Feedback Point der Station (z.B. InPort 1)
   - **Workflow:** Aktion bei Ankunft (optional)

#### Beispiel-Journey:
```
Journey: "ICE 1234 Hamburg → Frankfurt"
├─ Station 1: "Hamburg Hbf" (InPort 1)
│  └─ Workflow: Ansage "Der Zug fährt ab"
├─ Station 2: "Bremen Hbf" (InPort 3)
│  └─ Workflow: Ansage "Nächster Halt: Hannover"
└─ Station 3: "Frankfurt Hbf" (InPort 5)
   └─ Workflow: Ansage "Endstation erreicht"
```

#### Journey aktivieren:
1. **Journey auswählen** in der Liste
2. **Start Journey** klicken
3. **Zug bewegen** → Bei jedem Feedback wird die passende Station erkannt
4. **Counter erhöht sich** → Zeigt wie oft die Journey durchlaufen wurde

---

### ⚡ Workflows

**Automatisiere Aktionen mit ereignisgesteuerten Workflows.**

#### Was ist ein Workflow?
Ein **Workflow** ist eine Sequenz von Aktionen, die automatisch bei einem Ereignis ausgeführt werden (z.B. Feedback-Event, Zeit-Trigger, Button-Klick).

#### Workflow erstellen:

1. **Workflows Page** öffnen
2. **Add Workflow** klicken
3. **Properties:**
   - **Name:** z.B. "Bahnhofsansage Berlin"
   - **InPort:** Trigger-Feedback Point (z.B. InPort 1)
   - **Execution Mode:** Sequential (nacheinander) oder Parallel (gleichzeitig)
   - **Actions:** Liste der auszuführenden Aktionen

#### Execution Modes:

| Mode | Beschreibung | DelayAfterMs Bedeutung |
|------|--------------|------------------------|
| **Sequential** | Actions laufen nacheinander | Pause NACH Action-Ende vor nächster Action |
| **Parallel** | Actions starten gestaffelt (overlapping) | Start-Offset (kumulativ von vorheriger Action) |

**Beispiel Sequential:**
```
Action 1: Gong abspielen → Wartet bis Ende → Pause 1000ms → Action 2 startet
Action 2: Ansage → Wartet bis Ende → Action 3 startet
```

**Beispiel Parallel (Staggered Start):**
```
t=0ms:    Action 1: Gong (DelayAfterMs=0)           → Startet sofort
t=500ms:  Action 2: Ansage (DelayAfterMs=500)       → Startet nach 500ms (Gong läuft noch)
t=2500ms: Action 3: Beleuchtung (DelayAfterMs=2000) → Startet nach weiteren 2s
```

#### Verfügbare Actions:

| Action Type | Beschreibung | Parameter |
|-------------|--------------|-----------|
| **Announcement** | Text-to-Speech Ansage | Text, Voice, Rate, Volume |
| **Command** | Z21-Befehl senden | Command Bytes |
| **Audio** | WAV-Datei abspielen | File Path |

**Alle Actions unterstützen:**
- **DelayAfterMs:** Zeitverzögerung (Bedeutung abhängig von Execution Mode)

#### Beispiel-Workflow (Sequential):
```yaml
Workflow: "Bahnhofsansage Berlin Hbf"
Trigger: InPort 1
Execution Mode: Sequential

Actions:
1. Audio: "gong.wav" (DelayAfterMs: 1000)          → Gong + 1s Pause danach
2. Announcement: "ICE 1234 fährt ein"              → Ansage
3. Announcement: "Vorsicht bei der Einfahrt"       → Zweite Ansage
```

#### Beispiel-Workflow (Parallel):
```yaml
Workflow: "Bahnhof mit Effekten"
Trigger: InPort 1
Execution Mode: Parallel

Actions:
1. Audio: "gong.wav" (DelayAfterMs: 0)             → t=0ms: Gong startet
2. Announcement: "Zug fährt ein" (DelayAfterMs: 500) → t=500ms: Ansage startet (Gong läuft noch)
3. Command: Beleuchtung (DelayAfterMs: 2000)       → t=2500ms: Licht schaltet
```

---

### 🎨 Track Plan Editor

**Visualisiere und bearbeite deinen Gleisplan.**

#### Funktionen:
- **AnyRail Import:** Importiere Gleispläne aus AnyRail XML
- **Drag & Drop:** Platziere Gleise auf der Canvas
- **Feedback Points:** Verknüpfe Gleise mit InPorts
- **Zoom & Pan:** Navigation mit Maus/Touchpad

#### AnyRail Import:

1. **AnyRail öffnen** → Gleisplan erstellen
2. **Export:** File → Export → XML
3. **MOBAflow:** Track Plan Page → Import → XML auswählen
4. **Fertig!** Gleisplan wird automatisch konvertiert

#### Manuelle Bearbeitung:

1. **Track Library** (links): Verfügbare Gleise (Piko A-Gleis)
2. **Canvas** (Mitte): Arbeitsbereich
3. **Properties** (rechts): Eigenschaften des ausgewählten Gleises

**Gleise platzieren:**
- Drag & Drop aus Library
- Doppelklick auf Track → Rotation ändern
- Rechtsklick → Löschen

---

### 🗂️ Solution Management

**Organisiere deine Anlage in Projects und Solutions.**

#### Was ist eine Solution?
Eine **Solution** ist eine Datei (`.mobaflow.json`), die alle deine Daten enthält:
- Journeys
- Workflows
- Track Plans
- Trains
- Locomotives
- Feedback Points

#### Neue Solution erstellen:

1. **File** → **New Solution**
2. **Name:** z.B. "Meine Anlage 2025"
3. **Speicherort:** Wähle einen Ordner
4. **Save**

#### Solution öffnen:

1. **File** → **Open Solution**
2. **Wähle `.mobaflow.json` Datei**
3. **Fertig!** Alle Daten werden geladen

#### Auto-Load beim Start:

1. **Settings** → **Auto-load last solution**
2. ✅ **Aktivieren**
3. Beim nächsten Start wird die letzte Solution automatisch geladen

---

## 🎙️ Text-to-Speech (Azure Cognitive Services)

**Professionelle Ansagen mit Azure Speech.**

### Einrichtung:

1. **Azure Account:** Erstelle einen kostenlosen Azure Account
2. **Speech Service:** Erstelle eine Speech Resource
3. **API Key kopieren:** Key + Region notieren

### In MOBAflow konfigurieren:

1. **Settings** → **Speech**
2. **API Key:** Einfügen
3. **Region:** z.B. "germanywestcentral"
4. **Voice:** z.B. "de-DE-ConradNeural" (männlich) oder "de-DE-KatjaNeural" (weiblich)

### Test:

1. **Workflows** → **Add Workflow** → **Add Announcement Action**
2. **Text:** "Dies ist ein Test"
3. **Play** → Ansage sollte abgespielt werden

### Kostenlose Kontingente:
- **5 Millionen Zeichen/Monat** kostenlos
- Für private Nutzung mehr als ausreichend!

---

## 🔧 Einstellungen (Settings Page)

### General

| Einstellung | Beschreibung | Default |
|-------------|--------------|---------|
| **Auto-load last solution** | Letzte Solution beim Start laden | ✅ Aktiviert |
| **Reset window layout on start** | Fenstergröße/-position zurücksetzen | ❌ Deaktiviert |

### Z21

| Einstellung | Beschreibung | Default |
|-------------|--------------|---------|
| **Current IP Address** | Z21 IP-Adresse | 192.168.0.111 |
| **Default Port** | UDP-Port | 21105 |
| **Auto-connect retry interval** | Wiederverbindung (Sekunden) | 10 |
| **System state polling interval** | Status-Abfrage (Sekunden) | 5 |

### Speech

| Einstellung | Beschreibung | Default |
|-------------|--------------|---------|
| **API Key** | Azure Speech Key | (leer) |
| **Region** | Azure Region | germanywestcentral |
| **Voice** | Standard-Stimme | de-DE-ConradNeural |
| **Rate** | Sprechgeschwindigkeit (-10 bis +10) | -1 |
| **Volume** | Lautstärke (0-100) | 90 |

### Counter

| Einstellung | Beschreibung | Default |
|-------------|--------------|---------|
| **Count of Feedback Points** | Anzahl InPorts | 0 |
| **Target Lap Count** | Ziel-Rundenzahl | 10 |
| **Use Timer Filter** | Anti-Doppelzählung | ✅ Aktiviert |
| **Timer Interval** | Filter-Intervall (Sekunden) | 10.0 |

---

## 🛠️ Problemlösung

### Problem: Z21 verbindet nicht

**Lösung:**
1. **IP-Adresse prüfen:** Stimmt sie mit der Z21 überein?
2. **Firewall:** Windows Firewall erlaubt MOBAflow? (Port UDP 21105)
3. **WLAN:** PC und Z21 im gleichen Netzwerk?
4. **Z21 neustarten:** Stromversorgung trennen, 10s warten

### Problem: Azure Speech funktioniert nicht

**Lösung:**
1. **API Key korrekt?** Prüfe in Azure Portal
2. **Region korrekt?** Muss mit Key übereinstimmen
3. **Internet-Verbindung?** Azure erfordert Internet
4. **Kontingent aufgebraucht?** Prüfe Azure Nutzung

### Problem: Journeys zählen nicht

**Lösung:**
1. **InPort korrekt?** Journey.InPort muss mit Feedback Point übereinstimmen
2. **Feedback empfangen?** Prüfe auf Overview Page (Lap Counter)
3. **Journey aktiviert?** "Start Journey" geklickt?

### Problem: Workflow wird nicht ausgeführt

**Lösung:**
1. **InPort korrekt?** Workflow.InPort muss Trigger-Feedback sein
2. **Actions vorhanden?** Mindestens 1 Action erforderlich
3. **Fehler in Action?** Log prüfen (View → Logs)

---

## 💡 Tipps & Tricks

### 🚂 Best Practice: Journey-Struktur

**Gute Journey:**
```
Journey: "ICE Hamburg → München"
InPort: 10 (Lok-Decoder Feedback)
Stations:
  1. Hamburg (InPort 1)
  2. Bremen (InPort 3)
  3. Hannover (InPort 5)
  4. Frankfurt (InPort 7)
  5. München (InPort 9)
```

**Schlechte Journey:**
```
Journey: "Alle Züge"
InPort: 0 (kein spezifischer Zug)
Stations:
  1. Irgendwo (InPort 1)
```

### ⚡ Performance-Optimierung

**Problem:** App wird langsam bei vielen Feedbacks

**Lösung:**
1. **Polling-Intervall erhöhen:** Settings → Z21 → Polling Interval: 10s
2. **Weniger Workflows:** Deaktiviere ungenutzte Workflows
3. **Log-Level reduzieren:** Settings → Logging → Level: Warning

### 🎨 Track Plan Import

**Tipp:** AnyRail Gleispläne sind präziser als manuelles Zeichnen!

**Workflow:**
1. **AnyRail:** Exakte Planung mit Maßen
2. **Export XML:** Alle Geometrie-Infos erhalten
3. **MOBAflow Import:** Automatische Konvertierung
4. **Feedback Points zuweisen:** InPorts verknüpfen

---

## 📋 Keyboard Shortcuts

| Shortcut | Funktion |
|----------|----------|
| **Ctrl + N** | Neue Solution |
| **Ctrl + O** | Solution öffnen |
| **Ctrl + S** | Solution speichern |
| **Ctrl + Q** | App beenden |
| **F1** | Hilfe öffnen |
| **F5** | Z21 Verbindung aktualisieren |
| **Ctrl + T** | Track Power Toggle |

---

## 📜 Lizenz & Credits

**MOBAflow** ist Open Source (MIT License).

- **Entwickler:** Andreas Huelsmann
- **Repository:** [Azure DevOps](https://dev.azure.com/ahuelsmann/MOBAflow)
- **Version:** 3.9 (Dezember 2025)

**Drittanbieter:**
- Roco Z21 (Protokoll)
- Azure Cognitive Services (Speech)
- AnyRail (Import-Format)
- Microsoft WinUI 3 (UI Framework)

Siehe [`THIRD-PARTY-NOTICES.md`](../THIRD-PARTY-NOTICES.md).

---

**Viel Spaß mit MOBAflow!** 🚂✨
