# TrackPlan: SVG vs. Win2D Rendering – Vergleich

## Übersicht

| Aspekt | PathToSvgConverter (SVG) | SegmentPlanPathBuilder (XAML Path) | PathToCanvasGeometryConverter (Win2D) |
|--------|--------------------------|-----------------------------------|--------------------------------------|
| **Ausgabe** | SVG path-d-String | PathGeometry | CanvasGeometry |
| **Koordinaten** | Welt mm (ToSvgPath) | Welt DIPs (Tx/Ty) | Lokal mm → Transform |
| **Transformation** | Punktweise: `Tx = originX + lx*cos - ly*sin` | Punktweise: `Tx = (originX + lx*cos - ly*sin) * scale` | Matrix: `T * R * S` |
| **MoveTo** | Nur x,y aktualisieren | x,y aktualisieren | `EndFigure` + `BeginFigure(move.X, move.Y)` |
| **LineTo** | `M x1,y1 L x2,y2` | PathFigure + LineSegment | `BeginFigure` wenn nötig, `AddLine` |
| **ArcTo** | `M x1,y1 A rx,ry 0 0,sweep x2,y2` | PathFigure + ArcSegment | `AddArc(endPoint, r, r, 0, sweep, Small)` |
| **Stroke** | `stroke-width="4"` | StrokeThickness=4, Round Join/Caps | DrawGeometry, CanvasStrokeStyle Round |
| **largeArc** | 0 (klein) | IsLargeArc=false | CanvasArcSize.Small |
| **sweep** | Clockwise=1 | SweepDirection.Clockwise | CanvasSweepDirection.Clockwise |

## Transformation (mathematisch äquivalent)

**SegmentPlanPathBuilder:**
```
world = scale * (origin + rotate(local))
Tx(lx,ly) = (originX + lx*cos - ly*sin) * scale
Ty(lx,ly) = (originY + lx*sin + ly*cos) * scale
```

**Win2D:**
```
M = T * R * S  (Translation * Rotation * Scale)
world = origin + R * (scale * local)
     = scale * ((origin/scale) + R * local)
     = scale * (placed + R * local)   mit origin = placed * scale
```
→ Identisch.

## Abweichungen (behoben)

1. **Stroke Join/Caps:** Win2D standard war Miter/Flat. Jetzt `CanvasStrokeStyle` mit `LineJoin=Round`, `StartCap=Round`, `EndCap=Round` wie SegmentPlanPathBuilder.

2. **Arc:** SVG und Win2D AddArc(endPoint, rx, ry, sweep, arcSize) sind kompatibel. Beide verwenden Endpunkt + Radius + sweep-flag + small arc.

## Konsistenz-Checkliste

- [x] Geometrie in lokalen mm (Port A = 0,0)
- [x] Transform: scale * (origin + rotate(local))
- [x] Arc: Small, Clockwise/CounterClockwise wie SegmentLocalPathBuilder
- [x] Stroke: 4 DIP (10 bei Auswahl)
- [x] Stroke-Style: Round Join & Caps

## Dateien

| Datei | Zweck |
|-------|-------|
| `TrackPlan.Renderer/PathToSvgConverter.cs` | SVG-Export (TrackPlanSvgRenderer) |
| `WinUI/View/SegmentPlanPathBuilder.cs` | XAML Path (Ghost, früher Segmente) |
| `WinUI/View/PathToCanvasGeometryConverter.cs` | Win2D CanvasGeometry |
| `WinUI/View/TrackPlanPage.xaml.cs` | Draw-Handler mit Transform + StrokeStyle |
