# Feature Specification: Remove route reservations

**Feature Directory**: `003-remove-reservations`

**Source Issue**: #146

**Created**: 2026-09-24

**Status**: Approved scope; implementation in progress

**Spec Kit**: Required

**Input**: Simplify version 1.0 by removing route reservations and automatic locking/release. Preserve direct turnout/signal commands, block occupancy, InPort counters and journey rules. The operator is responsible for collision-free operation.

## Governance and Traceability

**Affected Platforms**: Shared libraries and Windows UI; Android and API consumer compilation.

**Out of Scope**: Journey rules, workflow action-list changes, maintenance, new counter actions, collision avoidance, train identification, migration/compatibility, live railway operation, and expansion from [#34](https://github.com/ahuelsmann/MOBAflow/issues/34). Product authority is [MVP #144](https://github.com/ahuelsmann/MOBAflow/issues/144).

**Sensitive Data**: Do not inspect or alter operator-owned layout/settings data. Use repository test fixtures and fake hardware.

**Data and API Effects**: Remove saved route definitions and reservation/locking fields and commands. Preserve turnout mappings, signal definitions, block feedback and representation bindings, existing configuration defaults, direct commands and Z21 protocol. Remove superseded contracts directly; no migrations or schema-version bump.

## User Scenarios & Testing

### User Story 1 - Direct operation remains available (Priority: P1)

An operator commands a configured turnout or signal without reserving a route.

**Why this priority**: Removing reservations must not remove the remaining operating controls.

**Independent Test**: Use fake hardware to command a turnout with an unrelated occupied/unknown block and verify the configured command and confirmation states; exercise the existing direct signal command path.

**Acceptance Scenarios**:

1. **Given** a configured turnout and live connection, **When** the operator requests a supported position, **Then** its configured command sequence is sent without a route prerequisite.
2. **Given** an invalid mapping, disconnected command station or outstanding command for the same turnout, **When** a command is requested, **Then** existing rejection/failure behavior prevents invalid or duplicate dispatch.
3. **Given** a pending turnout command, **When** confirmation or disconnect arrives, **Then** the displayed state reflects the observation and never invents confirmation.
4. **Given** a configured signal, **When** a direct signal command is requested, **Then** the existing command mapping and error behavior remain available.

### User Story 2 - Feedback remains observational (Priority: P1)

An operator sees block occupancy and uses InPort counts for journey rules without automatic resource ownership.

**Why this priority**: Feedback is useful independently of collision prevention.

**Independent Test**: Publish ordered block observations and exercise existing counter/journey regression fixtures.

**Acceptance Scenarios**:

1. **Given** configured block inputs, **When** clear, occupied, contradictory or missing observations occur, **Then** the block reports free, occupied, fault or unknown respectively and reserves no resource.
2. **Given** active journeys and incoming feedback, **When** counts match their rules, **Then** the same workflows run and counters retain their existing lifetime/reset behavior.
3. **Given** a disconnect, **When** the display is updated, **Then** block and turnout observations become unknown without any automatic signal or turnout command.

### User Story 3 - Only available capabilities are offered (Priority: P2)

An operator edits and loads a project with direct controls and occupancy, without route authoring or reservation actions.

**Independent Test**: Round-trip the retained definition, inspect UI bindings and verify operational selection using ViewModel tests.

**Acceptance Scenarios**:

1. **Given** an editable project, **When** it is saved, **Then** its current shape contains no routes, route ownership, locks or release configuration.
2. **Given** either operating page, **When** an object is selected, **Then** direct turnout controls and observational information remain, with no route editor, reserve/set/release/reconcile controls.
3. **Given** product help, **When** the operator reads the operating scope, **Then** it states their responsibility for collision-free operation and promises no automatic collision avoidance.

### Edge Cases

- Duplicate feedback cannot duplicate counts or regress confirmed state; stale command completion after disconnect cannot restore pending state.
- Replacing the active project cannot apply an old command completion to the new definition.
- Unknown/occupied blocks do not block direct turnout commands; invalid turnout mapping and disconnected transport still do.
- Existing removed JSON fields follow the normal serializer behavior; there is no conversion or legacy execution path.

## Requirements

### Functional Requirements

- **FR-001**: Remove route definitions, reservations, ownership, conflict evaluation and automatic locking/release throughout persistence, runtime, UI and tests.
- **FR-002**: Preserve direct turnout and signal command mapping, cancellation, connection checks and observed command results.
- **FR-003**: Preserve explicit block occupancy observations without affecting direct commands or automatically commanding hardware.
- **FR-004**: Preserve InPort counters, active-journey rule evaluation, accepted workflows and configuration defaults.
- **FR-005**: Remove route editor/actions and update help to state operator responsibility for collision-free operation.
- **FR-006**: Retain meaningful regression coverage for direct commands, feedback, serialization, DI, UI selection and journey isolation; remove tests solely for the superseded reservation model.
- **FR-007**: Add no replacement reservation mechanism, migrations, compatibility paths, counter actions or journey features.

### Key Entities

- **Turnout**: Stable identity, configured commands and confirmations, requested/observed position.
- **Block**: Stable identity, explicit feedback inputs and observed occupancy.
- **Signal**: Existing identity and command configuration; directly operated through existing controls.
- **Operational binding**: Maps an existing object to track-plan or signal-box representations.

## Success Criteria

- **SC-001**: No current project or operating control can define, reserve, lock or release a route.
- **SC-002**: All preserved direct command and feedback acceptance scenarios pass without physical hardware.
- **SC-003**: All existing InPort and active-journey regression scenarios pass on the integrated baseline.
- **SC-004**: Both operating pages retain their distinct selection and direct-control workflows with zero route controls.

## Assumptions

- Pure block occupancy remains in scope to preserve; removing it would require a separate product decision.
- Implementation starts from reviewed PR #148 and incorporates integrated main before publishing a separate draft PR.
- App starts, manual theme checks and hardware actions require separate authorization; no such actions are included here.
