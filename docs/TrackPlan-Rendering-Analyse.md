# Analyse: SVG-Renderer vs. TrackPlanPage (WinUI) – Verbundene Geometrie

## Kurzfassung

**Kernproblem:** Der WinUI-Renderer verwendet einen Offset `(-minX, -minY)`, der den **Pfad-Anfang (Entry-Port)** nicht zuverlässig auf den Rotationsursprung legt. Bei Entry Port B (z.B. S-Kurve aus zwei R9) liegt der Bounding-Box-Minimum am **Pfad-Ende**, wodurch um den falschen Punkt rotiert wird.

---

## 1. SVG-Renderer (funktioniert korrekt)

### Ablauf
1. **Logische Verkettung:** Der Renderer traversiert den Graphen (Tiefensuche) und berechnet Position/Winkel aus der vorherigen Verbindung.
2. **Pro Segment:** `(x, y, angle)` = Position und Richtung des **Entry-Ports**.
3. **PathToSvgConverter.ToSvgPath(path, x, y, angle):**
   - Jeder Punkt wird transformiert: `P_world = R(angle) * P_local + (x, y)`
   - Der Pfad wird **vollständig in Weltkoordinaten** gezeichnet.
   - Der Ursprung (0,0) des Pfads landet exakt bei `(x, y)`.

### Zwei R9 verbunden (S-Kurve)
- R9₁: Entry A, gezeichnet bei (0,0), Winkel 0°.
- R9₂: Entry A, bekommt `(x, y, angle)` = Position/Winkel von R9₁ Port B.
- R9₂ wird mit `DrawSegmentPath(r9, 'A', branchX, branchY, branchAngle)` gezeichnet.
- `PathToSvgConverter` transformiert den Pfad so, dass der lokale Ursprung (Port A) bei `(branchX, branchY)` liegt und mit `branchAngle` rotiert ist.
- Die Kurven treffen sich exakt am Verbindungspunkt.

---

## 2. TrackPlanPage / WinUI (Fehler)

### Ablauf
1. Jedes Segment hat `PlacedSegment` mit `X`, `Y`, `RotationDegrees`.
2. `SegmentPlanPathBuilder.CreatePath()`:
   - `pathCommands = SegmentLocalPathBuilder.GetPath(segment, entryPort)` (gleiche Geometrie wie SVG)
   - `offset = (-minX, -minY)` aus `GetBounds(pathCommands)` – **Problemquelle**
   - `BuildPathGeometry(..., -minX, -minY)` – Verschiebung, sodass Bounding-Box-Minimum bei (0,0) liegt
   - `Canvas.SetLeft(shape, placed.X * scale)`, `SetTop(shape, placed.Y * scale)`
   - `RenderTransform = RotateTransform(Angle = placed.RotationDegrees)` um `RenderTransformOrigin (0,0)` = **top-left des Path**

### Das Problem: Offset vs. Pfad-Anfang

**Entry A (curveDirection 1):**
- Pfad: (0,0) → (endX, endY), typischerweise minX=0, minY=0.
- Offset (0,0): Pfad-Anfang bleibt bei (0,0).
- Layout-Top-Left = Minimum = (0,0) = Pfad-Anfang.
- Rotation um (0,0) = Rotation um Entry-Port. Korrekt.

**Entry B (curveDirection -1, z.B. zweites R9 in S-Kurve):**
- Pfad: (0,0) → (endX, endY) mit endY < 0.
- minX=0, minY=endY.
- Offset (0, -minY) = (0, -endY): Verschiebung nach unten.
- Nach Offset: Pfad-Anfang (0,0) → (0, -endY), Pfad-Ende → (endX, 0).
- Das **Minimum** der neuen Geometrie ist (0, 0) – das liegt am **Pfad-Ende**.
- Layout-Top-Left = (0,0) = **Pfad-Ende**, nicht Pfad-Anfang.
- `placed.X, placed.Y` wird als Position des **Entry-Ports** interpretiert.
- Die Rotation erfolgt aber um (0,0) = Pfad-Ende.
- **Ergebnis:** Segment wird um den falschen Punkt gedreht und platziert, die Geometrie passt nicht mehr zusammen.

### Koordinaten-Kompensation fehlt

Selbst wenn der Ursprung stimmt: `Canvas.SetLeft` und `SetTop` positionieren die **Top-Left** des Path. Wenn der Pfad-Anfang nicht an der Top-Left liegt, müsste:

- `SetLeft = (placed.X + pathStartOffsetInLayout.X) * scale`
- `SetTop = (placed.Y + pathStartOffsetInLayout.Y) * scale`

gelten, damit der Entry-Port exakt bei `(placed.X, placed.Y)` liegt. Das passiert derzeit nicht.

---

## 3. Unterschiedliche Semantik

| Aspekt | SVG | WinUI (aktuell) |
|--------|-----|------------------|
| Position (x,y) | Entry-Port | Soll Entry-Port sein |
| Rotation | Um Entry-Port | Um Top-Left des Path |
| Geometrie-Offset | Kein Offset, Punkt-für-Punkt-Transform | Bounding-Box-Minimum auf (0,0) |
| Pfad-Anfang = Rotationszentrum? | Ja | Nur bei Entry A |

---

## 4. Lösung

**Option A – Korrekter Offset:**
- Offset so wählen, dass der **Pfad-Anfang (0,0)** immer bei (0,0) in der finalen Geometrie bleibt.
- Offset = (0, 0), keine Verschiebung anhand der Bounds.
- Canvas-Position anpassen: `SetLeft = (placed.X + geomMinX) * scale`, `SetTop = (placed.Y + geomMinY) * scale`, sodass der Pfad-Anfang bei `(placed.X, placed.Y)` liegt.
- `RenderTransformOrigin` auf den relativen Punkt setzen, der dem Pfad-Anfang entspricht (z.B. wenn der Pfad-Anfang nicht bei (0,0) liegt).

**Option B – Geometrie wie beim SVG transformieren (empfohlen):**
- Geometrie direkt wie im SVG in Weltkoordinaten erzeugen (Translation + Rotation pro Punkt).
- Kein separates `RenderTransform` am Path.
- Position über `Canvas.SetLeft/Top` setzen, Geometrie enthält bereits die transformierten Punkte.

**Option C – Pfad-Anfang als Rotationszentrum:**
- Offset so wählen, dass der Pfad-Anfang immer an der Top-Left des Path liegt (Minimum der Geometrie).
- Für Entry B: Pfad vor dem Offset spiegeln oder umsortieren, sodass der Startpunkt zum Minimum wird.
- Alternative: `RenderTransformOrigin` dynamisch berechnen, sodass sie auf den Pfad-Anfang zeigt.

---

## 5. AnyRail-Referenz (zweiter Screenshot)

AnyRail zeigt zwei mögliche R9-Verbindungen mit:
- präzisen tangentialen Übergängen an den Anschlusspunkten,
- klar definierten Anschlusspunkten (kleine Dreiecke/Paddles),
- durchgehend glatten Kurven.

Unsere SVG-Darstellung erreicht das, weil Position und Rotation pro Segment aus der Verkettung berechnet werden und der Pfad immer um den Entry-Port transformiert wird. Die WinUI-Darstellung weicht davon ab, solange der Rotationsursprung und die Position des Pfad-Anfangs nicht konsistent sind.
