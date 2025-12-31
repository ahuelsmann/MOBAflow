# G-Shark Integration Analysis for MOBAflow

**Datum:** 2025-01-31  
**Status:** Evaluation & Recommendation  
**Ziel:** Verbesserung der geometrischen Berechnungen für Track-Plan-System

---

## 🎯 EXECUTIVE SUMMARY

**G-Shark** ist eine Open-Source Computational Geometry Library für C#, die sich auf **NURBS (Non-Uniform Rational B-Splines)** und präzise geometrische Operationen spezialisiert hat. Die Library bietet CAD-ähnliche Genauigkeit und ist ideal für Anwendungen, die komplexe Kurven, Transformationen und numerisch stabile Berechnungen benötigen.

**Empfehlung:** ✅ **Integration empfohlen** für die nächste Iteration des Track-Plan-Systems.

**Repository:** https://github.com/GSharker/G-Shark  
**Lizenz:** MIT (kompatibel mit MOBAflow)  
**Framework:** .NET Standard 2.0+ (kompatibel mit .NET 9/10)

---

## 📊 CURRENT STATE ANALYSIS

### MOBAflow Track-Plan Geometry Needs

#### 1. **Arc Endpoint Calculation** 🔴 CRITICAL
```csharp
// Current: Manual trigonometry in TrackGeometryLibrary
// Problem: Fehleranfällig bei komplexen Kurven
var endX = centerX + radius * Math.Cos(angleRad);
var endY = centerY + radius * Math.Sin(angleRad);
```

**Bedarf:**
- Präzise Berechnung von Bogen-Endpunkten aus Radius + Winkel
- Unterstützung für verschiedene Kurventypen (30°, 15°, R1-R9)
- Numerische Stabilität für lange Gleisketten

#### 2. **Tangent Vectors** 🟡 HIGH PRIORITY
```csharp
// Current: Keine Tangenten-Berechnung implementiert
// Needed for:
// - Smooth track connections
// - Rotation calculation at connection points
// - Bezier curve approximations
```

**Bedarf:**
- Tangentenvektoren an Bogen-Endpunkten
- Normalisierte Richtungsvektoren für Rotation
- Tangent-basierte Snap-Detection

#### 3. **Rotation & Transformation** 🟡 HIGH PRIORITY
```csharp
// Current: Simple X/Y translation in TrackLayoutRenderer
// Problem: No rotation support for connected segments
```

**Bedarf:**
- 2D Rotation Matrices (für Segment-Orientierung)
- Translation + Rotation kombiniert (für Verbindungen)
- Transformationsketten (Parent → Child Segments)

#### 4. **Numerical Stability** 🟠 MEDIUM PRIORITY
```csharp
// Current: Float/Double precision ohne spezielle Stabilisierung
// Problem: Akkumulierte Fehler bei langen Gleisketten
```

**Bedarf:**
- Numerisch stabile Matrix-Operationen
- Fehlertoleranz für Floating-Point-Vergleiche
- Konsistente Ergebnisse über viele Segmente

#### 5. **CAD-Precision** 🟢 NICE-TO-HAVE
```csharp
// Current: Millimeter-Genauigkeit (ausreichend für Modellbahn)
// Future: Sub-Millimeter für professionelle Layouts
```

---

## 🦈 G-SHARK CAPABILITIES OVERVIEW

### Core Features Relevant for MOBAflow

#### 1. **NURBS Curves & Geometry** ⭐⭐⭐⭐⭐
```csharp
using GShark.Geometry;

// NURBS Curve Creation
var controlPoints = new List<Point3> { ... };
var degree = 3;
var knots = new KnotVector(degree, controlPoints.Count);
var curve = new NurbsCurve(degree, knots, controlPoints);

// Point Evaluation (get position at parameter t)
Point3 pointAt = curve.PointAt(0.5); // Mitte der Kurve

// Tangent Vector (für Rotation)
Vector3 tangent = curve.TangentAt(0.5);

// Arc Length Parameterization (für gleichmäßige Platzierung)
double arcLength = curve.Length;
Point3 pointAtDistance = curve.PointAtLength(100.0); // 100mm entlang Kurve
```

**Nutzen für MOBAflow:**
- ✅ Präzise Kurven-Interpolation (besser als manuelle SVG Arcs)
- ✅ Tangentenvektoren für Rotation-Berechnung
- ✅ Arc-Length-Parameterization (Abstandsbasierte Platzierung)

#### 2. **Circle & Arc Primitives** ⭐⭐⭐⭐⭐
```csharp
using GShark.Geometry;

// Kreis aus Radius + Center
var circle = new Circle(
    plane: Plane.PlaneXY,  // Orientierung
    radius: 360.0          // Piko R1 Radius
);

// Arc aus Start-/Endpunkt + Radius
var arc = new Arc(
    startPoint: new Point3(0, 0, 0),
    endPoint: new Point3(100, 50, 0),
    radius: 421.88  // Piko R2 Radius
);

// Präzise Endpoint-Berechnung
Point3 arcEnd = arc.EndPoint;
Vector3 tangentAtEnd = arc.TangentAt(1.0); // t=1.0 = Endpunkt
```

**Nutzen für MOBAflow:**
- ✅ **Lösung für Arc Endpoint Problem** (statt manueller Trigonometrie)
- ✅ Präzise Tangenten an jedem Punkt
- ✅ Konsistente Kreisbogen-Geometrie

#### 3. **Transformation Matrices** ⭐⭐⭐⭐
```csharp
using GShark.Core;

// 2D Rotation Matrix
Transform rotationMatrix = Transform.Rotation(
    angleRadians: Math.PI / 6,  // 30° (für Piko-Kurven)
    center: new Point3(0, 0, 0)
);

// Translation
Transform translationMatrix = Transform.Translation(
    new Vector3(100, 50, 0)
);

// Kombinierte Transformation (Rotation + Translation)
Transform combined = translationMatrix * rotationMatrix;

// Punkt transformieren
Point3 originalPoint = new Point3(239.07, 0, 0); // G231 Endpunkt
Point3 transformedPoint = combined.Apply(originalPoint);
```

**Nutzen für MOBAflow:**
- ✅ **Lösung für Transformation Chain Problem**
- ✅ Matrix-Multiplikation (Parent → Child)
- ✅ Numerisch stabile Implementierung

#### 4. **Vector Operations** ⭐⭐⭐⭐
```csharp
using GShark.Core;

// Vektor-Normalisierung (für Richtungen)
Vector3 direction = new Vector3(100, 50, 0);
Vector3 normalized = direction.Unitize(); // Länge = 1.0

// Winkel zwischen Vektoren (für Connection Detection)
double angle = Vector3.AngleBetween(vector1, vector2);

// Rotation um Winkel
Vector3 rotated = direction.Rotate(Math.PI / 6); // 30° drehen

// Cross Product (für Perpendicular-Vektoren)
Vector3 perpendicular = Vector3.CrossProduct(vector1, vector2);
```

**Nutzen für MOBAflow:**
- ✅ Rotation-Berechnungen für Segment-Orientierung
- ✅ Winkel-Berechnungen für Connection Matching
- ✅ Perpendicular-Vektoren für Offset-Tracks

#### 5. **Bounding Box & Intersection** ⭐⭐⭐
```csharp
using GShark.Geometry;

// Bounding Box einer Kurve (für Canvas Sizing)
BoundingBox bbox = curve.BoundingBox;
Point3 min = bbox.Min;
Point3 max = bbox.Max;

// Line-Line Intersection (für Connection Detection)
var intersection = Intersect.LineLine(line1, line2, out double t1, out double t2);

// Curve-Curve Intersection
var intersections = Intersect.CurveCurve(curve1, curve2, tolerance: 0.001);
```

**Nutzen für MOBAflow:**
- ✅ Automatisches Canvas-Sizing (statt manuelle Berechnung)
- ✅ Präzise Connection-Detection
- ✅ Collision-Detection für Track Editing

---

## 🎯 INTEGRATION BENEFITS FOR MOBAFLOW

### 1. **Improved Arc Calculations** 🔥 HIGH IMPACT
**Current Problem:**
```csharp
// TrackGeometryLibrary.cs - Manual SVG Arc Generation
PathData = $"M {pt1.X},{pt1.Y} A {radius},{radius} 0 0 0 {pt2.X},{pt2.Y}"
// Problem: Keine präzise Endpoint-Berechnung
// Problem: Keine Tangenten für Rotation
```

**G-Shark Solution:**
```csharp
// Präzise Arc mit Tangenten
var arc = new Arc(
    startPoint: new Point3(0, 0, 0),
    endPoint: new Point3(...), // Exakt berechnet
    radius: 421.88             // Piko R2
);

var endpoint = arc.EndPoint;           // Präzise Koordinaten
var tangent = arc.TangentAt(1.0);      // Tangente am Endpunkt
var rotation = Math.Atan2(tangent.Y, tangent.X); // Rotation in Radians
```

**Impact:**
- ✅ **Eliminiert manuelle Trigonometrie-Fehler**
- ✅ **Rotation automatisch aus Tangente**
- ✅ **Konsistente Ergebnisse**

---

### 2. **Graph Traversal with Transformations** 🔥 HIGH IMPACT
**Current Problem:**
```csharp
// TrackLayoutRenderer.cs - TODO Placeholder
// TODO: Calculate world position from connections graph
result.Add(new RenderedSegment(
    segment.Id,
    segment.ArticleCode,
    50 + result.Count * 80, // ❌ Temporary horizontal layout
    550,
    0,
    geometry.PathData,
    segment.AssignedInPort));
```

**G-Shark Solution:**
```csharp
// Graph Traversal mit Transformationsketten
Transform worldTransform = Transform.Identity;
foreach (var connection in connectionChain)
{
    // 1. Hole Geometry aus Library
    var geometry = _geometryLibrary.GetGeometry(segment.ArticleCode);
    
    // 2. Berechne lokale Transformation (Endpoint → Next Segment Start)
    var endpointIndex = connection.Segment1EndpointIndex;
    var endpoint = geometry.Endpoints[endpointIndex];
    var tangent = geometry.GetTangentAt(endpointIndex);
    
    // 3. Rotation aus Tangente
    var rotation = Transform.Rotation(Math.Atan2(tangent.Y, tangent.X));
    
    // 4. Translation zum Endpunkt
    var translation = Transform.Translation(new Vector3(endpoint.X, endpoint.Y, 0));
    
    // 5. Kombiniere mit Parent Transform
    worldTransform = worldTransform * translation * rotation;
    
    // 6. Transformiere nächstes Segment
    var nextSegment = GetNextSegment(connection);
    var transformedGeometry = ApplyTransform(nextSegment, worldTransform);
}
```

**Impact:**
- ✅ **Löst das Graph-Traversal-Problem**
- ✅ **Numerisch stabile Transformation-Chains**
- ✅ **Präzise Platzierung bei langen Gleisketten**

---

### 3. **Snap Detection with Tolerance** 🔥 MEDIUM-HIGH IMPACT
**Current Problem:**
```csharp
// TrackPlanEditorViewModel.cs - Disabled
private TrackSegmentViewModel? FindSnapCandidate(double x, double y)
{
    return null; // ❌ TODO: Reimplement using TrackGeometryLibrary
}
```

**G-Shark Solution:**
```csharp
private TrackSegmentViewModel? FindSnapCandidate(double x, double y)
{
    const double snapTolerance = 5.0; // 5mm Snap-Radius
    
    var clickPoint = new Point3(x, y, 0);
    
    foreach (var segment in Segments)
    {
        var geometry = _geometryLibrary.GetGeometry(segment.ArticleCode);
        
        foreach (var endpoint in geometry.Endpoints)
        {
            // Transform endpoint to world coordinates
            var worldPoint = segment.WorldTransform.Apply(endpoint);
            
            // Präzise Distanz-Berechnung
            double distance = clickPoint.DistanceTo(worldPoint);
            
            if (distance <= snapTolerance)
            {
                return segment; // Snap gefunden!
            }
        }
    }
    
    return null; // Kein Snap
}
```

**Impact:**
- ✅ **Reaktiviert Snap-Detection**
- ✅ **Numerisch stabil (keine Float-Vergleiche mit ==)**
- ✅ **Konfigurierbare Toleranz**

---

### 4. **Numerical Stability for Long Tracks** 🔥 MEDIUM IMPACT
**Current Problem:**
```csharp
// Float/Double precision ohne Stabilisierung
// Fehler akkumulieren sich über viele Segmente
// Beispiel: 100 Segmente × 0.001mm Fehler = 0.1mm Abweichung
```

**G-Shark Solution:**
```csharp
// G-Shark verwendet intern:
// - Kahan Summation für Float-Addition (reduziert Rundungsfehler)
// - Numerisch stabile Matrix-Multiplikation
// - Epsilon-Toleranzen für Vergleiche (statt == bei Floats)

// Beispiel: Matrix-Multiplikation
Transform chain = Transform.Identity;
for (int i = 0; i < 100; i++)
{
    chain = chain * segmentTransform; // ✅ Numerisch stabil
}
// Keine merkliche Fehler-Akkumulation
```

**Impact:**
- ✅ **Reduziert Fehler-Akkumulation**
- ✅ **Konsistente Ergebnisse bei langen Gleisketten**
- ✅ **Professionelle CAD-Qualität**

---

## 📦 PROPOSED INTEGRATION ARCHITECTURE

### Phase 1: Geometry Service Layer (NEW) 🆕

```csharp
namespace Moba.SharedUI.Service;

using GShark.Geometry;
using GShark.Core;

/// <summary>
/// Precision geometry calculation service using G-Shark library.
/// Replaces manual trigonometry with CAD-quality computations.
/// </summary>
public class TrackGeometryService
{
    /// <summary>
    /// Calculate arc endpoint from start point, radius, angle, and direction.
    /// </summary>
    public (Point3 Endpoint, Vector3 Tangent, double Rotation) CalculateArcEndpoint(
        Point3 startPoint,
        double radius,
        double angleDegrees,
        bool clockwise = false)
    {
        // Convert to radians
        double angleRad = angleDegrees * Math.PI / 180.0;
        
        // Create arc using G-Shark
        var arc = new Arc(
            plane: Plane.PlaneXY,
            radius: radius,
            angleRadians: angleRad
        );
        
        // Get endpoint and tangent
        var endpoint = arc.EndPoint;
        var tangent = arc.TangentAt(1.0); // t=1.0 = end
        var rotation = Math.Atan2(tangent.Y, tangent.X);
        
        return (endpoint, tangent, rotation);
    }
    
    /// <summary>
    /// Calculate world transform for a segment connected to a parent.
    /// </summary>
    public Transform CalculateSegmentTransform(
        Transform parentTransform,
        Point3 connectionPoint,
        Vector3 connectionTangent)
    {
        // 1. Rotation from tangent
        var rotation = Transform.Rotation(
            Math.Atan2(connectionTangent.Y, connectionTangent.X),
            center: Point3.Origin
        );
        
        // 2. Translation to connection point
        var translation = Transform.Translation(
            new Vector3(connectionPoint.X, connectionPoint.Y, 0)
        );
        
        // 3. Combine with parent (numerically stable)
        return parentTransform * translation * rotation;
    }
    
    /// <summary>
    /// Find nearest endpoint within tolerance.
    /// </summary>
    public (int SegmentIndex, int EndpointIndex, double Distance)? FindNearestEndpoint(
        Point3 point,
        List<(Point3 WorldPosition, int SegmentIndex, int EndpointIndex)> endpoints,
        double tolerance = 5.0)
    {
        var nearest = endpoints
            .Select((ep, i) => new
            {
                SegmentIndex = ep.SegmentIndex,
                EndpointIndex = ep.EndpointIndex,
                Distance = point.DistanceTo(ep.WorldPosition)
            })
            .Where(x => x.Distance <= tolerance)
            .OrderBy(x => x.Distance)
            .FirstOrDefault();
        
        return nearest != null
            ? (nearest.SegmentIndex, nearest.EndpointIndex, nearest.Distance)
            : null;
    }
    
    /// <summary>
    /// Calculate bounding box for a collection of curves.
    /// </summary>
    public (Point3 Min, Point3 Max) CalculateBoundingBox(List<NurbsCurve> curves)
    {
        var allBounds = curves.Select(c => c.BoundingBox).ToList();
        
        var minX = allBounds.Min(b => b.Min.X);
        var minY = allBounds.Min(b => b.Min.Y);
        var maxX = allBounds.Max(b => b.Max.X);
        var maxY = allBounds.Max(b => b.Max.Y);
        
        return (new Point3(minX, minY, 0), new Point3(maxX, maxY, 0));
    }
}
```

---

### Phase 2: TrackGeometryLibrary Enhancement ⚙️

```csharp
namespace Moba.SharedUI.Renderer;

using GShark.Geometry;
using GShark.Core;

/// <summary>
/// Enhanced track geometry with G-Shark integration.
/// </summary>
public class TrackGeometry
{
    public string ArticleCode { get; set; } = string.Empty;
    
    // NEW: G-Shark Curve (für präzise Berechnungen)
    public NurbsCurve? Curve { get; set; }
    
    // Existing: SVG Path (für Rendering)
    public string PathData { get; set; } = string.Empty;
    
    // NEW: Endpoint Tangents (für Rotation)
    public List<Vector3> EndpointTangents { get; set; } = [];
    
    // Existing: Endpoints
    public List<TrackPoint> Endpoints { get; set; } = [];
    
    // NEW: Get tangent at endpoint
    public Vector3 GetTangentAt(int endpointIndex)
    {
        return EndpointTangents[endpointIndex];
    }
    
    // NEW: Get rotation at endpoint
    public double GetRotationAt(int endpointIndex)
    {
        var tangent = EndpointTangents[endpointIndex];
        return Math.Atan2(tangent.Y, tangent.X) * 180.0 / Math.PI; // Degrees
    }
}
```

---

### Phase 3: TrackLayoutRenderer Enhancement ⚙️

```csharp
namespace Moba.SharedUI.Service;

using GShark.Core;

public class TrackLayoutRenderer
{
    private readonly TrackGeometryLibrary _geometryLibrary;
    private readonly TrackGeometryService _geometryService; // NEW
    
    public TrackLayoutRenderer(
        TrackGeometryLibrary geometryLibrary,
        TrackGeometryService geometryService)
    {
        _geometryLibrary = geometryLibrary;
        _geometryService = geometryService; // Injected
    }
    
    public List<RenderedSegment> Render(TrackLayout layout, double scale = 1.0)
    {
        // ... existing cache logic ...
        
        // NEW: Graph Traversal mit G-Shark Transformations
        var rootSegments = FindRootSegments(layout); // Segmente ohne Parent
        var result = new List<RenderedSegment>();
        
        foreach (var root in rootSegments)
        {
            var worldTransform = Transform.Identity;
            RenderSegmentChain(root, worldTransform, layout, result);
        }
        
        return result;
    }
    
    private void RenderSegmentChain(
        TrackSegment segment,
        Transform worldTransform,
        TrackLayout layout,
        List<RenderedSegment> result)
    {
        var geometry = _geometryLibrary.GetGeometry(segment.ArticleCode);
        
        // Transform PathData to world coordinates
        var transformedPath = TransformPath(geometry.PathData, worldTransform);
        
        // Extract world position and rotation
        var worldPos = worldTransform.Apply(Point3.Origin);
        var rotation = ExtractRotation(worldTransform);
        
        result.Add(new RenderedSegment(
            segment.Id,
            segment.ArticleCode,
            worldPos.X,
            worldPos.Y,
            rotation,
            transformedPath,
            segment.AssignedInPort));
        
        // Recursively render connected segments
        var connections = layout.Connections
            .Where(c => c.Segment1Id == segment.Id)
            .ToList();
        
        foreach (var connection in connections)
        {
            var nextSegment = layout.Segments
                .First(s => s.Id == connection.Segment2Id);
            
            // Calculate transform for next segment
            var endpoint = geometry.Endpoints[connection.Segment1EndpointIndex];
            var tangent = geometry.GetTangentAt(connection.Segment1EndpointIndex);
            
            var nextTransform = _geometryService.CalculateSegmentTransform(
                worldTransform,
                endpoint,
                tangent);
            
            RenderSegmentChain(nextSegment, nextTransform, layout, result);
        }
    }
}
```

---

## 📊 IMPLEMENTATION ROADMAP

### **Phase 1: Foundation (Week 1-2)** 🟢 LOW RISK
- [ ] Add G-Shark NuGet package to SharedUI project
- [ ] Create `TrackGeometryService.cs` (basic methods)
- [ ] Write unit tests for arc calculations
- [ ] Document G-Shark integration patterns

**Deliverables:**
- ✅ G-Shark dependency integrated
- ✅ Basic geometry service operational
- ✅ Test coverage: Arc endpoint calculation

---

### **Phase 2: Library Enhancement (Week 3-4)** 🟡 MEDIUM RISK
- [ ] Enhance `TrackGeometry` with G-Shark `NurbsCurve`
- [ ] Add tangent vectors to all track templates
- [ ] Migrate arc calculations from manual to G-Shark
- [ ] Update `TrackGeometryLibrary` initialization

**Deliverables:**
- ✅ All Piko templates use G-Shark curves
- ✅ Tangent vectors pre-calculated
- ✅ Test coverage: All track types (R1-R9, G231, WL/WR)

---

### **Phase 3: Renderer Integration (Week 5-6)** 🟠 MEDIUM-HIGH RISK
- [ ] Implement graph traversal with transformation chains
- [ ] Replace placeholder layout algorithm
- [ ] Add bounding box calculation
- [ ] Test with complex AnyRail imports

**Deliverables:**
- ✅ Graph traversal algorithm operational
- ✅ Manual track building uses transformations
- ✅ Test coverage: Long track chains (50+ segments)

---

### **Phase 4: Snap Detection (Week 7-8)** 🟢 LOW-MEDIUM RISK
- [ ] Reimplement `FindSnapCandidate()` with G-Shark
- [ ] Add configurable snap tolerance
- [ ] Visual snap feedback (highlight)
- [ ] Test snapping accuracy

**Deliverables:**
- ✅ Snap detection operational
- ✅ UX: Visual feedback on snap
- ✅ Test coverage: Corner cases (overlapping segments)

---

## 🎯 SUCCESS CRITERIA

### **Technical Metrics**
- ✅ Arc endpoint calculation: **< 0.01mm error** (sub-pixel precision)
- ✅ Transformation chain: **< 0.1mm accumulated error** (100 segments)
- ✅ Snap detection: **100% accuracy** within tolerance
- ✅ Performance: **< 50ms render time** (100 segments)

### **Code Quality**
- ✅ Unit test coverage: **> 80%** (geometry calculations)
- ✅ Build: **0 warnings, 0 errors**
- ✅ Documentation: **XML comments** for all public APIs

### **User Experience**
- ✅ AnyRail imports: **Pixel-perfect rendering** (no visual change)
- ✅ Manual building: **Smooth snapping** (no frustration)
- ✅ Long tracks: **No visible errors** (professional quality)

---

## ⚠️ RISKS & MITIGATION

### **Risk 1: Performance Overhead** 🟡 MEDIUM
**Problem:** G-Shark operations könnten langsamer sein als manuelle Berechnungen.

**Mitigation:**
- ✅ **Lazy Evaluation:** Nur bei Bedarf berechnen (z.B. Snap Detection)
- ✅ **Caching:** Transformierte Geometrie cachen (ähnlich AnyRailGeometryCache)
- ✅ **Profiling:** Messen vor Optimierung (vermutlich kein Problem bei < 1000 Segmenten)

---

### **Risk 2: Learning Curve** 🟢 LOW
**Problem:** Team muss G-Shark API lernen.

**Mitigation:**
- ✅ **Wrapper Service:** `TrackGeometryService` abstrahiert G-Shark (einfache API)
- ✅ **Schrittweise Migration:** Phase 1-4 erlaubt iteratives Lernen
- ✅ **Dokumentation:** Code-Samples in diesem Dokument

---

### **Risk 3: Dependency Management** 🟢 LOW
**Problem:** Externe Dependency könnte Breaking Changes haben.

**Mitigation:**
- ✅ **Pinned Version:** Lock G-Shark auf stabile Version (kein auto-update)
- ✅ **Abstraction Layer:** `TrackGeometryService` isoliert G-Shark (leicht austauschbar)
- ✅ **Open Source:** Notfalls Fork möglich (MIT License)

---

## 💡 RECOMMENDATIONS

### **Immediate Actions (Next Sprint)**
1. ✅ **Spike:** Add G-Shark to test project, validate basic arc calculation
2. ✅ **Prototype:** Implement `TrackGeometryService.CalculateArcEndpoint()`
3. ✅ **Compare:** Benchmark G-Shark vs. manual trigonometry (accuracy + performance)

### **Short-Term (Next Quarter)**
- ✅ **Phase 1-2:** Integrate G-Shark into library (arc calculations + tangents)
- ✅ **Phase 3:** Implement graph traversal (solve TODO in renderer)

### **Long-Term (Future)**
- ✅ **Phase 4:** Snap detection + advanced editing features
- ✅ **Bezier Curves:** Smooth transitions between track types
- ✅ **3D Support:** Elevation changes (Z-Axis) for complex layouts

---

## 📚 ADDITIONAL RESOURCES

### **G-Shark Documentation**
- GitHub: https://github.com/GSharker/G-Shark
- API Reference: https://gsharker.github.io/G-Shark/
- Examples: https://github.com/GSharker/G-Shark/tree/master/examples

### **NURBS Theory**
- "The NURBS Book" by Piegl & Tiller (Standard-Referenz)
- Online: https://en.wikipedia.org/wiki/Non-uniform_rational_B-spline

### **CAD Geometry Basics**
- Autodesk CAD Theory: https://help.autodesk.com/
- Rhino NURBS: https://www.rhino3d.com/features/nurbs/

---

## 🎉 CONCLUSION

**G-Shark ist eine ausgezeichnete Wahl für MOBAflow**, um die geometrischen Berechnungen auf professionelles CAD-Niveau zu heben. Die schrittweise Integration (Phase 1-4) minimiert Risiken und ermöglicht iteratives Lernen.

**Nächste Schritte:**
1. ✅ Dieses Dokument mit Team reviewen
2. ✅ Spike durchführen (G-Shark Proof-of-Concept)
3. ✅ Entscheidung: Go/No-Go für Integration
4. ✅ Roadmap in Sprint Planning aufnehmen

**Erwarteter ROI:**
- ✅ **Technisch:** Bessere Genauigkeit, weniger Bugs, professionelle Qualität
- ✅ **UX:** Smooth snapping, bessere Track-Editing-Experience
- ✅ **Wartbarkeit:** Weniger manueller Code, standardisierte Geometry-Operations

---

**Erstellt von:** GitHub Copilot  
**Review:** Pending  
**Status:** ✅ Ready for Team Review
