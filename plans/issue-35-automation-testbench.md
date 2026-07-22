# Issue #35 Automation Testbench Implementation Plan

## Document status

- Status: Proposed; implementation blocked by the readiness gates below
- GitHub owner: [Issue #35 - Add AutomationTestbenchPage for safe event and workflow simulation](https://github.com/ahuelsmann/MOBAflow/issues/35)
- Priority: P1
- Plan/issue relationship: one plan for one actionable issue
- Delete this plan after Issue #35 is accepted and closed; Git history and the closed issue remain the record
- Planning baseline: clean `github/main` at `677ba975`, inspected locally on 2026-07-22 after Workflow 2.0 PR #74, RecorderPage PR #76, and RF-02 follow-up PR #78
- Local inspection: refreshed for the instruction, workflow graph/execution, action/effect, journey, recording/replay, event, DI, schema, and test seams listed below; every file was scanned individually before reading
- Secrets scan: `sonar analyze secrets <path>` works non-interactively and reported no findings for every inspected file
- Review follow-up: incorporates all ten actionable comments from merged planning PR #52 and corrects all six actionable comments left unresolved on merged baseline PR #66

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
- Feature-specific legacy compatibility branches. Additive JSON evolution follows the repository compatibility rules; any genuinely breaking change requires a separately approved upgrade path.

## Readiness gates

Implementation must not begin until all gates applicable to the selected slice are satisfied.

| Gate | Requirement | Blocks |
| --- | --- | --- |
| G1 | RF-01 through RF-05 are complete according to the approved refactoring sequence. RF-01, RF-02, RF-04, and RF-05 are complete; RF-03/#50 remains open. | All implementation beyond research |
| G2 | Issue #32 Workflow 2.0 domain/executor contracts and structured lifecycle-event shape are stable. Satisfied by merged PR #74; the testbench must consume those contracts rather than recreate them. | Satisfied; re-verify at implementation start |
| G3 | The isolated-runtime and external-effect contracts in this plan are accepted. | Runtime and handler changes |
| G4 | The post-#32 schema baseline is version 4. The optional initialized scenario collection remains additive under the repository JSON compatibility rules; recheck immediately before the persistence slice, and require an approved upgrade path for any final breaking change. | Persistence slice |
| G5 | The implementation workspace passes the mandatory secret scan and the current local instruction/plan consolidation is reconciled. | Any local file read or edit |
| G6 | The plan is linked from open Issue #35 and `plan-required` remains present until implementation completion. | Implementation start |

Research and plan refinement may continue before G1. No production-code or characterization-test implementation starts before G1, G3, G5, and G6. G2 is satisfied but its merged contracts must be re-verified after the remaining programme gate lands.

## Dependencies and sequencing

### Hard dependencies

- RF-04 Ordered Z21 event pipeline: satisfied by #43; its bounded FIFO and failure-isolation behavior remains the production-parity event source.
- Workflow 2.0 domain, executor, cancellation, retry, failure-policy, dry-run, and lifecycle contracts: satisfied by #32/PR #74 and now owned by `WorkflowService`, `WorkflowExecutionCoordinator`, and `WorkflowLifecycleEvent`.
- RF-03/#50: not a direct testbench code dependency, but it remains the final open RF-01-through-RF-05 programme gate and therefore blocks implementation under G1.
- Current schema: post-#32 version 4 is the confirmed baseline. The scenario collection is still additive, but the classification is repeated immediately before Slice 6.

### Program sequencing dependencies

The repository quality plan places RF-01 through RF-05 before broad feature development. RF-03/#50 is the only remaining open package. It is a governance/release-program gate even though it is not a direct technical dependency of the testbench.

### Soft dependencies and consumers

- Issue #30 RecorderPage: PR #76 is merged and supplies recording payloads, ordered capture, command correlation, replay scheduling, a live-runtime safety gate, and a dependency-free replay projection. Reuse those contracts and terminology where compatible, but do not treat `IsolatedReplayRuntime` as a production workflow/journey runtime. Remaining Issue #30 work does not block the MVP.
- Issue #34 interlocking: consume the testbench as the preferred simulation and safety-regression surface before hardware-active interlocking acceptance.
- Issue #36 display interface: capture current display intents in the MVP, then extend typed capability-aware assertions after the interface stabilizes.
- Issues #31 and #33: future scenario consumers; no MVP dependency.

## Local Slice 1 baseline (2026-07-22)

This baseline is analysis only. It does not release product implementation while G1 remains open.

### Confirmed execution and isolation seams

- `IWorkflowService` now owns the validated Workflow 2.0 graph API through `WorkflowExecutionRequest` and propagates `CancellationToken`. `WorkflowService` and `WorkflowExecutionCoordinator` use injected `TimeProvider`, time-provider-aware delays, bounded retries, per-source FIFO coordination, dry-run planning, nested workflows, parallel joins, and structured lifecycle events.
- Live parallel branches still use `Task.WhenAll`, and `WorkflowLifecycleEvent.Sequence` is allocated under a shared lock in arrival order. The lifecycle contract has correlation IDs and an explicit `TimestampUtc`, but no stable branch/step path that would make live parallel event order independent of task interleaving. Slice 1 must characterize this before defining deterministic testbench merge keys.
- `ActionExecutionContext` still requires the live `IZ21` service and carries optional live speaker and sound-player services. `ActionExecutionContextFactory.Create` allocates a new wrapper but shallow-copies `Project`, `Journey`, `JourneySessionState`, `Station`, and `Platform` references from `ActionExecutionContextState`; it does not isolate that mutable state. The testbench deep clone remains the actual isolation boundary.
- Workflow handlers perform effects directly: raw commands and signal aspects call `context.Z21`, audio calls `ISoundPlayer`, announcements call `IAnnouncementService`, and scripts perform file checks and start a PowerShell process.
- `WorkflowEffectPlanner` already performs pure payload validation and emits typed effect categories/resource descriptors for dry runs, but live handlers do not consume a replaceable effect sink. Slice 1 must extend this single planning vocabulary rather than introduce a competing effect taxonomy.
- Audio and script handlers still call `IFileSystem.FileExists`, and the script handler starts a process. Environment-dependent checks and all I/O must move behind the production effect boundary for zero-filesystem testbench runs.
- `JourneyManager` subscribes directly to `IZ21.Received`, serializes feedback mutation with a `SemaphoreSlim`, queues Workflow 2.0 runs through `WorkflowExecutionCoordinator`, and publishes immutable journey transitions before legacy callbacks. It still records `JourneySessionState.LastFeedbackTime` with `DateTime.Now`; production DI still defaults to a file-backed runtime state store.
- `LocomotiveWhistleAutomationService` already accepts `TimeProvider` and cancellation, but its production `ILocomotiveFunctionCommandGateway` delegates to the root `IMobaRuntime`. The isolated scope therefore needs a recording gateway registration, not a second whistle implementation.
- `WorkflowLifecycleEvent.TimestampUtc` already uses the injected workflow `TimeProvider`, while inherited `EventBase.CreatedUtc` and journey runtime events still use wall-clock construction. The testbench should prefer explicit domain timestamps and add a production-event construction seam only where a required event has no deterministic timestamp field.
- RecorderPage now provides `RecordingEventBusDecorator`, `RecordingRuntimeCommandGateway`, `TimeProviderRecordingReplayDelayScheduler`, `RecordingReplaySafetyGate`, and dependency-free `IsolatedReplayRuntime`. These are reusable safety, payload, ordering, and scheduling references, but replay only projects allow-listed journal payloads into private state and does not run production journey/workflow services.
- `AddMobaBackendServices` registers production Z21, file stores, handlers, runtime, action context, locomotive gateway, whistle automation, recording services, replay services, and the Workflow 2.0 graph as one root service graph. The testbench factory must build a separate fail-closed graph instead of decorating this provider with a mode flag.
- `Project` contains Workflow 2.0 graphs but no automation-scenario collection. `Solution.CurrentSchemaVersion` is 4; an initialized optional scenario collection remains additive under the current exact-version policy.

### Existing regression coverage to preserve

- `WorkflowGraphExecutionTests`: dry-run effect planning, condition branches, bounded retry, failure branches, persisted-order dry-run parallel effects, nested correlation, cancellation, and validation rejection.
- `WorkflowServiceTests` and `WorkflowExecutionEndToEndTests`: compatibility entry point, cancellation rejection, graph execution, command execution, and lifecycle completion behavior.
- `WorkflowActionHandlersTests`: command, audio, announcement, signal-aspect, script-file, journey-stop, and cancellation propagation behavior.
- `JourneyManagerFeedbackTests`: unexpected inputs, repeats, stop-transition ordering, independent-source concurrency, same-source serialization, source correlation, cancellation, stable completion, and structured-event-before-legacy-callback ordering.
- `RecordingReplayServiceTests`: replay order, speed changes, pause/step/seek/cancel, live-hardware blocking, dependency-free runtime construction, and payload allow-listing.
- `LocomotiveWhistleAutomationServiceTests`: delayed activation, project-activation cancellation, pulse coalescing, and validation bounds using the default system clock and real waits. They do not currently prove controlled virtual-time behavior.
- `EventBusTests`: invocation, failure isolation, subscription lifecycle, and event-instance delivery. They do not currently assert subscriber invocation order.
- `MobaBackendServiceCollectionExtensionsTests`: core service resolution and unique workflow-handler types. The double-registration test resolves one singleton twice; it does not prove descriptor-level idempotence or absence of duplicate `IZ21` registrations.

### Characterization tests required before changing existing seams

1. Live parallel Workflow 2.0 branch start/completion and lifecycle sequencing, including simultaneous effects and multiple failures; do not infer deterministic order from `Task.WhenAll` completion or the current locked sequence counter.
2. `ActionExecutionContextFactory` reference-sharing behavior, followed by an explicit proof that the testbench deep clone shares no mutable project, journey, session, station, or platform object.
3. One inventory test proving every external workflow action has exactly one planner descriptor and one production effect path.
4. Audio/script tests separating platform-neutral payload validation from production-only file/process behavior.
5. Journey feedback ordering across state persistence, stop transition, coordinator enqueue, workflow execution, index advancement, cancellation, and completion.
6. Controlled-`TimeProvider` whistle tests covering delay, active duration, coalescing, and cancellation without real waits.
7. An EventBus ordering assertion if testbench behavior relies on subscription order; otherwise the isolated bus contract must explicitly avoid that dependency.
8. Descriptor-level DI multiplicity tests for registrations whose idempotence is required, using service-descriptor counts or `IEnumerable<T>` resolution rather than repeated resolution of one singleton.

### Post-contract integration coverage

These tests require the testbench contracts or isolated provider to exist and therefore are not pre-change characterization prerequisites:

- isolated whistle registration proving feedback-triggered function commands cannot resolve the root runtime;
- synthetic-event construction proving production defaults remain unchanged while testbench-facing timestamps use virtual time;
- DI-negative tests rejecting live Z21, root runtime, file state stores, process, audio, MOBApi, and physical display adapters from an isolated scope.

## Architecture decisions

### AD-1: Dedicated fail-closed runtime scope

Decision: construct every test run through an `IAutomationTestbenchRuntimeFactory` that creates a dedicated disposable runtime scope. The scope receives a canonical deep clone of the selected project and owns its own EventBus, journey state store, virtual time, workflow executor dependencies, trace collector, and recording effects.

The scope must not resolve or reference the root `IMobaRuntime`, production `IZ21`, MOBApi clients, physical `ISpeakerEngine`, production script launcher, physical `IFrameSender`, or file-backed journey state store.

Reuse RecorderPage's dependency-free construction, replay scheduling, allow-listed payload, and safety-gate patterns where their contracts fit. Do not reuse `IsolatedReplayRuntime` as the automation engine: it projects journal payloads into private state and does not execute production Workflow 2.0 or journey services.

Rationale: a UI mode flag around the production singleton is not a sufficient safety boundary. Dependency construction must make live effects impossible even when a future handler forgets a runtime-mode check.

Alternatives rejected:

- Reusing `MobaRuntimeService.SimulateFeedbackAsync`: it routes through the production Z21 instance and active runtime.
- Temporarily disconnecting Z21: disconnect state is mutable and does not prevent other external effects.
- Handler-specific UI checks: distributed checks are not fail-closed and are easy to bypass.

### AD-2: Production handlers call a typed effect boundary

Decision: introduce a platform-neutral `IWorkflowEffectSink` used by production workflow action handlers after payload resolution and validation. Provide:

- `ProductionWorkflowEffectSink` for live Z21, audio, announcement, script, and display adapters.
- `RecordingWorkflowEffectSink` for testbench runs; it records typed intents and never performs I/O.

`WorkflowEffectPlanner` remains the single pure action-validation and effect-description owner. The live and recording sinks consume the same resolved action/effect vocabulary so dry-run, production execution, RecorderPage payloads, and testbench assertions do not diverge into competing taxonomies.

State-only actions such as journey-stop transitions remain production domain operations and do not go through the external-effect sink. Feedback-triggered locomotive commands are also part of the boundary: `ILocomotiveFunctionCommandGateway` receives production and recording implementations, so `LocomotiveWhistleAutomationService` cannot resolve the root runtime in an isolated scope.

Environment-dependent validation belongs to the production effect adapter. Audio and script handlers perform platform-neutral payload validation before calling the sink; only the production sink may check file existence or launch a process. The recording sink performs no filesystem access.

The script effect records an opaque action ID, a sanitized project-relative path or leaf name, and `ArgumentsRedacted = true`. Raw `PowerShellActionPayload.Arguments` is never copied, hashed, logged, persisted, or displayed by the testbench. Process creation moves behind the production sink so `ExecuteScriptWorkflowActionHandler` no longer calls `Process.Start` directly.

Rationale: the production ActionExecutor and handlers remain the single engine, while effects become replaceable and testable.

Alternative rejected: registering separate testbench-only action handlers, because that would test a second behavior path rather than the production handlers.

### AD-3: One time abstraction for production and testbench

Decision: retain the Workflow 2.0 and coordinator `TimeProvider`/`CancellationToken` contracts and extend the same abstraction to remaining journey timestamps and automation-delay paths. Use the .NET time-provider-aware delay APIs. Implement a controlled testbench time provider/scheduler that supports:

- automatic advance at a configured speed;
- pause without completing future delays;
- advancing to the next scheduled operation for single-step;
- stable ordering by `DueTime`, explicit scenario order, and a stable logical operation path;
- a scheduler pump that, after every advance, processes registered continuations and all newly scheduled same-time work to quiescence before selecting the next due operation;
- cancellation of all pending waits;
- restart from a newly cloned initial snapshot.

The pump tracks runner-owned operations explicitly and must not infer quiescence from a single `Task.Yield` or an empty timer queue while registered continuations are still active.

No testbench code may rely on wall-clock time, `DateTime.Now`, `DateTimeOffset.UtcNow`, or unqualified `Task.Delay`. Existing production defaults may remain system-time based where compatibility requires it, but the isolated scope must inject its clock explicitly.

### AD-4: Synthetic provenance is explicit

Decision: every injected input is wrapped in an immutable `AutomationTestInputEnvelope` containing scenario ID, run ID, input ID, virtual timestamp, explicit order, event kind, typed payload, and `Origin = SyntheticTestbench`.

The runner converts the payload to the production-facing event/feedback contract inside the isolated scope. Existing explicit timestamp contracts such as `WorkflowLifecycleEvent.TimestampUtc` receive the testbench `TimeProvider`. For event types that expose only `EventBase.CreatedUtc`, add an explicit construction timestamp only when that event must participate in deterministic assertions; do not change all production event defaults speculatively. Trace entries retain the original envelope and provenance. Synthetic events are never published on the root application EventBus.

### AD-5: Deterministic trace and comparison

Decision: trace entries use a run-local monotonically increasing sequence and carry:

- virtual timestamp;
- correlation/run/input/workflow/action IDs;
- source and event kind;
- typed payload;
- outcome and optional error category;
- state-before/state-after references where relevant.

Concurrent producers do not allocate observable order by racing on an atomic counter. Each emission carries a stable logical key: virtual timestamp, scenario input order, workflow invocation path, branch/step path, action index, and per-action effect index. The runner buffers emissions and performs a deterministic merge at each scheduler quiescence boundary; only then does it allocate display sequence numbers.

The current Workflow 2.0 lifecycle sequence records lock-acquisition order and does not expose a stable branch path. Slice 1 must either add that logical path to the production lifecycle contract or derive it from immutable execution metadata before testbench assertions consume parallel traces.

Comparison sorts by that deterministic merged sequence, not wall-clock timestamp or task completion order. Partial assertions match only declared fields. Ordered assertions consume matching entries in order. The result reports the first mismatch but retains the complete trace and all comparison results.

### AD-6: Production state remains immutable from the testbench

Decision: clone the selected project through a canonical runtime-project projection at run start. The projection excludes all automation scenario definitions and fingerprint metadata before cloning or fingerprinting. Store journey/runtime state in memory.

Isolation is verified through construction and attribution, not full before/after equality of a live root snapshot. Tests prove that the isolated object graph shares no mutable runtime state or live effect adapters, publishes no synthetic event to the root EventBus, and performs no root-runtime command. Legitimate concurrent Z21 telemetry or timestamp updates therefore cannot create false isolation failures.

Scenario editing changes the editor project only through ViewModel commands and the existing solution save path. Executing a scenario never mutates the editor project or production runtime.

### AD-7: Schema ownership

Decision: add `AutomationTestScenarios` to `Project`, initialized to an empty collection. The post-#32 baseline is `Solution.CurrentSchemaVersion = 4`. Because the property is additive and safely defaults when absent, keep version 4 unchanged. Update `MOBAflow/solution.json` and `MOBAflow/Build/Schemas/solution.schema.json` together and add no feature-specific legacy branch.

Rebase immediately before the persistence slice and repeat the compatibility classification. If the final model contains a genuinely breaking change, stop and obtain an approved upgrade path rather than incrementing the version without one.

### AD-8: UI remains a thin MVVM surface

Decision: use a specialized `AutomationTestbenchViewModel` for editor and ephemeral run state. Commands own create, duplicate, delete, validate, run, pause, step, cancel, restart, and assertion editing. Code-behind performs only WinUI view adaptation. EventBus handlers rely on `UiThreadEventBusDecorator` and do not dispatch manually.

The page and ViewModel lifecycle must be explicitly selected during implementation. A transient run ViewModel is preferred unless product review requires run-state preservation across navigation.

## Data model

### AutomationTestScenario

- `Id: Guid`
- `Name: string`
- `Description: string?`
- `ProjectSnapshotFingerprint: string?` for diagnostics, not as a compatibility mechanism; computed only from a canonical runtime-project projection that excludes every scenario and fingerprint field
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
- typed, allow-listed payload;
- status: intended, rejected, failed, or cancelled;
- script arguments are always represented as redacted and are never copied or hashed;
- no live credential, audio data, file contents, private network data, or sensitive absolute path.

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

`Idle -> Validating -> Ready|Invalid`; `Ready -> Running <-> Paused -> Completed|Failed|Cancelled`

- `Invalid -> Validating|Idle`; validation failure returns an editable invalid result and never leaves the runner stuck in `Validating`.
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
- PowerShell/script intents with raw arguments redacted;
- display/frame actions;
- feedback-triggered locomotive-function commands through the same recording boundary used by `ILocomotiveFunctionCommandGateway`.

Platform-neutral handlers validate payload shape and references. Environment checks such as file existence and all I/O occur only in the production implementation. The recording implementation has no dependency on network, process, filesystem, audio, or display implementations.

### IAutomationTestClock

Built on or compatible with `TimeProvider`; exposes current virtual time, pending-operation inspection, controlled advancement, pause/resume, registered-operation tracking, quiescence pumping, and cancellation. Production services consume `TimeProvider`, not the testbench-specific control API.

### IAutomationTraceSink

Run-local buffered trace. Producers attach stable logical ordering keys; the runner deterministically merges emissions at scheduler quiescence boundaries and allocates final sequence numbers after the merge. Atomic append order is not treated as deterministic. Consumers receive immutable snapshots.

### IAutomationAssertionEvaluator

Pure comparison service from validated assertions plus immutable trace/state snapshots to a deterministic result. It performs no I/O and has no runtime dependency.

## Existing files expected to change

Final line-level changes must be confirmed against the implementation baseline before editing.

### Domain and schema

- `Domain/Project.cs`: persisted scenario collection.
- `Domain/Solution.cs`: verify compatibility behavior; do not increment the schema version for the additive scenario collection.
- `MOBAflow/Build/Schemas/solution.schema.json`: complete scenario and assertion schema.
- `MOBAflow/solution.json`: English sample scenario only if a useful minimal sample is approved.

### Backend execution seams

- `Backend/Interface/IWorkflowService.cs` and `Backend/Interface/WorkflowExecution.cs`: preserve the Workflow 2.0 request/result and cancellation contracts; extend only if the accepted isolated effect or logical-path contract requires it.
- `Backend/Service/WorkflowService.cs` and `Backend/Service/WorkflowService.Graph.cs`: preserve injected time, cancellation, retry, dry-run, nested, and lifecycle behavior; add stable logical execution metadata only if characterization proves the current parallel sequence insufficient.
- `Backend/Service/WorkflowEffectPlanner.cs`: remain the pure payload-validation and effect-description owner shared by dry-run, live, and recording paths.
- `Backend/Service/ActionExecutionContext.cs`: effect sink and run correlation dependencies; do not treat the factory's shallow wrapper as mutable-state isolation.
- `Backend/Service/WorkflowActionHandlers.cs`: typed effect sink calls; move environment-dependent file checks and direct process launch into the production sink.
- `Backend/Service/LocomotiveWhistleAutomationService.cs` and its gateway registration: provide an isolated recording path for feedback-triggered function commands.
- `Backend/Manager/JourneyManager.cs`: replace the remaining wall-clock session timestamp, retain coordinator cancellation/ordering, add an injected synthetic feedback path, and support in-memory state.
- `Common/Events/WorkflowEvents.cs` and selected production event factories: reuse explicit workflow timestamps; add a construction timestamp only for event types required by deterministic testbench assertions.
- `Backend/Service/Recording/IsolatedReplayRuntime.cs`, `Backend/Service/Recording/RecordingEventBusDecorator.cs`, and `SharedUI/Service/RecordingRuntimeCommandGateway.cs`: reuse compatible scheduling, payload, correlation, and fail-closed patterns without routing testbench traffic through root recording services.
- `Backend/Extensions/MobaBackendServiceCollectionExtensions.cs`: production and RecorderPage registrations only; testbench isolated registrations belong in a dedicated factory/extension. Add descriptor-multiplicity guards only where characterization demonstrates they are required.
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

- Rebase after RF-03/#50 closes the final programme gate, then re-verify every affected file against current `main`.
- Add the pre-change characterization coverage listed above for live parallel lifecycle ordering, shallow context sharing, action planning/effect paths, journey transitions, controlled whistle time, EventBus ordering assumptions, and DI multiplicity.
- Finalize `IWorkflowEffectSink`, time, correlation, and cancellation contracts without changing user-visible behavior.
- Verify that every external action type has exactly one effect-boundary path.
- Reconcile the contracts with RecorderPage's merged recording payload, command-correlation, replay-delay, and safety vocabulary; do not reuse its projection runtime as a workflow engine.

Exit criteria:

- contracts are reviewed;
- production behavior is characterized;
- no second workflow engine exists;
- no external effect remains hidden inside a handler.

### Slice 2: Scenario model and validation

Goal: persist and validate scenario definitions without executing them.

- Add domain entities and collection initialization.
- Add pure validation for references, ordering, ranges, contradictory initial state, malformed assertions, and unsupported event/effect types.
- Add a canonical runtime-project projection for deep clone/fingerprint that excludes all scenarios and fingerprint metadata.
- Retain the confirmed version-4 additive classification and repeat it immediately before the persistence slice; do not assume a version increment.

Exit criteria:

- complete domain tests;
- malformed scenarios cannot reach runner construction;
- no platform references in Domain/Common/Backend.

### Slice 3: Virtual time, trace, and recording effects

Goal: prove deterministic scheduling and zero I/O independently of the full runner.

- Implement controllable time, registered-operation tracking, quiescence pumping, and stable simultaneous-event ordering.
- Implement immutable correlated trace with logical producer keys and deterministic merge points.
- Route production action handlers and feedback-triggered locomotive command gateways through recording-capable effect boundaries.
- Add production adapters preserving current behavior, including production-only file existence checks.
- Add recording sink and negative tests proving no network, process, filesystem, audio, display, or root-runtime dependency is reachable.

Exit criteria:

- repeated schedules produce byte-for-byte-equivalent normalized traces;
- cancellation clears pending operations;
- script actions are captured without process creation;
- existing workflow tests remain green.

### Slice 4: Isolated runner and production service integration

Goal: execute feedback-to-journey-to-workflow scenarios inside a disposable isolated scope.

- Build the isolated runtime factory with cloned project, private EventBus, in-memory journey state, test clock, recording effects, and the merged Workflow 2.0 executor.
- Inject synthetic envelopes without using the root runtime or root EventBus and stamp converted production events with virtual time.
- Implement state machine operations, explicit invalid-validation recovery, and restart-from-clean-snapshot.
- Capture workflow lifecycle and domain transitions.
- Verify object-graph, attribution, EventBus, and command isolation without comparing volatile live root snapshots.

Exit criteria:

- no Z21 connection is required;
- isolation-negative tests fail if any production effect registration is introduced;
- pause, step, cancellation, retry, timeout, and simultaneous events are deterministic;
- no testbench-attributed mutation, event, or command reaches production state after pass, failure, or cancellation.

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

- Rebase on the then-current schema and confirm version 4 remains the active exact-version baseline.
- Add the empty-initialized `Project.AutomationTestScenarios` collection.
- Keep the schema version unchanged for this additive property; if any final change is breaking, stop for an approved upgrade path.
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
- deterministic same-time and parallel-branch ordering independent of task scheduling;
- scheduler quiescence after chained delays and same-time continuation registration;
- concurrent command serialization, invalid runner transitions, and recovery from validation failure.

### Isolation and safety

- isolated provider contains no production Z21/UDP client, MOBApi client, physical speaker, process launcher, physical frame sender, or file journey store;
- raw command, signal, turnout, audio, announcement, script, and display actions produce captured intents only;
- PowerShell test proves zero process starts;
- network test proves zero UDP/HTTP/SignalR sends;
- forbidden-effect assertion remains zero;
- root EventBus receives no synthetic test event;
- no isolated object shares mutable state with the root runtime, and no testbench-attributed event or command reaches the root runtime during pass/fail/cancel.

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
8. Cancel once during delay, retry, and parallel execution; prove no later effects and no testbench-attributed root-runtime mutation, event, or command.
9. Manually validate create/save/reopen/run/pause/step/restart plus Light, Dark, High Contrast, keyboard, focus, Narrator, and text scaling.
10. Confirm no unintended `<Page Remove>` entry exists and the XAML compiler generated the page.

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Live side effect escapes isolation | Dedicated fail-closed runtime scope, negative DI tests, recording-only sink, zero-I/O assertions |
| Post-#32 or RecorderPage contract drift invalidates the runner | Rebase after RF-03 and immediately before each slice; consume the merged Workflow 2.0 executor and reuse compatible RecorderPage contracts without duplicating either engine |
| Real time leaks into deterministic paths | TimeProvider and cancellation characterization; analyzer/code-search validation for wall-clock calls |
| Parallel actions produce unstable traces | Stable logical operation keys, quiescence barriers, deterministic merge points, then final sequence allocation |
| Script handler launches a process | Move process creation behind production effect sink; recording sink has no process dependency |
| Test run mutates editor or production state | Scenario-free canonical projection, in-memory stores, disjoint object graphs, and attribution/reference isolation checks |
| Schema conflicts with Issues #31-#34 | Rebase immediately before persistence; preserve the version for additive fields and require an approved upgrade path for breaking changes |
| Trace leaks private data | Typed allow-listed payloads; raw script arguments omitted rather than hashed; sensitive paths redacted; no credentials, file contents, audio data, or unnecessary endpoints |
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

- [ ] RF-01 through RF-05 complete; only RF-03/#50 remains open
- [x] Issue #32 executor/lifecycle contracts stable through merged PR #74
- [x] Post-#32 schema version 4 and additive scenario-collection classification confirmed; repeat before Slice 6
- [x] Mandatory local secret scan succeeds
- [x] Local instruction and plan consolidation reconciled
- [x] Plan reviewed and linked from Issue #35
- [x] `plan-required` present
- [ ] Slice 1 affected files re-verified after RF-03/#50 and immediately before implementation
- [ ] No unrelated working-tree changes overlap the implementation slice
