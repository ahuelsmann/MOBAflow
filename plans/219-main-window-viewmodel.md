# RF-24: Decompose MainWindowViewModel

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/219
**Spec Kit**: Not applicable - behavior-preserving decomposition; no product or persisted-format change
**Status**: Active; slices 1, 2a, 2b, 2c, 3a, 3b and 4 integrated through #248, #257, #264, #272, #282, #401 and #409; slice 5 (counters, diagnostics, health and synchronization status) is planned below as two sequential PRs; slice 6 is not allocated
**Evidence**: Settings integrated at bfe9b91d; locomotives at 656bc630; wagons at c4e427d0; train composition at f6ad0853; stations/platforms at 983524e847df1a36a2f91f9ef99041f54249d895 on 2026-10-13; journeys/cities at 1f8c77fd; workflows at 0e6ef5f1 on 2026-11-05. Slice 5 planning resolves against main a2b36f11 on 2026-11-05.

## Purpose and scope

[Issue #219](https://github.com/ahuelsmann/MOBAflow/issues/219) owns acceptance and live status.
The [umbrella plan](QUALITY-AND-REFACTORING-PLAN.md) owns programme dependencies.
This is the single RF-24 child plan, transferring the umbrella's extraction order and acceptance anchor.
RF-23 is integrated; the maintainer authorized implementation on 2026-10-11, starting with Settings.

The outcome is a shell responsible for navigation and shell state, with focused page ViewModels
and one layout persistence service. Preserve commands, bindings, defaults, model-wrapper notifications,
selection, auto-save and local/remote runtime routing. Remove superseded partials rather than moving them.

No runtime ownership, Z21 lifecycle, API routing, format migration, speech-provider implementation,
new runtime command, dynamic plugin or later RF package is included. New user-visible behavior or
a persisted-format change requires reclassification and Spec Kit before implementation.

## Start gate and dependency evidence

RF-23/#191 closed after #239 was integrated at `eb4ebecd414c978324e3b7005b8691fcc2c802b4`.
The [acceptance record](../specs/008-project-runtimes/acceptance.md) maps integrated slices,
fake-based tests and Windows/Android evidence; app/device/hardware checks are recorded separately.
The maintainer authorized the Settings slice on 2026-10-11. It starts from integrated main.

### Resolved acceptance boundary

#191 and FR-001/SC-004 now agree with
[SolutionSessionArchitectureTests](../Test/Architecture/SolutionSessionArchitectureTests.cs):
existing shell/composition, WinUI page bindings/navigation, grid-column persistence,
MonitorPageViewModel and Android shutdown lookup remain named residuals; 3b removes the EventManagerViewModel shell dependency.
PostStartupInitializationService already uses the session and is not a shell exception.
SettingsPage, LocomotivesPage, PassengerWagonPage, GoodsWagonPage, TrainsPage, StationsPage, JourneysPage and WorkflowsPage reject shell dependencies even while other pages remain allowed.
The remaining core retains selected-project projections, operating state and shutdown;
ISolutionSession owns the solution and canonical selection, and operator commands stay on
IRuntimeCommandGateway. Settings extraction changes neither boundary.
#136 was refreshed before extraction; this slice retains its existing SAPI/Piper integration
without provider implementation or project changes.

## Source inventory and proposed owners

The source links below reflect the refreshed integrated slice; counts describe the original inventory. Line counts are file-size
observations, not coverage or quality metrics. The Settings, rolling-stock and journey slices remove their superseded shell partials. Stock editing and train composition belong to focused page ViewModels; the shell retains narrow uploaded-photo routing and runtime snapshot projection for the later status slice.

| Responsibility and evidence | Proposed boundary | Existing behavior to preserve |
| --- | --- | --- |
| [SettingsPageViewModel](../SharedUI/ViewModel/SettingsPageViewModel.cs), extracted from the settings partial; [SettingsPage](../MOBAflow/View/SettingsPage.xaml.cs):24-36 and its layout partial | Focused SettingsPageViewModel using AppSettings, ISettingsService and existing speech-test integration | Setter equality checks, notifications, immediate persistence, reset, feature availability, Piper voices, speech success/failure |
| [LocomotivesPageViewModel](../SharedUI/ViewModel/LocomotivesPageViewModel.cs), extracted locomotive editor; focused [WagonsPageViewModel](../SharedUI/ViewModel/WagonsPageViewModel.cs) and [TrainsPageViewModel](../SharedUI/ViewModel/TrainsPageViewModel.cs) (originally 482 and 264 lines) | Focused locomotive, passenger/goods wagon and train page state; reuse LocomotiveManagementViewModel | CRUD, filtered lists, rename selection identity, composition ordering and photo assignment to the initiating context |
| [StationsPageViewModel](../SharedUI/ViewModel/StationsPageViewModel.cs), extracted station/platform editor; [JourneysPageViewModel](../SharedUI/ViewModel/JourneysPageViewModel.cs), extracted journey/city editor | Focused page ViewModels using the session's canonical selection | Shared journey selection, stations/platforms, city library, command availability, nested model change tracking |
| [WorkflowsPageViewModel](../SharedUI/ViewModel/WorkflowsPageViewModel.cs), extracted from the workflow partial (originally 187 lines); [WorkflowsPage](../MOBAflow/View/WorkflowsPage.xaml):32 onwards | Inject the existing host-registered WorkflowLibraryViewModel; add only page presentation state that it lacks | Selection, action editing/order, validation, dry-run cancellation, trace and editor state; no second workflow engine |
| [Counter](../SharedUI/ViewModel/MainWindowViewModel.Counter.cs), [Diagnostics](../SharedUI/ViewModel/MainWindowViewModel.Diagnostics.cs), [HealthStatus](../SharedUI/ViewModel/MainWindowViewModel.HealthStatus.cs), [RestApiStatus](../SharedUI/ViewModel/MainWindowViewModel.RestApiStatus.cs) and [SyncDiagnostics](../SharedUI/ViewModel/MainWindowViewModel.SyncDiagnostics.cs) partials (216, 141, 78, 94 and 194 lines), the traffic region of the [Z21 partial](../SharedUI/ViewModel/MainWindowViewModel.Z21.cs) and [MonitorPageViewModel](../SharedUI/ViewModel/MonitorPageViewModel.cs) | InPortCounterViewModel, a reworked MonitorPageViewModel, ProjectDiagnosticsViewModel and SystemStatusViewModel using the existing runtime query contracts and EventBus; see the slice 5 plan below | Counter errors and reset result, filters, health/API status, sync state and project isolation |
| [LayoutPanels](../SharedUI/ViewModel/MainWindowViewModel.LayoutPanels.cs), 337 lines; [SettingsPageViewModel.Layout](../SharedUI/ViewModel/SettingsPageViewModel.Layout.cs), extracted from the settings layout partial; [GridColumnResizeBehavior](../MOBAflow/Behavior/GridColumnResizeBehavior.cs):474 onwards | One layout persistence service; reuse LayoutColumnWidthsViewModel | Panel expansion, section usage/order, setting keys, star-column values and intentionally fixed pixels |

[MainWindowViewModel.cs](../SharedUI/ViewModel/MainWindowViewModel.cs) subscribes to session/runtime/status/photo
events and disposes the host-owned WorkflowLibraryViewModel at shutdown through the station editor. It also owns project
CRUD and shutdown coordination outside the named extraction partials. Journey/city state is extracted in 3b,
workflow page state in slice 4, and the counter, traffic, diagnostics and status projections in slice 5.
[MobaWinUiServiceCollectionExtensions](../MOBAflow/Extensions/MobaWinUiServiceCollectionExtensions.cs)
registers session/context aliases and constructs the shell. The extraction must update this actual
composition root rather than introducing another registration path.

Additional consumers are part of RF-24 acceptance, not incidental cleanup:

- [MonitorPageViewModel](../SharedUI/ViewModel/MonitorPageViewModel.cs):25,54,79 reads shell traffic
  and connection state; slice 5a moves it to ITrafficMonitor, IRuntimeSnapshotProvider and the session.
- [EventManagerViewModel](../SharedUI/ViewModel/EventManagerViewModel.cs) now reads the session,
  shared workflow library and journey-page status directly; its shell dependency is removed in 3b.
- [NavigationRegistration](../MOBAflow/Service/NavigationRegistration.cs) creates shell-bound pages;
  switch each page factory together with its XAML and constructor.
- [MainWindow.xaml](../MOBAflow/View/MainWindow.xaml):353-362 binds speech selection, and its
  diagnostics/status controls also bind extracted state. These shell surfaces must use the same
  focused state instances as their pages.
- [Solution partial](../SharedUI/ViewModel/MainWindowViewModel.Solution.cs):84-122 coordinates
  page command notifications. The session remains the selection/persistence owner.
  The SolutionAutoSave partial's TrackChanges and diagnostics reactions move with their owners.
- Commands, Signals, RawTurnout, Z21 and OperatingState partials need explicit classification
  during slices 3-5: operator behavior belongs to focused collaborators/gateways; the shell may
  display status and forward host commands, but may not retain page logic or own a runtime.

## Design decisions and alternatives

Use focused classes in SharedUI with required constructor dependencies and existing host adapters.
Do not add projects for this decomposition. Use the existing project/journey contexts for editing,
runtime snapshot/connection/traffic interfaces for queries and IRuntimeCommandGateway for commands.
Do not instantiate Backend services or create fallback runtime gateways in the extracted classes.
RF-25 later owns the wider constructor cleanup; RF-24 only makes its new dependencies explicit.

Preserve persistent ViewModel state with host-owned registrations matching the current singleton
page state and existing view registrations. The shell and pages may share a focused status/settings instance;
page ViewModels must never depend on the shell. Dispose subscriptions at the actual owning lifetime.
Keep EventBus UI dispatch at the decorated bus; inspect raw callbacks separately.

Reuse WorkflowLibraryViewModel, LocomotiveManagementViewModel and LayoutColumnWidthsViewModel.
Do not create parallel editors, move every method to one new manager, or treat partial-file movement
as separation. Smaller classes per responsibility keep dependencies testable without new abstraction
layers. Use one narrow layout persistence boundary while keeping existing serialized keys; RF-26,
not RF-24, owns settings relocation and feature-toggle representation changes.

## Independently reviewable delivery slices

Each code slice starts only after the start gate and is delivered as its own draft PR.
Refresh the base and update this plan from evidence after each integrated slice.

1. **Settings (integrated through #248; automated gates passed, manual UI acceptance open):** SettingsPageViewModel owns setters/reset,
   speech testing, feature toggles and settings-section state. SettingsPage, shell speech/theme and
   the overview startup indicator share this DI instance. Both superseded settings partials are
   removed. Regression tests cover persistence, reset/error paths, Piper selection, shared identity
   and shutdown unsubscription. Speech behavior remains behind its current integration; #244
   characterizes the current first-resolved settings-test engine and is preserved here.
2. **Rolling stock:** extract locomotives, then wagons, then train composition in separable PRs.
   - **2a Locomotives (integrated through #257; automated gates passed, manual UI acceptance open):** LocomotivesPageViewModel owns
     selection, search, stable filtered-list identity, CRUD, photo commands and panel expansion.
     It reads ISolutionSession directly, reuses LocomotiveManagementViewModel and keeps the same
     state in LocomotivesPage and the locomotive library in TrainsPage. The shell only forwards
     uploaded photos for the active page. Explorer launching is adapted in WinUI composition.
     Remove the superseded locomotive members from Wagons, Train and LayoutPanels partials;
     keep wagon and train-composition behavior for their later slices. Regression tests cover
     cancelled/stale photo operations, persistence, rename selection, project changes, management
     projection and shutdown unsubscription. Manual UI acceptance remains a separate gate.
   - **2b Wagons (integrated through #264; automated gates passed, manual UI acceptance open):** WagonsPageViewModel owns
     passenger/goods selection, search, stable filter identity, CRUD, photo commands and persisted
     panel expansion. Both inventory pages and TrainsPage share the same host-owned editor,
     session and column-width state. The shell forwards uploaded photos by the active page tag;
     Explorer launching remains in WinUI composition. Remove the superseded Wagons partial and
     its wagon members in Train, Counter, Solution and LayoutPanels; retain train composition for 2c.
     Regression tests cover both wagon types, project/solution changes, cancelled/late photos,
     same-path image refresh, removal, auto-save, shared DI identity and shutdown unsubscription.
     Manual UI acceptance remains open.
   - **2c Train composition (integrated through #272; automated gates and maintainer-reported UI check passed):** TrainsPageViewModel owns
     train/vehicle selection, search, train CRUD, composition insertion/removal/reordering and persisted
     panel expansion. TrainsPage receives this focused DI instance, sharing the existing locomotive
     and wagon editors, session and column widths. The Train partial and its shell selection/layout
     members are removed; the remaining station/journey runtime refresh helper is removed in 3b below.
     Session-owned train property tracking is preserved. Tests cover vehicle references, ordering and
     direction, duplicate prevention, project replacement, callback disposal, auto-save suppression,
     layout persistence and shared host identity. The maintainer reported the UI check as unremarkable
     on 2026-10-13 and authorized merge after fresh CI/Sonar verification. This records the reported
     acceptance of slice 2c; it does not close the other slices or the programme-wide UI criteria.
3. **Stations/journeys:** keep canonical project/journey selection in the session and move
   page commands and auto-save tracking with their owners, preserving the RF-23 lifecycle.
   - **3a Stations/platforms (integrated through #282; manual UI acceptance open):** StationsPageViewModel
     owns station/platform selection, properties, CRUD and panel expansion. StationsPage receives
     the focused host-owned editor; the Stations shell partial and its layout members are removed.
     Selected station/platform property tracking and collection subscriptions detach on selection,
     project/solution replacement and shutdown, including wrappers discarded by platform refresh.
     Preserve defaults, workflow assignment and runtime snapshot updates. The existing workflow
     library is registered once through host DI as a prerequisite for the station library;
     shell and page share its search/catalog state. Workflow behavior remains in the existing editor.
     Tests cover persistence, references, CRUD/defaults, command availability, replacement,
     stale subscriptions, auto-save suppression, layout and actual-host shared identity.
     Manual station/platform UI acceptance remains open.
   - **3b Journeys/cities (integrated through #401; manual UI acceptance open):** JourneysPageViewModel owns
     journey/station CRUD, city loading/search, ItemClicked properties, selected-station tracking,
     reset and persisted panel expansion. The host registers one instance for JourneysPage,
     the shell reset surface, EventManager status and deferred city loading. The Journey shell
     partial, city members, selection hooks and journey layout members are removed.
     Project changes select the first journey through the same session; station selection and
     discarded wrappers detach on journey/project/solution changes, refresh and disposal.
     The session retains canonical project/journey selection, journey tracking, auto-save and
     runtime ownership. Reset uses the injected IRuntimeCommandGateway. EventManager now reads
     the session, existing workflow library and journey status without a shell dependency.
     Navigation composition supplies the existing reset-all counter command; its behavior stays
     with the later counter/status slice.
     Tests cover defaults, CRUD, selection/ItemClicked, nested persistence, suppression,
     stale wrappers/subscriptions, ordering, gateway routing, city dispatch/late completion,
     layout and actual-host shared identity. Manual theme/UI acceptance remains open.
4. **Workflows (integrated through #409; manual UI acceptance open):** WorkflowsPageViewModel owns the
   workflow page presentation that the shared library lacks: the persisted workflow-list, actions
   and properties panel expansion under the existing layout keys. It receives the host-registered
   WorkflowLibraryViewModel, the settings service and a logger; WorkflowsPage binds to it instead
   of the shell. Selection, action editing/order, validation, dry run, trace and the editor object
   remain in the one library shared with StationsPage and EventManagerViewModel, whose bindings
   already read it directly. The Workflow shell partial, the shell's mirrored workflow/action
   selection and commands, its unbound ItemClicked command and its workflow layout members are
   removed; the shell keeps no workflow forwarding and takes no workflow page dependency, because
   no shell surface binds workflow state. Clearing the selected project already clears the
   library selection. Shutdown still disposes the shared library once through the station editor.
   Tests cover constructor guards, library identity and selection through the page, layout
   defaults/persistence/no-op writes, logged persistence failure, shell shutdown disposal,
   the architecture guard for WorkflowsPage and actual-host shared identity across the workflow,
   station and event-manager surfaces. Manual theme/UI acceptance remains open.
5. **Counters, diagnostics, health and synchronization status (planned; two sequential PRs, see
   [the slice 5 plan](#slice-5-implementation-plan-2026-11-05)):**
   - **5a Counters and traffic monitor:** InPortCounterViewModel owns the feedback-point statistics, counter
     settings, persistence error, reset command and counter snapshot projection; the reworked MonitorPageViewModel
     owns the traffic packets, connection status and Monitor expansion without a shell dependency. OverviewPage
     and EventManager receive the host counter instance. The Counter partial and the shell's traffic region,
     `ITrafficMonitor` dependency and Monitor layout members are removed; Z21 telemetry stays in the Z21 partial.
   - **5b Diagnostics and system status:** ProjectDiagnosticsViewModel owns the Messages pane state and refresh
     triggers; SystemStatusViewModel owns speech health, post-startup, REST API and synchronization status. The
     shell exposes both for the status bar and Messages pane, reads the operating-state inputs from
     SystemStatusViewModel and keeps `OperatingStateEvaluator` as the collaborator. The Diagnostics, HealthStatus,
     RestApiStatus, SyncDiagnostics and SolutionAutoSave partials and the optional diagnostics service are removed.
6. **Layout and final shell audit:** centralize panel/section/column persistence and replace the
   visual-tree search for MainWindowViewModel. Reuse any narrow settings-layout boundary established
   in slice 1. Remove LayoutPanels.cs, move SettingsPageViewModel.Layout persistence into the shared boundary, and remove superseded page glue.
   Narrow architecture exceptions and review every remaining shell member and dependency.

For every slice, the implementing PR owns the exact test count, build result, manual acceptance
and CI/Sonar evidence. A file moves only with its callers, notifications, subscriptions and tests.
Rollback is a revert to the preceding validated slice; do not retain a parallel legacy implementation.

### Remaining shell responsibility classification after slice 5 (planned)

| Members | Owner and next boundary |
| --- | --- |
| Solution new/open/save, stop confirmation and shutdown | Host/shell actions delegating to ISolutionSession; preserve RF-23 runtime lifecycle and persistence ownership. |
| Add/DeleteProject, generic ItemClicked, CurrentSelectedObject, Solution-page expansion | Solution/project UI to extract during slice 6's final shell audit; selection stays in the session. |
| Shared workflow library disposal at shutdown | Extracted in slice 4; the shell disposes the host-owned library once through the station editor. Slice 6's final shell audit decides whether host disposal replaces it. |
| Counter, traffic, diagnostics, health, REST API and synchronization projections | Slice 5a: InPortCounterViewModel and MonitorPageViewModel. Slice 5b: ProjectDiagnosticsViewModel and SystemStatusViewModel. |
| Journey runtime projection (`ApplyJourneyRuntimeSnapshots`) and Z21 status/telemetry in the Z21 partial | Stays in the shell as snapshot projection without runtime ownership; slice 5 removes only the counter and traffic parts. Slice 6's final shell audit decides whether a Z21 status ViewModel with rebinding of the status bar, Overview, SignalBox, TrackPlan and Solution surfaces is worthwhile. |
| Connect/disconnect, track power, feedback simulation, raw turnout and operating-state commands | Classified in slice 5 as host command forwarding through the injected gateway or connection runtime; they stay in the shell. The operating state reads its REST, speech and post-startup inputs from SystemStatusViewModel; `OperatingStateEvaluator` remains the collaborator. |
| SetSignalAspect and ordered ApplySignalBoxElementChange | Signal-box editor collaborator in slice 6's final shell audit. Preserve configuration-update/command/save order and the gateway; no signal behavior changes in 3b or 5. |
| Remaining page layout and GridColumnResizeBehavior shell lookup | Slice 6's single persistence boundary; keep serialized keys and star-column semantics. Slice 5a moves only the two Monitor expansion flags with their page. |

## Slice 5 implementation plan (2026-11-05)

Resolved against main `a2b36f11` (after #409 and #412). Slice 5 extracts the counter, traffic, diagnostics,
health and synchronization projections. It adds no runtime ownership, no new command and no persisted-format
change. It is delivered as two sequential draft PRs on their own branches after allocation (5a first), and only
after #414 is integrated (see the overlap check below): 5b alone changes the status bar and the shell's
operating-state inputs, and each half stays at the size of #401. Both follow the pattern of #401 and #409: one focused ViewModel per
responsibility in `SharedUI/ViewModel/`, required constructor dependencies, one host-owned singleton registered in
`MobaWinUiServiceCollectionExtensions`, pages receive the instance directly, the shell exposes an instance only where
a shell surface binds it, and superseded partials are removed with their tests moved rather than duplicated.
MOBAsmart is unaffected: it registers no shell, and `MauiViewModel` keeps its own counter and connection projection.

### 5a Counters and traffic monitor

| Leaves the shell | New owner and behavior |
| --- | --- |
| Counter partial: `Statistics`, `CounterPersistenceError`, `CounterCommandStatus`, `GlobalTargetLapCount`, `UseTimerFilter`, `TimerIntervalSeconds` with their persisting `On…Changed` hooks, `InitializeStatisticsFromFeedbackPoints`, `ResetCountersCommand`, `ApplyInPortCounterSnapshot`, `LogCounterCommandFailure`; `MainWindowViewModel.cs`: the constructor's counter initialization, `OnFeedbackPointCountChanged` and the `FeedbackPointCountChanged` subscription and its shutdown detach; Z21 partial: the `ApplyInPortCounterSnapshot` call | New `InPortCounterViewModel` (`SharedUI/ViewModel/InPortCounterViewModel.cs`, `IDisposable`). Required: `ISolutionSession`, `IRuntimeSnapshotProvider`, `IEventBus`, `IRuntimeCommandGateway`, `SettingsPageViewModel`, `ISettingsService`, `ILogger<InPortCounterViewModel>`. The constructor reads `Counter.TargetLapCount`, `UseTimerFilter` and `TimerIntervalSeconds` before it builds the statistics (the shell's order), applies `IRuntimeSnapshotProvider.Current`, then subscribes `RuntimeSnapshotChangedEvent` (counters and persistence error), `SettingsPageViewModel.FeedbackPointCountChanged` and the session's `SelectedProject` change; both re-initializations replace the collection instead of mutating it. Statistics keep the injected gateway; the three counter settings persist through `ISettingsService.SaveSettingsAsync` with the logged-failure pattern of `WorkflowsPageViewModel`. `Dispose` detaches all three subscriptions and ignores later snapshots. |
| Z21 partial traffic region: `TrafficPackets`, `InitializeTrafficMonitor`, `ReloadTrafficPackets`, `OnTrafficPacketLogged` (100-packet cap), `ClearTrafficMonitorCommand`, the `Z21TrafficPacketLoggedEvent` and `SelectedRuntimeChangedEvent` subscriptions, the `_trafficMonitor` field and the `ITrafficMonitor` constructor parameter; LayoutPanels partial: `IsMonitorTrafficExpanded`, `IsMonitorActivityLogExpanded` and their initialization | Existing `MonitorPageViewModel`, reworked. Required: `ITrafficMonitor`, `IRuntimeSnapshotProvider`, `IEventBus`, `ISolutionSession`, `ISettingsService`, `IUiDispatcher`, `ILogger<MonitorPageViewModel>`; the `MainWindowViewModel` parameter and property are removed. It owns `TrafficPackets` and the two subscriptions, folds the shell's clear behavior into its existing `ClearTrafficCommand`, derives `ConnectionStatus` from `RuntimeSnapshotChangedEvent` and `IRuntimeSnapshotProvider.Current` through `RuntimeSnapshotProjector.ProjectStatus` plus the session's `SelectedProject.Z21IpAddress`, and persists the two expansion flags under the existing `Layout.MonitorPage` keys. `Dispose` unsubscribes the bus, the session and the log sink and drops late packets. |

Stays in the shell, relocated from the Counter partial into the Z21 partial: `_latestRuntimeSnapshot`,
`MainCurrent`, `Temperature`, `SupplyVoltage` and `VccVoltage` (Z21 telemetry that OverviewPage binds next to
`SerialNumber`, `FirmwareVersion` and `HardwareType`). `HandleSelectedProjectChanged` moves into the Solution partial
and keeps the diagnostics refresh, `ApplyJourneyRuntimeSnapshots` and `UpdateSolutionLoadedStatus` until 5b.

Files. Deleted: `SharedUI/ViewModel/MainWindowViewModel.Counter.cs`. Added: `SharedUI/ViewModel/InPortCounterViewModel.cs`,
`Test/SharedUI/InPortCounterViewModelTests.cs`, `Test/SharedUI/MonitorPageViewModelTests.cs`. Changed:
`SharedUI/ViewModel/MainWindowViewModel.cs`, `.Z21.cs`, `.LayoutPanels.cs`, `.Solution.cs` and `MonitorPageViewModel.cs`;
`MOBAflow/View/OverviewPage.xaml` and `.xaml.cs` (second constructor parameter `InPortCounterViewModel`, exposed as
`Counters`; the eight counter bindings move from `ViewModel.` to `Counters.`); `MOBAflow/View/MonitorPage.xaml` and
`.xaml.cs` (`ViewModel.MainWindowViewModel.IsMonitor…` becomes `ViewModel.IsMonitor…`, `nameof` targets follow; the
code-behind keeps its star-value persistence for slice 6); `MOBAflow/View/MainWindow.xaml.cs` and
`MOBAflow/Extensions/MobaWinUiServiceCollectionExtensions.cs` (register `InPortCounterViewModel`; the `MainWindow`
factory also receives `InPortCounterViewModel` and `MonitorPageViewModel`, which `MainWindow.PrepareForShutdownAsync`
disposes after the shell confirmed shutdown, next to `RestApiStatusService` and `HealthCheckService`, because no shell
surface binds them and the host never disposes its container); `MOBAflow/Service/NavigationRegistration.cs`
(`EventManagerViewModel` receives `InPortCounterViewModel.ResetCountersCommand`);
`Test/Architecture/SolutionSessionArchitectureTests.cs` (the `MonitorPageViewModel.cs` allowance is removed and
`MonitorPage` joins the pages that reject shell dependencies; `OverviewPage` stays allowed because it still binds Z21
telemetry); the ten shell construction sites in tests lose the `ITrafficMonitor` argument
(`JourneyCounterProjectionTests`, `MainWindowDiagnosticsTests`, `MainWindowViewModelShutdownTests`,
`MainWindowViewModelPhotoAssignmentTests`, `MainWindowViewModelSignalBoxTests`, `MainWindowViewModelStartupAutoSaveTests`,
`JourneyMapViewModelTests`, `TimetablePageViewModelTests`, `TrainControlViewModelCleanupTests`,
`RestApiStatusServiceTests`); `Test/MOBAflow/MobaWinUiServiceCollectionExtensionsTests.cs`; the three
`quality/analyzer-baseline*.json` files (the removed Counter partial's diagnostics travel with it, the new fixtures add the
usual CA1812 entries, the Android file mirrors the SharedUI net10.0 entries); `docs/ARCHITECTURE.md`; this plan.

Tests. `InPortCounterViewModelTests` takes over the counter cases of `JourneyCounterProjectionTests` (unsigned
precision and timing, re-initialization and project selection keeping counts, cleared feedback-point count, feedback
count change and detach on dispose, reset availability waiting for the authoritative reset snapshot) and adds
constructor guards, settings persistence with no-op writes and a logged failure, the persistence error text and a
snapshot after dispose. `MonitorPageViewModelTests` takes over the shutdown traffic case of
`MainWindowViewModelShutdownTests` as a dispose case and adds reload on `SelectedRuntimeChangedEvent`, the 100-packet
cap, connection status following snapshot and selected project, clear through `ITrafficMonitor`, and expansion
persistence with no-op writes. `JourneyCounterProjectionTests` keeps its journey cases.
`MobaWinUiServiceCollectionExtensionsTests` verifies that `EventManagerViewModel.ResetCountersCommand` is the host
counter instance's command and that `OverviewPage`, `MonitorPage` and `MainWindow` resolve the same host instances.

### 5b Diagnostics and system status

| Leaves the shell | New owner and behavior |
| --- | --- |
| Diagnostics partial: `ProjectDiagnostics`, `VisibleProjectDiagnostics`, `IsDiagnosticsExpanded`, the three `ShowDiagnostic…` filters, the three counts, `DiagnosticsSummary`, `LocomotiveUsageCoverageSummary`, `RefreshProjectDiagnosticsCommand`, `ApplyDiagnosticFilter`, `IsDiagnosticVisible` and the locomotive-usage subscription with its shutdown detach; SolutionAutoSave partial (`OnSolutionModelChanged`); the optional `IProjectDiagnosticsService?` constructor parameter; the diagnostics refresh in `HandleSelectedProjectChanged` | New `ProjectDiagnosticsViewModel` (`IDisposable`). Required: `ISolutionSession`, `IProjectDiagnosticsService`, `LocomotiveTrainUsageViewModel` (the host singleton that `LocomotivesPage.Usage` already is, see `UsageReaderAndProjection_ResolveTheExistingDesktopOwnerAndSharedPageInstance`). It refreshes on construction, on the session's `SelectedProject` change, on `ISolutionSession.ModelChanged`, when the pane opens and when usage diagnostics change, expanding the pane for an edited usage result as today; `Dispose` detaches the session and usage subscriptions. The shell receives it as `Diagnostics` because `MainWindow.xaml` binds the Messages pane, and disposes it at shutdown. |
| HealthStatus partial: `SpeechHealthStatus`, `SpeechHealthIcon`, `SpeechHealthColor`, `SpeechStatusDisplayText`, `UpdateHealthStatus`, `IsPostStartupInitializationRunning`, `PostStartupStatusText`, `UpdatePostStartupInitializationStatus`; RestApiStatus partial: `RestApiStatusText`, `RestApiIsReachable`, `RestApiConnectedClients`, `RestApiConnectedClientsEmpty`, `UpdateRestApiStatus`, `OnRestApiStatusChanged`; SyncDiagnostics partial: the seven healthy/text pairs, `SolutionLoadedIsHealthy`, `SolutionLoadedStatusText`, `UpdateSyncDiagnostics`, `UpdateSolutionLoadedStatus` and the formatting helpers; `MainWindowViewModel.cs`: `OnSettingsPagePropertyChanged` and the `PostStartupStatusEvent`, `RestApiStatusChangedEvent` and `MobaflowSyncDiagnosticsChangedEvent` subscriptions | New `SystemStatusViewModel` (`IDisposable`), the status ViewModel that `docs/ARCHITECTURE.md` already announces. Required: `IEventBus`, `ISolutionSession`, `SettingsPageViewModel`. It subscribes the three events, resets speech health to Initializing when `SelectedSpeechProviderId` changes, evaluates `HasActiveProject` from the session and recomputes the solution-loaded status on `CurrentSolutionPath` and `SelectedProject` changes. The shell receives it as `SystemStatus` because the status bar binds speech health and REST status, and disposes it at shutdown. |

The RestApiStatus partial's `OnPhotoAssigned` and its `PhotoAssignedEvent` subscription are photo routing and move
to the PhotoAssignment partial. The OperatingState partial stays in the shell: `RecomputeOperatingState` reads
`SystemStatus.RestApiIsReachable`, `SpeechHealthStatus` and `IsPostStartupInitializationRunning`; the five partial
hooks for the REST, speech and post-startup properties become one `SystemStatus.PropertyChanged` handler for those
five names, detached at shutdown, and the direct recompute calls inside the moved update methods disappear.
`OperatingStateEvaluator` remains the pure collaborator; no second evaluator is added.

Files. Deleted: `SharedUI/ViewModel/MainWindowViewModel.Diagnostics.cs`, `.HealthStatus.cs`, `.RestApiStatus.cs`,
`.SyncDiagnostics.cs`, `.SolutionAutoSave.cs` and `Test/SharedUI/MainWindowDiagnosticsTests.cs`. Added:
`SharedUI/ViewModel/ProjectDiagnosticsViewModel.cs`, `SharedUI/ViewModel/SystemStatusViewModel.cs`,
`Test/SharedUI/ProjectDiagnosticsViewModelTests.cs`, `Test/SharedUI/SystemStatusViewModelTests.cs`. Changed:
`SharedUI/ViewModel/MainWindowViewModel.cs` (constructor: `ProjectDiagnosticsViewModel` and `SystemStatusViewModel`
required, `IProjectDiagnosticsService?` removed; shutdown disposes both), `.OperatingState.cs`, `.PhotoAssignment.cs`,
`.Solution.cs`; `MOBAflow/View/MainWindow.xaml` (Messages pane and status bar bind `ViewModel.Diagnostics.…` and
`ViewModel.SystemStatus.…`) and `.xaml.cs` (`ViewModel.SystemStatus.UpdateHealthStatus`); `MOBAflow/View/OverviewPage.xaml`
and `.xaml.cs` (third constructor parameter `SystemStatusViewModel`, exposed as `SystemStatus`; the REST and sync
bindings move); `MOBAflow/Extensions/MobaWinUiServiceCollectionExtensions.cs` (register both; shell factory);
`Test/Helpers/TestRollingStockPages.cs` (factories for the two status ViewModels so the ten construction sites change
mechanically); the ten construction sites; `Test/MOBAflow/MobaWinUiServiceCollectionExtensionsTests.cs`; the three
analyzer baselines; `docs/ARCHITECTURE.md`; this plan. `NavigationRegistration.cs` and
`SolutionSessionArchitectureTests.cs` are unchanged in 5b.

Tests. `ProjectDiagnosticsViewModelTests` takes over the seven diagnostics cases of `MainWindowDiagnosticsTests`
and adds constructor guards, refresh on model change and project change, and detach on dispose.
`SystemStatusViewModelTests` takes over the two speech-health cases of `MainWindowDiagnosticsTests` and the speech
selection case of `JourneyCounterProjectionTests`, and adds REST status and client projection from the event, every
sync pair with fresh and stale timestamps and an unreachable REST API, the solution-loaded text for no solution,
in-memory and file-backed solutions, the post-startup defaults and detach on dispose. The shell tests add one case: a
`SystemStatus` change recomputes the operating state, with the presentation unchanged from the existing evaluator
tests. `MobaWinUiServiceCollectionExtensionsTests` verifies that the shell, `MainWindow` and `OverviewPage` share the
host `ProjectDiagnosticsViewModel` and `SystemStatusViewModel` instances.

### Shell classification after slice 5

The shell keeps, explicitly classified: the Z21 status and telemetry projection from `RuntimeSnapshotChangedEvent`
(`IsConnected`, `IsTrackPowerOn`, `StatusText`, `SerialNumber`, `FirmwareVersion`, `HardwareType`, the four telemetry
values and the private fail-safe flags), the host command forwarding through the injected gateway or connection
runtime (`ConnectCommand`, `DisconnectCommand`, `SetTrackPowerCommand`, `SimulateFeedbackCommand`,
`RefreshZ21StatusCommand`, `AcknowledgeOperatingStateCommand`, `SendTurnoutCommandAsync` and the signal-box commands),
`ApplyJourneyRuntimeSnapshots` and the operating-state presentation. These are shell status and command forwarding
bound by the status bar, OverviewPage, SignalBoxPage, TrackPlanPage, SolutionPage and MOBAsmart's shutdown lookup;
they contain no page logic and own no runtime. Project CRUD, `CurrentSelectedObject`, `ItemClickedCommand`, the
Solution and SignalBox expansion flags and the signal-box editor methods remain slice 6 items as classified above.

### Manual UI acceptance after slice 5

After explicit launch authorization, in Light, Dark and High Contrast with keyboard and Narrator on the toggles:
Overview counters (count and lap progress, target lap count, timer filter and interval persisting across restart,
persistence error and reset status text, Reset all counters), Overview Z21 telemetry, REST API status and connected
clients, every synchronization row and the solution-loaded row for no solution, in-memory and file-backed solutions;
Monitor traffic list, clear, both expanders persisting across navigation and restart, connection status following the
project selection; the main window Messages pane (counts, three filters, refresh, coverage summary, opening the pane
refreshes, Show usage expands it), status bar speech health including a provider switch that shows Initializing until
the next check, post-startup text, REST status and the operating-state badge and InfoBar with acknowledge; Event
Manager Reset all InPort counters; shutdown with a connected runtime where cancelling keeps counters and traffic
updating. Record the result in #365 with commit, environment and omissions. No hardware, track power or train movement.

### Overlap check (observed 2026-11-05, local time)

- **RF-25 slice 4 (#318, Draft PR #414 on branch `claude/issue-318-shared-ui-dependencies`):** concrete overlap,
  verified with `gh pr diff 414 --name-only`. #414 changes these slice 5 files: `MOBAflow/Extensions/MobaWinUiServiceCollectionExtensions.cs`,
  `Test/Helpers/TestRollingStockPages.cs`, `Test/MOBAflow/MobaWinUiServiceCollectionExtensionsTests.cs`,
  `Test/MOBAflow/RestApiStatusServiceTests.cs`, `Test/SharedUI/JourneyCounterProjectionTests.cs`,
  `MainWindowDiagnosticsTests.cs`, `MainWindowViewModelPhotoAssignmentTests.cs`, `MainWindowViewModelShutdownTests.cs`,
  `MainWindowViewModelSignalBoxTests.cs`, `MainWindowViewModelStartupAutoSaveTests.cs`, `JourneyMapViewModelTests.cs`,
  `TimetablePageViewModelTests.cs`, `TrainControlViewModelCleanupTests.cs`, `docs/ARCHITECTURE.md`,
  `quality/analyzer-baseline.json` and `quality/analyzer-baseline.windows.json`. It does not change
  `MainWindowViewModel*.cs`, the pages or the Android baseline. By the Project Manager's rule, slice 5
  implementation (5a and 5b) waits for the integration of #414 and starts from the main that contains it; the
  construction-site and helper changes above are then rebased onto #414's required-dependency helpers.
- **#410 (`claude/issue-366-shared-ui-lane`):** no file overlap. It changes the quality workflow, the mutation lane
  configuration and scripts, `SharedUI/Properties/AssemblyAttributes.cs`, `MauiAdapterDispatchTests.cs`,
  `WagonsPageViewModelTests.cs`, documentation and the #360/#366 plans; slice 5 touches none of them. Sequencing
  note: once #410 is integrated, the SharedUI lane compiles every file under `Test/SharedUI` and `Test/Helpers`
  against SharedUI only, so the new fixtures must stay free of WinUI types (they are), and the new ViewModels need
  the tests above so the lane stays above its 37 % break threshold.

### Slice 5 validation

Per PR, from the repository root with the SDK in `global.json`, after the existing-fixture baseline above:

```powershell
dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0 --filter "FullyQualifiedName~InPortCounterViewModelTests|FullyQualifiedName~MonitorPageViewModelTests|FullyQualifiedName~JourneyCounterProjectionTests|FullyQualifiedName~MainWindowViewModelShutdownTests|FullyQualifiedName~SolutionSessionArchitectureTests"
dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0 --filter "FullyQualifiedName~ProjectDiagnosticsViewModelTests|FullyQualifiedName~SystemStatusViewModelTests|FullyQualifiedName~MainWindowViewModel|FullyQualifiedName~JourneyCounterProjectionTests"
```

The first filter belongs to 5a, the second to 5b; each must discover and execute tests. Then the MOBAflow FastDebug
compile with the changed XAML, both full suites (the Windows suite includes `MobaWinUiServiceCollectionExtensionsTests`
and `RestApiStatusServiceTests`), both Release analyzer gates with exact baselines, a manual Quality run with
`validate_android` for the Android baseline, documentation links, secrets and line endings. The draft PR owns the
remote CI and Sonar evidence; Chief Project Manager sets Ready for review.

## Slice 3b local validation (2026-10-15, refreshed 2026-10-16)

The source is synchronized with main `22346a7d523de58a6d97284f4afbace10d0c4969`.
MOBAsmart (Android) and both full Release test graphs were rebuilt with `-m:1 -t:Rebuild
-p:MobaAnalyzerGate=true -p:UseSharedCompilation=false`; the Windows graph includes the active
WinUI pages and normal MOBApi build/copy dependency. The exact analyzer gates match 3,235
diagnostics in 1,128 groups (portable), 2,471 in 947 groups (Windows) and 1,509 in 551 groups
(Android). The portable and Android baselines only remove or decrease diagnostics made obsolete
by extraction; no new diagnostic group or increased count is accepted.

Full Release suites ran sequentially from the compiled outputs with `--no-build --no-restore`:
2,802 portable and 2,923 Windows tests passed with no failures. The explicit sustained-refresh
test is not executed. The portable run excludes the eight MOBApi process-start cases
(`MobApiProcessTests`, `RuntimeHubLiveE2ETests`), which CI runs. Earlier counts (2,793 portable,
2,874 Windows) predate this synchronization; the Windows count was not confirmed by a final run. 31
page regression cases extend the existing JourneyCounterProjectionTests fixture; actual-host DI
also verifies shared editor/session/layout identity, the existing reset-all counter command,
injected journey-reset routing and shutdown unsubscription. Documentation links (475), instruction
consistency, secrets, diff and line-ending checks pass. The implementing draft PR owns current
remote CI/Sonar evidence; local checks do not establish a remote gate result.

The selection regression follows the documented WinUI event order: ItemClick precedes
SelectionChanged when item clicks and selection are enabled together. The focused regression
failed before the fix because the properties object and title were cleared, then passed after
the fix for both event orders. The selected journey remains owned by the session, while the
clicked journey's properties survive its selection notification and old station tracking is detached.
The project-switch regression also failed before restoring the original notification order:
the page now publishes the new project/items source before selecting its first journey.
Programmatic journey switches test clearing both old station and old journey properties.

The station editor regressions follow the [WinUI Selector implementation](https://github.com/microsoft/microsoft-ui-xaml/blob/8463f45162149de0ec3ad7df752596893fe3e13e/dxaml/xcp/dxaml/lib/Selector_Partial.cpp#L371-L395):
replacing ItemsSource clears list selection. Ten focused reset/editor-lifetime cases failed before
the fix. A valid open station editor now retains its properties and auto-save ownership through
search and reordering, including when search hides that station. Selecting a different station,
closing/replacing the editor, journey/project/solution changes, wrapper replacement and disposal
detach its subscription.
Both station-click event orders verify that the old station stops saving and the new one saves
exactly once. The diagnosis uses source inspection and bounded tests without starting the app.

Earlier portable/Windows checks used the process-local `MSBuildEnableWorkloadResolver=false`
property; the refreshed runs use the installed SDK 10.0.402 workloads without it. Analyzer and
XAML gates remain enabled. Android Release compilation and its analyzer gate pass; AAB
validation remains unverified. No machine-wide repair/install was performed. Manual Light/Dark/High Contrast,
accessibility and app/device checks remain open; no application or hardware was started.

## Slice 3b integration refresh (2026-10-17)

The current source includes main `91f80ab5d0639f2df76b533441b37bf02acd0b3f` and preserves
all intervening PR commits and the interrupted P3 changes. Two journey regression fixtures
still referenced the removed `IIoService` after the file-role split. They now use the existing
`TestFileServices.CreateMock()` helper and `ISolutionIoService`, including its persistence capability.
The station editor fix and its behavior assertions remain intact.

Both Release graphs were rebuilt with analyzers enabled, including active WinUI XAML and the
normal MOBApi dependency. The final integration includes RF-25's required workflow dependencies
and its DI/test updates; the complete affected graphs and suites were rechecked for that change.
The baselines inherited from main pass unchanged: 3,201 diagnostics / 1,122 groups (portable)
and 2,456 / 946 (Windows).

The final sequential runs passed 2,909 portable and 3,031 Windows tests. All 41 journey fixture
cases pass, including 13 station search/reorder/editor-lifetime cases. The portable run excludes
the eight MOBApi process-start cases listed above; CI owns those cases. The explicit
sustained-refresh test is not executed. Internal documentation links (513), instruction
consistency, secrets, diff and line-ending checks pass.

The preceding dated Android evidence is historical; this refresh does not claim a new local
Android Release/AAB result. Current remote CI and Sonar evidence belongs to the exact pushed
head of draft PR #293. Manual Light/Dark/High Contrast, accessibility and app/device acceptance
remain open. No application, debugger, device or hardware was started.

## Slice 4 local validation (2026-11-05)

The source starts from main `0c031269`. After the review follow-up, the portable suite passed 3,118
tests and the Windows suite 3,226 tests, each with zero failures and zero skips; the Windows run
excluded MOBAsmart tests as in CI. The MOBAflow FastDebug compile check, including the WorkflowsPage
bindings, passed with zero warnings before that follow-up, which changed no WinUI file. Both Release
analyzer gates match exactly: 3,193 diagnostics in 1,126 groups (portable) and 2,461 in 958
(Windows). The portable and Android baselines only drop the three diagnostics of the removed
Workflow partial, lower the shell's MVVMTK0042 count from 16 to 13 and its CA1848 count from 6 to 5
for the removed unused helper; each test baseline adds the usual CA1812 entry for the new fixture. The Android baseline change mirrors the
shared SharedUI net10.0 entries. No Android build was run locally; the manual Quality run
[42058304124](https://github.com/ahuelsmann/MOBAflow/actions/runs/42058304124) with `validate_android` on the head
merged as #409 verified it: the Android Release AAB job passed and its analyzer baseline matched 1,480 diagnostics
in 541 groups. The same run's firmware job failed in the ESP32-S3 build, which #412 fixed by pinning Python 3.13;
that failure is unrelated to slice 4.
Documentation links (623), secrets and line-ending checks pass. No application, device or hardware
was started; manual Light/Dark/High Contrast and accessibility acceptance remain open in #365.

## Validation strategy

Each implementation PR records actual validation. Slice 1 adds settings behavior, shared-instance,
shutdown-unsubscription and architecture tests, compiles active WinUI bindings, and runs full suites
and Release analyzer gates. Manual UI acceptance requires separate app launch authorization.

For implementation, choose fixtures from the refreshed inventory and add meaningful tests for
uncovered behavior before extraction. Existing anchors include:

| Area | Existing fixtures and uncovered checks to add where needed |
| --- | --- |
| Settings/layout | LayoutColumnWidthsViewModelTests and NullSettingsServiceTests; characterize settings equality/no-op writes, dependent notifications, reset/error path, section sort/usage/order and persisted expansion |
| Rolling stock | MainWindowViewModelPhotoAssignmentTests and LocomotiveManagementViewModelTests; preserve stale-project/solution/photo-copy checks, CRUD, filtering and rename binding identity; add composition reorder/selection cases |
| Stations/journeys | SolutionSessionTests and EventManagerEventPlanTests; add page CRUD, selected-project switch, nested model changes and command-state checks |
| Workflows | WorkflowLibraryViewModelTests; exercise selection, validation, order, cancellation and shared instance lifetime |
| Status/commands | JourneyCounterProjectionTests, MainWindowDiagnosticsTests, MainWindowViewModelSignalBoxTests, MainWindowViewModelShutdownTests, MainWindowViewModelStartupAutoSaveTests; slice 5 moves the counter, traffic, diagnostics and status cases into the fixtures named in its plan and adds subscription disposal coverage |
| Architecture | SolutionSessionArchitectureTests and RuntimeCommandPathArchitectureTests; reject page/service shell dependencies, confirm required DI and one session/runtime route |

Run from the slice's repository root, using the SDK in global.json:

```powershell
dotnet build SharedUI/SharedUI.csproj
dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0 --filter "FullyQualifiedName~MainWindowViewModel|FullyQualifiedName~MainWindowDiagnosticsTests|FullyQualifiedName~WorkflowLibraryViewModelTests|FullyQualifiedName~LocomotiveManagementViewModelTests|FullyQualifiedName~SolutionSessionTests|FullyQualifiedName~LayoutColumnWidthsViewModelTests|FullyQualifiedName~EventManagerEventPlanTests"
dotnet restore MOBAflow/MOBAflow.csproj
dotnet build MOBAflow/MOBAflow.csproj -c FastDebug --no-restore -p:BuildMOBApiDependency=false -p:CopyMOBApiToOutput=false
dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0
dotnet test Test/Test.csproj -f net10.0-windows10.0.22621.0 -p:IncludeMobaSmartTests=false
```

Narrow this existing-fixture baseline to the affected slice and include its new tests.
A filter must execute tests. Run the relevant full suites for DI, subscription and persistence
boundaries. FastDebug is a compile check; Release builds and analyzer baselines follow the actual
[quality workflow](../.github/workflows/quality.yml). If shared host contracts change, compile/test
affected consumers and include the Android lane. Preserve all gates; do not exclude active XAML.

Manual acceptance requires explicit MOBAflow launch authorization. After authorization, verify
affected commands, navigation away/back, project/solution switch, unsaved/cancelled/failed save,
photo selection, reset and status in Light, Dark and High Contrast, plus keyboard, focus, Narrator
and text scaling as applicable. Record commit, environment, expected/actual result and omissions.
Hardware operation is not required for this behavior-preserving decomposition: tests use fakes.
No train movement, live track power, flashing or deployment is authorized.

## Risks, compatibility and publication

| Risk | Mitigation and stop condition |
| --- | --- |
| RF-23 changes the inventory or ownership | Rebase preparation after completion; stop implementation until acceptance and residual consumers are reconciled |
| Stale page state after project switch or late async completion | Reuse session contexts; detach old models; preserve photo/context checks and test switch/failure paths |
| Broken compiled bindings or lost shared state | Change page constructor, factory, XAML and shell surface in the same slice; compile active pages and exercise shared-instance state |
| Duplicate events, auto-save or runtime ownership | One lifetime per subscription; session owns persistence; test teardown and keep all commands on the gateway |
| TTS changes overlap settings | Refresh #136/specs before slice 1; move the integration as it exists without adding providers or the speech feature |
| Serialized layout/default drift | Keep keys and defaults; stop and reclassify if a format or behavior change is needed |

No schema bump, compatibility layer, migration, new credentials or telemetry is planned.
Preserve existing logged failure handling and display-safe status; do not log user speech/configuration
secrets. Every changed file passes deterministic scanning before reading, committing and publishing.
Follow [AGENTS.md](../AGENTS.md) and the [Sonar policy](../.github/instructions/sonarqube-pre-pr.instructions.md):
draft PR, green current-head SonarCloud, zero OPEN/CONFIRMED PR findings and applicable CI before review.
An unavailable lane remains an open gate rather than a claimed pass.

## Downstream sequence and completion

After RF-24 completion, the maintainer's order is the joint analysis/architecture decision in
[#135](https://github.com/ahuelsmann/MOBAflow/issues/135) and
[#141](https://github.com/ahuelsmann/MOBAflow/issues/141), then RF-25 through RF-31 using current
dependencies. This preparation does not perform that later analysis or choose module boundaries.
#135 currently requires Claude Opus 5.5; verify that requirement and execution capability at its
start. Do not claim that a Codex analysis fulfills that model-specific acceptance.

At RF-29, coordinate sound/file adapters with
[#136](https://github.com/ahuelsmann/MOBAflow/issues/136) and
[specs/009-tts-providers](../specs/009-tts-providers/plan.md). Its merged planning artifacts describe
Speech.Base and separate SystemSpeech/Piper/Azure providers, retain effect playback in Sound, and
keep providers out of MOBAsmart. The plan still states awaiting maintainer approval; merged
documentation does not establish implementation approval or completion. Refresh this live before
RF-29 and avoid competing Sound moves or duplicate speech contracts.

Close #219 only after all slices and its acceptance evidence are complete. Update durable architecture
guidance and #47 tracking, remove this completed standalone plan, and retain the record in closed issues
and Git history. #47 remains open until its entire current programme acceptance is fulfilled.
