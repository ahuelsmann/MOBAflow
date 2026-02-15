# TrackPlan Win2D: Drag & Drop und Selektion

## DPI, Skalierung und Auflösung (Win2D + WinUI)

### Grundlagen

- **DIPs (Device Independent Pixels):** Win2D und WinUI arbeiten standardmäßig in DIPs – nicht in physischen Pixeln.
- **Formel:** `pixel = dips × dpi / 96` (96 DPI = neutral, DIPs = Pixel)
- **Win2D-APIs:** Koordinaten und Größen (float, Vector2) sind in DIPs
- **WinUI Pointer-Positionen:** `e.GetCurrentPoint(...).Position` liefert DIPs (logical pixels)
- **CanvasControl:** nutzt automatisch die DPI des Displays, `CreateResources` wird bei DPI-Änderung mit `DpiChanged` erneut aufgerufen

### Koordinatensystem TrackPlan

- **Weltkoordinaten:** mm (z.B. 239 mm für G239)
- **Skalierung:** `ScaleMmToPx` = mm → Anzeige-Einheiten (aktuell 1:1, 1 mm = 1 DIP)
- **Zeichnen:** Welt-mm × `ScaleMmToPx` → DIPs für Win2D
- **Pointer → mm:** `pointerDips / ScaleMmToPx` → Welt-mm
- **Einheitliche Basis:** Wenn Zeichnen und Hit-Test beide mm nutzen, entfallen DPI-Umrechnungen in der Logik

### Wichtige Punkte

1. **CanvasControl.Dpi / DpiScale:** Control passt sich der Display-DPI an. Optional `DpiScale` für reduzierte Auflösung (Performance).
2. **StrokeContainsPoint:** Punkt und StrokeWidth in derselben Einheit wie die Geometry – hier mm. Transform-Matrix ebenfalls in mm.
3. **CreateResources / DpiChanged:** Bei DPI-Änderung (z.B. Monitor-Wechsel) `CanvasGeometry`-Cache invalidieren und neu erstellen.
4. **ScrollViewer + Zoom:** Zoom-Faktor und Offsets in DIPs; Umrechnung Screen-DIPs → Welt-mm muss Zoom/Pan berücksichtigen.
5. **FlatteningTolerance:** Bei `StrokeContainsPoint(..., tolerance)` – z.B. `CanvasGeometry.ComputeFlatteningTolerance(dpi, zoom)` für variable Zoom-Stufen.

### Empfehlung für TrackPlan

- **Logik-Ebene:** Alles in mm (Weltkoordinaten)
- **Zeichnen:** `ds.Transform = M` (Zoom, Pan, mm→DIP); Geometrie in mm
- **Hit-Test:** Pointer-DIPs → Welt-mm (inverse View-Transform), dann `StrokeContainsPoint` in mm mit passender Tolerance
- **DPI-Wechsel:** `CreateResources` mit `DpiChanged` → Geometry-Cache leeren, bei nächstem Draw neu aufbauen

---

## Ausgangslage (aktuell XAML)

### Aktuelle Implementierung
- **Canvas** mit `Path`-Elementen pro Segment
- **HitTestSegment**: Manuell via Port-Paare + `DistanceToSegment` (Linie-Punkt-Abstand)
- **Pointer-Events**: `PointerPressed`, `PointerMoved`, `PointerReleased` auf dem Canvas
- **Toolbox-Drag**: `DragStarting` auf Toolbox-Items, `AllowDrop` + `Drop` auf Canvas
- **Ghost**: Separater `_ghostLayer` mit Path, wird bei Drag aktualisiert
- **Rotation-Handle**: Eigener Layer mit `Border`/Ellipse, eigenes `PointerPressed`
- **Koordinaten**: `ScaleMmToPx`, `TransformToVisual` für Maus → Canvas-Pixel → mm

---

## Win2D-Ansatz: Drag & Drop

### Toolbox → Canvas (Drop neues Gleis)

**Unverändert:** Die Toolbox bleibt XAML. `DragStarting` wird wie bisher auf den Toolbox-Items ausgelöst.

**CanvasControl:**
- `AllowDrop="True"`
- `DragOver` / `Drop` wie bisher
- **Koordinaten-Umrechnung**: Beim Drop den Mauspunkt von Control-Koordinaten in Welt-mm übersetzen:
  - Bei ScrollViewer + Zoom: `e.GetPosition(CanvasControl)` → inverse View-Transform anwenden
  - Bei fester View: `(x, y) / ScaleMmToPx` bzw. `(x - panX, y - panY) / zoom / ScaleMmToPx`

Die restliche Drop-Logik (`TrySnapAndPlace`, `FindBestSnap`) bleibt unverändert, da sie nur mit mm-Werten arbeitet.

### Canvas → Canvas (Bewegen von Segmenten)

**Ablauf wie bisher:**
1. `PointerPressed` auf CanvasControl → Hit-Test → Segment gefunden?
2. Ja: `_draggedSegmentId` setzen, `PointerMoved`/`PointerReleased` registrieren, Pointer erfassen
3. `PointerMoved`: Delta berechnen, `MoveGroup` aufrufen, Ghost-Position aktualisieren
4. `PointerReleased`: `TrySnapOnDrop`, Aufräumen

**Ghost mit Win2D:**
- Im `Draw`-Handler: Wenn `_draggedPlaced != null`, Segment ein zweites Mal mit Ghost-Brush (z.B. halbtransparent) zeichnen
- Kein separater Layer nötig – alles in einem `Draw`-Aufruf

---

## Win2D-Ansatz: Selektion und Hit-Testing (Option B)

**Vorgabe:** Hit-Testing erfolgt über Win2D `CanvasGeometry` + `StrokeContainsPoint` – präziser Treffer entlang der tatsächlich gezeichneten Linienstärke.

### Ablauf

1. **Cache:** Pro `PlacedSegment` eine `CanvasGeometry` in **lokalen** Segment-Koordinaten (ohne Platzierungs-Transform) erstellen und cachen.
2. **Invalidation:** Bei Plan-Änderungen und bei `CreateResources` mit `DpiChanged` den Cache leeren.
3. **Hit-Test:**
   - Pointer-Position (DIPs) in Welt-mm umrechnen: `ScreenToWorldMm(pointerX, pointerY)`
   - Für jedes Segment (reihenfolge: von vorne nach hinten):
     - `M = translation(placed.X, placed.Y) * rotation(placed.RotationDegrees)` (in mm)
     - `geometry.StrokeContainsPoint(new Vector2((float)xMm, (float)yMm), (float)strokeWidthMm, null, M, tolerance)`
   - Erstes getroffenes Segment = Treffer

4. **Path-zu-CanvasGeometry:**
   - `PathToCanvasGeometryConverter`: Aus `SegmentLocalPathBuilder.PathCommand`-Liste `CanvasPathBuilder` befüllen (analog zu `PathToSvgConverter`):
     - `MoveTo` → `BeginFigure` (ggf. `EndFigure` vorher wenn nötig)
     - `LineTo` → `AddLine`
     - `ArcTo` → `AddArc` (Win2D-`AddArc` hat eigenes Format – Endpunkte + Radius auf Win2D-Schema mappen)
   - `CanvasGeometry.CreatePath(pathBuilder)` → Geometry in lokalen Koordinaten (mm)

### DPI- und Skalierungsaspekte

- **Geometrie:** In mm aufgebaut – DPI-unabhängig.
- **StrokeWidth:** Gleiche Einheit wie Geometrie → `strokeWidthMm` (z.B. 4 mm).
- **Tolerance:** `CanvasGeometry.ComputeFlatteningTolerance(control.Dpi, maxZoom)` oder feste Tolerance in mm (z.B. 0,5 mm) für konstante Treffergenauigkeit unabhängig von Zoom.
- **Transform M:** In mm; `StrokeContainsPoint` arbeitet in denselben Einheiten wie die Geometry.

---

## Koordinaten-Transformation (Zoom/Pan, DIPs)

### Aktuell
- `ScrollViewer` mit Zoom, Canvas fix 3000×2000
- `MainGrid.TransformToVisual(GraphCanvas).TransformPoint(...)` für Toolbox-Drag
- `ptr.Position` direkt für Klicks auf Canvas (relativ zu Canvas)

### Mit Win2D

**Einheiten:** Pointer-Positionen und CanvasControl-Größen sind in DIPs. `ScaleMmToPx` definiert mm → DIPs (1 mm = 1 DIP bei ScaleMmToPx = 1).

1. **Pointer relativ zum CanvasControl:** `e.GetCurrentPoint(canvasControl).Position` (DIPs)
2. **View-Transform:** ScrollViewer: `HorizontalOffset`, `VerticalOffset`, `ZoomFactor` (alle DIPs)
3. **Screen-DIPs → Welt-mm:**
   - Control-DIPs `(px, py)` von `GetPosition(CanvasControl)`
   - Je nach ScrollViewer-Setup: z.B. `worldDipsX = px * zoom + offsetX`, `worldDipsY = py * zoom + offsetY` (oder inverse Form – muss mit der tatsächlichen View-Matrix übereinstimmen)
   - mm: `xMm = worldDipsX / ScaleMmToPx`, `yMm = worldDipsY / ScaleMmToPx`

4. **Draw-Transform:** Weltkoordinaten (mm × ScaleMmToPx = DIPs) auf View abbilden:
   - `ds.Transform = Matrix3x2.CreateScale((float)zoom) * Matrix3x2.CreateTranslation((float)-offsetX, (float)-offsetY)`
   - Alternativ: Zeichnen in Welt-DIPs, dann nur diese Transform – je nachdem ob Geometrie in mm oder DIPs aufgebaut wird. **Konsistenz mit Hit-Test ist entscheidend.**

---

## Rotation-Handle

- **Zeichnen:** Im selben `Draw`-Aufruf wie die Segmente (Kreis + Linie zum Drehpunkt).
- **Hit-Test:** Einfacher Abstandscheck: `Distance(pointer, handleCenter) < handleRadius` – keine CanvasGeometry nötig.
- **Pointer-Events:** Weiterhin auf dem CanvasControl. Wenn Pointer auf Handle (Abstandscheck), Rotation starten; sonst normales Klick-/Drag-Verhalten.

---

## Checkliste DPI / Skalierung / Auflösung

- [ ] Alle Koordinaten in der Logik in **mm** (Weltkoordinaten)
- [ ] Win2D-Zeichenbefehle und Pointer-Positionen in **DIPs**; Umrechnung mm ↔ DIPs über `ScaleMmToPx`
- [ ] `StrokeContainsPoint` mit Punkt, StrokeWidth und Transform in **mm**
- [ ] `CreateResources` mit `DpiChanged`: Geometry-Cache leeren
- [ ] FlatteningTolerance für `StrokeContainsPoint` bei variablem Zoom anpassen
- [ ] Optional: `DpiScale` für Performance auf High-DPI-Displays
- [ ] ScrollViewer-Zoom und -Offset bei `ScreenToWorldMm` berücksichtigen

---

## Drag & Drop: Überblick

| Aktion                     | Quelle              | Ziel          | Umsetzung mit Win2D                                      |
|---------------------------|---------------------|---------------|----------------------------------------------------------|
| Toolbox → Canvas          | XAML Toolbox-Item   | CanvasControl | `AllowDrop`, `Drop`; Koordinaten in mm umrechnen         |
| Segment bewegen           | CanvasControl       | CanvasControl | `PointerPressed` → Hit-Test → `PointerMoved`/`Released`  |
| Ghost während Drag        | -                   | -             | Im `Draw` zusätzlich Ghost-Segment zeichnen              |
| Rotation-Handle           | CanvasControl       | -             | Im Draw zeichnen; Hit-Test per Abstand; gleiche Pointer  |

---

## Empfehlung

1. **Hit-Test:** **Option B** – `CanvasGeometry` + `StrokeContainsPoint` für präzisen Treffer entlang der Stroke-Breite. Keine Port-basierte Fallback-Logik.
2. **Path-to-Win2D:** `PathToCanvasGeometryConverter` analog zu `PathToSvgConverter` implementieren, um die bestehende `PathCommand`-Struktur wiederzuverwenden.
3. **Koordinaten:** Zentrale Hilfsmethoden `ScreenToWorldMm(x, y)` und `WorldMmToScreen(xMm, yMm)` für konsistente Umrechnung bei Draw, Hit-Test und Drop.
4. **DPI:** Geometrie und Hit-Test in mm; bei `CreateResources` mit `DpiChanged` Geometry-Cache invalidieren; `ComputeFlatteningTolerance` für variable Zoom-Stufen.
5. **Ghost:** Kein eigener Layer; Ghost im `Draw` mit anderem Brush rendern.
