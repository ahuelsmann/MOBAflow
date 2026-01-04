# MOBAflow - Session Archive (January 2025)

> This document contains detailed session logs from January 2025.  
> **Current instructions:** See `.github/instructions/copilot-instructions.md`

---

## ✅ TrainsPage Implementation: Inventarverwaltung für rollendes Material (Jan 31, 2025) 🚂✨

**Problem:** Keine UI zum Erfassen von Lokomotiven, Personenwagen und Güterwagen

**Solution:** Vollständige TrainsPage mit 3-Spalten-Layout + EntityTemplates

**Architecture Changes:**
1. **Domain Extensions:** InvoiceDate, DeliveryDate, PhotoPath zu Wagon + Locomotive
2. **ViewModel Extensions:** HasPhoto Property für Foto-Indikator
3. **MainWindowViewModel.Train.cs:** NEU - Commands für Add/Delete (Locomotives/PassengerWagons/GoodsWagons)
4. **TrainsPage.xaml:** 3-Spalten-ListView-Layout (Locomotives | PassengerWagons | GoodsWagons | Properties)
5. **EntityTemplates.xaml:** Erweiterte LocomotiveTemplate + WagonTemplate mit Purchase-Info
6. **Navigation:** TrainsPage zu NavigationService + DI + MainWindow.xaml hinzugefügt
7. **Converter:** NullToVisibilityConverter, InvertedBoolToVisibilityConverter, NullableUIntConverter, DateTimeOffsetConverter

**Build Status:** ✅ Zero errors, zero warnings

---

## ✅ Async-Everywhere Pattern Implementation (Feb 3, 2025) 🔄⚡

**Problem:** Mixed sync/async patterns, `ApplicationData.Current` threw `InvalidOperationException` on non-UI thread

**Root Cause:** WinRT APIs require UI thread context, services used synchronous methods

**Solution:** Full async/await pattern implementation across all services

**Architecture Changes:**
1. **IUiDispatcher Extended:**
   - Added `InvokeOnUiAsync<T>` for async operations with return values
   - All platforms implemented: WinUI (`TaskCompletionSource`), MAUI (`MainThread`), Blazor (direct)
2. **NavigationService → Fully Async:**
   - `InitializeAsync(Frame)` - async initialization
   - `NavigateToPageAsync(string)` - async navigation
   - `NavigateToOverviewAsync()` - async default navigation
3. **SnapToConnectService → Fully Async:**
   - `GetEndpointsAsync()` - async endpoint extraction
   - `FindSnapEndpointAsync()` - async snap detection
   - `FindSnapTargetAsync()` - async target finding
4. **IoService.SavePhotoAsync:**
   - Uses `_uiDispatcher.InvokeOnUiAsync<string?>()` for WinRT API access
   - Proper exception handling with Debug logging

**Build Status:** ✅ Zero errors, zero warnings

---

## ✅ AnyRail Import Fix: Direct EndpointNrs Index Mapping (Jan 31, 2025) 🎉

**Problem:** Import created 0 connections → 91 disconnected components (starburst pattern)

**Root Cause:** Complex BuildConnectorMapping with spatial sorting was broken and never executed

**Solution:** Reverted to simple ToTrackConnections() with direct EndpointNrs index mapping

**Impact:**
- ✅ **Import:** 96/96 connections created successfully
- ✅ **Rendering:** All 91 segments in 1 connected component
- ✅ **Validation:** Zero errors, zero warnings (Library, Connection, Rendering all PASSED)

---

## ✅ Domain-Based WorldTransform: Pure Topology Renderer (Jan 31, 2025) 🏗️🎉

**Problem:** WorldTransform was in ViewModel layer, not in Domain (violated pure topology-first)

**Solution:** Moved WorldTransform to TrackSegment (runtime-only, [JsonIgnore]), created pure TopologyRenderer

**Architecture Changes:**
1. **Transform2D moved to Domain:** `Domain/Geometry/Transform2D.cs` (was in SharedUI)
2. **TrackSegment.WorldTransform:** Runtime-only property (NOT serialized)
3. **TopologyRenderer:** NEW pure domain renderer (`SharedUI/Service/TopologyRenderer.cs`)
   - NO ViewModels, NO UI concerns, NO normalization
   - Pure graph traversal: BFS with ConstraintSolver
4. **ConstraintSolver:** Rigid/Rotational/Parametric constraint implementations

**Build Status:** ✅ Zero errors, zero warnings

---

## ✅ Complete Piko A-Gleis Geometry Catalog Implementation (Jan 31, 2025) 📐🎉

**Problem:** TrackGeometryLibrary hatte falsche Radien/Winkel + fehlende Weichen

**Solution:** Vollständige Neuimplementierung basierend auf offiziellen Piko-Katalog-Daten

**Gerade Gleise (7 Typen):**
- G239 (239.07mm), G231 (230.93mm), G119 (119.54mm)
- G115 (115.46mm), G107 (107.32mm), G62 (61.88mm)
- G940 (940mm Flexgleis)

**Bogengleise (5 Typen):**
- R1: 30°, r=360.00mm
- R2: 30°, r=421.88mm
- R3: 30°, r=483.75mm
- R4: 30°, r=545.63mm
- R9: 15°, r=907.97mm (Weichengegenbogen)

**Weichen (8 Typen):**
- WL/WR (Linksweiche/Rechtsweiche): G231 + R9-Abzweig (15°)
- BWL/BWR (Bogenweiche R2→R3): 61.88mm spacing
- BWL-R3/BWR-R3 (Bogenweiche R3→R4): 61.88mm spacing
- W3 (Dreiwegweiche): 4 Endpoints
- WY (Y-Weiche): Symmetrische Abzweigung (±15°)

**Build Status:** ✅ Zero errors, zero warnings

---

## ✅ Full Track-Graph Architecture Implementation (Jan 31, 2025) 🏗️🎉

**Architecture Components:**
1. **TrackConnector** - Lokale Position + Heading + ConnectorType
2. **ConstraintType** - Rigid, Rotational, Parametric
3. **TrackConnection** - Erweitert mit ConstraintType + Parameters
4. **ConstraintSolver** - Berechnet WorldTransform aus Parent + Constraint
5. **ConnectorMatcher** - Toleranz-basiertes Matching (1mm Position, 5° Heading)

**Import-Pipeline:**
1. Parse AnyRail XML (temporäre Koordinaten)
2. Erstelle Segmente (nur ArticleCode, KEINE Koordinaten)
3. ConnectorMatcher: Finde Connector-Paare → Connections
4. **Discard** temporäre Koordinaten (wichtig!)
5. Renderer: Berechne World-Positionen aus Connections + Constraints

**Build Status:** ✅ Zero errors, zero warnings
