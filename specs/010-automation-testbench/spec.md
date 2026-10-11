# Feature Specification: Isolated Automation Testbench

**Feature Directory**: `010-automation-testbench`

**Source Issue**: #35

**Created**: 2026-10-11

**Status**: Draft requirements; implementation blocked by RF-23 slice 3 and the remaining Spec Kit phases.

**Spec Kit**: Required

**Input**: Issue #35 and the maintainer's request to continue the next steps permitted by the current prerequisite state.

## Governance and Traceability *(mandatory)*

**Affected Platforms**: MOBAflow Windows desktop provides the editor and execution controls. Scenario definitions,
execution, comparison and results remain independent of a desktop or mobile UI. An Android editor, remote execution
through MOBApi and physical display/device deployment are outside this first delivery.

**Out of Scope**: Live layout control; executing scripts, real audio, announcements or display output; concrete RFID,
camera or AI providers; RecorderPage import; automatic scenario generation; adding workflow graphs, joins, nested
workflows or retry policies; changing the existing production automation semantics; persisted run history.

**Sensitive Data**: Project references and configured script paths may contain private information. Captured results
must omit credentials, file contents, audio bytes, raw script arguments and sensitive absolute paths.

**Data and API Effects**: Reusable scenarios become explicit project data in the solution and its JSON schema.
Run state, counters, captured outputs and results are separate from saved scenario definitions. This specification
does not modify a solution file or schema. It adds no public REST/SignalR endpoint, configuration default, Z21 wire
behavior or persisted layout change. No compatibility or migration path is introduced. Recheck schema version 4
before the persistence slice; a version bump requires a concrete format reason. Andreas owns the example solution.

**Evidence and prerequisites**: Requirements were reconciled against main `2aabcbe8e3adc75a94db865dfc14ecf9187efcae`,
the [preparation plan](../../plans/issue-35-automation-testbench.md), ordered workflow actions and direct InPort/count
event plans. RF-23 [#191](https://github.com/ahuelsmann/MOBAflow/issues/191) is incomplete: project-runtime construction,
per-project persistent counters and safe disposal in #207/#211 are not merged. Preparing these requirements is allowed;
production code and characterization tests wait for integrated RF-23 slice 3. Technical planning, tasks and cross-artifact
analysis must use that integrated base. RF-24/#219 and #141 are not additional hard prerequisites for #35.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Test an automation without affecting the layout (Priority: P1)

An operator selects a project and creates a scenario with explicit starting counts, journey activation and current-stop
state. Train assignments and locomotive addresses come from the captured project definitions; no separate train position,
speed or occupancy state is seeded. The operator adds synthetic feedback and inspects the actions it would have produced.
Every visible output belongs to the isolated test run.

**Why this priority**: The testbench is useful only when it exercises normal automation behavior without operating devices.

**Independent Test**: Use a test-owned project with an active journey whose event-plan target is InPort 1/count 12.
Seed that input at 10, submit two accepted feedbacks and inspect the recorded workflow output and isolated count.

**Acceptance Scenarios**:

1. **Given** the scenario starts at count 10, **When** two accepted feedbacks arrive, **Then** the isolated count reaches
   12 and the matching enabled event-plan entry of the active journey invokes its assigned workflow once.
2. **Given** the journey is inactive or its entry disabled, **When** those feedbacks arrive, **Then** counts still advance
   according to production rules and that entry invokes no workflow.
3. **Given** a workflow contains device, announcement, audio, script or display actions, **When** it runs, **Then** the
   testbench records validated intended effects and makes zero real device, network, process, filesystem or audio calls.
4. **Given** a production runtime is active, **When** a scenario succeeds, fails or is cancelled, **Then** it sends no event,
   command or state mutation to that runtime; legitimate unrelated production telemetry may continue.
5. **Given** a requested execution target is a live runtime, **When** execution is requested, **Then** it is rejected before
   any input or effect; executing a scenario always requires an explicitly identified isolated testbench target.
6. **Given** a structurally valid audio/script action names a missing file, **When** it runs, **Then** its intent is captured
   without any file probe; a missing payload or empty required path still fails shared structural validation.
7. **Given** a rapid feedback activation is rejected by the counter's timer filter and an enabled whistle rule matches,
   **When** that synthetic activation arrives, **Then** raw feedback still reaches the whistle rule and records its production
   function behavior, while the count stays unchanged and no counted-feedback journey workflow is invoked again.
8. **Given** a selected workflow is not assigned to an enabled event entry of a selected, initially active journey,
   **When** execution is requested, **Then** validation rejects that runnable selection without effects; selecting a workflow
   alone never invokes it, activates a journey or changes an assignment.
9. **Given** an explicit journey current-stop identity and index, **When** the scenario is initialized, **Then** they resolve
   consistently against captured journey definitions without emitting feedback or an arrival workflow; an inconsistent
   stop/index is rejected before effects. Train assignment and locomotive address remain the captured project values.
10. **Given** the captured project has a journey outside the operator's target selection, **When** initialization runs,
    **Then** that journey uses its explicit scenario activation/stop state, never its implicit captured activation.
    An omitted journey state fails validation; an explicitly active unselected journey reacts normally to matching feedback.
11. **Given** the second stop is the initial stop, **When** the scenario is saved and reopened, **Then** its stored index
    is 1, isolated journey position is 1 and workflow announcement position is 2. An empty journey stores both stop identity
    and index as null; its action context retains production's fallback index 1 with no current station.

---

### User Story 2 - Control virtual time and reproduce a result (Priority: P1)

The operator plays, pauses, steps, cancels or restarts a scenario and changes playback speed without changing the
meaning or ordering of its virtual times. Repeating unchanged inputs against unchanged project definitions gives
the same logical trace and result.

**Why this priority**: Delays and simultaneous events must be inspectable and repeatable without waiting for real time.

**Independent Test**: Run a scenario with an initial input, delayed output and two explicitly ordered inputs at the same
virtual time. Repeat it at different playback speeds and exercise pause, single-step, cancel and restart.

**Acceptance Scenarios**:

1. **Given** a future delayed action and a paused run, **When** real time passes, **Then** virtual time does not advance
   and that future action produces no recorded effect.
2. **Given** a paused run, **When** Single step is selected, **Then** it processes the next virtual instant, including
   explicitly ordered inputs and their resulting same-time work, and returns to Paused before a later instant.
3. **Given** simultaneous inputs have distinct explicit order values, **When** the scenario runs repeatedly, **Then**
   accepted inputs, resulting transitions and recorded effects have the same logical order.
4. **Given** a run is waiting or executing, **When** Cancel completes, **Then** it accepts no further input or effect,
   retains the trace captured so far and reports Cancelled rather than Passed or a comparison mismatch.
5. **Given** a completed, failed or cancelled run, **When** Restart is selected, **Then** execution begins with the
   scenario's explicit initial state and no counters, pending work or results carried from the preceding run.

---

### User Story 3 - Explain expected-versus-actual differences (Priority: P1)

The operator states only the effects and state transitions relevant to an automation. The testbench shows the first
failed expectation, the corresponding input/workflow/action and the complete chronological trace.

**Why this priority**: A deterministic run must explain why an automation passed or failed.

**Independent Test**: Give an otherwise valid scenario one deliberately wrong command payload expectation and an
absence expectation. Verify mismatch reporting and its correlation with the retained trace.

**Acceptance Scenarios**:

1. **Given** an expectation specifies only selected output fields, **When** comparison runs, **Then** it evaluates those
   fields without demanding equality of unspecified fields.
2. **Given** ordered output expectations or a virtual-time bound, **When** the actual output violates either, **Then**
   the result identifies the first failing expectation, expected/actual values and its trace location.
3. **Given** an invalid expectation, executor failure or cancellation, **When** the result is displayed, **Then** it is
   distinguishable from an ordinary expected-versus-actual mismatch and all available trace entries remain inspectable.
4. **Given** script arguments contain sensitive text, **When** the script intent is captured, **Then** no argument value,
   derivative/hash of that value or sensitive absolute path appears in any testbench-generated output.
5. **Given** a positive effect expectation has no explicit count, **When** one matching effect and unrelated effects occur,
   **Then** it passes; two matching effects fail. An explicit inclusive range of 1 to 2 passes for two matches.
6. **Given** an absence expectation, **When** one matching occurrence appears, **Then** it fails even if other effects
   are unrelated; no matching occurrence passes.
7. **Given** ordered expectations for effects A then B, **When** an unrelated effect appears between A and B, **Then**
   order passes; B then A fails. One trace entry cannot satisfy two slots in the same ordered group.
8. **Given** an ordered group A(count=2) then B(count=1), **When** the trace is A, B, A, **Then** it fails because the
   last A follows B; A, A, B passes. A zero-match slot whose count allows zero contributes no ordering position.

---

### User Story 4 - Maintain reusable scenarios (Priority: P2)

The operator creates, edits, duplicates and deletes named project-owned scenarios, saves the solution and reopens
it to use the same initial state, input sequence, expectations and playback settings.

**Why this priority**: A reusable regression scenario makes an automation change easy to verify again.

**Independent Test**: Use temporary, test-owned solution data for create/duplicate/edit/save/reopen/delete; never
use or generate the maintainer's example solution.

**Acceptance Scenarios**:

1. **Given** a valid named scenario, **When** the solution is saved and reopened, **Then** its complete definition and
   stable references are preserved, with no previous execution state or result restored into the editor.
2. **Given** an existing scenario, **When** it is duplicated and the copy edited, **Then** it has an independent identity,
   and the source definition remains unchanged.
3. **Given** a referenced journey or workflow was removed, **When** execution is requested, **Then** validation identifies
   the unresolved reference and produces no automation effect.
4. **Given** a scenario is Running or Paused, **When** the user requests project/solution replacement or leaves the page,
   **Then** the run is cancelled and released before the replacement or departure completes; saved definitions remain intact.

### Edge Cases

- Empty/duplicate identifiers or names, missing references, negative virtual times, duplicate time/order input pairs
  and non-finite or non-positive playback speeds are invalid and produce no execution effects.
- Seeding/correcting an initial count produces no artificial accepted feedback or workflow invocation. Counter overflow,
  filters, active/inactive journeys and explicit stop-change actions retain current production behavior.
- A scenario with no inputs can validate initial-state expectations; with no expectations it reports execution completion
  without claiming that expected behavior was verified.
- Inputs with equal virtual time are processed in explicit scenario order. All resulting same-time work settles before
  progression; elapsed wall time or competing task completion order must not determine the logical trace.
- Delayed actions, a failing action and cancellation use current action-list behavior; the testbench adds no retry engine.
- Invalid control transitions or repeated cancel requests must not strand execution in Running, Paused or Validating.
- Concurrent legitimate production updates do not belong to the test trace and are not treated as testbench mutations.
- Assertion failures preserve the entire available trace. A trace that cannot be completed reports an execution error
  or cancellation; a truncated or partial trace cannot silently receive a Passed result.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Users MUST be able to create, edit, duplicate and delete named scenarios owned by a selected project.
  Names MUST be nonempty and unique within that project; duplicated scenarios MUST have independent identities.
- **FR-002**: A scenario MUST define its project references, controlled initial state, synthetic input times and order,
  typed partial expectations and playback settings. Users MUST be able to select the journeys/workflows under test.
  A selected runnable workflow MUST be referenced by an enabled event entry of a selected, initially active journey.
  Workflow selection is a test target, not a direct invocation or an execution filter: synthetic feedback and production
  event-plan matching supply its invocation context. All matching enabled entries of the isolated active journeys execute,
  including other workflows; selection MUST NOT suppress them. Known workflows may be referenced by absence expectations
  even when they are not runnable targets. Unassigned workflows require a project assignment before selection as a target.
  Journey selection identifies test targets, not a reduced project graph. Initial state MUST explicitly cover every journey
  in the captured project, including unselected journeys; omission is invalid and captured activation MUST NOT be inferred.
- **FR-003**: Execution MUST validate identifiers, references, input ordering, times, playback settings and expectations
  before effects. Duplicate `(virtual time, explicit order)` input pairs MUST be rejected.
- **FR-004**: A run MUST use isolated project definitions captured at its start. No mutable runtime or edited project data
  may be shared with live execution. Editing an unrelated scenario MUST NOT change its execution baseline.
- **FR-005**: Every run MUST be visibly identified as isolated and every input visibly synthetic. A live execution target
  MUST be rejected; test inputs, recorded events and commands MUST never reach the production runtime or its recorder.
- **FR-006**: The testbench MUST exercise the same raw/accepted-feedback, journey, ordered-action, structural validation, delay,
  cancellation and failure behavior as production automation. It MUST NOT implement a second automation engine.
  Payload/reference validation MUST be shared. Environment-dependent checks such as audio/script file existence,
  executable availability and device availability belong only to production execution and MUST NOT run in the testbench;
  a structurally valid intent is captured regardless of those resources. This exception does not suppress structural errors
  or failures in the shared executor and isolated domain operations.
- **FR-007**: Device commands, locomotive functions, turnouts/signals, announcements, audio, scripts, displays and
  feedback-triggered whistles MUST be captured as intended effects, with zero external execution or environment probes.
  Script intent inspection MUST NOT read script files or check for their existence.
- **FR-008**: Recorded effects and traces MUST use only safe typed fields. Script arguments MUST be redacted without
  copying or hashing their values; only an opaque action identity and sanitized leaf/project-relative path may identify
  a script. Credentials, file contents, audio bytes and sensitive absolute paths MUST NOT be captured or displayed.
- **FR-009**: Scenario counts MUST be explicitly seeded independent of persisted production counters. Seed/correction,
  journey activation and workflow execution MUST NOT implicitly reset live or isolated counts or trigger fake feedback.
- **FR-010**: Accepted feedback MUST retain current filtering/count semantics. Only enabled entries of active journeys
  whose direct InPort/count targets match may invoke their assigned reusable workflow.
  Each synthetic raw activation MUST also reach the isolated raw-feedback consumers independently of counter acceptance.
  Counter-filter rejection MUST NOT suppress raw feedback delivery or feedback-triggered whistle behavior; it MUST prevent
  that activation from incrementing the count or producing a counted-feedback journey invocation.
- **FR-011**: Play, Pause, Single step, Cancel, Restart and adjustable playback speed MUST operate on virtual time.
  Playback speed MUST NOT alter input ordering, delayed-action timing or expectation results.
- **FR-012**: Pause MUST prevent progression to future virtual times. Single step MUST complete the next virtual instant
  and all work due at that instant, then return to Paused; no later-time effect may leak into that step.
- **FR-013**: Inputs at the same time MUST follow their explicit order. Resulting correlated effects and transitions,
  including independently executing journeys, MUST have a stable logical order across repeated runs.
- **FR-014**: Cancel MUST stop further inputs/effects and release pending work before reporting completion of cancellation.
  Restart MUST construct an independent run from the defined initial state without retaining preceding run work/state.
- **FR-015**: Conflicting/invalid controls MUST be rejected with a recoverable state. Editing a scenario's execution fields
  while Running or Paused MUST be disabled; project/solution replacement or page departure MUST cancel/release the run first.
- **FR-016**: The trace MUST retain input, virtual time, logical sequence, workflow/action correlation, safe payload,
  relevant state transition and outcome. Repeat comparison MUST ignore only opaque per-run/correlation identities.
- **FR-017**: Expectations MUST support invocation/absence, ordered commands, selected typed payload fields,
  journey/stop transitions, count values and virtual-time bounds without requiring every trace field to be duplicated.
  An occurrence predicate consists of its event/effect kind, references, selected fields and optional inclusive virtual-time
  bounds. A positive invocation/effect expectation MUST default to exactly one matching occurrence; an explicit exact count
  or inclusive minimum/maximum MAY replace that default. Absence means exactly zero and MUST reject a nonzero count setting.
  Counts MUST be nonnegative integers with minimum no greater than maximum. Counts cover the entire applicable trace/time
  window; additional matching occurrences MUST fail an exceeded maximum, while unrelated occurrences and unspecified fields
  remain unverified. Independent expectations may match the same entry. In an ordered group, each slot binds all occurrences
  matching its predicate/window and checks its count constraint. Sets for different slots MUST be disjoint. Every occurrence
  of an earlier nonempty slot MUST precede every occurrence of the next nonempty slot in logical sequence; unrelated entries
  may appear between them. A zero-match slot that satisfies its count constraint contributes no ordering position.
  State-value expectations compare the named initial/final state or correlated transition rather
  than counting repeated state snapshots.
- **FR-018**: Results MUST distinguish invalid scenario/expectation, completed without expectations, Passed,
  comparison mismatch, execution failure, cancellation and testbench error. A first mismatch MUST identify its
  expectation and corresponding trace data; all available trace and comparison results MUST remain inspectable.
- **FR-019**: Saving/reopening MUST preserve the complete reusable definition through the existing solution save flow,
  without persisting run state/results as scenario data or changing configuration defaults. Test data MUST be independently
  owned; no operator or shipped example solution is created or changed by this feature's preparation/automated validation.
- **FR-020**: The desktop editor and execution controls MUST use English text and support keyboard operation,
  identifiable focus and readable Light/Dark/High Contrast states. Errors and results MUST remain understandable without color alone.

### Key Entities *(include if feature involves data)*

- **Scenario**: Named, stable project-owned definition containing reference selections, initial state, ordered synthetic
  inputs, partial expectations and playback settings; separate from executions and their results.
- **Initial State**: Explicit isolated input counts and every captured journey's activation and current stop. Stop identity
  and stored index MUST identify the same stop for a nonempty journey; both MUST be null for an empty journey. The stored index
  is zero-based: the first stop is 0, the second is 1. It maps directly to `JourneySessionState.CurrentPos`; the action context's
  one-based `CurrentStationIndex` is derived as index + 1. For an empty journey, the isolated session has empty name/null stop
  identity and internal position 0; the action context has no current station and retains production's fallback index 1.
  A separate no-stop-reached state for a nonempty journey is unsupported by current production and MUST NOT be introduced here.
  No implicit arrival action is emitted. Stop name is derived from the referenced definition rather than persisted as state.
  Train assignments and locomotive addresses remain captured project data, not mutable train runtime state.
  Runtime run/correlation identities and feedback timestamps are derived by the isolated executor, not seeded operator fields.
  Initial state is never borrowed from or written into production runtime state implicitly.
- **Synthetic Input**: Scenario-owned input identity, virtual time, explicit order, event category and typed payload;
  retains synthetic origin throughout execution and trace inspection.
- **Run**: One independent execution of a scenario against fixed project definitions, with controlled virtual time,
  a recoverable execution state and separate captured outputs/results.
- **Captured Effect / Trace Entry**: Safe representation of an intended output or state transition, its logical time/order,
  input/workflow/action correlation and outcome; no executed external side effect or sensitive raw payload.
- **Expectation / Comparison Result**: Selected fields, exact/inclusive occurrence counts, ordered occurrences, absence
  or time bounds and the corresponding actual match or first mismatch. Unspecified behavior has not been verified.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Valid scenarios covering every currently supported external-effect category produce zero live commands,
  network/file/process/audio/display operations and zero scenario-origin mutations of production state on success,
  failure, cancellation and restart.
- **SC-002**: Ten runs of unchanged scenarios against unchanged project definitions produce identical logical traces
  and comparison outcomes, after normalization of opaque run/correlation identities, at different playback speeds.
  Acceptance includes simultaneous inputs, concurrent journeys and delayed actions.
- **SC-003**: Every pause/step acceptance case produces zero future-time effects while paused and exactly one next
  virtual-instant group per step. Every cancellation case produces zero effects after cancellation completes.
- **SC-004**: Every deliberate expectation failure in the acceptance suite identifies its first mismatch and retains
  the complete available trace; invalid input, execution failure, cancellation and a comparison mismatch are distinguishable.
- **SC-005**: All create/edit/duplicate/delete and save/reopen acceptance cases preserve the intended complete definition
  and stable references, with zero execution state/results restored as editable scenario data.
- **SC-006**: The complete create-run-inspect-restart workflow can be performed without a device connection and with
  keyboard controls; required desktop states remain readable in Light, Dark and High Contrast.

## Assumptions

- This is a requirements draft, not approval of a runtime design or permission to implement before RF-23 slice 3.
  The technical plan, tasks and analysis remain absent until their integrated source contracts can be reviewed.
- The first operator surface is the Windows desktop page described in #35 and its existing preparation plan.
  Android/remote testbench execution requires a separate scope decision.
- Current production workflows are ordered action lists. Historical graph/retry wording in #35 does not authorize
  restoring superseded behavior; assertions cover only currently supported action failures, cancellation and delays.
- Single step means the next virtual instant with all resulting same-time work. This explicit default is a reviewable
  requirement; if operator feedback chooses an action-level step, reconcile it before technical planning.
- Playback speed must be finite and positive. Its editor range and default belong to the later UI design, while
  execution timing and outcomes are invariant across supported settings.
- Leaving the page or replacing its project/solution cancels/releases execution; edit locking while Running/Paused
  avoids ambiguous changes to the active scenario. These operator defaults must be reviewed before implementation.
- Production counts resume their stored values after restart. Scenario initialization is independent and never
  reintroduces zero-on-restart semantics for production counters.
- Scenario schema changes, isolated execution data, technical contracts and disposal guarantees will be detailed
  in the later technical plan. No real/example solution, public API, runtime lifecycle or module boundary is changed here.
