# Issue #35 Automation Testbench Implementation Plan

## Document status

- Status: Proposed; implementation blocked by the readiness gates below
- GitHub owner: [Issue #35 - Add AutomationTestbenchPage for safe event and workflow simulation](https://github.com/ahuelsmann/MOBAflow/issues/35)
- Priority: P1
- Plan/issue relationship: one plan for one actionable issue
- Delete this plan after Issue #35 is accepted and closed; Git history and the closed issue remain the record
- Planning baseline: versioned repository state on `main` as inspected on 2026-07-20
- Local inspection: pending because the mandatory Sonar secrets scan is unavailable without authentication

## Outcome

Deliver a deterministic AutomationTestbenchPage that runs persisted automation scenarios against an isolated clone of the selected project. It must exercise production journey, workflow, validation, and event-facing services through replaceable interfaces while making live Z21, MOBApi, speaker, script process, filesystem side effects, and physical display access impossible by construction.

The page is the final delivery surface. The primary deliverable is the platform-neutral isolated runtime, virtual-time scheduler, recording effect boundary, scenario validator, trace, and assertion comparator.

## Scope

### In scope

- Persisted named scenarios owned by a project.
- Controlled initial feedback, journey, train, and relevant domain/runtime state.
- Timed and step-based synthetic feedback/runtime events.
- Deterministic play, pause, single-step, cancel, restart, and virtual-time speed.
- Production journey and workflow execution behind testbench-safe interfaces.
- Recording of intended commands, signal/turnout operations, announcements, audio, scripts, display actions, and workflow lifecycle events.
- Typed partial assertions and expected-versus-actual comparison.
- Complete correlated trace with first-mismatch reporting.
- WinUI editor and execution page.
- Schema, sample data, automated tests, and operator documentation.

### Out of scope

- Sending any command to live hardware or external services.
- Executing PowerShell or other scripts.
- Replacing the production workflow engine with a testbench-only engine.
- RecorderPage import in the MVP.
- Automatic scenario generation.
- Hardware acceptance testing.
- Backward-compatibility adapters or migrations for older solution schema versions.

## Readiness gates

Implementation must not begin until all gates applicable to the selected slice are satisfied.

| Gate | Requirement | Blocks |
| --- | --- | --- |
| G1 | RF-01 through RF-05 are complete according to the approved refactoring sequence. RF-04 must provide deterministic ordered Z21 event processing and defined overload/shutdown behavior. | All implementation beyond research |
| G2 | Issue #32 Workflow 2.0 domain/executor contracts and structured lifecycle-event shape are stable. Cancellation, retries, failure policies, and deterministic ordering must have one production owner. | Runner, assertions, trace |
| G3 | The isolated-runtime and external-effect contracts in this plan are accepted. | Runtime and handler changes |
| G4 | The current solution schema version after Issue #32 is known; Issue #35 owns exactly one subsequent schema update with no migration path. | Persistence slice |
| G5 | The implementation workspace passes the mandatory secret scan and the current local instruction/plan consolidation is reconciled. | Any local file read or edit |
| G6 | The plan is linked from Issue #35 and `plan-required` remains present until implementation completion. | Implementation start |

Slice 1 research/design may be refined before G1 and G2. No production-code implementation starts before G1, G3, G5, and G6. The runner does not start before G2.

## Dependencies and sequencing

### Hard dependencies

- RF-04 Ordered Z21 event pipeline: required for production-parity ordering semantics.
- Issue #32 slices 1 and 2: required for retries, cancellation, failure policies, deterministic workflow execution, and structured lifecycle events.
- Current-schema coordination with Issue #32: both features alter persisted workflow/scenario data directly without compatibility migration.

### Program sequencing dependencies

The repository quality plan places RF-01 through RF-05 before broad feature development. These packages are not all technical dependencies of the testbench, but they are release-program gates.

### Soft dependencies and consumers

- Issue #30 RecorderPage: align event envelope, correlation, scheduling, and isolated replay terminology; do not block the MVP.
- Issue #34 interlocking: consume the testbench as the preferred simulation and safety-regression surface before hardware-active interlocking acceptance.
- Issue #36 display interface: capture current display intents in the MVP, then extend typed capability-aware assertions after the interface stabilizes.
- Issues #31 and #33: future scenario consumers; no MVP dependency.

## Architecture decisions

### AD-1: Dedicated fail-closed runtime scope

Decision: construct every test run through an `IAutomationTestbenchRuntimeFactory` that creates a dedicated disposable runtime scope. The scope receives a canonical deep clone of the selected project and owns its own EventBus, journey state store, virtual time, workflow executor dependencies, trace collector, and recording effects.

The scope must not resolve or reference the root `IMobaRuntime`, production `IZ21`, MOBApi clients, physical `ISpeakerEngine`, production script launcher, physical `IFrameSender`, or file-backed journey state store.

Rationale: a UI mode flag around the production singleton is not a sufficient safety boundary. Dependency construction must make live effects impossible even when a future handler forgets a runtime-mode check.

Alternatives rejected:

- Reusing `MobaRuntimeService.SimulateFeedbackAsync`: it routes through the production Z21 instance and active runtime.
- Temporarily disconnecting Z21: disconnect state is mutable and does not prevent other external effects.
- Handler-specific UI checks: distributed checks are not fail-closed and are easy to bypass.

### AD-2: Production handlers call a typed effect boundary

Decision: introduce a platform-neutral `IWorkflowEffectSink` used by production workflow action handlers after payload resolution and validation. Provide:

- `ProductionWorkflowEffectSink` for live Z21, audio, announcement, script, and display adapters.
- `RecordingWorkflowEffectSink` for testbench runs; it records typed intents and never performs I/O.

State-only actions such as journey-stop transitions remain production domain operations and do not go through the external-effect sink.

The script effect records path and argument intent only in the testbench. Process creation moves behind the production sink so `ExecuteScriptWorkflowActionHandler` no longer calls `Process.Start` directly.

Rationale: the production ActionExecutor and handlers remain the single engine, while effects become replaceable and testable.

Alternative rejected: registering separate testbench-only action handlers, because that would test a second behavior path rather than the production handlers.

### AD-3: One time abstraction for production and testbench

Decision: pass `TimeProvider` and `CancellationToken` through workflow, journey, retry, timeout, and automation-delay paths. Use the .NET time-provider-aware delay APIs. Implement a controlled testbench time provider/scheduler that supports:

- automatic advance at a configured speed;
- pause without completing future delays;
- advancing to the next scheduled operation for single-step;
- stable ordering by `DueTime`, explicit scenario order, and monotonic internal sequence;
- cancellation of all pending waits;
- restart from a newly cloned initial snapshot.

No testbench code may rely on wall-clock time, `DateTime.Now`, `DateTimeOffset.UtcNow`, or unqualified `Task.Delay`.

### AD-4: Synthetic provenance is explicit

Decision: every injected input is wrapped in an immutable `AutomationTestInputEnvelope` containing scenario ID, run ID, input ID, virtual timestamp, explicit order, event kind, typed payload, and `Origin = SyntheticTestbench`.

The runner converts the payload to the production-facing event/feedback contract inside the isolated scope. Trace entries retain the original envelope and provenance. Synthetic events are never published on the root application EventBus.

### AD-5: Deterministic trace and comparison

Decision: trace entries use a run-local monotonically increasing sequence and carry:

- virtual timestamp;
- correlation/run/input/workflow/action IDs;
- source and event kind;
- typed payload;
- outcome and optional error category;
- state-before/state-after references where relevant.

Comparison sorts by trace sequence, not wall-clock timestamp. Partial assertions match only declared fields. Ordered assertions consume matching entries in order. The result reports the first mismatch but retains the complete trace and all comparison results.

### AD-6: Production state remains immutable from the testbench

Decision: clone the selected project through canonical `JsonOptions` serialization at run start. Store journey/runtime state in memory. Snapshot the root production runtime before and after each run and assert that no relevant production state changed.

Scenario editing changes the editor project only through ViewModel commands and the existing solution save path. Executing a scenario never mutates the editor project or production runtime.

### AD-7: Schema ownership

Decision: add `AutomationTestScenarios` to `Project`, initialized to an empty collection. Increment `Solution.CurrentSchemaVersion` once in the persistence slice, update `MOBAflow/solution.json` and `MOBAflow/Build/Schemas/solution.schema.json` together, and add no compatibility or migration path.

Because Issue #32 also changes the schema, the implementation rebases after #32 and applies the next version from the then-current value rather than assuming version 4.

### AD-8: UI remains a thin MVVM surface

Decision: use a specialized `AutomationTestbenchViewModel` for editor and ephemeral run state. Commands own create, duplicate, delete, validate, run, pause, step, cancel, restart, and assertion editing. Code-behind performs only WinUI view adaptation. EventBus handlers rely on `UiThreadEventBusDecorator` and do not dispatch manually.

The page and ViewModel lifecycle must be explicitly selected during implementation. A transient run ViewModel is preferred unless product review requires run-state preservation across navigation.

## Data model

### AutomationTestScenario

- `Id: Guid`
- `Name: string`
- `Description: string?`
- `ProjectSnapshotFingerprint: string?` for diagnostics, not as a compatibility mechanism
- `InitialState: AutomationTestInitialState`
- `Inputs: List<AutomationTestInput>`
- `Assertions: List<AutomationTestAssertion>`
- `DefaultPlaybackRate: double`
- `Enabled: bool`

Validation:

- ID must be non-empty and unique within the project.
- Name must be non-empty and unique using the repository's normal name comparison.
- Playback rate must be finite and within an explicitly documented safe range.
- References must resolve against the selected project.
- Input IDs and assertion IDs must be unique.
- Input timestamps must be non-negative.

### AutomationTestInitialState

- feedback values keyed by stable feedback identity or InPort as supported by the current domain;
- selected journey and its position/occurrence state;
- selected train and locomotive runtime state;
- relevant signal/turnout/domain state supported by the current runtime;
- optional deterministic seed.

Validation rejects contradictory, missing, deleted, or unsupported references before constructing a runtime.

### AutomationTestInput

- `Id: Guid`
- `At: TimeSpan`
- `Order: int`
- `Kind: AutomationTestInputKind`
- typed payload appropriate to the kind
- optional label

Ordering key: `At`, then `Order`, then stable list position captured at validation time. Duplicate full ordering keys are validation errors rather than sources of nondeterminism.

### CapturedWorkflowEffect

- run sequence and virtual timestamp;
- input/workflow/action correlation;
- effect kind;
- typed payload;
- status: intended, rejected, failed, or cancelled;
- no live credential, audio data, file contents, or private network data.

### AutomationTestAssertion

- `Id: Guid`
- assertion kind;
- expected occurrence constraint;
- ordering group/index when ordered;
- typed partial payload matcher;
- optional virtual-time bound;
- optional negation for forbidden effects.

### AutomationTestRunResult

- run ID and scenario ID;
- start/end virtual time;
- terminal state: passed, failed, cancelled, invalid, or runner error;
- complete trace;
- assertion results;
- first mismatch reference;
- production-state isolation result.

### Runner state machine

`Idle -> Validating -> Ready -> Running <-> Paused -> Completed|Failed|Cancelled`

- Single-step is an operation while Paused and returns to Paused.
- Restart is allowed only after cancellation/completion/failure and creates a fresh runtime scope.
- Editing a scenario while Running or Paused is blocked.
- Disposal from any non-terminal state cancels and disposes the isolated scope.

## Contracts

### IAutomationTestbenchRuntimeFactory

Responsibilities:

- clone and validate the project snapshot;
- create only isolated dependencies;
- reject construction if a production effect implementation is present;
- return an async-disposable run scope.

### IAutomationTestbenchRunner

Operations:

- `ValidateAsync`
- `StartAsync`
- `PauseAsync`
- `StepAsync`
- `CancelAsync`
- `RestartAsync`

All asynchronous operations accept and propagate `CancellationToken`. Concurrent state-changing operations are serialized and invalid transitions return typed results.

### IWorkflowEffectSink

Typed operations cover:

- raw/semantic Z21 commands;
- locomotive functions and drive commands;
- signal and turnout operations;
- announcements;
- audio playback intents;
- PowerShell/script intents;
- display/frame actions.

The recording implementation has no dependency on network, process, filesystem, audio, or display implementations.

### IAutomationTestClock

Built on or compatible with `TimeProvider`; exposes current virtual time, pending-operation inspection, controlled advancement, pause/resume, and cancellation. Production services consume `TimeProvider`, not the testbench-specific control API.

### IAutomationTraceSink

Append-only run-local trace. Sequence allocation must be atomic and deterministic within the single scheduler/runner pipeline. Consumers receive immutable snapshots.

### IAutomationAssertionEvaluator

Pure comparison service from validated assertions plus immutable trace/state snapshots to a deterministic result. It performs no I/O and has no runtime dependency.

## Existing files expected to change

Final line-level changes must be confirmed against the implementation baseline before editing.

### Domain and schema

- `Domain/Project.cs`: persisted scenario collection.
- `Domain/Solution.cs`: schema version increment in the persistence slice.
- `MOBAflow/Build/Schemas/solution.schema.json`: complete scenario and assertion schema.
- `MOBAflow/solution.json`: English sample scenario only if a useful minimal sample is approved.

### Backend execution seams

- `Backend/Interface/IWorkflowService.cs`: cancellation/time contract aligned with Issue #32.
- `Backend/Service/WorkflowService.cs`: time-provider delays, cancellation, and lifecycle correlation; avoid duplicating Issue #32 changes.
- `Backend/Service/ActionExecutionContext.cs`: effect sink and run correlation dependencies.
- `Backend/Service/WorkflowActionHandlers.cs`: typed effect sink calls; remove direct process launch from the handler.
- `Backend/Manager/JourneyManager.cs`: time provider, cancellation, injected synthetic feedback path, and in-memory state compatibility.
- `Backend/Extensions/MobaBackendServiceCollectionExtensions.cs`: production registrations only; testbench isolated registrations belong in a dedicated factory/extension.
- `Common/Events/IEventBus.cs`: change only if Issue #32/RF-04 establishes an async ordered contract; do not introduce testbench-specific branches.

### SharedUI and WinUI

- `SharedUI/ViewModel/MainWindowViewModel.Solution.cs`: only if selection/save notifications require integration; do not add testbench behavior to the root ViewModel.
- `MOBAflow/Service/NavigationRegistration.cs`: page registration and English navigation metadata.
- `MOBAflow/Extensions/MobaWinUiServiceCollectionExtensions.cs`: runner, factory, and ViewModel registrations.
- `MOBAflow/Extensions/WinUiDiContainerValidator.cs`: resolve the page/ViewModel without constructing live testbench effects.

## Planned new files

Names follow existing repository layout and must be verified immediately before creation.

### Domain

- `Domain/AutomationTestScenario.cs`
- `Domain/AutomationTestInput.cs`
- `Domain/AutomationTestAssertion.cs`
- `Domain/AutomationTestRunResult.cs`

### Backend

- `Backend/Interface/IAutomationTestbenchRunner.cs`
- `Backend/Interface/IWorkflowEffectSink.cs`
- `Backend/Service/AutomationTestbenchRunner.cs`
- `Backend/Service/AutomationTestbenchRuntimeFactory.cs`
- `Backend/Service/AutomationTestClock.cs`
- `Backend/Service/AutomationTraceCollector.cs`
- `Backend/Service/AutomationAssertionEvaluator.cs`
- `Backend/Service/ProductionWorkflowEffectSink.cs`
- `Backend/Service/RecordingWorkflowEffectSink.cs`

### SharedUI and WinUI

- `SharedUI/ViewModel/AutomationTestbenchViewModel.cs`
- `MOBAflow/View/AutomationTestbenchPage.xaml`
- `MOBAflow/View/AutomationTestbenchPage.xaml.cs`

### Tests

- `Test/Domain/AutomationTestScenarioTests.cs`
- `Test/Backend/AutomationTestClockTests.cs`
- `Test/Backend/AutomationTestbenchIsolationTests.cs`
- `Test/Backend/AutomationTestbenchRunnerTests.cs`
- `Test/Backend/AutomationAssertionEvaluatorTests.cs`
- `Test/SharedUI/AutomationTestbenchViewModelTests.cs`

Existing workflow, journey, schema, DI, and integration fixtures receive focused regression cases rather than duplicating entire suites.

## Delivery slices

### Slice 1: Prerequisite contracts and characterization tests

Goal: make the existing execution behavior explicit before changing seams.

- Rebase after RF-04 and the Issue #32 core contracts.
- Add characterization coverage for current sequential/parallel ordering, action payload resolution, journey feedback transitions, error handling, and project runtime cloning.
- Finalize `IWorkflowEffectSink`, time, correlation, and cancellation contracts without changing user-visible behavior.
- Verify that every external action type has exactly one effect-boundary path.

Exit criteria:

- contracts are reviewed;
- production behavior is characterized;
- no second workflow engine exists;
- no external effect remains hidden inside a handler.

### Slice 2: Scenario model and validation

Goal: persist and validate scenario definitions without executing them.

- Add domain entities and collection initialization.
- Add pure validation for references, ordering, ranges, contradictory initial state, malformed assertions, and unsupported event/effect types.
- Add typed deep-clone/fingerprint helpers only where canonical serialization does not already provide them.
- Defer schema version change until the persistence slice is rebased on Issue #32.

Exit criteria:

- complete domain tests;
- malformed scenarios cannot reach runner construction;
- no platform references in Domain/Common/Backend.

### Slice 3: Virtual time, trace, and recording effects

Goal: prove deterministic scheduling and zero I/O independently of the full runner.

- Implement controllable time and stable simultaneous-event ordering.
- Implement immutable correlated trace.
- Route production action handlers through `IWorkflowEffectSink`.
- Add production adapters preserving current behavior.
- Add recording sink and negative tests proving no network, process, filesystem, audio, or display dependency is reachable.

Exit criteria:

- repeated schedules produce byte-for-byte-equivalent normalized traces;
- cancellation clears pending operations;
- script actions are captured without process creation;
- existing workflow tests remain green.

### Slice 4: Isolated runner and production service integration

Goal: execute feedback-to-journey-to-workflow scenarios inside a disposable isolated scope.

- Build the isolated runtime factory with cloned project, private EventBus, in-memory journey state, test clock, recording effects, and Issue #32 executor.
- Inject synthetic envelopes without using the root runtime or root EventBus.
- Implement state machine operations and restart-from-clean-snapshot.
- Capture workflow lifecycle and domain transitions.
- Compare production runtime snapshots before and after each run.

Exit criteria:

- no Z21 connection is required;
- isolation-negative tests fail if any production effect registration is introduced;
- pause, step, cancellation, retry, timeout, and simultaneous events are deterministic;
- production state is unchanged after pass, failure, and cancellation.

### Slice 5: Assertion engine and diagnostics

Goal: produce actionable deterministic results.

- Implement typed partial matching, occurrence constraints, ordered groups, negative assertions, virtual-time bounds, and final-state assertions.
- Identify the first mismatch while retaining complete trace and comparison detail.
- Add correlation navigation between input, workflow, step/action, transition, and effect.

Exit criteria:

- evaluator is pure and fully unit tested;
- malformed expectations are distinguished from failed expectations;
- trace remains complete after first mismatch.

### Slice 6: Persistence and schema

Goal: save and reopen complete scenarios.

- Rebase on the current schema after Issue #32.
- Add `Project.AutomationTestScenarios`.
- Increment schema version exactly once.
- Update schema and sample solution atomically.
- Add canonical serialization, schema validation, and solution save/load roundtrip tests.
- Do not add legacy migration code.

Exit criteria:

- saved scenarios round-trip with all inputs and assertions;
- malformed JSON is rejected with actionable diagnostics;
- current schema and sample data validate in build checks.

### Slice 7: ViewModel and AutomationTestbenchPage

Goal: expose safe scenario authoring and execution controls.

- Implement specialized ViewModel with commands and CanExecute state derived from runner state.
- Add master-detail scenario editor, initial-state editor, ordered input timeline, assertion editor, execution controls, result summary, and trace.
- Register page, ViewModel, navigation metadata, feature toggle only if the existing product convention requires one, and DI validation.
- All UI strings are English.
- Use `ThemeResource`, accessible names, keyboard operation, text trimming, and responsive/scroll-safe layout.
- Keep code-behind limited to view adaptation.

Exit criteria:

- create/edit/duplicate/delete/save/reopen flow works;
- live runtime ambiguity is visibly blocked;
- Light, Dark, and High Contrast checks pass;
- keyboard and Narrator validation is documented;
- XAML compiler contains the page and no `<Page Remove>` entry exists.

### Slice 8: Optional integrations

Not part of MVP acceptance:

- derive a scenario from RecorderPage recordings after Issue #30;
- add interlocking-focused scenario templates after Issue #34;
- add capability-aware display assertions after Issue #36.

## Test strategy

### Domain and serialization

- default collections and stable IDs;
- duplicate and malformed input/assertion validation;
- missing/deleted project references;
- stable simultaneous-event ordering;
- complete JSON roundtrip;
- solution schema validation and current-version rejection behavior.

### Time and concurrency

- delay completion only after virtual advance;
- pause prevents completion;
- single-step advances exactly one next operation group;
- cancellation completes no later effects;
- restart creates new run/correlation IDs and clean state;
- bounded retries and timeouts;
- deterministic same-time ordering;
- concurrent command serialization and invalid runner transitions.

### Isolation and safety

- isolated provider contains no production Z21/UDP client, MOBApi client, physical speaker, process launcher, physical frame sender, or file journey store;
- raw command, signal, turnout, audio, announcement, script, and display actions produce captured intents only;
- PowerShell test proves zero process starts;
- network test proves zero UDP/HTTP/SignalR sends;
- forbidden-effect assertion remains zero;
- root EventBus receives no synthetic test event;
- production runtime snapshot is identical before and after pass/fail/cancel.

### Workflow and journey integration

- feedback selects the correct journey step;
- journey position and stop transitions;
- sequential and parallel workflow ordering from Issue #32;
- retry, continue, stop, failure branch, nested workflow, cancellation, and timeout;
- structured lifecycle correlation;
- action validation failure before any effect intent.

### Assertion evaluator

- exact and partial typed payload match;
- positive, negative, occurrence, ordered, and time-bounded assertions;
- first mismatch selection;
- complete trace preservation;
- malformed assertion versus runtime failure;
- deterministic normalized results across repeat runs.

### ViewModel and WinUI

- command CanExecute for every runner state;
- editing disabled during active run;
- selection and project change handling;
- save notification integration;
- DI resolution and page registration;
- XAML compile;
- English visible strings;
- Light/Dark/High Contrast, keyboard, focus, Narrator, scaling, and non-color-only status indicators.

## Validation quickstart

Implementation is not complete until the following sequence is documented with exact observed results.

1. Run focused platform-neutral tests for Domain, Backend, Common, and SharedUI.
2. Run `dotnet test Test/Test.csproj`.
3. Restore and run the WinUI FastDebug compile check:
   `dotnet build MOBAflow/MOBAflow.csproj -c FastDebug --no-restore /p:BuildMOBApiDependency=false /p:CopyMOBApiToOutput=false`.
4. Run repository JSON validation for `MOBAflow/solution.json` and `MOBAflow/Build/Schemas/solution.schema.json`.
5. Resolve the full WinUI DI container and AutomationTestbenchPage in the existing DI tests.
6. Run an automated safety scenario containing every external effect type and prove that all are captured while live-effect counters remain zero.
7. Run the same scenario twice from the same project snapshot and compare normalized trace/result output for equality.
8. Cancel once during delay, retry, and parallel execution; prove no later effects and unchanged production state.
9. Manually validate create/save/reopen/run/pause/step/restart plus Light, Dark, High Contrast, keyboard, focus, Narrator, and text scaling.
10. Confirm no unintended `<Page Remove>` entry exists and the XAML compiler generated the page.

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Live side effect escapes isolation | Dedicated fail-closed runtime scope, negative DI tests, recording-only sink, zero-I/O assertions |
| Workflow 2.0 changes invalidate the runner | Hard gate on Issue #32 core contracts; no duplicate executor |
| Real time leaks into deterministic paths | TimeProvider and cancellation characterization; analyzer/code-search validation for wall-clock calls |
| Parallel actions produce unstable traces | One scheduler ordering rule and run-local monotonic sequence |
| Script handler launches a process | Move process creation behind production effect sink; recording sink has no process dependency |
| Test run mutates editor or production state | Canonical deep clone, in-memory stores, before/after snapshot assertion |
| Schema conflicts with Issues #31-#34 | Rebase immediately before persistence slice; one atomic version increment |
| Trace leaks private data | Typed allow-listed payloads; no credentials, file contents, audio data, or unnecessary endpoints |
| Page grows into code-behind behavior | Commands and specialized ViewModel; code-behind limited to input/view adaptation |
| Testbench becomes a second engine | Production workflow/journey services remain the only execution owners |

## Documentation and issue traceability

During delivery:

- Each pull request names Issue #35 as its primary work item and identifies the delivery slice.
- Secondary dependency references use `Refs #32`, `Refs #30`, `Refs #34`, or `Refs #36`; they do not close those issues.
- GitHub owns status and acceptance criteria. This plan owns technical sequence, architecture decisions, risks, and validation.
- Update `docs/ARCHITECTURE.md` when the isolated runtime and effect boundary are implemented.
- Update user documentation when the page becomes available.
- Record exact test/build/manual-validation evidence in the final pull request.
- Delete this plan when Issue #35 is closed.

## Implementation start checklist

- [ ] RF-01 through RF-05 complete, including RF-04
- [ ] Issue #32 executor/lifecycle contracts stable
- [ ] Current schema baseline confirmed
- [ ] Mandatory local secret scan succeeds
- [ ] Local instruction and plan consolidation reconciled
- [ ] Plan reviewed and linked from Issue #35
- [ ] `plan-required` present
- [ ] Slice 1 affected files re-verified against current code
- [ ] No unrelated working-tree changes overlap the slice
