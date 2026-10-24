# Implementation Plan: Solution save and runtime refresh without races

**Source Issue**: #390

**Spec Kit**: Required

**Chronology**: Written on 2026-10-24 after the implementation in `142e6b69`; it records the approach that was taken.

Input: [spec.md](spec.md).

## Approach

1. **Generation:** `SolutionSession` keeps a solution generation that increments on the UI thread whenever a solution
   is loaded or created. A save captures the generation with its path when it is requested.
2. **Snapshot on the UI thread:** before waiting for the save semaphore, a save takes its snapshot through
   `IUiDispatcher.InvokeOnUi` (inline on the UI thread): it checks the generation, raises `SolutionSaving` and copies
   the solution with a JSON round trip (`JsonOptions.Default`). A save of an earlier generation takes no snapshot and
   writes nothing. `InvokeOnUi` is used instead of a new dispatcher method, so existing test dispatchers keep working.
3. **Completion guard:** a successful write updates path and save state only for the current generation.
4. **Owner routing:** `TrackChanges` records the project selected at tracking time in a `ConditionalWeakTable`; a
   change refreshes that project's runtime. A `ProjectViewModel` owns itself.
5. **Runtime copy timing:** `MobaRuntimeService.ActivateProjectAsync` and `UpdateProjectAsync` call `CloneForRuntime`
   before waiting for `_projectActivationLock`. The signal-box change chain in `MainWindowViewModel.Signals.cs` uses
   `ConfigureAwait(true)` so it calls the runtime and the save on the UI context.

## Corrections after the review of `eeba64f3`

Planned on 2026-10-24 before the corrective code, from the two Codex review findings on PR #394.

6. **Snapshot inside the write lock (FR-007):** the cause of P1 is that the snapshot is taken before the save waits for
   `_saveSemaphore`; its continuation (`RunContinuationsAsynchronously`) can then reach the lock after a newer save.
   The save now enters the lock first and takes its UI-thread snapshot inside it. Writes are serialized, so each later
   write holds a state at least as new as the one before. The generation check stays at snapshot time.
7. **Edit version for every save (FR-008):** the snapshot records the edit version (`_saveRequestedVersion`) it
   contains. A completed save reports Saved only if no edit followed; otherwise the solution stays dirty. A manual save
   that is not the latest then requests a follow-up save, which writes the newer state to the path the save just set.
   Auto-saves need no follow-up: the edit that made them stale requested its own save.
8. **Picker on the UI thread (FR-009):** Save As runs through the same UI-thread helper as the snapshot, because the
   save continuation is no longer on the UI thread. The UI helper uses `InvokeOnUi` and a completion source, so no new
   dispatcher method is needed.

## Constitution Check

- Shared logic stays in `SharedUI` and `Backend`; no WinUI or MAUI types enter them.
- Runtime ownership is unchanged: ViewModels still reach the runtime through the existing session and host interfaces.
- No legacy path, no format change, no new dependency. `ConfigureAwait(false)` is kept wherever no UI access follows.
- No application, debugger or hardware start is part of validation.

## Validation Strategy

- Characterization tests first (each fails on the previous code): queued save after a load, UI-thread snapshot with a
  queuing dispatcher, late edit of an earlier project, runtime copy before the activation wait.
- Portable and Windows suites with a hang guard, MOBAflow FastDebug build, analyzer gates without baseline changes,
  Backend mutation host, documentation, Spec Kit, line-ending and secrets checks; GitHub CI and Sonar on the PR head.
- Combined validation with PR #293 once a permitted common stand exists (see [tasks.md](tasks.md)).

## Dependencies

- PR #293 owns `MainWindowViewModel.Solution.cs` and removes `RefreshProjectRuntimeSnapshot`. Direct file overlap with
  #394: `Test/SharedUI/MainWindowViewModelPhotoAssignmentTests.cs` and `docs/ARCHITECTURE.md`.
