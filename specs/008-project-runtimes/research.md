# Research: Solution session with one runtime per project

**GitHub Issue**: #191

Findings on `main` at `0113551f` (2026-10-06).

## Solution ownership today

- `MainWindowViewModel` owns the loaded `Solution`, `SelectedProject`, `SelectedJourney`, the dirty state,
  `SaveSolutionInternalAsync`, auto-save (`MainWindowViewModel.SolutionAutoSave.cs`) and the runtime project
  activation. It implements `IProjectContext` and `IJourneySelectionContext`; MOBAflow registers it under both.
- MOBAsmart implements `IProjectContext` with `MobileSolutionContext`.
- 30 files outside `MainWindowViewModel` reference it. Host services (`PostStartupInitializationService`,
  `RestApiSolutionSyncService`, `RestApiStatusService`, `TrackPlanSolutionBinder`, `WinUiAppStartupService`,
  `WinUiRecordingContextProvider`, `NavigationRegistration`) and ViewModels (`EventManagerViewModel`,
  `MonitorPageViewModel`, `TimetablePageViewModel`, `TrainControlViewModel`, `MatrixPageViewModel`) can move to a
  session service. 15 WinUI pages and `GridColumnResizeBehavior` bind their XAML to `MainWindowViewModel`
  (for example 26 bindings in `JourneysPage.xaml`); they need page ViewModels first, which is RF-24.

## Runtime and Z21 today

- `IZ21`, `MobaRuntimeService`, `InPortCounterService`, `InterlockingRuntimeService`, `SemanticTurnoutCommandService`
  and `ActionExecutionContext` are singletons. `IZ21` is consumed only inside `Backend`.
- `MobaRuntimeService.ActivateProjectAsync` replaces the active project with a JSON copy (`CloneForRuntime`).
  Station, train and journey edits re-activate the project; journey event edits and (since RF-22) signal-box edits
  use targeted updates (`UpdateJourneyEventsAsync`, `UpdateSignalBoxAsync`).
- The runtime executes the project selected in the UI: selecting a project activates it
  (`OnSelectedProjectChanged` -> `RefreshActiveProjectRuntimeAsync`), and adding a journey, station or train
  re-activates it. Looking at another project therefore stops the running one without a warning (#190).
- Z21 address, port and recent addresses live in `AppSettings.Z21` (`Common/Configuration/AppSettings.Sections.cs`).
- Feedback counters persist through `FileInPortCounterStore` to one file for the whole app.

## Events, MOBApi and MOBAsmart

- One app-wide `IEventBus` is registered by `AddEventBusWithUiDispatch`; runtime events (`FeedbackReceivedEvent`,
  `RuntimeSnapshotChangedEvent`, ...) carry no project identifier.
- MOBApi caches one solution, one runtime snapshot and one runtime-settings record; remote commands carry no project.
- MOBAsmart runs one local runtime against one Z21 and mirrors MOBAflow's single snapshot.

## Decisions

- **Runtime scope**: each project runtime is a DI scope created by a project-runtime factory. The scope holds the
  project's `IZ21` connection, `MobaRuntimeService`, counters, interlocking and workflow context. Rationale: these
  types already depend only on each other; scoping them avoids a second set of hand-written factories.
- **Z21 takeover**: an app-lifetime `Z21ConnectionRegistry` owns connections keyed by endpoint; a runtime borrows its
  endpoint's connection, so a runtime created again for the same project reuses it (FR-005).
- **Events**: each runtime scope gets its own event bus for runtime-internal handlers; the session forwards the
  selected project's runtime events to the app-wide UI bus. UI subscribers therefore keep their contract and show the
  selected project (FR-009) without a project identifier on every event.
- **No copy**: the runtime keeps runtime values (signal aspect, current station, counters) in runtime state keyed by
  id and reads master data from the session's project; `CloneForRuntime`, `UpdateJourneyEventsAsync` and
  `UpdateSignalBoxAsync` disappear (FR-007, FR-008).
- **Synchronization rule (slice 2, maintainer decision 2026-10-08)**: `JourneyManager` reads journeys, event plans
  and stations on the Z21 pipeline, and workflows read stations, journey texts and the workflow list for seconds
  on background threads, while the editor changes the same lists on the UI thread. Sharing one object would race
  (`Collection was modified`, half-applied edits). The runtime therefore keeps an invisible definitions snapshot
  that `IConnectionRuntime.UpdateProjectAsync` refreshes after every saved change without re-activation. Runtime
  values stay keyed by id (`JourneySessionState`, signal aspects); a running workflow keeps the snapshot it started
  with. `UpdateJourneyEventsAsync` and `UpdateSignalBoxAsync` are replaced by `UpdateProjectAsync`; adding
  journeys, stations or trains no longer re-activates the project. The interlocking definition still changes only
  on activation.
- **Pages**: WinUI pages keep binding to `MainWindowViewModel` until RF-24 introduces page ViewModels; RF-23 moves
  services and ViewModels to the session and guards that with an architecture test.

Alternatives considered: one runtime with a project identifier on every command and event (rejected: it contradicts
"runtime and project belong together" and touches every event type); per-project singletons through keyed services
(rejected: lifetime of a whole object graph is clearer with a scope).
