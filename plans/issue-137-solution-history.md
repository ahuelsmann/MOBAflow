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

## Library research (2026-10-11; preparation only)

This source review is allowed now. Installing, prototyping or integrating a history library remains gated by RF-23,
RF-24 and the agreed Spec Kit artifacts above. It makes no decision for #141 and selects no library.
Package assets and nuspec metadata were inspected in memory without installation; source findings below are
static observations, not executed tests. Refresh versions and source behavior before the implementation decision.

| Evidence | Doraku/DefaultUnDo | KirillOsenkov/Undo (`GuiLabs.Undo`) |
| --- | --- | --- |
| Latest stable package observed | [2.1.0, published 2026-02-21][du-package] | [1.0.2, published 2018-04-09][gui-package] |
| DLL targets actually included | `netstandard2.0`, `netstandard2.1`, `net8.0`, `net9.0`, `net10.0` ([package][du-assets], [project][du-project]) | Only `netstandard2.0` ([package][gui-assets], [project][gui-project]); NuGet's `net10.0`/Windows/Android entries are computed compatibility, not included targets or tested platform support |
| License | MIT-0 ([package metadata][du-assets], [license][du-license]) | MIT with notice retention ([license][gui-license]); nuspec links MIT but has no repository commit field |
| Maintenance indicator, not a support promise | Latest default-branch commit [`be05ad3ff07e298737e81cd7d722e989c98b2f16`][du-head], 2026-10-10 21:37:10 UTC, moves tests to MTP; package repository commit is `73d17f81d10b91d8f9395bb290b0f3c4d5274664` | Latest default-branch commit [`6147034013212fed05640eca67f2f7e774c13fd8`][gui-head], 2018-04-09 17:26:36 UTC; no newer commit observed, repository not archived at inspection |
| Grouping and merging | [Transactions][du-manager] nest, commit one `GroupUnDo`; dispose without commit reverses recorded commands. [`IMergeableUnDo`][du-stack] merges adjacent commands; [groups][du-group] merge only when wrapping one mergeable command | [Transactions][gui-transaction] nest and default to delayed execution; dispose commits unless explicitly aborted. [History][gui-history] calls previous action's `TryToMerge`; [property action][gui-property] merges consecutive writes to the same object/property without consulting `AllowToMergeWithPrevious` |
| Limits | [Constructor][du-manager] accepts a positive operation capacity (default `int.MaxValue`); [ring buffer][du-buffer] overwrites oldest slots. Capacity counts groups/commands, not retained bytes; active transaction lists are unbounded | [Linked history][gui-history] has no capacity/byte limit in inspected source; [manager][gui-manager] fixes the internal history implementation. Application-level bounded retention remains unproven |

DefaultUnDo behavior was inspected at its 2.1.0 package commit; GuiLabs source at its current commit.
The GuiLabs nuspec does not establish source-to-binary identity; identical package behavior is therefore unverified.
Neither NuGet computed compatibility nor commit recency validates MOBAflow integration.

### Failure, rollback and reentrancy findings

- DefaultUnDo [Do][du-manager] executes before recording: an exception resets cyclic depth in `finally` and
  skips recording, but does not compensate partial model changes. A failed command is absent from transaction rollback.
- Both DefaultUnDo [stack][du-stack] and [buffer][du-buffer] move history state before calling Undo/Do for replay.
  If the callback throws, history has moved while the manager's `Version` assignment is skipped; no compensation is present.
- DefaultUnDo [transaction disposal][du-manager] pops its scope before reversing commands; [group replay][du-group]
  stops at the first exception. Remaining changes are not automatically restored. Rollback callbacks are outside the
  manager's cyclic-depth guard, so callback-driven recording also needs explicit tests.
- DefaultUnDo's cyclic depth suppresses recording of nested Do calls during Do/Undo/Redo; it does not reject nested
  Undo/Redo calls generally. Undo/Redo/Clear are rejected inside open transactions ([manager][du-manager]).
- DefaultUnDo [bounded Clear][du-buffer] resets indices/flags without clearing the array: old command references remain
  until overwritten or the manager is released. Redo branches becoming inaccessible does not guarantee reference release.
- GuiLabs [RecordAction][gui-manager] appends before executing. If Execute throws, its `finally` clears `CurrentAction`,
  but the appended action remains a redo candidate and any preceding redo branch may already have been replaced
  ([history][gui-history]). A model partly changed by Execute is not rolled back automatically.
- GuiLabs [Undo/Redo][gui-manager] lack `finally` around `CurrentAction`; callback exceptions can leave the manager busy.
  [History][gui-history] advances its cursor after successful callback completion, but provides no model compensation.
- GuiLabs [Rollback][gui-manager] unexecutes only the current transaction, then clears the entire transaction stack;
  an UnExecute exception skips that clearing. [Dispose][gui-transaction] commits even during exception unwinding unless
  Rollback has succeeded. Delayed/custom actions and nested immediate scopes need separate cancellation tests.
- GuiLabs blocks recording and nested Undo/Redo while `CurrentAction` is set; immediate transaction callbacks are
  executed without setting that guard ([manager][gui-manager]). Neither inspected manager provides synchronization;
  thread safety and event-handler exception recovery are unverified for MOBAflow.

### Existing internal service and later comparison tests

[`UndoRedoService<T>`][internal-history] only stores supplied values in two unbounded stacks and clears redo on Record.
Despite its bounded-history summary, it has no capacity, cloning, grouping, merging, notifications or replay suppression.
TryUndo/TryRedo transfer history before the caller applies state; application failure recovery and solution ownership
are caller responsibilities. Keep this small internal option in the comparison; these gaps do not prove a library is needed.

After the implementation gates, use the following acceptance cases to compare the internal option and both candidates:

1. Property/nested-property/collection edits undo across pages and retain stable references; mutable snapshots stay isolated.
2. Nested grouping, empty groups and cancelled text/drag gestures produce exactly the agreed entry count and inverse order.
3. Undo followed by a new edit discards redo, including after merging; the chosen capacity behaves correctly after wraparound.
4. Fail before mutation and after partial mutation in Do, Undo, Redo, grouped replay and rollback; inspect model, cursor,
   CanUndo/CanRedo, version, dirty state and next successful operation. No partial history or silent recovery is accepted.
5. Reentrant callbacks and exceptions in notification/save handlers cannot record replay or leave a busy manager stuck.
6. Capacity eviction, redo discard, Clear and solution replacement release captured models, closures and large payloads;
   measure both retained bytes and operation count, including one large group.
7. Replay emits the agreed final notification, runtime-definition refresh and one consistent save; fake gateways receive
   zero runtime/hardware commands and persisted InPort counts remain unchanged, including after restarting the counter store.
8. Failed/cancelled load preserves history; successful solution replacement separates it; textbox shortcuts never double-undo.

Open evaluation order: define these contracts in Spec Kit, compare the small internal service's required changes,
then assess DefaultUnDo's bounded-buffer/failure gaps and GuiLabs' transaction/failure gaps with the same cases.
Package integration, binary/source matching for GuiLabs, retained-byte measurements and platform builds remain pending.

[du-package]: https://www.nuget.org/packages/DefaultUnDo/2.1.0
[gui-package]: https://www.nuget.org/packages/GuiLabs.Undo/1.0.2
[du-assets]: https://api.nuget.org/v3-flatcontainer/defaultundo/2.1.0/defaultundo.2.1.0.nupkg
[gui-assets]: https://api.nuget.org/v3-flatcontainer/guilabs.undo/1.0.2/guilabs.undo.1.0.2.nupkg
[du-project]: https://github.com/Doraku/DefaultUnDo/blob/73d17f81d10b91d8f9395bb290b0f3c4d5274664/source/DefaultUnDo/DefaultUnDo.csproj#L5-L23
[gui-project]: https://github.com/KirillOsenkov/Undo/blob/6147034013212fed05640eca67f2f7e774c13fd8/src/UndoFramework/UndoFramework.csproj#L1-L14
[du-license]: https://github.com/Doraku/DefaultUnDo/blob/73d17f81d10b91d8f9395bb290b0f3c4d5274664/LICENSE.md
[gui-license]: https://github.com/KirillOsenkov/Undo/blob/6147034013212fed05640eca67f2f7e774c13fd8/LICENSE
[du-head]: https://github.com/Doraku/DefaultUnDo/commit/be05ad3ff07e298737e81cd7d722e989c98b2f16
[gui-head]: https://github.com/KirillOsenkov/Undo/commit/6147034013212fed05640eca67f2f7e774c13fd8
[du-manager]: https://github.com/Doraku/DefaultUnDo/blob/73d17f81d10b91d8f9395bb290b0f3c4d5274664/source/DefaultUnDo/UnDoManager.cs#L13-L288
[du-group]: https://github.com/Doraku/DefaultUnDo/blob/73d17f81d10b91d8f9395bb290b0f3c4d5274664/source/DefaultUnDo/GroupUnDo.cs#L80-L116
[du-stack]: https://github.com/Doraku/DefaultUnDo/blob/73d17f81d10b91d8f9395bb290b0f3c4d5274664/source/DefaultUnDo/Internal/UnDoStack.cs#L27-L67
[du-buffer]: https://github.com/Doraku/DefaultUnDo/blob/73d17f81d10b91d8f9395bb290b0f3c4d5274664/source/DefaultUnDo/Internal/UnDoBuffer.cs#L85-L168
[gui-manager]: https://github.com/KirillOsenkov/Undo/blob/6147034013212fed05640eca67f2f7e774c13fd8/src/UndoFramework/ActionManager.cs#L75-L267
[gui-history]: https://github.com/KirillOsenkov/Undo/blob/6147034013212fed05640eca67f2f7e774c13fd8/src/UndoFramework/History/SimpleHistory.cs#L70-L168
[gui-transaction]: https://github.com/KirillOsenkov/Undo/blob/6147034013212fed05640eca67f2f7e774c13fd8/src/UndoFramework/Transaction.cs#L16-L119
[gui-property]: https://github.com/KirillOsenkov/Undo/blob/6147034013212fed05640eca67f2f7e774c13fd8/src/UndoFramework/Actions/SetPropertyAction.cs#L40-L49
[internal-history]: https://github.com/ahuelsmann/MOBAflow/blob/9b03676a9865275711d4031815f26a2d35e2474b/Backend/Service/TrackPlan/UndoRedoService.cs#L5-L41

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
