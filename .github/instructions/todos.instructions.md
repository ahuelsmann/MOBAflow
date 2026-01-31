---
description: 'MOBAflow offene Aufgaben'
applyTo: '**'
---

# MOBAflow TODOs

> Letzte Aktualisierung: 2025-01-30 (Session 3 abgeschlossen - Connection-basiertes Rendering + Fehlerfix)

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

### 1. Zusätzliche Gleistypen (👤 BENUTZER: Domain-Klassen)

**Reserviert für Domain-Layer Implementierung:**
- [ ] `R10` (Kurvengleis 10°) Domain-Klasse
  - Erbt von `Curved`
  - `ArcInDegree = 10`
  - `RadiusInMm = 1360`
  - Ports: A (Eingang), B (Ausgang)
  
- [ ] `R12` (Kurvengleis 12°) Domain-Klasse
  - Erbt von `Curved`
  - `ArcInDegree = 12`
  - `RadiusInMm = 1130`
  
- [ ] `R15` (Kurvengleis 15°) Domain-Klasse
  - Erbt von `Curved`
  - `ArcInDegree = 15`
  - `RadiusInMm = 908`
  - (Gleich wie R9, aber für Übersicht separat)

- [ ] `WL` (Linksweiche) Domain-Klasse
  - Erbt von `SwitchLeft`
  - `LengthInMm = 239`
  - `RadiusInMm = 908`
  - `ArcInDegree = 15`
  - Ports: A (Eingang), B (Gerade), C (Kurve links statt rechts)

**Nach Domain-Implementierung:**
- [ ] Renderer-Methoden hinzufügen (`RenderR10()`, `RenderWL()`, etc.)
- [ ] Segment-Registry für dynamische Renderer (Reflection Pattern)

### 2. Persistenz (JSON Serialisierung)
- [ ] TrackPlanResult zu JSON serialisieren
- [ ] JSON zu TrackPlanResult deserialisieren
- [ ] Versionierung für TrackPlan-Format
- [ ] File-Dialog zum Speichern/Laden

### 3. UI Integration (NACH Tests abgeschlossen)
- [ ] **WinUI**: Interactive TrackPlan Editor
- [ ] **MAUI**: Mobile TrackPlan Viewer
- [ ] **Blazor**: Web-basierter TrackPlan Planner
- [ ] Drag-and-Drop Gleis-Platzierung
- [ ] Live-Preview während Bearbeitung
- [ ] Export: PDF, PNG, SVG

### 4. Visualisierung Erweiterungen
- [ ] 3D-Rendering (Three.js / Babylon.js)
- [ ] Höhenangaben für Gleise
- [ ] Schattierungen / Texturen
- [ ] Animation: Lok-Bewegung entlang Pfad

### 5. Performance & Qualität
- [ ] Unit-Tests für Edge-Cases (ungültige Verbindungen, etc.)
- [ ] Performance-Test für große TrackPläne (1000+ Gleise)
- [ ] SVG-Optimierung (Path-Zusammenfassung, etc.)

---

## 📚 Dokumentation

**Verfügbare Dokumentation:**
- ✅ XML-Comments in TrackLibrary.PikoA/TrackPlan.cs (erweitert)
- ✅ XML-Comments in TrackPlan.Renderer/TrackPlanSvgRenderer.cs (komplett neugeschrieben)
- ✅ Connection-basiertes Rendering dokumentiert
- ✅ Entry-Port-Logik erklärt
- ✅ Test-Beispiele in Test/TrackPlanRenderer/RendererTests.cs

**Architektur-Übersicht:**
```
TrackPlanBuilder (Fluent API)
    ↓
TrackPlanResult (Segments + Connections)
    ↓
TrackPlanSvgRenderer (Connection-basiert)
    ↓
SVG-Output
```

---

## 🚀 Nächste Prioritäten

1. **Domain-Klassen implementieren** (R10, R12, R15, WL)
2. **Renderer-Methoden für neue Gleistypen** (nach Domain fertig)
3. **Persistenz-Schicht** (JSON Serialisierung)
4. **UI Integration** (WinUI, MAUI, Blazor - nur nach Tests!)

---

## 📌 Wichtige Hinweise

- **Domain ist Benutzer-Aufgabe**: Neue Gleistypen gehören ins Domain Layer
- **Tests ERST**: UI-Integration kommt erst nach abgeschlossenen Tests
- **Renderer ist erweiterbar**: Segment-Registry Pattern für dynamische Renderer verwenden
- **Connection-basiertes Design**: Keine sequenzielle Verarbeitung mehr!

---






