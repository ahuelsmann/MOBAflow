# RF-28: Consolidate the track-plan model

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/298
**Spec Kit**: Not applicable - structural refactoring; the persisted `TrackPlanDocument` format is unchanged
**Status**: Active; slices 1 and 2 integrated through #300, slice 3 through #305; slice 4 (thin page) in review; page layout restore/save waits for RF-24 slice 6

## Purpose and scope

[Issue #298](https://github.com/ahuelsmann/MOBAflow/issues/298) owns acceptance and live status.
The [umbrella plan](QUALITY-AND-REFACTORING-PLAN.md) owns programme dependencies (RF-28 builds on RF-13/#91,
which extracted the editor service, and on the RF-20 architecture tests). This is the single RF-28 child plan.

Target: one persisted track-plan model (`Domain.TrackPlanDocument`), one catalogue-neutral editor boundary that
receives catalogue geometry through an interface, catalogue projects with catalogue geometry only, editor-only
services outside `Backend`, and a `TrackPlanPage` that only adapts WinUI input and Win2D rendering. Editor
behavior, the stored format, the runtime feedback projection and the RF-13 tests stay unchanged.

## Start gate and coordination

- RF-13 (#91) is closed; `SharedUI/Service/TrackPlanEditorService` and `TrackPlanViewModel` are the existing
  platform-neutral editor entry points and stay the owners of editor commands.
- No open PR touches `TrackLibrary.*`, `TrackPlan.Renderer`, `Backend/Service/TrackPlan` or the track-plan page
  (checked on 2026-10-15). RF-24 slice 6 (layout persistence boundary) is a dependency only for the page layout
  restore/save in slice 4; RF-26 does not touch the track-plan projects.

## Source inventory and proposed owners

Consumers were counted from type references on main `877cd41e`.

| Current location | Content | Consumers | Proposed owner |
| --- | --- | --- | --- |
| `Domain/TrackPlanDocument.cs` | Persisted plan in `Project.TrackPlan` | Domain, PikoA editor document, tests | stays: the one persisted model |
| `Domain/TrackLayout.cs` `Layout`, `TrackInstance`, `TrackDefinition`, `Connection`, `ConnectorDefinition` | Second layout model | `Backend` `LayoutService`, `TrackPlanDocumentMapper`, tests | remove with its services |
| `Domain/TrackPlanDocumentMapper.cs` | Legacy document ↔ `Layout` mapping (`LegacyLibraryId`) | tests only | remove (no-legacy policy) |
| `Domain/TrackLayout.cs` `ITrackLibrary`, `TrackLibraryRegistry` | Library lookup by persisted id | Piko library, DI registration | keep `ITrackLibrary` if the editor document needs it; remove the unused registry registration |
| `Domain/TrackLayout.cs` `RailroadState`, `ITrackFeedbackLookup` | Runtime feedback/switch/signal state | Backend projector, editable plan | stays (runtime state, not layout) |
| `Backend/Service/TrackPlan/LayoutService`, `GraphService` | Operations on the second model | DI registration, tests | remove |
| `Backend/Service/TrackPlan/SelectionService`, `UndoRedoService<T>` | Editor selection and history | `TrackPlanEditorService`, DI | editor project |
| `Backend/Service/TrackPlan/TrackPlanRailroadStateProjector` | Runtime feedback projection | MOBAflow, tests (architecture test pins it to Backend) | stays in `Backend` |
| `TrackLibrary.PikoA` `EditableTrackPlan`, `PlacedSegment`, `PortConnection`, `TrackPlanInteractionService`, `TrackPlanSnapHelper`, `TrackPlanSpatialIndex`, `TrackPlanValidationHelper`, `Rendering/TrackPlanRenderSceneBuilder` | Editable plan, interaction, snapping, index, validation, scene | SharedUI, MOBAflow, tests | editor project (depend only on `TrackLibrary.Base.Segment`) |
| `TrackLibrary.PikoA` `TrackPlanEditorDocument` | Editor document ↔ `TrackPlanDocument` via Piko library | SharedUI, MOBAflow, tests | editor project, library passed in |
| `TrackLibrary.PikoA` `SegmentPortGeometry`, `SegmentLocalPathBuilder`, track types (`R1`…`WY`), `PikoACatalog`, `PikoATrackLibrary`, `TrackCatalogEntry` | Piko A geometry and catalogue | editor files, MOBAflow toolbox | stay; implement the geometry interface |
| `TrackLibrary.PikoA` `SegmentGeometryProvider` (`ISegmentGeometryProvider`) | Unused geometry interface | none | becomes the editor's geometry interface (slice 2) or is removed |
| `TrackLibrary.PikoA` `TrackPlan.cs` (`TrackPlanBuilder`, `TrackBuilder<T>`, `PortBuilder`, `TrackPlanResult`) | Fluent plan builder | one test | remove unless a test fixture needs it; then move to `Test` |
| `TrackLibrary.PikoA` `Rendering/TrackPlanSvgRenderer`, `TrackPlanSvgRenderingSupport` | Piko-specific SVG export | MOBAflow, tests | decide in slice 2: renderer project behind the geometry interface |
| `TrackPlan.Renderer/TrackPlan.cs` | Width/height/zoom holder | tests | remove after checking `SvgExporter` |
| `MOBAflow/View/TrackPlanPage.xaml.cs` (1,727 lines) + `.EditorFeatures.cs` | Input, Win2D drawing, plus snap/port-highlight, drag/rotation math, content bounds, zoom/viewport, layout restore/save | WinUI | keep input/drawing; move math to editor or `SharedUI` collaborators |

## Design decisions and alternatives

- **Editor project.** A new `TrackPlan.Editor` project (namespace `Moba.TrackPlan.Editor`) references
  `TrackLibrary.Base` and `Domain`. Alternative: put the editor into `SharedUI` — rejected, because the
  architecture tests already require catalogue-neutral, UI-free editor code and `SharedUI` pulls in Backend.
  Alternative: reuse `TrackPlan.Renderer` — rejected, rendering and editing are separate concerns.
- **Geometry interface.** The editor asks a segment-geometry interface for connector positions and local paths;
  the Piko A catalogue implements it with `SegmentPortGeometry` and `SegmentLocalPathBuilder`. This removes the
  editor's only catalogue-specific dependency. The unused `ISegmentGeometryProvider` is the starting point.
- **One persisted model.** `TrackPlanDocument` stays the stored model; the second `Layout` model, its services
  and the legacy mapper are removed instead of merged, because nothing outside tests uses them.
- **Page adapter.** Math that needs no WinUI types (snap candidates, highlighted ports, group drag delta,
  rotation angle, content bounds, zoom clamp and viewport offset) moves into tested collaborators; pointer
  capture, cursors, Win2D drawing and XAML element creation stay in the page.

## Independently reviewable delivery slices

Each slice is one PR, deletes the superseded code in the same change, moves analyzer baseline entries with their
files and extends the track-plan architecture tests for the boundary it creates.

1. **Remove the second model and dead types.** Remove `Layout`/`TrackInstance`/`TrackDefinition`/`Connection`/
   `ConnectorDefinition`, `TrackPlanDocumentMapper`, `LayoutService`, `GraphService`, unused DI registrations,
   `TrackPlan.Renderer.TrackPlan` and the fluent builder after reference checks; adapt tests that only exercised
   them. Architecture rule: `Domain` has no layout model besides `TrackPlanDocument`.
2. **Editor project and catalogue interface.** `ITrackCatalog` and `SegmentPort` in `TrackLibrary.Base` (ports, path,
   code, display name, segment creation) implemented by `PikoATrackCatalog`; it replaces the unused
   `ISegmentGeometryProvider`, `PikoATrackLibrary` and `Domain.ITrackLibrary`. Create `TrackPlan.Editor`, move the editable plan, placement,
   interaction, snapping, index, validation, scene builder and editor document; Piko A implements the geometry
   interface; update references (SharedUI, MOBAflow, Test, `Moba.slnx`) and the documented dependency table.
   Architecture rules: catalogue projects reference no editor project; the editor references no catalogue.
3. **Editor services out of Backend.** Move `SelectionService` and `UndoRedoService<T>` to the editor project;
   `TrackPlanRailroadStateProjector` stays in Backend.
4. **Thin page.** Extract the math listed above into tested collaborators and reduce the page to input and
   drawing; page layout restore/save moves to the RF-24 layout boundary when RF-24 slice 6 is integrated,
   otherwise it stays and is listed as the remaining exception. Extend `TrackPlanPage_CodeBehind_RemainsAPlatformAdapter`.
5. **Documentation and completion.** Update `docs/ARCHITECTURE.md`, `AGENTS.md` repository map and the umbrella
   plan, then remove this plan.

## Validation strategy

- Every slice: portable and Windows test suites and the cross-platform, Windows and Android analyzer gates;
  the existing `TrackLayoutArchitectureTests*`, `TrackPlanEditorService` and `TrackPlanViewModel` tests must pass.
- Slice 2: MOBAflow FastDebug/Release compile, SVG export tests, editor document round trip with the Piko library.
- Slice 4: new collaborator tests for snap/highlight, drag, rotation, bounds and zoom; manual track-plan UI check
  is listed for the maintainer (no app launch by the agent).

## Risks, compatibility and publication

- **Stored plans:** the persisted `TrackPlanDocument` is untouched; removing the legacy mapper drops no stored data.
- **Performance:** the spatial index and 10,000-track tests must keep passing after the move.
- **Hidden UI coupling:** XAML may bind to moved types; compile MOBAflow for slices 2-4.
- **Analyzer baselines:** moved diagnostics travel with their file; no wholesale refresh.

## Downstream sequence and completion

RF-30 (repository layout) builds on the final project set. Completion: issue #298 acceptance boxes checked,
slice 5 integrated, this plan removed.
