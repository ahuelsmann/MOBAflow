# Tasks: Selectable text-to-speech providers

**GitHub Issue**: #136

**Spec Kit**: Required

Input: [spec.md](spec.md), [plan.md](plan.md).

Implementation starts after RF-23 slice 3 (#191) is merged. Each slice is one draft PR from its own worktree.

## Slice 1 - Characterise current behavior

- [ ] T001 Add characterization tests for announcements through the current engines: engine selection, voice, rate and volume forwarding, overlapping announcements, cancellation and failure handling
- [ ] T002 Run the portable and Windows test suites for validation; secrets scan and line endings
- [ ] T003 Open the slice PR as draft; Sonar quality gate (SonarCloud) green with zero open issues before review

## Slice 2 - Contract and shared output

- [ ] T004 Create `Speech.Base` (net10.0) with `ISpeechProvider`, `SpeechRequest`, `SpeechCapabilities`, `SpeechResult`, `ISpeechOutput` and `NullSpeechOutput`; add it to `Moba.slnx`
- [ ] T005 Add `SpeechOutput` (per-call provider resolution, playback through `ISoundPlayer`, structured failures, default provider for unknown ids with a diagnostic)
- [ ] T006 Switch `AnnouncementService`, `ActionExecutionContext` and the Backend DI to `ISpeechOutput`; MOBAsmart registers `NullSpeechOutput`
- [ ] T007 Tests for resolution, immediate switch, unknown ids, failures and cancellation with fake providers; run suites and the Android build for validation
- [ ] T008 Slice PR with the Sonar quality gate (SonarCloud) green and zero open issues

## Slice 3 - Local providers

- [ ] T009 Create `Speech.SystemSpeech` (net10.0-windows): move `SystemSpeechEngine`, synthesise to a WAV stream, report voices and rate/volume support
- [ ] T010 Create `Speech.Piper` (net10.0-windows): move the Piper engine, `PiperPronunciationNormalizer` and `PcmWavePostProcessor`; return the WAV instead of playing it
- [ ] T011 Register both providers in MOBAflow only; remove `System.Speech` and `System.Windows.Extensions` from `Sound`
- [ ] T012 Remove `ISpeakerEngine`, `SpeakerEngineFactory`, `SpeechSpeakerEngineSelection`, `SpeakerEngineConfiguration` and the old speech settings fields with their tests and docs
- [ ] T013 Move and adapt the provider tests; run the Windows suite, MOBAflow FastDebug build and Android build for validation
- [ ] T014 Slice PR with the Sonar quality gate (SonarCloud) green and zero open issues

## Slice 4 - Audio cache

- [ ] T015 Add `AudioCache`: hashed key over provider id, voice, language, rate, volume and text; WAV files plus index; least-recently-used eviction at the size limit (default 500 MB); corrupt entries dropped
- [ ] T016 Use the cache in `SpeechOutput` (hit plays without a provider call, miss stores the result); expose size and clear
- [ ] T017 Tests for hit, miss, key changes, eviction, corrupt files and clear; run suites for validation
- [ ] T018 Slice PR with the Sonar quality gate (SonarCloud) green and zero open issues

## Slice 5 - Azure provider and fallback

- [ ] T019 Create `Speech.Azure` (net10.0-windows): REST synthesis with RIFF PCM output, voice list, region and key from the Windows credential store; use `11d1c821` as reference
- [ ] T020 Add the fallback provider setting and the fallback path in `SpeechOutput` (Piper if configured, otherwise System Speech) with a diagnostic
- [ ] T021 Add `PendingSynthesisQueue`: merged by cache key, background retry with backoff, synthesis into the cache without playback, stop on shutdown
- [ ] T022 Tests with a fake `HttpMessageHandler` and fake providers: unreachable service, fallback, deferred synthesis, no replay, cancellation, and that the key never reaches logs, diagnostics or cache file names; run suites for validation
- [ ] T023 Slice PR with the Sonar quality gate (SonarCloud) green and zero open issues

## Slice 6 - Settings and diagnostics

- [ ] T024 Settings ViewModel: provider list with capabilities, only supported options enabled, fallback provider, cache size display and "Clear cache" command, Azure key entry saved to the credential store
- [ ] T025 Settings page in MOBAflow (English text, ThemeResources); speech health check and diagnostics show fallback and skipped announcements
- [ ] T026 ViewModel tests; MOBAflow FastDebug build; Settings page checked in Light and Dark by the maintainer (launch approval) for validation
- [ ] T027 Slice PR with the Sonar quality gate (SonarCloud) green and zero open issues

## Slice 7 - Documentation and closure

- [ ] T028 Update `docs/wiki/PIPER-TTS-SETUP.md`, the user guide speech section and `docs/ARCHITECTURE.md`; add a short "add a provider" guide
- [ ] T029 Maintainer acceptance on the PC: test message per provider and one fallback run with the network off; comment on #47 that RF-29 lost its speech part
- [ ] T030 Final PR with the Sonar quality gate (SonarCloud) green and zero open issues; close #136
