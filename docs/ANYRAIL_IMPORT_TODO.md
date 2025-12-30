# AnyRail Import - TODO / Diagnostics

Status: In work
Date: 2025-12-29

## Kurzbeschreibung
Beim Import einer AnyRail-XML-Datei werden Segmente (ArticleCodes) erkannt, aber der Renderer erhält keine Verbindungen: `RenderLayout` loggt "91 segments, 0 connections" und nur ein Segment wird gerendert.

Ziel: Aus AnyRail nur die TOPOLOGIE speichern (ArticleCode + Verbindungen). Positionen (X/Y, Rotation, PathData) werden zur Laufzeit vom `TopologyRenderer` berechnet. Import soll robust gegen fehlende Endpoint-Mappings sein.

---

## Bisher implementierte Änderungen
- `AnyRailLayout.ToTrackConnections()` verbessert:
  - Unterstützt mehrere Parts pro Endpoint
  - Dedupliziert Verbindungen
  - Detailliertes Debug-Logging (konvertierte Verbindungen)
  - Coordinate-based fallback: Wenn eine Endpoint→Part Zuordnung fehlt, werden Part-Endpoints innerhalb einer Toleranz (1.0 Einheiten) per (X,Y) gesucht und als Fallback genutzt. Fallback-Matches werden geloggt.
- `TrackGeometryLibrary` erweitert (Basis-Weichen/Turnouts: `WR`, `WL`, `DWW`, `DKW`) mit Endpoints und PathData.
- `TrackPlanEditorViewModel.ImportAnyRailAsync()`:
  - Nutzt `ToTrackConnections()` und fällt bei 0 Verbindungen auf `DetectConnections()` zurück.
  - Loggt importierte ArticleCodes und fehlende Geometrien.

Geänderte Dateien:
- `Domain/TrackPlan/AnyRailLayout.cs`
- `SharedUI/Renderer/TrackGeometryLibrary.cs`
- `SharedUI/ViewModel/TrackPlanEditorViewModel.cs`

---

## Symptome / Beobachtungen (Logs)
- `🎨 RenderLayout: 91 segments, 0 connections`
- `✅ Rendered: 1 segments`
- `📂 Imported AnyRail: 91 segments, 0 connections`
- (Eventuell) `System.ArgumentException` im UI beim Rendern

---

## Diagnoseschritte (ausführen und Logs sammeln)
1. App neu starten (HotReload ist möglich, aber Neustart empfohlen).
2. Öffne Visual Studio -> Ausgabefenster (Debug) oder laufende App-Konsole.
3. Importiere die AnyRail-XML über UI (Import -> AnyRail).
4. Sammle folgende Log-Zeilen aus Output:
   - `AnyRail: Parts=..., Endpoints=..., Connections=...`
   - Alle Zeilen mit `🔗 ToTrackConnections:` oder `🔁 ToTrackConnections:` (erzeugte Mappings / Fallbacks)
   - Alle Warnungen `⚠️ ToTrackConnections:` (fehlende Zuordnungen oder fehlende Koordinaten)
   - `📦 Imported article codes:` und `⚠️ Missing geometries for article codes:`
5. Wenn `Connections.Count == 0`: prüfe ob `<connections>`-Elemente in XML korrekt geparst wurden (AnyRailLayout.Connections.Count > 0).

CLI/VS-Befehle zum Bauen (optional):
- `dotnet build` (im Repo root)
- `dotnet build SharedUI/SharedUI.csproj`

---

## Priorisierte ToDos (nächste Aktionen)
1. (Dringend) Führe Import durch und poste die aufgezeichneten Logs hier.
2. (Wenn Fallbacks erfolgreich) Prüfe, ob `Connections.Count > 0` und ob `TopologyRenderer.Render()` mehr Segmente zurückgibt.
3. (Wenn fehlende Geometrien gelistet) Ergänze `TrackGeometryLibrary` für die fehlenden ArticleCodes oder erstelle Mapping-Regeln (z. B. aliasing, e.g. AnyRail code -> Piko code mapping).
4. (Wenn Mapping-Lücken verbleiben) Erhöhe Fallback-Toleranz (z. B. 1.0 → 2.0) oder implementiere nearest-neighbor mit maxDistance-Konfig.
5. (Optional) Unit-Tests für `ToTrackConnections()` erstellen:
   - Test: vollständige Connections-Set mit exact matches
   - Test: Verbindungen mit fehlenden direct mappings -> coordinate fallback
6. (Optional) Replace Debug.WriteLine mit structured `ILogger` calls (production readiness).

---

## Implementation-Details (Technisch)
- Transformation-Konzept (Wichtig):
  - Positionen werden nicht gespeichert.
  - `TopologyRenderer` führt BFS/DFS ab einem Root-Segment aus.
  - Welttransformation eines Child-Segments = Parent.WorldTransform * Parent.ConnectorTransform(from) * Inverse(Target.ConnectorTransform(to)).
  - Nur `TrackSegment.ArticleCode`, `Connections`, `AssignedInPort` sind persistent.

- Coordinate-fallback parameters:
  - initial tolerance = `1.0` (AnyRail units)
  - fallback behavior: match all part endpoints within tolerance, order by distance, use matches for connection creation
  - skip connection if no match found after fallback

---

## Reproduktionspfade (Dateien)
- Parser + converter: `Domain/TrackPlan/AnyRailLayout.cs`
- Import UI: `SharedUI/ViewModel/TrackPlanEditorViewModel.cs`
- Renderer: `SharedUI/Renderer/TopologyRenderer.cs`
- Geometry library: `SharedUI/Renderer/TrackGeometryLibrary.cs`
- WinUI View: `WinUI/View/TrackPlanEditorPage.xaml(.cs)`

---

## Notes / Decisions
- Designentscheid: Domain speichert nur Topologie; Positionen sind Laufzeit-Resultate.
- Logging aktuell per `Debug.WriteLine`/`Console` — für Produktion sollte `ILogger` verwendet werden (separater Task).
- Falls Sie wünschen, erweitere ich automatisch `TrackGeometryLibrary` mit allen fehlenden Codes, sobald Sie die `Missing geometries`-Liste posten.

---

## Nächster Thread / Übergabe
Wenn wir in einem neuen Thread fortsetzen, referenziere diese Datei: `docs/ANYRAIL_IMPORT_TODO.md`.
Kopiere die angeforderten Logs hierhin und ich mache den nächsten Patch (Geometrien ergänzen oder Mapping-Fixes).


*Ende der TODO-Datei.*

## Session Snapshot (2025-12-29)

Latest run summary:

- AnyRail parsed: Parts=91, Endpoints=194, Connections=96
- ToTrackConnections converted 96 connections (raw 96) — many `🔗 ToTrackConnections:` lines logged (see output)
- TopologyRenderer rendered 91 segments and returned PathData for each
- UI raised `System.ArgumentException` during render cycle (WinRT.Runtime.dll) but render completed for 91 segments afterwards

Important log excerpts (already captured):

```
AnyRail: Parts=91, Endpoints=194, Connections=96
ToTrackConnections: converted 96 connections (raw 96)
📦 Imported article codes: DKW,DWW,G119,G231,G62,R1,R2,R3,WL,WR
🎨 RenderLayout: 91 segments, 96 connections
✅ Rendered: 91 segments
Ausnahme ausgelöst: "System.ArgumentException" in WinRT.Runtime.dll
```

Likely cause of exception: invalid `PathData` or unexpected UI binding value during the apply-phase in `TrackPlanEditorViewModel.RenderLayout()`.

## Resume Instructions

When resuming on another machine/window, continue here:

1. Open repository and branch `main` (current working branch). Confirm latest commits.
2. Open `docs/ANYRAIL_IMPORT_TODO.md` for context (this file).
3. Reproduce import once to re-capture logs.
   - Start app, Import AnyRail XML, copy Output logs.
4. If exception persists, implement diagnostic guard (already suggested):
   - Wrap assignment of `vm.PathData` and `vm.Rotation` in try/catch and log offending `rs.PathData` and segment id.
5. If many `Missing geometries` appear later, extend `SharedUI/Renderer/TrackGeometryLibrary.cs` with additional ArticleCodes.

Files of interest:
- `Domain/TrackPlan/AnyRailLayout.cs` (ToTrackConnections + fallback)
- `SharedUI/ViewModel/TrackPlanEditorViewModel.cs` (Import + RenderLayout)
- `SharedUI/Renderer/TrackGeometryLibrary.cs` (geometry templates)
- `SharedUI/Renderer/TopologyRenderer.cs` (BFS -> positions)

## Next prioritized tasks (when continuing)

1. Add try/catch and logging around PathData assignment in `RenderLayout()` (high priority).
2. Replace `Debug.WriteLine` with `ILogger` for structured logs (medium priority).
3. Add small unit tests for `ToTrackConnections()` (coordinate fallback cases) (low priority).

---

End session snapshot.

## Mental Model & Transformations (Mandatory)

Diese Regeln müssen im Code und in zukünftigen Patches strikt eingehalten werden.

- Der Gleisplan ist ein Graph, kein Canvas mit gespeicherten Koordinaten.
- Knoten = `TrackSegment` (speichert nur ArticleCode, lokale Geometrie-Referenz, Connectoren, Verbindungen)
- Kanten = `TrackConnection` (verbindet zwei Connectoren verschiedener Segmente)
- Weltpositionen (X/Y/Rotation / WorldTransform) werden zur Laufzeit durch Traversieren des Graphen berechnet — sie werden nicht persistiert!

Technische Spezifikation (Transformationsalgorithmus):

```
class TrackSegment
{
    string Id;
    TrackGeometry Geometry; // aus TrackGeometryLibrary (endpoints, connector transforms)
    List<TrackConnection> Connections;
    // WorldTransform wird NICHT gespeichert; nur im Renderer berechnet
}

class TrackConnection
{
    string FromConnectorId; // Connector index/name on source segment
    string ToSegmentId;
    string ToConnectorId;   // Connector index/name on target segment
}

// Compute world transforms starting from a chosen root segment
void ComputeWorldTransforms(TrackSegment root)
{
    root.WorldTransform = Transform2D.Identity;
    Traverse(root);
}

void Traverse(TrackSegment segment)
{
    foreach (var connection in segment.Connections)
    {
        var target = connection.TargetSegment;
        if (target.WorldTransform.IsValid) continue; // already computed

        var parentConnector = segment.Geometry.GetConnectorTransform(connection.FromConnectorId);
        var childConnectorInv = target.Geometry.GetInverseConnectorTransform(connection.ToConnectorId);

        target.WorldTransform = segment.WorldTransform * parentConnector * childConnectorInv;

        Traverse(target);
    }
}

// Render pass
foreach (var segment in TrackPlan.Segments)
{
    var geometry = segment.Geometry;
    var world = segment.WorldTransform; // computed above
    DrawSegment(geometry, world);
}
```

Hinweise:
- `GetConnectorTransform(id)` liefert die lokale Transform (translation + rotation) vom Segment-Ursprung zum Connector.
- `GetInverseConnectorTransform(id)` ist die inverse Transform des Connector-Systems.
- Matrix-Multiplikation ist in Reihenfolge: ParentWorld * ParentConnector * Inverse(ChildConnector).
- Wurzel-Segment (Root) kann beliebig gewählt werden; oft ist es das erste Segment oder ein benutzerdefiniertes Root.

### Enforce in Code
- Entferne alle Persistenz-Felder für X/Y/Rotation aus Domain-Modellen.
- ViewModels dürfen temporär X/Y anzeigen, aber müssen aus Renderer-Ergebnissen abgeleitet werden.
- Unit-Tests: Add tests that verify world transform propagation for small graphs (line, branch, loop).

---

Referenz: `SharedUI/Renderer/TopologyRenderer.cs` (BFS basierte Layout-Berechnung)
