# Feature Specification: Solution save and runtime refresh without races

**Feature Directory**: `016-solution-save-concurrency`

**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/390

**Created**: 2026-10-24

**Status**: Implemented in draft [PR #394](https://github.com/ahuelsmann/MOBAflow/pull/394) at `142e6b69`; the remaining
`MainWindowViewModel.Solution.cs` part depends on [PR #293](https://github.com/ahuelsmann/MOBAflow/pull/293). Issue #390
stays open until the whole agreed scope is delivered.

**Spec Kit**: Required

**Chronology**: The defect was found by reading the code in the architecture audit on 2026-10-23. The implementation
(`142e6b69`, 2026-10-24 15:36 CEST) came first, with characterization tests written before each fix. This specification,
the plan, the tasks and the analysis were written afterwards on 2026-10-24, at the maintainer's request through the
Chief Project Manager, to close the Spec Kit gap. They record what was decided and verified; they do not claim an
earlier planning run.

**Input**: Issue #390 (audit findings 1-3).

## Governance and Traceability *(mandatory)*

**GitHub Issue**: #390

**Related Work**: #389 and #391 (the other audit findings), RF-23 #191 (editor vs runtime snapshot), RF-24 #219 and
PR #293 (session and MainWindow glue), RF-25 #318, RF-29 #313.

## User Scenarios & Testing

### User Story 1 - Opening another solution never damages the previous file (Priority: P1)

An operator edits solution A; an auto-save is still waiting. The operator opens solution B.

**Acceptance Scenarios**:

1. **Given** a save of A is queued behind another save, **When** B is loaded before the queued save runs, **Then**
   file A only ever contains A's projects, and the session keeps B's path and saved state.
2. **Given** a save of A finishes after B was loaded, **Then** it does not change the path, the unsaved-changes flag
   or the save state of B.

### User Story 2 - A save writes one consistent state (Priority: P1)

**Acceptance Scenarios**:

1. **Given** a save is requested, **Then** `SolutionSaving` subscribers (for example the track-plan binder) run on the
   UI thread, and the file receives a copy of the solution as it was at that moment.
2. **Given** the editor changes the model while the file is being written, **Then** the written file is not affected.
3. **Given** the runtime refreshes its copy of a project while an activation holds its lock, **Then** the copy is taken
   when the refresh is requested, on the caller's thread, not after the wait.

### User Story 3 - A late change refreshes the right runtime (Priority: P2)

**Acceptance Scenarios**:

1. **Given** project A is no longer selected, **When** a tracked object of A changes (for example an asynchronous
   editor completing), **Then** A's runtime is refreshed, not the runtime of the selected project B.

### User Story 4 - Saves keep their order and Save As keeps later edits (Priority: P1)

Added after the Codex review of `eeba64f3` (threads `PRRT_kwDOMgU7j86v6uLP` and `PRRT_kwDOMgU7j86v6uLV`).

**Acceptance Scenarios**:

1. **Given** a save is writing and a second save is queued, **When** the model changes before the queued save may write,
   **Then** the queued save writes the newer state; a write never replaces a newer write with an older snapshot.
2. **Given** a new solution without a path, **When** the operator edits while the Save As picker is open, **Then** after
   the picker returns the edit is also saved to the chosen file, and the solution is only reported as saved once it is.
3. **Given** a Save As file was just written, **When** the operator edits before its completion reaches the UI thread,
   **Then** that edit is also written to the chosen file (second review, `PRRT_kwDOMgU7j86wExO_`).
4. **Given** New asks to save unsaved changes, **When** the operator edits while the picker is open, **Then** the edit
   is written to the chosen file before New replaces the solution (second review, `PRRT_kwDOMgU7j86wExWR`).

### Edge Cases

- New solution while a save is pending: same rule as loading (a new generation).
- Save As with a file picker: the picker runs on the UI thread as before; the written content is the snapshot.
- Shutdown: `DrainPendingSaveAsync` still waits for a running write.

## Requirements

- **FR-001**: A save MUST write only the solution generation it was requested for, to that generation's path.
- **FR-002**: A save finishing for an earlier generation MUST NOT change the current solution's path or save state.
- **FR-003**: `SolutionSaving` and the copy that is written MUST be taken on the UI thread.
- **FR-004**: The runtime MUST copy the editor project before it waits for its activation lock.
- **FR-005**: A tracked change MUST refresh the runtime of the project that owns the changed object.
- **FR-006**: The persisted file format MUST NOT change.
- **FR-007** (review finding P1, 2026-10-24): Saves MUST be written in the order of their snapshots; an older snapshot
  MUST NOT overwrite a newer write of the same solution.
- **FR-008** (review finding P2, 2026-10-24): An edit made while a manual save is in progress (for example while the Save
  As picker is open) MUST NOT be lost. If the written snapshot is older than the latest edit, the solution stays dirty
  and the newer state is saved to the new path.
- **FR-009**: The Save As picker MUST be opened on the UI thread.
- **FR-010** (second review, 2026-10-25): Whether a written snapshot holds the latest edit MUST be decided on the UI
  thread when the save completes, not before. A manual save MUST return only once its file holds the latest edit, so a
  caller such as New does not replace the solution before the edits made while its picker was open are written.

## Out of scope

- `MainWindowViewModel.Solution.cs` `RefreshProjectRuntimeSnapshot`: PR #293 removes this helper; its Journey callers
  move to a page that uses `ISolutionSession`. No owner parameter is added to a helper that the combined architecture
  removes (CPM disposition on PR #394, 2026-10-24).
- Untracking of a deselected project's own `ProjectViewModel` (existing behavior, unchanged).

## Success Criteria

- **SC-001**: Deterministic tests for the save/load interleaving, the UI-thread snapshot, the runtime copy timing and the
  owner routing fail on the previous code and pass with the fix.
- **SC-002**: Portable and Windows suites, analyzer gates and Sonar stay green without baseline changes.
