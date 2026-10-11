# RF-24: Decompose MainWindowViewModel

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/219
**Spec Kit**: Not applicable - behavior-preserving decomposition; no product or persisted-format change
**Status**: Planning only; implementation blocked by RF-23/#191
**Evidence**: main commit `9b03676a9865275711d4031815f26a2d35e2474b`, inspected on 2026-10-11

## Purpose and scope

[Issue #219](https://github.com/ahuelsmann/MOBAflow/issues/219) owns acceptance and live status.
The [umbrella plan](QUALITY-AND-REFACTORING-PLAN.md) owns programme dependencies.
This is the single RF-24 child plan, transferring the umbrella's extraction order and acceptance anchor.
It permits independent preparation while RF-23 is open; it does not permit implementation yet.

The outcome is a shell responsible for navigation and shell state, with focused page ViewModels
and one layout persistence service. Preserve commands, bindings, defaults, model-wrapper notifications,
selection, auto-save and local/remote runtime routing. Remove superseded partials rather than moving them.

No runtime ownership, Z21 lifecycle, API routing, format migration, speech-provider implementation,
new runtime command, dynamic plugin or later RF package is included. New user-visible behavior or
a persisted-format change requires reclassification and Spec Kit before implementation.

## Start gate and dependency evidence

On the inspection date, RF-23/#191 was open and its PRs
[#207](https://github.com/ahuelsmann/MOBAflow/pull/207),
[#211](https://github.com/ahuelsmann/MOBAflow/pull/211) and
[#217](https://github.com/ahuelsmann/MOBAflow/pull/217) were drafts.
PR #217 targeted the safe-switch branch, not main. Green checks on an individual slice do not
complete #191. Do not adopt these unmerged branches as the RF-24 base or modify their worktrees.

Before the first implementation slice:

1. Verify #191 is completed, its required slices are integrated, and its Windows/Android/test
   acceptance evidence applies to the integrated commit. Refresh GitHub state and main.
2. Resolve the acceptance mismatch described below in the owning issue, with an explicit
   approved boundary for residual RF-24 consumers. Do not silently waive the RF-23 criterion.
3. Re-scan and refresh this inventory against that commit, including selected-project runtime
   access, shutdown, settings and command state. Record the base and any changed boundaries.
4. Confirm this child plan is linked and reviewed with no unresolved design question.
5. Create a dedicated worktree and task branch for the first responsibility; preserve other sessions.

### Acceptance mismatch to resolve

#191 currently says no ViewModel, page or service except the shell depends on MainWindowViewModel.
However, [SolutionSessionArchitectureTests](../Test/Architecture/SolutionSessionArchitectureTests.cs)
lines 23-33 explicitly allow WinUI pages, GridColumnResizeBehavior, PostStartupInitializationService,
MonitorPageViewModel and EventManagerViewModel until RF-24; composition and Android cleanup also
have exceptions. These are source-backed residual dependencies, not proof that RF-23 is complete.
The RF-23 owner must reconcile its acceptance statement and these approved residuals before this
plan becomes executable. RF-24 must remove page/service shell dependencies and narrow the guard;
the final guard must retain only justified shell/composition consumers.

## Source inventory and proposed owners

Paths below refer to the evidence commit, not unmerged RF-23 work. Line counts are file-size
observations, not coverage or quality metrics. The shell spans 21 source files at this commit.

| Responsibility and evidence | Proposed boundary | Existing behavior to preserve |
| --- | --- | --- |
| [Settings partial](../SharedUI/ViewModel/MainWindowViewModel.Settings.cs), 1,256 lines; [SettingsPage](../MOBAflow/View/SettingsPage.xaml.cs):24-36 and its layout partial | Focused SettingsPageViewModel using AppSettings, ISettingsService and existing speech-test integration | Setter equality checks, notifications, immediate persistence, reset, feature availability, Piper voices, speech success/failure |
| [Wagons partial](../SharedUI/ViewModel/MainWindowViewModel.Wagons.cs), 482 lines, and [Train partial](../SharedUI/ViewModel/MainWindowViewModel.Train.cs), 264 lines | Focused locomotive, passenger/goods wagon and train page state; reuse LocomotiveManagementViewModel | CRUD, filtered lists, rename selection identity, composition ordering and photo assignment to the initiating context |
| [Stations partial](../SharedUI/ViewModel/MainWindowViewModel.Stations.cs), 127 lines; [Journey partial](../SharedUI/ViewModel/MainWindowViewModel.Journey.cs), 223 lines; city commands in the core file | Stations and journey page ViewModels reading IProjectContext/IJourneySelectionContext | Shared journey selection, stations/platforms, city library, command availability, nested model change tracking |
| [Workflow partial](../SharedUI/ViewModel/MainWindowViewModel.Workflow.cs), 187 lines; [WorkflowsPage](../MOBAflow/View/WorkflowsPage.xaml):32 onwards | Register and inject the existing WorkflowLibraryViewModel; add only page presentation state that it lacks | Selection, action editing/order, validation, dry-run cancellation, trace and editor state; no second workflow engine |
| Counter, Diagnostics, HealthStatus, RestApiStatus and SyncDiagnostics partials | Focused counter/diagnostic/status projections using existing runtime query contracts and EventBus | Counter errors and reset result, filters, health/API status, sync state and project isolation |
| [LayoutPanels](../SharedUI/ViewModel/MainWindowViewModel.LayoutPanels.cs), 337 lines; [SettingsPageLayout](../SharedUI/ViewModel/MainWindowViewModel.SettingsPageLayout.cs), 126 lines; [GridColumnResizeBehavior](../MOBAflow/Behavior/GridColumnResizeBehavior.cs):474 onwards | One layout persistence service; reuse LayoutColumnWidthsViewModel | Panel expansion, section usage/order, setting keys, star-column values and intentionally fixed pixels |

[MainWindowViewModel.cs](../SharedUI/ViewModel/MainWindowViewModel.cs):107-236 constructs
WorkflowLibraryViewModel and subscribes to session/runtime/status/photo events. It also owns project
CRUD, city operations and shutdown coordination outside the named extraction partials.
[MobaWinUiServiceCollectionExtensions](../MOBAflow/Extensions/MobaWinUiServiceCollectionExtensions.cs):249-292
registers session/context aliases and constructs the shell. The extraction must update this actual
composition root rather than introducing another registration path.

Additional consumers are part of RF-24 acceptance, not incidental cleanup:

- [MonitorPageViewModel](../SharedUI/ViewModel/MonitorPageViewModel.cs):25,54,79 reads shell traffic
  and connection state; move to ITrafficMonitor and IConnectionRuntime plus a focused UI projection.
- [EventManagerViewModel](../SharedUI/ViewModel/EventManagerViewModel.cs) reads shell command/status
  and workflow state; reuse journey context, workflow library and focused status contracts.
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
page state and transient views. The shell and pages may share a focused status/settings instance;
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

1. **Settings:** characterize settings setters/reset and failure messages; move settings and the
   required settings-section state; update SettingsPage and shell speech/theme consumers. Delete
   Settings.cs when no consumer remains. Keep speech behavior behind its current integration.
2. **Rolling stock:** extract locomotives, then wagons, then train composition in separable PRs.
   Reuse management state; move photo commands and their context checks together. Delete Wagons.cs
   and Train.cs after their consumers move, including relevant selection handlers in core.
3. **Stations/journeys:** extract station/platform and journey/city presentation; keep canonical
   project/journey selection in the session. Move ItemClicked/page commands and auto-save tracking
   with their owners. Classify solution/project UI and signal/journey commands without changing
   the RF-23 lifecycle.
4. **Workflows:** inject WorkflowLibraryViewModel through DI, move any remaining page state,
   update workflow and EventManager bindings, and remove shell construction/forwarding.
   Preserve one library instance shared by existing workflow surfaces.
5. **Counter and status:** extract counter, diagnostics, health, API/sync and traffic projections;
   replace Monitor/PostStartup and shell status consumers. Move operating-state computation to a
   focused collaborator if it combines non-shell behavior. Remove relevant event handling/partials.
6. **Layout and final shell audit:** centralize panel/section/column persistence and replace the
   visual-tree search for MainWindowViewModel. Reuse any narrow settings-layout boundary established
   in slice 1. Remove LayoutPanels.cs/SettingsPageLayout.cs and all superseded page glue.
   Narrow architecture exceptions and review every remaining shell member and dependency.

For every slice, the implementing PR owns the exact test count, build result, manual acceptance
and CI/Sonar evidence. A file moves only with its callers, notifications, subscriptions and tests.
Rollback is a revert to the preceding validated slice; do not retain a parallel legacy implementation.

## Validation strategy

This planning-only change requires secrets scans, line endings, plan governance, link/path review
and a clean diff. No .NET build, test result or manual UI acceptance is claimed for preparation.

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
