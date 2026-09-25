# Feature Specification: Remove vehicle maintenance from the MVP

**Feature Branch**: `codex/issue-147-remove-maintenance`
**Created**: 2026-09-24
**Status**: Approved scope
**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/147
**MVP decision**: https://github.com/ahuelsmann/MOBAflow/issues/144
**Input**: Remove all vehicle maintenance, including calendar maintenance, from version 1.0.

## Governance and Traceability

**Feature Directory**: `004-remove-maintenance`
**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/147
**Spec Kit**: Required
**Affected Platforms**: Windows UI, shared libraries, Android project data and MOBApi solution transport.
**Out of Scope**: Vehicle statistics, reservations, Journey/workflow behavior, new maintenance features.
**Sensitive Data**: None required; use synthetic regression data.
**Data and API Effects**: Remove vehicle maintenance from stored/shared data and passports without migration.
Configuration defaults, operational controls, safe startup and saved layouts remain unchanged.

## User Scenarios & Testing

### User Story 1 - Manage rolling stock without maintenance (Priority: P1)

Operators manage locomotives, passenger wagons and goods wagons without maintenance plans,
history, reminders or due-state filters. Their vehicle inventory and operational controls remain available.

**Why this priority**: This is the explicitly approved version 1.0 scope reduction.
**Independent Test**: For each vehicle kind, search, select, add, edit, delete and switch projects;
save and reopen the resulting inventory. Export a locomotive passport and retain decoder backups.

**Acceptance Scenarios**:

1. **Given** an inventory of all three vehicle kinds, **when** the operator searches and edits it,
   **then** matching vehicles and selection stay current and changes are saved without maintenance controls.
2. **Given** vehicle master data and decoder backups, **when** saved data is loaded or shared with mobile,
   **then** those values remain intact and no vehicle maintenance data is produced.
3. **Given** a locomotive, **when** its passport is exported, **then** inventory and decoder information
   remain available without maintenance history or status.
4. **Given** the current user documentation, **when** an operator checks the MVP scope,
   **then** calendar maintenance is not advertised as available.

### Edge Cases

- Empty inventory, no selected project or vehicle, and switching between populated and empty projects.
- Adding or deleting a matching vehicle while a search is active; renaming a vehicle into/out of a search.
- A project file contains obsolete maintenance fields: they are discarded by normal loading and are
  not retained when saved. No migration, recovery or compatibility feature is introduced.
- The train category for maintenance/works trains is inventory terminology, not vehicle servicing.

## Requirements

### Functional Requirements

- **FR-001**: Remove vehicle maintenance plans, intervals, reminders, due states, history and costs
  from every vehicle kind, including all associated UI, services, stored fields and presentation contracts.
- **FR-002**: Preserve vehicle master data, photos, consist membership, decoder/CV backups, passport
  export, search, selection, add/delete/edit, automatic saving and operational controls.
- **FR-003**: Newly saved and shared project data contains no vehicle maintenance data; obsolete
  properties have no retained representation. Do not add migration or legacy paths.
- **FR-004**: Update current documentation and supersede the earlier calendar-maintenance retention
  statement. Do not reintroduce vehicle statistics; any reconsideration remains issue #143.
- **FR-005**: Preserve the maintenance/works-train category. Any future vehicle maintenance requires
  a separate product decision. No unrelated feature changes are authorized.

### Key Entities

- **Rolling stock**: locomotive, passenger wagon and goods wagon inventory, without maintenance records.
- **Decoder backup**: retained locomotive configuration and captured CV values.
- **Locomotive passport**: retained inventory/decoder summary, without maintenance sections.

## Success Criteria

### Measurable Outcomes

- **SC-001**: All three vehicle pages offer zero vehicle-maintenance controls or filters.
- **SC-002**: Inventory, selection and save/load regression scenarios pass for all three vehicle kinds.
- **SC-003**: Saved/shared inventories and exported passports contain zero vehicle-maintenance fields.
- **SC-004**: Current feature descriptions contain no promise of available calendar maintenance.

## Assumptions

- Scope is fully decided by #147 and #144; no clarification is needed.
- Windows UI and shared Windows/Android data are affected. No app start or hardware action is authorized.
- Workflow, Journey, reservation and timetable changes are coordinated separately by the master task.
