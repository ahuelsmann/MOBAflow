---
description: 'MOBAflow offene Aufgaben'
applyTo: '**'
---

# MOBAflow TODOs

> Letzte Aktualisierung: 2025-01-31 (Session 5 abgeschlossen - Train Control: Dynamische Tacho-Skalierung mit DCC Speed Steps)

---

## 🎯 SESSION 5 ABGESCHLOSSEN ✅

### Train Control: Dynamische Tacho-Skalierung

**DCC Speed Steps Konfiguration (Common.Configuration)**
- ✅ `DccSpeedSteps` Enum erstellt (14, 28, 128 Steps)
- ✅ `TrainControlSettings` erweitert um SpeedSteps Property
- ✅ Persistence in AppSettings integriert

**TrainControlViewModel erweitert (SharedUI.ViewModel)**
- ✅ `SpeedSteps` Property mit `[ObservableProperty]`
- ✅ `MaxSpeedStep` Property berechnet (13, 27, 126)
- ✅ `SpeedKmh` Berechnung korrigiert: `(Speed / MaxSpeedStep) * Vmax`
- ✅ Laden/Speichern in Settings

**SpeedometerControl: Doppel-Ring-Anzeige (WinUI.Controls)**
- ✅ Hardcodierte Markierungen entfernt
- ✅ **Äußerer Ring (km/h):** `RenderKmhMarkers()` - dynamisch basierend auf `VmaxKmh`
  - 5 Marker: 0%, 25%, 50%, 75%, 100% von Vmax
  - Primäre Farbe, MAX-Marker in Rot
- ✅ **Innerer Ring (Steps):** `RenderSpeedStepMarkers()` - dynamisch basierend auf `SpeedSteps`
  - 14 Steps: 0, 3, 7, 10, 13
  - 28 Steps: 0, 7, 14, 21, 27
  - 128 Steps: 0, 32, 63, 95, 126
  - Accent-Farbe, kleinere Schrift, leicht transparent
- ✅ `VmaxKmh` DependencyProperty für km/h-Anzeige
- ✅ `MaxValue` ist jetzt `MaxSpeedStep` (nicht Vmax!)

**TrainControlPage UI (WinUI.View)**
- ✅ ComboBox für Speed Steps Auswahl (14/28/128)
- ✅ `UpdateSpeedometerScale()` setzt `MaxValue` und `VmaxKmh`
- ✅ Automatische Updates bei Vmax- oder SpeedSteps-Änderung
- ✅ Settings-Persistence

**Korrekte Skalierung implementiert:**
```
14 Steps:  Schaltstufe 13  → Vmax km/h
28 Steps:  Schaltstufe 27  → Vmax km/h
128 Steps: Schaltstufe 126 → Vmax km/h

Formel: km/h = (CurrentStep / MaxStep) × Vmax
```

**Beispiel (BR 103, Vmax 200 km/h, 128 Steps):**
```
Äußerer Ring (km/h):    0 — 50 — 100 — 150 — 200
Innerer Ring (Steps):   0 — 32 —  63 —  95 — 126
                        ↕    ↕     ↕     ↕     ↕
Schaltstufe 63 → 100 km/h ✓
Schaltstufe 126 → 200 km/h ✓
```

---

## 🎯 SESSION 4 ABGESCHLOSSEN ✅

### Port-Visualisierung Refaktorierung

**Striche statt Kreise (TrackPlan.Renderer)**
- ✅ `DrawPortStroke()` Hilfsmethode implementiert
  - Ersetzt Kreis-Visualisierung durch senkrechte Striche (20px)
  - Striche stehen im rechten Winkel zur Fahrtrichtung
  - Labels positioniert 25px neben dem Strich
  
**Farbcodierung für alle Port-Typen**
- ✅ `GetPortColor()` Hilfsfunktion hinzugefügt
  - Port A = schwarz (physisch)
  - Port B = rot (physisch)
  - Port C = grün (WR only)
  - Port D = blau (für zukünftige Typen)
- ✅ Unabhängig von Entry-Richtung: Ports behalten ihre physischen Farben

**Alle 9 Render-Methoden aktualisiert**
- ✅ RenderWR() - 3 Ports mit physischen Farben
- ✅ RenderR9() - Port-Labels basierend auf entryPort
- ✅ RenderR1(), RenderR2(), RenderR3(), RenderR4() - Kurvengleise mit Strich-Visualisierung
- ✅ RenderG239(), RenderG231(), RenderG62() - Gerade-Gleise mit Strich-Visualisierung

**Tests & Build**
- ✅ Komplexer Test-Fall: WR mit 3 Branches (G239, G62, R9s)
- ✅ Build erfolgreich mit allen 9 Gleistypen
- ✅ Striche zentriert auf Port-Positionen, keine Versetzung

**Offene Fragestellung**
- ⏳ Port-Strich-Positionierung bei Verbindungen prüfen
  - Aktuell: Striche zentriert auf Port-Punkt (können überlappen wenn verbunden)
  - Benutzer prüft noch optimale Lösung für kante-an-kante Positionierung

---

## 📚 Piko A Gleissystem - Offizielle Dokumentation

**Quelle:** `docs/99556__A-Gleis_Prospekt_2019.pdf` (Offizieller Piko A Prospekt 2019)

**Vollständige Gleistypen in Piko A:**
- WR (Weiche rechts)
- R1, R2, R3, R4, R9 (Kurvengleise mit verschiedenen Krümmungen)
- G62, G231, G239 (Gerade Gleise)

---

## 🎯 SESSION 3 ABGESCHLOSSEN ✅

### Implementierung & Fehlerfix

**TrackPlanResult erweitert (TrackLibrary.PikoA)**
- ✅ `PortConnection` Record hinzugefügt für Port-Verbindungen
- ✅ `Connections` Liste in TrackPlanResult exportiert
- ✅ `Create()` exportiert jetzt alle Verbindungen mit GUIDs

**TrackPlanSvgRenderer refaktoriert (TrackPlan.Renderer)**
- ✅ Komplett neues Design: Connection-basiertes Rendering statt sequenziell
- ✅ `RenderSegmentRecursive()` für Depth-First Traversal
- ✅ `FindFirstSegment()` - findet Segment ohne eingehende Verbindung
- ✅ `ExtractPortChar()` - Hilfsmethode für Port-Namen
- ✅ `CalculateWRPortPosition()` - berechnet Positions-Offsets pro Port
- ✅ Entry-Port-Bestimmung basierend auf `TargetPort` der Verbindung
- ✅ Mehrere Branches pro Port werden korrekt gerendert
- ✅ R9 mit dynamischen Port-Labels: Entry A/B → Label A/B korrekt

**Bugfixes**
- ✅ FromA.ToB<R9>() - R9 wird jetzt mit korrektem Port verbunden
- ✅ FromC.ToA<R9>() - Zweite R9 wird jetzt an Port C gerendert
- ✅ Port C Label (grün) jetzt sichtbar bei WR
- ✅ Entry-Port-Logik korrigiert: `incomingConnection.TargetPort` statt falsche Annahmen

**Tests (Test.TrackPlanRenderer.RendererTests)**
- ✅ `TrackPlan()` Test mit komplexem Szenario: WR + 3 × R9
- ✅ FromA → ToB: Erste R9
- ✅ FromC → ToA → FromB → ToA: Zweite + Dritte R9
- ✅ Rendering validiert

---

## 📋 BACKLOG (NÄCHSTE SESSIONS)

### 1. Train Control - 4-Bereiche Layout (UPCOMING)
- [ ] Mittlere Spalte in 4 Bereiche aufteilen
  - [ ] Bereich 1: Speedometer (25% Höhe)
  - [ ] Bereich 2: Letzter Haltepunkt (Journey Info)
  - [ ] Bereich 3: Aktueller Haltepunkt (Journey Info)
  - [ ] Bereich 4: Nächster Haltepunkt (Journey Info)
- [ ] `JourneyStationControl` erstellen
  - [ ] Vertikale Darstellung
  - [ ] Station Name, Ankunft/Abfahrt, Gleis
  - [ ] Kompaktes Design
- [ ] Integration in TrainControlPage
- [ ] Responsive Layout-Tests

### 2. Port-Strich-Positionierung (TrackPlan)
- [ ] Lösung für überlappungsfreie Strich-Positionierung bei Verbindungen
  - Optionen: Versetzung, separate Verbindungslinie, andere Strategie
  - User formuliert noch konkrete Anforderung

### 3. Zusätzliche Gleistypen (👤 BENUTZER: Domain-Klassen)

**Hinweis:** Die folgenden Typen wurden als möglich angenommen, müssen aber gegen offizielle Piko A Dokumentation validiert werden:
- [ ] Weitere Kurvengleise (falls in Piko A dokumentiert)
- [ ] Weitere Weichen-Typen (falls in Piko A dokumentiert)

**Aktuell implementiert (9 Gleistypen):** WR, R1-R4, R9, G62, G231, G239

### 4. Persistenz (JSON Serialisierung)
- [ ] TrackPlanResult zu JSON serialisieren
- [ ] JSON zu TrackPlanResult deserialisieren
- [ ] Versionierung für TrackPlan-Format
- [ ] File-Dialog zum Speichern/Laden

### 5. UI Integration (NACH Tests abgeschlossen)
- [ ] **WinUI**: Interactive TrackPlan Editor
- [ ] **MAUI**: Mobile TrackPlan Viewer
- [ ] **Blazor**: Web-basierter TrackPlan Planner
- [ ] Drag-and-Drop Gleis-Platzierung
- [ ] Live-Preview während Bearbeitung
- [ ] Export: PDF, PNG, SVG

### 6. Visualisierung Erweiterungen
- [ ] 3D-Rendering (Three.js / Babylon.js)
- [ ] Höhenangaben für Gleise
- [ ] Schattierungen / Texturen
- [ ] Animation: Lok-Bewegung entlang Pfad

### 7. Performance & Qualität
- [ ] Unit-Tests für Edge-Cases (ungültige Verbindungen, etc.)
- [ ] Performance-Test für große TrackPläne (1000+ Gleise)
- [ ] SVG-Optimierung (Path-Zusammenfassung, etc.)

---

## 📚 Dokumentation

**Train Control:**
- ✅ XML-Comments in Common/Configuration/DccSpeedSteps
- ✅ XML-Comments in SharedUI/ViewModel/TrainControlViewModel
- ✅ XML-Comments in WinUI/Controls/SpeedometerControl
- ✅ Doppel-Ring-Rendering dokumentiert (km/h + Steps)
- ✅ Dynamische Skalierung erklärt

**TrackPlan:**
- ✅ XML-Comments in TrackLibrary.PikoA/TrackPlan.cs
- ✅ XML-Comments in TrackPlan.Renderer/TrackPlanSvgRenderer.cs (komplett neugeschrieben)
- ✅ Connection-basiertes Rendering dokumentiert
- ✅ Entry-Port-Logik erklärt
- ✅ Port-Strich-Visualisierung dokumentiert
- ✅ Test-Beispiele in Test/TrackPlanRenderer/RendererTests.cs
- ✅ Offizielle Piko A Dokumentation: `docs/99556__A-Gleis_Prospekt_2019.pdf`

**Architektur-Übersicht (TrackPlan):**
```
TrackPlanBuilder (Fluent API)
    ↓
TrackPlanResult (Segments + Connections)
    ↓
TrackPlanSvgRenderer (Connection-basiert, Striche-Visualisierung)
    ↓
SVG-Output
```

**Architektur-Übersicht (Train Control):**
```
TrainControlViewModel (SpeedSteps, MaxSpeedStep, SpeedKmh)
    ↓
SpeedometerControl (MaxValue=MaxSpeedStep, VmaxKmh)
    ↓
Doppel-Ring Rendering:
  - Äußerer Ring: km/h (0 - Vmax)
  - Innerer Ring: Steps (0 - MaxSpeedStep)
```

---

## 🚀 Nächste Prioritäten

1. **4-Bereiche Layout** - Train Control mit Journey-Info erweitern
2. **Port-Strich-Positionierung klären** - User definiert optimale Lösung
3. **Domain-Klassen erweitern** (nur wenn in Piko A dokumentiert)
4. **Renderer erweitern** für ggf. neue Gleistypen
5. **Persistenz-Schicht** (JSON Serialisierung)
6. **UI Integration** (WinUI, MAUI, Blazor - nur nach Tests!)

---

## 📌 Wichtige Hinweise

**Train Control:**
- **Doppel-Ring-Anzeige**: Äußerer Ring km/h, innerer Ring DCC Steps
- **Dynamische Skalierung**: MaxSpeedStep ändert sich mit SpeedSteps (13/27/126)
- **Korrekte Berechnung**: km/h = (Step / MaxSpeedStep) × Vmax
- **Persistence**: Settings werden automatisch gespeichert

**TrackPlan:**
- **Striche sind zentriert**: Auf Port-Positionen, können bei Verbindungen überlappen
- **Physische Port-Farben**: Unabhängig von Entry-Richtung konsistent
- **9 Gleistypen**: WR, R9, R1-R4, G62, G231, G239 vollständig unterstützt
- **Tests funktionieren**: Komplexer Test-Fall validiert mehrzeilige Rendering
- **Piko A Dokumentation**: `99556__A-Gleis_Prospekt_2019.pdf` ist offizielle Quelle für Gleistypen






