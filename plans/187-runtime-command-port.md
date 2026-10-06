# RF-22 One Runtime Command Port

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/187
**Spec Kit**: Not applicable - behavior-preserving refactoring governed by the RF-22 anchor in plans/QUALITY-AND-REFACTORING-PLAN.md

## Status

- Parent programme: [#47](https://github.com/ahuelsmann/MOBAflow/issues/47), package RF-22
- Follow-up fix: [#188](https://github.com/ahuelsmann/MOBAflow/issues/188) records the newly routed commands
- Baseline commit: `90b0b342`
- Branch: `codex/issue-187-runtime-command-port`

## Purpose and scope

ViewModels send every operator command through `IRuntimeCommandGateway` and read
runtime state through narrow runtime roles. Hosts register the local, recording
and remote gateways; ViewModels never create one.

Committed scope:

1. Route the commands that bypass the port today through `IRuntimeCommandGateway`.
2. Make the gateway a required ViewModel dependency and remove the local fallbacks.
3. Replace `IMobaRuntime` in ViewModels with the roles they use; move the
   layout commands out of the role that ViewModels keep.
4. Document the command path and guard it with an architecture test.

Out of scope: recording the newly routed commands (#188), new MOBApi operations,
non-ViewModel services such as `SolutionRemoteLoader` and
`LocomotiveWhistleAutomationService` (RF-12, RF-25), and splitting the large
ViewModels (RF-24).

## Technical context (baseline `90b0b342`)

- `IRuntimeCommandGateway` (SharedUI) has ten commands. Implementations:
  `LocalRuntimeCommandGateway`, `RecordingRuntimeCommandGateway` (decorator),
  `MobileRuntimeCoordinator` (MOBAsmart local/remote routing) and
  `NoOpRuntimeCommandGateway`. MOBAflow and MOBAsmart register the recording
  gateway as `IRuntimeCommandGateway`.
- `IMobaRuntime` (Backend) aggregates `IRuntimeSnapshotProvider`,
  `IConnectionRuntime`, `ILocomotiveRuntime`, `ISignalTurnoutRuntime` and
  `ITrafficMonitor`. Only the aggregate and `IRuntimeSnapshotProvider` are used
  anywhere; its comment calls it a "backward-compatible aggregate facade".
- Commands that bypass the gateway in ViewModels:

  | Call site | Command |
  | --- | --- |
  | `TrainControlViewModel` | `SetAllLocomotiveFunctionsOffAsync`, `RequestLocomotiveInfoAsync` |
  | `MainWindowViewModel.OperatingState` | `AcknowledgeFailSafeAsync` |
  | `MainWindowViewModel.Signals` | `SetSignalAspectAsync(SbSignal)` (signal-box editor) |
  | `MauiViewModel` | `SetTrackPowerAsync`, `ResetInPortCountersAsync` fallbacks when no gateway is injected |

- Local gateway fallbacks: `MainWindowViewModel` and `TrainControlViewModel`
  constructors, `MauiViewModel` (`InPortStatistic`, signal box) and
  `InPortStatistic(IRuntimeCommandGateway? commands = null)`.
- Lifecycle calls in ViewModels (`StartAsync`, `ActivateProjectAsync`,
  `UpdateJourneyEventsAsync`, `ConnectAsync`, `DisconnectAsync`,
  `RequestSystemStateAsync`) configure the local runtime; they are never
  recorded or routed remotely and stay on the runtime.
- The `SbSignal` overload uses the editor's signal configuration, while the
  `Guid` overload uses the runtime's deep copy of the last activated project.
  A signal that was just added or edited in the editor is therefore unknown or
  stale for the `Guid` path. The gateway gets an `SbSignal` overload so the
  editor behavior stays identical.
- Tests: 24 ViewModel constructions in 18 test files; 55 test files reference
  `IMobaRuntime`, mostly through `Mock<IMobaRuntime>`, which implements every role.

## Design decisions

- **Port**: keep `IRuntimeCommandGateway` (maintainer decision on the anchor).
  Add `SetAllLocomotiveFunctionsOffAsync`, `RequestLocomotiveInfoAsync`,
  `AcknowledgeFailSafeAsync` and `SetSignalAspectAsync(SbSignal)`.
- **Routing of the added commands** stays as today: the local gateway calls the
  runtime; `MobileRuntimeCoordinator` sends them to the local runtime
  (`SbSignal`: remote by id and aspect when a MOBAflow session is active, like
  the `Guid` overload); the recording gateway passes them through **without**
  recording until #188.
- **Roles** (maintainer decision 2026-10-06: narrow roles):
  - `IConnectionRuntime` keeps lifecycle and connection only: `StartAsync`,
    `ActivateProjectAsync`, `UpdateJourneyEventsAsync`, `ConnectAsync`,
    `DisconnectAsync`, `RequestSystemStateAsync`.
  - `ISignalTurnoutRuntime` becomes `ILayoutControlRuntime` and takes
    `SetTrackPowerAsync` and `AcknowledgeFailSafeAsync` from
    `IConnectionRuntime`.
  - `ILocomotiveRuntime`, `IRuntimeSnapshotProvider` and `ITrafficMonitor` stay.
  - `IMobaRuntime` stays as the complete runtime surface for hosts, gateway
    implementations and runtime services; its comment no longer calls it a
    compatibility facade.
  - DI forwards each role to the single runtime instance.
- **ViewModels** take only the roles they use plus a required
  `IRuntimeCommandGateway`; `InPortStatistic` requires its gateway.
- **Guard**: an architecture test fails when a type in `Moba.SharedUI.ViewModel`
  has a constructor parameter or field of type `IMobaRuntime`,
  `ILocomotiveRuntime` or `ILayoutControlRuntime`, or creates a gateway.

Alternatives considered: keeping `IMobaRuntime` in ViewModels with a convention
test (rejected by the maintainer: the compiler should prevent bypasses); a new
ViewModel-specific aggregate interface (rejected: it would recreate a facade).

## Compatibility, security and telemetry

No persisted format, API endpoint or setting changes. MOBApi is untouched.
Recorder output stays identical until #188. Logging and telemetry do not change.

## Risks, stop conditions and rollback

- Wrong routing in MOBAsmart: covered by `MobileRuntimeCoordinator` tests for
  every added command.
- DI resolution gaps: the WinUI and MAUI container validators resolve every role
  and the gateway; both run in tests.
- Stop condition: any change to recorder output or MOBAsmart routing beyond
  this plan. Rollback: revert the slice PR; slices are independent.

## Automated tests

```text
dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0
dotnet test Test/Test.csproj -f net10.0-windows10.0.22621.0 -p:IncludeMobaSmartTests=false
dotnet build MOBAsmart/MOBAsmart.csproj -f net10.0-android -c FastDebug --no-restore
```

Plus the cross-platform, Windows and Android analyzer gates from full rebuilds.
New tests: gateway pass-through for the added commands (local, recording without
entries, mobile routing, no-op) and the architecture guard. Existing recording and
remote routing tests must pass unchanged.

## Manual and hardware acceptance

Not applicable for merging: the change is internal routing with unchanged
behavior and is covered by unit and container tests. Starting the apps or
hardware actions are not authorized by this plan.

## Gates and delivery slices

Each slice is a draft PR with secrets scan, line-ending check, green CI and
SonarCloud with zero open issues before review.

1. **Port completion**: add the four commands to the gateway and all
   implementations; route the bypassing ViewModel calls; remove the
   `MauiViewModel` fallbacks.
2. **Roles and required gateway**: introduce `ILayoutControlRuntime`, narrow
   ViewModel dependencies, make the gateway required, remove the
   `new LocalRuntimeCommandGateway(...)` fallbacks, update tests and DI.
3. **Guard and documentation**: architecture test, `docs/ARCHITECTURE.md`
   command path, `IMobaRuntime` comment; delete this plan when #187 closes.
