# Implementation Plan: A controlled locomotive cannot be deleted

**Source Issue**: #389

**Spec Kit**: Required

**Chronology**: Written on 2026-10-24 after the implementation in `0136b5d7`; it records the approach that was taken.

Input: [spec.md](spec.md).

## Approach

1. **Control state:** a new `SharedUI/Service/RuntimeLocomotiveControl` reads `IProjectRuntimeSnapshots`. A connected
   snapshot's `LocomotiveStates` keys are the controlled addresses of that project. It keeps one address set per
   project and raises `Changed` on the UI thread only when a set changes.
2. **Why runtime state is enough:** Train Control requests the locomotive state (`RequestLocomotiveInfoAsync`) when it
   selects a locomotive, so a selected locomotive appears in `LocomotiveStates` at speed 0 while the Z21 is connected.
   Every drive or function command also creates a state.
3. **Delete guard:** `LocomotivesPageViewModel` gets the control state through the existing `LocomotivePageAnalysis`
   record, so the constructor keeps its parameter count. `CanDeleteLocomotive` and `DeleteLocomotive` both check it;
   `IsDeleteLocomotiveBlocked` and `DeleteLocomotiveBlockedReason` explain it.
4. **UI and composition:** a non-closable informational `InfoBar` on `LocomotivesPage.xaml`; MOBAflow registers
   `RuntimeLocomotiveControl` as a singleton. MOBAsmart has no locomotive editor.
5. **Docs:** the locomotives part of `docs/wiki/MOBAFLOW-USER-GUIDE.md`.

## Corrections after the review of `f4b9939c` (2026-10-25)

From the Claude review threads `PRRT_kwDOMgU7j86wEzBI` and `PRRT_kwDOMgU7j86wEzDe`, and the Codex threads
`PRRT_kwDOMgU7j86wE07o`, `PRRT_kwDOMgU7j86wE07p` and `PRRT_kwDOMgU7j86wE07q`:

6. **Address edits (FR-006):** `OnLocomotivePropertyChanged` notifies the delete state for `DigitalAddress`.
7. **Current snapshots (FR-007):** a snapshot event only requests a refresh. One thread at a time reads the current
   snapshot of every project, and requests made meanwhile lead to one more refresh. The service subscribes before its
   first read. The remaining limit: `MobaRuntimeService.PublishSnapshot` assigns `Current` without synchronization, so
   concurrent publishes can leave `Current` one snapshot behind until the next one. That ordering affects every snapshot
   consumer and is outside this feature.
8. **Request on connect (FR-008):** `TrainControlViewModel.OnZ21ConnectionChanged` requests the selected address when
   the connection becomes active, as a new selection does. The request reads the locomotive state; it does not drive.

## Constitution Check

- ViewModels read runtime state only through the existing runtime interfaces; no runtime ownership in the ViewModel.
- Snapshot events from runtime threads are marshalled with `IUiDispatcher`; theme resources stay in XAML (`InfoBar`).
- UI text is English. No legacy path, no format change, no new dependency.
- No application, debugger or hardware start is part of automated validation; the practical check is W6.

## Validation Strategy

- ViewModel tests for every acceptance scenario and service tests for the control state; portable and Windows suites;
  MOBAflow FastDebug build; analyzer gates; documentation, Spec Kit, line-ending and secrets checks; CI and Sonar.
- Practical acceptance W6 in #365 with the maintainer.
