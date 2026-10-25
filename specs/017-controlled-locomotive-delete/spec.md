# Feature Specification: A controlled locomotive cannot be deleted

**Feature Directory**: `017-controlled-locomotive-delete`

**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/389

**Created**: 2026-10-24

**Status**: Implemented in draft [PR #392](https://github.com/ahuelsmann/MOBAflow/pull/392) at `0136b5d7`; practical
acceptance W6 in [#365](https://github.com/ahuelsmann/MOBAflow/issues/365) is open.

**Spec Kit**: Required

**Chronology**: The defect was found by reading the code in the architecture audit on 2026-10-23. The maintainer's
hard requirement is that a locomotive that is currently controlled must not be deleted while the runtime runs, also at
speed 0. The implementation (`0136b5d7`, 2026-10-24 00:36 CEST) came first. This specification, the plan, the tasks
and the analysis were written afterwards on 2026-10-24, at the maintainer's request through the Chief Project
Manager. They record what was decided and verified; they do not claim an earlier planning run.

**Input**: Issue #389.

## Governance and Traceability *(mandatory)*

**GitHub Issue**: #389

**Related Work**: #390 and #391 (the other audit findings). [specs/013-locomotive-train-usage](../013-locomotive-train-usage/spec.md)
(#306) shows locomotive usage read-only; it neither implements nor authorizes deletion behavior and is not changed.
[specs/008-project-runtimes](../008-project-runtimes/spec.md) defines one runtime per project and the runtime snapshots
this feature reads.

## User Scenarios & Testing

### User Story 1 - A controlled locomotive stays (Priority: P1)

**Acceptance Scenarios**:

1. **Given** the Z21 is connected and a locomotive is selected in Train Control at speed 0, **When** the operator
   selects it on the Locomotives page, **Then** Delete is disabled and the page shows "Stop controlling this locomotive
   before you delete it."
2. **Given** the locomotive is driving, **Then** Delete is disabled.
3. **Given** the delete command is invoked anyway (without `CanExecute`), **Then** the locomotive is not deleted and a
   warning is logged.

### User Story 2 - Delete works again when the locomotive is released (Priority: P1)

**Acceptance Scenarios**:

1. **Given** the runtime no longer controls the locomotive (for example after disconnecting from the Z21), **Then**
   Delete is available again without reselecting the locomotive.
2. **Given** the locomotive has no digital address, or another project's runtime uses the same address, **Then** Delete
   is available.

### Edge Cases

- A locomotive the runtime only saw on the Z21 (another throttle) counts as controlled: the runtime holds a state for it.
- After "Stop trains and disconnect" the runtime keeps its states. They are ignored while disconnected; after a
  reconnect they count again until the project's runtime is reactivated (deliberately conservative).
- Train Control behavior when its locomotive disappears through another path (external file change, remote client) is
  not observed yet and stays an open question.

## Requirements

- **FR-001**: A locomotive is controlled when its project's runtime is connected to its Z21 and holds a runtime state for
  the locomotive's digital address.
- **FR-002**: Delete MUST be disabled for a controlled locomotive, and the delete command MUST refuse it.
- **FR-003**: The page MUST explain in English why Delete is disabled.
- **FR-004**: The rule MUST read the existing runtime snapshots (`IProjectRuntimeSnapshots`); the ViewModel does not
  duplicate runtime state.
- **FR-005**: The control state MUST update on the UI thread, and only when the set of controlled addresses changes.
- **FR-006** (review, 2026-10-25): Changing the selected locomotive's digital address MUST update the delete state and its
  explanation without waiting for a runtime snapshot.
- **FR-007** (review, 2026-10-25): The control state MUST follow each project's current snapshot: a late event of an
  older snapshot MUST NOT replace a newer state, and a snapshot published while the service starts MUST NOT be lost.
- **FR-008** (review, 2026-10-25): When the Z21 connection becomes active, Train Control MUST request the locomotive it
  selected while disconnected, so that locomotive counts as controlled.

## Success Criteria

- **SC-001**: Tests cover a locomotive at speed 0, a driving locomotive, the uncontrolled cases, another project's runtime
  and a released locomotive.
- **SC-002**: Practical acceptance W6 in #365 with the Z21, a locomotive and launch approval.
