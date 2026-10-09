# Tasks: Selectable text-to-speech providers

**GitHub Issue**: #136

**Spec Kit**: Required

Input: [spec.md](spec.md), [plan.md](plan.md).

Implementation starts after RF-23 slice 3 (#191) is merged. Each slice is one draft PR from its own worktree.

## Slice 1 - Characterise current behavior

- [ ] T001 Add characterization tests for announcements through the current engines: engine selection, voice, rate and volume forwarding, overlapping announcements, cancellation and failure handling
- [ ] T002 Run the portable and Windows test suites for validation; secrets scan and line endings
- [ ] T003 Open the slice PR as draft; Sonar quality gate (SonarCloud) green with zero open issues before review

## Slice 2 - Contract, shared output and local providers

- [ ] T004 Create `Speech.Base` (net10.0) with `ISpeechProvider`, `SpeechRequest`, `SpeechCapabilities`, `SpeechResult`, `ISpeechOutput` and `NullSpeechOutput`; add it to `Moba.slnx`
- [ ] T005 Add `SpeechOutput` (per-call provider resolution, default provider `system-speech` for unknown or missing ids with a diagnostic, structured failures)
- [ ] T006 Add a cancellable WAV player for the shared layer that stops audible playback on cancellation, with a regression test against the production player behavior
- [ ] T007 Create `Speech.SystemSpeech` (net10.0-windows): move `SystemSpeechEngine`, synthesise to a WAV stream, report voices and rate/volume support
- [ ] T008 Create `Speech.Piper` (net10.0-windows): move the Piper engine, `PiperPronunciationNormalizer` and `PcmWavePostProcessor`; return the WAV instead of playing it
- [ ] T009 Switch `AnnouncementService`, `ActionExecutionContext` and the Backend DI to `ISpeechOutput`; register both providers in MOBAflow; MOBAsmart registers `NullSpeechOutput`
- [ ] T010 Remove `System.Speech` from `Sound` (keep `System.Windows.Extensions` for `WindowsSoundPlayer`); remove `ISpeakerEngine`, `SpeakerEngineFactory`, `SpeechSpeakerEngineSelection`, `SpeakerEngineConfiguration` and the old speech settings fields with their tests and docs; set `ProviderId` to `piper` in `MOBAflow/appsettings.json`
- [ ] T011 Tests for resolution, immediate switch, unknown ids, a fresh configuration, a settings file with only removed fields, failures and cancellation; run the Windows suite, MOBAflow FastDebug build and Android build for validation
- [ ] T012 Slice PR with the Sonar quality gate (SonarCloud) green and zero open issues

## Slice 3 - Audio cache

- [ ] T013 Add `AudioCache`: hashed key over provider id, provider options fingerprint, voice, language, rate, volume and text; WAV files plus index; least-recently-used eviction at the size limit (default 500 MB); corrupt entries dropped
- [ ] T014 Use the cache in `SpeechOutput` (hit plays without a provider call, miss stores the result, concurrent identical misses share one synthesis); expose size and clear
- [ ] T015 Tests for hit, miss, key and provider-option changes, least-recently-used eviction, concurrent identical requests, corrupt files and clear; run suites for validation
- [ ] T016 Slice PR with the Sonar quality gate (SonarCloud) green and zero open issues

## Slice 4 - Azure provider and fallback

- [ ] T017 Create `Speech.Azure` (net10.0-windows): REST synthesis with RIFF PCM output, voice list, region from the app settings and key from the Windows credential store; use `11d1c821` as reference
- [ ] T018 Add the fallback provider setting and the fallback path in `SpeechOutput`: first available local provider other than the failed one (configured fallback, Piper if configured, then System Speech), with a diagnostic
- [ ] T019 Add `PendingSynthesisQueue`: merged by cache key, background retry with backoff, synthesis into the cache without playback, stop on shutdown
- [ ] T020 Tests with a fake `HttpMessageHandler` and fake providers: unreachable cloud service, failing local provider (Piper selected and failing falls back to System Speech), deferred synthesis, no replay, cancellation, and that the key never reaches logs, diagnostics or cache file names; run suites for validation
- [ ] T021 Slice PR with the Sonar quality gate (SonarCloud) green and zero open issues

## Slice 5 - Settings and diagnostics

- [ ] T022 Settings ViewModel: provider list with capabilities, only supported options enabled, fallback provider, cache size display and "Clear cache" command, Azure key entry saved to the credential store
- [ ] T023 Settings page in MOBAflow (English text, ThemeResources); speech health check and diagnostics show fallback and skipped announcements
- [ ] T024 ViewModel tests; MOBAflow FastDebug build; Settings page checked in Light and Dark by the maintainer (launch approval) for validation
- [ ] T025 Slice PR with the Sonar quality gate (SonarCloud) green and zero open issues

## Slice 6 - Documentation and closure

- [ ] T026 Update `docs/wiki/PIPER-TTS-SETUP.md`, the user guide speech section and `docs/ARCHITECTURE.md`; add a short "add a provider" guide
- [ ] T027 Maintainer acceptance on the PC: test message per provider and one fallback run with the network off; comment on #47 that RF-29 lost its speech part
- [ ] T028 Final PR with the Sonar quality gate (SonarCloud) green and zero open issues; close #136
