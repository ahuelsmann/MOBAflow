# Feature Specification: UI localization - first slice

**Feature Directory**: `011-ui-localization`

**Source Issue**: #140

[GitHub issue](https://github.com/ahuelsmann/MOBAflow/issues/140)

**Created**: 2026-10-14

**Status**: Draft - implementation scope clarified

**Spec Kit**: Required

**Input**: English source/fallback UI and German translations on desktop and Android,
with persistent system/explicit language selection and platform-neutral shared text.

## Governance and Traceability *(mandatory)*

**Affected Platforms**: MOBAflow Windows, MOBAsmart Android, Common configuration and SharedUI.

**Out of Scope**: Live language switching, additional languages, translating user-authored
solution content, TTS providers/voices/settings, domain defaults, technical logs, API messages,
protocols, display firmware and documentation article translations (embedded or remote). Existing unlisted page
bodies and Settings sections remain English as tracked follow-up work in #140.

**Sensitive Data**: None added; do not collect or expose settings-file contents in diagnostics.

**Data and API Effects**: One app-local UI preference, independent on each host. No solution
schema/version, undo history, REST/SignalR contract, Z21 behavior, speed startup, runtime
ownership or persisted layout changes. All unrelated defaults remain unchanged.

## Clarifications

### Session 2026-10-14

- Q: Which UI scope should be implemented first? A: First slice on desktop and MOBAsmart,
  including language selection, navigation and independently extracted areas; remaining
  pages stay explicit follow-up slices in #140. User selected this option in this chat.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Choose my UI language (Priority: P1)

An operator chooses System, English or German in application settings on either device,
saves the preference and sees an explanation that a full app restart applies the choice.

**Why this priority**: A stable preference makes all later translation slices usable.

**Independent Test**: Start with a fake settings store; select/save/reload each choice and
verify active language changes only on the next startup, including a failed save.

**Acceptance Scenarios**:

1. **Given** System is selected and the system UI language is German, **When** the host
   starts, **Then** this slice uses German; all other system languages fall back to English.
2. **Given** an English session, **When** German is saved, **Then** a restart notice appears
   in the current language and the current UI stays English until a full restart.
3. **Given** German was saved, **When** the app restarts on an English system, **Then**
   German remains active and the selection still shows German.
4. **Given** a save fails, **When** the operator saves a language, **Then** an actionable
   localized error appears, the prior stored preference remains authoritative and retry is possible.

### User Story 2 - Read the covered interface (Priority: P1)

An operator can read desktop navigation, desktop Help chrome/contents navigation and Info content, mobile tab
navigation, and the new language settings area in English or German.

**Why this priority**: These areas provide a visible, reviewable slice without conflicting
with the ongoing Journey/Workflow/Counter extraction in RF-24/#219.

**Independent Test**: Resolve every catalog entry and dynamic template in both languages;
compile both hosts and manually inspect only the coverage rows marked Slice 1.

**Acceptance Scenarios**:

1. **Given** either supported language, **When** a covered view or message appears,
   **Then** labels, buttons, tooltips, empty/error/status text and accessible names use that language.
2. **Given** a translation is absent, **When** its known key is resolved, **Then** complete
   English text appears, never a blank or a resource key.
3. **Given** user-authored names and announcement text, **When** language changes and
   the app restarts, **Then** those values, TTS options and solution undo history are unchanged.
4. **Given** numbers/dates in a covered dynamic message, **When** displayed, **Then**
   they use the active display culture while serialized and technical values remain invariant.

### User Story 3 - Extend translations safely (Priority: P2)

A contributor adds a UI text or another language through a documented resource workflow.

**Why this priority**: Shared translations must not diverge between platforms.

**Independent Test**: Introduce a missing key, empty translation, wrong placeholder or
stale generated resource into isolated fixtures and verify the checker rejects each case.

**Acceptance Scenarios**:

1. **Given** a new language, **When** it is added through the documented workflow,
   **Then** shared text has one translation source and coverage checks validate all entries.
2. **Given** a missing key or mismatched template argument, **When** validation runs,
   **Then** it reports the offending key and fails before publication.

### Edge Cases

- System German variants such as de-AT/de-CH resolve to German; explicit choices are en-US/de-DE.
- Missing, null, malformed or unsupported preference values resolve safely to System without a crash.
- A missing German resource falls back to English; an unknown key is a development error caught by checks.
- The same app session keeps its startup language even when saved preference changes; no app is restarted automatically.
- Offline Android uses its local language preference; no server connection is needed and hosts do not synchronize UI language.
- Longer German text and text scaling must not hide controls or accessible names; keyboard focus remains usable.
- Custom page-label suffixes, technical product names, URLs and version identifiers are preserved verbatim.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Ship complete English and German UI text for exactly the Slice 1 surfaces in [coverage.md](coverage.md).
- **FR-002**: Provide System (default), English and German selection on both hosts; save locally across full restarts.
- **FR-003**: Apply language only at startup; saving shows a restart explanation without changing the running UI.
- **FR-004**: Resolve German system UI language families to German and every other language to English; invalid choices use System.
- **FR-005**: Resolve known missing translations to complete English text, without blanks or visible keys.
- **FR-006**: Translate complete dynamic templates, preserve argument order flexibility, and format UI values with the active display culture.
- **FR-007**: Maintain stable keys and context; one translation source owns shared strings across both hosts.
- **FR-008**: Check duplicate/missing/empty keys, template placeholder parity, used-key existence and generated-resource freshness.
- **FR-009**: Leave solution data, undo history, TTS options, domain/sample defaults, serialization, technical identifiers and protocol formats unchanged.
- **FR-010**: Preserve themes, keyboard navigation, accessible names and usable layout with long text and text scaling.
- **FR-011**: Document current coverage and how to add a language/text; keep uncovered pages tracked in #140.
- **FR-012**: Explain save failure in the active language, allow retry and do not report a failed save as success.

### Key Entities

- **UI preference**: App-local System/English/German choice, separate from solution and speech language.
- **Active language**: Supported language resolved once at startup plus its display culture.
- **Text resource**: Stable key, English source, German translation and translator context; complete template when parameterized.
- **Coverage row**: Surface, owned files, included/excluded text and slice/acceptance status.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All three choices persist and apply after restart on both hosts in the documented acceptance matrix.
- **SC-002**: 100% of Slice 1 text entries resolve in both languages; missing-translation cases return English and zero blank/key labels.
- **SC-003**: Key and placeholder checks detect all malformed fixture cases before publication.
- **SC-004**: A language-only edit changes zero solution values, undo entries or TTS settings.
- **SC-005**: All Slice 1 manual scenarios pass in both themes with keyboard navigation and 200% text scaling on desktop,
  and Android font scale 1.5 or higher; release evidence lists any unexecuted case explicitly.

## Assumptions

- English and German are the first two languages; additions require an explicit scope update.
- A full app restart is acceptable for Slice 1, as permitted by #140; no automated restart is introduced.
- User approved incremental delivery on both platforms. #140 remains open until its further coverage slices and acceptance finish.
- RF-24/#219 is not a hard prerequisite; do not refactor its Journey/Workflow/Counter implementations.
- TTS #136/PR #278 owns speech providers and pending speech Settings work; this slice adds a separate UI-language area.
