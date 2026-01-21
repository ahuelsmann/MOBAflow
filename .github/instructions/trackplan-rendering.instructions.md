---
description: 'TrackPlan Rendering Pipeline'
applyTo: '**/TrackPlan*/**/*.cs,**/WinUI/Rendering/**/*.cs'
---

# TrackPlan Rendering

## CONVENTION: Rails on Top, Sleepers on Bottom

```
    ════════════════════    ← Rails (oben)
    ┃  ┃  ┃  ┃  ┃  ┃  ┃    ← Sleepers/Schwellen (unten)
```

Diese Konvention definiert die **Orientierung** jedes Gleisstücks eindeutig.
Im SVG-Export werden Schwellen gerendert, um die "Unterseite" zu markieren.

## Rendering Pipeline

```
TrackEdge (Domain)
    ↓
TrackTemplate (Catalog)
    ↓
TrackGeometrySpec (Length, Radius, Angle)
    ↓
TrackGeometryRenderer.Render()
    ↓
IGeometryPrimitive[] (LinePrimitive, ArcPrimitive)
    ↓
PrimitiveShapeFactory.CreateShape()
    ↓
WinUI Shape (Line, Path)
    ↓
Canvas.Children.Add()
```

## Primitive Types

### LinePrimitive
```csharp
record LinePrimitive(Point2D From, Point2D To);
```

### ArcPrimitive
```csharp
record ArcPrimitive(
    Point2D Center,      // Bogenzentrum
    double Radius,       // Bogenradius (mm)
    double StartAngleRad, // Startwinkel (Radiant, von Center aus)
    double SweepAngleRad  // Bogenwinkel (Radiant, positiv=CCW)
);
```

## WinUI Arc Rendering

**ArcSegment erfordert:**
- `StartPoint`: Berechnet aus Center + Radius * cos/sin(StartAngle)
- `Point` (EndPoint): Berechnet aus Center + Radius * cos/sin(StartAngle + SweepAngle)
- `Size`: (Radius, Radius) für kreisförmigen Bogen
- `SweepDirection`: Clockwise wenn SweepAngle >= 0, sonst Counterclockwise
- `IsLargeArc`: true wenn |SweepAngle| > π (180°)

```csharp
var startPoint = new Point(
    cx + r * Math.Cos(arc.StartAngleRad),
    cy + r * Math.Sin(arc.StartAngleRad));

var endPoint = new Point(
    cx + r * Math.Cos(arc.StartAngleRad + arc.SweepAngleRad),
    cy + r * Math.Sin(arc.StartAngleRad + arc.SweepAngleRad));

var segment = new ArcSegment
{
    Point = endPoint,
    Size = new Size(r, r),
    SweepDirection = arc.SweepAngleRad >= 0
        ? SweepDirection.Clockwise      // ⚠️ WinUI: CW bei positivem Sweep
        : SweepDirection.Counterclockwise,
    IsLargeArc = Math.Abs(arc.SweepAngleRad) > Math.PI
};
```

## Display Scale

```
World (mm) ──[× displayScale]──→ Screen (px)

displayScale = 0.5  →  1mm = 0.5px (Standard)
displayScale = 1.0  →  1mm = 1px
displayScale = 2.0  →  1mm = 2px (Zoom in)
```

## Farben

| State | Dark Theme | Light Theme |
|-------|------------|-------------|
| Normal Track | Silver | DimGray |
| Selected | DeepSkyBlue | Blue |
| Hovered | LightSkyBlue | CornflowerBlue |
| Port (Open) | Orange | DarkOrange |
| Port (Connected) | LimeGreen | Green |
| Feedback Point | Red | DarkRed |
| Snap Preview | Yellow | Gold |

## Performance

- Canvas.Children.Clear() vor jedem Render
- Keine Shape-Wiederverwendung (einfach aber nicht optimal)
- Bei >1000 Gleisen: Virtualisierung erwägen (nur sichtbare rendern)
