# Tasks: Selectable text-to-speech providers

**GitHub Issue**: #136

**Spec Kit**: Required

**Status**: Approved; slices 1 and 2 merged in [PR #244](https://github.com/ahuelsmann/MOBAflow/pull/244) and [PR #250](https://github.com/ahuelsmann/MOBAflow/pull/250), after prerequisite #207 merged. Slice 3 cache implementation in progress. T011 manual UI acceptance and slices 4-6 remain pending.

Input: [spec.md](spec.md), [plan.md](plan.md).

Implementation starts only after [PR #207](https://github.com/ahuelsmann/MOBAflow/pull/207) (RF-23 runtime per project, tracked by #191) is merged. Verify the current GitHub merge state before T001. Spec, plan and task corrections are independent. Each slice is one draft PR from its own worktree.

## Slice 1 - Characterise current behavior

- [X] T001 Add characterization tests for announcements through the current engines: engine selection, voice, rate and volume forwarding, overlapping announcements, cancellation and failure handling; distinguish per-call factory resolution in `AnnouncementService` from the singleton used by the Settings test callback
- [X] T002 Run the portable and Windows test suites for validation; secrets scan and line endings
- [X] T003 Open the slice PR as draft; Sonar quality gate (SonarCloud) green with zero open issues before review

Validation on merged `main` at `31d2d3a6`: portable Release suite 2,224 passed, 1 skipped; Windows Release suite 2,283 passed, 0 skipped. Both test graphs compile and their analyzer baselines match unchanged. No production speech behavior changes. CI/Sonar evidence is tracked in [PR #244](https://github.com/ahuelsmann/MOBAflow/pull/244); its current commit must pass the Sonar gate with zero OPEN/CONFIRMED findings before review.

### Characterization baseline

Slice 1 starts from merged `main` at `39312ca9`. The tests retain the current behavior rather than implement the
new provider contract:

- `AnnouncementService` resolves the selected engine and current voice/rate/volume options for each call. It passes
  a null voice override and the caller's cancellation token to the engine.
- The production Settings test callback resolves the singleton `ISpeakerEngine`: after its first resolution it
  retains the same engine even if selection changes. The new shared output must replace this path in slice 2.
- The service and Piper do not queue overlapping calls; later calls can finish first. Piper uses independent WAV
  files and removes them after playback, synthesis cancellation or playback cancellation.
- Piper maps rate to length scale, selects its voice from the configured model, ignores the voice override and
  currently leaves the WAV unchanged when the volume option changes. These are current limitations, not the
  required option support of the new providers.
- Empty templates do not resolve an engine; caller cancellation propagates; ordinary factory/engine failures are
  logged and suppressed by default, while the explicit non-suppression path propagates the failure.

## Slice 2 - Contract, shared output and local providers

- [X] T004 Create `Speech.Base` (net10.0) with `ISpeechProvider`, `SpeechRequest`, `SpeechCapabilities`, `SpeechResult`, `ISpeechOutput` and `NullSpeechOutput`; add it to `Moba.slnx`
- [X] T005 Add `SpeechOutput` (per-call provider resolution, default provider `system-speech` for unknown or missing ids with a diagnostic, structured failures)
- [X] T006 Add a cancellable WAV player for the shared layer that stops audible playback on cancellation, with a regression test against the production player behavior
- [X] T007 Create `Speech.SystemSpeech` (net10.0-windows): move `SystemSpeechEngine`, synthesise to a WAV stream, report voices and rate/volume support
- [X] T008 Create `Speech.Piper` (net10.0-windows): move the Piper engine, `PiperPronunciationNormalizer` and `PcmWavePostProcessor`; return the WAV instead of playing it
- [X] T009 Switch `AnnouncementService`, `ActionExecutionContext`, the Settings test callback and the Backend DI to `ISpeechOutput`; register both providers in MOBAflow; MOBAsmart registers `NullSpeechOutput`; convert speech health/status callers that depend on removed contracts in the same cutover
- [X] T010 Replace existing local-provider Settings ViewModel properties and page bindings with `ProviderId` and the new option sections, preserving saving, local provider selection and test behavior; remove `System.Speech` from `Sound` (keep `System.Windows.Extensions` for `WindowsSoundPlayer`); remove `ISpeakerEngine`, `SpeakerEngineFactory`, `SpeechSpeakerEngineSelection`, `SpeakerEngineConfiguration` and the old speech settings fields with all their callers, bindings, tests and docs in this same slice; set `ProviderId` to `piper` in `MOBAflow/appsettings.json`
- [ ] T011 Tests for resolution, immediate switch through both `AnnouncementService` and the Settings test command, local-provider settings persistence, unknown ids, a fresh configuration, a settings file with only removed fields, failures and cancellation; run the Windows suite, MOBAflow FastDebug build and Android build for validation; have the maintainer check changed Settings bindings in Light and Dark with launch approval
- [X] T012 Slice PR with the Sonar quality gate (SonarCloud) green and zero open issues

Slice 2 automated validation: portable Release suite 2,330 passed, 1 skipped; Windows Release suite 2,422 passed,
0 skipped, including controlled process timeout/cancellation tests and in-memory Windows speech synthesis.
WinUI FastDebug and Android FastDebug/Release compile. The Android restored graph contains `Speech.Base`, with no local Windows speech provider
or `System.Speech`. All three analyzer snapshots remove obsolete diagnostics and reflect relocated NUnit/SAPI
false positives already suppressed by their analyzers or a narrowly justified source suppression; no combined
rule quota increases and the baseline guards remain unchanged. T011 stays open for the maintainer's Light/Dark
Settings check with explicit launch approval. T012 is verified by SonarCloud analysis of `7dbda9c5` on 2026-10-12: quality gate OK and zero OPEN/CONFIRMED findings. Cache, Azure, fallback and deferred synthesis remain for later slices.

CI/Sonar follow-up (2026-10-12): split WAV format parsing and Windows voice selection into small helpers,
and make the status getter's generated instance-property access explicit. Add native sound-adapter regression
tests for missing files and pre-cancelled calls without audible playback. Local full Windows suite: 2,466 passed;
final focused regressions: 125 Windows and 84 portable passed. Sound line coverage is 86.95% against the existing
64.50% threshold. Portable/Windows analyzer baselines match without changes. The synchronized branch also
passes the 125 Windows regressions with NUnit3TestAdapter 6.3.1. CI completion and the T011 manual Light/Dark
Settings check remain pending; the PR stays draft.

Review follow-up (2026-10-13): all five findings are confirmed by failing regressions and addressed within
T004-T012. Add a bounded Piper startup probe, reset provider health to pending on selection, discard stale
check results and publish repeated completed results, render Unavailable as a warning/degraded operating state,
use the selected provider's default for a stale voice, and guard process-wide speech playback ownership so older
completion/cancellation cannot stop a newer announcement. Tests use controlled processes, SAPI WAV synthesis
to memory, virtual time and fake playback sessions; no audible playback, desktop application or hardware run.
Current-head CI/Sonar evidence is recorded in PR #250. T011 manual Light/Dark acceptance remains open.
## Slice 3 - Audio cache

- [X] T013 Add `AudioCache`: hashed key over provider id, provider options fingerprint, voice, language, rate, volume and text; WAV files plus index; least-recently-used eviction at the size limit (default 500 MB); corrupt entries dropped
- [X] T014 Use the cache in `SpeechOutput` (hit plays without a provider call, miss stores the result, concurrent identical misses share one synthesis); keep caller cancellation separate from the shared synthesis token, cancel/remove the shared work when its last waiter cancels, and cancel shared work on shutdown; cancelled callers neither play audio nor initiate fallback or deferred synthesis; expose size and clear service APIs (UI in T022/T023)
- [ ] T015 Tests for hit, miss, key and provider-option changes, least-recently-used eviction, concurrent identical requests, corrupt files and clear; cover one waiter cancelling while another completes with one provider call, all waiters cancelling the provider, a new request after cancellation, shutdown during shared synthesis and no playback/fallback/deferred synthesis for cancelled callers; run suites for validation
- [ ] T016 Slice PR with the Sonar quality gate (SonarCloud) green and zero open issues

## Slice 4 - Azure provider and fallback

- [ ] T017 Create `Speech.Azure` (net10.0-windows): REST synthesis with RIFF PCM output, voice list, region from the app settings and key from the Windows credential store; use `11d1c821` as reference
- [ ] T018 Add the fallback provider setting and the fallback path in `SpeechOutput`: first available local provider other than the failed one (configured fallback, Piper if configured, then System Speech), with a diagnostic
- [ ] T019 Add `PendingSynthesisQueue`: merged by cache key, background retry with backoff, synthesis into the cache without playback, stop on shutdown
- [ ] T020 Tests with a fake `HttpMessageHandler` and fake providers: unreachable cloud service, failing local provider (Piper selected and failing falls back to System Speech), deferred synthesis, no replay, cancellation with fallback and the pending queue enabled (including one cancelled caller sharing synthesis with another), and that the key never reaches logs, diagnostics or cache file names; run suites for validation
- [ ] T021 Slice PR with the Sonar quality gate (SonarCloud) green and zero open issues

## Slice 5 - Settings and diagnostics

- [ ] T022 Extend the local-provider Settings ViewModel controls delivered in T010: provider list with capabilities, only supported options enabled, Azure and fallback configuration, cache size display and "Clear cache" command, Azure key entry saved to the credential store
- [ ] T023 Extend the Settings page bindings delivered in T010 with the T022 controls (English text, ThemeResources); extend the converted speech health check and diagnostics to show fallback and skipped announcements
- [ ] T024 ViewModel tests; MOBAflow FastDebug build; Settings page checked in Light and Dark by the maintainer (launch approval) for validation
- [ ] T025 Slice PR with the Sonar quality gate (SonarCloud) green and zero open issues

## Slice 6 - Documentation and closure

- [ ] T026 Update `docs/wiki/PIPER-TTS-SETUP.md`, the user guide speech section and `docs/ARCHITECTURE.md`; add a short "add a provider" guide
- [ ] T027 Maintainer acceptance on the PC: test message per provider and one fallback run with the network off; comment on #47 that RF-29 lost its speech part
- [ ] T028 Final PR with the Sonar quality gate (SonarCloud) green and zero open issues; close #136
