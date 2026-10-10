# Issues #137 and #130: shared solution history preparation

**GitHub Issue**: #137
**Related Issue**: [#130](https://github.com/ahuelsmann/MOBAflow/issues/130)
**Spec Kit**: Required
**Status**: Preparation only; implementation is blocked.

## Baseline and start conditions

Inspected on 2026-10-11 against `github/main` at `9b03676a9865275711d4031815f26a2d35e2474b`.
Both issues are open. PR [#131](https://github.com/ahuelsmann/MOBAflow/pull/131) is closed without a merge;
its checked acceptance boxes in #130 are historical branch evidence, not evidence that main contains that implementation.
Current main already has event-plan editing; assess each requested UI change against that implementation.

Implementation starts only after:

1. RF-23 [#191](https://github.com/ahuelsmann/MOBAflow/issues/191) is complete and integrated.
   PRs #207, #211 and #217 were open drafts at inspection time; do not build this feature on their working branches.
2. RF-24 [#219](https://github.com/ahuelsmann/MOBAflow/issues/219) is complete and integrated.
   It owns page extraction and removal of shell dependencies.
3. The Spec Kit specification, clarification, plan, tasks and analysis for #137/#130 exist and agree with the integrated base.
   No such feature directory exists at this baseline.

Refresh this audit immediately before implementation. Delete this standalone plan after delivery and acceptance;
the closed issues, feature specification and Git history retain the record.

## Confirmed scope and boundaries

One bounded history belongs to the opened solution. Page, project and journey selection within that solution preserve it;
successful replacement by a different/new solution separates it. Failed or cancelled loads preserve the current history.
Model edits, collection edits, imports and grouped gestures participate. Load/save operations are not user history steps.
Undo/redo refresh bindings, selections and validation and save one consistent result without recording a new step.
New edits after undo discard the redo branch; cancelled and failed operations leave no partial history entry.

Persisted InPort counts are runtime data in a separate store. They survive restart, are outside `solution.json`,
and are never restored by editor undo. Journey activation, workflow execution and page selection do not reset them.
The zero-on-restart wording still present in #137 is superseded by the maintainer's explicit persistence requirement.
Runtime commands, locomotive movement, announcements and already executed actions are not repeated by history replay.
Runtime projections and external/remote updates need an explicit conflict policy before any whole-model snapshot is considered.
Check #137's older `VehicleUsage` examples against the post-refactoring domain; do not reintroduce removed models.

## Source-backed initial coverage inventory

This is an inventory of seams to recheck after RF-24, not a claim of complete operation coverage.
Every editable field and command, including direct XAML bindings, must enter the final Spec Kit coverage matrix.

| Data / surface | Current seam | History integration and acceptance to specify |
| --- | --- | --- |
| Solution / projects | `SharedUI/Service/SolutionSession.cs`, `SolutionViewModel.cs`, `ProjectViewModel.cs` | Name, add/remove projects, project Z21 assignment; stable IDs and references on restore |
| Rolling stock | `MainWindowViewModel.Wagons.cs`, `LocomotiveViewModel.cs`, `PassengerWagonViewModel.cs`, `GoodsWagonViewModel.cs` | Property and nested payload changes, add/remove, photo references; file effects require a separate policy |
| Trains | `MainWindowViewModel.Train.cs`, `TrainViewModel.cs` | Add/remove trains, vehicle assignments, ordering and duplicate references |
| Stations / journeys | `MainWindowViewModel.Stations.cs`, `MainWindowViewModel.Journey.cs`, `StationViewModel.cs`, `JourneyViewModel.cs` | Stations, platforms, stop definitions, membership and reference restoration |
| Journey event plan / Event Manager | `EventManagerViewModel.cs`, `JourneyEventViewModel.cs` | Add/delete/duplicate/reorder, workflow assignment, enabled/InPort/count edits and count-text commit |
| Shared workflows | `WorkflowLibraryViewModel.cs`, `WorkflowViewModel.cs` | Create/duplicate/delete, ordered actions, action properties and payloads; current workflows are action lists |
| Track plan | `TrackPlanViewModel.cs`, `Backend/Service/TrackPlan/UndoRedoService.cs` | Integrate existing editor history; gestures are grouped; no competing local stack remains |
| Signal box / interlocking | `SignalBoxPlanViewModel.cs`, `SignalBoxPropertiesViewModel.cs`, `MainWindowViewModel.Signals.cs` | Definition/presentation edits enter history; runtime aspects and hardware commands do not |
| Timetable | `TimetablePageViewModel.cs` | Persisted services/policy edits; exclude runtime timing/projections |
| Remaining project data | `Domain/Project.cs`: matrices and locomotive whistle rules | Locate every editing/import route and assign its tests before claiming complete coverage |
| Application settings / layout | `MainWindowViewModel.Settings.cs`, `MainWindowViewModel.LayoutPanels.cs` | Outside solution history unless stored in solution; preserve star-column settings |

Paths without a prefix in the table are in `SharedUI/ViewModel/`.

## Existing histories and save boundary

`EventManagerViewModel` owns JSON undo/redo stacks for `JourneyEventPlan` and clears them on journey selection.
It calls `CaptureUndo` around commands and through row edit callbacks. Removing only its buttons would leave a competing history.
`TrackPlanViewModel` delegates undo/redo to its editor service; `UndoRedoService<T>` has two stacks and no implemented size limit.
The integration must trace both callers and service ownership before replacing either history.

`SolutionSession` owns load/save, `ModelChanged`, dirty state and `SuppressAutoSave`.
Suppression currently bypasses tracked change handling, including runtime-definition refresh.
History replay therefore needs a tested final notification/save/definition-refresh sequence, not merely a suppression block.
RF-23 owns that runtime update boundary; use its final integrated contract rather than changing it here.

## Event Manager scope to carry into the specification

Keep Event Manager responsible for event-plan editing and assignment of an existing workflow.
Keep workflow action authoring on the existing Workflows page. Preserve current count editing, direct InPort/count matching,
workflow reference validation, drag/drop and runtime status. Reconcile #130's older repeat-count, stop and graph wording with
`JourneyEventPlan`, ordered workflow actions and explicit stop-change actions; do not restore the abandoned graph editor from #131.
Shared Undo/Redo controls and Ctrl+Z/Ctrl+Y must consume the same service. Textbox undo must not also execute global undo.

## Narrow delivery sequence after the gates

1. Complete the specification and coverage matrix. Compare a small internal service with the libraries named in #137;
   verify current licenses, maintained targets, grouping, failure behavior and memory bounds before selecting or installing one.
2. Prove the history/save boundary using property, nested property, collection and grouped changes.
   Include cross-page undo, load failure, external update conflicts and reference restoration.
3. Connect the Event Manager and one further page to the shared history with regression tests;
   remove replaced local history code in the same slice and preserve other pages until their explicit integration slice.
4. Complete the remaining coverage matrix and integrate the track-plan history. Refine Event Manager density against current main.
   Completion requires every editable solution route to participate and all competing histories to be gone.

## Required implementation evidence

Extend meaningful existing fixtures, including `EventManagerEventPlanTests`, `SolutionSessionTests` and the affected track-plan tests.
Prove page/project/journey switches retain one history, solution replacement separates it, failed edits are atomic,
text edits/drag gestures group correctly, memory limits release references, and replay causes no save/record loops.
Use fake gateways to prove zero hardware commands and an independent counter store to prove persisted counts remain unchanged.
Run affected portable tests and consumer builds; run the required broader suite for persistence/runtime boundaries.
Compile the desktop app and, with explicit launch authorization, check Light/Dark, keyboard and drag/drop.
Each slice is a draft PR; current-head CI, green SonarCloud and zero OPEN/CONFIRMED PR issues precede readiness.

## Preparation validation

This change contains planning documentation only. Validate secrets, line endings, internal links, Spec Kit governance
and the final diff; no product execution, dependency installation, schema change or .NET test claim is part of preparation.
