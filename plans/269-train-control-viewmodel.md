# RF-11: Decompose TrainControlViewModel

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/269
**Spec Kit**: Not applicable - behavior-preserving decomposition; no product, protocol or persisted-format change
**Status**: Plan for review; slice 1 follows in its own PR
**Evidence**: main commit `c4e427d0`, inspected on 2026-10-13

## Purpose and scope

[Issue #269](https://github.com/ahuelsmann/MOBAflow/issues/269) owns acceptance and live status.
The [umbrella plan](QUALITY-AND-REFACTORING-PLAN.md) owns programme dependencies; RF-11 depends only on
RF-04, which is complete. This is the single RF-11 child plan; it transfers the umbrella's extraction order
and acceptance anchor.

The outcome is a train-control ViewModel that binds the MOBAflow Train Control page and the MOBAsmart
Engines/Control tabs and composes focused collaborators. Preserve bindings, commands, defaults, the host
differences (`TrainControlHost`, remote and hybrid snapshot modes), command routing through
`IRuntimeCommandGateway`, the local-command grace periods and the UI-thread continuations of ramps and
timers. Remove superseded code after each extraction.

No protocol, persisted-format or new user-visible behavior is included; RF-12, RF-24 and the solution-wide
constructor policy of RF-25 stay out of scope.

## Source inventory

`SharedUI/ViewModel/TrainControlViewModel.cs` has 2,637 lines in one file (plus a 22-line
`ConnectionStatus` partial), 29 observable properties, 13 relay commands and 11 constructor parameters
(eight optional). Responsibilities by area (line ranges at the evidence commit):

| Lines | Responsibility |
| ---: | --- |
| 90-265, 829-1000, 1170-1210, 1562-1575, 2563-2585 | locomotive list from project and fleet snapshots, picker selection, restore of the last locomotive, address |
| 175-245, 467-510, 1622-1860, 1879-1905, 2485-2560 | speed and km/h conversion, DCC speed steps, ramp, 75 ms drive debounce, direction change, presets, stop and emergency stop |
| 265-285, 1310-1486, 1905-2322 | function buttons, glyph/colour appearance, function bits and per-address cache, all-functions-off, snapshot function merge, local command grace periods |
| 286-366, 2322-2465 | parking brake, door release blink, door blocking timer, derived button colours and glyphs |
| 521-757, 1116-1170 | journey observation, previous/current/next station, arrival/departure/platform texts |
| 394-460, 2586-2628 | system state, currents, voltage, temperature, filtered current and peaks |
| 1210-1310 | host-specific settings (selected locomotive, series, Vmax, ramp) |
| 757-830, 1009-1070, 1486-1560 | construction, EventBus subscriptions, local/remote/hybrid snapshot routing, pause/resume, disposal |

Cross-cutting state the slices must hand over explicitly instead of sharing fields:

- `_isApplyingRuntimeLocomotiveState`, `_isRestoringLocomotive`, `_updatesPaused` (re-entrancy guards);
- `_lastLocalDriveCommandAt` / `_lastLocalFunctionCommandAt` with 3 s grace periods on `DateTimeOffset.UtcNow`;
- four `CancellationTokenSource` fields (drive debounce, ramp, all-functions-off, door blink);
- `GetActiveRuntimeSnapshot()` and the remote/hybrid switches that decide which snapshot is authoritative.

Reusable helpers that stay: `TrainControlDccSpeed`, `TrainControlHost`/`TrainControlHostSettings`,
`TrainControlViewModelOptions`, `IFunctionAppearancePicker`.

Existing tests: `TrainControlViewModelLocomotiveListTests` (8), `TrainControlViewModelPerformanceTests` (4),
`TrainControlViewModelSpeedTests` (2), `TrainControlViewModelCleanupTests` (2), `TrainControlHostSelectionTests`
(1), plus train-control cases in `MauiViewModelControlTabTests` and `MauiViewModelMobaflowOfflineTests`.

## Target collaborators

All live in `SharedUI/TrainControl` (namespace `Moba.SharedUI.TrainControl`), depend only on existing
interfaces and `TimeProvider`, return results or raise plain events and never touch WinUI/MAUI types or
bindable properties. The ViewModel maps their results to its observable properties.

1. `LocomotiveRoster`: project/fleet merge, in-place list updates, picker selection and restore.
2. `SpeedController`: km/h and step conversion, ramp loop, drive debounce and direction-change sequence.
3. `FunctionPanel`: function bits, per-address cache, all-functions-off, snapshot merge with grace periods,
   glyph/colour appearance.
4. `BrakeAndDoorState`: parking brake and door release/blink/blocking state machine and its timers.
5. `JourneyStationProjection`: previous/current/next station and its display texts.
6. `TelemetryProjection`: system-state values, filtered current and peaks with reset.
7. `TrainControlSettingsStore`: host-specific selection, series, Vmax and ramp persistence; the remaining
   coordinator moves to `SharedUI/TrainControl/TrainControlViewModel.cs`.

## Alternatives considered

- Partial files per area: rejected; the shared fields and guards keep the areas coupled.
- One `LocomotiveCommandService` for speed and functions: rejected; the ramp/debounce timing and the
  function cache change for different reasons and need separate time tests.
- Separate WinUI and MAUI ViewModels: rejected; both hosts share the behavior and differ only by explicit
  options.

## Risks

- UI-thread continuations: the ramp and door blink loops await without `ConfigureAwait(false)` because they
  update bindable state. Collaborators stay UI-free; the ViewModel keeps applying results where it does today.
- Timing: grace periods use `DateTimeOffset.UtcNow`, ramps and blinks use `Task.Delay`; collaborators take
  `TimeProvider` so tests control time.
- Snapshot authority (local, remote, hybrid) drives which drive and function state is applied; characterization
  tests cover all three modes before slice 2 and slice 3 move that logic.
- Disposal: collaborators owning `CancellationTokenSource` must be disposed by the ViewModel (CA1001/CA2213).
- Concurrent work: RF-24 (Codex) works on `MainWindowViewModel` and page ViewModels, not on this file at the
  evidence commit; re-check `git worktree list` and recent pushes before each slice.

## Validation per slice

- Characterization tests for the extracted behavior before moving it; collaborator tests with fakes and
  controlled time afterwards; superseded ViewModel tests move to the collaborator.
- Portable and Windows test suites, the WinUI and MAUI DI validators, the Android Release build and the
  cross-platform, Windows and Android analyzer gates.
- No device, Z21 or locomotive movement is needed; manual checks on hardware need separate approval.

## Delivery slices

Each slice is one reversible PR against `main`, in this order:

1. Locomotive roster (target 1).
2. Speed controller (target 2).
3. Function panel (target 3).
4. Brake and door state (target 4).
5. Journey and station projection (target 5).
6. Telemetry projection (target 6).
7. Settings store and move of the coordinator (target 7); delete this plan when #269 closes.
