# Issue #35 Automation Testbench Preparation Plan

**GitHub Issue**: #35
**Spec Kit**: Required
**Status**: Proposed; preparation only. Implementation remains blocked by G3 and G7/G8 below.

## Baseline and ownership

Refreshed on 2026-10-11 against `github/main` at `9b03676a9865275711d4031815f26a2d35e2474b`.
Issue #35 is open. Its 2026-10-09 comment confirms that the early RF-01 through RF-05 gates are resolved
and recommends the RF-23 project-runtime factory as the basis. RF-23 [#191](https://github.com/ahuelsmann/MOBAflow/issues/191)
remains open; PRs #207, #211 and #217 are drafts and not merged. `Backend/Service/ProjectRuntimes/` is absent from this main.
The existing `specs/008-project-runtimes/` belongs to RF-23; no automation-testbench feature specification exists yet.

This replaces the July execution inventory with the current ordered-action and event-plan baseline.
Workflow 2.0 #32 was delivered, but #132 subsequently replaced workflow graphs with ordered actions;
#124 introduced direct InPort/count event-plan matching. Do not restore graph branches, joins, nested workflows
or retry policies just because older testbench planning mentioned them.
Keep one standalone plan for #35; delete it after accepted completion.

## Outcome and scope

Named, persisted scenarios exercise the production workflow/journey services in an isolated project runtime.
The runtime records intended effects and compares typed expectations with actual state and a correlated chronological trace.
The WinUI page is the final surface; the platform-neutral runtime, clock, runner and comparison are the first deliverables.

In scope: scenario create/edit/duplicate/delete, selected project/journey/workflow and explicit initial state,
timed or step-based synthetic inputs, play/pause/step/cancel/restart, adjustable virtual time, partial assertions
and first-mismatch reporting with the full trace retained. Cover all current external effect types.

Out of scope: live hardware/network/audio/script/display effects, a second automation engine, automatic scenario generation,
RecorderPage import in the MVP, concrete RFID/camera providers and restoring superseded workflow behavior.
Do not create or edit an example `solution.json`: Andreas will create the example himself.
The persistence slice updates the scenario model and JSON schema; use isolated in-memory test data and temporary test fixtures
for roundtrip validation rather than changing the operator's or shipped sample solution.

## Readiness gates

| Gate | Requirement / current evidence | Blocks |
| --- | --- | --- |
| G1 | RF-01/#49, RF-02/#48, RF-04/#43 and RF-05/#51 are closed completed; RF-03/#50 is closed not planned. Verified on GitHub on 2026-10-11. | Satisfied; old RF-03 observation work is not reopened here |
| G2 | Consume the current production contract. #32, #124 and #132 are closed completed; current workflows are action lists and journeys match event-plan targets directly. | Recheck at implementation start |
| G3 | Isolated construction, effect boundary, clock, trace and assertion proposals below are reconciled and accepted through Spec Kit. | Runtime/handler implementation |
| G4 | `Solution.CurrentSchemaVersion` is 4; scenario data is directly added to the current project/schema. No legacy paths or migrations. Recheck the final model before persistence. | Persistence slice |
| G5 | Mandatory secrets scans and a clean dedicated task worktree on the verified integration base. | Every implementation slice |
| G6 | Issue #35 links this plan and the final feature artifacts; GitHub tracks the remaining work. A historical label alone is not implementation evidence. | Implementation start |
| G7 | Specification, clarification, plan, tasks and analysis exist under `specs/NNN-automation-testbench/`, agree with this plan and are linked from #35. | Implementation start; currently missing |
| G8 | RF-23 slice 3 is integrated, including project-runtime construction, per-project persistent counters and safe disposal. #207 and #211 were unmerged drafts at inspection time. | Production-code and characterization-test implementation |

Research and plan refinement can proceed now. No runtime/model/UI implementation starts until the applicable gates pass.
#35 does not acquire RF-24 or #141 as an additional hard dependency; use their final contracts if they are integrated by then.
Do not alter another session's RF-23 branches or turn their proposed factory into a testbench factory here.

## Current execution and isolation seams

| Concern | Verified current source / behavior | Required follow-up after RF-23 integration |
| --- | --- | --- |
| Workflow execution | `Domain/Workflow.cs`, `Backend/Service/WorkflowService.Sequence.cs`: ordered actions, lifecycle correlation, cancellation, TimeProvider-aware action delay, stop on action failure | Use this executor; characterize its actual behavior rather than historical graph/retry expectations |
| Feedback acceptance | `Backend/Service/InPortCounterService.cs` and `.Persistence.cs`: filtering, direct counts, load-before-feedback, buffered arrivals, stale-activation guards | Use an isolated counter store seeded from scenario state; never read/write production counter files |
| Journey reactions | `Backend/Manager/JourneyManager.cs`, `Domain/JourneyEventPlan.cs`: enabled entries of active journeys match exact InPort/count | Exercise matching and production stop-change actions; activation is not a counter reset |
| External effects | `Backend/Service/WorkflowActionHandlers.cs`: direct IZ21, audio, announcements, display and script process/file paths | Isolate every path; script/audio environment checks must move behind the production effect adapter |
| Dry run | `Backend/Service/WorkflowEffectPlanner.cs`: pure effect descriptions | Reuse its vocabulary; dry run alone does not execute full journey/state/delay semantics |
| Construction | `Backend/Extensions/MobaBackendServiceCollectionExtensions.cs`: root live registrations; RF-23 #207 proposes a per-project graph sharing some handlers/stores/audio | Review each dependency; a project runtime is not automatically a zero-I/O testbench runtime |
| Replay | `Backend/Service/Recording/IsolatedReplayRuntime.cs` | Reuse safe payload/correlation concepts where applicable; replay projection is not production workflow/journey execution |
| Editor/save | `SharedUI/Service/SolutionSession.cs`, `Domain/Project.cs` | Persist scenario definitions through the session; run-state/results are separate from edited project definitions |

Production counters resume stored values across restarts. A test scenario explicitly seeds its own independent counts;
it does not reset live counts or adopt a zero-on-restart production model. Manual seed/correction emits no counted feedback.

## Contract proposals to reconcile in Spec Kit

### Isolated runtime construction

A dedicated async-disposable factory constructs a private runtime graph from a deep clone of the selected project.
Use RF-23's integrated construction seams, but provide a private EventBus, in-memory journey/counter stores, virtual TimeProvider
and recording-only external adapters. No live Z21/UDP, MOBApi/SignalR client, root gateway, production state store,
process launcher, physical audio output or display sender can be resolved from that graph.
Do not decorate the live provider with a mode flag, temporarily disconnect Z21, or call live `SimulateFeedbackAsync`.
Reference/attribution tests prove no mutable state or command/event path is shared with the editor or production runtime.
Legitimate concurrent production telemetry is allowed; whole live snapshot equality is not the isolation test.

### Effect boundary

Retain the production action executor and handlers. The proposed typed effect sink has live and recording implementations;
it consumes the same validated effect vocabulary as `WorkflowEffectPlanner` rather than creating another taxonomy.
Capture Z21/drive/functions, turnout/signal commands, announcements, audio, scripts and display intents.
Journey stop transitions remain production domain operations against isolated state, not external effects.
The feedback-triggered whistle gateway must also be recording-only and cannot resolve the root runtime.

Payload/reference validation stays common. Only the production adapter checks file existence or launches a process.
The recording adapter performs no filesystem access. Script capture includes an opaque action identity and a sanitized
leaf/project-relative path with arguments explicitly redacted: never copy, hash, log, persist or display raw script arguments.
Keep trace payloads typed and allow-listed; exclude credentials, file contents, audio bytes and sensitive absolute paths.

### Virtual time and order

Production services consume TimeProvider and CancellationToken; testbench controls own pause/resume/advance.
Pause cannot complete future delays; single-step advances one defined next operation group and returns to Paused.
After advancing, process tracked continuations and newly scheduled same-time work to quiescence before choosing the next time.
An empty timer queue or a single Task.Yield is not proof that all operations have settled.
Cancellation drains/cancels pending work and prevents later effects; restart builds a fresh isolated graph.
Serialize runner commands and reject invalid transitions without leaving the runner stuck in Validating or Running.

Order inputs by virtual time and explicit scenario order; duplicate `(At, Order)` pairs are invalid.
Concurrent journeys may still execute independently. Define stable logical ordering keys from input, journey/workflow invocation,
action index and effect index; merge at quiescence before allocating visible trace sequences.
Do not use wall-clock timestamps, task completion order or a racing atomic sequence as the reproducibility contract.
Run/correlation IDs may differ per execution; normalize those identifiers when comparing repeated results.
The issue's older retries/parallel-workflow assertions must be clarified against the current engine; do not invent those mechanics.

### Synthetic provenance and comparison

Every input retains scenario/run/input identity, virtual timestamp, explicit order, typed payload and synthetic origin
in the private runner/trace envelope. Convert to current production-facing contracts only inside the isolated graph.
Reuse explicit lifecycle timestamps; add a construction-time seam only for events actually used in deterministic assertions.
Synthetic events never publish to the root EventBus or root recorder.

Trace includes virtual time, stable sequence, input/workflow/action correlation, typed payload, outcome and relevant state transitions.
Assertions may match a subset of fields, ordered occurrences, absence/forbidden effects and virtual-time bounds.
Distinguish invalid scenario/assertion, execution failure, comparison mismatch, cancellation and runner error.
Report the first mismatch while preserving the complete trace and comparison results.

### Scenario persistence and UI

Define named project-owned scenarios with stable IDs, initial state, ordered inputs, typed partial assertions and playback settings.
Validate nonempty/unique IDs and names, resolved project references, nonnegative times and finite bounded playback settings.
Do not store run results as editable scenario data. If fingerprint metadata is retained, derive it from a canonical runtime-project
projection excluding all scenarios and fingerprints; otherwise editing a scenario would change its own baseline.
Clone the same scenario-free projection so unrelated scenario edits cannot alter execution state.
Update `Domain/Project.cs` and `MOBAflow/Build/Schemas/solution.schema.json` directly. Keep schema version 4 unless the final feature requires a bump.
Do not add compatibility, migration or adoption code. No example solution is generated.

A focused shared ViewModel owns editing and run commands; code-behind only adapts the WinUI view.
Select and specify navigation/disposal lifetime, project-switch behavior, invalid-result recovery and edit locking during Running/Paused.
EventBus UI handlers use the existing decorated UI bus without an extra dispatcher layer.

## Narrow delivery sequence after the gates

1. Complete Spec Kit on the integrated RF-23 base. Inventory every effect and time seam and record the supported action-list semantics.
2. Characterize production feedback/count restoration, workflow action delay/cancellation/failure, journey matching/stop transitions
   and shallow context references. Introduce only the agreed replaceable effect boundary and preserve production behavior.
3. Add validated scenario/initial-state contracts, virtual clock and isolated construction with negative DI/reference tests.
   Prove every external effect is captured with zero network/process/file/audio/display access before adding UI execution controls.
4. Add runner operations, stable trace and pure partial-assertion comparison. Prove repeatability, pause/step/cancel/restart
   and deterministic simultaneous input handling with current workflows.
5. Persist scenarios and validate JSON schema/roundtrip using test-owned data. Andreas owns the example solution.
6. Add the page/ViewModel and existing navigation/DI registration. Document operator behavior and remaining manual acceptance.

Each slice names #35 as the primary issue and remains a narrow draft PR. Later slices depend on tested integrated predecessors;
the initial characterization and effect work is not authorized before G8.

## Validation and acceptance

Regression anchors: `WorkflowSequenceExecutionTests`, `WorkflowActionHandlersTests`, `WorkflowEffectPlannerTests`,
`JourneyEventPlanTests`, `JourneyEventPlanAcceptanceTests`, `PersistentInPortCounterTests`, `RecordingReplayServiceTests`
and final RF-23 runtime-host tests. Refresh fixture names at each implementation start.

- Prove isolated DI cannot construct a live effect adapter; capture every effect category, including script and whistle actions.
- Prove no testbench event/command/mutation reaches the production runtime and no mutable project/journey/state is shared.
- Run unchanged scenarios twice and compare normalized traces/results, including simultaneous inputs and concurrent journeys.
- Test delayed actions, pause, one-step, cancellation while waiting/executing, restart and invalid runner transitions.
- Test stored initial counts, direct targets, counter saturation/correction, active/inactive journeys and explicit stop transitions.
- Test malformed references/IDs/times/assertions, partial matching, ordered/negative assertions, first mismatch and full trace retention.
- Test project/session switches and persistence/schema roundtrip without editing any real or example solution.
- Run affected portable tests and consumers plus broader suites for runtime/DI/persistence changes. Compile the desktop app.
- With explicit launch authorization, inspect Light/Dark/High Contrast, keyboard, focus, Narrator, scaling and drag/drop.
  Never launch the app, touch hardware or deploy from a code-validation request alone.
- Before readiness, require current-head CI, green SonarCloud and zero OPEN/CONFIRMED PR issues.

## Preparation validation

This refresh changes documentation only: check the final diff, secrets, line endings, internal links and Spec Kit governance.
No .NET build/test result, implemented factory, scenario file or manual/hardware acceptance is claimed.
