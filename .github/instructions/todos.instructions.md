---
description: 'MOBAflow offene Aufgaben'
applyTo: '**'
---

# MOBAflow TODOs

> Letzte Aktualisierung: 2025-01-24 Session 11 FINAL (Drag Pattern + Port Click + Toolbox Icons)

---

## 🎊 SESSION 11 SUMMARY (COMPLETE)

### **1. Port Hover Animation - Dual-Port Snap Feedback** ✅
- ✅ Enhanced `RenderPortHoverEffects` with dual-port color-coding
  - Red (Target Port), Turquoise (Moving Port), Blue (Hover)
- ✅ Helper method `RenderSnapPort` for reusable port rendering
- **User Benefit:** Clear visual feedback when dragging tracks near snap targets

### **2. WinUI 3 Resource Deployment Fixed** ✅
- ✅ Re-enabled `XamlControlsResources` in App.xaml
- ✅ Fixed missing `SymbolThemeFontFamily` in MainWindow TitleBar
- **Root Cause:** WinUI 3 requires XamlControlsResources for theme resources

### **3. MainWindow Initialization Restored** ✅
- ✅ Restored `BuildNavigationFromRegistry()` call
- ✅ Restored event handler wiring
- **Lesson:** Always use explicit `// ...existing code...` markers

### **4. TrackPlanEditorViewModel DI Fixed** ✅
- ✅ Removed obsolete `ITopologyConstraint[]` parameter
- ✅ Added `ILayoutEngine` parameter

### **5. Ghost Track Drop** ✅
- ✅ `PointerUp()` in ViewModel behandelt jetzt `GhostPlacement`
- ✅ `CommitGhostPlacement()` + `AddTrack()` + Snap-Connection
- ✅ Cursor-Reset in TrackPlanPage
- **Fixed:** Ghost Track löst sich beim Loslassen

### **6. Port Click Prevention** ✅
- ✅ `Port_PointerPressed` handler mit `e.Handled = true`
- ✅ Zeigt Port-Info in StatusBar
- **Fixed:** Klick auf Port startet KEIN Track-Drag mehr

### **7. Toolbox Icons Proportional** ✅
- ✅ Gemeinsame Skala basierend auf G107 (107mm)
- ✅ `iconScale = 56.0 / 107.0` für alle Templates
- ✅ StrokeThickness 2.5 für bessere Sichtbarkeit
- **Result:** G107 länger als G62 sichtbar, Proportionen korrekt

---

## 🔴 CRITICAL FOR SESSION 12

### **1. Drag-Start Pattern (BROKEN UX)** 🚨 HIGHEST PRIORITY
**Problem:**
- ❌ Klick startet SOFORT Drag (falsches UX)
- ❌ Kein Threshold → versehentliches Drag
- ❌ Unmöglich nur zu selektieren

**Root Cause:**
```csharp
// FALSCH (aktuell):
PointerPressed → BeginMultiGhostPlacement() SOFORT

// RICHTIG (Microsoft Pattern):
PointerPressed → Merke Start-Position
PointerMoved → if (distance > 8px) → Drag Start
PointerReleased → Cleanup
```

**Reference:** 
- Microsoft AutomaticDragHelper (WinUI Source Code)
- https://github.com/UnigramDev/Unigram/blob/main/Telegram/Common/AutomaticDragHelper.cs
- Threshold: 8px (SM_CXDRAG * 2.0 multiplier)

**Solution Prepared:**
- ✅ `DragThresholdHelper.cs` created in `WinUI\View\`
- ⚠️ **MANUAL IMPLEMENTATION REQUIRED:**

**Changes Needed in TrackPlanPage.xaml.cs:**

**A) Add Field (after line 46):**
```csharp
private readonly DragThresholdHelper _dragThreshold = new();
```

**B) PointerPressed (replace lines 698-710):**
```csharp
_viewModel.PointerDown(world, true, isCtrlPressed);

// Defer drag until threshold crossed (Microsoft pattern)
if (_viewModel.SelectedTrackIds.Count > 0 && _viewModel.GhostPlacement is null)
{
    _dragStartWorldPos = world;
    _dragThreshold.BeginTracking(pos);  // ← Only tracking, no drag yet!
}

GraphCanvas.CapturePointer(e.Pointer);
RenderGraph();
UpdatePropertiesPanel();
```

**C) PointerMoved (insert BEFORE line 818 `if (_viewModel.GhostPlacement...`):**
```csharp
// Check drag threshold (Microsoft AutomaticDragHelper pattern)
if (_dragThreshold.IsWaiting && _viewModel.SelectedTrackIds.Count > 0)
{
    if (_dragThreshold.ShouldStartDrag(pos))
    {
        _dragThreshold.Reset();
        _viewModel.BeginMultiGhostPlacement(_viewModel.SelectedTrackIds.ToList());
        
        ProtectedCursor = null;
        _= _attentionControl.DimIrrelevantTracksAsync(_viewModel.SelectedTrackIds.ToList(), dimOpacity: 0.3f);
        StatusText.Text = $"Dragging {_viewModel.SelectedTrackIds.Count} track(s)...";
    }
}
```

**D) PointerReleased (after line 1002 `_portHoverAffordance.ClearAllHighlightsAsync();`):**
```csharp
_dragThreshold.Reset();
```

**Effort:** ~15 LOC changes, **MUST BE DONE** before V-Shaped Bug

---

## 📋 REMAINING WORK (Session 12+)

### **TIER 3 - BUG FIXES**
- [ ] **V-Shaped Track Angle Issue** 🐛 (After Drag-Threshold fix)
  - **Problem:** Tracks rotate 90° incorrectly when snapped
  - **Approach:** Unit Tests → SVG Export → Visual Validation
  - **Test Scenarios:** 0°, 45°, 90°, 135°, 180°, -45° snap angles
  - **Investigation Targets:**
    - `SnapToConnectService.FindSnapCandidates()` (angle calculation)
    - `TrackPlanEditorViewModel.DropTrack()` (rotation application)
    - `GetPortWorldPosition()` (coordinate transformation)
    - Y-axis inversion in Canvas vs World coordinates
  - **Effort:** TBD (diagnosis first)

### **TIER 4 (FUTURE) - BACKLOG**
- [ ] **SkiaSharp Integration Evaluation**
- [ ] **Section Labels Rendering**
- [ ] **Feedback Points Optimization**
- [ ] **Movable Ruler Implementation**
- [ ] **C++ Performance Library** (Only if bottleneck detected)

---

## 📚 CODE QUALITY IMPROVEMENTS PENDING

### **High Priority (Session 13+):**
- [ ] **Theme Resources in XAML**
  - Move hardcoded colors to `{ThemeResource}`
  - Effort: 40 LOC

- [ ] **Memory Cleanup**
  - Verify event handler leaks
  - Add IDisposable where needed
  - Effort: 20 LOC

### **Medium Priority (Session 14+):**
- [ ] **Performance Monitoring**
  - Add WPR/WPA support if needed
  - Document baselines

---

## 🗂️ SESSION 11 FILES MODIFIED

| Category | Files | Status |
|----------|-------|--------|
| **Port Hover** | WinUI/Rendering/CanvasRenderer.cs | ✅ Dual-port colors |
| **Port Hover** | WinUI/View/TrackPlanPage.xaml.cs | ✅ Pass SnapPreview |
| **WinUI Resources** | WinUI/App.xaml | ✅ XamlControlsResources |
| **WinUI Resources** | WinUI/WinUI.csproj | ✅ BuildTools re-enabled |
| **Navigation** | WinUI/View/MainWindow.xaml.cs | ✅ Constructor restored |
| **DI Registration** | TrackPlan.Editor/TrackPlanServiceExtensions.cs | ✅ Factory registration |
| **ViewModel** | TrackPlan.Editor/ViewModel/TrackPlanEditorViewModel.cs | ✅ GhostPlacement in PointerUp |
| **Port Click** | WinUI/View/TrackPlanPage.xaml.cs | ✅ Port_PointerPressed handler |
| **Toolbox Icons** | WinUI/Rendering/CanvasRenderer.cs | ✅ Proportional scale |
| **Drag Helper** | WinUI/View/DragThresholdHelper.cs | ✅ NEW - Microsoft pattern |

---

## 🎯 ARCHITECTURE STATUS

### **Topology-First Design** ✅ VALIDATED
```
Project (User-JSON)
  └── TopologyGraph (POCO: Nodes, Edges only)
      ├── Nodes: TrackNode[]
      ├── Edges: TrackEdge[] (with Connections dict)
      └── Rendering Pipeline:
          ├── TopologyResolver (analyze structure)
          ├── GeometryCalculationEngine (positions/angles)
          ├── SkiaSharpCanvasRenderer (visualization)
          └── CanvasRenderer (WinUI display)
```

### **Layer Architecture** ✅ COMPLETE
```
Domain (POCO layer) ✅
  ├── TrackPlan.Domain (Graph/Topology) ✅ 
  └── All POCO classes ✅

Rendering/Geometry ✅
  ├── TrackPlan.Geometry (Real module) ✅
  ├── TrackPlan.Renderer (Visualization) ✅
  │   └── TypeForwarding.cs (re-exports Geometry types) ✅
  └── GeometryCalculationEngine ✅

Editor (Business Logic) ✅ COMPLETE
  ├── SnapToConnectService ✅
  ├── TopologyResolver ✅
  ├── TrackConnectionService ✅
  └── TrackPlanEditorViewModel (DI-ready) ✅
```

---

## 🗂️ RULES FOR CONTINUITY

1. ✅ Phase-Struktur: Session 10 ✅, Session 11 ✅
2. ✅ Architektur dokumentiert (Topology-First, Layer-Based)
3. ✅ TODOs für nächste Session klar (Drag-Threshold FIRST)
4. ✅ Build-Status transparent (0 C# errors)
5. ✅ Port Hover Animation implementiert (Dual-Port Feedback)
6. ✅ DragThresholdHelper.cs erstellt (Microsoft Pattern Reference)
7. ✅ **Empfehlung für Session 12:** Drag-Threshold implementieren → dann V-Shaped Bug

---

## 📖 LESSONS LEARNED (Session 11)

### **UX Pattern Research:**
1. **Microsoft AutomaticDragHelper** (UnigramDev/Unigram)
   - Source: WinUI `dxaml\xcp\dxaml\lib\AutomaticDragHelper.cpp`
   - Threshold: `SM_CXDRAG * 2.0 = 8 pixels`
   - Pattern: Pressed → Track → Moved > Threshold → Start Drag

2. **Port Click Handling:**
   - Ports should have `PointerPressed` with `e.Handled = true`
   - Prevents event bubbling to Canvas
   - Allows click-to-inspect without dragging

3. **Toolbox Icon Scaling:**
   - Use COMMON scale for all templates
   - Reference: Longest track (G107 = 107mm)
   - Result: Proportional visual representation

### **Anti-Patterns Identified:**
- ❌ Immediate drag on PointerPressed
- ❌ No threshold → accidental drags
- ❌ Per-icon scaling → destroys proportions
- ❌ Port clicks bubble to Canvas → unwanted drag

---

## 🚀 SESSION 12 FOCUS

**Primary Goal:** Implement Microsoft Drag-Threshold Pattern

**Why First:**
- Blocks proper UX (can't click without dragging)
- Reference implementation available (DragThresholdHelper.cs)
- Small change (4 locations, ~15 LOC total)
- Unblocks clean testing for V-Shaped Bug

**Then:** V-Shaped Track Angle Bug (Unit Tests + SVG Validation)



