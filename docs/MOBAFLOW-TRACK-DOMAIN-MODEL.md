# MOBAflow Track-Plan Domain Model (Explicit)

**Datum:** 2025-01-31  
**Status:** Domain Design (Learning from G-Shark, NOT using as dependency)  
**Philosophie:** Topologie-First, kein Zeichenprogramm, sondern Gleis-CAD

---

## 🎯 DESIGN PRINCIPLES

### **Wir sind KEIN Zeichenprogramm**
❌ Nicht: Freies Zeichnen von Linien/Kurven  
✅ Sondern: **Gleis-CAD** mit realen Gleisgeometrien (Piko, Roco, Tillig)

### **Wir denken in Topologie, nicht Koordinaten**
❌ Nicht: X/Y-Koordinaten im Domain Model  
✅ Sondern: **ArticleCode + Connections** (Topologie-Graph)

### **Wir lernen von G-Shark, verwenden es aber nicht**
❌ Nicht: G-Shark als NuGet-Dependency  
✅ Sondern: **Mathematische Konzepte** übernehmen (Transformationsketten, Numerische Stabilität)

---

## 📊 G-SHARK vs. MOBAflow RENDERER COMPARISON

### **G-Shark Renderer Architecture**
```
┌─────────────────────────────────────────────────────────┐
│ G-Shark (General-Purpose Geometry Library)              │
├─────────────────────────────────────────────────────────┤
│ 1. NURBS Curves (B-Splines, Bezier, Arcs)              │
│    → Arbitrary curves, control points, weights          │
│    → Überkill für Modellbahn (nur Geraden + Kreisbögen)│
│                                                          │
│ 2. Transform Chains (Matrix4x4)                         │
│    → 3D Transformationen (Translation, Rotation, Scale) │
│    → Wir brauchen nur 2D (XY-Ebene)                    │
│                                                          │
│ 3. Arc-Length Parameterization                          │
│    → Punkt bei Länge t finden                           │
│    → Nützlich für gleichmäßige Traverse                │
│                                                          │
│ 4. Tangent Vectors (Derivatives)                        │
│    → Ableitung der Kurve → Richtung                    │
│    → ✅ KRITISCH für Rotation an Verbindungspunkten    │
│                                                          │
│ 5. Numerical Stability (Kahan Summation)                │
│    → Fehlerreduktion bei Float-Arithmetik               │
│    → ✅ WICHTIG für lange Gleisketten                  │
└─────────────────────────────────────────────────────────┘
```

### **MOBAflow Renderer (Current State)**
```
┌─────────────────────────────────────────────────────────┐
│ TrackLayoutRenderer (Specialized for Model Railways)    │
├─────────────────────────────────────────────────────────┤
│ 1. Track Geometry (Straight + Arc only)                 │
│    ✅ Einfacher: Nur Gerade (Line) + Kreisbogen (Arc)  │
│    ✅ Reicht für 99% aller Modellbahn-Gleise           │
│                                                          │
│ 2. Transform Chains (Missing!)                          │
│    ❌ TODO: "Calculate world position from connections" │
│    ❌ Aktuell: Temporary horizontal layout              │
│                                                          │
│ 3. Topology Graph (Connections)                         │
│    ✅ Vorhanden: TrackLayout.Connections                │
│    ❌ Aber: Nicht genutzt für Rendering                │
│                                                          │
│ 4. Tangent Vectors (Missing!)                           │
│    ❌ Keine Tangenten → Keine korrekte Rotation        │
│    ❌ EndpointHeadingsDeg ist hartcodiert (0°, 180°)   │
│                                                          │
│ 5. Numerical Stability (Naive)                          │
│    ❌ Standard Float-Arithmetik                         │
│    ❌ Fehler akkumulieren bei langen Ketten            │
└─────────────────────────────────────────────────────────┘
```

---

## 🧮 MATHEMATICAL CONCEPTS TO ADOPT (from G-Shark)

### **1. Transformation Chains (2D Matrix Algebra)**

#### **G-Shark Approach (3D):**
```csharp
// Matrix4x4 (Overkill für 2D)
Transform worldTransform = Transform.Identity;
worldTransform = worldTransform * translation * rotation * scale;
```

#### **MOBAflow Approach (2D Specialized):**
```csharp
/// <summary>
/// 2D Affine Transformation (3x3 Matrix, homogeneous coordinates).
/// Spezialisiert für Gleisplan (Translation + Rotation, kein Scale).
/// </summary>
public readonly struct Transform2D
{
    // Matrix Layout:
    // | m11  m12  tx |   | cos(θ)  -sin(θ)  dx |
    // | m21  m22  ty | = | sin(θ)   cos(θ)  dy |
    // |  0    0    1 |   |   0        0      1 |
    
    public readonly double M11, M12, Tx;
    public readonly double M21, M22, Ty;
    
    public static Transform2D Identity => new(1, 0, 0, 0, 1, 0);
    
    /// <summary>
    /// Translation (Verschiebung um dx, dy).
    /// </summary>
    public static Transform2D Translation(double dx, double dy)
        => new(1, 0, dx, 0, 1, dy);
    
    /// <summary>
    /// Rotation (Drehung um Winkel in Radians).
    /// </summary>
    public static Transform2D Rotation(double angleRadians)
    {
        var cos = Math.Cos(angleRadians);
        var sin = Math.Sin(angleRadians);
        return new(cos, -sin, 0, sin, cos, 0);
    }
    
    /// <summary>
    /// Matrix-Multiplikation (Verkettung von Transformationen).
    /// Numerisch stabil durch explizite Matrix-Multiplikation.
    /// </summary>
    public static Transform2D operator *(Transform2D a, Transform2D b)
    {
        return new Transform2D(
            a.M11 * b.M11 + a.M12 * b.M21,
            a.M11 * b.M12 + a.M12 * b.M22,
            a.M11 * b.Tx  + a.M12 * b.Ty + a.Tx,
            a.M21 * b.M11 + a.M22 * b.M21,
            a.M21 * b.M12 + a.M22 * b.M22,
            a.M21 * b.Tx  + a.M22 * b.Ty + a.Ty
        );
    }
    
    /// <summary>
    /// Punkt transformieren (x, y) → (x', y').
    /// </summary>
    public TrackPoint Apply(TrackPoint point)
    {
        return new TrackPoint(
            M11 * point.X + M12 * point.Y + Tx,
            M21 * point.X + M22 * point.Y + Ty
        );
    }
    
    /// <summary>
    /// Vektor transformieren (nur Rotation, keine Translation).
    /// </summary>
    public TrackVector ApplyVector(TrackVector vector)
    {
        return new TrackVector(
            M11 * vector.X + M12 * vector.Y,
            M21 * vector.X + M22 * vector.Y
        );
    }
}
```

**Warum besser als G-Shark für uns:**
- ✅ **2D-optimiert** (keine unnötige Z-Koordinate)
- ✅ **Explizit** (keine Black-Box-Library)
- ✅ **Einfacher zu verstehen** (Matrix-Algebra 101)
- ✅ **Gleiche numerische Stabilität** (Matrix-Multiplikation)

---

### **2. Tangent Vectors (Derivatives)**

#### **G-Shark Approach (NURBS Derivatives):**
```csharp
// Komplexe NURBS-Ableitung (Overkill)
Vector3 tangent = curve.TangentAt(t);
```

#### **MOBAflow Approach (Simple Analytical Formulas):**
```csharp
/// <summary>
/// Tangenten-Vektor an einem Punkt (Richtung der Kurve).
/// </summary>
public readonly struct TrackVector
{
    public readonly double X, Y;
    
    public TrackVector(double x, double y)
    {
        X = x;
        Y = y;
    }
    
    /// <summary>
    /// Länge des Vektors (Pythagoras).
    /// </summary>
    public double Length => Math.Sqrt(X * X + Y * Y);
    
    /// <summary>
    /// Normalisierter Vektor (Länge = 1).
    /// </summary>
    public TrackVector Normalize()
    {
        var len = Length;
        return len > 1e-10 ? new TrackVector(X / len, Y / len) : new TrackVector(1, 0);
    }
    
    /// <summary>
    /// Winkel des Vektors (in Radians, 0° = nach rechts).
    /// </summary>
    public double AngleRadians => Math.Atan2(Y, X);
    
    /// <summary>
    /// Tangente einer Geraden (konstante Richtung).
    /// </summary>
    public static TrackVector FromLine(TrackPoint start, TrackPoint end)
    {
        return new TrackVector(end.X - start.X, end.Y - start.Y).Normalize();
    }
    
    /// <summary>
    /// Tangente eines Kreisbogens am Endpunkt (analytisch).
    /// </summary>
    public static TrackVector FromArcEndpoint(
        TrackPoint center,
        double radius,
        double angleRadians,
        bool clockwise)
    {
        // Ableitung von (x,y) = center + r*(cos(θ), sin(θ))
        // → dx/dθ = -r*sin(θ), dy/dθ = r*cos(θ)
        var theta = angleRadians;
        var tangent = new TrackVector(-Math.Sin(theta), Math.Cos(theta));
        return clockwise ? new TrackVector(-tangent.X, -tangent.Y) : tangent;
    }
}
```

**Warum besser als G-Shark für uns:**
- ✅ **Analytische Formeln** (Gerade, Kreisbogen) → exakt, kein Approximationsfehler
- ✅ **Einfach** (keine NURBS-Mathematik nötig)
- ✅ **Schneller** (keine numerische Ableitung)

---

### **3. Numerical Stability (Kahan Summation for Long Chains)**

#### **G-Shark Approach:**
```csharp
// Interne Kahan Summation bei Matrix-Operationen
// (Black-Box, nicht direkt sichtbar)
```

#### **MOBAflow Approach (Explicit Accumulator):**
```csharp
/// <summary>
/// Numerisch stabiler Akkumulator für Transformationsketten.
/// Verhindert Fehler-Akkumulation bei langen Gleisketten.
/// </summary>
public class TransformChain
{
    private Transform2D _accumulated = Transform2D.Identity;
    private double _errorCompensation = 0.0; // Kahan summation state
    
    /// <summary>
    /// Füge Transformation hinzu (numerisch stabil).
    /// </summary>
    public void Append(Transform2D transform)
    {
        // Standard: _accumulated = _accumulated * transform;
        // Problem: Rundungsfehler akkumulieren sich
        
        // Lösung: Periodisches Re-Orthogonalisieren (Gram-Schmidt)
        _accumulated = _accumulated * transform;
        
        // Alle 10 Schritte: Korrigiere Rotation-Matrix (falls verzerrt)
        if (++_stepCount % 10 == 0)
        {
            _accumulated = ReOrthogonalize(_accumulated);
        }
    }
    
    private int _stepCount = 0;
    
    /// <summary>
    /// Gram-Schmidt Orthogonalisierung (korrigiert Rundungsfehler).
    /// Stellt sicher, dass Rotation-Matrix orthonormal bleibt.
    /// </summary>
    private static Transform2D ReOrthogonalize(Transform2D t)
    {
        // Erste Zeile normalisieren
        var len1 = Math.Sqrt(t.M11 * t.M11 + t.M12 * t.M12);
        var m11 = t.M11 / len1;
        var m12 = t.M12 / len1;
        
        // Zweite Zeile orthogonalisieren
        var dot = m11 * t.M21 + m12 * t.M22;
        var m21 = t.M21 - dot * m11;
        var m22 = t.M22 - dot * m12;
        
        // Zweite Zeile normalisieren
        var len2 = Math.Sqrt(m21 * m21 + m22 * m22);
        m21 /= len2;
        m22 /= len2;
        
        return new Transform2D(m11, m12, t.Tx, m21, m22, t.Ty);
    }
    
    public Transform2D Result => _accumulated;
}
```

**Warum besser als G-Shark für uns:**
- ✅ **Explizit** (wir verstehen, was passiert)
- ✅ **Kontrollierbar** (wann Re-Orthogonalisierung)
- ✅ **Ausreichend** (keine Kahan-Summation nötig für 2D-Rotation)

---

## 🏗️ EXPLICIT MOBAflow TRACK-PLAN DOMAIN MODEL

### **Principle: Separation of Concerns**

```
┌──────────────────────────────────────────────────────────┐
│ DOMAIN LAYER (Pure Topology)                             │
├──────────────────────────────────────────────────────────┤
│ TrackSegment:                                             │
│   - Id: string                                            │
│   - ArticleCode: string (z.B. "R2", "G231", "WL")        │
│   - AssignedInPort: uint?                                 │
│                                                           │
│ TrackConnection:                                          │
│   - Segment1Id, Segment1EndpointIndex                     │
│   - Segment2Id, Segment2EndpointIndex                     │
│                                                           │
│ TrackLayout:                                              │
│   - Segments: List<TrackSegment>                          │
│   - Connections: List<TrackConnection>                    │
│                                                           │
│ ✅ Keine Koordinaten! Nur Topologie-Graph!              │
└──────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────┐
│ GEOMETRY LAYER (Track Library + Calculations)            │
├──────────────────────────────────────────────────────────┤
│ TrackGeometry:                                            │
│   - ArticleCode: string                                   │
│   - GeometryType: Straight | Arc                          │
│   - Length: double (für Gerade)                           │
│   - Radius, AngleDegrees: double (für Bogen)             │
│   - Endpoints: List<TrackPoint> (lokale Koordinaten)     │
│   - EndpointTangents: List<TrackVector> (Richtungen)    │
│                                                           │
│ TrackGeometryLibrary:                                     │
│   - GetGeometry(ArticleCode): TrackGeometry?              │
│   - Piko, Roco, Tillig Templates                         │
│                                                           │
│ TrackCalculator:                                          │
│   - CalculateArcEndpoint(...)                             │
│   - CalculateTangent(...)                                 │
│   - CreateTransformChain(...)                             │
│                                                           │
│ ✅ Geometrie berechnen, nicht speichern!                │
└──────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────┐
│ RENDERER LAYER (Topology → World Coordinates)            │
├──────────────────────────────────────────────────────────┤
│ TrackLayoutRenderer:                                      │
│   - Render(TrackLayout): List<RenderedSegment>            │
│   - Traversiere Topologie-Graph                          │
│   - Berechne World-Koordinaten (Transform Chains)        │
│   - Generiere SVG PathData                                │
│                                                           │
│ RenderedSegment:                                          │
│   - Id, ArticleCode                                       │
│   - WorldX, WorldY, WorldRotation                         │
│   - PathData (SVG)                                        │
│                                                           │
│ ✅ Rendering-Zeit berechnet, nicht persistiert!         │
└──────────────────────────────────────────────────────────┘
```

---

## 📐 CORE DOMAIN TYPES (Explicit Modeling)

### **1. TrackPoint (2D Point)**
```csharp
namespace Moba.Domain.TrackPlan.Geometry;

/// <summary>
/// 2D Punkt (lokale oder Welt-Koordinaten).
/// Immutable Value Object.
/// </summary>
public readonly struct TrackPoint
{
    public readonly double X, Y;
    
    public TrackPoint(double x, double y)
    {
        X = x;
        Y = y;
    }
    
    public static TrackPoint Origin => new(0, 0);
    
    /// <summary>
    /// Euklidische Distanz zu anderem Punkt.
    /// </summary>
    public double DistanceTo(TrackPoint other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
    
    /// <summary>
    /// Punkt verschieben um Vektor.
    /// </summary>
    public TrackPoint Add(TrackVector vector)
        => new(X + vector.X, Y + vector.Y);
    
    public override string ToString()
        => $"({X:F2}, {Y:F2})";
}
```

---

### **2. TrackVector (2D Direction)**
```csharp
namespace Moba.Domain.TrackPlan.Geometry;

/// <summary>
/// 2D Richtungsvektor (Tangente, Offset).
/// Immutable Value Object.
/// </summary>
public readonly struct TrackVector
{
    public readonly double X, Y;
    
    public TrackVector(double x, double y)
    {
        X = x;
        Y = y;
    }
    
    public static TrackVector UnitX => new(1, 0);
    public static TrackVector UnitY => new(0, 1);
    
    public double Length => Math.Sqrt(X * X + Y * Y);
    
    public TrackVector Normalize()
    {
        var len = Length;
        return len > 1e-10 ? new TrackVector(X / len, Y / len) : UnitX;
    }
    
    public double AngleRadians => Math.Atan2(Y, X);
    public double AngleDegrees => AngleRadians * 180.0 / Math.PI;
    
    /// <summary>
    /// Rotiere Vektor um Winkel (in Radians).
    /// </summary>
    public TrackVector Rotate(double angleRadians)
    {
        var cos = Math.Cos(angleRadians);
        var sin = Math.Sin(angleRadians);
        return new TrackVector(
            X * cos - Y * sin,
            X * sin + Y * cos
        );
    }
    
    public override string ToString()
        => $"Vec({X:F2}, {Y:F2}) @ {AngleDegrees:F1}°";
}
```

---

### **3. Transform2D (2D Affine Transformation)**
```csharp
namespace Moba.Domain.TrackPlan.Geometry;

/// <summary>
/// 2D Affine Transformation (Translation + Rotation).
/// Immutable Value Object.
/// 3x3 Homogene Matrix (aber nur 2D-Operationen).
/// </summary>
public readonly struct Transform2D
{
    // Matrix:
    // | M11  M12  Tx |
    // | M21  M22  Ty |
    // |  0    0    1 |
    
    public readonly double M11, M12, Tx;
    public readonly double M21, M22, Ty;
    
    private Transform2D(double m11, double m12, double tx, double m21, double m22, double ty)
    {
        M11 = m11; M12 = m12; Tx = tx;
        M21 = m21; M22 = m22; Ty = ty;
    }
    
    public static Transform2D Identity => new(1, 0, 0, 0, 1, 0);
    
    /// <summary>
    /// Translation (Verschiebung).
    /// </summary>
    public static Transform2D Translation(double dx, double dy)
        => new(1, 0, dx, 0, 1, dy);
    
    /// <summary>
    /// Rotation um Ursprung (in Radians).
    /// </summary>
    public static Transform2D Rotation(double angleRadians)
    {
        var cos = Math.Cos(angleRadians);
        var sin = Math.Sin(angleRadians);
        return new(cos, -sin, 0, sin, cos, 0);
    }
    
    /// <summary>
    /// Rotation um Punkt (in Radians).
    /// </summary>
    public static Transform2D RotationAround(double angleRadians, TrackPoint center)
    {
        // 1. Translate to origin
        // 2. Rotate
        // 3. Translate back
        var t1 = Translation(-center.X, -center.Y);
        var r = Rotation(angleRadians);
        var t2 = Translation(center.X, center.Y);
        return t2 * r * t1;
    }
    
    /// <summary>
    /// Matrix-Multiplikation (Verkettung).
    /// </summary>
    public static Transform2D operator *(Transform2D a, Transform2D b)
    {
        return new Transform2D(
            a.M11 * b.M11 + a.M12 * b.M21,
            a.M11 * b.M12 + a.M12 * b.M22,
            a.M11 * b.Tx  + a.M12 * b.Ty + a.Tx,
            a.M21 * b.M11 + a.M22 * b.M21,
            a.M21 * b.M12 + a.M22 * b.M22,
            a.M21 * b.Tx  + a.M22 * b.Ty + a.Ty
        );
    }
    
    /// <summary>
    /// Punkt transformieren.
    /// </summary>
    public TrackPoint Apply(TrackPoint point)
    {
        return new TrackPoint(
            M11 * point.X + M12 * point.Y + Tx,
            M21 * point.X + M22 * point.Y + Ty
        );
    }
    
    /// <summary>
    /// Vektor transformieren (keine Translation).
    /// </summary>
    public TrackVector ApplyVector(TrackVector vector)
    {
        return new TrackVector(
            M11 * vector.X + M12 * vector.Y,
            M21 * vector.X + M22 * vector.Y
        );
    }
    
    /// <summary>
    /// Extrahiere Rotation (in Radians).
    /// </summary>
    public double ExtractRotation()
        => Math.Atan2(M21, M11);
    
    /// <summary>
    /// Extrahiere Translation.
    /// </summary>
    public TrackPoint ExtractTranslation()
        => new(Tx, Ty);
}
```

---

### **4. TrackGeometry (Track Template)**
```csharp
namespace Moba.Domain.TrackPlan.Geometry;

/// <summary>
/// Geometrie-Template für einen Gleis-Typ (z.B. Piko R2).
/// Definiert lokale Geometrie (Endpunkte, Tangenten, SVG).
/// </summary>
public class TrackGeometry
{
    public required string ArticleCode { get; init; }
    public required TrackGeometryType Type { get; init; }
    
    /// <summary>
    /// Lokale Endpunkte (Ursprung bei (0,0)).
    /// </summary>
    public required List<TrackPoint> Endpoints { get; init; }
    
    /// <summary>
    /// Tangenten an Endpunkten (normalisiert).
    /// Länge = Endpoints.Count.
    /// </summary>
    public required List<TrackVector> EndpointTangents { get; init; }
    
    /// <summary>
    /// SVG PathData (lokale Koordinaten).
    /// </summary>
    public required string PathData { get; init; }
    
    // Geometry-spezifische Properties
    public double? Length { get; init; }         // Für Straight
    public double? Radius { get; init; }         // Für Arc
    public double? AngleDegrees { get; init; }   // Für Arc
    
    /// <summary>
    /// Hole Tangente an Endpoint-Index.
    /// </summary>
    public TrackVector GetTangentAt(int endpointIndex)
        => EndpointTangents[endpointIndex];
    
    /// <summary>
    /// Hole Rotation an Endpoint-Index (in Degrees).
    /// </summary>
    public double GetRotationDegreesAt(int endpointIndex)
        => EndpointTangents[endpointIndex].AngleDegrees;
}

public enum TrackGeometryType
{
    Straight,
    Arc,
    Switch,      // Weiche (mehrere Ausgänge)
    Crossing     // Kreuzung
}
```

---

## 🔧 TRACK CALCULATOR SERVICE

```csharp
namespace Moba.SharedUI.Service;

using Domain.TrackPlan.Geometry;

/// <summary>
/// Berechnet Gleis-Geometrie (Arc-Endpunkte, Tangenten, Transformationen).
/// Spezialisiert für Modellbahn (Gerade + Kreisbogen).
/// Keine Abhängigkeit von G-Shark - eigene Implementierung.
/// </summary>
public class TrackCalculator
{
    /// <summary>
    /// Berechne Endpunkt eines Kreisbogens.
    /// </summary>
    public (TrackPoint Endpoint, TrackVector Tangent) CalculateArcEndpoint(
        TrackPoint startPoint,
        double radius,
        double angleDegrees,
        bool clockwise = false)
    {
        var angleRad = angleDegrees * Math.PI / 180.0;
        var sign = clockwise ? -1.0 : 1.0;
        
        // Kreisbogen-Formel (Polar → Kartesisch)
        var deltaX = radius * Math.Sin(angleRad);
        var deltaY = radius * (1 - Math.Cos(angleRad)) * sign;
        
        var endpoint = new TrackPoint(
            startPoint.X + deltaX,
            startPoint.Y + deltaY
        );
        
        // Tangente am Endpunkt (Ableitung der Kreisformel)
        var tangentAngle = sign * angleRad;
        var tangent = new TrackVector(
            Math.Cos(tangentAngle),
            Math.Sin(tangentAngle)
        );
        
        return (endpoint, tangent.Normalize());
    }
    
    /// <summary>
    /// Berechne Transformation von Parent-Segment zu Child-Segment.
    /// </summary>
    public Transform2D CalculateConnectionTransform(
        Transform2D parentWorldTransform,
        TrackGeometry parentGeometry,
        int parentEndpointIndex,
        TrackGeometry childGeometry,
        int childEndpointIndex)
    {
        // 1. Parent-Endpunkt in Welt-Koordinaten
        var parentEndpointLocal = parentGeometry.Endpoints[parentEndpointIndex];
        var parentEndpointWorld = parentWorldTransform.Apply(parentEndpointLocal);
        
        // 2. Parent-Tangente in Welt-Koordinaten
        var parentTangentLocal = parentGeometry.GetTangentAt(parentEndpointIndex);
        var parentTangentWorld = parentWorldTransform.ApplyVector(parentTangentLocal);
        
        // 3. Child-Endpunkt (muss an Parent-Endpunkt andocken)
        var childEndpointLocal = childGeometry.Endpoints[childEndpointIndex];
        var childTangentLocal = childGeometry.GetTangentAt(childEndpointIndex);
        
        // 4. Rotation: Child-Tangente muss entgegengesetzt zu Parent-Tangente sein
        var requiredRotation = parentTangentWorld.AngleRadians - childTangentLocal.AngleRadians + Math.PI;
        
        // 5. Translation: Child-Ursprung muss so verschoben werden, dass Child-Endpunkt bei Parent-Endpunkt liegt
        var rotatedChildEndpoint = Transform2D.Rotation(requiredRotation).Apply(childEndpointLocal);
        var translation = new TrackVector(
            parentEndpointWorld.X - rotatedChildEndpoint.X,
            parentEndpointWorld.Y - rotatedChildEndpoint.Y
        );
        
        // 6. Kombiniere Translation + Rotation
        var childWorldTransform = Transform2D.Translation(translation.X, translation.Y)
                                * Transform2D.Rotation(requiredRotation);
        
        return childWorldTransform;
    }
    
    /// <summary>
    /// Traversiere Topologie-Graph und berechne World-Transforms für alle Segmente.
    /// </summary>
    public Dictionary<string, Transform2D> CalculateWorldTransforms(
        TrackLayout layout,
        TrackGeometryLibrary library)
    {
        var result = new Dictionary<string, Transform2D>();
        var visited = new HashSet<string>();
        
        // Finde Root-Segmente (keine eingehenden Connections)
        var incomingConnections = new HashSet<string>(
            layout.Connections.Select(c => c.Segment2Id)
        );
        var roots = layout.Segments
            .Where(s => !incomingConnections.Contains(s.Id))
            .ToList();
        
        // Wenn keine Roots: Nimm erstes Segment
        if (roots.Count == 0 && layout.Segments.Count > 0)
            roots.Add(layout.Segments[0]);
        
        // Traversiere ab jedem Root
        foreach (var root in roots)
        {
            TraverseSegment(root, Transform2D.Identity, layout, library, result, visited);
        }
        
        return result;
    }
    
    private void TraverseSegment(
        TrackSegment segment,
        Transform2D worldTransform,
        TrackLayout layout,
        TrackGeometryLibrary library,
        Dictionary<string, Transform2D> result,
        HashSet<string> visited)
    {
        if (visited.Contains(segment.Id))
            return; // Zyklen vermeiden
        
        visited.Add(segment.Id);
        result[segment.Id] = worldTransform;
        
        // Finde ausgehende Connections
        var outgoing = layout.Connections
            .Where(c => c.Segment1Id == segment.Id)
            .ToList();
        
        foreach (var connection in outgoing)
        {
            var nextSegment = layout.Segments.FirstOrDefault(s => s.Id == connection.Segment2Id);
            if (nextSegment == null) continue;
            
            var segmentGeometry = library.GetGeometry(segment.ArticleCode);
            var nextGeometry = library.GetGeometry(nextSegment.ArticleCode);
            if (segmentGeometry == null || nextGeometry == null) continue;
            
            // Berechne Transform für nächstes Segment
            var nextTransform = CalculateConnectionTransform(
                worldTransform,
                segmentGeometry,
                connection.Segment1EndpointIndex,
                nextGeometry,
                connection.Segment2EndpointIndex
            );
            
            TraverseSegment(nextSegment, nextTransform, layout, library, result, visited);
        }
    }
}
```

---

## 🎯 USAGE EXAMPLE (Renderer Integration)

```csharp
public class TrackLayoutRenderer
{
    private readonly TrackGeometryLibrary _library;
    private readonly TrackCalculator _calculator;
    
    public List<RenderedSegment> Render(TrackLayout layout)
    {
        // 1. Berechne World-Transforms für alle Segmente
        var transforms = _calculator.CalculateWorldTransforms(layout, _library);
        
        var result = new List<RenderedSegment>();
        
        foreach (var segment in layout.Segments)
        {
            var geometry = _library.GetGeometry(segment.ArticleCode);
            if (geometry == null) continue;
            
            var worldTransform = transforms.GetValueOrDefault(segment.Id, Transform2D.Identity);
            
            // 2. Transformiere PathData zu Welt-Koordinaten
            var transformedPath = TransformPathData(geometry.PathData, worldTransform);
            
            // 3. Extrahiere Position + Rotation
            var worldPos = worldTransform.ExtractTranslation();
            var worldRot = worldTransform.ExtractRotation() * 180.0 / Math.PI;
            
            result.Add(new RenderedSegment(
                segment.Id,
                segment.ArticleCode,
                worldPos.X,
                worldPos.Y,
                worldRot,
                transformedPath,
                segment.AssignedInPort
            ));
        }
        
        return result;
    }
}
```

---

## 🎉 BENEFITS OF OUR APPROACH

### **vs. G-Shark (General-Purpose Library)**
✅ **Einfacher:** Nur Gerade + Kreisbogen (kein NURBS-Overkill)  
✅ **Explizit:** Wir verstehen jede Zeile Code  
✅ **2D-optimiert:** Keine unnötige 3D-Mathematik  
✅ **Wartbar:** Keine Black-Box-Dependency  
✅ **Ausreichend:** Numerische Stabilität durch Re-Orthogonalisierung

### **vs. Current Renderer (Placeholder)**
✅ **Topologie-First:** Graph-Traversal statt harter X/Y  
✅ **Transformationsketten:** Korrekte Platzierung verbundener Segmente  
✅ **Tangentenvektoren:** Korrekte Rotation an Verbindungspunkten  
✅ **Numerisch stabil:** Re-Orthogonalisierung alle 10 Schritte

---

## 📚 NEXT STEPS

1. ✅ **Implementiere Core Types:** `TrackPoint`, `TrackVector`, `Transform2D`
2. ✅ **Erweitere TrackGeometry:** Add `EndpointTangents`
3. ✅ **Implementiere TrackCalculator:** Arc-Berechnung + Transformation
4. ✅ **Update TrackLayoutRenderer:** Graph-Traversal mit Transforms
5. ✅ **Unit Tests:** Numerische Stabilität testen (100+ Segmente)

---

**Philosophie:** Wir bauen **unser** Gleis-CAD mit **eigenem** mathematischen Fundament - gelernt von G-Shark, aber **nicht abhängig** davon! 🚂
