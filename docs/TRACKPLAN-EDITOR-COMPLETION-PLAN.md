# Track Plan Editor Completion Plan

This document hands the remaining TrackPlan editor work to a new session.

## 1. Repair the overall baseline

- Inspect and correct the missing `ChangeJourneyStopViewModel` reference in `MOBAflow/Selector/EntityTemplateSelector.cs`.
- Remove the duplicate `FeedbackSequence` initialization in `Test/Backend/JourneyManagerFeedbackTests.cs`.
- Verify with:

```powershell
dotnet build MOBAflow/MOBAflow.csproj --no-restore --no-dependencies
dotnet test Test/Test.csproj --no-restore --framework net10.0
```

## 2. Extract remaining canvas interaction adapters

- Keep WinUI pointer, drag/drop, pointer-capture, and canvas-invalidation code in `TrackPlanPage`.
- Move drag state, snap preview, group movement, selection decisions, and document mutations to `TrackPlanInteractionService` and `TrackPlanViewModel`.
- Keep `TrackPlanPage.xaml.cs` as a coordinate/input adapter only.
- Add tests for drag, snap, delete, disconnect, rotate, and undo/redo.

## 3. Complete the render scene

- Extend `TrackPlanRenderScene` with labels, selection, feedback pulse, and validation markers.
- Make Win2D drawing, SVG export, and a future Skia adapter consume only the render scene.
- Test identical item IDs, paths, bounds, and label data across projections.

## 4. Complete RailroadState

- Define feedback reset/timeout behavior.
- Add runtime-only switch and signal states; do not persist them in `TrackPlanDocument`.
- Extend `TrackPlanRailroadStateProjector` for all runtime events.
- Test feedback activation/deactivation, timeouts, and persistence isolation.

## 5. Finish performance work

- Keep `TrackPlanSpatialIndex` incremental for add, move, remove, and connection changes; reserve full rebuilds for bulk load.
- Add regression/benchmark coverage for 10,000 tracks: hit test, snapping, and index updates.
- Acceptance criterion: pointer-move paths query local candidates and do not scan the full plan.

## 6. Final acceptance

- Manually validate Light and Dark themes.
- Validate save/load, snap, undo/redo, Z21 feedback, and SVG export.
- Run the full test suite and WinUI build before declaring the architecture work complete.
