---
description: 'TrackPlan Geometrie-Berechnungen'
applyTo: '**/TrackPlan*/**/*.cs'
---

# TrackPlan Geometrie

## Koordinatensystem

```
+Y ↑
   |
   |    → +X
   +--------
   
Winkel: 0° = rechts (→), 90° = oben (↑), gegen Uhrzeigersinn positiv
```

**WICHTIG:**
- WinUI Canvas hat Y-Achse nach UNTEN (+Y = down)
- TrackPlan Welt-Koordinaten haben Y-Achse nach OBEN (+Y = up)
- `displayScale` konvertiert mm → Pixel (z.B. 0.5 = 1mm wird 0.5px)

## Einheiten

| Kontext | Einheit |
|---------|---------|
| TrackGeometrySpec | mm (Millimeter) |
| Point2D (World) | mm |
| Canvas (Screen) | Pixel |
| Winkel | Grad (außer intern: Radiant) |

## Gerade (StraightGeometry)

```csharp
// Endpunkt berechnen
endX = startX + length * cos(angle)
endY = startY + length * sin(angle)
```

## Kurve (CurveGeometry)

```
          ↑ Normal (zum Zentrum)
          |
  Start ──┼── Tangente (startAngle)
          |
        Center
```

**Formel:**
```csharp
// Normal = 90° links von der Tangente (zeigt zum Kurvenzentrum)
normalX = -sin(startAngleRad)
normalY = +cos(startAngleRad)

// Zentrum = Start + Normal * Radius
centerX = startX + normalX * radius
centerY = startY + normalY * radius

// Arc-Startwinkel = Tangente - 90° (zeigt von Center zu Start)
arcStartRad = tangentRad - π/2
```

**Sweep-Richtung:**
- Positiver Sweep = gegen Uhrzeigersinn (CCW)
- Negativer Sweep = im Uhrzeigersinn (CW)

## Weiche (SwitchGeometry)

```
        C (branch)
       /
  A───┼───B (straight through)
      │
   Junction
```

**Komponenten:**
1. **Gerade** von A nach B (volle Länge)
2. **Bogen** von Junction nach C (abzweigender Ast)

**Formel für Bogen:**
```csharp
// Junction-Position (auf der Geraden)
junctionX = startX + junctionOffset * cos(startAngle)
junctionY = startY + junctionOffset * sin(startAngle)

// Normal für Bogen (links oder rechts je nach Weichentyp)
side = isLeftSwitch ? +1 : -1
normalX = -sin(startAngleRad) * side
normalY = +cos(startAngleRad) * side

// Bogenzentrum
centerX = junctionX + normalX * radius
centerY = junctionY + normalY * radius
```

## Testen von Geometrie

**Golden Rule:** Teste mit bekannten Werten!

```csharp
// Beispiel: Gerade 100mm bei 0°
// Start: (0, 0), Länge: 100, Winkel: 0°
// Erwartetes Ende: (100, 0)

// Beispiel: Kurve R=500mm, 30° bei Startwinkel 0°
// Erwartetes Zentrum: (0, 500) - direkt oberhalb
// Erwarteter Endpunkt: (500*sin(30°), 500*(1-cos(30°))) = (250, ~67)
```

## Häufige Fehler

1. **Vorzeichen-Fehler bei Normal**: `sin` und `cos` vertauscht
2. **Arc-Sweep-Richtung**: CW/CCW verwechselt
3. **Winkel-Einheit**: Grad vs. Radiant gemischt
4. **Y-Achsen-Inversion**: Screen vs. World vergessen
