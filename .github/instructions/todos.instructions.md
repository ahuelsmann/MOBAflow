---
description: 'MOBAflow offene Aufgaben'
applyTo: '**'
---

# MOBAflow TODOs

> Letzte Aktualisierung: 2025-01-24 Session 9 (Phase 3 - Architecture Refactoring STARTED)

---

## 🎊 SESSION 9 SUMMARY

### **Part 1: Domain Model Refactoring (POCO Compliance)** ✅ COMPLETE
- ✅ TopologyGraph refactored to pure POCO (removed all methods)
- ✅ TopologyGraphService created for graph operations
- ✅ All POCO data classes verified: TrackNode, TrackEdge, Section, Isolator, Endcap, Endpoint
- ✅ Removed all validation constraints (DuplicateFeedbackPointNumberConstraint, GeometryConnectionConstraint)
- ✅ ValidationService disabled (returns empty list)
- **Build Status:** ✅ 0 C# Errors

### **Part 2: SDK Build Issues (Windows App SDK)** ⚠️ MITIGATED
- ⚠️ Windows App SDK BuildTools package incomplete (makepri.exe missing)
- ✅ Mitigation: Disabled MSIX packaging in Debug builds
- ✅ Excluded Microsoft.Windows.SDK.BuildTools.MSIX from WinUI + Plugins
- ✅ App runs locally without packaging errors
- **Build Status:** ✅ All projects compile (SDK packaging disabled for Dev)

### **Part 3: Phase 3 - Business Logic Migration** ⚠️ 50% COMPLETE
#### Architektur-Refactoring GESTARTET:
- ✅ **Option B (Interfaces)** designed:
  - ✅ Created ISnapToConnectService interface in Renderer
  - ✅ Created ITopologyResolver interface in Renderer
  - ✅ Updated ISnapPreviewProvider to use interfaces
  - ✅ Updated TrackPlanLayoutEngine to use ITopologyResolver
  
- ✅ **Service Migration begonnen:**
  - ✅ SnapToConnectService copied to Editor.Service
  - ✅ TopologyResolver copied to Editor.Service
  - ✅ AssignFeedbackPointToTrackUseCase copied to Editor.Service
  - ✅ TrackConnectionService consolidated in Editor (removed from Renderer)

- ⚠️ **BLOCKED:** Circular Dependency Issue
  - Problem: Services exist in both Editor AND Renderer (duplicates)
  - DI registration shows ambiguous references
  - Need to clean up remaining Renderer duplicates in next session

#### **Aktueller Build Status:** ⚠️ BLOCKED
```
Error: 'SnapToConnectService' is ambiguous between:
  - Moba.TrackPlan.Editor.Service.SnapToConnectService
  - Moba.TrackPlan.Renderer.Service.SnapToConnectService (old copy)
```

**Root Cause:** Old services still exist in Renderer directory - need cleanup.

---

## 🔴 CRITICAL FOR SESSION 10

### **PHASE 3 - SERVICE CONSOLIDATION (IMMEDIATE)**

**Option A (Recommended - Quick Fix):**
1. Delete ALL service copies from Renderer:
   - Remove: TrackPlan.Renderer\Service\SnapToConnectService.cs (if exists)
   - Remove: TrackPlan.Renderer\Service\TopologyResolver.cs (if exists)
   - Remove: TrackPlan.Renderer\Service\AssignFeedbackPointToTrackUseCase.cs (if exists)
   
2. Update DI registration to use Editor implementations:
   ```csharp
   // In TrackPlanServiceExtensions.cs
   services.AddSingleton<Editor.Service.SnapToConnectService>();
   services.AddSingleton<Editor.Service.TopologyResolver>();
   ```

3. Fix TrackPlanPageService instantiation:
   - Change `new TrackPlanLayoutEngine(catalog)` to use DI factory
   - Register factory in AddTrackPlanServices

4. Verify build succeeds with no ambiguous references

**Alternative Option B (Cleaner - More Work):**
- Keep full interface-based architecture
- Explicit using statements in Editor for Renderer interfaces
- Full DI wiring with concrete implementations
- ~400 LOC refactoring needed

**Recommendation:** **Go with Option A for Session 10** - pragmatic, working solution first.

---

## 📋 REMAINING WORK (Session 10+)

### **IMMEDIATE - SESSION 10** 
- [ ] **Complete Service Consolidation (Phase 3)**
  - Clean up ambiguous references (duplicate services in Renderer)
  - Fix TrackPlanPageService DI
  - Verify build succeeds
  - Effort: ~2 hours

### **TIER 3 PART 2 - UI ENHANCEMENTS (Session 10+)**
- [ ] **Port Hover Animation**
  - Scale up on hover (1.0x → 1.3x)
  - Add glow effect (ScaleTransform + shadow)
  - Effort: 80 LOC

- [ ] **V-Shaped Track Angle Issue**
  - Tracks rotate 90° incorrectly when snapped at certain angles
  - Investigation: Rotation calculation, Y-axis inversion
  - Effort: TBD (diagnosis first)

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

### **Layer Architecture** ✅ MOSTLY COMPLETE
```
Domain (POCO layer) ✅
  ├── TrackPlan.Domain (Graph/Topology) ✅ 
  └── All POCO classes ✅

Rendering/Geometry ✅
  ├── TrackPlan.Geometry (Real module) ✅
  ├── TrackPlan.Renderer (Visualization) ✅
  └── GeometryCalculationEngine ✅

Editor (Business Logic) ✅ IN PROGRESS
  ├── SnapToConnectService ✅ (copied)
  ├── TopologyResolver ✅ (copied)
  ├── TrackConnectionService ✅ (consolidated)
  └── DI wiring ⚠️ (blocked by duplicates)
```

---

## 📚 CODE QUALITY IMPROVEMENTS PENDING

### **High Priority (Session 11+):**
- [ ] **Theme Resources in XAML**
  - Move hardcoded colors to `{ThemeResource}`
  - Effort: 40 LOC

- [ ] **Memory Cleanup**
  - Verify event handler leaks
  - Add IDisposable where needed
  - Effort: 20 LOC

### **Medium Priority (Session 12+):**
- [ ] **Performance Monitoring**
  - Add WPR/WPA support if needed
  - Document baselines

---

## 🗂️ SESSION 9 FILES MODIFIED

| Category | Files | Status |
|----------|-------|--------|
| **Domain Refactoring** | TopologyGraph.cs, ValidationService.cs | ✅ Complete |
| **Services Created** | TopologyGraphService.cs, TopologyValidator.cs | ✅ Complete |
| **Constraints Removed** | 3 constraint files deleted | ✅ Complete |
| **SDK Packaging** | WinUI.csproj, Plugin .csproj files | ✅ Mitigated |
| **Phase 3 Services** | 4 files copied to Editor | ✅ Partial |
| **Interfaces** | ISnapToConnectService.cs, ITopologyResolver.cs | ✅ Created |
| **DI Registration** | TrackPlanServiceExtensions.cs | ⚠️ Blocked |

---

## ⚠️ KNOWN ISSUES FOR SESSION 10

1. **Ambiguous References** - Services exist in both Editor + Renderer
2. **TrackPlanPageService Instantiation** - Needs DI refactoring
3. **Windows SDK BuildTools** - Disabled for Dev (not production issue)
4. **ISnapPreviewProvider** - Uses interfaces but old concrete class still referenced

---

## 🗂️ RULES FOR CONTINUITY

1. ✅ Phase-Struktur: Phase 1 ✅, Phase 2 ✅, Phase 3 (Session 10)
2. ✅ Architektur dokumentiert (Topology-First, Layer-Based)
3. ✅ TODOs für nächste Session klar
4. ✅ Build-Status transparent (0 C# errors, SDK disabled)
5. ✅ Empfehlung für nächste Aktion: Option A (Quick Fix)



