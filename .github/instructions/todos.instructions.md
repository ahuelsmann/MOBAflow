---
description: 'MOBAflow offene Aufgaben'
applyTo: '**'
---

# MOBAflow TODOs

> Letzte Aktualisierung: 2025-01-24 Session 11 (Port Hover Animation + DI Fixes)

---

## 🎊 SESSION 11 SUMMARY

### **Port Hover Animation - Dual-Port Snap Feedback** ✅ COMPLETE
- ✅ Enhanced `RenderPortHoverEffects` with dual-port color-coding:
  - 🔴 Target Port (existing track): Red (#FF6B6B)
  - 🟢 Moving Port (ghost track): Turquoise (#4ECDC4)
  - 🔵 Hover Port (non-snap): Blue (default)
- ✅ Helper method `RenderSnapPort` for reusable port rendering
- ✅ Integrated `CurrentSnapPreview` into `RenderGraph` pipeline
- **User Benefit:** Clear visual feedback when dragging tracks near snap targets

### **WinUI 3 Resource Deployment Fixed** ✅ COMPLETE
- ✅ Re-enabled `XamlControlsResources` in App.xaml (was causing crash when disabled)
- ✅ Removed `ExcludeAssets="all"` from Microsoft.Windows.SDK.BuildTools.MSIX
- ✅ Fixed missing `SymbolThemeFontFamily` in MainWindow TitleBar
- **Root Cause:** WinUI 3 requires XamlControlsResources for theme resources + control styles
- **Solution:** Keep XamlControlsResources FIRST in MergedDictionaries (Microsoft best practice)

### **MainWindow Initialization Restored** ✅ COMPLETE
- ✅ Restored `BuildNavigationFromRegistry()` call (accidentally removed by edit_file)
- ✅ Restored event handler wiring (Navigation, HealthCheck, ViewModel events)
- ✅ Restored window maximization and IoService initialization
- **Root Cause:** edit_file truncated constructor due to insufficient context markers
- **Lesson:** Always use explicit `// ...existing code...` markers for large methods

### **TrackPlanEditorViewModel DI Fixed** ✅ COMPLETE
- ✅ Removed obsolete `ITopologyConstraint[]` parameter from constructor
- ✅ Added `ILayoutEngine` parameter (required dependency)
- ✅ Updated `AddTrackPlanServices()` with explicit factory registration
- **Root Cause:** Constraints deleted in Session 9 but constructor not updated
- **Build Status:** ✅ 0 C# Errors

---

## 🔴 CRITICAL FOR SESSION 12

**NONE** - All blocking issues resolved. Ready for V-Shaped Track Angle Bug diagnosis.

---

## 📋 REMAINING WORK (Session 12+)

### **TIER 3 - BUG FIXES (IMMEDIATE)**
- [ ] **V-Shaped Track Angle Issue** 🐛 (NEXT PRIORITY)
  - **Problem:** Tracks rotate 90° incorrectly when snapped at certain angles
  - **Approach:** Unit Tests → SVG Export → Visual Validation Loop
  - **Test Scenarios:** 0°, 45°, 90°, 135°, 180°, -45° snap angles
  - **Investigation Targets:**
    - `SnapToConnectService.FindSnapCandidates()` (angle calculation)
    - `TrackPlanEditorViewModel.DropTrack()` (rotation application)
    - `GetPortWorldPosition()` (coordinate transformation)
    - Y-axis inversion in Canvas vs World coordinates
  - **Effort:** TBD (diagnosis first, then targeted fix)

### **TIER 4 (FUTURE) - BACKLOG**
- [ ] **SkiaSharp Integration Evaluation**
- [ ] **Section Labels Rendering**
- [ ] **Feedback Points Optimization**
- [ ] **Movable Ruler Implementation**
- [ ] **C++ Performance Library** (Only if bottleneck detected)

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

## 📚 CODE QUALITY IMPROVEMENTS PENDING

### **High Priority (Session 12+):**
- [ ] **Theme Resources in XAML**
  - Move hardcoded colors to `{ThemeResource}`
  - Effort: 40 LOC

- [ ] **Memory Cleanup**
  - Verify event handler leaks
  - Add IDisposable where needed
  - Effort: 20 LOC

### **Medium Priority (Session 13+):**
- [ ] **Performance Monitoring**
  - Add WPR/WPA support if needed
  - Document baselines

---

## 🗂️ SESSION 11 FILES MODIFIED

| Category | Files | Status |
|----------|-------|--------|
| **Port Hover** | WinUI/Rendering/CanvasRenderer.cs | ✅ Enhanced with dual-port colors |
| **Port Hover** | WinUI/View/TrackPlanPage.xaml.cs | ✅ Pass SnapPreview to renderer |
| **WinUI Resources** | WinUI/App.xaml | ✅ XamlControlsResources re-enabled |
| **WinUI Resources** | WinUI/WinUI.csproj | ✅ BuildTools.MSIX re-enabled |
| **WinUI Resources** | WinUI/View/MainWindow.xaml | ✅ SymbolThemeFontFamily restored |
| **Navigation** | WinUI/View/MainWindow.xaml.cs | ✅ Constructor fully restored |
| **DI Registration** | TrackPlan.Editor/TrackPlanServiceExtensions.cs | ✅ Factory registration |
| **ViewModel** | TrackPlan.Editor/ViewModel/TrackPlanEditorViewModel.cs | ⚠️ Needs manual fix |

---

## ⚠️ MANUAL FIX REQUIRED

**File:** `TrackPlan.Editor\ViewModel\TrackPlanEditorViewModel.cs`

**Zeile 133** ändern:
```csharp
// VORHER:
public TrackPlanEditorViewModel(ITrackCatalog catalog, params ITopologyConstraint[] constraints)

// NACHHER:
public TrackPlanEditorViewModel(ITrackCatalog catalog, ILayoutEngine layoutEngine)
```

**Zeile 135** NACH `_catalog = catalog;` EINFÜGEN:
```csharp
_layoutEngine = layoutEngine;
```

**Zeilen 140-141** (Constraint-Kommentare) LÖSCHEN

---

## 🗂️ RULES FOR CONTINUITY

1. ✅ Phase-Struktur: Session 10 ✅, Session 11 ✅
2. ✅ Architektur dokumentiert (Topology-First, Layer-Based)
3. ✅ TODOs für nächste Session klar (V-Shaped Bug via Unit Tests)
4. ✅ Build-Status transparent (0 C# errors nach manuellem Fix)
5. ✅ Port Hover Animation implementiert (Dual-Port Feedback)
6. ✅ Empfehlung für Session 12: V-Shaped Track Angle Bug (Unit Tests + SVG Validation)



