# Tasks: Solution session with one runtime per project

**GitHub Issue**: #191

**Spec Kit**: Required

Input: [spec.md](spec.md), [plan.md](plan.md), [research.md](research.md).

## Slice 1 - Solution session (no behavior change)

- [ ] T001 Add characterization tests for solution load, save, auto-save, dirty state and project/journey selection in `Test/SharedUI/SolutionSessionTests.cs`
- [ ] T002 Create `ISolutionSession` (extends `IProjectContext`) and `SolutionSession` in `SharedUI`; move solution ownership, selection, dirty state and auto-save from `MainWindowViewModel`
- [ ] T003 Make `MainWindowViewModel` delegate to the session; register `SolutionSession` as `ISolutionSession`, `IProjectContext` and `IJourneySelectionContext` in MOBAflow DI
- [ ] T004 Inject the session instead of `MainWindowViewModel` into `PostStartupInitializationService`, `RestApiSolutionSyncService`, `RestApiStatusService`, `TrackPlanSolutionBinder`, `WinUiAppStartupService`, `WinUiRecordingContextProvider`, `NavigationRegistration`
- [ ] T005 Inject the session instead of `MainWindowViewModel` into `EventManagerViewModel`, `MonitorPageViewModel`, `TimetablePageViewModel`, `TrainControlViewModel`, `MatrixPageViewModel` and `MOBAsmart/Platforms/Android/MainActivity`
- [ ] T006 Add an architecture test that no service or ViewModel depends on `MainWindowViewModel`, listing the WinUI pages left for RF-24
- [ ] T007 Run the portable and Windows test suites and the analyzer gates for validation; secrets scan and line endings
- [ ] T008 Open the slice PR as draft; SonarCloud green with zero open issues before review

## Slice 2 - No runtime copy

- [ ] T009 Characterize runtime master-data reads on the Z21 pipeline and record the synchronization rule in `plan.md`
- [ ] T010 Hold runtime values (signal aspect, current station, journey progress, counters) by id in the runtime
- [ ] T011 Read master data from the session's project; remove `CloneForRuntime`, `UpdateJourneyEventsAsync` and `UpdateSignalBoxAsync` and their callers
- [ ] T012 Test that editor changes keep running workflows, journey progress and aspects; run suites and analyzer gates for validation
- [ ] T013 Slice PR with SonarCloud green and zero open issues

## Slice 3 - Runtime and Z21 per project

- [ ] T014 Add the Z21 endpoint to `Project`; remove the Z21 address, port and recent list from `AppSettings` and the settings UI; per-project Z21 field in the project editor
- [ ] T015 Add `Z21ConnectionRegistry` (connections keyed by endpoint, takeover) with tests
- [ ] T016 Add `ProjectRuntimeFactory`: one DI scope per project with its `IZ21`, runtime, counters, interlocking and workflow context
- [ ] T017 Session creates and discards project runtimes; selected-project event forwarding to the UI bus; fixes #190
- [ ] T018 Per-project counter store and duplicate-Z21 diagnostics (later runtime does not connect)
- [ ] T019 Switch/close warning dialog and speed 0 for every known locomotive before discarding runtimes
- [ ] T020 Tests with two fake Z21 endpoints: independence, conflict, takeover, switch and cancel; run suites and analyzer gates for validation
- [ ] T021 Slice PR with SonarCloud green and zero open issues

## Slice 4 - MOBApi and MOBAsmart

- [ ] T022 Project identifier in MOBApi commands, runtime snapshots and runtime settings; reject unknown projects
- [ ] T023 MOBAflow applies remote commands to the named project's runtime only
- [ ] T024 MOBAsmart project selection and per-project Z21 address for the direct connection
- [ ] T025 Tests for project routing and MOBAsmart selection; run suites, Android build and analyzer gates for validation
- [ ] T026 Slice PR with SonarCloud green and zero open issues

## Slice 5 - Documentation

- [ ] T027 Update `docs/wiki/MOBAFLOW-USER-GUIDE.md`, `INSTALLATION.md` and `MOBASMART-USER-GUIDE.md`: one runtime and one Z21 per project, per-project Z21 setting, switch warning
- [ ] T028 Update `docs/ARCHITECTURE.md` and `CHANGELOG.md`; validate links and instruction consistency
- [ ] T029 Final PR closes #191 with SonarCloud green and zero open issues; record manual checks still pending approval
