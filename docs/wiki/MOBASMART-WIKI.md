# MOBAsmart Wiki

**Willkommen im MOBAsmart Wiki!** 🚂

Diese Dokumentation hilft dir, das Beste aus deiner Modellbahn-App herauszuholen.

---

## 📚 Inhaltsverzeichnis

- [Erste Schritte](#-erste-schritte)
- [Verbindung zur Z21](#-verbindung-zur-z21)
- [Lap Counter Einstellungen](#-lap-counter-einstellungen)
- [Lap Counting verstehen](#-lap-counting-verstehen)
- [Best Practices](#-best-practices)
- [Problemlösung](#-problemlösung)
- [FAQ](#-faq)
- [Technische Details](#-technische-details)

---

## 🚀 Erste Schritte

### Was du brauchst

| Komponente | Beschreibung | Erforderlich |
|------------|--------------|--------------|
| **Android-Gerät** | Smartphone oder Tablet (Android 7.0+) | ✅ Ja |
| **Roco Z21** | Digital-Zentrale (alle Varianten: Z21, Z21 start, z21) | ✅ Ja |
| **WLAN** | Z21 und Android-Gerät im gleichen Netzwerk | ✅ Ja |
| **Rückmeldemodule** | z.B. Roco 10808, 10787 | ✅ Ja |
| **Gleiskontakte** | z.B. Roco 42614, Märklin 74030 | ✅ Ja |

### Installation

#### Google Play Store *(geplant)*
1. Öffne Google Play Store
2. Suche nach **"MOBAsmart"**
3. Tippe auf **Installieren**
4. Öffne die App

#### Manuelle Installation (APK)
1. Lade die APK-Datei herunter
2. **Einstellungen** → **Sicherheit** → **Unbekannte Quellen** aktivieren
3. APK-Datei antippen und installieren
4. Öffne **MOBAsmart**

### Erster Start

1. **App öffnen** → Du siehst den Hauptbildschirm
2. **Berechtigungen erteilen** (Netzwerk) → Tippe auf "Erlauben"
3. **Fertig!** → Die App ist bereit

---

## 🔌 Verbindung zur Z21

### Z21 IP-Adresse finden

#### Methode 1: Z21 App (einfachste)
1. Öffne die **Z21 App** (Roco)
2. **Menü** → **Einstellungen** → **Z21-Informationen**
3. Notiere die **IP-Adresse** (z.B. `192.168.0.111`)

#### Methode 2: Router-Webinterface
1. Router-Webinterface öffnen (meist `192.168.0.1` oder `192.168.1.1`)
2. **Netzwerk** → **Verbundene Geräte**
3. Suche nach **"Z21"** oder **"ROCO"**
4. Notiere die IP-Adresse

#### Methode 3: Netzwerk-Scanner App
1. Installiere **"Fing"** oder **"Network Scanner"** (Google Play Store)
2. Scanne dein Netzwerk
3. Suche nach Gerät mit Name **"Z21"**
4. Notiere die IP-Adresse

### Verbindung herstellen

1. **IP-Adresse eingeben:**
   - Tippe in das Eingabefeld (oben auf dem Bildschirm)
   - Gib die Z21 IP-Adresse ein (z.B. `192.168.0.111`)

2. **Verbinden:**
   - Tippe auf den **Connection Switch** (neben "Disconnected")
   - Warte 2-3 Sekunden

3. **Verbindung prüfen:**
   - **Grüner Punkt** oben rechts → ✅ Verbunden
   - **Roter Punkt** oben rechts → ❌ Keine Verbindung
   - **System Stats** werden angezeigt (Temperatur, Spannung)

### Verbindung trennen

- Tippe erneut auf den **Connection Switch**
- Warte bis **"Disconnected"** angezeigt wird

---

## ⚙️ Lap Counter Einstellungen

### Feedback Points (Tracks)

#### Was sind Feedback Points?
**Feedback Points** sind die Rückmeldemodule an deiner Anlage, die erkennen, wenn ein Zug vorbeifährt.

**Beispiel:**
```
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

**Einstellung:** `CountOfFeedbackPoints = 3`

#### Wie einstellen?

1. **Zähle deine Rückmeldemodule:**
   - Wie viele Roco 10808/10787 hast du angeschlossen?
   - Jedes Modul = 1 Feedback Point

2. **In der App einstellen:**
   - **Tracks:** Tippe **−** oder **+**
   - Beispiel: 3 Module → Setze auf **3**

3. **Ergebnis:**
   - Die App erstellt 3 separate Zähler:
     - Track 1
     - Track 2
     - Track 3

**💡 Tipp:** Beginne mit **1 Feedback Point** zum Testen!

### Target Lap Count (Ziel-Runden)

#### Was ist das?
**Target Lap Count** ist die Anzahl der Runden, die du erreichen möchtest.

**Beispiel:**
- **Racing:** 10 Runden
- **Dauertest:** 100 Runden
- **Kurz-Test:** 5 Runden

#### Wie einstellen?

1. **Ziel festlegen:**
   - Wie viele Runden soll der Zug fahren?

2. **In der App einstellen:**
   - **Target:** Tippe **−** oder **+**
   - Beispiel: 10 Runden → Setze auf **10**

3. **Ergebnis:**
   - **Fortschrittsbalken** zeigt den Fortschritt
   - Beispiel: 3 von 10 Runden = 30% ━━━━░░░░░░

### Timer Filter (Anti-Doppelzählung)

#### Was ist das?
**Timer Filter** verhindert, dass ein langer Zug mehrfach gezählt wird, wenn er langsam über einen Gleiskontakt fährt.

**Problem ohne Timer Filter:**
```
Zug fährt über Gleiskontakt:
  Sekunde 0: Lok aktiviert Kontakt     → Count: 1
  Sekunde 2: Wagen 3 noch auf Kontakt  → Count: 2 ❌
  Sekunde 4: Wagen 6 noch auf Kontakt  → Count: 3 ❌
  Sekunde 6: Letzter Wagen verlässt    → Count: 4 ❌

Ergebnis: 4 Counts, aber nur 1 Durchgang!
```

**Lösung mit Timer Filter (10s):**
```
Zug fährt über Gleiskontakt:
  Sekunde 0: Lok aktiviert Kontakt     → Count: 1 ✅
  Sekunde 2: Filter aktiv (noch 8s)    → Ignoriert
  Sekunde 4: Filter aktiv (noch 6s)    → Ignoriert
  Sekunde 6: Filter aktiv (noch 4s)    → Ignoriert
  
Nächster Durchgang (12 Sekunden später):
  Sekunde 12: Filter abgelaufen        → Count: 2 ✅

Ergebnis: 2 Counts, 2 Durchgänge = Korrekt!
```

#### Wie einstellen?

**1. Timer aktivieren/deaktivieren:**
- ✅ **Checkbox** anhaken → Timer aktiv
- ⬜ **Checkbox** leer → Timer inaktiv

**2. Intervall einstellen:**
- **Tippe −/+** neben dem Timer-Wert
- **Werte:** 1.0s bis 60.0s (Schritte: 1.0s)

**3. Empfohlene Werte:**

| Szenario | Empfehlung | Grund |
|----------|------------|-------|
| **Kurze Züge** (2-3 Wagen) | 5-8 Sekunden | Schnelle Durchgänge |
| **Mittlere Züge** (4-6 Wagen) | 10-15 Sekunden | Standard-Länge |
| **Lange Züge** (>6 Wagen) | 15-20 Sekunden | Lange Kontaktzeit |
| **Sehr langsame Fahrt** | 20-30 Sekunden | Viel Zeit über Kontakt |

**💡 Tipp:** Teste mit **10 Sekunden** (Standard) und passe bei Bedarf an!

---

## 📊 Lap Counting verstehen

### Zähler-Anzeige erklärt

```
┌──────────────────────────────────────────────┐
│ [5]  Track 1                                 │
│      Lap: 00:12.5  @  22:15:30               │
│      Lap 5/10 ━━━━━━━━━━░░░░░░  50%         │
└──────────────────────────────────────────────┘
```

**Bedeutung der Elemente:**

| Element | Bedeutung | Beispiel |
|---------|-----------|----------|
| **[5]** | Aktuelle Rundenanzahl | 5 Runden gefahren |
| **Track 1** | Feedback Point Nummer | Gleiskontakt Nr. 1 |
| **Lap: 00:12.5** | Letzte Rundenzeit | 12,5 Sekunden für letzte Runde |
| **@ 22:15:30** | Zeitpunkt der Erfassung | Heute um 22:15:30 Uhr |
| **Lap 5/10** | Fortschritt | 5 von 10 Ziel-Runden |
| **━━━━━━━━━━** | Fortschrittsbalken | 50% erreicht |
| **50%** | Prozentangabe | Halbe Strecke geschafft |

### Badge-Farben

| Farbe | Bedeutung | Wann? |
|-------|-----------|-------|
| 🟦 **Blau (Primary)** | Noch nicht aktiv | Keine Runde erfasst |
| 🟢 **Grün (Accent)** | Aktiv | Mindestens 1 Runde erfasst |

### Rundenzeit-Berechnung

**Wie wird die Rundenzeit berechnet?**

```
Zeit zwischen zwei aufeinanderfolgenden Feedbacks:

Durchgang 1: 22:15:30 (Erste Erfassung, keine Zeit)
Durchgang 2: 22:15:42 → Lap Time: 12 Sekunden
Durchgang 3: 22:15:55 → Lap Time: 13 Sekunden
Durchgang 4: 22:16:07 → Lap Time: 12 Sekunden
```

**💡 Hinweis:** 
- Die **erste Runde** hat keine Zeit (Startpunkt unbekannt)
- Ab der **zweiten Runde** wird die Zeit gemessen
- Die Zeit zeigt **nur die letzte Runde** (nicht Durchschnitt)

---

## ✅ Best Practices

### 🏁 Racing Setup (3 Züge, 10 Runden)

**Szenario:** Du willst ein Rennen mit 3 Zügen fahren.

#### Hardware-Setup
```
3 separate Gleiskontakte:
┌─────────────────────────────────────┐
│  [Track 1] ← Zug 1 (ICE)           │
│  [Track 2] ← Zug 2 (TGV)           │
│  [Track 3] ← Zug 3 (Railjet)       │
└─────────────────────────────────────┘
```

#### App-Einstellungen
```yaml
Tracks: 3
Target: 10
Timer Filter: ✅ Aktiviert
Intervall: 8 Sekunden (schnelle Züge)
```

#### Workflow
1. **Reset** → Zähler auf 0
2. **Track Power ON** → Gleisspannung einschalten
3. **Züge starten** (via Z21 App oder Handregler)
4. **Beobachten:** Welcher Zug erreicht zuerst 10/10?
5. **Sieger:** Zug mit 100% zuerst! 🏆

### 🔄 Automatik-Betrieb (1 Zug, Dauerbetrieb)

**Szenario:** Ein Zug fährt automatisch im Kreis.

#### Hardware-Setup
```
1 Gleiskontakt:
┌─────────────────────────────────────┐
│              ↓                      │
│  ←────── [Track 1] ──────→         │
│              ↑                      │
│  ────────────┘                      │
└─────────────────────────────────────┘
```

#### App-Einstellungen
```yaml
Tracks: 1
Target: 50 (lange Session)
Timer Filter: ✅ Aktiviert
Intervall: 15 Sekunden (langsamer Zug)
```

#### Workflow
1. **Track Power ON**
2. **Zug auf Geschwindigkeit 40-50%** (langsame, konstante Fahrt)
3. **App beobachten** (Display an lassen!)
4. **Nach 50 Runden:** Zug stoppen, Statistik auswerten

### 📱 Display-Management (lange Sessions)

**Problem:** Akku leert sich, Display schaltet ab.

**Lösung 1: Display-Timeout erhöhen**
```
Android Einstellungen
→ Display
→ Bildschirm-Timeout
→ 10 Minuten
```

**Lösung 2: Entwickleroptionen (mit Ladegerät!)**
```
Android Einstellungen
→ Entwickleroptionen
→ Display bleibt an
→ ✅ Aktivieren
→ Ladegerät anschließen!
```

**Lösung 3: Power Bank**
```
USB-C Power Bank anschließen
→ Display auf 50% Helligkeit
→ Nachtmodus aktivieren (spart Energie)
```

---

## 🛠️ Problemlösung

### Problem: Keine Verbindung zur Z21

#### Symptom
- Roter Punkt oben rechts
- "Disconnected" wird angezeigt
- Keine System Stats sichtbar

#### Lösungen

**1. IP-Adresse prüfen**
```
Richtig: 192.168.0.111
Falsch:  192.168.0.1   (Router, nicht Z21!)
Falsch:  192.168.1.111 (falsches Subnetz)
```

**2. WLAN-Verbindung prüfen**
- Ist das **Android-Gerät** im gleichen WLAN wie die **Z21**?
- Router-Einstellung: **"AP Isolation"** deaktiviert?
  - Manche Router isolieren WLAN-Geräte untereinander!

**3. Z21 neustarten**
```
1. Stromversorgung der Z21 trennen
2. 10 Sekunden warten
3. Stromversorgung wieder anschließen
4. 30 Sekunden warten (Bootvorgang)
5. In MOBAsmart erneut verbinden
```

**4. Firewall prüfen**
- Nutzt du eine **Firewall-App** auf Android?
- MOBAsmart muss **UDP Port 21105** nutzen dürfen

---

### Problem: Lap Counter zählen nicht

#### Symptom
- Zug fährt über Gleiskontakt
- Zähler bleibt bei 0 oder erhöht sich nicht

#### Lösungen

**1. Feedback Points korrekt eingestellt?**
```
Anzahl Rückmeldemodule an deiner Anlage:
→ 3 Module = Tracks: 3 einstellen

Wenn falsch eingestellt:
→ Track 1, 2, 3 vorhanden, aber Track 4 wird erwartet
→ Feedbacks gehen verloren!
```

**2. Z21 empfängt Rückmeldungen?**
```
Test mit Z21 App:
1. Z21 App öffnen
2. Menü → Rückmeldungen
3. Zug über Gleiskontakt fahren
4. Leuchtet die LED auf? → Rückmeldung funktioniert
```

**3. Verkabelung prüfen**
```
Rückmeldemodule (Roco 10808):
- Korrekt an Z21 angeschlossen? (RBus)
- Gleiskontakte richtig verkabelt?
- Plus/Minus vertauscht? → Funktioniert trotzdem!
- Kontakte sauber? (Oxidation verhindert Kontakt)
```

**4. App im Vordergrund?**
```
⚠️ WICHTIG: App muss sichtbar sein!
- Display an?
- App nicht im Hintergrund?
- Andere App im Vordergrund? → MOBAsmart wieder öffnen!
```

---

### Problem: Doppelzählungen

#### Symptom
- Zug fährt einmal vorbei
- Zähler erhöht sich um 2, 3 oder 4

#### Lösungen

**1. Timer Filter aktivieren**
```
✅ Checkbox "Timer in s" anhaken
→ Intervall: 10 Sekunden (Standard)
→ Test: Zug langsam vorbeifahren lassen
→ Nur 1 Count? → Problem gelöst!
```

**2. Intervall erhöhen**
```
Langer Zug (>6 Wagen):
→ Intervall: 15-20 Sekunden

Sehr langsame Fahrt:
→ Intervall: 20-30 Sekunden
```

**3. Gleiskontakte überprüfen**
```
Sind mehrere Gleiskontakte zu nah beieinander?
→ Zug aktiviert 2 Kontakte gleichzeitig
→ Lösung: Kontakte weiter auseinander platzieren
```

---

### Problem: App stürzt ab / friert ein

#### Lösungen

**1. App neu starten**
```
1. Task-Switcher öffnen (Quadrat-Symbol)
2. MOBAsmart nach oben wischen (schließen)
3. App-Icon antippen (neu starten)
```

**2. Cache leeren**
```
Android Einstellungen
→ Apps
→ MOBAsmart
→ Speicher
→ Cache leeren
```

**3. App-Daten löschen (⚠️ Einstellungen gehen verloren!)**
```
Android Einstellungen
→ Apps
→ MOBAsmart
→ Speicher
→ Daten löschen
→ App neu starten
```

**4. App neu installieren**
```
1. MOBAsmart deinstallieren
2. Gerät neu starten
3. MOBAsmart neu installieren (Google Play / APK)
```

---

## ❓ FAQ

### Allgemeine Fragen

#### **Funktioniert MOBAsmart mit allen Z21-Varianten?**
✅ **Ja!** Alle Varianten werden unterstützt:
- Z21 (schwarz)
- Z21 start (weiß)
- z21 (klein, weiß)

#### **Brauche ich eine Internetverbindung?**
❌ **Nein!** MOBAsmart kommuniziert **lokal** per UDP mit der Z21. Keine Cloud, keine Internetverbindung nötig.

#### **Kann ich die App offline nutzen?**
✅ **Ja!** Solange Android-Gerät und Z21 im gleichen WLAN sind, funktioniert alles offline.

#### **Werden meine Daten irgendwo hochgeladen?**
❌ **Nein!** Alle Daten bleiben **lokal** auf deinem Gerät. Kein Cloud-Sync, keine Telemetrie.

#### **Kostet die App etwas?**
✅ **Kostenlos!** MOBAsmart ist Open Source (MIT License).

---

### Technische Fragen

#### **Welche Android-Version brauche ich?**
- **Minimum:** Android 7.0 (Nougat)
- **Empfohlen:** Android 10+ (bessere Netzwerk-Performance)

#### **Funktioniert die App im Hintergrund?**
❌ **Nein.** Android beendet die UDP-Verbindung nach ~10 Minuten im Hintergrund. **Lösung:** App im Vordergrund lassen (siehe [Display-Management](#-display-management-lange-sessions)).

#### **Kann ich mehrere Z21 gleichzeitig überwachen?**
❌ **Aktuell nicht.** Die App unterstützt nur **1 Z21-Verbindung** gleichzeitig.

#### **Warum zeigt die App keine Lok-Steuerung?**
💡 **Design-Entscheidung:** MOBAsmart ist auf **Monitoring** fokussiert (Lap Counting, Feedback-Events). Für Lok-Steuerung nutze die **Z21 App** oder **MOBAflow (WinUI)**.

#### **Kann ich die Lap-Counts exportieren?**
⏳ **Geplant!** Export als **CSV** oder **JSON** ist für eine zukünftige Version geplant.

---

### Troubleshooting Fragen

#### **Warum verbindet sich die App nicht?**
Häufigste Ursachen:
1. **Falsche IP-Adresse** → Prüfe in Z21 App
2. **Falsches WLAN** → Android-Gerät im Gast-WLAN?
3. **AP Isolation aktiv** → Router-Einstellung prüfen
4. **Z21 ausgeschaltet** → Stromversorgung prüfen

#### **Warum zählt nur Track 1, aber nicht Track 2/3?**
Mögliche Ursachen:
1. **Falsche Anzahl Tracks** → Setze `Tracks: 3` (nicht 1!)
2. **Rückmeldemodule nicht angeschlossen** → RBus-Verkabelung prüfen
3. **Gleiskontakte defekt** → Mit Z21 App testen

#### **Warum ist die Rundenzeit 00:00.0?**
💡 **Normal!** Die **erste Runde** hat keine Zeit, weil der Startpunkt unbekannt ist. Ab der **zweiten Runde** wird die Zeit gemessen.

---

## 🔧 Technische Details

### UDP-Kommunikation

**Protokoll:** Z21 LAN Protocol (Roco)  
**Port:** 21105 (UDP)  
**Datenrichtung:** Bidirektional (App ↔ Z21)

**Gesendete Befehle:**
- `LAN_GET_SERIAL_NUMBER` → Z21 Seriennummer abfragen
- `LAN_GET_HWINFO` → Hardware-Info abfragen
- `LAN_SYSTEMSTATE_GETDATA` → System-Status abfragen (Polling alle 5s)
- `LAN_SET_TRACK_POWER_ON/OFF` → Gleisspannung ein/aus

**Empfangene Events:**
- `LAN_SYSTEMSTATE_DATACHANGED` → System-Status (Strom, Temperatur)
- `LAN_RMBUS_DATACHANGED` → Rückmeldebus-Ereignis (Feedback!)
- `LAN_X_TURNOUT_INFO` → Weichenstellung (nicht genutzt in MOBAsmart)

### Feedback-Event-Verarbeitung

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

### Datenmodell

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

### Settings-Persistierung

**Speicherort:** `/data/user/0/com.mobaflow.mobasmart/files/appsettings.json`

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

**Auto-Save:** Änderungen werden **sofort** gespeichert (nach jedem `+`/`−` Klick).

---

## 📜 Lizenz & Credits

**MOBAsmart** ist Teil des **MOBAflow**-Projekts.

- **Lizenz:** MIT License
- **Entwickler:** Andreas Hülsmann
- **Repository:** [Azure DevOps](https://dev.azure.com/ahuelsmann/MOBAflow)
- **Version:** 1.0 (Dezember 2025)

### Drittanbieter-Software

- **Roco Z21** - Digital-Zentrale & Protokoll
- **.NET MAUI** - Cross-Platform Framework (Microsoft)
- **CommunityToolkit.Mvvm** - MVVM Framework
- **UraniumUI** - Material Design Controls

Siehe [`THIRD-PARTY-NOTICES.md`](../THIRD-PARTY-NOTICES.md) für vollständige Lizenz-Informationen.

---

## 🤝 Beitragen

**Fehler gefunden? Feature-Wunsch?**

1. **GitHub Issue erstellen:**  
   https://dev.azure.com/ahuelsmann/MOBAflow/_workitems/create/Bug

2. **Pull Request einreichen:**  
   Fork → Feature Branch → Pull Request

3. **Feedback per E-Mail:**  
   *(E-Mail-Adresse einfügen)*

---

## 📖 Weitere Dokumentation

- **User Guide (kompakt):** [`MOBASMART-USER-GUIDE.md`](MOBASMART-USER-GUIDE.md)
- **Architecture:** [`ARCHITECTURE.md`](ARCHITECTURE.md)
- **Contributing:** [`CONTRIBUTING.md`](../CONTRIBUTING.md)

---

**Viel Spaß mit MOBAsmart!** 🚂✨

*Letzte Aktualisierung: 27.12.2025*
