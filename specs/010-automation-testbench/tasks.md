# Tasks: Isolated Automation Testbench

**Source Issue**: #35

**Input**: [spec.md](spec.md), [plan.md](plan.md), [research.md](research.md), [data-model.md](data-model.md),
[runtime contract](contracts/isolated-runtime.md), [operator contract](contracts/operator-ui.md) and [quickstart.md](quickstart.md).

**Branch**: `codex/issue-35-automation-testbench`

**Baseline**: reviewed design PR #241 at `3a57dff2c2b6edca769e4daaaa302a835ae9a283`, merged as `4fa6402c` on 2026-10-11.
The backlog is based on integrated main `e7cc6f3232ffc6b22fe890dba054371642e901ec`. #241 was marked ready only after
current-head required checks/Sonar passed with zero OPEN/CONFIRMED issues. T003 rechecks current source and backlog acceptance.

**Status**: First foundation slice implemented: T001-T004 and T009-T010 (6 of 70 tasks).
Scenario definitions, scheduling, private runtime, comparisons, persistence and UI remain open. See [quickstart.md](quickstart.md)
for local evidence and separate CI/manual gates; this is not complete feature acceptance.

**Tests**: Meaningful regression tests are required by the feature/AGENTS.md and explicit acceptance scenarios.
Within each implementation slice, add the specified failing cases before changing behavior. A filtered zero-test run is
not evidence. This task-generation PR itself needs only document/diff/link/secrets/line-ending/governance checks.

**Format**: `- [ ] TNNN [P?] [USn?] Description with exact repository paths`.
[P] denotes independent test files at the same phase's entry checkpoint; it never overrides dependency/file ownership.
Paths for proposed types/fixtures are deliberate new files; reuse existing services instead of inventing another engine.

## Phase 1: Analysis and Setup

Purpose: refresh integrated source contracts before implementation. Revalidated on 2026-10-14/15 against main 7c1d9242; #241/#245 are merged.

- [x] T001 Recheck `Backend/Service/ProjectRuntimes/ProjectRuntimeFactory.cs`, `Backend/Service/WorkflowActionHandlers.cs` and `Backend/Service/WorkflowService.Sequence.cs` against current main; record the implementation base and approved scope in `specs/010-automation-testbench/plan.md` (FR-006).
- [x] T002 Map the existing counter/journey/workflow/whistle and architecture regression fixtures to `specs/010-automation-testbench/quickstart.md`; confirm fixture discovery, named consumer builds and portable/Windows target commands (FR-006, SC-001).
- [x] T003 Confirm the integrated #241 design, current source contracts, backlog acceptance and the read-only consistency report before writing product code; record remaining review/manual gates in `plans/issue-35-automation-testbench.md` (FR-019).

## Phase 2: Foundational Work

Purpose: shared definitions, real-effect boundary and owned scheduling. This phase blocks every executable story. New paths are proposed files, not existing implementations.

- [x] T004 Characterize current source-local FIFO, independent journeys, stop-change context, cancellation and filtered raw feedback in `Test/Backend/WorkflowSequenceExecutionTests.cs`, `Test/Backend/WorkflowExecutionCoordinatorTests.cs` and `Test/Backend/LocomotiveWhistleAutomationServiceTests.cs`; preserve existing behavior before introducing seams (FR-006, FR-010).
- [ ] T005 Define `Domain/Automation/AutomationScenario.cs` and `Domain/Automation/AutomationInitialState.cs` with GUID selections, counter seeds and journey state; retain these exact constraints: "Nonempty unique identity; trimmed nonempty project-unique name (ordinal ignore-case); no duplicate selections; speed finite and positive", "Exactly one state for every captured journey; no inherited activation or unrecognized references", "Supported nonzero InPort; unique port; unspecified ports initialize to zero; no reset/feedback emitted", "Nonempty journey: both stop fields resolve to the same zero-based stop; empty journey: both null". Use uint InPort, ulong Count, nullable stop ID/index and scenario PlaybackSpeed default 1.0 (FR-002, FR-009).
- [ ] T006 Define `Domain/Automation/SyntheticInput.cs` with GUID Id, long AtTicks, int Order and Feedback payload {uint InPort, bool IsActive}; preserve "Identity unique within scenario; nonnegative ticks/order; unique (AtTicks, Order); payload structurally valid", 100-nanosecond ticks and checked DateTimeOffset-range validation (FR-003, FR-010).
- [ ] T007 Define typed effect/invocation/state/absence/ordered-group models in `Domain/Automation/AutomationExpectation.cs`; preserve "Predicate fields legal for its kind; referenced IDs resolve; default positive exact count 1", "Known workflow; runnable selection rules are separate; default positive exact count 1", "State field/type valid; snapshot assertions evaluate once, not once per repeated projection", "Exactly zero matches; cannot also specify a positive count", and "Distinct scenario-owned references; no cycles/nested groups; positive or zero-allowing counts; disjoint matches across slots". Use inclusive windows, nonnegative integer Exact/Range with min <= max, and missing-window whole-trace matching (FR-017). Configure explicit typed JSON discriminators with existing Domain JSON options; do not introduce a second serializer.
- [ ] T008 Add model-only serialization/constraint tests in `Test/Domain/AutomationScenarioDefinitionTests.cs` using `Domain/JsonOptions.cs`; verify camelCase typed discriminators and that no run/state/result object enters definitions (FR-002, FR-019).
- [x] T009 Extend shared validated effect requests at `Backend/Interface/IWorkflowEffectPlanner.cs` / `Backend/Service/WorkflowEffectPlanner.cs`, introduce `Backend/Interface/IWorkflowEffectAdapter.cs` and `Backend/Service/ProductionWorkflowEffectAdapter.cs`, and route `Backend/Service/WorkflowActionHandlers.cs` through them; keep structural/reference validation and stop changes shared, with environment probes/real execution only in the production adapter (FR-006, FR-007).
- [x] T010 Extend `Test/Backend/WorkflowActionHandlersTests.cs` and `Test/Backend/WorkflowEffectPlannerTests.cs` to preserve production missing-file, malformed-payload, cancellation, signal-resolution and unconfigured-display behavior across all seven action families (FR-006, FR-007).
- [ ] T011 Define internal `Backend/Interface/IAutomationWorkScheduler.cs` and `Backend/Service/AutomationScheduling/AutomationWorkHandle.cs` with stable keys, registered-before-launch ownership, Ready/Running/WaitingTimer/WaitingChildren/Terminal states and native deferred/delay/owned-join awaitables (FR-012, FR-013, FR-014).
- [ ] T012 Add scheduling proof tests in `Test/Backend/AutomationWorkSchedulerTests.cs`: nested-result continuations, same-time descendants, future waits, no callback under manager locks, subscriber/owned-work faults, and waiting-child cycles with no possible future deadline and deterministic joined cancellation; prove ordinary Task/ConfigureAwait(false) escapes cannot produce a successful run (FR-012, FR-013, FR-014, FR-018).
- [ ] T013 Implement `Backend/Service/AutomationScheduling/ProductionAutomationWorkScheduler.cs`, `Backend/Service/AutomationTestbench/DeterministicAutomationWorkScheduler.cs` and `Backend/Service/AutomationTestbench/VirtualAutomationClock.cs`; preserve production independence/FIFO, fixed UTC epoch 2000-01-01, UTC timezone, checked timestamps/timers and current-instant draining without awaiting future work (FR-011, FR-012, FR-013).
- [ ] T014 Integrate owned deferred/predecessor/result joins and delay/cleanup seams in `Backend/Service/WorkflowExecutionCoordinator.cs`, `Backend/Service/WorkflowService.Sequence.cs`, `Backend/Manager/JourneyManager.cs` and `Backend/Service/LocomotiveWhistleAutomationService.cs`; retain source FIFO, execution-time contexts, zero-delay behavior and whistle coalescing, without a second evaluator or global serialization (FR-006, FR-010, FR-014).
- [ ] T015 Run the existing production anchors and `Test/Backend/AutomationWorkSchedulerTests.cs`; prove two independent journeys, one delayed predecessor, coalesced whistles and joined function-off cleanup before isolated factory work. Record actual counts/results in `specs/010-automation-testbench/quickstart.md` (FR-006, FR-013, SC-002, SC-003).

## Phase 3: User Story 1 - Test automation without affecting the layout (P1)

Goal: a headless isolated increment using the shared production graph. Independent test: seed InPort 1 to 10; two accepted pulses reach 12 and invoke one enabled active-journey workflow, with zero live/environment calls. This is backend acceptance, not the complete desktop MVP.

- [ ] T016 [P] [US1] Add validation regressions in `Test/Backend/AutomationScenarioValidationTests.cs` for all IDs/references/input pairs, explicit state for every journey, reachable selected workflow, absent known workflow, invalid stop/index and finite positive speed (FR-002, FR-003).
- [ ] T017 [P] [US1] Add isolation regressions in `Test/Backend/AutomationScenarioIsolationTests.cs` for all effect families/whistles, forbidden dependency resolution, live-target rejection, scenario-free cloning and zero scenario-origin live state/event/recorder mutations on success/failure/cancel/restart (FR-004, FR-005, FR-007, SC-001).
- [ ] T018 [P] [US1] Add seed/feedback regressions in `Test/Backend/AutomationScenarioFeedbackTests.cs`: 10 -> 12 exact target, disabled/inactive entries, unselected active journeys, timer filtering with raw whistle delivery, saturation, second-stop index 1/context 2 and empty-journey fallback (FR-009, FR-010).
- [ ] T019 [US1] Implement `Backend/Service/AutomationTestbench/AutomationScenarioValidator.cs` using the model constraints and shared payload/reference validation before graph construction; reject omitted journey state, unassigned runnable selections and unresolved/stale references without effects (FR-002, FR-003, FR-006).
- [ ] T020 [US1] Extract `Backend/Service/ProjectRuntimes/ProjectRuntimeComposition.cs` from `Backend/Service/ProjectRuntimes/ProjectRuntimeFactory.cs`; pass explicit private collaborators while retaining the production registry lease/startup outside the helper. Do not call the live host/factory StartAsync or connection synchronization in isolated construction (FR-004, FR-006, FR-007).
- [ ] T021 [US1] Implement scenario-free deep capture and private checkpoint initialization in `Backend/Service/AutomationTestbench/AutomationProjectCapture.cs` / `InMemoryJourneyCheckpointStore.cs` using `Domain/JsonOptions.cs`; set cloned activation/checkpoints before manager creation and null-store counter Set before trace, emitting no counted feedback/arrival (FR-004, FR-009).
- [ ] T022 [US1] Implement `Backend/Service/AutomationTestbench/SyntheticFeedbackSource.cs`: active inputs deliver Received -> raw FeedbackReceivedEvent -> occupancy true with shared correlation; inactive inputs deliver occupancy false only. Inject after physical parsing/dedup, preserve explicit logical pulse semantics and private buses only (FR-005, FR-010, FR-016).
- [ ] T023 [US1] Implement `Backend/Service/AutomationTestbench/RecordingWorkflowEffectAdapter.cs` and `RecordingLocomotiveFunctionCommandGateway.cs` using existing `Backend/Interface/ILocomotiveFunctionCommandGateway.cs`; capture structurally valid missing-file intents without probes, keep real stop transitions, and project safe fields before any logging/output (FR-006, FR-007, FR-008).
- [ ] T024 [US1] Implement `Backend/Service/AutomationTestbench/IsolatedAutomationRuntimeFactory.cs` / `IsolatedAutomationRuntime.cs` using the shared composition and owned graph; exclude registry/root host, production stores/logger, UDP/API, processes, speakers and display senders. Retain only private interlocking definition activation if shared handlers require it (FR-004, FR-005, FR-007).
- [ ] T025 [US1] Implement run-owned typed `Backend/Service/AutomationTestbench/AutomationTrace.cs` and private failure observation: controller time/global logical sequence, initial state and workflow/journey/input correlation; surface subscriber/task faults instead of swallowing them or serializing raw events/exceptions (FR-008, FR-016, FR-018).
- [ ] T026 [US1] Implement the one-run execution/completion primitive in `Backend/Service/AutomationTestbench/AutomationTestbenchService.cs`: validate/capture first, process actual shared handlers/delays (never DryRun), run every matching active journey, retain failures, and finish only when inputs and owned work are exhausted (FR-006, FR-010, FR-018).
- [ ] T027 [US1] Execute the US1 fixtures in `Test/Backend/AutomationScenarioValidationTests.cs`, `AutomationScenarioIsolationTests.cs` and `AutomationScenarioFeedbackTests.cs`; record exact test counts and zero-I/O evidence in `specs/010-automation-testbench/quickstart.md` before exposing execution to UI (SC-001).

## Phase 4: User Story 2 - Control virtual time and reproduce results (P1)

Goal: play/pause/step/cancel/restart without timing drift. Independent test: two same-time inputs and independently delayed journeys produce identical normalized state/trace across ten runs/speeds; pause, step and cancellation obey their boundaries.

- [ ] T028 [P] [US2] Add control-state/invalid-control tests in `Test/Backend/AutomationScenarioRunnerTests.cs` for validation, Running/Paused/Cancelling, terminal categories, repeated Cancel, interrupted pacing and restart (FR-011, FR-014, FR-015, FR-018).
- [ ] T029 [P] [US2] Add ten-run/speed repeatability tests in `Test/Backend/AutomationScenarioDeterminismTests.cs` for explicit equal-time input order, source-local FIFO, independent journeys, nested delays, stop changes and coalesced whistle contributor identities (FR-013, FR-016, SC-002).
- [ ] T030 [P] [US2] Add joined-disposal tests in `Test/Backend/AutomationScenarioDisposalTests.cs` for paused/delayed/active whistle cancellation, retained function-off cleanup, zero post-completion effects/timers, failed-drain ownership and fresh restart state (FR-014, FR-018, SC-003).
- [ ] T031 [P] [US2] Add boundary tests in `Test/Backend/VirtualAutomationClockTests.cs` for fixed UTC epoch/timezone, checked overflow, next input/timer deadline, same-time scheduling, no wall-time progression while paused and no-input initial-state completion (FR-011, FR-012).
- [ ] T032 [US2] Extend `Backend/Service/AutomationTestbench/AutomationTestbenchService.cs` with the data-model control transitions and async admission; never hold admission while awaiting future time, reject invalid controls recoverably and let repeated Cancel join one drain (FR-011, FR-015).
- [ ] T033 [US2] Implement exact next-instant progression in `Backend/Service/AutomationTestbench/DeterministicAutomationWorkScheduler.cs`: drain existing due work, explicit ordered inputs with descendants, then remaining same-time microtasks; single-step returns Paused/terminal before any later instant (FR-012, FR-013).
- [ ] T034 [US2] Complete Cancel/disposal in `Backend/Service/AutomationTestbench/IsolatedAutomationRuntime.cs`: stop input admission, cancel waits, join all owned cleanup/notifications/subscriptions, seal capture and only then report cancellation completion. Failed drain retains ownership and blocks replacement (FR-014, SC-003).
- [ ] T035 [US2] Implement Restart in `Backend/Service/AutomationTestbench/AutomationTestbenchService.cs` by joined disposal followed by revalidation/current-definition capture and fresh counts/checkpoints/clock/scheduler/trace; share no former mutable state or callbacks (FR-004, FR-014).
- [ ] T036 [US2] Implement pacing in `Backend/Service/AutomationTestbench/AutomationPlaybackPacer.cs` outside the graph; PlaybackSpeed is finite/positive with scenario default 1.0, Pause/Cancel interrupt waits, and speed affects no virtual timestamp/order/result (FR-011).
- [ ] T037 [US2] Execute `Test/Backend/AutomationScenarioRunnerTests.cs`, `AutomationScenarioDeterminismTests.cs`, `AutomationScenarioDisposalTests.cs` and `VirtualAutomationClockTests.cs`; retain ten normalized comparisons, exact discovered counts and production regressions in `specs/010-automation-testbench/quickstart.md` (SC-002, SC-003).

## Phase 5: User Story 3 - Explain expected versus actual differences (P1)

Goal: pure partial comparisons, complete safe trace and useful first mismatch. Independent test: deliberate payload/count/order failure identifies the first saved expectation and retains every outcome and available trace entry.

- [ ] T038 [P] [US3] Add predicate/count/window tests in `Test/Backend/AutomationScenarioComparisonTests.cs`: unspecified fields/unrelated outputs, missing-window whole trace, inclusive bounds, positive default exact 1, Exact/Range, exceeded maximum and absence zero (FR-017).
- [ ] T039 [P] [US3] Add initial/final/transition and ordered-group tests in `Test/Backend/AutomationScenarioOrderedComparisonTests.cs`: all matches disjoint, A(2),B(1) rejects A,B,A/passes A,A,B, zero slot is vacuous, independent assertions may reuse entries and repeated snapshots are not transitions (FR-017).
- [ ] T040 [P] [US3] Add projection/error/trace-capacity tests in `Test/Backend/AutomationScenarioTraceTests.cs`: script argument/derivative redaction, lexical safe paths, safe error codes, no raw wall-clock events/exception data, retained partial evidence and no Passed after capture failure (FR-008, FR-016, FR-018, SC-004).
- [ ] T041 [US3] Implement pure typed predicate/window/state matching in `Backend/Service/AutomationTestbench/AutomationScenarioComparer.cs`; legal selected fields only, inclusive virtual windows, whole trace when absent, initial/final evaluation once and transition-only occurrence matching (FR-017).
- [ ] T042 [US3] Implement Exact/Range/absence and all-match ordered-group comparison in `Backend/Service/AutomationTestbench/AutomationScenarioComparer.cs`; bounds nonnegative with min <= max, disjoint sets, every earlier-slot match before every next nonempty slot and no greedy subset matching (FR-017).
- [ ] T043 [US3] Implement `Backend/Service/AutomationTestbench/AutomationRunResult.cs` and completion integration in `AutomationTestbenchService.cs`; keep all comparison outcomes/trace and first mismatch in saved order; distinguish invalid, no-expectation completion, Passed, Mismatch, ExecutionFailed, Cancelled and TestbenchError (FR-018).
- [ ] T044 [US3] Complete stable workflow/action/journey/whistle attribution and normalized comparison in `Backend/Service/AutomationTestbench/AutomationTrace.cs`; coalesced effects include every contributing input/rule and generated output contains only allow-listed safe fields, never raw script arguments/hash/sensitive absolute paths (FR-008, FR-016).
- [ ] T045 [US3] Enforce complete capture or explicit failure in `Backend/Service/AutomationTestbench/AutomationTrace.cs`; do not reuse silently evicting production WorkflowTraceStore, hide subscriber/owned-task faults or classify incomplete trace as Passed (FR-016, FR-018).
- [ ] T046 [US3] Run US3 comparison/trace fixtures in `Test/Backend/AutomationScenarioComparisonTests.cs`, `AutomationScenarioOrderedComparisonTests.cs` and `AutomationScenarioTraceTests.cs`, including run-result integration; record first-mismatch/full-outcome evidence in `specs/010-automation-testbench/quickstart.md` (SC-004).

## Phase 6: User Story 4 - Maintain reusable scenarios and integrate the desktop (P2)

Goal: CRUD/save/reopen with the final editor and guarded context transitions. Independent test: temporary test-owned solution roundtrip/deep duplicate; editing saves, run notifications never save; delayed/failed departure cannot mutate page/history/project/session early. Depends on US1-US3 for the combined desktop flow.

- [ ] T047 [P] [US4] Add solution roundtrip/schema tests in `Test/Domain/AutomationScenarioSerializationTests.cs`: missing/empty optional collection, complete typed definitions/IDs, schema version 4, and no restored execution state/results/history (FR-019, SC-005).
- [ ] T048 [P] [US4] Add deep-duplicate and definition-save tests in `Test/SharedUI/AutomationScenarioEditingTests.cs`: independent lists/owned IDs, remapped input/expectation/group references, preserved project references, unique names and create/edit/delete/reopen behavior (FR-001, FR-019, SC-005).
- [ ] T049 [P] [US4] Add VM tests in `Test/SharedUI/AutomationTestbenchViewModelTests.cs`: edit locking during validation/running/pause/drain, all controls/results, recoverable errors, private-notification dispatch, generation filtering and zero run-triggered saves (FR-005, FR-011, FR-015, FR-018).
- [ ] T050 [P] [US4] Add delayed/failed drain/bypass tests in `Test/SharedUI/AutomationTestbenchTransitionTests.cs` and `Test/MOBAflow/AutomationTestbenchNavigationTests.cs`; cover navigation/back, selection/add/delete, New/Load/shutdown and unchanged context/history until successful drain (FR-014, FR-015).
- [ ] T051 [US4] Add optional `Project.AutomationScenarios : List<AutomationScenario> = []` in `Domain/Project.cs` and explicit definition variants in `MOBAflow/Build/Schemas/solution.schema.json`; retain version 4, containing-project ownership and no run/history fields, migration or example solution (FR-019).
- [ ] T052 [US4] Implement definition duplication/edit wrappers in `SharedUI/ViewModel/AutomationScenarioViewModel.cs` and `SharedUI/Service/AutomationScenarioEditingService.cs`; preserve quoted model constraints and existing Domain JSON/atomic solution save patterns, with fresh owned IDs/remapped internal references (FR-001, FR-002, FR-019).
- [ ] T053 [US4] Implement CommunityToolkit editor/control/result state in `SharedUI/ViewModel/AutomationTestbenchViewModel.cs`; inject project/session/dialog plus isolated feature control only, never live runtime surfaces/simulation or concrete railway gateways; normal definition edits save and private run projection never enters session change tracking (FR-001, FR-005, FR-011, FR-018, FR-019).
- [ ] T054 [US4] Wire run-notification UI dispatch and activation/departure subscription lifetime in `SharedUI/ViewModel/AutomationTestbenchViewModel.cs`; use existing `SharedUI/Interface/IUiDispatcher.cs` outside the decorated bus, with late-generation rejection after joined disposal and no duplicate dispatch in EventBus handlers (FR-015, FR-016).
- [ ] T055 [US4] Introduce narrow `SharedUI/Interface/IAutomationTestbenchRunGuard.cs` and `SharedUI/Service/AutomationTestbenchRunGuard.cs`; await private Cancel/drain before context mutation, retain old context on failure and avoid a generic lifecycle registry/async-void setter hook (FR-014, FR-015).
- [ ] T056 [US4] Await the guard before history push/pop and Frame.Content changes in `MOBAflow/Service/NavigationService.cs` for NavigateToPageAsync/GoBackAsync; unsuccessful drain leaves page/history intact (FR-015).
- [ ] T057 [US4] Await the guard before clearing/applying loaded project state in `SharedUI/Service/SolutionSession.cs` for NewSolutionAsync/LoadSolutionAsync/LoadSolutionFromPathAsync; read may precede drain, failed load does not disturb a run, and direct synchronous project setter cannot bypass the guarded commit (FR-015).
- [ ] T058 [US4] Route selection/add/delete/shutdown through awaited guarded commands in `SharedUI/ViewModel/MainWindowViewModel.Solution.cs`, `SharedUI/ViewModel/MainWindowViewModel.cs` and `MOBAflow/View/SolutionPage.xaml`; commit selection only after drain, before ClearAllSelections/removal/TryBeginShutdown as applicable (FR-015).
- [ ] T059 [US4] Implement active `MOBAflow/View/AutomationTestbenchPage.xaml` and `MOBAflow/View/AutomationTestbenchPage.xaml.cs` with scenario CRUD, all initial state/input/expectation fields, Play/Pause/Single step/Cancel/Restart/speed, first mismatch/all outcomes/full trace, English isolated/synthetic labels, ThemeResource/focus/keyboard and no color-only errors; code-behind adapts lifecycle/visuals only (FR-001, FR-005, FR-020).
- [ ] T060 [US4] Register transient page/singleton VM/private service and metadata in `MOBAflow/Service/NavigationRegistration.cs` and `MOBAflow/Extensions/MobaWinUiServiceCollectionExtensions.cs`; extend `Test/MOBAflow/MobaWinUiServiceCollectionExtensionsTests.cs` and preserve `Test/Architecture/RuntimeCommandPathArchitectureTests.cs`, with no new feature-toggle/configuration default or active Page Remove (FR-005, FR-020).
- [ ] T061 [US4] Execute US4 fixtures, `Test/SharedUI/SolutionSessionTests.cs` and host/architecture regressions; verify complete create-run-inspect-restart and deep roundtrip with temporary test-owned data, zero run-triggered saves and stale-reference rejection. Record results in `specs/010-automation-testbench/quickstart.md` (SC-005, SC-006).

## Final Phase: Validation and Documentation

Purpose: full target evidence and quality gates. Repeat slice-appropriate checks at every checkpoint; do not postpone regressions or PR gates until this final phase.

- [ ] T062 Run the full portable `Test/Test.csproj` target (-p:TargetFrameworks=net10.0 -f net10.0) and named `MOBApi/MOBApi.csproj` consumer build after runtime/persistence integration; require intended discovery and record exact failures/skips/counts in `specs/010-automation-testbench/quickstart.md` (SC-001, SC-002, SC-004).
- [ ] T063 Run full Windows `Test/Test.csproj` target (-f net10.0-windows10.0.22621.0 -p:IncludeMobaSmartTests=false), compile `MOBAflow/MOBAflow.csproj` FastDebug and normal dependencies/Release quality checks for host integration; report actual evidence rather than treating FastDebug as packaging acceptance (SC-005, SC-006).
- [ ] T064 After explicit app-launch authorization, inspect Light/Dark/High Contrast, English labels, keyboard/focus/Narrator/scaling and disconnected create-run-inspect/cancel/departure in `MOBAflow/View/AutomationTestbenchPage.xaml`; record pending rather than a pass if launch/manual acceptance is unavailable in `specs/010-automation-testbench/quickstart.md` (FR-020, SC-006).
- [ ] T065 Update actual operator behavior in `docs/wiki/MOBAFLOW-USER-GUIDE.md`, architecture ownership in `docs/ARCHITECTURE.md` and `CHANGELOG.md`; link `specs/010-automation-testbench/quickstart.md` without claiming unexecuted CI/manual/hardware outcomes (FR-020, SC-006).
- [ ] T066 Review final shared boundaries/public API XML documentation in `Backend/Service/AutomationTestbench/AutomationTestbenchService.cs` and `SharedUI/ViewModel/AutomationTestbenchViewModel.cs`; preserve configuration defaults/safe startup/layout units, no second evaluator, superseded behavior, migration, sample or unrelated cleanup (FR-006, FR-019).
- [ ] T067 Scan every changed file and run `scripts/Test-LineEndings.ps1 -Path <changed paths> -Fix`, verify again and run -Staged before commit; review diff/links and execute `scripts/Test-SpecKitGovernance.ps1` for the changed feature/plan files (FR-008, FR-019).
- [ ] T068 Publish each narrow implementation slice as a draft PR linked to #35; use `.github/workflows/quality.yml` for current-head required checks/Sonar and verify zero OPEN/CONFIRMED remote issues before ready. Recheck changed head/base; do not run local code analysis, lower gates or merge without authorization (FR-006, SC-001).
- [ ] T069 Record all FR-001–FR-020 / SC-001–SC-006 acceptance evidence, task outcomes and separate automated/CI/manual limitations in `specs/010-automation-testbench/quickstart.md`; rerun the Constitution Check in `specs/010-automation-testbench/plan.md` and consistency analysis after substantive changes (FR-018, SC-001, SC-002, SC-003, SC-004, SC-005, SC-006).
- [ ] T070 After accepted feature completion, remove `plans/issue-35-automation-testbench.md` and retain the Spec Kit artifacts/Git history/#35 evidence; do not mark pending UI/CI/manual acceptance or generated tasks as delivered product behavior (FR-019).

## Dependencies and Execution Order

```text
Setup T001-T003
  -> Foundations T004-T015 (effects + owned scheduling proof)
  -> US1 T016-T027 (private graph and headless zero-I/O acceptance)
  -> US2 T028-T037 (control states and ten-run timing acceptance)
  -> US3 T038-T046 (pure comparison and complete safe evidence)
  -> US4 T047-T061 (definition persistence + final desktop/transition integration)
  -> Final T062-T070 (target suites, manual evidence, docs, CI and completion)
```

- T005-T008 introduce model contracts before executable graphs; T005-T007 may be designed together but are not marked
  parallel because the composite scenario references the other definitions. Test-only parallel groups below use distinct files.
- T009-T010 prove the shared effect boundary before T023/T024. T011-T015 prove owned scheduler/production regressions
  before any private factory or UI execution. New tests may use small test-owned stubs for their already documented contract.
- T016-T018 precede US1 implementation. T019 validation precedes T020-T024 construction. T025 precedes T026 capture/execution.
  Shared source files are edited sequentially; no two tasks may edit the same fixture/service concurrently.
- US2 requires US1 and foundations; US3 requires the resulting typed trace/control terminal states. Their algorithm tests
  are independently repeatable with test-owned data after prerequisites; stories are not claimed independent of their graph.
- US4's persistence/editing tests can be prepared after the Domain contracts, but its combined editor/departure execution
  depends on US1-US3. T050 precedes T055-T058; T055's narrow guard precedes each transition integration.
- T059 active XAML follows backend zero-I/O/control/comparison proofs. T060 follows VM/page creation, and T061 precedes final
  end-to-end evidence. Compile normal host dependencies for transition/DI integration.
- Every slice repeats relevant regressions, secrets/line-ending checks and draft/current-head CI gates from T067-T068.
  Final tests do not replace a slice's own evidence. A new failure/change requires the relevant recheck.
- Maintainer design acceptance/integration and app-launch/manual approval remain distinct from task format/analysis checks.
  Never merge without authorization. No hardware/deployment or sample creation belongs to this backlog.

## Parallel Examples

| Story | Eligible tasks after its prerequisites | Boundary |
| --- | --- | --- |
| US1 | T016 validation tests, T017 isolation tests, T018 feedback tests | Separate fixtures; no simultaneous factory/manager edits |
| US2 | T028 controls, T029 determinism, T030 disposal, T031 clock tests | US1/foundational contracts ready; independent files |
| US3 | T038 predicate/count, T039 ordered/state, T040 trace/error tests | US2 typed trace/terminal contracts ready |
| US4 | T047 serialization, T048 editing, T049 VM, T050 transition tests | Domain contracts/isolated feature contracts ready; later UI integration remains sequential |

These are implementation opportunities, not agent dispatches. Test scaffolding must use run-owned collaborators.
Do not run portable/Windows builds concurrently in one physical worktree if their generated files conflict.

## Independent Acceptance and MVP

| Story | Acceptance checkpoint |
| --- | --- |
| US1 (12 tasks) | T027: explicit seed/all-journey state, production feedback/workflows/whistles and zero external calls |
| US2 (10 tasks) | T037: ten equal normalized traces/results at varied speeds, exact pause/step and joined cancel/restart |
| US3 (9 tasks) | T046: partial/count/window/absence/ordered comparisons, first mismatch, complete safe evidence and distinct errors |
| US4 (15 tasks) | T061: temporary solution roundtrip/deep duplicate, zero run saves, editor flow and awaited context barriers |

Setup has 3 tasks, foundations 12 and final validation 9: 70 total, with 14 test tasks marked [P].
US1 is the first useful headless engineering increment. The full specified MVP requires all four stories and final
automated/CI/manual gates; no isolated backend increment is reported as the complete Windows feature.

## Implementation Strategy

Use narrow task branches/worktrees and draft PRs on integrated predecessors. Deliver effect isolation/production regression
first, then owned scheduling, the private graph, controls/trace/comparison, definition persistence and the final editor.
Keep #35 as primary traceability; task issues are not generated by this step. If later requested, use the canonical
TNNN issue workflow without duplicating this backlog. Record actual outcomes in quickstart rather than checking tasks
because their descriptions exist.

## Constraints and Notes

Use constructor injection, the existing serializers/services/command boundaries and CommunityToolkit patterns. Preserve
zero-speed safe startup/configuration/layout semantics, centralized application EventBus dispatch and current action lists.
Keep Domain/Backend/SharedUI platform-neutral and identifiers ASCII. No sync-over-async, raw script argument/derivative
capture, sensitive absolute paths, local Sonar code analysis, migration, legacy path, replay import, retry/graph engine,
new dependency, public runtime lifecycle API or generic lifecycle registry.
Andreas creates the example solution himself; test-owned temporary fixtures are the only solution data used by automation.
