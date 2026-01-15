# MOBAsmart - Benutzerhandbuch

**Version:** 1.0  
**Plattform:** Android  
**Letzte Aktualisierung:** 27.12.2025

---

## 📱 Was ist MOBAsmart?

**MOBAsmart** ist die mobile Android-App für die Überwachung deiner Modellbahnanlage. Sie verbindet sich direkt per UDP mit deiner **Roco Z21 Digital-Zentrale** und zählt automatisch die Runden deiner Züge basierend auf Rückmelde-Ereignissen.

---

## 🚀 Erste Schritte

### 1. Voraussetzungen

- **Android-Gerät** (Android 7.0 oder neuer)
- **Roco Z21 Digital-Zentrale** im gleichen WLAN-Netzwerk
- **Rückmeldemodule** (z.B. Roco 10808) an deiner Anlage angeschlossen

### 2. App installieren

1. Lade die App aus dem Google Play Store herunter *(oder installiere die APK manuell)*
2. Öffne **MOBAsmart**
3. Erteile Netzwerk-Berechtigungen (falls abgefragt)

### 3. Z21 verbinden

1. Gib die **IP-Adresse** deiner Z21 ein (z.B. `192.168.0.111`)
   - **Tipp:** Die IP findest du in der Z21-App unter "Einstellungen"
2. Tippe auf den **Verbindungs-Toggle**
3. Wenn verbunden, erscheint ein **grüner Punkt** oben rechts

✅ **Erfolgreich verbunden**, wenn du die Z21-System-Daten siehst:
- 🌡️ **Temperatur** (z.B. 28°C)
- 🔌 **Versorgungsspannung** (z.B. 16500mV)
- ⚡ **VCC-Spannung** (z.B. 5000mV)

---

## 🎯 Hauptfunktionen

### ⚙️ Einstellungen

#### **Feedback Points (Tracks)**
- **Was ist das?** Anzahl der Rückmeldemodule an deiner Anlage
- **Beispiel:** Wenn du 3 Gleiskontakte hast → Setze auf **3**
- **Wie ändern?** 
  - Tippe **−** oder **+** neben "Tracks"
  - Die App erstellt automatisch 3 separate Zähler (Track 1, Track 2, Track 3)

#### **Target Lap Count**
- **Was ist das?** Ziel-Rundenzahl für alle Gleise
- **Beispiel:** Wenn du 10 Runden fahren möchtest → Setze auf **10**
- **Wie ändern?** 
  - Tippe **−** oder **+** neben "Target"
  - Der **Fortschrittsbalken** zeigt den Fortschritt (z.B. 3/10 = 30%)

#### **Timer Filter**
- **Was ist das?** Verhindert Doppelzählungen bei langen Zügen
- **Warum wichtig?** Ein langer Zug kann einen Gleiskontakt mehrere Sekunden lang auslösen
- **Empfehlung:** 
  - ✅ **Aktiviert** (Checkbox angehakt)
  - **Intervall:** 10 Sekunden (Standard)
  - **Bedeutung:** Innerhalb von 10 Sekunden wird ein Feedback nur 1x gezählt

**Beispiel:**
```
Ohne Timer Filter:
  Zug fährt über Track 1 → Count: 1
  (2 Sekunden später, Zug noch auf Track 1) → Count: 2 ❌ (Doppelzählung!)

Mit Timer Filter (10s):
  Zug fährt über Track 1 → Count: 1
  (2 Sekunden später, Zug noch auf Track 1) → Ignoriert ✅
  (12 Sekunden später, neuer Durchgang) → Count: 2 ✅
```

---

## 📊 Lap Counter (Rundenzähler)

### Anzeige verstehen

Jeder Feedback Point hat seinen eigenen Zähler:

```
┌─────────────────────────────────────────┐
│ [5]  Track 1                            │
│      Lap: 00:12.5  @  22:15:30          │
│      Lap 5/10 ━━━━━━━━━━━░░░░░░  50%   │
└─────────────────────────────────────────┘
```

**Legende:**
- **[5]** → Aktuelle Rundenanzahl
- **Track 1** → Feedback Point Nummer
- **Lap: 00:12.5** → Letzte Rundenzeit (12,5 Sekunden)
- **@ 22:15:30** → Zeitpunkt der letzten Erfassung
- **Lap 5/10** → 5 von 10 Ziel-Runden
- **━━━━━━━━━━━** → Fortschrittsbalken (50%)
- **50%** → Prozentuale Angabe

### Badge-Farben

- **🟦 Blau (Primary):** Noch keine Runde erfasst
- **🟢 Grün (Accent):** Mindestens 1 Runde erfasst

### Zähler zurücksetzen

1. Tippe auf **↻ Reset** (oben rechts im Lap Counter Bereich)
2. Alle Zähler werden auf **0** zurückgesetzt
3. Fortschrittsbalken werden zurückgesetzt

---

## 🔋 Wichtig: App im Vordergrund lassen

### ⚠️ **Warum muss die App geöffnet bleiben?**

**Android schränkt Hintergrund-Aktivitäten ein:**
- Nach ~10 Minuten im Hintergrund trennt Android die Netzwerk-Verbindung
- UDP-Pakete von der Z21 werden nicht mehr empfangen
- **Resultat:** Lap Counts werden **NICHT** aktualisiert

### ✅ **So nutzt du MOBAsmart richtig:**

#### **Option 1: App immer im Vordergrund (EMPFOHLEN)**
1. Starte **MOBAsmart**
2. Verbinde mit Z21
3. **Lasse das Display eingeschaltet** (oder nutze "Display bleibt an"-Funktion)
4. Lege das Handy neben die Anlage

**Vorteile:**
- ✅ Zuverlässige Zählung
- ✅ Echtzeit-Updates
- ✅ Keine verpassten Runden

**Tipp:** Nutze einen Ständer oder lege das Handy so hin, dass du die Zähler sehen kannst!

#### **Option 2: Display-Timeout erhöhen**
1. **Android Einstellungen** → **Display**
2. **Bildschirm-Timeout** → **10 Minuten** (oder länger)
3. Platziere das Handy so, dass du die App siehst

#### **Option 3: "Display bleibt an" (Entwickleroptionen)**
1. **Android Einstellungen** → **Entwickleroptionen**
   - Falls nicht sichtbar: **Über das Telefon** → 7x auf **Build-Nummer** tippen
2. **Entwickleroptionen** → **Display bleibt an**
3. ✅ **Aktivieren**
4. Schließe Ladegerät an (wegen Akku!)

**⚠️ Vorsicht:** Hoher Akkuverbrauch! Nur mit Ladegerät nutzen.

---

## 🔌 Gleispower (Track Power)

### An/Aus schalten

1. **Track Power Toggle** → Schaltet Gleisspannung der Z21 ein/aus
2. **Status:**
   - 🟡 **Gelb (Warning):** Track Power ist **AN** (Züge fahren)
   - ⚫ **Grau:** Track Power ist **AUS** (Züge stehen)

### Wann ausschalten?

- ✅ **Nach dem Fahrbetrieb** (spart Energie)
- ✅ **Bei Wartungsarbeiten** (Sicherheit!)
- ✅ **Bei längeren Pausen**

---

## 🛠️ Problemlösung

### Problem: Keine Verbindung zur Z21

**Lösung:**
1. **Prüfe IP-Adresse:**
   - Z21-App öffnen → Einstellungen → IP-Adresse notieren
   - In MOBAsmart eingeben (z.B. `192.168.0.111`)
2. **Prüfe WLAN:**
   - Handy **im gleichen Netzwerk** wie Z21?
   - Router-Einstellungen: "AP Isolation" deaktiviert?
3. **Z21 neustarten:**
   - Stromversorgung kurz trennen (10 Sekunden warten)

### Problem: Lap Counter zählen nicht

**Lösung:**
1. **Feedback Points korrekt eingestellt?**
   - Anzahl Tracks = Anzahl Rückmeldemodule?
2. **Z21 empfängt Rückmeldungen?**
   - Teste mit Z21-App: "Rückmeldungen" anzeigen lassen
3. **Timer Filter zu kurz?**
   - Erhöhe Intervall auf **15 Sekunden**
4. **App im Vordergrund?**
   - Siehe [App im Vordergrund lassen](#-wichtig-app-im-vordergrund-lassen)

### Problem: Doppelzählungen

**Lösung:**
1. **Timer Filter aktivieren:**
   - ✅ Checkbox "Timer in s" anhaken
2. **Intervall erhöhen:**
   - Lange Züge? → **15-20 Sekunden**
   - Kurze Züge? → **5-10 Sekunden**
3. **Rückmeldemodule prüfen:**
   - Sind Gleiskontakte zu nah beieinander?
   - Gleiskontakte richtig angeschlossen?

---

## 📸 Foto-Upload zu MOBAflow (Windows)

MOBAsmart kann Fotos direkt an die MOBAflow Desktop-App senden. Damit dies funktioniert, muessen Handy und PC im **gleichen Netzwerk** sein und die **Windows Firewall** muss korrekt konfiguriert sein.

### Netzwerk-Voraussetzungen

| Anforderung | Details |
|-------------|---------|
| **Gleiches Netzwerk** | Handy und Windows-PC muessen im selben WLAN sein |
| **Kein VPN aktiv** | Firmennetzwerk/VPN verhindert die Verbindung! |
| **Kein "AP Isolation"** | Im Router muss Geraete-Kommunikation erlaubt sein |

### Windows Firewall konfigurieren

MOBAflow benoetigt zwei Firewall-Freigaben:

| Dienst | Protokoll | Port | Zweck |
|--------|-----------|------|-------|
| REST API | **TCP** | 5001 | Foto-Upload |
| Discovery | **UDP** | 21106 | Automatische Erkennung |

#### Firewall-Regeln erstellen (PowerShell als Administrator):

```powershell
# TCP fuer REST API (Foto-Upload)
New-NetFirewallRule -DisplayName "MOBAflow REST API" -Direction Inbound -Protocol TCP -LocalPort 5001 -Action Allow -Profile Private,Public

# UDP fuer Discovery (automatische Erkennung)
New-NetFirewallRule -DisplayName "MOBAflow Discovery" -Direction Inbound -Protocol UDP -LocalPort 21106 -Action Allow -Profile Private,Public
```

#### Alternative: Manuell ueber Windows 11 Einstellungen

**Schritt 1: Windows Defender Firewall oeffnen**
1. Druecke `Win + I` um **Einstellungen** zu oeffnen
2. Gehe zu **Datenschutz und Sicherheit** → **Windows-Sicherheit**
3. Klicke auf **Firewall- und Netzwerkschutz**
4. Scrolle nach unten und klicke auf **Erweiterte Einstellungen**
   - *(Alternativ: `Win + R`, dann `wf.msc` eingeben)*

**Schritt 2: Neue eingehende Regel erstellen (TCP 5001)**
1. Klicke links auf **Eingehende Regeln**
2. Klicke rechts auf **Neue Regel...**
3. Regeltyp: **Port** auswaehlen → **Weiter**
4. Protokoll: **TCP** auswaehlen
5. Ports: **Bestimmte lokale Ports** → `5001` eingeben → **Weiter**
6. Aktion: **Verbindung zulassen** → **Weiter**
7. Profil: ☑️ **Domäne**, ☑️ **Privat**, ☑️ **Öffentlich** → **Weiter**
8. Name: `MOBAflow REST API` → **Fertig stellen**

**Schritt 3: Zweite Regel erstellen (UDP 21106)**
1. Wiederhole Schritt 2, aber waehle:
   - Protokoll: **UDP**
   - Port: `21106`
   - Name: `MOBAflow Discovery`

**Ergebnis pruefen:**
Nach Abschluss solltest du zwei neue Regeln sehen:
```
✅ MOBAflow REST API      (TCP 5001)
✅ MOBAflow Discovery     (UDP 21106)
```

> **💡 Tipp:** Die Regeln werden sofort aktiv - kein Neustart erforderlich!

### Troubleshooting Foto-Upload

#### Discovery funktioniert nicht (Handy findet PC nicht)

**Ursachen:**
- VPN/Firmennetzwerk aktiv -> **VPN trennen**
- Router blockiert Multicast -> **AP Isolation deaktivieren**
- Falsches Netzwerk-Profil -> Firewall-Regel fuer "Private" UND "Public" erstellen

**Test:** Kann das Handy die IP des PCs anpingen?

#### Upload-Timeout / Verbindung fehlgeschlagen

**Ursachen:**
- Firewall-Regel fehlt oder falsch -> **TCP** (nicht UDP!) fuer Port 5001
- MOBAflow nicht gestartet -> WinUI-App muss laufen
- Falscher Port -> REST API laeuft auf Port **5001**

**Test am PC (PowerShell):**
```powershell
# Pruefen ob Port 5001 lauscht
netstat -an | Select-String ":5001"

# Sollte zeigen: TCP 0.0.0.0:5001 LISTENING
```

#### Handy und PC in verschiedenen Netzwerken

**Symptom:** Discovery findet nichts, manueller Upload schlaegt fehl

**Pruefen:**
- PC: `ipconfig` -> IPv4-Adresse notieren (z.B. 192.168.1.100)
- Handy: Einstellungen -> WLAN -> IP-Adresse (z.B. 192.168.1.xxx)
- **Gleiche Netzwerk-ID?** (192.168.1.x vs 192.168.1.x = OK)

**Typische Probleme:**
- PC via Ethernet (192.168.0.x), Handy via WLAN (192.168.1.x) -> **Verschiedene Subnetze!**
- PC mit VPN verbunden -> VPN hat eigenes Subnetz

---

### Problem: App stuerzt ab / friert ein

**Loesung:**
1. **App neu starten:**
   - Task-Switcher -> MOBAsmart schliessen -> Neu oeffnen
2. **Cache leeren:**
   - Android Einstellungen -> Apps -> MOBAsmart -> Speicher -> Cache leeren
3. **App neu installieren:**
   - Deinstallieren -> Neu installieren (Einstellungen bleiben erhalten!)

---

## 💡 Tipps & Tricks

### 🎯 **Optimale Einstellungen für Racing**

**Szenario:** 3 Züge fahren Rennen, 10 Runden

```
✅ Tracks: 3
✅ Target: 10
✅ Timer Filter: Aktiviert
✅ Intervall: 8 Sekunden (schnelle Züge)
```

**Warum?** 
- 3 separate Zähler (ein Zug pro Track)
- 10 Runden → Fortschritt gut sichtbar (10%, 20%, ...)
- 8 Sekunden → Verhindert Doppelzählungen bei schnellen Durchgängen

### 🚂 **Optimale Einstellungen für Automatik-Betrieb**

**Szenario:** 1 Zug fährt automatisch im Kreis

```
✅ Tracks: 1 (nur ein Gleiskontakt)
✅ Target: 50 (lange Session)
✅ Timer Filter: Aktiviert
✅ Intervall: 15 Sekunden (langsamer Zug)
```

**Warum?**
- 1 Zähler ausreichend (nur ein Gleiskontakt nötig)
- 50 Runden → Kann stundenlang laufen
- 15 Sekunden → Sicher gegen Doppelzählungen

### 📱 **Display-Management**

**Problem:** Akku leert sich zu schnell

**Lösung:**
1. Reduziere **Display-Helligkeit** auf 50%
2. Nutze **Nachtmodus** (Dark Theme automatisch aktiv)
3. Schließe **Ladegerät** an (bei langen Sessions)

---

## 📊 Beispiel-Szenario: Rennen mit 3 Zügen

### Setup
- **3 Rückmeldemodule** (Roco 10808) an der Strecke
- **3 Züge** (ICE, TGV, Railjet)
- **Ziel:** Wer erreicht als erstes 10 Runden?

### Konfiguration in MOBAsmart

1. **Verbinde mit Z21**
   - IP-Adresse eingeben → Toggle aktivieren
2. **Einstellungen:**
   - Tracks: **3**
   - Target: **10**
   - Timer: **Aktiviert**, **10 Sekunden**
3. **Reset** → Zähler zurücksetzen
4. **Track Power** → Einschalten
5. **Züge starten** (über Z21-App oder Handregler)

### Rennen beobachten

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

**Sieger:** Track 2 (TGV) erreicht als erstes 10/10! 🏆

---

## 🔒 Datenschutz & Berechtigungen

### Erforderliche Berechtigungen

| Berechtigung | Grund |
|--------------|-------|
| **Internet** | UDP-Kommunikation mit Z21 |
| **Netzwerkstatus** | WLAN-Verbindung prüfen |

### Was wird NICHT gesammelt?

- ❌ Keine persönlichen Daten
- ❌ Keine Standortdaten
- ❌ Keine Nutzungsstatistiken
- ❌ Keine Cloud-Verbindung

**Alle Daten bleiben lokal auf deinem Gerät!**

---

## 📞 Support & Feedback

### Probleme melden

**GitHub Issues:**  
https://dev.azure.com/ahuelsmann/MOBAflow/_git/MOBAflow

**E-Mail:**  
andreas.huelsmann@example.com *(bitte durch echte E-Mail ersetzen!)*

### Feature-Wünsche

Wir freuen uns über Feedback! 🎉

Teile uns mit:
- Was fehlt dir in der App?
- Welche Funktionen würdest du gerne sehen?
- Was könnte verbessert werden?

---

## 📜 Lizenz & Credits

**MOBAsmart** ist Teil des **MOBAflow**-Projekts.

- **Lizenz:** MIT License
- **Entwickler:** Andreas Huelsmann
- **Version:** 1.0 (Dezember 2025)

**Drittanbieter-Software:**
- Roco Z21 Digital-Zentrale (Kommunikationsprotokoll)
- .NET MAUI (Microsoft)

Siehe [`THIRD-PARTY-NOTICES.md`](../THIRD-PARTY-NOTICES.md) für Details.

---

## 🎯 Fazit

**MOBAsmart** macht das Zählen von Runden kinderleicht! 🚂

**Viel Spaß beim Fahren!** 🎉
