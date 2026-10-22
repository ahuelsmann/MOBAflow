# RF-25: Explicit dependencies

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/318
**Spec Kit**: Not applicable - structural refactoring without behavior change
**Status**: Planned; implementation starts with slice 1 after this plan is integrated

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
| `MobaRuntimeService` | two constructors; `?? new` for `InPortCounterService`, `JourneyManagerFactory`, `IZ21EndpointSource`; optional event bus, interlocking runtime, time provider | 9 | one constructor; every collaborator required and created by `ProjectRuntimeFactory`/DI; `_ownsInPortCounters` follows the factory's ownership instead of the fallback |
| `JourneyManager`, `JourneyManagerDependencies`, `JourneyManagerFactory` | `?? new` for stop transitions, runtime state store, counters, execution coordinator, `ActionExecutionContext`; factory created with `logger: null` | 7 | dependencies record with required members; factory receives them from DI |
| `PlatformManager` | `?? new ActionExecutionContext { Z21 = z21 }` | 0 | required `ActionExecutionContextFactory` |
| `Common/Discovery/LanIpv4AddressHelper.AugmentAddresses` | mutable static `Action` set by `MOBAsmart/Service/LanIpv4AddressHelperAndroidBootstrap.cs` | - | injected `ILocalIpv4AddressSource` with the network-interface implementation in `Common` and an Android decorator in MOBAsmart; `Z21DiscoveryService` and `RestApiDiscoveryService` receive it |
| `MOBAflow/View/SegmentPlanPathBuilder.ScaleMmToPx` | mutable static set by `TrackPlanPage` to the constant 1.0 | - | scale passed by the caller (or one shared constant); no setter |
| `SharedUI/ViewModel/WorkflowLibraryViewModel` | `?? new NullDialogService()`, `?? new WorkflowValidator()`; `WorkflowLibraryRuntimeServices.ExecutionContext` | 19 | required dialog service and `IWorkflowValidator`; the test-run path receives a narrow workflow-run service instead of the runtime context |
| `SolutionViewModel`, `ProjectViewModel`, `WorkflowViewModel`, `JourneyViewModel`, `AudioViewModel`, `CommandViewModel` | optional dispatcher, `IIoService`, sound player, `ILoggerFactory`/logger(s) | 48+ | the creating session passes one required services record (`SolutionViewModelServices`, a new type that slice 4 of this plan creates; RF-25 does not depend on RF-29 for it); tests build ViewModels through one helper |
| `SolutionSession` | optional `ISoundPlayer` and `ILoggerFactory` parameters, forwarded to the project ViewModels | many | both required, or replaced by the services record above; hosts register it |
| `ProductionWorkflowEffectAdapter` | optional announcement service, file system (`?? SystemFileSystem.Instance`) and logger | few | all required; DI supplies the system file system |
| `SolutionRemoteLoader`, `MobileRuntimeProjection`, `MobiStartupService` | optional `IMobileRuntimeCoordinator`, `IUiDispatcher`, `IMobileSolutionStore`, `IProjectContext`, `ISolutionRemoteLoader` | few | required; MOBAsmart registers null objects where it has no implementation |
| `TrainControlSettingsStore`, `LocomotiveRoster`, `FeedbackCounterEngine`, `PhotosController` | optional logger, project context, time provider, hub context | few | required; `TimeProvider.System` registered by DI |

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
5. **DI validation and completion.** WinUI and MAUI container validators resolve every changed registration (including the executable Android DI test lane described under validation);
   update `docs/ARCHITECTURE.md` where it describes construction, replace the RF-25 anchor in the umbrella plan
   with a delivery note and remove this plan.

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
- **Ownership.** `MobaRuntimeService` currently disposes the counter service only when it created it; the factory
  must keep exactly one owner.
- **Open PRs.** #293 (RF-24) and other Codex branches touch SharedUI ViewModels and test helpers; slice 4 waits,
  and each slice re-checks open PRs for overlapping files before it starts.
- **Analyzer baselines:** moved or deleted diagnostics travel with their file; new test helpers need no new
  baseline entries beyond the usual CA1812 for fixtures.
