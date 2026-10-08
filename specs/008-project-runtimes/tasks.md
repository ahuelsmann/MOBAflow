# Tasks: Solution session with one runtime per project

**GitHub Issue**: #191

**Spec Kit**: Required

Input: [spec.md](spec.md), [plan.md](plan.md), [research.md](research.md).

## Slice 1 - Solution session (no behavior change)

- [x] T001 Add characterization tests for solution load, save, auto-save, dirty state and project/journey selection in `Test/SharedUI/SolutionSessionTests.cs`
- [x] T002 Create `ISolutionSession` (extends `IProjectContext`) and `SolutionSession` in `SharedUI`; move solution ownership, selection, dirty state and auto-save from `MainWindowViewModel`
- [x] T003 Make `MainWindowViewModel` delegate to the session; register `SolutionSession` as `ISolutionSession`, `IProjectContext` and `IJourneySelectionContext` in MOBAflow DI
- [x] T004 Inject the session instead of `MainWindowViewModel` into `PostStartupInitializationService`, `RestApiSolutionSyncService`, `RestApiStatusService`, `TrackPlanSolutionBinder`, `WinUiAppStartupService`, `WinUiRecordingContextProvider`, `NavigationRegistration`
- [x] T005 Inject the session instead of `MainWindowViewModel` into `EventManagerViewModel`, `MonitorPageViewModel`, `TimetablePageViewModel`, `TrainControlViewModel`, `MatrixPageViewModel` and `MOBAsmart/Platforms/Android/MainActivity`
- [x] T006 Add an architecture test that no service or ViewModel depends on `MainWindowViewModel`, listing the WinUI pages left for RF-24
- [x] T007 Run the portable and Windows test suites and the analyzer gates for validation; secrets scan and line endings
- [ ] T008 Open the slice PR as draft; Sonar quality gate (SonarCloud) green with zero open issues before review

## Slice 2 - Live definitions snapshot

- [x] T009 Characterize runtime master-data reads on the Z21 pipeline and record the synchronization rule in `research.md`
- [x] T010 Add `IConnectionRuntime.UpdateProjectAsync` and `IJourneyManager.UpdateDefinitions`: refresh the definitions snapshot, keep journey progress, running workflows and signal aspects by id
- [x] T011 Refresh the snapshot from the session after every saved change; replace `UpdateJourneyEventsAsync`, `UpdateSignalBoxAsync` and the re-activations after adding journeys, stations or trains
- [x] T012 Test that editor changes keep running workflows, journey progress and aspects; run suites and analyzer gates for validation
- [x] T013 Slice PR with the Sonar quality gate (SonarCloud) green and zero open issues

## Slice 3 - Runtime and Z21 per project

### 3a - Z21 assignment

- [x] T014 Add the Z21 endpoint to `Project` with Z21 fields in the project properties; network search for every Z21 with IP address and serial number; Z21 finder column on the solution page to drag an unassigned Z21 onto a project
- [x] T014a Tests for discovery responses, the endpoint JSON and the finder (unassigned list, one project per Z21, removal); run suites and analyzer gates for validation
- [ ] T014b Slice PR with the Sonar quality gate (SonarCloud) green and zero open issues

### 3b - One runtime per project

- [ ] T014c Connect each runtime to its project's Z21; remove the Z21 address, port and recent list from the MOBAflow settings and settings UI and the runtime's own Z21 search; MOBAflow sends the selected project's Z21 to MOBAsmart until slice 4
- [ ] T015 Add `Z21ConnectionRegistry` (connections keyed by endpoint, takeover) with tests
- [ ] T016 Add `ProjectRuntimeFactory`: one DI scope per project with its `IZ21`, runtime, counters, interlocking and workflow context
- [ ] T017 Session creates and discards project runtimes; selected-project event forwarding to the UI bus; fixes #190
- [ ] T018 Per-project counter store and duplicate-Z21 diagnostics (later runtime does not connect)
- [ ] T018a Tests with two fake Z21 endpoints (independence, conflict, takeover); slice PR with the Sonar quality gate (SonarCloud) green and zero open issues

### 3c - Safe switch

- [ ] T019 Switch/close warning dialog and speed 0 for every known locomotive before discarding runtimes or changing a project's Z21 address
- [ ] T020 Tests for switch, close, cancel and address change with fake Z21 endpoints; run suites and analyzer gates for validation
- [ ] T021 Slice PR with the Sonar quality gate (SonarCloud) green and zero open issues

## Slice 4 - MOBApi and MOBAsmart

- [ ] T022 Project identifier in MOBApi commands, runtime snapshots and runtime settings; reject unknown projects
- [ ] T023 MOBAflow applies remote commands to the named project's runtime only
- [ ] T024 MOBAsmart project selection and per-project Z21 address for the direct connection
- [ ] T025 Tests for project routing and MOBAsmart selection; run suites, Android build and analyzer gates for validation
- [ ] T026 Slice PR with the Sonar quality gate (SonarCloud) green and zero open issues

## Slice 5 - Documentation

- [ ] T027 Update `docs/wiki/MOBAFLOW-USER-GUIDE.md`, `INSTALLATION.md` and `MOBASMART-USER-GUIDE.md`: one runtime and one Z21 per project, per-project Z21 setting, switch warning
- [ ] T028 Update `docs/ARCHITECTURE.md` and `CHANGELOG.md`; validate links and instruction consistency
- [ ] T029 Final PR closes #191 with the Sonar quality gate (SonarCloud) green and zero open issues; record manual checks still pending approval
