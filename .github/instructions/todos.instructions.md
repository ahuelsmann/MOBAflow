---
description: 'MOBAflow offene Aufgaben'
applyTo: '**'
---

# MOBAflow TODOs

> Letzte Aktualisierung: 2025-01-29 Session 12 (Port C Topology + Rendering Architecture Complete)

---

## 🎊 SESSION 12 SUMMARY (CURRENT)

### **1. Port Rendering Architecture** ✅ COMPLETE
- ✅ `TopologyGraphRenderer` - Completely rewritten with proper exit point calculations
- ✅ `RenderAllPorts()` - Iterates all template.Ends (not just connected A→B)
- ✅ `CalculatePortPosition()` - Computes world coordinates for each port
- ✅ `CalculateDivergingPortPosition()` - Switch Port C branching math
- ✅ Port C now **visually rendered** in SVG with correct position (orange circle + label)

**Renderers Implemented:**
- ✅ `CalculateCurveExit()` - Curve track exit position/angle
- ✅ `CalculateStraightExit()` - Straight track exit
- ✅ `CalculateSwitchExit()` - Switch main path (A→B, Port C diverging)

### **2. WR Switch with Port C Visualization** ✅ COMPLETE
- ✅ WR template integrated into R9 circle (replaces position 0)
- ✅ Port A (red) - Entry point visible
- ✅ Port B (green) - Straight exit visible
- ✅ Port C (orange) - Diverging branch now visible at correct offset

**SVG Output Verified:**
```
r9-circle-with-wr.svg (25334 bytes)
- Port A: (458.163, 500) - Red
- Port B: (541.837, 500) - Green
- Port C: (540.413, 510.828) - Orange diverging offset
```

### **3. Port C Topology Connection** ⚠️ PARTIAL (NEEDS CLARIFICATION)
- ✅ Port C added to `wrEdge.Connections["C"]` dictionary
- ✅ Connected to lastR9Edge.Id and Port B
- ❓ **QUESTION: Is this functional or just data structure?**

**Current Implementation:**
```csharp
wrEdge.Connections["C"] = (lastR9EndNode.Value, lastR9Edge.Id.ToString(), "B");
```

**CRITICAL QUESTION FOR SESSION 13:**
What should Port C actually do topologically?

**Option A: Port C = Abzweigung (Branch)** 
- Port C führt zu einem ANDEREN Gleisstück ab (z.B. Ausweichgleis)
- Würde eine neue Edge brauchen, die von Port C startet
- Example: `WR_PortC_Edge` → R9 oder andere Track

**Option B: Port C = Alternative Route im Kreis**
- Port C verbindet zu einem anderen R9-Stück im Kreis (nicht zum letzten)
- Schafft eine "innere" Route parallel zum Außenkreis
- Example: WR Port C → R9[10] Port A

**Option C: Port C = Nur Visualisierung**
- Port C wird nur angezeigt, aber ist nicht in der echten Topologie aktiv
- Wird nur für UI/Rendering verwendet
- Keine funktionalen Auswirkungen

**Needed from User:**
1. Welche Topologie wünscht sich der Benutzer?
2. Soll Port C in der Kreis-Topologie eine funktionale Rolle spielen?
3. Oder ist Port C nur visuell zum Zeigen "dies ist eine Weiche"?

---

## 🔴 CRITICAL FOR SESSION 13

### **1. Port C Topology Semantics - DECISION NEEDED** 🚨 BLOCKING
**Current State:** Port C is rendered visually but topology connection unclear

**Required Decision Points:**
- [ ] Determine Port C functional role
- [ ] Implement proper Edge/Node structure if needed
- [ ] Update TopologyGraphRenderer if new connection type needed
- [ ] Add test cases for Port C routing

**Files Affected if Changes Needed:**
- `Test\TrackPlan\R9OvalTest.cs` - `CreateR9CircleWithWr()`
- `TrackPlan.Domain\Graph\TrackEdge.cs` - Connections dictionary usage
- `TrackPlan.Renderer\Service\TopologyGraphRenderer.cs` - Port rendering logic

---

## 📋 BACKLOG (Session 14+)

### **TIER 1 - HIGH PRIORITY**
- [ ] **Drag-Start Pattern** (from Session 11)
  - Status: `DragThresholdHelper.cs` prepared
  - Needs: Manual integration in `TrackPlanPage.xaml.cs`
  - Blocker: False drag-starts on click

- [ ] **V-Shaped Track Angle Issue** 🐛
  - Problem: Tracks rotate 90° incorrectly when snapped
  - Approach: Unit Tests → SVG Export → Visual Validation
  - Status: Needs investigation after Port C resolved

### **TIER 2 - MEDIUM PRIORITY**
- [ ] **Switch Three-Way (3WS)** Port rendering
  - Similar to WR (2 diverging ports)
  - May have Ports D, E depending on template
  
- [ ] **Port Connection Visualization**
  - Optional: Draw lines between connected ports in SVG
  - Useful for understanding topology visually

### **TIER 3 - FUTURE**
- [ ] **SkiaSharp Integration Evaluation**
- [ ] **Section Labels Rendering**
- [ ] **Feedback Points Optimization**
- [ ] **Movable Ruler Implementation**

---

## 📚 CODE QUALITY IMPROVEMENTS

### **High Priority (Session 14+):**
- [ ] **Theme Resources in XAML** - Move hardcoded colors to `{ThemeResource}`
- [ ] **Memory Cleanup** - Verify event handler leaks, add IDisposable where needed
- [ ] **Documentation** - Add XML comments to new renderer methods

---

## 🗂️ SESSION 12 FILES MODIFIED

| File | Changes | Status |
|------|---------|--------|
| `TrackPlan.Renderer/Service/TopologyGraphRenderer.cs` | Complete rewrite with exit calculations | ✅ COMPLETE |
| `Test/TrackPlan/R9OvalTest.cs` | Added Port C connection logic | ✅ COMPLETE |
| `TrackPlan.Renderer/Service/SvgExporter.cs` | Port label rendering (Y-axis fix from S11) | ✅ WORKING |

---

## 🎯 ARCHITECTURE STATUS

### **Topology-First Design** ✅ VALIDATED
```
Project (User-JSON)
  └── TopologyGraph (POCO: Nodes, Edges only)
      ├── Nodes: TrackNode[]
      ├── Edges: TrackEdge[] (with Connections dict)
      │   └── Port A, B, C (Switch) defined via StartPortId/EndPortId/Connections
      └── Rendering Pipeline:
          ├── TopologyGraphRenderer (geometry type detection + port tracking)
          ├── CurveGeometry, SwitchGeometry, StraightGeometry renderers
          ├── Exit point calculation (Curve, Straight, Switch variants)
          └── SvgExporter (primitives → SVG + port labels)
```

### **Port Rendering Architecture** ✅ COMPLETE
- All template Ends (ports) rendered, not just connected ones
- Each port gets world position + angle + visual (circle + label)
- Color-coded by port ID (A=Red, B=Green, C=Orange, D=Purple, E=Cyan)
- Supports arbitrary port counts per template

---

## 📖 LESSONS LEARNED (Session 12)

1. **Port Rendering Separation:**
   - Visual rendering ≠ Topological function
   - Port C can be drawn without it being an active route yet

2. **Exit Point Calculations:**
   - Each geometry type (Curve, Straight, Switch) needs custom exit logic
   - Critical for multi-edge topology chaining

3. **Connections Dictionary Usage:**
   - Current use is unclear - may need refactoring based on intended Port C role
   - Tuple structure: `(NodeId, ConnectedEdgeId?, ConnectedPortId?)`

4. **Template Port Variations:**
   - Curves: Usually 2 ports (A, B)
   - Switches: Usually 3 ports (A, B, C)
   - Three-way: Usually 4 ports (A, B, C, D)
   - Architecture now supports arbitrary counts

---

## ❓ QUESTIONS FOR USER (Session 13)

**Before implementing further Port C changes:**

1. **Port C Functional Role:**
   - Should Port C be an active routing choice in the topology?
   - Or just a visual indicator that this is a switch?

2. **Topology Structure:**
   - If Port C routes somewhere, where should it go?
   - Should it be a separate Edge (alternative path)?
   - Or connect within the existing R9 circle?

3. **Test Validation:**
   - How should we validate Port C routing is correct?
   - SVG visualization? Topology traversal test? Both?

4. **Future Switch Types:**
   - After Port C, should we support 3-way switches (3WS)?
   - Should rendering generalize to N-port switches?

---

## 🚀 NEXT SESSION ENTRY POINT

**Start with:** User answers to Port C topology questions
**Then:** Implement proper Port C routing based on answers
**Finally:** Add tests to validate Port C functionality



