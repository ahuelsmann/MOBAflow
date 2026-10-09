# Feature Specification: Selectable text-to-speech providers

**Feature Directory**: `009-tts-providers`

**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/136

**Created**: 2026-10-08

**Status**: Draft (clarified 2026-10-09; awaiting plan approval)

**Spec Kit**: Required

**Input**: User description (issue #136, translated): "MOBAflow should offer different text-to-speech APIs, so
announcements can be produced with different providers without changing the calling application logic for each
provider. A base project contains provider-neutral interfaces and shared request and result types; every provider
has its own class library referencing that base project." Clarification (maintainer, 2026-10-09): "The special
part of the request is the modularisation: we need a common interface so that MOBAflow can use any text-to-speech
connection. Calls can be cached to save money."

## Governance and Traceability *(mandatory)*

**GitHub Issue**: #136

**Related Work**: #141 (modularisation decision; this feature is its named example case), RF-29 in #47 (its speech
part is taken over by this feature, see Q5), #140 (UI language stays independent of announcement language).
Historical starting point for the Azure provider: `Sound/CognitiveSpeechEngine.cs`, removed in commit `11d1c821`
(2026-06-14) when MOBAflow switched to Piper.

**Affected Platforms**: MOBAflow (Windows) and shared libraries (`Sound`, `Common`, `Backend`). Text-to-speech is
a MOBAflow-on-Windows feature only; MOBAsmart will never offer speech output (maintainer decision, 2026-10-09).

**Out of Scope**: Announcement text generation (`AnnouncementService` templates), sound-effect playback, UI
localisation (#140), speech output on Android or in MOBAsmart, runtime loading of provider assemblies (plugins),
paid live calls in automated tests.

**Sensitive Data**: The Azure AI Speech key and region. They are stored in the Windows credential store, never in
versioned files, `appsettings.json`, `solution.json`, logs, diagnostics or cache file names.

**Data and API Effects**: `AppSettings.Speech` gains a provider id, a fallback provider id and provider-specific
option sections; the current Piper and System Speech fields move into their provider sections. A new audio cache
lives in the MOBAflow application data folder. No migration: a settings file with the old fields loads with the
default provider. `solution.json` is unchanged.

## Current State (main at `d45e14c5`)

- `Sound/ISpeakerEngine.cs` is already a provider-neutral contract (`Name`, `AnnouncementAsync(message, voice,
  cancellationToken)`).
- `Sound/SpeakerEngineFactory.cs` selects one of two built-in registrations (`PiperSpeechEngine`,
  `SystemSpeechEngine`) by `AppSettings.Speech.SpeakerEngineName`; unknown names fall back silently.
- Both engines live in `Sound`, which also targets Android. `Sound.csproj` therefore references
  `System.Speech` and `System.Windows.Extensions` for every platform.
- MOBAflow creates one `ISpeakerEngine` singleton at startup (`MobaWinUiServiceCollectionExtensions`), so a
  changed selection takes effect only after a restart. MOBAsmart registers `NullSpeakerEngine`.
- Piper writes a WAV file and plays it; System Speech speaks directly to the default audio device. Nothing is
  cached.

## Clarifications

### Session 2026-10-09

- Q: Which providers belong to the first delivery? → A: The two local providers (Windows System Speech and Piper)
  and Azure AI Speech, each in its own library behind one common interface. The core of the request is the
  modularisation: any further text-to-speech connection must be addable as another library.
- Q: Should generated announcements be cached? → A: Yes (maintainer's addition). One audio cache for all
  providers in the shared layer, stored in the application data folder with a size limit (oldest entries are
  removed first). Settings show the cache size and offer "Clear cache".
- Q: When does a changed provider selection take effect? → A: Immediately, for the next announcement and the
  Settings test button, without a restart.
- Q: What happens when the selected provider fails? → A: The workflow always continues. A cached announcement is
  played from the cache. Without a cache entry and with the provider unreachable, the local provider speaks
  immediately as a fallback (Piper if configured, otherwise Windows System Speech). The text is remembered and its
  audio is synthesised with the selected provider and cached once the provider is reachable again; the
  announcement itself is not replayed later.
- Q: Does MOBAsmart get speech output? → A: No, never. Speech output is a MOBAflow-on-Windows feature; no
  follow-up issue for Android.
- Q: Does this feature take over RF-29's speech part? → A: Yes. Windows speech moves into a Windows provider
  library, and `Sound` no longer references Windows speech packages; RF-29 keeps sound playback and the
  file-service split.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Choose the speech provider (Priority: P1)

The operator opens Settings, sees the available speech providers with their voices, selects one, tests it with the
test message and hears the next announcement from that provider, without restarting MOBAflow.

**Why this priority**: It is the visible outcome of the issue.

**Independent Test**: With two fake providers registered, selecting a provider persists the id, and the next
announcement reaches only that provider without a restart.

**Acceptance Scenarios**:

1. **Given** Piper and Azure are configured, **When** the operator selects Azure and presses Test, **Then** the
   test message is synthesised by Azure.
2. **Given** a saved provider selection, **When** MOBAflow restarts, **Then** the same provider and its valid
   options are selected.

---

### User Story 2 - Add a provider without touching existing providers (Priority: P1)

A developer adds a new provider as one class library plus one registration line in the host, without editing the
contract project or other provider libraries. Caching and fallback work for the new provider automatically.

**Why this priority**: It is the structural goal of #136 and the example case for #141.

**Independent Test**: A test-only provider library registered in a test host appears in the provider list,
receives announcements and has its audio cached.

**Acceptance Scenarios**:

1. **Given** a new provider library, **When** it is registered, **Then** the settings list shows it with its
   reported capabilities (voices, languages, rate and volume support).

---

### User Story 3 - Save cost with the audio cache (Priority: P2)

A repeated announcement (same text, provider, voice and options) is played from the cache instead of calling the
provider again.

**Why this priority**: Cloud providers charge per character; recurring announcements are frequent.

**Independent Test**: Two identical announcements cause exactly one provider call; changing the voice causes a new
call.

**Acceptance Scenarios**:

1. **Given** an announcement was spoken once, **When** it is requested again with the same settings, **Then** the
   cached audio is played and the provider is not called.
2. **Given** the cache exceeds its size limit, **When** a new entry is added, **Then** the oldest entries are
   removed first.
3. **Given** the operator presses "Clear cache", **When** the next announcement runs, **Then** it is synthesised
   again.

---

### User Story 4 - Keep announcing when the cloud is unreachable (Priority: P2)

When Azure is selected but unreachable, cached announcements still play, new ones are spoken by the local fallback,
and their Azure audio is filled into the cache later.

**Why this priority**: A network outage must not silence the layout or stop automation.

**Independent Test**: A fake cloud provider that fails yields a fallback call, a diagnostic entry, a pending
synthesis entry and a completed workflow step; when the fake recovers, the pending entry is synthesised and cached
without playback.

**Acceptance Scenarios**:

1. **Given** Azure is unreachable and the text is not cached, **When** an announcement runs, **Then** the local
   fallback speaks it immediately and the diagnostics show the reason.
2. **Given** pending texts exist, **When** Azure is reachable again, **Then** their audio is synthesised and
   cached, and nothing is played.
3. **Given** an announcement is running, **When** the workflow is cancelled, **Then** synthesis and playback stop.

### Edge Cases

- A stored provider id that is no longer registered: the default provider is selected and the diagnostics say so.
- A voice that the newly selected provider does not offer: the provider default voice is used and shown.
- Rate or volume unsupported by a provider: the option is disabled for that provider, not silently ignored.
- Neither cache entry, selected provider nor local fallback available: the announcement is skipped with a
  diagnostic entry; the workflow continues.
- A corrupt or missing cache file: the entry is discarded and synthesised again.
- Two announcements overlap: the current ordering behavior is kept for every provider (characterised by a test
  before the move).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A contract project MUST contain the provider-neutral interfaces, request, capability and result
  types, and MUST NOT reference provider SDKs or UI frameworks.
- **FR-002**: Every provider (System Speech, Piper, Azure AI Speech) MUST live in its own class library that
  references the contract project only (plus its own SDK and shared infrastructure such as logging).
- **FR-003**: The host MUST register providers through dependency injection; selection MUST use a stable provider
  id stored in the app settings and MUST take effect for the next announcement without a restart.
- **FR-004**: Providers MUST report their capabilities (voices, languages, rate, volume) so the settings page can
  show only supported options.
- **FR-005**: Providers MUST return synthesised audio to the shared layer, which plays it; this makes caching and
  fallback provider-independent.
- **FR-006**: The shared layer MUST cache audio keyed by provider id, voice, language, rate, volume and text,
  MUST enforce a size limit by removing the oldest entries first, and MUST expose the cache size and a clear
  command.
- **FR-007**: On a cache miss with the selected provider unavailable, the shared layer MUST speak through the
  local fallback provider, record the reason in the speech diagnostics, and synthesise and cache the text with the
  selected provider once it is available again, without playing it.
- **FR-008**: Synthesis MUST be asynchronous and cancellable; provider errors MUST be returned as structured
  failures and MUST NOT stop the calling workflow.
- **FR-009**: Credentials MUST stay in the Windows credential store and MUST NOT appear in logs, diagnostics or
  cache file names.
- **FR-010**: Existing announcements MUST keep working through the new contract with the current default voice,
  rate and volume.
- **FR-011**: Automated tests MUST cover selection, option forwarding, capability filtering, caching, fallback,
  deferred synthesis, cancellation and failure handling without live paid calls.

### Key Entities

- **Speech provider**: stable id, display name, capabilities, availability state.
- **Speech request**: text, voice, language, rate, volume.
- **Speech result**: audio on success, or a failure with a reason that is safe to display.
- **Audio cache entry**: key derived from provider id, voice, language, rate, volume and text; audio file; last
  use time.
- **Pending synthesis**: text and request settings waiting for the selected provider to become available.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Three providers (System Speech, Piper, Azure) are selectable in Settings and use the same contract.
- **SC-002**: Adding a provider changes no file in the contract project or in other provider libraries.
- **SC-003**: A repeated identical announcement causes zero additional provider calls.
- **SC-004**: The Android build contains no Windows speech packages.
- **SC-005**: A failing provider never fails or blocks a workflow in automated tests.

## Assumptions

- Text generation (`AnnouncementService`) stays unchanged and only calls the contract.
- The provider library naming follows the existing `TrackLibrary.Base` / `TrackLibrary.PikoA` pattern.
- The default cache size limit is 500 MB; the plan may adjust it with a measured reason.
- Pending syntheses are kept for the running session only; a restart discards them.
- Implementation starts after RF-23 slice 3 is merged, because both change MOBAflow DI registration and the
  Settings page.
