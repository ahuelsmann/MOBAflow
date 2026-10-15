# RF-24: Decompose MainWindowViewModel

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/219
**Spec Kit**: Not applicable - behavior-preserving decomposition; no product or persisted-format change
**Status**: Active; slices 1, 2a, 2b, 2c and 3a integrated through #248, #257, #264, #272 and #282; slice 3b (Journeys/cities) implemented with validation tracked in #219; later slices planned
**Evidence**: Settings integrated at bfe9b91d; locomotives at 656bc630; wagons at c4e427d0; train composition at f6ad0853; stations/platforms at 983524e847df1a36a2f91f9ef99041f54249d895 on 2026-10-13. Journey/city implementation starts from main 7c1d92422c0cbf675fddc03a8979990957733d15, verified against github/main on 2026-10-15.

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
SettingsPage, LocomotivesPage, PassengerWagonPage, GoodsWagonPage, TrainsPage and StationsPage reject shell dependencies even while other pages remain allowed.
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
| [Workflow partial](../SharedUI/ViewModel/MainWindowViewModel.Workflow.cs), 187 lines; [WorkflowsPage](../MOBAflow/View/WorkflowsPage.xaml):32 onwards | Register and inject the existing WorkflowLibraryViewModel; add only page presentation state that it lacks | Selection, action editing/order, validation, dry-run cancellation, trace and editor state; no second workflow engine |
| Counter, Diagnostics, HealthStatus, RestApiStatus and SyncDiagnostics partials | Focused counter/diagnostic/status projections using existing runtime query contracts and EventBus | Counter errors and reset result, filters, health/API status, sync state and project isolation |
| [LayoutPanels](../SharedUI/ViewModel/MainWindowViewModel.LayoutPanels.cs), 337 lines; [SettingsPageViewModel.Layout](../SharedUI/ViewModel/SettingsPageViewModel.Layout.cs), extracted from the settings layout partial; [GridColumnResizeBehavior](../MOBAflow/Behavior/GridColumnResizeBehavior.cs):474 onwards | One layout persistence service; reuse LayoutColumnWidthsViewModel | Panel expansion, section usage/order, setting keys, star-column values and intentionally fixed pixels |

[MainWindowViewModel.cs](../SharedUI/ViewModel/MainWindowViewModel.cs) constructs
WorkflowLibraryViewModel and subscribes to session/runtime/status/photo events. It also owns project
CRUD and shutdown coordination outside the named extraction partials. Journey/city state is extracted in 3b.
[MobaWinUiServiceCollectionExtensions](../MOBAflow/Extensions/MobaWinUiServiceCollectionExtensions.cs)
registers session/context aliases and constructs the shell. The extraction must update this actual
composition root rather than introducing another registration path.

Additional consumers are part of RF-24 acceptance, not incidental cleanup:

- [MonitorPageViewModel](../SharedUI/ViewModel/MonitorPageViewModel.cs):25,54,79 reads shell traffic
  and connection state; move to ITrafficMonitor and IConnectionRuntime plus a focused UI projection.
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
     members are removed; the runtime refresh helper remains in the shell for station/journey consumers.
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
   - **3b Journeys/cities (implemented; validation tracked in #219):** JourneysPageViewModel owns
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
4. **Workflows:** the shared library DI registration is established by 3a; move remaining page state,
   update workflow and EventManager bindings, and remove shell construction/forwarding.
   Preserve one library instance shared by existing workflow surfaces.
5. **Counter and status:** extract counter, diagnostics, health, API/sync and traffic projections;
   replace Monitor and shell status consumers; PostStartup already uses the session. Move operating-state computation to a
   focused collaborator if it combines non-shell behavior. Remove relevant event handling/partials.
6. **Layout and final shell audit:** centralize panel/section/column persistence and replace the
   visual-tree search for MainWindowViewModel. Reuse any narrow settings-layout boundary established
   in slice 1. Remove LayoutPanels.cs, move SettingsPageViewModel.Layout persistence into the shared boundary, and remove superseded page glue.
   Narrow architecture exceptions and review every remaining shell member and dependency.

For every slice, the implementing PR owns the exact test count, build result, manual acceptance
and CI/Sonar evidence. A file moves only with its callers, notifications, subscriptions and tests.
Rollback is a revert to the preceding validated slice; do not retain a parallel legacy implementation.

### Remaining shell responsibility classification after 3b

| Members | Owner and next boundary |
| --- | --- |
| Solution new/open/save, stop confirmation and shutdown | Host/shell actions delegating to ISolutionSession; preserve RF-23 runtime lifecycle and persistence ownership. |
| Add/DeleteProject, generic ItemClicked, CurrentSelectedObject, Solution-page expansion | Solution/project UI to extract during slice 6's final shell audit; selection stays in the session. |
| WorkflowsPage ItemClicked, selected action and page presentation | Slice 4; reuse the already registered WorkflowLibraryViewModel. EventManager's journey status is already extracted in 3b. |
| Journey runtime projection in Z21/Counter partials | Slice 5's runtime/status projection; continue reading snapshots and updating the existing wrappers without runtime ownership. |
| Connect/disconnect, track power, feedback simulation, raw turnout and operating-state commands | Classify and extract focused control/status collaborators in slice 5; operator mutations keep the injected gateway, lifecycle stays with the host/session. |
| SetSignalAspect and ordered ApplySignalBoxElementChange | Signal-box editor collaborator in slice 6's final shell audit. Preserve configuration-update/command/save order and the gateway; no signal behavior changes in 3b. |
| Remaining page layout and GridColumnResizeBehavior shell lookup | Slice 6's single persistence boundary; keep serialized keys and star-column semantics. |

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
| Status/commands | MainWindowDiagnosticsTests, MainWindowViewModelSignalBoxTests, MainWindowViewModelShutdownTests, MainWindowViewModelStartupAutoSaveTests; add status/counter/monitor projection and subscription disposal coverage |
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
