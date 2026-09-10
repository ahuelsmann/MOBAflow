# Feature Specification: Journey event plans

**Feature Directory**: `001-journey-event-plan`

**Source Issue**: #124

**Created**: 2026-09-10

**Status**: Implemented locally; issue linked and portable tests/Windows compile passed; manual acceptance and remote quality gate open

**Spec Kit**: Required

**Input**: Separate continuous InPort counts from virtual stops and provide a slim Event Manager with meaningful drag-and-drop support.

## Governance and Traceability

**Affected Platforms**: Shared domain/runtime and ViewModels, Windows MOBAflow editor, existing runtime consumers where adaptation is necessary.

**Out of Scope**: Collision avoidance, train identification, route reservations, future counter modes, new action types, live railway operation and firmware changes.

**Sensitive Data**: No new sensitive data; tests use synthetic plans and fake feedback.

**Compatibility Surface**: Journey JSON, runtime snapshots/commands, workflow execution, existing Z21 activation semantics and editor state. Legacy feedback sequences retain their meaning until explicit adoption of an event plan; source data remains available afterward.

## User Scenarios & Testing

### User Story 1 - Run a virtual journey over repeated laps (Priority: P1)

The operator runs several laps around an oval before advancing to the next virtual stop. One physical station represents successive virtual stops; arbitrary workflows run between them.

**Independent Test**: Start at InPort 1 count 42 and deliver five activations to a plan with triggers 2, 3 and 5.

**Acceptance Scenarios**:

1. **Given** 2 = signal, 3 = announcement and 5 = change stop, **When** InPort 1 advances from 42 to 47, **Then** those workflows run once at 44, 45 and 47; 43 and 46 cause nothing.
2. **Given** an event-plan workflow, **When** it executes, **Then** the current stop changes only through an explicit stop-change action. Counts and starting values remain unchanged.

### User Story 2 - Observe multiple ports and concurrent journeys (Priority: P1)

Connected parallel tracks allow several journeys to run independently. The operator controls movements and prevents collisions.

**Independent Test**: Start A at count 10 and B at 12, then deliver interleaved port activations.

**Acceptance Scenarios**:

1. **Given** InPort 1 at 5 and InPort 2 at 1 are configured, **When** InPort 2 reaches 1, **Then** its workflow runs even if InPort 1 has not reached 5.
2. **Given** A started at 10 and B at 12, **When** the shared count reaches 13, **Then** A observes 3 and B observes 1.
3. **Given** a stopped journey, **When** feedback continues, **Then** session counters continue but that run executes no further events. Restart captures fresh baselines.
4. **Given** multiple trains can activate a port, **When** any does so, **Then** the port increments without assigning the activation to a train.
5. **Given** another event-plan journey is running and the selected journey or shared workflow definitions have changed, **When** start is requested, **Then** the request is rejected clearly instead of starting stale definitions. After all event-plan journeys stop, activation/start uses the updated definitions. Unchanged journeys can still start concurrently.

### User Story 3 - Reset session counts safely (Priority: P1)

The operator may reset all counters through an explicit UI command only while no journey runs.

**Independent Test**: Attempt reset before, during and after a run, including direct runtime invocation.

**Acceptance Scenarios**:

1. **Given** application startup, **Then** every port starts at zero, including ports first encountered later.
2. **Given** no active journey, **When** reset is invoked, **Then** all counters become zero.
3. **Given** any active journey, **When** reset is attempted, **Then** UI disables it and runtime rejects it without altering counts.
4. Journey start/stop, stop changes and workflows never reset session counters automatically.

### User Story 4 - Edit a concise event plan (Priority: P1)

The existing Event Manager presents InPort, count since start and workflow in each row. Drag and drop accelerates assignment and organization; keyboard-accessible controls support the same edits.

**Independent Test**: Create the 2/3/5 example, assign/replace workflows, duplicate/reorder/delete rows and save/reload, then repeat without dragging.

**Acceptance Scenarios**:

1. Add only rows 2, 3 and 5; no placeholder is needed for counts 1 or 4.
2. Drop a palette workflow onto a row to assign/replace it, or onto the add target to create a row with that workflow.
3. Drag a row or use move controls to change stored presentation order without changing its trigger or creating an execution dependency.
4. Add, assignment, duplication, movement and deletion are possible using keyboard-accessible controls.
5. A running journey's plan is read-only; labels clearly explain counts since journey start.

### User Story 5 - Preserve existing journeys (Priority: P1)

Opening an old project must not reinterpret per-step repeat counts as new start-relative event counts.

**Independent Test**: Load/save legacy JSON, run the original sequence, explicitly adopt an empty event plan and save/reload again.

**Acceptance Scenarios**:

1. An absent event plan preserves existing feedback fields and execution behavior.
2. Explicit adoption creates an empty plan and retains the old sequence as a read-only reference; no arithmetic migration is performed.
3. A newly created journey uses an event plan by default.
4. Journeys using event plans do not show the legacy feedback editor in the shared journey template; a message directs the operator to Event Manager. Journeys still using the legacy sequence retain that editor.

### Edge Cases

- Duplicate occupied packets, release packets and initial occupied state retain existing activation detection semantics.
- Unseen ports have zero baselines. Counters and baselines are never restored from saved project data.
- Invalid ports, zero thresholds and unavailable workflows cannot silently create executable rows. Disabled events never run.
- Equal-condition rows each run once in stored order. Different conditions remain independent.
- A dispatched event is not automatically retried after workflow failure. A new run creates a fresh dispatch scope.
- Stopping prevents further dispatch; already executing work follows existing cancellation behavior.
- Reset and start must not interleave so as to invalidate a captured baseline.
- Project selection and deferred editor updates do not replace an active event-plan execution. Starting changed definitions requires all active event-plan journeys to stop first.

## Requirements

### Functional Requirements

- **FR-001**: Maintain one non-persisted application-session activation count per InPort, starting at zero.
- **FR-002**: Reset only by explicit user command with no active journey; enforce in UI and runtime.
- **FR-003**: Explicit start captures immutable baselines; progress is current count minus that run's baseline.
- **FR-004**: Each enabled event maps a port, positive relative count and workflow, and dispatches at most once when reached.
- **FR-005**: Evaluate different ports independently and support concurrent runs with separate baselines.
- **FR-006**: Change virtual stops only through existing workflow actions in event-plan mode, without resetting counts.
- **FR-007**: Provide a compact editor with workflow assignment/creation and row reordering by drag and drop, plus keyboard-accessible add/assign/duplicate/move/delete controls.
- **FR-008**: Persist plan configuration while preserving legacy feedback data/behavior; adoption is explicit and new journeys default to a plan.
- **FR-009**: Keep running plans stable; expose start/stop and reset availability; restart creates fresh baselines and dispatch state.
- **FR-010**: Reuse arbitrary workflows, English labels, theme resources and visible validation feedback.

### Key Entities

- **Journey**: Virtual stop sequence/current stop with a new plan or its original feedback sequence.
- **Journey run**: Baselines, active state and dispatched event identities for one execution.
- **InPort session counters**: Shared real-activation totals for the application lifetime.
- **Event plan/event**: Presentation-ordered independent port/count-to-workflow mappings.
- **Workflow**: Existing arbitrary action composition, optionally changing a stop.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Five activations execute exactly three workflows at 2/3/5 with no implicit stop change.
- **SC-002**: Three concurrent journeys maintain independent progress with interleaved port activations.
- **SC-003**: Every active-run reset attempt leaves counts unchanged; idle reset clears all counts.
- **SC-004**: Pointer and keyboard operations can create, assign, duplicate, reorder and delete equivalent saved plans.
- **SC-005**: Legacy fixtures preserve feedback fields and execution; adoption never fabricates thresholds from repeat counts.

## Assumptions

- The accepted conversation resolves behavioral scope; future counter modes are deferred.
- Row order organizes the editor and resolves equal triggers; it does not gate other conditions.
- Each activation counts regardless of train identity; the user handles routing and collisions.
- Explicit stop ends a run. Reaching the last event does not implicitly reset counts or advance a stop.
- Runtime state remains authoritative through existing projections and is not saved as resumable movement.
