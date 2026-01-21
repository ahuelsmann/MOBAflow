---
description: 'TrackPlan Snap-to-Connect System'
applyTo: '**/TrackPlan*/**/*.cs'
---

# TrackPlan Snapping

## Snap-to-Connect Konzept

Beim Ziehen eines Gleisstücks wird geprüft, ob ein Port nahe genug an einem anderen offenen Port ist, um eine Verbindung herzustellen.

```
     [Dragged Track]
          │
    Port A┼───────
          │
          ↓ snap
          
    ───────┼Port B
           │
   [Stationary Track]
```

## Snap-Bedingungen

1. **Distanz**: Port-Abstand < `SnapDistance` (Standard: 30mm)
2. **Winkel**: Ports müssen ~180° zueinander zeigen (±`SnapAngleTolerance`, Standard: 5°)
3. **Offen**: Beide Ports müssen unverbunden sein

## Port-Winkel Berechnung

Der "Ausgangswinkel" eines Ports zeigt nach AUSSEN vom Gleis weg:

```csharp
// Port-Position in Weltkoordinaten
Point2D GetPortWorldPosition(Guid edgeId, string portId)
{
    var pos = Positions[edgeId];
    var rot = Rotations[edgeId];
    var template = catalog.GetById(edge.TemplateId);
    var end = template.Ends.First(e => e.Id == portId);
    
    // Lokale Position rotieren und verschieben
    var localPos = end.Position;  // relativ zum Gleis-Origin
    var rotRad = rot * Math.PI / 180.0;
    
    return new Point2D(
        pos.X + localPos.X * Math.Cos(rotRad) - localPos.Y * Math.Sin(rotRad),
        pos.Y + localPos.X * Math.Sin(rotRad) + localPos.Y * Math.Cos(rotRad)
    );
}

// Port-Winkel (zeigt nach außen)
double GetPortWorldAngle(Guid edgeId, string portId)
{
    var rot = Rotations[edgeId];
    var template = catalog.GetById(edge.TemplateId);
    var end = template.Ends.First(e => e.Id == portId);
    
    return NormalizeDeg(rot + end.AngleDeg);
}
```

## Snap-Matching

```csharp
bool ArePortsCompatible(
    Point2D pos1, double angle1,
    Point2D pos2, double angle2,
    double snapDistance, double angleTolerance)
{
    // 1. Distanz-Check
    var dist = Distance(pos1, pos2);
    if (dist > snapDistance)
        return false;
    
    // 2. Winkel-Check: Ports müssen gegenüber zeigen (180° Differenz)
    var angleDiff = Math.Abs(NormalizeDeg(angle1 - angle2 - 180));
    if (angleDiff > angleTolerance && angleDiff < 360 - angleTolerance)
        return false;
    
    return true;
}

double NormalizeDeg(double deg)
{
    while (deg < 0) deg += 360;
    while (deg >= 360) deg -= 360;
    return deg;
}
```

## Snap-Position Berechnung

Wenn ein Snap gefunden wird, muss das bewegte Gleis so positioniert werden, dass die Ports exakt übereinander liegen:

```csharp
(Point2D newPos, double newRot) CalculateSnapPosition(
    Guid movingEdgeId, string movingPortId,
    Guid targetEdgeId, string targetPortId)
{
    // 1. Ziel-Port Position und Winkel holen
    var targetPos = GetPortWorldPosition(targetEdgeId, targetPortId);
    var targetAngle = GetPortWorldAngle(targetEdgeId, targetPortId);
    
    // 2. Neue Rotation: Moving-Port muss in Gegenrichtung zeigen
    var template = catalog.GetById(movingEdge.TemplateId);
    var localPortAngle = template.Ends.First(e => e.Id == movingPortId).AngleDeg;
    var newRotation = NormalizeDeg(targetAngle + 180 - localPortAngle);
    
    // 3. Neue Position: Port muss auf Ziel-Position landen
    var localPortPos = template.Ends.First(e => e.Id == movingPortId).Position;
    var rotRad = newRotation * Math.PI / 180.0;
    
    var newPos = new Point2D(
        targetPos.X - (localPortPos.X * Math.Cos(rotRad) - localPortPos.Y * Math.Sin(rotRad)),
        targetPos.Y - (localPortPos.X * Math.Sin(rotRad) + localPortPos.Y * Math.Cos(rotRad))
    );
    
    return (newPos, newRotation);
}
```

## SnapPreview

Während des Ziehens wird ein `SnapPreview` angezeigt:

```csharp
record SnapPreview(
    Guid MovingEdgeId,
    string MovingPortId,
    Guid TargetEdgeId,
    string TargetPortId,
    Point2D MovingPortPosition,
    Point2D TargetPortPosition,
    Point2D PreviewPosition,
    double PreviewRotation);
```

## Grid Snap

Unabhängig vom Port-Snap kann auch Grid-Snap aktiviert sein:

```csharp
Point2D SnapToGrid(Point2D pos, double gridSize)
{
    return new Point2D(
        Math.Round(pos.X / gridSize) * gridSize,
        Math.Round(pos.Y / gridSize) * gridSize
    );
}
```

## Priorisierung

1. **Port-Snap** hat Vorrang vor Grid-Snap
2. Bei mehreren möglichen Port-Snaps: Nächster Port gewinnt
3. Grid-Snap nur wenn kein Port-Snap möglich
