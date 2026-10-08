# Feature Specification: Selectable text-to-speech providers

**Feature Directory**: `009-tts-providers`

**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/136

**Created**: 2026-10-08

**Status**: Draft (clarification pending)

**Spec Kit**: Required

**Input**: User description (issue #136, translated): "MOBAflow should offer different text-to-speech APIs, so
announcements can be produced with different providers without changing the calling application logic for each
provider. A base project contains provider-neutral interfaces and shared request and result types; every provider
has its own class library referencing that base project."

## Governance and Traceability *(mandatory)*

**GitHub Issue**: #136

**Related Work**: #141 (modularisation decision; this feature is its named example case), RF-29 in #47 (move
Windows speech out of the `Sound` project used by Android), #140 (UI language stays independent of announcement
language).

**Affected Platforms**: MOBAflow (Windows) and shared libraries (`Sound`, `Common`, `Backend`). MOBAsmart keeps
its current silent speech engine unless clarification Q4 decides otherwise.

**Out of Scope**: Announcement text generation (`AnnouncementService` templates), sound-effect playback, UI
localisation (#140), a plugin mechanism that loads provider assemblies at runtime, paid live calls in automated
tests.

**Sensitive Data**: Cloud provider keys or tokens, if a cloud provider is selected in Q1. They never enter
versioned files, `solution.json`, logs or diagnostics.

**Data and API Effects**: `AppSettings.Speech` gains a provider id and provider-specific option sections; the
current Piper and System Speech fields move into their provider sections. No migration: a settings file with the
old fields loads with the default provider. `solution.json` is unchanged.

## Current State (main at `d45e14c5`)

- `Sound/ISpeakerEngine.cs` is already a provider-neutral contract (`Name`, `AnnouncementAsync(message, voice,
  cancellationToken)`).
- `Sound/SpeakerEngineFactory.cs` selects one of two built-in registrations (`PiperSpeechEngine`,
  `SystemSpeechEngine`) by `AppSettings.Speech.SpeakerEngineName`; unknown names fall back silently.
- Both engines live in `Sound`, which also targets Android. `Sound.csproj` therefore references
  `System.Speech` and `System.Windows.Extensions` for every platform.
- MOBAflow creates one `ISpeakerEngine` singleton at startup (`MobaWinUiServiceCollectionExtensions`), so a
  changed selection takes effect only after a restart. MOBAsmart registers `NullSpeakerEngine`.
- Piper writes a WAV file and plays it; System Speech speaks directly to the default audio device.

## Clarifications

### Session 2026-10-08 (open, answers from the maintainer required)

Each question lists the recommended answer. The plan is drafted with the recommended answers and is revised when
an answer differs.

- Q1: Which providers belong to the first delivery? → Recommended: the two existing local providers (Windows
  System Speech and Piper) move into their own provider libraries; this already satisfies "at least two
  providers on one contract" without cost or credentials. A cloud provider (for example Azure AI Speech) follows
  as a separate slice only if you name one, together with where its key is stored (recommended: the Windows
  credential store, never `appsettings.json`).
- Q2: When does a changed provider selection take effect? → Recommended: for the next announcement, without a
  restart, so the existing Settings test button always tests the selected provider.
- Q3: What happens when the selected provider fails (missing configuration, API error, timeout)? → Recommended:
  no silent switch to another provider; the announcement is skipped, the error is shown in the speech
  diagnostics, and the workflow continues.
- Q4: Does MOBAsmart get speech output in this feature? → Recommended: no; MOBAsmart keeps `NullSpeakerEngine`.
  Android speech is a separate issue.
- Q5: Does this feature also take over RF-29 step 1 for speech (Windows-only speech packages leave the shared
  `Sound` project)? → Recommended: yes, because the System Speech provider library is Windows-only by nature;
  RF-29 then keeps only sound playback and the file-service split.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Choose the speech provider (Priority: P1)

The operator opens Settings, sees the available speech providers with their voices, selects one, tests it with the
test message and hears the next announcement from that provider.

**Why this priority**: It is the visible outcome of the issue and works with the existing local providers.

**Independent Test**: With two fake providers registered, selecting a provider persists the id, and the next
announcement reaches only that provider.

**Acceptance Scenarios**:

1. **Given** System Speech and Piper are installed, **When** the operator selects Piper and presses Test,
   **Then** the test message is synthesised by Piper.
2. **Given** a saved provider selection, **When** MOBAflow restarts, **Then** the same provider and its valid
   options are selected.

---

### User Story 2 - Add a provider without touching existing providers (Priority: P2)

A developer adds a new provider as one class library plus one registration line in the host, without editing the
contract project or other provider libraries.

**Why this priority**: It is the structural goal of #136 and the example case for #141.

**Independent Test**: A test-only provider library registered in a test host appears in the provider list and
receives announcements.

**Acceptance Scenarios**:

1. **Given** a new provider library, **When** it is registered, **Then** the settings list shows it with its
   reported capabilities (voices, languages, rate and volume support).

---

### User Story 3 - Understandable failures (Priority: P3)

When the selected provider is not configured or fails, the operator sees why, and the railroad automation keeps
running.

**Why this priority**: Announcements are optional for safe operation; a speech error must not stop a workflow.

**Independent Test**: A fake provider that throws or times out yields a diagnostic entry and a completed workflow
step.

**Acceptance Scenarios**:

1. **Given** Piper is selected but its model path is empty, **When** an announcement runs, **Then** it is skipped
   and the diagnostics name the missing setting.
2. **Given** an announcement is running, **When** the workflow is cancelled, **Then** synthesis and playback stop.

### Edge Cases

- A stored provider id that is no longer registered: the default provider is selected and the diagnostics say so.
- A voice that the newly selected provider does not offer: the provider default voice is used and shown.
- Rate or volume unsupported by a provider: the option is disabled for that provider, not silently ignored.
- Two announcements overlap: the current ordering behavior is kept for every provider (characterised by a test
  before the move).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A contract project MUST contain the provider-neutral interfaces, request, capability and result
  types, and MUST NOT reference provider SDKs or UI frameworks.
- **FR-002**: Every provider MUST live in its own class library that references the contract project only (plus
  its own SDK and shared infrastructure such as logging).
- **FR-003**: The host MUST register providers through dependency injection; selection MUST use a stable provider
  id stored in the app settings.
- **FR-004**: Providers MUST report their capabilities (voices, languages, rate, volume) so the settings page can
  show only supported options.
- **FR-005**: Synthesis MUST be asynchronous and cancellable; provider errors MUST be returned as structured
  failures and MUST NOT stop the calling workflow.
- **FR-006**: Credentials MUST stay outside versioned files and MUST NOT appear in logs or diagnostics.
- **FR-007**: Existing announcements MUST keep working through the new contract with the current default voice,
  rate and volume.
- **FR-008**: Automated tests MUST cover selection, option forwarding, capability filtering, cancellation and
  failure handling without live paid calls.

### Key Entities

- **Speech provider**: stable id, display name, capabilities, availability state.
- **Speech request**: text, voice, language, rate, volume.
- **Speech result**: success with audio or played state, or a failure with a reason that is safe to display.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: At least two providers are selectable in Settings and use the same contract.
- **SC-002**: Adding a provider changes no file in the contract project or in other provider libraries.
- **SC-003**: The Android build contains no Windows speech packages (if Q5 is accepted).
- **SC-004**: A failing provider never fails or blocks a workflow in automated tests.

## Assumptions

- Text generation (`AnnouncementService`) stays unchanged and only calls the contract.
- The provider library naming follows the existing `TrackLibrary.Base` / `TrackLibrary.PikoA` pattern.
- Implementation starts after RF-23 slice 3 is merged, because both change MOBAflow DI registration and the
  Settings page.
