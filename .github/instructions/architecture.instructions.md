---
description: 'Runtime ownership, threading boundaries and source locations.'
applyTo: '**/*.cs'
---

# MOBAflow architecture

Layer constraints and the project map are in [AGENTS.md](../../AGENTS.md).
Use [docs/ARCHITECTURE.md](../../docs/ARCHITECTURE.md) for broader context and verify details in these entry points.

## Runtime and events

- [IMobaRuntime](../../Backend/Interface/IMobaRuntime.cs) combines connection, locomotive, signal/journey and
  snapshot roles. Its snapshot property is `Current` via `IRuntimeSnapshotProvider`.
- [MobaRuntimeService](../../Backend/Service/MobaRuntimeService.cs) owns the active project execution context.
  Its partials separate `RuntimeApi`, `Z21Handlers` and `AutoConnect`.
  [MobaRuntimeSnapshotBuilder](../../Backend/Service/MobaRuntimeSnapshotBuilder.cs) and
  [MobaRuntimeStatusFormatter](../../Backend/Service/MobaRuntimeStatusFormatter.cs) hold extracted helpers.
- Treat [MobaRuntimeSnapshot](../../Common/Runtime/MobaRuntimeSnapshot.cs) as read-only query state; route mutations
  through runtime commands. Keep editable project models separate from active runtime execution state.
- Shared ViewModels depend on this runtime boundary. Preserve
  [IRuntimeCommandGateway](../../SharedUI/Interface/IRuntimeCommandGateway.cs) where commands can target local or
  remote runtimes, and [MobileRuntimeCoordinator](../../SharedUI/Service/MobileRuntimeCoordinator.cs) routing.
- Z21 raw callbacks reach the runtime in the background; UI EventBus subscriptions go through
  [UiThreadEventBusDecorator](../../SharedUI/Service/UiThreadEventBusDecorator.cs).
  Its `Publish` queues through `InvokeOnUiLowPriority`; subscribers need no second dispatch.
  This guarantee does not cover arbitrary .NET events or a bare EventBus in backend tests.
  Preserve the bounded FIFO publication in [Z21EventPipeline](../../Backend/Service/Z21EventPipeline.cs).

## DI and lifecycle

- [Backend registrations](../../Backend/Extensions/MobaBackendServiceCollectionExtensions.cs):
  `AddMobaBackendServices` owns shared services, including `IZ21`, `IMobaRuntime`, workflow handlers and `MasterDataStore`.
- [WinUI registrations](../../MOBAflow/Extensions/MobaWinUiServiceCollectionExtensions.cs) and
  [MAUI registrations](../../MOBAsmart/Extensions/MobaMauiServiceCollectionExtensions.cs) compose platform services,
  dispatchers, ViewModels and pages. Extend these rather than duplicating registrations in startup files.
- [EventBus registration](../../SharedUI/Extensions/EventBusUiExtensions.cs) registers the inner bus and decorated
  `IEventBus`; provide the UI dispatcher first. Do not replace the decorated registration with a bare bus later.
- Preserve singleton shared state and transient page lifetimes. Unsubscribe transient views from singleton events
  on unload and resubscribe on load. Preserve disposal of owned subscriptions, timers and cancellation sources.

## Data and workflows

- [MasterDataStore](../../Backend/Data/MasterDataStore.cs) owns shipped `data.json` master data.
- [WorkflowService](../../Backend/Service/WorkflowService.cs) provides Workflow 2.0 execution with validation,
  effect planning, conditions and traces, using `IActionExecutor` and handlers in `Backend/Manager/`.
  Inspect its request/result contracts and partials before changing execution. The legacy overload taking
  `WorkflowExecutionOptions` is a compatibility adapter; do not assume old failure options still control execution.
- Keep configuration in `Common/Configuration/`. Reuse `Common/Path/PhotoPathHelper.cs` for photo paths and
  `Common/Discovery/DiscoveryResponseParser.cs` for `MOBAFLOW_REST_API|ip|port` discovery messages.
- Platform-neutral UI state can live in `Common`, such as `Common/Display/LedMatrix5x5State.cs`;
  adapt its ARGB values to brushes in the platform layer.

## Regression starting points

- Paths/discovery/defaults: `Test/Common/PhotoPathHelperTests.cs`, `DiscoveryResponseParserTests.cs`, `AppSettingsDefaultsTests.cs`.
- Runtime isolation/safety: `Test/Backend/MobaRuntimeServiceProjectIsolationTests.cs`, `MobaRuntimeServiceTrackPowerTests.cs`.
- Workflows/DI: `Test/Backend/WorkflowServiceTests.cs`, `MobaBackendServiceCollectionExtensionsTests.cs`.
- Remote routing/projection: `Test/SharedUI/MobileRuntimeCoordinatorTests.cs`, `RuntimeSnapshotProjectorTests.cs`.

Select the tests that cover the changed contract; this list is not a requirement to run all fixtures for every edit.
