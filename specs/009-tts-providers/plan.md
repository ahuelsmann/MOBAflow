# Implementation Plan: Selectable text-to-speech providers

**Branch**: `codex/issue-136-tts-providers-spec` | **Date**: 2026-10-08 | **Spec**: [spec.md](spec.md)

**GitHub Issue**: #136

**Spec Kit**: Required

**Status**: Draft (based on the recommended answers in spec.md; revise after clarification)

**Input**: Feature specification from `/specs/009-tts-providers/spec.md`

## Summary

Split the existing speech engines into a provider-neutral contract project and one class library per provider,
register providers through DI in the host, let Settings list providers with their capabilities, and resolve the
selected provider per announcement. The first delivery moves the two existing local providers; a cloud provider is
an optional later slice (spec Q1).

## Technical Context

**Language/Version**: .NET 10 / C#

**Primary Dependencies**: Existing `System.Speech` (Windows only after the move) and the Piper executable; no new
package for the first delivery. A cloud SDK is added only with its own slice after Q1.

**Storage/Protocols**: `AppSettings.Speech` (app settings JSON): provider id plus one options section per provider.

**Testing**: NUnit and Moq; fake providers in `Test/Sound`; existing `SpeakerEngineFactoryTests`,
`PiperSpeechEngineTest`, `SystemSpeechEngineTest`, `AnnouncementServiceTests`.

**Target Platforms**: Contract and Piper provider cross-platform (`net10.0`); System Speech provider
`net10.0-windows`.

**Affected Layers**: `Sound` (split), `Common/Configuration` (speech settings), `Backend` (announcement callers),
`SharedUI` (settings ViewModel), `MOBAflow` (DI, Settings page), `MOBAsmart` (DI only, unchanged behavior).

**Performance Goals**: Resolving the selected provider adds no noticeable delay before an announcement; provider
instances are reused while their settings are unchanged.

**Data and API Effects**: Speech settings get a provider id and provider sections; old fields are removed and
ignored on load (no migration). `solution.json` unchanged.

**Scale/Scope**: Two providers in the first delivery; Android speech and runtime plugin loading excluded.

## Constitution Check

- [x] Layer and platform boundaries are preserved: the contract has no SDK or UI types; Windows code moves to a
  Windows-only library.
- [x] EventBus UI-thread marshalling remains centralized in `UiThreadEventBusDecorator` (no EventBus change).
- [x] Async flows use `await` without sync-over-async.
- [x] UI behavior uses ViewModel commands; Settings changes use existing ThemeResources.
- [x] All user-visible UI strings are English.
- [x] Data and API effects are explicit; old speech fields are removed without migration.
- [x] Regression tests cover selection, forwarding, cancellation and failures.
- [x] Validation follows the `AGENTS.md` matrix (shared logic plus WinUI compile, Android compile for package
  removal).
- [x] The design reuses `ISpeakerEngine`, `ISpeakerEngineRegistration`, `ISoundPlayer` and the DI extension
  methods.
- [x] Spec and plan reference #136.
- [x] Artifacts stay below `specs/`.
- [x] Changed files are scanned for secrets before commit; no credentials in the first delivery.
- [x] Sonar runs only in GitHub CI.
- [x] PRs stay draft until SonarCloud is green with zero OPEN/CONFIRMED issues.

## Design

### Projects

```text
Speech.Base/             # contracts: ISpeechProvider, SpeechRequest, SpeechCapabilities, SpeechResult,
                         # ISpeechProviderCatalog (lists registered providers)
Speech.Piper/            # Piper provider (net10.0), PiperPronunciationNormalizer, PcmWavePostProcessor
Speech.SystemSpeech/     # Windows System Speech provider (net10.0-windows)
Sound/                   # keeps sound-effect playback (ISoundPlayer, SoundManager) and the null engine
```

Names follow the `TrackLibrary.Base` / `TrackLibrary.PikoA` pattern; final names are confirmed in review.

### Contract (sketch)

- `ISpeechProvider`: `Id`, `DisplayName`, `GetCapabilitiesAsync`, `SpeakAsync(SpeechRequest, CancellationToken)`
  returning `SpeechResult`.
- `SpeechResult`: success or failure with a display-safe reason; providers do not throw for expected failures.
- The current `ISpeakerEngine` callers (`AnnouncementService`, `ActionExecutionContext`, the Settings test action)
  switch to a small `ISpeechOutput` service that resolves the selected provider per call (Q2) and records failures
  in the speech diagnostics (Q3). `ISpeakerEngine`, `SpeakerEngineFactory` and `SpeechSpeakerEngineSelection` are
  removed in the same change.

### Settings

- `SpeechSettings.ProviderId` plus `Piper` and `SystemSpeech` option sections; shared `Rate`, `Volume`,
  `VoiceName`, `TestMessage` stay at the top level.
- The Settings ViewModel lists providers from `ISpeechProviderCatalog` and enables only supported options.

## Delivery Slices

1. **Characterise**: tests for current announcement behavior (selection, voice, rate/volume forwarding, ordering,
   cancellation) against the existing engines.
2. **Contract and Piper**: add `Speech.Base` and `Speech.Piper`, move Piper code and tests, add `ISpeechOutput`.
3. **System Speech**: add `Speech.SystemSpeech` (Windows), move code and tests, remove `System.Speech` and
   `System.Windows.Extensions` from `Sound` (Q5), register in MOBAflow only.
4. **Settings and diagnostics**: provider list with capabilities, per-call resolution (Q2), failure diagnostics
   (Q3); remove `ISpeakerEngine` and the old factory and settings fields.
5. **Optional cloud provider** (only if Q1 names one): own library, credential store, tests with a fake HTTP
   handler; no live calls in CI.
6. **Documentation**: `docs/wiki/PIPER-TTS-SETUP.md`, user guide speech section, `docs/ARCHITECTURE.md`, and a
   short "add a provider" guide.

Each slice is one draft PR from its own worktree.

## Dependencies and Sequencing

- Start implementation after RF-23 slice 3 (#191) is merged: both change `MobaWinUiServiceCollectionExtensions`
  and the Settings page. Spec and plan work is independent.
- Coordinate with #141: this feature is the ADR's example; the ADR can reference the result instead of
  re-deciding it.
- RF-29 (#47) drops its speech part if Q5 is accepted.

## Validation Strategy

- **Automated tests**: `dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0 --filter
  "FullyQualifiedName~Sound|FullyQualifiedName~Announcement|FullyQualifiedName~Speech"`; Windows target for the
  System Speech provider tests.
- **Builds**: `dotnet build MOBAflow/MOBAflow.csproj -c FastDebug ...` and `dotnet build
  MOBAsmart/MOBAsmart.csproj -f net10.0-android -c FastDebug` (proves the Android graph has no Windows speech
  packages).
- **Manual checks**: Settings page in Light and Dark; a spoken test message per provider on the maintainer's PC
  (requires launch approval).
- **Regression checks**: app settings load with removed fields; analyzer baselines unchanged.
- **Secrets scan**: all changed files; cloud slice adds a check that keys never reach logs.
- **Sonar gates**: SonarCloud green, zero OPEN/CONFIRMED issues per PR.
- **Issue traceability**: every PR references #136.
