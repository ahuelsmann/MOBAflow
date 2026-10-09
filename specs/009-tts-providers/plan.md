# Implementation Plan: Selectable text-to-speech providers

**Branch**: `codex/issue-136-tts-providers-spec` | **Date**: 2026-10-09 | **Spec**: [spec.md](spec.md)

**GitHub Issue**: #136

**Spec Kit**: Required

**Status**: Draft (updated with the clarifications of 2026-10-09; awaiting maintainer approval)

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
folder. Azure key and region in the Windows credential store.

**Testing**: NUnit and Moq; fake providers and a fake clock/file system for the cache; existing
`SpeakerEngineFactoryTests`, `PiperSpeechEngineTest`, `SystemSpeechEngineTest`, `AnnouncementServiceTests` and
`MobaBackendServiceCollectionExtensionsTests` are moved or replaced.

**Target Platforms**: Contract and shared speech output `net10.0` (Backend calls them on every platform); all
provider libraries `net10.0-windows`, referenced by MOBAflow only.

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
- [x] The design reuses `ISoundPlayer`, the speech health check and the DI extension methods; `ISpeakerEngine` and
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
  play it (FR-005).
- `SpeechResult`: WAV audio on success, or a failure kind (`Unavailable`, `InvalidRequest`, `Failed`) with a
  display-safe reason. Expected failures are results, not exceptions.
- `SpeechCapabilities`: voices with language, and whether rate and volume are supported.
- `ISpeechOutput`: `SpeakAsync(text, CancellationToken)` for callers; `AnnouncementService`,
  `ActionExecutionContext` and the Settings test command use it instead of `ISpeakerEngine`.

### Shared speech output

`SpeechOutput` runs each announcement in this order:

1. Build the request from the current settings and resolve the selected provider by id (FR-003, takes effect for
   the next call). An unknown id selects the default provider and writes a diagnostic.
2. **Cache hit**: play the cached WAV through `ISoundPlayer`; no provider call (FR-006, SC-003).
3. **Cache miss**: synthesise with the selected provider, store the audio in the cache, play it.
4. **Selected provider unavailable**: write a diagnostic, speak through the fallback provider immediately (Piper if
   configured, otherwise System Speech), and add the request to the pending queue (FR-007). Fallback audio is not
   cached under the selected provider's key.
5. **Nothing available**: skip the announcement with a diagnostic; the workflow continues (FR-008, SC-005).

Overlapping announcements keep today's ordering, characterised in slice 1.

### Audio cache

- Key: SHA-256 of provider id, voice, language, rate, volume and normalised text; file name is the hash, so no text
  or credential appears in a file name (FR-009).
- WAV files plus an index with size and last-use time; the size limit (default 500 MB) removes the
  least-recently-used entries first. A corrupt or missing file is dropped and synthesised again.
- `GetSizeAsync` and `ClearAsync` back the Settings size display and the "Clear cache" command.

### Pending synthesis

- An in-memory queue of requests whose selected provider was unavailable; duplicates by cache key are merged.
- A background loop retries with backoff while the queue is not empty, synthesises into the cache and never plays.
  It stops on application shutdown; the queue is discarded on restart (spec assumption).

### Settings

- `SpeechSettings.ProviderId`, `FallbackProviderId` (`piper` or `system-speech`), `CacheSizeLimitMegabytes`, plus
  `Piper`, `SystemSpeech` and `Azure` option sections (Azure: region and voice only; the key is in the credential
  store). Shared `Rate`, `Volume`, `VoiceName`, `TestMessage` stay at the top level.
- The Settings ViewModel lists providers with their capabilities, enables only supported options, shows the cache
  size and offers "Clear cache"; the Azure key is entered in a password box and saved to the credential store.

## Delivery Slices

1. **Characterise**: tests for current announcement behavior (selection, voice, rate/volume forwarding, ordering,
   cancellation) against the existing engines.
2. **Contract and shared output**: add `Speech.Base` with `ISpeechOutput`, `SpeechOutput` (no cache yet) and
   `NullSpeechOutput`; switch Backend callers; MOBAsmart registers the null output.
3. **Local providers**: add `Speech.SystemSpeech` and `Speech.Piper`, move code and tests, synthesise to WAV and
   play through `ISoundPlayer`; remove `System.Speech` and `System.Windows.Extensions` from `Sound`; remove
   `ISpeakerEngine`, `SpeakerEngineFactory` and the old settings fields.
4. **Cache**: `AudioCache` with size limit, size display and "Clear cache".
5. **Azure and fallback**: `Speech.Azure` with credential store and fake HTTP tests; fallback provider setting;
   pending synthesis queue.
6. **Settings and diagnostics**: provider list with capabilities, immediate switch, diagnostics for fallback and
   skipped announcements.
7. **Documentation**: `docs/wiki/PIPER-TTS-SETUP.md`, user guide speech section, `docs/ARCHITECTURE.md`, and a
   short "add a provider" guide.

Each slice is one draft PR from its own worktree.

## Dependencies and Sequencing

- Start implementation after RF-23 slice 3 (#191) is merged: both change `MobaWinUiServiceCollectionExtensions`
  and the Settings page. Spec and plan work is independent.
- Coordinate with #141: this feature is the ADR's example; the ADR can reference the result instead of
  re-deciding it.
- RF-29 (#47) drops its speech part (spec Q5); a comment on #47 records this when slice 3 lands.

## Validation Strategy

- **Automated tests**: `dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0 --filter
  "FullyQualifiedName~Sound|FullyQualifiedName~Announcement|FullyQualifiedName~Speech"` for the contract, cache,
  fallback and queue; the Windows test target for the provider libraries.
- **Builds**: `dotnet build MOBAflow/MOBAflow.csproj -c FastDebug ...` and `dotnet build
  MOBAsmart/MOBAsmart.csproj -f net10.0-android -c FastDebug` (proves the Android graph has no speech provider or
  Windows speech package).
- **Manual checks**: Settings page in Light and Dark; a spoken test message per provider and one fallback run with
  the network off on the maintainer's PC (requires launch approval and the maintainer's Azure key).
- **Regression checks**: app settings load with removed fields; analyzer baselines unchanged.
- **Secrets scan**: all changed files; a test asserts that the key never appears in logs, diagnostics or cache
  file names.
- **Sonar gates**: SonarCloud green, zero OPEN/CONFIRMED issues per PR.
- **Issue traceability**: every PR references #136.
