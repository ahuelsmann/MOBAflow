# TrackPlan Refactoring- und Testplan

## Ziel

1. **TrackPlanSvgRenderer** als einzige Quelle für Layout-Berechnung
2. Ausgabe: SVG **und** Platzierungen für Win2D (gleiche Traversierung, gleiche Koordinaten)
3. **EditableTrackPlan** ↔ **TrackPlanResult** konvertierbar (Editor liefert gleiches Datenmodell)
4. Win2D-Rendering nutzt ausschließlich die vom Renderer berechneten Platzierungen

---

## Phase 1: TrackPlanSvgRenderer erweitern

### 1.1 Layout-Berechnung extrahieren und ausgeben

**Aktuell:** `RenderSegmentRecursive` berechnet (x, y, angle) beim Zeichnen, speichert nichts.

**Ziel:** Beim Traversieren werden Platzierungen in einer Liste gesammelt.

**Änderungen:**

| Datei | Aktion |
|-------|--------|
| `TrackPlan.Renderer/TrackPlanSvgRenderer.cs` | Neue Methode `RenderWithPlacements(TrackPlanResult)` → `(string Svg, IReadOnlyList<PlacedSegment> Placements)` |
| | Oder: `ComputePlacements(TrackPlanResult)` hinzufügen, die dieselbe Traversierung wie `Render` nutzt – **gemeinsame private Methode** für beide |

**Implementierungsansatz:**

```
1. Traversierungslogik in private Methode auslagern:
   TraverseAndRender(segment, incoming, x, y, angle, trackPlan, rendered, placements, drawSvg)
   - placements: List<PlacedSegment> – wird beim Einstieg in jedes Segment gefüllt
   - drawSvg: bool – wenn true, SVG zeichnen; wenn false, nur Platzierungen sammeln

2. Render(TrackPlanResult) ruft TraverseAndRender mit drawSvg=true auf

3. ComputePlacements(TrackPlanResult) ruft TraverseAndRender mit drawSvg=false auf
   → Rückgabe: placements (Bounds werden für SVG nicht benötigt, für Win2D optional)
```

**Alternative (einfacher):** Traversierung einmal laufen lassen und dabei **immer** Platzierungen sammeln. SVG-Rendering bleibt wie bisher, zusätzlich wird eine `Placements`-Liste gefüllt. Neue API:

```csharp
public record RenderResult(string Svg, IReadOnlyList<PlacedSegment> Placements);
public RenderResult Render(TrackPlanResult trackPlan)
```

### 1.2 TrackPlanLayoutCalculator entfernen

- Logik vollständig in TrackPlanSvgRenderer integrieren
- TrackPlanLayoutCalculator.cs löschen
- Alle Aufrufer auf `TrackPlanSvgRenderer.ComputePlacements()` umstellen

---

## Phase 2: EditableTrackPlan ↔ TrackPlanResult

### 2.1 TrackPlanResult aus EditableTrackPlan erzeugen

**Ziel:** Editor-Inhalt als TrackPlanResult exportieren (gleiche Topologie, Startwinkel ableitbar).

```csharp
// In TrackLibrary.PikoA oder TrackPlan.Renderer
public static TrackPlanResult FromEditableTrackPlan(EditableTrackPlan plan, double startAngleDegrees = 0)
{
    return new TrackPlanResult
    {
        Segments = plan.Segments.Select(ps => ps.Segment).ToList(),
        Connections = plan.Connections.ToList(),
        StartAngleDegrees = startAngleDegrees
    };
}
```

**Startwinkel:** Aus erstem Segment (ohne eingehende Connection) ableitbar: `placed.RotationDegrees`.

### 2.2 EditableTrackPlan aus TrackPlanResult befüllen

**Ziel:** Platzierungen vom Renderer übernehmen (ersetzt TrackPlanLayoutCalculator).

```csharp
// In EditableTrackPlan
public void LoadFromTrackPlanResult(TrackPlanResult result)
{
    var renderResult = new TrackPlanSvgRenderer().Render(result); // oder ComputePlacements
    LoadFromPlacements(renderResult.Placements, result.Connections);
}
```

**Abhängigkeit:** EditableTrackPlan ← TrackPlan.Renderer würde Zirkularität erzeugen (Renderer ← TrackLibrary.PikoA). Daher: Aufrufer (WinUI) holt Platzierungen und ruft `LoadFromPlacements` auf – **kein** `LoadFromTrackPlanResult` in EditableTrackPlan.

---

## Phase 3: TrackPlanPage (WinUI) anpassen

### 3.1 Load Test Plan

```csharp
private void LoadTestPlan()
{
    var plan = CreateTestPlan();
    var renderResult = new TrackPlanSvgRenderer().Render(plan); // erweitert um Placements
    _plan.LoadFromPlacements(renderResult.Placements, plan.Connections);
    StatusText.Text = "Test Plan geladen.";
}
```

### 3.2 SVG im Browser

- Bleibt wie bisher: `TrackPlanSvgRenderer.Render(plan)` → SVG-String → Export

### 3.3 Export aus Editor

- EditableTrackPlan → `TrackPlanResult.FromEditableTrackPlan(plan)`
- Danach: SVG rendern oder Win2D neu zeichnen (mit berechneten Platzierungen, falls gewünscht)

**Hinweis:** Beim manuellen Editor haben Nutzer eigene Positionen. Für SVG-Export müssen wir entscheiden:
- **A)** Gespeicherte Positionen nutzen (Rendering ohne erneute Layout-Berechnung)
- **B)** Topologie als TrackPlanResult exportieren und Layout neu berechnen (könnte andere Positionen ergeben)

Für Konsistenz mit dem Test-Plan: **A** für Editor-Anzeige (Win2D nutzt PlacedSegment direkt), **B** nur für „Auto-Layout neu berechnen“.

---

## Phase 4: Testplan

### 4.1 Unit-Tests

| Test | Beschreibung | Erwartung |
|------|--------------|-----------|
| **TrackPlanSvgRenderer_ComputePlacements_MatchesSvgPositions** | Dasselbe TrackPlanResult: ComputePlacements vs. Render. Die (x,y,angle) pro Segment aus der SVG (implizit in path-d) müssen mit Placements übereinstimmen. | Platzierungen identisch |
| **TrackPlanSvgRenderer_Render_WithPlacements** | `Render(plan)` liefert nicht-leere Placements-Liste, Anzahl = plan.Segments.Count | ✅ `Render_PlacementsCount_MatchesSegmentsCount` |
| **EditableTrackPlan_FromTrackPlanResult_Roundtrip** | TrackPlanResult → LoadFromPlacements → FromEditableTrackPlan → gleiche Segments/Connections | Segments und Connections gleich |
| **TrackPlanBuilder_TestPlan_SvgEqualsWin2D** | Test-Plan rendern: SVG-Pfad-Koordinaten vs. Win2D-Geometrie (PathToCanvasGeometryConverter + Transform). Visueller Abgleich oder Koordinaten-Vergleich. | Gleiche Eckpunkte/Trajektorien |

### 4.2 Manuelle Tests

| Schritt | Aktion | Erwartung |
|---------|--------|-----------|
| 1 | WinUI starten, Track Plan öffnen | Seite lädt |
| 2 | „Load Test Plan“ klicken | Gleisplan erscheint wie im SVG (Weiche, Zweige, Verbindungen) |
| 3 | „SVG im Browser“ klicken | Browser zeigt gleichen Plan |
| 4 | WinUI und SVG nebeneinander | Optisch identisch (Kurven, Positionen, Verzweigungen) |
| 5 | Segment aus Toolbox auf Canvas ziehen, verbinden | Drag & Drop funktioniert |
| 6 | Export/„Als TrackPlanResult speichern“ (falls implementiert) | Gültiges TrackPlanResult mit korrekter Topologie |

---

## Phase 5: Ablaufübersicht

```
                    TrackPlanResult
                          │
          ┌───────────────┼───────────────┐
          │               │               │
          ▼               ▼               ▼
   TrackPlanBuilder   FromEditablePlan   (Import)
          │               ▲               │
          │               │               │
          └───────────────┼───────────────┘
                          │
                          ▼
              TrackPlanSvgRenderer
                    .Render()
                          │
              ┌───────────┴───────────┐
              ▼                       ▼
         SVG-String            Placements
              │                       │
              ▼                       ▼
       SvgExporter /          EditableTrackPlan
       Browser                        │
                                      ▼
                              Win2D Draw (TrackPlanPage)
```

---

## Reihenfolge der Umsetzung

1. ✅ **TrackPlanSvgRenderer** um Platzierungen erweitern (`Render` → `RenderResult`)
2. ✅ **TrackPlanLayoutCalculator** entfernt, Aufrufer umgestellt
3. ✅ **TrackPlanPage** Load Test Plan und SVG im Browser auf Renderer umgestellt
4. ✅ **CalculateCurvedPortPosition** für R9/R1–R4: korrekte Branch-Positionen bei mehreren Ausgängen (war Ursache für „drei getrennte Kurven“)
5. ✅ **PathToCanvasGeometryConverter.ToCanvasGeometryInWorldCoords**: Win2D nutzt jetzt dieselbe Transformation wie SVG und SegmentPlanPathBuilder
6. ⏸ **FromEditableTrackPlan** (optional, für Export)
7. ✅ Unit-Tests ergänzt (`Render_PlacementsCount_MatchesSegmentsCount`)
8. Manueller Abgleich SVG vs. Win2D: Bitte „Load Test Plan“ testen

---

## Dateien-Übersicht

| Datei | Status |
|-------|--------|
| `TrackPlan.Renderer/TrackPlanSvgRenderer.cs` | ✅ Placements + CalculateCurvedPortPosition |
| `TrackPlan.Renderer/TrackPlanLayoutCalculator.cs` | ✅ Entfernt |
| `WinUI/View/PathToCanvasGeometryConverter.cs` | ✅ ToCanvasGeometryInWorldCoords hinzugefügt |
| `WinUI/View/TrackPlanPage.xaml.cs` | ✅ LoadTestPlan + GraphCanvasControl_Draw auf Renderer/ToCanvasGeometryInWorldCoords |
| `TrackLibrary.PikoA/EditableTrackPlan.cs` | LoadFromPlacements (unverändert) |
| `Test/TrackPlanRenderer/RendererTests.cs` | ✅ Render_PlacementsCount_MatchesSegmentsCount |
| `docs/Piko-A-Gleis-Referenz.md` | Referenz: PIKO-Spez vs. MOBAflow |
| `docs/99556__A-Gleis_Prospekt_2019.pdf` | Offizieller PIKO A-Gleis Prospekt |
