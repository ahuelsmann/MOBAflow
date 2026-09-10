# Tasks: Journey event plans

**Input**: [spec.md](spec.md), [plan.md](plan.md), [data-model.md](data-model.md), [contracts/event-plan.md](contracts/event-plan.md)

**Tests**: Required for every changed behavior. Checked implementation/test-authoring tasks mean the code is present; execution/build/manual/publication gates remain separately unchecked until their final evidence is available.

## Phase 1: Analysis and setup

- [x] T001 Record approved requirements and compatibility in `specs/001-journey-event-plan/spec.md` and `plan.md` using repository templates.
- [x] T002 Define runnable validation and manual acceptance in `specs/001-journey-event-plan/quickstart.md`.
- [x] T003 Create authoritative issue #124, reference it in `specs/001-journey-event-plan/spec.md` and `plan.md`, and pass actual issue-body and PR governance validation.

## Phase 2: Foundational work

- [x] T004 Add nullable plan/event entities in `Domain/Journey.cs` and `Domain/JourneyEventPlan.cs`, keeping legacy properties unchanged.
- [x] T005 Add session counters and runtime state/commands in `Backend/Service/InPortCounterService.cs`, `Backend/Interface/IMobaRuntime.cs` and `Common/Runtime/InPortCounterSnapshot.cs`.

## Phase 3: US1 - Sparse events and virtual stops

**Independent check**: Five activations run only 2/3/5 and alter stops only through actions.

- [x] T006 [US1] Add sparse/equal/disabled/invalid-trigger and workflow-failure regressions in `Test/Backend/JourneyEventPlanTests.cs` and `Test/Backend/JourneyEventPlanAcceptanceTests.cs`.
- [x] T007 [US1] Implement baseline matching and once-per-run workflow dispatch in `Backend/Manager/JourneyManager.EventPlans.cs` using existing workflow actions.
- [x] T008 [US1] Run relevant journey fixtures in `Test/Test.csproj`, including unchanged `Test/Backend/JourneyManagerFeedbackTests.cs`; the runtime baseline portable full suite passed.

## Phase 4: US2 - Independent ports and concurrent journeys

**Independent check**: Different starting times produce independent deltas, including three concurrent runs.

- [x] T009 [US2] Cover interleaved ports, three simultaneous runs, stop/restart and no historical replay in `Test/Backend/JourneyEventPlanTests.cs` and `Test/Backend/JourneyEventPlanAcceptanceTests.cs`.
- [x] T010 [US2] Implement isolated run baselines/lifecycle in `Backend/Manager/JourneyManager.EventPlans.cs` and `Backend/Service/JourneySessionState.cs`; reject stale-definition starts during deferred activation in `Backend/Service/MobaRuntimeService.RuntimeApi.cs`, with regressions in `Test/Backend/MobaRuntimeEventPlanTests.cs`.
- [x] T011 [US2] Preserve local/replay command routing and explicit remote-session rejection for new lifecycle commands in `SharedUI/Service/RecordingRuntimeCommandGateway.cs`, `SharedUI/Service/MobileRuntimeCoordinator.cs`, `Backend/Service/Recording/CoreRecordingPayloadValidators.cs` and `Backend/Service/Recording/IsolatedReplayRuntime.cs`, with `Test/SharedUI/RecordingRuntimeCommandGatewayTests.cs` and `Test/SharedUI/MobileRuntimeCoordinatorTests.cs`.

## Phase 5: US3 - Idle-only reset

**Independent check**: Direct and UI reset reject active runs without changing counters.

- [x] T012 [US3] Cover counts before a run, application-session lifecycle, idle reset and reset/start coordination in `Test/Backend/InPortCounterServiceTests.cs`, `Test/Backend/JourneyEventPlanTests.cs` and `Test/Backend/MobaRuntimeEventPlanTests.cs`, including an externally supplied manager factory.
- [x] T013 [US3] Add guarded reset/start/stop runtime integration in `Backend/Service/MobaRuntimeService.RuntimeApi.cs`, `SharedUI/ViewModel/MainWindowViewModel.Journey.cs` and `SharedUI/ViewModel/MainWindowViewModel.Counter.cs`; cover projections in `Test/SharedUI/JourneyCounterProjectionTests.cs`.

## Phase 6: US4 - Slim editor and drag and drop

**Independent check**: Pointer and keyboard can create the same saved 2/3/5 plan.

- [x] T014 [P] [US4] Add observable row/plan command regressions in `Test/SharedUI/EventManagerEventPlanTests.cs`.
- [x] T015 [US4] Implement validation, add/assign/duplicate/move/delete and active-run guards in `SharedUI/ViewModel/EventManagerViewModel.cs` and `SharedUI/ViewModel/JourneyEventViewModel.cs`.
- [x] T016 [US4] Implement compact rows, workflow palette, row/workflow drop targets and keyboard alternatives in `MOBAflow/View/EventManagerPage.xaml` and `EventManagerPage.xaml.cs`.
- [ ] T017 [US4] Validate Light/Dark, focus, drag feedback and keyboard scenarios in `specs/001-journey-event-plan/quickstart.md` after explicit application-launch authorization.

## Phase 7: US5 - Existing data compatibility

**Independent check**: Legacy data round-trips unchanged; adoption creates an empty plan and keeps reference data.

- [x] T018 [P] [US5] Add absent/present/empty plan round-trip and legacy preservation tests in `Test/Domain/JourneyTests.cs`.
- [x] T019 [US5] Default new journeys to event plans and preserve explicit legacy adoption in `SharedUI/ViewModel/MainWindowViewModel.Journey.cs` and `SharedUI/ViewModel/EventManagerViewModel.cs`; hide legacy controls for event plans in `MOBAflow/Resources/EntityTemplates.xaml`.

## Final phase: Validation and convergence

- [x] T020 Run the shared-logic portable full suite in `Test/Test.csproj`: runtime baseline 1,710 passed, zero failed, four skipped, including the GotoJourney active-target regression. After UI refinement, the 18 focused EventManager/counter-projection/icon tests passed. Additional Windows-specific runtime tests were not run.
- [x] T021 Compile affected consumers through the portable `Test/Test.csproj` build graph, including its `MOBApi/MOBApi.csproj` reference, and build `MOBAflow/MOBAflow.csproj`: Windows FastDebug passed with zero warnings/errors. No Android source files changed.
- [x] T022 Verify serialization/runtime compatibility and final diff, then align feature documentation and public XML comments in affected source files.
- [x] T023 Run changed-file secrets scanning and `scripts/Test-LineEndings.ps1`: 62 changed files passed secrets/line-ending checks; staged checks and `git diff --check` passed. Rerun staged checks after final metadata staging.
- [x] T024 Attempt local Sonar against verified `github/main` and record outcomes in `specs/001-journey-event-plan/analysis.md`: zero secrets issues; 62 agentic failures reporting unavailable organization capability (403 Forbidden), exit 1. The attempt is complete, not a green quality result.
- [ ] T025 Before any later ready-for-review state, verify draft PR/SonarCloud requirements documented in `specs/001-journey-event-plan/plan.md`.
- [x] T026 Converge requirements, tasks, actual file paths and evidence in `specs/001-journey-event-plan/analysis.md`; retain unavailable checks as open.

## Dependencies and execution order

T001-T002 precede design and code; T003 provides authoritative issue traceability. T004-T005 provide shared contracts. US1-US3 share the manager and must coordinate edits. US4 editor work can proceed against settled model/runtime contracts while backend work proceeds in other files. US5 tests may proceed independently after the additive model exists. Validation follows implementation, and manual/remote gates cannot be inferred from local compile success.

Parallel examples: T014 editor tests alongside T007 backend matching; T018 serialization tests alongside T016 XAML. Confirm fixture/source filenames against the final implementation before marking tasks complete.

## Evidence

2026-09-10: Twenty-four of 26 tasks are checked. Runtime baseline full suite: 1,710 passed, zero failed, four skipped, 1,714 total, 45 seconds; `Test/TestResults/event-plan-portable.trx`. Three skips require running MOBApi; one requires bundled photos. After UI refinement, 18 focused tests passed and the final Windows FastDebug build passed with zero warnings/errors in 1 minute 29 seconds. Secrets/line-ending checks across 62 files, staged checks and `git diff --check` passed. Additional Windows-specific runtime tests were not run. Issue #124 and issue/PR governance are complete. Local Sonar was attempted with zero secrets issues but 62 agentic capability failures (403 Forbidden), so no green analysis is claimed. T017 and T025 remain open for full manual acceptance and the remote Sonar/PR gate.
