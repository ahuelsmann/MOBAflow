# RF-25: Explicit dependencies

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/318
**Spec Kit**: Not applicable - structural refactoring without behavior change
**Status**: In progress; workflow execution and validation (slice 1, #325), runtime composition (slice 2, #330), static hooks (slice 3, #335), SharedUI ViewModels (slice 4, #414) and the SharedUI ViewModel remainder (slice 4b, #435) are integrated. Slice 4c (hosts, pages, Backend, speech and display) is in review on branch `claude/issue-318-rf25-slice4c`; see [Slice 4c result](#slice-4c-result). Issue #318 remains open for slice 5.

## Purpose and scope

[Issue #318](https://github.com/ahuelsmann/MOBAflow/issues/318) owns acceptance and live status.
The [umbrella plan](QUALITY-AND-REFACTORING-PLAN.md) owns programme dependencies. This is the single RF-25 child plan.

Target: every constructor shows the services it needs. Hosts register those services, including null objects
where a host has no implementation; tests pass explicit fakes. No `?? new` creates a service, no ViewModel
creates a Backend service or holds the runtime `ActionExecutionContext`, and no mutable static hook configures
behavior. Service lifetimes, runtime ownership, persisted formats and user-visible behavior stay unchanged.

## Source inventory and proposed changes

Counted on main `71830101`; "tests" counts construction sites in `Test/`.

| Area | Hidden dependency today | Tests | Change |
| --- | --- | --- | --- |
| `Backend/Service/WorkflowActionHandlers.cs` (7 handlers) | optional `IWorkflowEffectAdapter`, each falls back to its own `ProductionWorkflowEffectAdapter`; optional loggers | 36 | adapter and logger required; DI registers `ProductionWorkflowEffectAdapter` once |
| `WorkflowService`, `ActionExecutor`, `AnnouncementService` | two convenience `WorkflowService` constructors; `ActionExecutor.CreateWithDefaultHandlers`; optional loggers and event bus; optional `ISpeechOutput` | 25 | one constructor each, everything required; hosts without speech register the existing null speech output, so `IsSpeechOutputAvailable` (always true in both hosts) is removed |
| `WorkflowValidator`, `ProjectValidator` | `?? new WorkflowEffectPlanner()`, `?? new WorkflowValidator()` | 11 | `ProjectValidator` requires `IWorkflowValidator`; `WorkflowValidator` uses the pure planner directly (see design decisions) |
| `MobaRuntimeService` | two constructors; `?? new` for `InPortCounterService`, `JourneyManagerFactory`, `IZ21EndpointSource`; optional event bus, interlocking runtime, time provider | 9 | one constructor; every collaborator except the `IZ21EndpointSource` (defaulted only when the host registers none) required and created by `ProjectRuntimeFactory`/DI; the runtime borrows the counters, which are owned and disposed by DI or `ProjectRuntime` |
| `JourneyManager`, `JourneyManagerDependencies`, `JourneyManagerFactory` | `?? new` for stop transitions, runtime state store, counters, execution coordinator, `ActionExecutionContext`; factory created with `logger: null` | 7 | dependencies record with required members; DI or `ProjectRuntimeFactory` supplies the factory's collaborators, and the factory creates a distinct execution coordinator per `Create`; each manager owns and disposes its coordinator and borrows the counters |
| `PlatformManager` | `?? new ActionExecutionContext { Z21 = z21 }` | 0 | required `ActionExecutionContext` |
| `Common/Discovery/LanIpv4AddressHelper.AugmentAddresses` | mutable static `Action` set by `MOBAsmart/Service/LanIpv4AddressHelperAndroidBootstrap.cs` | - | injected `ILocalIpv4AddressSource` with the network-interface implementation in `Common` and an Android decorator in MOBAsmart; `Z21DiscoveryService` and `RestApiDiscoveryService` receive it |
| `MOBAflow/View/SegmentPlanPathBuilder.ScaleMmToPx` | mutable static set by `TrackPlanPage` to the constant 1.0 | - | scale passed by the caller (or one shared constant); no setter |
| `SharedUI/ViewModel/WorkflowLibraryViewModel` | `?? new NullDialogService()`, `?? new WorkflowValidator()`; `WorkflowLibraryRuntimeServices.ExecutionContext` | 19 | required dialog service and `IWorkflowValidator`; the test-run path receives a narrow workflow-run service instead of the runtime context (slice 4: `IWorkflowDryRunService`) |
| `SolutionViewModel`, `ProjectViewModel`, `WorkflowViewModel`, `JourneyViewModel`, `AudioViewModel`, `CommandViewModel` | optional dispatcher, `IIoService`, sound player, `ILoggerFactory`/logger(s) | 48+ | the creating session passes one required services record (`SolutionViewModelServices`; RF-24 had already introduced it with optional members, slice 4 makes every member required); tests build ViewModels through one helper |
| `SolutionSession` | optional `ISoundPlayer` and `ILoggerFactory` parameters, forwarded to the project ViewModels | many | both required, or replaced by the services record above; hosts register it |
| `ProductionWorkflowEffectAdapter` | optional announcement service, file system (`?? SystemFileSystem.Instance`) and logger | few | all required; DI supplies the system file system |
| `SolutionRemoteLoader`, `MobileRuntimeProjection`, `MobileSolutionSynchronizer`, `MobiStartupService` | optional `IMobileRuntimeCoordinator`, `IUiDispatcher`, `IMobileSolutionStore`, `IProjectContext`, `ISolutionRemoteLoader` | few | required; MOBAsmart registers null objects where it has no implementation |
| `TrainControlSettingsStore`, `LocomotiveRoster`, `FeedbackCounterEngine`, `TrackPlanFeedbackHighlighter`, `PhotosController` | optional logger, project context, time provider, hub context | few | required; `TimeProvider.System` registered by DI |
| `TrainControlViewModel` (found in slice 4) | optional `IProjectContext`, logger, `IUiDispatcher`, `IFunctionAppearancePicker`; `IEventBus eventBus = null!` | 10 | required; MOBAsmart registers `NullFunctionAppearancePicker`; the `IMobileRuntimeCoordinator` fallback (`?? runtimeCommandGateway as IMobileRuntimeCoordinator`) and the value options stay for the remainder |
| `MauiViewModel`, `MobaflowHubSession`, `MobileAppLifecycle` (found in slice 4) | optional `IRuntimeHubRemoteClient`, `IMobileRuntimeCoordinator`, `IProjectContext`, `IBackgroundService`, `IRestApiClientRegistration` with null branches throughout the partials | 4 (+ helpers) | remainder: required with MOBAsmart's existing registrations; the shared hub client is optional in both the session and the ViewModel, so both change together |

Kept as is: value options with documented defaults (`RecorderOptions`, `RecordingArtifactImportLimits`,
`DigitalAddressLimits`, `DisplayIdentifierSequence`/`DisplayPacketSequence` seeds), data defaults such as
`Deserialize(...) ?? new Solution()` and `SelectedProject?.Model ?? new Project()`, event payload defaults
(`RestApiStatusChangedEvent.clients`) and test-only gateways such as `RecordingTurnoutEffectGateway`.

## Design decisions

- **Services versus values.** RF-25 targets services: types with behavior, I/O, time or external effects.
  Immutable option and limit records keep defaults because a default value is not a hidden collaborator.
  Pure rules without state, I/O or time, such as `WorkflowEffectPlanner`, count as values: the action handlers
  and `WorkflowValidator` use them directly. `WorkflowService` still receives `IWorkflowEffectPlanner` because
  runtime tests replace it.
- **Null objects in the host.** When a host lacks a capability (speech, file pickers on Android), it registers
  the existing null object. Constructors never decide that a capability is missing.
- **One construction path.** Convenience constructors are removed rather than marked obsolete
  (no released users). Composition lives in DI registrations and `ProjectRuntimeFactory`.
- **ViewModels stay free of Backend construction.** A ViewModel that needs validation or a test run receives an
  interface (`IWorkflowValidator`, a narrow run service); it neither creates `WorkflowValidator` nor reads
  `ActionExecutionContext`.
- **Test construction.** Changed constructors get one helper per area in `Test/Helpers` (for example handlers
  with a recording effect adapter, ViewModels with a test services record) so call sites stay readable; helpers
  pass explicit fakes and never fall back silently.

## Independently reviewable delivery slices

1. **Workflow execution and validation (Backend).** Action handlers, `WorkflowService`, `ActionExecutor`,
   `AnnouncementService`, `ProductionWorkflowEffectAdapter`, `WorkflowValidator`, `ProjectValidator`; DI registrations in `MobaBackendServiceCollectionExtensions`.
2. **Runtime composition (Backend).** `MobaRuntimeService` with one constructor, `JourneyManager` dependencies,
   `JourneyManagerFactory`, `PlatformManager`, `ProjectRuntimeFactory`; ownership tests for the counter service.
3. **Static hooks.** `ILocalIpv4AddressSource` replaces `LanIpv4AddressHelper.AugmentAddresses` (MOBAsmart registers
   the Android decorator); `SegmentPlanPathBuilder.ScaleMmToPx` loses its setter.
4. **SharedUI ViewModels.** Starts after RF-24 slice 3b (#293), which changes `MainWindowViewModel` and the shared
   test helpers. `WorkflowLibraryViewModel`, the model-wrapper ViewModels, `SolutionSession`, train-control and
   counter services, `PhotosController`, `SolutionRemoteLoader`, `MobileRuntimeProjection`, `MobiStartupService`.
   The remainder of slice 4 is split into two slices:
   - **4b. SharedUI ViewModels.** `MauiViewModel` with `MobaflowHubSession` and `MobileAppLifecycle`, the
     `TrainControlViewModel` coordinator fallback, `MainWindowViewModel`, `LocomotiveManagementViewModel`,
     `SettingsPageViewModel` and their host registrations; see [Slice 4b result](#slice-4b-result).
   - **4c. Hosts, pages, Backend, speech and display.** The items listed as remaining under
     [Slice 4b result](#slice-4b-result); see [Slice 4c result](#slice-4c-result).
5. **DI validation and completion.** WinUI and MAUI container validators resolve every changed registration (including the executable Android DI test lane described under validation);
   update `docs/ARCHITECTURE.md` where it describes construction, replace the RF-25 anchor in the umbrella plan
   with a delivery note and remove this plan. Slice 5 completes #318 only after slices 4b and 4c have migrated the
   slice 4 remainder listed under [Slice 4 result](#slice-4-result); until then the issue stays open.

## Slice 4 result

Delivered against main after RF-24 (#401 journey and city pages, #409 workflows page), which had moved the
page state out of `MainWindowViewModel` and introduced `SolutionViewModelServices` with optional members.
Differences from the inventory above:

- `IWorkflowDryRunService` (Backend) is the narrow run service: `WorkflowDryRunService` plans through
  `IWorkflowService` with a context from `ActionExecutionContextFactory`. `WorkflowLibraryRuntimeServices` is
  gone; `WorkflowLibraryViewModel` requires dialog service, validator, dry-run service, trace store, EventBus and
  logger, and `CanDryRun` depends only on the selection. Only MOBAflow registers the library.
- `SolutionViewModelServices(FilePicker, SoundPlayer, LoggerFactory)` is required everywhere: `SolutionSession`,
  `MobileSolutionContext`, `SolutionViewModel`, `ProjectViewModel`, `WorkflowViewModel`,
  `WorkflowActionViewModelFactory`, `AudioViewModel` (player required; `CanPlayAudio` checks only the file) and
  `CommandViewModel` (logger required). MOBAsmart registers `NullFilePickerService` and the record. The
  `IUiDispatcher` the wrapper chain carried into `JourneyViewModel` was never read and is removed.
- `TrainControlViewModel` requires project context, logger, dispatcher, EventBus and function picker;
  `TrainControlSettingsStore` and `LocomotiveRoster` require logger and project context. The mobile services
  (`SolutionRemoteLoader`, `MobileRuntimeProjection`, `MobileSolutionSynchronizer`, `MobiStartupService`) and
  `TrackPlanFeedbackHighlighter`, `FeedbackCounterEngine`, `PhotosController` lost their optional parameters and
  null branches; MOBAsmart already registered every collaborator.
- Tests: `Test/Helpers/TestViewModelServices`, `TestWorkflowLibraries`, `ImmediateUiDispatcher`;
  `TestSolutionSynchronizers.Create` and `TestRollingStockPages.GetStationsPage` take the test's own fakes.
  `WorkflowDryRunServiceTests` covers the request the editor previously built itself.
- Remainder against the acceptance "no service-typed optional constructor parameter or `?? new` service fallback"
  (verified on this slice's tree; slice 5 must not close #318 while any of these remain, they need their own
  slice before the issue closes; slice 4b delivered the SharedUI items and the two `MobaWinUiServiceCollectionExtensions`
  resolutions, slice 4c owns the rest):
  - SharedUI: `MauiViewModel` and `MobaflowHubSession` optional hub client, coordinator, project context, client
    registration and `MobileAppLifecycle` background service (one change, see the table); the
    `TrainControlViewModel` coordinator fallback (`?? runtimeCommandGateway as IMobileRuntimeCoordinator`);
    `MainWindowViewModel` (`ISettingsService?`, `IDialogService?`; its `IProjectDiagnosticsService?` is removed
    by RF-24 slice 5, PR #416); `LocomotiveManagementViewModel` (`ILocomotivePassportHtmlRenderer?`,
    `IDecoderCvService?`, `IFilePickerService?`, `IProjectContext?`); `SettingsPageViewModel`
    (`IFeatureTogglePageProvider?`, the concrete `AnnouncementService?`, `Func<string, Task>? speechTestAction`).
  - MOBAflow host: `sp.GetService<IDialogService>()` for `MainWindowViewModel` and
    `sp.GetService<IFeatureTogglePageProvider>()` for `SettingsPageViewModel` in
    `MobaWinUiServiceCollectionExtensions`; the page code-behind constructors under `MOBAflow/View`
    (`EventManagerPage`, `GoodsWagonPage`, `HelpPage`, `JourneysPage`, `LocomotivesPage`, `MatrixPage`,
    `MonitorPage`, `PassengerWagonPage`, `SettingsPage` (also `INavigationService?`), `SignalBoxPage`,
    `SolutionPage`, `StationsPage`, `TimetablePage`, `TrackPlanPage`, `TrainControlPage`, `TrainsPage`,
    `WorkflowsPage`) with optional `ISettingsService?` and `ILogger<TPage>?`; `RestApiProcessService`
    (`ISettingsService?`); `CancellableWavePlayer` (`TimeProvider?`).
  - Backend: `UdpWrapper` (`ILogger?`), `InPortCounterService` (`TimeProvider?`, `ILogger?`,
    `IInPortCounterStore?`, `Func<Guid, IInPortCounterStore>?`), `LocomotiveWhistleAutomationService`
    (`TimeProvider?`), `Z21EventPipeline` (`ILogger?`), and the `ActionExecutionContext` registration that still
    resolves speech and sound output with `GetService`.
  - MOBAsmart: `RuntimeHubRemoteClient` (`ILogger?`).
- Out of scope, noted: `FeedbackCounterEngine` has no production caller after RF-24 (tests only).
- Documentation: `docs/ARCHITECTURE.md` (file roles, workflow library composition) corrected; `docs/wiki/`
  unchanged because no user-visible behavior, setting or screen changed.

## Slice 4b result

Delivered against main `9c451c8a` after #424, #425 and #433; RF-16 (#432) touches none of these files.
Service lifetimes and user-visible behavior are unchanged.

- `MauiViewModel`, `MobaflowHubSession` and `MobileAppLifecycle` require the runtime hub client, the mobile
  coordinator, the project context, the client registration and the background service; their null branches are
  gone. MOBAsmart already registered every collaborator.
- `TrainControlViewModel` requires an `IRuntimeRoutingState` instead of the optional coordinator and the
  `?? runtimeCommandGateway as IMobileRuntimeCoordinator` fallback. The new interface holds the read-only part of
  `IMobileRuntimeCoordinator` (`PreferRemoteRuntime`, `CanExecuteCommands`, `IsLocalZ21Connected`), which extends it.
  MOBAsmart registers its coordinator behind it, MOBAflow registers `NullRuntimeRoutingState`, which answers `false`
  to every member: without a coordinator, callers use their own connection state. For `PreferRemoteRuntime` and
  `CanExecuteCommands` this equals the old null path (`?.PreferRemoteRuntime == true`, `?.CanExecuteCommands ?? false`).
  One difference remains: in hybrid mode the old code fell back to the ViewModel's own `IsConnected` for
  `IsLocalZ21Connected` (`?? IsConnected`), the null object returns `false`. Neither host configures hybrid mode
  without a coordinator: MOBAflow's options are not hybrid, and MOBAsmart always passes its coordinator.
- `MainWindowViewModel` requires `ISettingsService` and `IDialogService`; MOBAflow resolves the dialog service with
  `GetRequiredService`, and project deletion and the stop-trains confirmation no longer handle a missing dialog.
- `LocomotiveManagementViewModel` requires passport renderer, CV service, file picker and project context, which
  MOBAflow's container already supplied.
- `SettingsPageViewModel` requires `IFeatureTogglePageProvider` and `ISpeechOutput`. The speech test speaks through
  the output and shows its failure reason, as the host's former `Func<string, Task>` did; the never-used
  `AnnouncementService` path is removed, so the ViewModel no longer references a Backend service type. MOBAflow
  resolves the page provider with `GetRequiredService`.
- Tests: `TestHubSessions.Create` requires hub client and registration; the MAUI ViewModel factories pass one hub
  client, coordinator and project context to both the ViewModel and its collaborators;
  `TestRollingStockPages.CreateManagement` and `TestSettingsPages.NoFeatureToggles` are new; shell tests pass
  `NullDialogService` and a settings mock; train-control tests pass `NullRuntimeRoutingState` outside hybrid mode
  and a `MobileRuntimeCoordinator` in hybrid mode, as the hosts do. The speech test covers success, failure result
  and exception.
  The WinUI container test resolves routing state, shell, train control, locomotive management and the settings
  page with its toggles; the MAUI container test checks that the routing state is the coordinator.
- Remaining for slice 4c, verified on this branch (items marked new were not in the slice 4 list):
  - MOBAflow host: the page code-behind constructors under `MOBAflow/View` listed under
    [Slice 4 result](#slice-4-result) and their `GetService` resolutions in `NavigationRegistration`;
    `RestApiProcessService` (`ISettingsService?`); `RuntimeHubHostClient` (`ILogger?`, new);
    `CancellableWavePlayer` (`TimeProvider?`) and the `AudioCache` registration's
    `GetService<TimeProvider>() ?? TimeProvider.System` (new). The page constructors also carry the layout
    persistence that RF-24 slice 6 centralizes; align the page part with that slice before starting it.
  - Backend: `UdpWrapper`, `InPortCounterService`, `LocomotiveWhistleAutomationService`, `Z21EventPipeline`,
    `Z21` (`ILogger<Z21>?`, `Z21Monitor?`, new) and the `ActionExecutionContext` registration that resolves speech
    and sound output with `GetService`.
  - MOBAsmart: `RuntimeHubRemoteClient` (`ILogger?`).
  - Speech and display (new): `PiperSpeechProvider` (`?? new PiperProcessRunner()`), `PiperProcessRunner` and
    `DisplayProtocolClient` (`TimeProvider?`).
  - Optional container lookups with a fallback branch (new):
    - `SharedUI/Extensions/EventBusUiExtensions.cs`: `GetService<IRecordingSessionService>()` and
      `GetService<RecordingEventMapperRegistry>()` silently decide whether `IEventBus` gets the recording decorator.
    - `MOBAflow/Service/WinUiAppStartupService.cs`: `GetService<ISettingsService>()` with a "skipping auto-load"
      branch, although MOBAflow always registers the service.
    - `MOBAsmart/App.xaml.cs`: `GetService<MauiViewModel>()` with null branches in `OnResume` and
      `CleanupOnWindowDestroyingAsync`, although the ViewModel is always registered.
    - `MOBAsmart/Platforms/Android/MainActivity.cs`: `OnDestroy` asks for `MainWindowViewModel`, which MOBAsmart never
      registers, so the block never runs; MOBAsmart's actual disconnect on close is `App.xaml.cs`
      `CleanupOnWindowDestroyingAsync` (`Window.Destroying`). Remove the dead block or route it through
      `MauiViewModel`.
  - To classify as value or service (new): optional delegates with a production default
    (`BrakeAndDoorState` random source, `CancellableWavePlayer` session factory, `SystemSpeechProvider` default
    voice, `ForwardingEventBus` delivery, the `ProjectZ21EndpointSource` solution lookup in
    `Backend/Service/Z21EndpointSources.cs`) and the optional `RecordingArtifactSerializer` payload validators.
- Documentation: `docs/ARCHITECTURE.md` (settings presentation, train control routing) corrected; `docs/wiki/`
  unchanged because no user-visible behavior, setting or screen changed.

## Slice 4c result

Delivered against main `9dcba691` after RF-24 slice 6 (#442), which had already moved layout persistence out of
the page constructors. Service lifetimes and user-visible behavior are unchanged.

- MOBAflow pages: the 17 page constructors listed under [Slice 4 result](#slice-4-result) require their
  `ILogger<TPage>`; `SignalBoxPage` also requires the canvas logger and `TrainControlPage` its `ISettingsService`.
  `NavigationRegistration` resolves the loggers with `GetRequiredService`. The `JourneysPage` factory had passed
  no logger, so that page now logs a failed layout save on unload like every other page. `SettingsPage` loses the
  `INavigationService` it never read. Pages are created by XAML and cannot be resolved in a headless test; the
  FastDebug build and the Windows analyzer gate compile them.
- MOBAflow services: `RuntimeHubHostClient`, `PhotoHubClient` (found here) and `RestApiStatusService` (found here)
  require their logger or clock. `RestApiProcessService` loses the `ISettingsService` it never read.
  `WinUiAppStartupService` resolves `ISettingsService` with `GetRequiredService`; the "skipping auto-load" branch
  is gone.
- Speech: `PiperSpeechProvider` requires `IPiperProcessRunner`, `PiperProcessRunner` its clock, and MOBAflow
  registers the runner. The `AudioCache` registration resolves the clock with `GetRequiredService`.
- Display: `DisplayProtocolClient` requires its clock. `UdpDisplayDeviceClient` and `UdpDisplayFrameSender` create
  the protocol client themselves, so their public constructors take the clock and DI supplies it.
- Backend: `UdpWrapper`, `Z21EventPipeline` and `Z21` require their logger; `Z21` requires its `Z21Monitor`, so
  `Z21.TrafficMonitor` is never null (`IZ21.TrafficMonitor` stays nullable for other implementations).
  `LocomotiveWhistleAutomationService` requires its clock. `InPortCounterService` requires clock, logger and an
  `InPortCounterPersistence` that states where counts are kept: `InMemory`, `InStore(store)` (one counter service
  per project runtime, MOBAflow) or `PerProject(factory)` (one counter service for all projects, MOBAsmart). It
  replaces the two optional store parameters; `ProjectRuntimeServices.CounterPersistence` is required and replaces
  the optional `CounterStoreFactory`. The shared registration defaults to `InMemory` with `TryAdd`. The
  `ActionExecutionContext` registration resolves `ISpeechOutput` and `ISoundPlayer` with `GetRequiredService`;
  MOBAflow now registers `WindowsSoundPlayer` with its speech services so the backend group resolves without the
  shell group.
- MOBAsmart: `RuntimeHubRemoteClient` requires its logger. `App.xaml.cs` resolves `MauiViewModel` with
  `GetRequiredService`. The dead `MainActivity.OnDestroy` block and its allowlist entry in
  `SolutionSessionArchitectureTests` are removed; `Window.Destroying` remains the disconnect on close.
- `AddEventBusWithUiDispatch` always registers the `RecordingEventBusDecorator` and requires the recording
  session and mapper registry. Both hosts register the backend services, so their `IEventBus` is unchanged.
- Optional delegates, classified by the rule under [Design decisions](#design-decisions):
  - Service, now required: the `CancellableWavePlayer` session factory opens WinMM playback (MOBAflow registers
    `CancellableWavePlayer.CreateNativeSession`); the `SystemSpeechProvider` default voice reads the registry
    (MOBAflow registers `SystemSpeechProvider.ReadWindowsDefaultVoice`).
  - Value, kept: the `BrakeAndDoorState` random source (`Random.Shared.Next`, a pure function of a state object
    the ViewModel creates); the `ForwardingEventBus` delivery (inline invocation, no I/O); the
    `ProjectZ21EndpointSource` solution lookup (data access, absent means no cross-project ownership check and
    nothing is created instead); the `RecordingArtifactSerializer` payload validators (a set that may be empty,
    DI always passes the registered set) and import limits (limit record).
- `SystemStatusViewModel` (found here) requires its clock.
- Tests: `TestRuntimes.Counters` and `TestRuntimes.CreateZ21` build counters and Z21 with explicit clock, logger,
  persistence and monitor; other construction sites pass `TimeProvider.System` or their fake.
  `MobaBackendServiceCollectionExtensionsTests` resolves Z21 with its monitor, the counters, the whistle service
  and the action context, and shows that a missing sound player fails the resolution.
  `MobaWinUiServiceCollectionExtensionsTests` resolves the speech, hub, REST, status and display services and
  expects the recording decorator as `IEventBus`.
- Analyzer baselines: one Windows entry dropped by one (`CA1848` in `WinUiAppStartupService.cs`, removed branch);
  the Android entries of `MainActivity.cs` are removed with its dead block.
- Remaining for slice 5, verified on this branch:
  - The DI validators and the executable MAUI container lane described under
    [Validation strategy](#validation-strategy); MOBAsmart registrations of this slice are verified by build only.
  - `MOBAflow/ViewModel/MatrixPageViewModel.cs` keeps a parameterless constructor next to the DI constructor and
    therefore nullable `ISolutionSession` and logger fields (found here).
  - `MobaRuntimeServiceDependencies.EndpointSource` keeps its documented default (slice 2): the host's
    `IZ21EndpointSource` is looked up with `GetService` and the runtime falls back to its active project.
  - `SignalBoxCanvasControl` keeps a nullable logger field until `SignalBoxPage` attaches the logger, because XAML
    creates the control.
- Documentation: `docs/ARCHITECTURE.md` (host audio outputs, EventBus registration, system status clock,
  solution-session allowlist) and `docs/PROJECT-REFERENCE.md` (counter storage) corrected; `docs/wiki/`
  unchanged because no user-visible behavior, setting or screen changed.
## Validation strategy

Every slice: portable and Windows test suites, the cross-platform, Windows and Android analyzer gates, the
affected host builds and the DI container validation tests (`MobaWinUiServiceCollectionExtensionsTests`,
`MobaBackendServiceCollectionExtensionsTests`, and `MobaMauiServiceCollectionExtensionsTests` with
`MobiDiContainerValidator`). The MAUI fixture is excluded from the default test graph (`IncludeMobaSmartTests=false`,
`SKIP_ANDROID_TESTS`) and no workflow runs it today. Slice 5 must therefore either add a CI job that builds `Test` with
`IncludeMobaSmartTests=true` or move the validator contract into a portable boundary, and must show that the fixture
is discovered and executed; until then slices that change MAUI registrations verify them with a MOBAsmart build. Slice 2 additionally keeps
the runtime ownership and per-project isolation tests unchanged; slice 3 covers the Android address source with a
portable test of the decorator contract. No app launch, device deployment or hardware action by the agent.

## Risks

- **Test churn.** About 150 construction sites in tests change; helpers keep the diff mechanical and reviewable.
- **Ownership.** `MobaRuntimeService` borrows the counter service and never disposes it. DI or `ProjectRuntime`
  owns and disposes it; retain exactly one owner and a distinct manager-owned coordinator per factory `Create`.
- **Open PRs.** #293 (RF-24) and other Codex branches touch SharedUI ViewModels and test helpers; slice 4 waits,
  and each slice re-checks open PRs for overlapping files before it starts.
- **Analyzer baselines:** moved or deleted diagnostics travel with their file; new test helpers need no new
  baseline entries beyond the usual CA1812 for fixtures.
