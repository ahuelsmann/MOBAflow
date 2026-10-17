# Feature Specification: Train consist card

**Feature Directory**: `014-train-consist-card`

**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/321

**Created**: 2026-10-17

**Status**: Draft

**Spec Kit**: Required

**Input**: Plan a read-only card for an existing user-defined train, on screen and on A5 paper, using the assigned product/UX handoff.

## Governance and Traceability

**Affected Platforms**: MOBAflow Windows desktop; platform-neutral presentation logic in SharedUI.

**Out of Scope**: Product implementation in this planning package; prototypes; Android/API/display features; automatic train formation; manifests or freight scheduling; persistent check-off/progress/history; auto-refresh; deferred changes; games; length/platform fit; runtime or physical-presence certification; hardware commands; QR codes; mandatory photos. #306/#309 are completed reference material, not reopened work.

**Sensitive Data**: Project and vehicle names are user-authored. Include only the selected project's card fields; no credentials, local file paths, network configuration, decoder backup data or unrelated projects. Use synthetic data for validation.

**Data and API Effects**: No changes to saved Solution JSON, schema version, settings/defaults, layouts, REST/SignalR APIs or Z21 protocol. A card is temporary read-only presentation data, not a persisted entity. Printing does not save the Solution.

**Authority**: Issue #321, freshly read on 2026-10-17, plus the assigned AD/UX handoff accepted by the Chief Project Manager at 21:11:30 CEST. This package stops at planning and a planning Draft PR. Implementation and practical feature acceptance require a later assignment.

## User Scenarios & Testing

### User Story 1 - Read the planned train (Priority: P1)

A guest opens the card for the currently selected existing train and reads the locomotive and wagon positions without navigating between libraries.

**Why this priority**: A complete, honest composition is the card's primary value.

**Independent Test**: Generate a card from a synthetic selected project/train with seven positions, including all three vehicle kinds, similar names, a repeated occurrence, a long name and a missing reference. Compare every numbered position with the edited definition.

**Acceptance Scenarios**:

1. **Given** an existing selected train, **When** its card opens, **Then** its name, project, creation time and "Planned consist · Edited Solution data" are visible.
2. **Given** mixed and repeated vehicle references, **When** the card is created, **Then** every occurrence appears once at its original one-based position, including repeated occurrences of the same vehicle.
3. **Given** similar names, **When** the card is read, **Then** the type and actual available identity distinguish different records; a configured locomotive DCC address is shown as configured data, and an absent address is explicitly unconfigured.
4. **Given** an empty consist, **When** its card opens, **Then** it states that the planned consist is empty, without invented positions.
5. **Given** a missing, invalid or ambiguous reference, **When** its position is read, **Then** that exact position remains visible with an explanatory text diagnostic and the available reference identity.

### User Story 2 - Use the card accessibly (Priority: P1)

An operator opens, reads and closes the card using the keyboard, including with larger text and either application theme.

**Why this priority**: The card must be usable by the same participants who use the existing train view.

**Independent Test**: Navigate from the selected train to the card, its print action and Close using the keyboard; inspect Light/Dark and enlarged text.

**Acceptance Scenarios**:

1. **Given** keyboard focus in the existing train view, **When** the operator activates "Train card", **Then** focus moves into the read-only card and all actions are reachable without editing the train.
2. **Given** a long name or diagnostic, **When** the viewport narrows or text is enlarged, **Then** the complete text wraps and remains reachable by scrolling; color is not the only diagnostic signal.
3. **Given** an open card, **When** system print preparation is cancelled or ends, **Then** the card stays open and focus returns to its Print action; **When** the card is closed, **Then** focus returns to the invoking control when it still exists, otherwise to the train list.
4. **Given** no current train or a stale selection outside the selected project, **When** entry is evaluated, **Then** the action is unavailable or an honest unavailable-source message is shown, rather than another train's data.

### User Story 3 - Print the same planned card (Priority: P2)

An operator prepares and prints an A5-friendly copy that a guest can read alongside the screen card.

**Why this priority**: Paper supports shared preparation away from the computer.

**Independent Test**: Compare an A5 multipage output with the open card, including the seven-position example, many positions, enlarged text and an individual position taller than a page.

**Acceptance Scenarios**:

1. **Given** an open card, **When** print preparation is opened, **Then** it uses the same train/project identity, timestamp, position order and diagnostics as that card.
2. **Given** content exceeding one A5 page, **When** pages are produced, **Then** content continues onto additional pages with readable text, repeated card identification, page x of y and uninterrupted occurrence numbering.
3. **Given** a position that fits a page, **When** a break is needed, **Then** the position is kept together where possible; a position exceeding a page is split into readable, explicitly associated continuations without clipping or endless empty pages.
4. **Given** printing is unsupported, unavailable, cancelled or fails, **When** the operation ends, **Then** an honest status appears and the existing card remains available for reading or an explicit retry.

### Edge Cases

- Same GUID in another project or another vehicle kind: do not resolve across either boundary.
- Empty GUID, undefined kind, duplicate target IDs in the same typed project collection, a null occurrence or a missing target: retain the source position and diagnose it; never select an arbitrary record.
- Empty/duplicate project or train identity: disclose the source ambiguity; do not claim an exact global diagnostic link. A stale train object that is no longer a member of the selected project is unavailable.
- Missing name: use an explicit unnamed label with the available identity; do not invent a name.
- A null/unreadable occurrence collection is unavailable data, not a successful empty consist.
- Edits or selection changes after opening: the generated card remains its original copied stand; reopening explicitly creates a new card. No background regeneration, history, persistence or pending apply state.
- Names containing markup characters, very long unbroken text, multiline text or non-ASCII user text: display as data with no truncation or execution.
- System print callbacks, cancellation, page-option changes and repeated open/close must not leak registrations, cross cards or change the source stand.

## Requirements

### Functional Requirements

- **FR-001**: Users MUST open a card for a selected existing user-defined train from the existing train view; absent/stale selection MUST NOT silently select a substitute.
- **FR-002**: Generation MUST copy the selected edited project/train data into one read-only card; it MUST NOT use a runtime definition or physical observation as its source.
- **FR-003**: Each source occurrence MUST retain its zero-based source index and displayed one-based position, original ordering and multiplicity across locomotive, passenger-wagon and goods-wagon kinds.
- **FR-004**: A reference MUST resolve only against its declared vehicle kind in the selected project, with exactly one matching non-empty identity; missing, unknown-kind, empty-ID, null or ambiguous entries MUST remain at their original positions with text diagnostics.
- **FR-005**: Empty consists MUST be explicitly empty; invalid/unavailable source data MUST be distinguished from a successfully inspected empty list. Invalid source identities MUST be disclosed without false uniqueness.
- **FR-006**: Each valid position MUST show the complete vehicle name, type, available identity and, as text, a reversed orientation (`Vehicle.IsReversed`) of that occurrence; configured locomotive DCC addresses MUST be labeled as configured values, and absent ones as "DCC not configured". Full identities MUST remain available; shortened identifiers MUST NOT imply uniqueness. No new model fields are required.
- **FR-007**: The header MUST identify train and project, show "Planned consist · Edited Solution data", and a single creation timestamp with an unambiguous time-zone offset; the card MUST state that it certifies neither the physical train nor runtime/decoder state.
- **FR-008**: Screen and print MUST consume the same generated contents and creation time; edits after generation MUST NOT silently alter either. Closing and explicitly reopening is the way to obtain a fresh card.
- **FR-009**: Opening, generating, printing, retrying and closing MUST NOT mutate definitions, save the Solution, start/update runtime owners, invoke operator commands or operate hardware.
- **FR-010**: Screen text MUST wrap without losing content, scroll when needed, support Light/Dark and enlarged text, and communicate diagnostics by text as well as any visual emphasis.
- **FR-011**: Entry, print preparation and Close MUST support keyboard navigation and accessible names; when print preparation is cancelled or ends, the card MUST stay open and focus MUST return to its Print action; Close MUST return focus to the invoking control, or to the train list when that control no longer exists.
- **FR-012**: A5 output MUST preserve all card content and use additional pages instead of reducing text below a readable size; each page MUST retain card identity and page x of y, with uninterrupted position numbering. Long train/project names MUST continue across pages like other content rather than fail: the repeated page header uses a bounded identifier while the first page carries the complete, continuable identity. "Larger requested text" means the Windows text-size setting (text scale factor) scales the 11-point body floor upward; no separate print text-size control is added.
- **FR-013**: Pagination MUST prefer keeping a position together when it fits; an overheight position MUST have readable identified continuations with forward progress, no clipped text and no endless keep-together loop.
- **FR-014**: Print preparation MUST report unsupported printing, failures and cancellation honestly, retain the readable card and permit an explicit retry without regenerating its contents.
- **FR-015**: Diagnostics MUST reuse the existing typed location convention where applicable: project, train, zero-based occurrence index, one-based displayed position and reference field. Existing locomotive Messages identifiers MAY be associated only where identities are valid; wagon diagnostics MUST NOT be mislabeled as locomotive findings.
- **FR-016**: Delivery evidence MUST separate automated data checks, actual Windows UI acceptance, A5 digital output and actual physical print acceptance. Unperformed checks MUST remain unperformed; preview alone MUST NOT be reported as a physical print.

### Key Entities

- **Planned card**: Copied project/train identity and names, one creation time, ordered positions and source diagnostics; temporary and read-only.
- **Card position**: One original occurrence, its index/number, declared type, available target identity, copied display data and local diagnostics. Two occurrences can refer to the same vehicle.
- **Diagnostic location**: The edited project/train and exact source occurrence/field; it is separate from English display text.
- **Print page**: A paper layout of the existing card, with page identity/count and continuation metadata; it is not a new card observation.

## Success Criteria

### Measurable Outcomes

- **SC-001**: In the seven-position example and boundary fixtures, 100% of source occurrences remain in order at the correct positions, including every repeat and invalid reference; no wrong-kind or wrong-project match appears.
- **SC-002**: Screen and output comparisons agree on 100% of train/project identities, creation time, position data and diagnostics for each generated card, including after a source edit.
- **SC-003**: At normal and 200% Windows text size in Light and Dark, all card content and controls remain readable/reachable; the open-print-cancel-close journey completes using only the keyboard with the required focus return.
- **SC-004**: At A5 portrait, the ordinary, multipage and overheight-position cases retain all text without clipping or omitted positions, show correct page counts and continuations, and use at least 11-point body text; larger requested text creates additional pages.
- **SC-005**: Automated safety checks observe zero definition mutations, Solution writes, runtime-owner start/update calls, operator commands and hardware calls across generation and presentation interactions.
- **SC-006**: The acceptance record identifies every executed check's scope and outcome, and separately records Windows UI, digital A5 output and physical A5 print results or their concrete remaining limitation.

## Assumptions

- This first delivery targets the existing Windows train editor; no Android or railroad hardware acceptance gate is introduced.
- The card depicts the edited model, including values not yet persisted. No load/save or runtime synchronization is required to obtain it.
- Existing model IDs are the available record identity, not a physical vehicle label or a certificate of uniqueness in malformed input.
- The 11-point A5 floor and 200% text check make the approved readability requirement measurable; they are routine planning defaults, not observed usability results.
- Photos and optional repeat badges are not required. Occurrence numbering, names, kind and full record identity meet the first scope without extra fields.

## Clarification review

The assigned issue and Art Director/Chief Project Manager handoff resolve product scope, source, entry flow, snapshot lifetime, paper format and exclusions. No new human question was needed. Functional, domain, interaction, edge-case, constraints, terminology and completion categories are clear. Printing platform details and failure handling are addressed in the technical plan; no uptime/service or legal compliance target applies to this local read-only feature. This review does not claim implementation or acceptance.
