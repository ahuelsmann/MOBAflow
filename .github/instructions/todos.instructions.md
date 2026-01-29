---
description: 'MOBAflow offene Aufgaben'
applyTo: '**'
---

# MOBAflow TODOs

> Letzte Aktualisierung: 2025-01-29 Session 13 (TrackPlan Test Refactoring - Domain-Only Architecture + WR Bogenweichen Fix)

---

## 🎊 SESSION 13 SUMMARY (CURRENT)

### **1. R9OvalTest Refactoring** ✅ COMPLETE
**Problem:** Test enthielt Rendering-Logik (`RotationDeg = 180.0 + 15.0`)
**Lösung:** Test komplett neu geschrieben mit reiner Topologie-Struktur

**Alte Implementierung (FALSCH):**
```csharp
// ❌ RENDERING LOGIC IM TEST
var branchR9Edge = new TrackEdge(Id, r9Template.Id) {
    RotationDeg = 180.0 + 15.0, // ❌ Test berechnet Rotation!
    StartPortId = "A",
    EndPortId = "B"
};
```

**Neue Implementierung (KORREKT):**
```csharp
// ✅ NUR TOPOLOGIE-STRUKTUR
var edge = new TrackEdge(Id, r9Template.Id) {
    // RotationDeg = 0 (default)
    StartPortId = "A",
    EndPortId = "B",
    StartNodeId = node.Id  // Nur Verbindungen!
};
```

**Architektur-Prinzip:**
- **Test:** Baut TopologyGraph (Nodes, Edges, Connections)
- **Renderer:** Berechnet ALLE Rotationen aus Topologie-Verkettung
- **Separation:** Domain (Test) ↔ Rendering (Renderer)

### **2. TopologyGraphRenderer - Rotations-Logik Validiert** ✅ VERIFIED
**Wie der Renderer Rotationen berechnet:**

```csharp
// Zeile 84: Basis-Rotation + Edge-Rotation (default 0)
var edgeAngleDeg = currentAngleDeg + edge.RotationDeg;

// Zeile 112: Exit-Winkel aus Geometrie berechnen
var (exitX, exitY, exitAngleDeg) = CalculateNextPosition(template, ...);

// Zeile 160: Exit-Winkel als Basis für nächstes Edge übergeben
edgesToProcess.Enqueue((nextEdge, nextIndex, exitX, exitY, exitAngleDeg));
```

**Ergebnis:**
- Wenn `edge.RotationDeg = 0` → Renderer verwendet automatisch `exitAngleDeg` vom vorherigen Edge
- R9 Circle: Edge[0] startet bei 0°, Edge[1] bei 15°, Edge[2] bei 30°, ... automatisch berechnet
- **Keine manuelle Rotation im Test nötig!**

### **3. WR Bogenweichen-Bug Fix** ✅ COMPLETE
**Problem:** WR-Weiche funktionierte nicht als R9-Ersatz im Kreis

**Root Cause:**
```csharp
// ❌ ALT: CalculateSwitchExit() behandelte ALLE Switches als gerade
private (double X, double Y, double AngleDeg) CalculateSwitchExit(...) {
    // Exit angle remains unchanged (no sweep on main path)
    return (exitX, exitY, startAngleDeg); // ❌ Immer 0° Änderung!
}
```

**WR ist eine Bogenweiche (Piko 55221):**
- `RadiusMm = R9` (907.97mm) → **Gleicher Radius wie R9!**
- `AngleDeg = 15°` → **Gleicher Winkel wie R9!**
- Sollte sich wie R9-Kurve verhalten (mit zusätzlichem Port C)

**Fix:**
```csharp
// ✅ NEU: Check RadiusMm > 0 für Bogenweichen
if (radius > 0) {
    // Curved switch - use curve math (same as CalculateCurveExit)
    var exitAngleDeg = startAngleDeg + sweepDeg; // ✅ +15° für WR!
    return (exitX, exitY, exitAngleDeg);
} else {
    // Straight switch - no angle change
    return (exitX, exitY, startAngleDeg);
}
```

**Test validiert Fix:**
- `SimpleR9Circle_ShouldCreatePerfectClosedTopology()` - 24× R9 perfekter Kreis ✅
- `R9CircleWithWR_ShouldMaintainCircleIntegrity()` - 23× R9 + 1× WR perfekter Kreis ✅

### **4. Test-Struktur Vereinfacht** ✅ COMPLETE
**Tests:**
1. `SimpleR9Circle` - Basis-Test, nur R9 Kurven
2. `R9CircleWithWR` - Erweitert, WR ersetzt erstes R9

**Vorteile:**
- Klare User Stories
- Einfache Tests zuerst, Komplexität schrittweise
- WR ohne Branch-Logik (nur Hauptweg A→B)

---

## 🔴 CRITICAL FINDINGS (Session 13)

### **1. Topology-First Architecture VALIDATED** ✅
**Beweis:** R9 Circle Test funktioniert OHNE `edge.RotationDeg` zu setzen

**Architektur-Flow:**
```
Domain (Test)                  Renderer
   ↓                              ↓
TopologyGraph              TopologyGraphRenderer
- Nodes                    - CalculateNextPosition()
- Edges                    - CalculateCurveExit()
- Connections              - CalculateSwitchExit() (FIXED!)
   ↓                              ↓
KEINE Rotationen           Rotationen aus Geometrie
```

**Bedeutung für zukünftige Tests:**
- Tests dürfen NUR TopologyGraph-Struktur aufbauen
- KEINE Berechnungen von Positionen/Winkeln im Test
- Renderer ist verantwortlich für ALLE Geometrie

### **2. Edge.RotationDeg Semantik Geklärt** ✅
**Verwendung:**
- **Default (0°):** Renderer berechnet aus Topologie-Verkettung
- **Override (≠0°):** Manuelles Drehen eines Edges (z.B. für UI Drag&Drop)

**Beispiel:**
- R9 Circle Test: Alle Edges haben `RotationDeg = 0` → Auto-Rotation
- TrackPlan Editor: User dreht ein Edge → `RotationDeg = 45` → Manual-Override

### **3. Bogenweichen vs. Gerade Weichen** ✅ CRITICAL DISTINCTION
**Piko A hat ZWEI Arten von Weichen:**

**Bogenweichen (Curved Switches):**
- WR, WL, BWR, BWL, BWR_R3, BWL_R3
- `RadiusMm > 0` → Hauptweg ist eine Kurve
- Können R9/R3 Kurven im Kreis ersetzen
- `exitAngleDeg = startAngleDeg + AngleDeg` (wie Kurven)

**Gerade Weichen (Straight Switches):**
- (Hypothetisch, falls RadiusMm = 0)
- Hauptweg ist gerade Linie
- `exitAngleDeg = startAngleDeg` (keine Änderung)

### **4. Renderer Exit-Calculation funktioniert korrekt** ✅
**Implementierung validiert:**
- `CalculateCurveExit()` - R9: +15° pro Piece
- `CalculateStraightExit()` - Gerade: 0° Änderung
- `CalculateSwitchExit()` - **FIXED:** Prüft RadiusMm für Bogenweichen

**Test zeigt:** 
- 24× R9: 360° perfekter Kreis ✅
- 23× R9 + 1× WR: 360° perfekter Kreis ✅

---

## 📋 BACKLOG (Session 14+)

### **TIER 1 - HIGH PRIORITY**
- [x] **WR Port C Branch Test erstellen** ✅ DONE Session 13\n  - Test: `WRPortCBranch_ShouldRenderDivergingPath()`\n  - Validiert Abzweigung funktioniert\n  - SVG zeigt beide Wege (Hauptweg + Abzweigung)\n  - Branch verwendet Connections dictionary
  
- [ ] **Drag-Start Pattern** (from Session 11)
  - Status: `DragThresholdHelper.cs` prepared
  - Needs: Manual integration in `TrackPlanPage.xaml.cs`
  - Blocker: False drag-starts on click

- [ ] **V-Shaped Track Angle Issue** 🐛
  - Problem: Tracks rotate 90° incorrectly when snapped
  - Approach: Unit Tests → SVG Export → Visual Validation
  - **NOTE:** Nach Session 13 WR-Fix prüfen ob Problem noch existiert

### **TIER 2 - MEDIUM PRIORITY**
- [ ] **Switch Three-Way (3WS)** Port rendering test
  - Similar architecture to R9 Circle test
  - Validate Port D, E rendering
  - 3WS hat 5 Ports (A, B, C, D, E)
  
- [ ] **Port Connection Visualization**
  - Optional: Draw lines between connected ports in SVG
  - Useful for understanding topology visually

- [ ] **Renderer Performance Profiling**
  - Test mit großen Topologien (100+ Edges)
  - Identify rendering bottlenecks

### **TIER 3 - FUTURE**
- [ ] **SkiaSharp Integration Evaluation**
- [ ] **Section Labels Rendering**
- [ ] **Feedback Points Optimization**
- [ ] **Movable Ruler Implementation**

---

## 🎯 SESSION 14+ INITIATIVES (Planned Architecture Refactoring)

### **MAJOR REFACTOR: Enum-Based Catalogs + Master Data Architecture** 📋

**Vision:**
Unified master catalog system similar to `germany-locomotives.json` and `germany-stations.json`:
- One central `catalogs.json` containing ALL track systems, scales, countries, cities, locomotives
- Enum-driven track catalog per manufacturer (PikoA, Roco, Märklin, etc.)
- Composite pattern: Individual catalogs aggregate into single master catalog
- Extensible for future: Europe, World, other manufacturers, scales

**Architecture:**
```
catalogs.json (Master Data File)
├── TrackSystems[]
│   ├── PikoA (H0, TT, N)
│   ├── Roco (H0, TT)
│   └── Märklin (H0)
├── Scales (H0, TT, N, Z, ...)
└── Countries[]
    ├── Germany (DE)
    │   ├── Stations[] (with main station flag)
    │   └── Locomotives[] (DB Baureihen)
    ├── Austria (AT)
    ├── France (FR)
    └── [Future: all Europe + World]
```

**Phase 1: Domain Model Design** (User-driven)
- [ ] **Create POCO classes** for Catalogs structure
  - `TrackSystem`, `TrackSpec`, `Scale`
  - `Country`, `Station`, `Locomotive`
  - Flat, deserializable JSON structure
  - **Owner:** User (domain expert design)
  - **Input:** Domain classes + JSON schema examples

**Phase 2: Enum-Based Factories** (Post-Phase 1)
- [ ] Replace `ITrackTypeFactory` with typed enums
  - `public enum PikoATrack { R9, WR, G231, ... }`
  - `public enum RocoTrack { ... }`
  - Each enum maps to `TrackSystem` specs
  
**Phase 3: Composite Catalog + Loader** (Post-Phase 2)
- [ ] Implement `Catalogs` class (Composite pattern)
  - Aggregates individual `ITrackCatalog` instances
  - Single `GetById()` searches all systems
  
- [ ] JSON Loader (like locomotives/stations)
  - Deserialize `catalogs.json`
  - Populate all enums + specs at startup
  
**Phase 4: Project Integration** (Post-Phase 3)
- [ ] Add `public Catalogs? Catalogs { get; set; }` to `Project` domain class
  - Tracks which catalogs project uses (e.g., only PikoA, or PikoA+Roco)
  - Persisted with project

**Benefits:**
- ✅ Single source of truth for all track data
- ✅ Type-safe enum-based selections (compile-time safety)
- ✅ Extensible for future manufacturers/countries
- ✅ Consistent JSON pattern (like germany-*.json)
- ✅ Clean separation: Domain classes ↔ Rendering

**Start Point:**
- User designs POCO domain classes for catalogs structure
- Provides example JSON schema
- Then implementation phases proceed

---

## 📚 CODE QUALITY IMPROVEMENTS

### **High Priority (Session 14+):**
- [ ] **Theme Resources in XAML** - Move hardcoded colors to `{ThemeResource}`
- [ ] **Memory Cleanup** - Verify event handler leaks, add IDisposable where needed
- [ ] **XML Documentation** - Add to new renderer methods
- [x] **CalculateSwitchExit() Fix** - Bogenweichen-Unterstützung ✅ DONE Session 13

---

## 🗂️ SESSION 13 FILES MODIFIED

| File | Changes | Status |
|------|---------|--------|
| `Test/TrackPlan/R9OvalTest.cs` | Complete rewrite + WR tests + Port C branch test | ✅ COMPLETE |
| `TrackPlan.Renderer/Service/TopologyGraphRenderer.cs` | CalculateSwitchExit() Bogenweichen-Fix | ✅ COMPLETE |
| `.github/instructions/todos.instructions.md` | Architecture insights + WR findings | ✅ UPDATED |

---

## 🎯 ARCHITECTURE STATUS

### **Topology-First Design** ✅ VALIDATED (Session 13)
```
Project (User-JSON)
  └── TopologyGraph (POCO: Nodes, Edges only)
      ├── Nodes: TrackNode[] (connection points)
      ├── Edges: TrackEdge[] (track pieces)
      │   ├── StartPortId / EndPortId (topology only)
      │   ├── StartNodeId / EndNodeId (connections)
      │   └── RotationDeg (optional override, default 0)
      └── Rendering Pipeline:
          ├── TopologyGraphRenderer (geometry type detection)
          ├── CurveGeometry, SwitchGeometry, StraightGeometry renderers
          ├── Exit point calculation (CalculateCurveExit, etc.)
          │   ├── CalculateCurveExit() - Kurven (+15° für R9)
          │   ├── CalculateStraightExit() - Geraden (0° Änderung)
          │   └── CalculateSwitchExit() - Weichen (prüft RadiusMm!)
          └── SvgExporter (primitives → SVG + port labels)
```

**Key Principle:** Renderer computes ALL rotations from topology structure

### **Renderer Rotation Architecture** ✅ VALIDATED + FIXED
**Flow:**
1. **First Edge:** `edgeAngleDeg = startAngleDeg + edge.RotationDeg`
2. **Calculate Exit:** `exitAngleDeg = CalculateNextPosition(...)`
   - **Curves:** `startAngleDeg + AngleDeg`
   - **Straights:** `startAngleDeg` (unchanged)
   - **Switches:** Check `RadiusMm`:
     - `> 0` → `startAngleDeg + AngleDeg` (Bogenweiche)
     - `= 0` → `startAngleDeg` (Gerade Weiche)
3. **Next Edge:** `currentAngleDeg = exitAngleDeg` (from previous edge)
4. **Repeat:** Each edge inherits rotation from previous exit

**Result:** Perfect circle for R9 AND R9+WR!

---

## 📖 LESSONS LEARNED (Session 13)

1. **Tests müssen Domain-Puristen sein:**
   - NUR TopologyGraph-Struktur aufbauen
   - KEINE Berechnungen (Position, Rotation, Geometrie)
   - Renderer ist die einzige Quelle für Geometrie-Logik

2. **edge.RotationDeg hat zwei Modi:**
   - **Default (0):** Auto-rotation from topology
   - **Override (≠0):** Manual user rotation (Editor)

3. **Renderer ist intelligent:**
   - Berechnet Exit-Position/Winkel aus Template-Geometrie
   - Propagiert Winkel automatisch durch Verkettung
   - Unterstützt Branches (Port C, etc.) via Connections
   - **NEU:** Unterscheidet Bogenweichen vs. Gerade Weichen

4. **SVG-Export ist essentiell für Tests:**
   - Visuelle Validierung zeigt Probleme sofort
   - HTML-Wrapper für Browser-Inspektion sehr wertvoll
   - Bounding Box Calculation nötig für Centering

5. **Test-Struktur Lessons:**
   - Einfache Tests zuerst (R9 Circle)
   - Komplexe Features separat (WR Switch)
   - Jeder Test eine klare User Story
   - **NEU:** Schrittweise Komplexität (R9 → R9+WR → R9+WR+Branch)

6. **Bogenweichen sind Kurven mit Extra-Port:**
   - WR/BWR haben gleiche Geometrie wie R9
   - Können 1:1 als Ersatz verwendet werden
   - Port C ist optional (für Abzweigung)

---

## ❓ NEXT SESSION ENTRY POINT (Session 14)

**Start with:** 
1. Run `SimpleR9Circle_ShouldCreatePerfectClosedTopology()` test ✅ DONE
2. Run `R9CircleWithWR_ShouldMaintainCircleIntegrity()` test ✅ DONE
3. Verify SVG shows perfect circle with WR integrated
4. If circle is perfect → WR fix validated ✅

**Then:**
- [x] Create WR Port C Branch test ✅\n- [x] Validate diverging path rendering ✅\n- [x] Test Branch topology ✅

**Finally:**
- Document WR Port C test in todos
- Address V-Shaped Track Issue (if still exists)
- Consider 3WS test (5-port switch)






