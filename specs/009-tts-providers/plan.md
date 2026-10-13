# Implementation Plan: Selectable text-to-speech providers

**Branch**: `codex/issue-136-tts-providers-spec` | **Date**: 2026-10-09 | **Spec**: [spec.md](spec.md)

**GitHub Issue**: #136

**Spec Kit**: Required

**Status**: Approved; slices 1 and 2 merged in [PR #244](https://github.com/ahuelsmann/MOBAflow/pull/244) and [PR #250](https://github.com/ahuelsmann/MOBAflow/pull/250), after prerequisite #207 merged. Slice 3 implemented in draft [PR #278](https://github.com/ahuelsmann/MOBAflow/pull/278); current CI/Sonar evidence is tracked in that PR. T011 manual UI acceptance and slices 4-6 remain pending.

**Input**: Feature specification from `/specs/009-tts-providers/spec.md`

## Summary

Split the existing speech engines into a provider-neutral contract project and one Windows class library per
provider (System Speech, Piper, Azure AI Speech). Providers only synthesise audio; a shared speech output service
resolves the selected provider per announcement, plays the audio, keeps an audio cache for all providers, falls
back to a local provider when the selected one is unreachable, and fills the cache later. MOBAsmart keeps a null
output and references no speech provider.

## Technical Context

**Language/Version**: .NET 10 / C#

**Primary Dependencies**: Existing `System.Speech` and the Piper executable (moved, not added); Azure AI Speech
through its REST API with `HttpClient` (no Speech SDK package, so no native binaries and a fake `HttpMessageHandler`
in tests). The old `Sound/CognitiveSpeechEngine.cs` (removed in `11d1c821`) is the reference for request shape and
voice names.

**Storage/Protocols**: `AppSettings.Speech` (app settings JSON): provider id, fallback provider id, cache size limit
and one options section per provider. Audio cache as WAV files plus a small index in the MOBAflow application data
folder. Azure key in the Windows credential store; Azure region and voice in the app settings.

**Testing**: NUnit and Moq; fake providers and a clock for cache eviction; owned temporary directories for atomic WAV/index persistence and corruption tests; cache service doubles for I/O failure and waiter cancellation; existing
`SpeakerEngineFactoryTests`, `PiperSpeechEngineTest`, `SystemSpeechEngineTest`, `AnnouncementServiceTests` and
`MobaBackendServiceCollectionExtensionsTests` are moved or replaced.

**Target Platforms**: Contract and shared speech output `net10.0` (Backend calls them on every platform); all
provider libraries `net10.0-windows` (local providers use the host target `net10.0-windows10.0.22621.0`),
referenced by MOBAflow only.

**Affected Layers**: `Sound` (speech parts move out), `Common/Configuration` (speech settings), `Backend`
(`AnnouncementService`, `ActionExecutionContext`, DI), `SharedUI` (settings and health ViewModels), `MOBAflow`
(DI, Settings page), `MOBAsmart` (DI only: null output), `Domain` (`SpeakerEngineConfiguration` removed or
replaced).

**Performance Goals**: A cache hit starts playback without a provider call. Provider resolution per call adds no
noticeable delay; provider instances are reused while their options are unchanged.

**Data and API Effects**: Speech settings get the new fields; the old engine name and Piper/System Speech fields are
removed and ignored on load (no migration). `solution.json` unchanged. New cache folder below the app data folder.

**Scale/Scope**: Three providers; default cache limit 500 MB; pending syntheses kept for the running session only.
Android speech and runtime plugin loading excluded.

## Constitution Check

- [x] Layer and platform boundaries are preserved: the contract has no SDK or UI types; every provider is a
  Windows-only library referenced by MOBAflow; MOBAsmart gets no speech provider reference.
- [x] EventBus UI-thread marshalling remains centralized in `UiThreadEventBusDecorator` (no EventBus change).
- [x] Async flows use `await` without sync-over-async; synthesis, playback and deferred synthesis are cancellable.
- [x] UI behavior uses ViewModel commands (provider selection, test, clear cache); Settings changes use existing
  ThemeResources.
- [x] All user-visible UI strings are English.
- [x] Data and API effects are explicit; old speech fields are removed without migration.
- [x] Regression tests cover selection, forwarding, capabilities, cache, fallback, deferred synthesis,
  cancellation and failures, all without live paid calls.
- [x] Validation follows the `AGENTS.md` matrix (shared logic plus WinUI compile, Android compile for the package
  removal).
- [x] The design retains `ISoundPlayer` for sound effects and uses `IWavePlayer` for cancellable speech WAV playback,
  with a Windows host adapter, the speech health check and the DI extension methods; `ISpeakerEngine` and
  its factory are replaced, not kept beside the new contract.
- [x] Spec and plan reference #136.
- [x] Artifacts stay below `specs/`.
- [x] Changed files are scanned for secrets before commit; the Azure key lives only in the Windows credential store
  and never reaches logs, diagnostics or cache file names.
- [x] Sonar runs only in GitHub CI.
- [x] PRs stay draft until SonarCloud is green with zero OPEN/CONFIRMED issues.

## Design

### Projects

```text
Speech.Base/             # net10.0: contracts (ISpeechProvider, SpeechRequest, SpeechCapabilities,
                         # SpeechResult, ISpeechOutput) and the shared layer (SpeechOutput, AudioCache,
                         # PendingSynthesisQueue, NullSpeechOutput)
Speech.SystemSpeech/     # net10.0-windows: Windows System Speech provider (synthesises to a WAV stream)
Speech.Piper/            # net10.0-windows: Piper provider, PiperPronunciationNormalizer, PcmWavePostProcessor
Speech.Azure/            # net10.0-windows: Azure AI Speech REST provider, credential store access
Sound/                   # keeps sound-effect playback (ISoundPlayer, SoundManager); no speech packages
```

Names follow the `TrackLibrary.Base` / `TrackLibrary.PikoA` pattern; final names are confirmed in review.

### Contract (sketch)

- `ISpeechProvider`: `Id`, `DisplayName`, `IsLocal`, `GetCapabilitiesAsync`, and
  `SynthesizeAsync(SpeechRequest, CancellationToken)` returning `SpeechResult`. Providers return audio; they never
  play it (FR-005). `PrepareSynthesis(request)` captures a non-secret options fingerprint and the synthesis operation
  before cache lookup, so later settings changes cannot relabel audio from the captured options.
- `SpeechResult`: WAV audio on success, or a failure kind (`Unavailable`, `InvalidRequest`, `Failed`) with a
  display-safe reason. Expected failures are results, not exceptions.
- `SpeechCapabilities`: voices with language, and whether rate and volume are supported.
- `ISpeechOutput`: `SpeakAsync(text, CancellationToken)` for callers; `AnnouncementService`,
  `ActionExecutionContext` and the Settings test command use it instead of `ISpeakerEngine`.

### Shared speech output

`SpeechOutput` runs each announcement in this order:

1. Build the request from the current settings and resolve the selected provider by id (FR-003, takes effect for
   the next call). An unknown or missing id selects the default provider `system-speech` and writes a diagnostic.
2. **Cache hit**: play the cached WAV through `IWavePlayer`; no provider call (FR-006, SC-003).
3. **Cache miss**: synthesise with the selected provider, store the audio in the cache, play it. Concurrent misses
   for the same cache key share one synthesis (per-key single flight), so overlapping identical announcements cause
   one provider call; different keys synthesise independently. Each caller observes its own cancellation token;
   the shared synthesis owns a separate token. Cancelling one caller ends its wait and prevents its playback,
   fallback and deferred synthesis, while other active callers continue. When the last waiting caller cancels,
   cancel the shared synthesis and remove its in-progress entry so a later request can start again. Application
   shutdown cancels all shared synthesis and playback.
4. **Selected provider unavailable**: write a diagnostic, speak through the first available local fallback provider
   other than the failed one (the configured fallback, Piper if configured, then System Speech), and add the request
   to the pending queue (FR-007). Fallback audio is not cached under the selected provider's key.
5. **Nothing available**: skip the announcement with a diagnostic; the workflow continues (FR-008, SC-005).

Overlapping announcements keep today's ordering, characterised in slice 1. Cancellation follows the caller and
shared-work rules above and stops audible playback: today's `WindowsSoundPlayer` checks the token only before `SoundPlayer.PlaySync()`, so the shared layer
needs a player that stops on cancellation (for example `SoundPlayer.Play()` plus `Stop()` on cancellation and a
completion wait from the WAV duration), covered by a regression test.

For slice 2, local provider availability includes a bounded Piper `--help` startup probe without synthesis.
Changing providers resets the shell health display to pending. Checks publish the current provider's completed
result even when its value repeats, and ignore results from a provider replaced during the check. A saved voice
not offered by the selected provider uses that provider's configured/default voice; Piper uses its selected model.
A newer speech playback session replaces the previous native waveform. Stop on cancellation or nominal
completion applies only to the current session, guarded across host-player instances; older callers cannot stop it.
### Audio cache

- Key: SHA-256 of provider id, a provider-supplied options fingerprint (for Piper: model, config, sentence silence,
  normalisation and pronunciation replacements; for Azure: region and output format), voice, language, rate, volume
  and normalised text; file name is the hash, so no text or credential appears in a file name (FR-009). Changing a
  synthesis-affecting provider option therefore misses the cache.
- WAV files plus an index with size and last-use time; at the size limit (default 500 MB) the entries not played for
  the longest time are removed first (least recently used), so frequently repeated announcements stay cached. A
  corrupt or missing file is dropped and synthesised again.
- `IAudioCache.GetSizeAsync` and `ClearAsync`, also exposed by `SpeechOutput`, back the later Settings size display and
  the "Clear cache" command. The host registers one cache below `%LOCALAPPDATA%/MOBAflow/SpeechCache`; the size limit
  is `Speech.CacheSizeLimitMegabytes` (500 MiB by default, zero disables storage). The limit covers retained WAV bytes.
- WAV/index replacement is atomic. A lock file serializes cache operations across MOBAflow processes; each operation
  reloads the index under that lock. Unindexed WAVs or temporary files left by an interrupted write are removed on the
  first operation of a process, after a failed index update, and whenever the size is read or the cache is cleared.
  A clear increments the generation stored in the index and, once deletion starts, completes regardless of
  cancellation: earlier in-progress synthesis, also in another process, can finish playback but cannot refill the
  cleared cache. A lookup returns the generation read under the same lock as its miss. Requests after clear use a new
  shared-flight generation. A lookup reads only the requested file; the size limit is enforced from the index before
  every hit, whenever audio is stored, and when the size is read.
  A cache I/O failure is diagnosed without raw filesystem details and does not silence an otherwise valid announcement.
- Synthesized audio is stored only after it has played successfully, with that playback as its last-use time, so the
  cache never exceeds its limit and unheard audio never displaces played audio. A completed shared synthesis stays
  joinable while any caller still plays or stores it, so an identical request during that playback reuses its audio
  instead of calling the provider again. Each successful playback of a cached entry updates its last-use time. Provider fingerprints include effective Piper executable/model/config
  paths, pronunciation options and sentence silence; System Speech includes its
  configured default voice. Availability is excluded, so a cached announcement remains usable when the provider
  cannot start. Replacing model contents at the same configured path requires clearing the cache. Request text uses
  Unicode canonical normalization in both synthesis and cache identity.
- Host service disposal cancels caller waits, shared synthesis and playback, then awaits their completion.
  `SpeechOutput` is only asynchronously disposable, so no caller can return before that wait.
- The WinUI host enables unsafe blocks for generated C#/WinRT interop of disposable cache/output services; no
  handwritten unsafe speech code or UI dependency is added to `Speech.Base`. Exact analyzer snapshots retire the
  resolved WinRT configuration diagnostics and record only the two source-suppressed NUnit reflection fixtures.

### Pending synthesis

- An in-memory queue of requests whose selected provider was unavailable; duplicates by cache key are merged.
- A background loop retries with backoff while the queue is not empty, synthesises into the cache and never plays.
  It stops on application shutdown; the queue is discarded on restart (spec assumption).

### Settings

- `SpeechSettings.ProviderId` (code default `system-speech`, available on every Windows machine without setup; the
  shipped `MOBAflow/appsettings.json` selects `piper` as today), `FallbackProviderId` (`piper` or `system-speech`),
  `CacheSizeLimitMegabytes`, plus `Piper`, `SystemSpeech` and `Azure` option sections. The Azure section holds the
  region and voice, which are not secret; only the key is in the credential store. Shared `Rate`, `Volume`,
  `VoiceName`, `TestMessage` stay at the top level. A settings file with only the removed fields loads with
  `system-speech`.
- Slice 2 replaces the existing Settings ViewModel properties and XAML bindings with `ProviderId` and the new
  local-provider option sections in the same cutover as removal of the old fields. The local provider selector,
  settings persistence, test callback and speech health bindings remain usable and compile in that slice.
- Slice 5 extends those controls with capability filtering, Azure and fallback configuration, cache size and
  "Clear cache"; the Azure key is entered in a password box and saved to the credential store.

## Delivery Slices

1. **Characterise**: tests for current announcement behavior (selection, voice, rate/volume forwarding, ordering,
   cancellation) against the existing engines.
2. **Contract, shared output and local providers** (one cutover, so no merged state lacks a working speaker): add
   `Speech.Base` with `ISpeechOutput`, `SpeechOutput` (no cache yet) and `NullSpeechOutput`; add
   `Speech.SystemSpeech` and `Speech.Piper`, move code and tests, synthesise to WAV and play through a cancellable
   player; switch Backend callers and register both providers in MOBAflow; MOBAsmart registers the null output;
   remove `System.Speech` from `Sound` (`System.Windows.Extensions` stays for `WindowsSoundPlayer`, whose split
   belongs to RF-29); replace the existing local-provider Settings ViewModel properties, page bindings, test
   callback and health bindings in this same slice, preserving saving and immediate switching; remove
   `ISpeakerEngine`, `SpeakerEngineFactory`, `SpeechSpeakerEngineSelection` and the old settings fields only with
   all their callers and bindings converted. This includes the compile-required part of the later Settings slice.
3. **Cache**: `AudioCache` with options fingerprint, least-recently-used eviction, per-key single flight with
   independent caller cancellation, and size/clear service APIs. The size display and "Clear cache" UI follow in
   slice 5.
4. **Azure and fallback**: `Speech.Azure` with credential store and fake HTTP tests; fallback provider setting
   excluding the failed provider; pending synthesis queue.
5. **Settings and diagnostics**: extend the local-provider controls from slice 2 with capabilities, Azure and
   fallback configuration, cache size display and "Clear cache", and diagnostics for fallback and skipped
   announcements. Immediate local-provider switching is already delivered and tested in slice 2.
6. **Documentation**: `docs/wiki/PIPER-TTS-SETUP.md`, user guide speech section, `docs/ARCHITECTURE.md`, and a
   short "add a provider" guide.

Each slice is one draft PR from its own worktree.

## Dependencies and Sequencing

- Start implementation only after PR #207 (RF-23 runtime per project, tracked by #191) is merged: both change
  `MobaWinUiServiceCollectionExtensions` and the Settings page. Verify its current GitHub merge state before slice
  1. Spec, plan and task corrections are independent; an open or ready-for-review PR does not satisfy this gate.
- Coordinate with #141: this feature is the ADR's example; the ADR can reference the result instead of
  re-deciding it.
- RF-29 (#47) drops its speech part (spec Q5); a comment on #47 records this when slice 2 lands.

## Validation Strategy

- **Automated tests**: `dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0 --filter
  "FullyQualifiedName~Sound|FullyQualifiedName~Announcement|FullyQualifiedName~Speech"` for the contract, cache,
  fallback and queue; the Windows test target for the provider libraries.
- **Builds**: `dotnet build MOBAflow/MOBAflow.csproj -c FastDebug ...` and `dotnet build
  MOBAsmart/MOBAsmart.csproj -f net10.0-android -c FastDebug` (proves the Android graph has no speech provider or
  Windows speech package).
- **Manual checks**: Settings page in Light and Dark; a spoken test message per provider and one fallback run with
  the network off on the maintainer's PC (requires launch approval and the maintainer's Azure key).
- **Regression checks**: slice 2 compiles with all removed-field callers and bindings converted; both the Settings
  test callback and `AnnouncementService` use the current selection and settings without restarting; app settings
  load with removed fields; analyzer baseline gates stay unchanged. Refresh snapshots only for removed/moved
  diagnostics and reviewed findings already suppressed as false positives. Slice 3 tests one cancelled waiter,
  all waiters cancelled,
  a new request after cancellation and shutdown during shared synthesis; cancelled calls start no fallback or
  deferred synthesis. Slice 4 repeats the cancellation assertions with fallback and the pending queue enabled.
- **Secrets scan**: all changed files; a test asserts that the key never appears in logs, diagnostics or cache
  file names.
- **Sonar gates**: SonarCloud green, zero OPEN/CONFIRMED issues per PR.
- **Issue traceability**: every PR references #136.
