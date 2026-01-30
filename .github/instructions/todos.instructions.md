---
description: 'MOBAflow offene Aufgaben'
applyTo: '**'
---

# MOBAflow TODOs

> Letzte Aktualisierung: 2025-01-30 (Session 2 abgeschlossen - TrackPlan Fluent Builder + SVG Renderer dokumentiert)

---

## 🎯 SESSION 2 ABGESCHLOSSEN ✅

### Erkenntnisse & Implementierung

**TrackPlan Fluent Builder (TrackLibrary.PikoA)**
- ✅ `TrackPlanResult` als Immutable-Container: `Segments` + `StartAngleDegrees`
- ✅ `TrackPlanBuilder` mit Fluent API für Gleis-Verkettung
- ✅ `.Start(angle)` für Winkel-Konfiguration (0°=rechts, 90°=oben, 180°=links, 270°=unten)
- ✅ Generische `TrackBuilder<T>` und `PortBuilder` für Port-Verbindungen
- ✅ Unterstützung mehrerer paralleler Pfade via `.Connections(branches)`
- ✅ Vollständig dokumentiert mit XML-Comments

**SVG Renderer (TrackPlan.Renderer)**
- ✅ `TrackPlanSvgRenderer` rendert TrackPlanResult zu W3C-Standard SVG
- ✅ Automatische Bounds-Berechnung während Rendering
- ✅ `viewBox` für responsive Skalierung (berechnet aus echtem Inhalt)
- ✅ 50px Padding um alle Elemente
- ✅ Segment-spezifische Renderer: `RenderWR()`, `RenderR9()`
- ✅ Automatische Entry-Port-Bestimmung basierend auf Segment-Verbindungen
- ✅ Port-Farbcodierung: schwarz=A, rot=B, grün=C
- ✅ Backward-Compatibility via `[Obsolete]` Overload für alte API
- ✅ Vollständig dokumentiert mit XML-Comments

**Tests (Test.TrackPlanRenderer.RendererTests)**
- ✅ `TrackPlan1`: Lineare Verkettung WR→R9→R9
- ✅ `TrackPlan2`: Parallele Pfade (Connections API)
- ✅ `TrackPlan3`: Rendering mit `.Start(0)` - einzelnes WR mit SVG Export

### Technische Details

**Port-Struktur:**
- WR: Port A (Eingang, 0,0), Port B (Gerade, 239mm), Port C (Kurve, R=908mm, Arc=15°)
- R9: Port A (Eingang), Port B (Ausgang)
- Entry-Port automatisch: Wenn vorheriges Segment auf Port B endet → Entry ist B, sonst A
- Kurvenrichtung: Entry A→links, Entry B→rechts

**Koordinatensystem:**
- Start: (0, 0)
- Start-Winkel: 0°=rechts, 90°=oben, 180°=links, 270°=unten
- Zeichnung: Linien + Kreisbögen + Punkte (Ports) + Beschriftung (Port-Name)
- Bounding-Box: Sammlung während Rendering, Berechnung in `BuildSvg()`

**SVG Output:**
```xml
<svg width="500" height="400" viewBox="-50 -50 500 400" xmlns="...">
  <circle cx="0" cy="0" r="10" fill="black" />
  <text ... fill="black">A</text>
  <path d="M 0,0 L 239,0" stroke="#333" stroke-width="4" fill="none" />
  ...
</svg>
```

---

## 📋 BACKLOG (NÄCHSTE SESSION)

### 1. Zusätzliche Gleistypen
- [ ] `R10` (Kurvengleis 10°) implementieren
- [ ] `WL` (Linksweiche) implementieren
- [ ] `WR` mit mehr Ports (PortD) nutzen
- [ ] Segment-Registry für dynamische Renderer

### 2. Editor-Features
- [ ] Interactive TrackPlan Editor (WinUI/MAUI/Blazor)
- [ ] Drag-and-Drop Gleis-Platzierung
- [ ] Live-Preview während Bearbeitung
- [ ] Export zu verschiedenen Formaten (PDF, PNG, etc.)

### 3. Persistenz
- [ ] TrackPlan zu JSON serialisieren/deserialisieren
- [ ] Speichern/Laden von Track-Plänen
- [ ] Versioning für Track-Plan-Format

### 4. Visualisierung Erwiterungen
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
- ✅ XML-Comments in TrackLibrary.PikoA/TrackPlan.cs
- ✅ XML-Comments in TrackPlan.Renderer/TrackPlanSvgRenderer.cs
- ✅ Fluent API Beispiele in TrackBuilder<T>.Connections()
- ✅ Test-Beispiele in Test/TrackPlanRenderer/RendererTests.cs

---

## 🚀 Nächste Prioritäten

1. **Editor-UI** (WinUI-Integration für TrackPlan-Bearbeitung)
2. **Zusätzliche Gleistypen** (Curven, Weichen erweitern)
3. **Persistenz-Schicht** (JSON Serialisierung)

---






