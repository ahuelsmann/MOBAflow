# Feature Specification: Journey event plans

**Feature Directory**: `001-journey-event-plan`

**Source Issue**: #124

**MVP Scope Issue**: #144

**Created**: 2026-09-10

**Revised**: 2026-09-24. The operator simplified the feature: no legacy feedback sequences, no follow-up
journeys, no runtime start/stop and no per-run baselines. Journeys carry a persisted active flag, and events match
absolute InPort session counts. The MVP removes remaining automatic completion/restart rules and configurable first positions.

The operator also removed vehicle operating-time and usage statistics from this delivery. Follow-up
[issue #143](https://github.com/ahuelsmann/MOBAflow/issues/143) defers any future reconsideration;
it does not authorize implementation. Remaining calendar maintenance is removed separately under #147; route reservations were removed under #146. Neither is part of the 1.0 MVP.

**Status**: Simplification implemented locally; remote quality gate open

**Spec Kit**: Required

**Input**: Separate continuous InPort counts from virtual stops and provide a slim Event Manager with meaningful drag-and-drop support.

## Governance and Traceability

**Affected Platforms**: Shared domain/runtime and ViewModels, Windows MOBAflow editor, existing runtime consumers where adaptation is necessary.

**Out of Scope**: Collision avoidance, train identification, route reservations, follow-up journeys, per-journey count baselines, live railway operation and firmware changes.

**Sensitive Data**: No new sensitive data; tests use synthetic plans and fake feedback.

**Compatibility Surface**: None required. MOBAflow has no users yet; the legacy `FeedbackSequence`, `NextJourneyId`, `BehaviorOnLastStop.GotoJourney` and the feedback-sequence REST endpoint are removed without migration.

The current prerelease schema number stays unchanged for this PR. The operator intends to begin the supported
format at schema version 1 with release 1.0; this does not introduce backwards compatibility or migration.

## User Scenarios & Testing

### User Story 1 - Run a virtual journey over repeated laps (Priority: P1)

The operator runs several laps around an oval before advancing to the next virtual stop. One physical station represents successive virtual stops; arbitrary workflows run between them.

**Independent Test**: After a counter reset, deliver six activations on InPort 1 to an active journey with events at 2, 3 and 5.

**Acceptance Scenarios**:

1. **Given** 2 = signal, 3 = announcement and 5 = change stop, **When** InPort 1 advances from 0 to 6, **Then** those workflows run once at 2, 3 and 5; 1, 4 and 6 cause nothing.
2. **Given** an event workflow, **When** it executes, **Then** the current stop changes only through an explicit stop-change action.

### User Story 2 - Observe multiple ports and journeys (Priority: P1)

Connected parallel tracks allow several journeys to be active at the same time. The operator controls movements and prevents collisions.

**Acceptance Scenarios**:

1. **Given** InPort 1 at 5 and InPort 2 at 1 are configured, **When** InPort 2 reaches 1, **Then** its workflow runs even if InPort 1 has not reached 5.
2. **Given** two active journeys with events on the same InPort and count, **When** that count is reached, **Then** both workflows run.
3. **Given** an inactive journey, **When** feedback arrives, **Then** counters continue but none of its events run.
4. **Given** multiple trains can activate a port, **When** any does so, **Then** the port increments without assigning the activation to a train.

### User Story 3 - Reset session counts (Priority: P1)

The operator resets all counters through an explicit UI command at any time. Afterwards every event can run again when its count is reached.

**Acceptance Scenarios**:

1. **Given** application startup, **Then** every port starts at zero, including ports first encountered later.
2. **When** reset is invoked, **Then** all counters become zero, also while journeys are active.
3. Activations that were still being delivered when the reset happened do not trigger events.
4. Activating journeys, stop changes and workflows never reset counters automatically.

### User Story 4 - Edit a concise event plan (Priority: P1)

The Event Manager presents InPort, count and workflow in each row. Drag and drop accelerates assignment and organization; keyboard-accessible controls support the same edits.

**Acceptance Scenarios**:

1. Add only rows 2, 3 and 5; no placeholder is needed for counts 1 or 4.
2. Drop a palette workflow onto a row to assign/replace it, or onto the add target to create a row with that workflow.
3. Drag a row or use move controls to change stored presentation order without changing its trigger or creating an execution dependency.
4. Add, assignment, duplication, movement and deletion are possible using keyboard-accessible controls.
5. Events stay editable while a journey is active; changes and the active flag are saved and re-applied to the runtime immediately.
6. Committed event/active-flag edits update only that journey's configuration; accepted workflows, other journeys, direct layout controls and block observations remain intact. A count text draft commits on Enter or focus loss as one undoable edit.

### Edge Cases

- Duplicate occupied packets, release packets and initial occupied state retain existing activation detection semantics.
- Counters are never restored from saved project data.
- Invalid ports, zero counts and unavailable workflows cannot silently run. Disabled events never run.
- Equal-condition rows each run once in stored order.
- A failed workflow is not retried.
- There is no journey completion, automatic restart or follow-up journey. Moving next at the last stop (or with no stops) leaves the current stop unchanged. Later matching rules still run; unmatched higher counts do nothing.
- A workflow can explicitly select an earlier stop. Stop changes never reset InPort counts.
- Re-applying the project to the runtime keeps the current stop through the runtime checkpoint and keeps the counters.

## Requirements

### Functional Requirements

- **FR-001**: Maintain one non-persisted application-session activation count per InPort, starting at zero. Each app owns its own counts; MOBAflow and MOBAsmart do not synchronize counters, and mobile display/reset always use the local runtime.
- **FR-002**: Reset all counts only by explicit user command; allowed at any time.
- **FR-003**: Persist an active flag per journey. On every accepted activation, evaluate the events of all active journeys.
- **FR-004**: Each enabled event maps a port, positive count and workflow and runs when that port's session count equals the event count.
- **FR-005**: Evaluate different ports independently.
- **FR-006**: Change virtual stops only through existing workflow actions, without resetting counts.
- **FR-007**: Provide a compact editor with workflow assignment/creation and row reordering by drag and drop, plus keyboard-accessible add/assign/duplicate/move/delete controls.
- **FR-008**: Reuse arbitrary workflows, English labels, theme resources and visible validation feedback.
- **FR-009**: Do not collect, persist or display vehicle operating time, completed-trip totals or distance statistics. Remove their checkpoints, corrections and usage-based maintenance intervals without migration; retain functional InPort counters. Remove remaining calendar maintenance separately under #147.

### Key Entities

- **Journey**: Virtual stop sequence/current stop, active flag and event plan.
- **InPort session counters**: Shared real-activation totals since application start or the last reset.
- **Event plan/event**: Presentation-ordered independent port/count-to-workflow mappings.
- **Workflow**: Existing arbitrary action composition, optionally changing a stop.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Six activations execute exactly three workflows at 2/3/5 with no implicit stop change.
- **SC-002**: Several active journeys react to interleaved port activations; inactive journeys do not.
- **SC-003**: A reset clears all counts, and events run again afterwards.
- **SC-004**: Pointer and keyboard operations can create, assign, duplicate, reorder and delete equivalent saved plans.

## Assumptions

- Row order organizes the editor and resolves equal triggers; it does not gate other conditions.
- Each activation counts regardless of train identity; the user handles routing and collisions.
- Runtime state remains authoritative through existing projections and is not saved as resumable movement.
