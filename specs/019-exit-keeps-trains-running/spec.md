# Feature Specification: Trains keep running when MOBAflow closes

**Feature Directory**: `019-exit-keeps-trains-running`

**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/426

**Created**: 2026-11-10

**Status**: Implemented with fakes; behavior on a real Z21 is not verified.

**Spec Kit**: Required

**Input**: Maintainer feedback while using MOBAflow on 2026-11-08 (verbatim): "beim beenden der mobaflow anwendung
nicht mehr nach dem stop der trains fragen. bei absturz oder beenden soll also weiter laufen". In English: closing
MOBAflow must no longer ask whether to stop the trains; the trains keep running when MOBAflow exits or crashes.

## Governance and Traceability *(mandatory)*

**GitHub Issue**: #426

**Related Work**: [specs/008-project-runtimes](../008-project-runtimes/spec.md) (#191) defined the warning and speed 0
"before a switch or close" (User Story 2, FR-006, SC-002). This feature replaces the "close" part of that rule; the
"switch" part is unchanged. [specs/017-controlled-locomotive-delete](../017-controlled-locomotive-delete/spec.md)
(#389, the delete guard) is unchanged. RF-24 (#219) changes the same shell class; this feature builds on slice 5b
(#425).

## Scope

Only closing MOBAflow, including an unexpected end of the process. No other way of discarding a runtime changes.

## User Scenarios & Testing

### User Story 1 - Close MOBAflow while trains run (Priority: P1)

An operator closes MOBAflow while a project is connected to its Z21 and trains are running. MOBAflow closes without
asking about the trains, and the trains keep running on the Z21.

**Independent Test**: With fake Z21 endpoints, connected runtimes and driving locomotives, run the shutdown; no dialog
is shown, no stop command reaches a Z21 and every Z21 connection is closed.

**Acceptance Scenarios**:

1. **Given** a project is connected to its Z21, **When** the operator closes MOBAflow, **Then** no "Stop all trains?"
   dialog appears and MOBAflow closes.
2. **Given** locomotives are driving, **When** MOBAflow closes, **Then** no Z21 receives a locomotive speed command,
   an emergency stop or a track-power-off command; each runtime is discarded and each Z21 connection is closed.
3. **Given** the solution has unsaved changes, **When** the operator closes MOBAflow, **Then** the save prompt appears
   as before, and cancelling it keeps MOBAflow open with everything running.
4. **Given** MOBAflow ends without its shutdown workflow (a crash, or a failed workflow followed by service disposal),
   **Then** no stop command is sent.

### User Story 2 - Switching a solution still stops the trains (Priority: P1)

**Acceptance Scenarios**:

1. **Given** a project is connected, **When** the operator creates or opens another solution, **Then** "Stop all
   trains?" still asks, cancelling changes nothing, and confirming sends speed 0 before the runtimes are discarded.
2. **Given** a connected project is removed or its Z21 assignment changes, **Then** its known locomotives still
   receive speed 0 before its runtime is discarded.

## Requirements

- **FR-001**: Closing MOBAflow MUST NOT show the stop-trains confirmation.
- **FR-002**: Closing MOBAflow MUST discard every project runtime and close every Z21 connection without sending a
  locomotive speed, emergency-stop or track-power command.
- **FR-003**: Disposing the runtime host at the end of the process MUST NOT send a stop command, and no crash handler
  may add one.
- **FR-004**: Opening or creating another solution, removing a project, changing a project's Z21 and a manual
  disconnect MUST keep their existing stop (and, where it exists, their confirmation).
- **FR-005**: The unsaved-changes save prompt and its cancel, safe startup (speed 0, no restored movement) and the
  delete guard for a runtime-controlled locomotive MUST stay unchanged.
- **FR-006**: The user guide MUST state that running trains keep running after MOBAflow closes and that control
  through MOBAflow, and through MOBAsmart via MOBAflow, ends.

## Success Criteria

- **SC-001**: Tests with fake Z21s show 0 stop commands on shutdown and on host disposal, and one disconnect per
  Z21 connection.
- **SC-002**: The existing tests for stopping on load, remove, replace and manual disconnect pass unchanged.
- **SC-003**: The shell test shows no dialog on shutdown with a connected runtime; the new-solution test still asks.

## Out of Scope and Limits

- What a real Z21 does with running locomotives after its last LAN client logs off is not verified here; no
  hardware, track power or app launch is part of this work.
- A Z21 feature that stops trains by itself (for example a configured timeout) is outside MOBAflow.
