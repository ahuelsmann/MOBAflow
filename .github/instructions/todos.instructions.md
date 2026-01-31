---
description: 'MOBAflow offene Aufgaben'
applyTo: '**'
---

# MOBAflow TODOs

> Letzte Aktualisierung: 2025-01-31 (Session 4 abgeschlossen - Port-Visualisierung mit Strichen + Farbcodierung)

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

### 1. Port-Strich-Positionierung (NÄCHSTER STEP)
- [ ] Lösung für überlappungsfreie Strich-Positionierung bei Verbindungen
  - Optionen: Versetzung, separate Verbindungslinie, andere Strategie
  - User formuliert noch konkrete Anforderung

### 2. Zusätzliche Gleistypen (👤 BENUTZER: Domain-Klassen)

**Reserviert für Domain-Layer Implementierung:**
- [ ] `R10` (Kurvengleis 10°) Domain-Klasse
- [ ] `R12` (Kurvengleis 12°) Domain-Klasse
- [ ] `R15` (Kurvengleis 15°) Domain-Klasse
- [ ] `WL` (Linksweiche) Domain-Klasse
- [ ] Renderer-Methoden für alle neuen Typen

### 3. Persistenz (JSON Serialisierung)
- [ ] TrackPlanResult zu JSON serialisieren
- [ ] JSON zu TrackPlanResult deserialisieren
- [ ] Versionierung für TrackPlan-Format
- [ ] File-Dialog zum Speichern/Laden

### 4. UI Integration (NACH Tests abgeschlossen)
- [ ] **WinUI**: Interactive TrackPlan Editor
- [ ] **MAUI**: Mobile TrackPlan Viewer
- [ ] **Blazor**: Web-basierter TrackPlan Planner
- [ ] Drag-and-Drop Gleis-Platzierung
- [ ] Live-Preview während Bearbeitung
- [ ] Export: PDF, PNG, SVG

### 5. Visualisierung Erweiterungen
- [ ] 3D-Rendering (Three.js / Babylon.js)
- [ ] Höhenangaben für Gleise
- [ ] Schattierungen / Texturen
- [ ] Animation: Lok-Bewegung entlang Pfad

### 6. Performance & Qualität
- [ ] Unit-Tests für Edge-Cases (ungültige Verbindungen, etc.)
- [ ] Performance-Test für große TrackPläne (1000+ Gleise)
- [ ] SVG-Optimierung (Path-Zusammenfassung, etc.)

---

## 📚 Dokumentation

**Verfügbare Dokumentation:**
- ✅ XML-Comments in TrackLibrary.PikoA/TrackPlan.cs
- ✅ XML-Comments in TrackPlan.Renderer/TrackPlanSvgRenderer.cs (komplett neugeschrieben)
- ✅ Connection-basiertes Rendering dokumentiert
- ✅ Entry-Port-Logik erklärt
- ✅ Port-Strich-Visualisierung dokumentiert
- ✅ Test-Beispiele in Test/TrackPlanRenderer/RendererTests.cs

**Architektur-Übersicht:**
```
TrackPlanBuilder (Fluent API)
    ↓
TrackPlanResult (Segments + Connections)
    ↓
TrackPlanSvgRenderer (Connection-basiert, Striche-Visualisierung)
    ↓
SVG-Output
```

---

## 🚀 Nächste Prioritäten

1. **Port-Strich-Positionierung klären** - User definiert optimale Lösung
2. **Domain-Klassen implementieren** (R10, R12, R15, WL)
3. **Renderer erweitern** für neue Gleistypen
4. **Persistenz-Schicht** (JSON Serialisierung)
5. **UI Integration** (WinUI, MAUI, Blazor - nur nach Tests!)

---

## 📌 Wichtige Hinweise

- **Striche sind zentriert**: Auf Port-Positionen, können bei Verbindungen überlappen
- **Physische Port-Farben**: Unabhängig von Entry-Richtung konsistent
- **9 Gleistypen**: WR, R9, R1-R4, G62, G231, G239 vollständig unterstützt
- **Tests funktionieren**: Komplexer Test-Fall validiert mehrzeilige Rendering






