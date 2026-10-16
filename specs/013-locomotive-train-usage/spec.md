# Feature Specification: Locomotive usage in train consists

**Feature Directory**: `013-locomotive-train-usage`

**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/306

**Created**: 2026-10-15

**Status**: Planning review - product implementation conditionally authorized for the assigned engineer after all planning gates pass; no product code implemented by the specification author

**Spec Kit**: Required

**Input**: Create the issue and Spec Kit artifacts for the first locomotive-to-train-consist slice, agree the runtime query contract with Brain, and implement no product code.

**Authorization update (2026-10-16)**: The user subsequently authorized completing planning and starting implementation in this same worktree if all planning checks pass. Art Director assigns implementation to Ingenieur after an explicit green handoff; Lokfuehrer remains the planning author until that handoff and makes no product changes.

## Governance and Traceability *(mandatory)*

**Affected Platforms**: Backend and SharedUI libraries; desktop MOBAflow Messages. Other hosts remain compatible; no new Android screen or remote endpoint.

**Out of Scope**: Delete changes/permission, warning or blocking policy, cascading removal, automatic deactivation/cancellation, other target relationships, generic dependency graph, new navigation/page, comprehensive validator unification, full operative-use tracking and runtime lifecycle redesign.

**Sensitive Data**: No credentials or real layout files. Tests use invented projects and fake owners. Do not export local file paths or runtime implementation objects.

**Data and API Effects**: Future internal read-only library contracts and diagnostics projection only. No JSON/schema version, persisted configuration/layout, REST/SignalR or Z21 change; no new package.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Find uses in edited data (Priority: P1)

An operator selects a locomotive and asks where it is used. Every train and vehicle position appears, including unsaved edits, without changing data.

**Why this priority**: Understand the relationship before considering future removal or reassignment.

**Independent Test**: Compare arranged unused/singly/repeatedly used locomotives and equal IDs in other projects/types against the complete result.

**Acceptance Scenarios**:

1. **Given** two positions in one train and one in another reference the target, **When** queried, **Then** all three appear with project/train names and positions.
2. **Given** an unsaved vehicle-entry edit, **When** refreshed, **Then** the edit appears without loading or saving a file.
3. **Given** the target is missing in the requested project, **When** queried, **Then** it is reported as missing, not as an existing unused locomotive.
4. **Given** the same ID belongs to another project or vehicle type, **When** queried, **Then** it is not attributed to this target.

### User Story 2 - Find broken locomotive references (Priority: P1)

The operator sees a data error identifying the exact train entry whose locomotive no longer exists.

**Why this priority**: Uses and missing targets must follow the same relationship definition.

**Independent Test**: Diagnose a project with valid and missing targets, repair the broken entry, and refresh.

**Acceptance Scenarios**:

1. **Given** a locomotive entry has a missing target, **When** diagnostics refresh, **Then** existing Messages identifies the train/entry as an error.
2. **Given** a valid assignment, **When** uses are shown, **Then** it is neutral information and increases no error/warning count.
3. **Given** a broken entry is repaired, **When** that source refreshes successfully, **Then** the old error disappears without duplicate accumulation.

### User Story 3 - Compare runtime evidence honestly (Priority: P1)

The operator distinguishes edited data from actual loaded runtime definitions. Missing evidence about retained older definitions and operative use remains visible.

**Why this priority**: Edited and loaded definitions can differ; selection or connection status is not usage evidence.

**Independent Test**: Fake owners expose differing definitions, missing owners, same-ID replacements and delayed responses.

**Acceptance Scenarios**:

1. **Given** only a corresponding existing runtime definition has a reference, **When** queried, **Then** its occurrence is shown separately, including for non-selected/disconnected projects.
2. **Given** a complete loaded-definition query finds zero references, **When** displayed, **Then** only that category reports zero; retained definitions and operative usage remain unknown.
3. **Given** an owner is missing, unsupported, disposed, replaced or wrong-context, **When** applying a response, **Then** it is unknown or discarded, never empty successful global non-use.
4. **Given** a successful answer followed by owner replacement with the same project ID before display, **When** the answer arrives, **Then** it is rejected.
5. **Given** another solution is loaded into the same solution object, **When** an old answer arrives, **Then** it is rejected.

### Edge Cases

- Zero references differs from a missing target or unavailable source; names are not identifiers.
- Preserve repeated entries. Empty/duplicate train identities make exact identity ambiguous: report invalid/unknown scope, never fabricate uniqueness. An empty locomotive ID in an entry is a broken reference.
- Definition changes during capture produce one consistent version; later changes invalidate the observation.
- Runtime A can be complete while the aggregate is partial because B/C remain unknown.
- Remote snapshots, fleet presence, address telemetry, speed zero and bounded traces do not prove usage completeness.
- Cancellation never becomes an empty successful result; old results cannot survive a changed query/context.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Identify a target by loaded context, project, type Locomotive and ID; distinguish missing from existing but unreferenced targets.
- **FR-002**: Read every direct locomotive-to-train-vehicle occurrence in the edited solution, including unsaved changes and repeated positions.
- **FR-003**: Diagnose missing locomotive targets using the same relationship definition as the usage query.
- **FR-004**: Return source/project/train/position/field/target locations; distinguish stable rule identity from finding-instance identity.
- **FR-005**: Query actual current definitions of corresponding existing runtime owners, independent of selection/connectivity; never create/update a runtime for analysis.
- **FR-006**: Materialize one consistent immutable observation; expose no mutable runtime project objects.
- **FR-007**: State separate coverage for edited data, current loaded runtime definitions (A), retained older definitions (B) and operative use (C). B/C are unknown in this slice; A complete never implies aggregate runtime complete.
- **FR-008**: Represent absent/unsupported/failed/cancelled/wrong-context evidence explicitly; infer neither global non-use nor deletion permission.
- **FR-009**: Invalidate pending/displayed results on relevant load/project/target/owner/definition changes, including in-place reload and same-ID owner replacement; revalidate immediately before applying results.
- **FR-010**: Reuse project diagnostics and Messages; replace active source-scoped results, keep uses neutral, missing targets as errors and unknown coverage visible.
- **FR-011**: Analysis causes zero model changes, saves, Delete/Reset, workflows/request-factory executions, hardware commands or runtime lifecycle operations.
- **FR-012**: Preserve existing input/execution/mutation-time checks, defaults, safe startup, formats and persisted layouts.
- **FR-013**: Keep shared logic platform-neutral; use existing constructor injection/lifetimes, cancellation and UI dispatch rules; introduce no remote protocol.

### Key Entities *(include if feature involves data)*

- **Target**: Loaded-context/project/type/ID identity and source-specific existence.
- **Occurrence**: A particular train vehicle entry referencing the target.
- **Finding**: Stable rule plus source-scoped occurrence, classified as valid use or missing target.
- **Coverage**: Checked category, completeness and concrete reason for unknown evidence.
- **Observation**: Copied source results with time and short-lived validity receipt; never a delete permission.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All arranged references/missing targets have the correct project/type/position; zero false cross-project/type matches.
- **SC-002**: Every runtime result states A/B/C coverage, including zero-reference A results; zero global non-use/delete-approval claims.
- **SC-003**: Zero stale answers applied in same-ID replacement, definition-update, target-change and in-place reload cases; unchanged-owner controls accepted.
- **SC-004**: Identical repeated checks keep finding identity/order and create no duplicates; repaired findings disappear only after successful source refresh.
- **SC-005**: Side-effect recorders observe zero analysis-triggered operations listed in FR-011.
- **SC-006**: Existing filters and English source/coverage labels distinguish use, error and unknown evidence; later Light/Dark/keyboard checks cover the Messages integration.

## Assumptions

- Direct vehicle references are project-local. All solution projects can be considered without merging equal IDs across projects.
- Desktop is the first presentation consumer; other hosts get no new screen/endpoint.
- The relationship scan is bounded by train entries and performs no I/O; no timing promise before measurement.
- Later Delete policy and atomic mutation validation remain a separate decision, not an unanswered requirement for this read-only slice.

## Clarifications

### Session 2026-10-15

- Q: Which runtime categories are in scope? A: Real owner query for current loaded train definitions (A); retained older definitions (B) and operative usage (C) explicitly unknown, without general workflow tracking. Agreed with Brain.
- Q: Is a deletion guard authorized? A: No. Read-only use/missing-target reports only; Delete behavior and warning/blocking policy unchanged.
- Q: How is context validity established? A: Existing session-load events cancel request-local contexts; backend receipts bind existing owner/definition identities and are revalidated. No persistent generation, lease or second runtime.
- Q: Does remote unavailability require a new endpoint? A: No; explicit unknown/unsupported evidence.
- Q: Is click-through navigation included? A: No; precise locations in existing Messages only.
