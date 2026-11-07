# Feature Specification: Keep selected-locomotive STOP visible

**Feature Directory**: `012-mobasmart-fixed-stop`
**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/304
**Created**: 2026-10-15
**Status**: Implemented and locally validated; device acceptance in progress
**Spec Kit**: Required
**Input**: Adopt the approved fixed-header pilot in the actual MOBAsmart Control tab.

## Governance and Traceability

**Affected Platforms**: MOBAsmart Android.
**Out of Scope**: Desktop changes, global emergency stop, new runtime owners/commands, lifecycle or persistence changes, the P2 dependency service, browser simulation UI, and unmerged localization changes.
**Sensitive Data**: None; validation uses synthetic locomotive identities.
**Data and API Effects**: None. Existing project JSON, configuration, safe startup, command routing and Z21 protocol remain unchanged.

## User Scenarios & Testing

### User Story 1 - Reach STOP while inspecting functions (Priority: P1)

An operator inspecting later locomotive functions can still identify and stop the selected locomotive without scrolling back.

**Why this priority**: STOP must remain reachable during function operation.
**Independent Test**: In the Control tab, inspect the beginning, middle and end of F0-F31 and activate the selected-locomotive STOP using an isolated command target.

**Acceptance Scenarios**:

1. **Given** a selected locomotive, **When** the operator scrolls to F31, **Then** its name, DCC address and one clearly scoped STOP remain fully visible.
2. **Given** the end of the list, **When** STOP is activated once, **Then** the existing selected-locomotive operation runs once without changing other locomotives or function states.
3. **Given** a scrolled list, **When** the operator needs speed or direction controls, **Then** the existing controls remain reachable in the same tab.

### User Story 2 - Identify the current STOP target (Priority: P2)

An operator switching locomotives sees the new identity and address before acting on that locomotive.

**Why this priority**: A retained STOP must not suggest a stale target.
**Independent Test**: Switch between two differently named/addressed locomotives while viewing F31, then stop each through an isolated command target.

**Acceptance Scenarios**:

1. **Given** locomotive A and a scrolled list, **When** selection changes to B, **Then** the fixed identity/address update and STOP uses B through existing local/remote routing.
2. **Given** no valid address or command availability, **When** the Control tab is shown, **Then** existing empty/disabled states remain; no fallback command is sent.

### User Story 3 - Read and operate at enlarged text (Priority: P2)

An operator using enlarged system text can read the STOP target and reach every function in both themes.

**Why this priority**: The fixed area must not consume or cover the usable function region.
**Independent Test**: On a narrow portrait Android layout, use system text scaling 100%, 150% and 200% in Light and Dark.

**Acceptance Scenarios**:

1. **Given** enlarged text and a long locomotive name, **When** inspecting any list position, **Then** identity, address and STOP remain readable without overlapping controls; within the function section the available scrolling area fits at least one complete operable function row.
2. **Given** keyboard or accessibility navigation, **When** reaching STOP or a function, **Then** its purpose and state are conveyed and the selected-locomotive scope is explicit.

### Edge Cases

- Long names wrap up to two display lines with an explicit trailing ellipsis; the full name remains the accessible label and unchanged stored value. The separately visible DCC address distinguishes similarly prefixed names. No character-length limit is imposed on the model.
- A locomotive switch at F31 can reflow content; the end remains reachable without reducing text size.
- No locomotive, invalid/zero address, disconnected or remote-unavailable state keeps existing command availability.
- Large text, short available page height and platform chrome are assessed together on Android; browser pixel measurements are not acceptance evidence.
- Function changes, All off, direction and speed retain existing command behavior.

## Requirements

### Functional Requirements

- **FR-001**: Name, DCC address and a single selected-locomotive STOP MUST remain visible while the function content scrolls.
- **FR-002**: STOP MUST visibly identify its scope as the selected locomotive and use the existing selected-locomotive operation exactly once per activation.
- **FR-003**: Selection changes MUST update displayed target and command target consistently without adding selection state.
- **FR-004**: All F0-F31 controls and existing speed, direction and All off operations MUST remain reachable.
- **FR-005**: Narrow portrait and system text sizes through 200% MUST retain readable target/STOP, no overlap or horizontal scrolling, and space for at least one complete operable row when viewing the function section. Long names use the bounded presentation defined in Edge Cases.
- **FR-006**: Light/Dark appearance, meaningful accessible names/state and at least 44 by 44 device-independent touch targets MUST be preserved for affected controls.
- **FR-007**: No JSON, configuration, public API, protocol, runtime ownership, persistence or startup behavior MUST change.
- **FR-008**: Android compile, shared-command regressions and actual device/theme/scaling checks MUST be reported separately; unavailable checks MUST remain open.

### Key Entities

- Selected locomotive: existing identity, name and DCC address; no new stored data.
- Selected-locomotive STOP: existing operation scoped to the current target.
- Function: existing F0-F31 identity, meaning and on/off state.

## Success Criteria

### Measurable Outcomes

- **SC-001**: At five list positions (0/25/50/75/100%), the selected identity/address and entire STOP remain visible for all six theme/text-size combinations.
- **SC-002**: One STOP activation after A-to-B and B-to-A switching targets only the displayed locomotive exactly once; function states remain unchanged.
- **SC-003**: Every F0-F31 control is reachable and operable at 200% text in narrow portrait; while viewing the function section at least one complete row fits beneath the fixed area.
- **SC-004**: A reviewer can distinguish executed command/compile evidence from pending Android visual and interaction acceptance.

## Assumptions

- Existing locomotive selection is retained; this feature adds no new picker or global stop.
- Only critical identity/STOP belongs in the fixed area. Existing speed/direction and function content may share the bounded scrolling area.
- English remains the current product text convention; unmerged #140/#294 resources are not imported.
- The user subsequently authorized the UI/UX expert to start an isolated Android emulator and MOBAsmart for 200% system text, Light/Dark and TalkBack checks. No real layout or hardware control is authorized. Device acceptance requires actual observations.

## Clarifications

### Session 2026-10-15

No critical ambiguities detected worth formal clarification. Zero user questions asked. Functional scope, entities, interaction/error states, accessibility, quality constraints, dependencies, edge cases, tradeoffs, terminology and completion signals are clear from #304 and the accepted pilot.

Designer review subsequently clarified two acceptance details before product edits: complete-row space applies within the function section (speed controls precede it); long names use at most two visual lines with ellipsis, a full accessible name and separate DCC address. These are reversible presentation decisions, not new product mechanics.
