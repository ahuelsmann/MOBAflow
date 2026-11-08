# RF-07: Complete the CI platform matrix

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/360
**Spec Kit**: Not applicable - CI coverage plus bounded fixes for portability defects exposed by the Linux lane
**Status**: Active; slices 1 and 2 are delivered (#361 plus the ruleset change of 2026-10-19; recorded Android
decision); slice 3 (mutation and formatting lanes, completion) waits for RF-09 and RF-10. Reconciled against main
`7b1c3f96` on 2026-11-08.

## Purpose and scope

[Issue #360](https://github.com/ahuelsmann/MOBAflow/issues/360) owns acceptance and live status.
The [umbrella plan](QUALITY-AND-REFACTORING-PLAN.md) owns programme dependencies. This is the single RF-07 child plan.

Target: every supported deliverable is reproduced from a clean checkout with the documented local command and an
equivalent mandatory CI lane, and its artifacts and test results are kept as workflow evidence.

## Deliverable inventory

Recorded on main `f4756172`, re-read on main `7b1c3f96` (2026-11-08). The CI lanes are jobs of
[Quality CI](../.github/workflows/quality.yml) unless noted.

| Deliverable | Local command (from [AGENTS.md](../AGENTS.md)) | CI lane | Runs on PRs / required check |
| --- | --- | --- | --- |
| Portable libraries and their tests (`Domain`, `Contracts`, `Common`, `Backend`, `SharedUI`, …) on Linux | `dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0` | `portable-linux` (**new in slice 1**) | yes, when code changes; a required check (ruleset 24007434, read on 2026-11-06) |
| Windows desktop (`MOBAflow`) and the Windows test suite, coverage ratchet, analyzer gates, NuGet audit | `dotnet test Test/Test.csproj -f net10.0-windows10.0.22621.0 -p:IncludeMobaSmartTests=false` | `desktop` | yes, when code changes |
| MOBApi host and its process integration tests | `dotnet build MOBApi/MOBApi.csproj` | `display-protocol` (MOBApi process integration) and the `desktop` test graph | yes, when code changes |
| Display host, parser and ESP32-S3 firmware | `dotnet build MOBAdisplay/MOBAdisplay.csproj`; PlatformIO in `MOBAdisplay/esp32` | `display-protocol` (pinned PlatformIO) | firmware build when firmware paths change |
| Android app (`MOBAsmart`) Release AAB | the clean Release AAB steps in [Build Performance](../docs/BUILD-PERFORMANCE.md); `scripts/Test-AndroidAppBundle.ps1` | `android-release` (AAB artifact kept 30 days) | **no**: only on manual `workflow_dispatch` with `validate_android` ([recorded maintainer decision](https://github.com/ahuelsmann/MOBAflow/issues/360#issuecomment-6256088821)); the check name is required and passes as skipped on PR and `main` runs |
| Mutation lanes (`domain`, `common`, `speech-base`, `backend`, `api`, `shared-ui` active) | `scripts/Invoke-MutationLane.ps1 -LaneName <lane>` | `mutation-domain`, `mutation-common`, `mutation-speech-base`, `mutation-backend`, `mutation-api`, `mutation-shared-ui` | all run by change scope; only Domain mutation testing is a required check; further lanes belong to [RF-09](366-coverage-mutation-ratchets.md) |
| Documentation and repository instructions | `scripts/Test-DocumentationLinks.ps1`, `scripts/Test-InstructionConsistency.ps1` | `instruction-consistency`; external links on a schedule | yes |
| Release package | [Release Studio](../.github/workflows/release-studio.yml) | manual release workflow | release only |

Required (merge-blocking) checks are those of the `main` branch ruleset 24007434 (last changed 2026-10-19, read
again on 2026-11-08): Repository instruction consistency; Windows desktop build, tests, and coverage; Display protocol
host, parser, and firmware; Android Release AAB; Domain mutation testing; SonarCloud Code Analysis; Portable tests on
Linux. The ruleset also requires the branch to be up to date with `main` (strict status checks). The other mutation
lanes (Common, Speech.Base, Backend, MOBApi, SharedUI) run but do not block a merge.

## Design decisions

- **Linux lane:** `portable-linux` runs the portable suite on `ubuntu-latest` in Release. Like the MOBApi
  integration step of `display-protocol`, it uses `scripts/Test-TestRunResults.ps1`, so a filter that selects nothing or a skipped test fails the lane
  (at least 2500 tests must pass). The exclusions are:
  - `MobaApiRuntimeDeploymentTests.Create_CopyFailureRemovesPartialRuntimeCopyAndPreservesSource` needs Windows file
    locks (`[Platform("Win")]`) and runs in the `desktop` lane;
  - the explicit, maintainer-run `Hardware` category, which never runs in CI;
  - the Skia rendering fixtures (`ClockRendererTests`, `Rgb565ConverterTests`,
    `SkiaFrameRendererTests`), which stay in the Windows desktop lane like in `display-protocol`, because there
    is no native Linux Skia library.

  The first Linux run found real portability defects, fixed in the same PR: `PhotoPathHelper` now rejects
  Windows absolute paths (`C:\…`, `C:…`, `\…`, UNC) on every platform and treats both separators of stored photo
  paths as directory boundaries. The highlighter also uses elapsed TimeSpan ticks consistently for pulse expiry,
  ignore-window comparisons and intensity calculation. Raw Stopwatch ticks have a platform-dependent frequency;
  treating them as TimeSpan ticks expired pulses prematurely on Linux. The ignore-window test counts only
  synchronous feedback raises, separate from animation-frame notifications. Fake-only `FrameLoopSchedulerTests`
  remain in the portable lane; they do not load native Skia.
- **Android:** the [maintainer decision](https://github.com/ahuelsmann/MOBAflow/issues/360#issuecomment-6256088821)
  keeps the Release AAB lane manual via `workflow_dispatch` / `validate_android`. It does not become a mandatory PR
  lane. This settles slice 2 without a workflow change. Correction on main `7b1c3f96`: [Release
  Studio](../.github/workflows/release-studio.yml) publishes only `MOBAflow` and `MOBApi`; no workflow builds the
  AAB for a release. The only CI evidence for the AAB is the manual `android-release` job, whose artifact is kept
  for 30 days. Sample read on 2026-11-08 (19:30 UTC): the 15 Quality `workflow_dispatch` runs created between
  2026-11-07T01:08Z and 2026-11-08T19:25Z contain 13 completed `Android Release AAB` jobs on `windows-latest`
  (two runs were still in progress). Twelve passed in 4.7 to 7.2 minutes (median about 6.2), of which 2.7 to 4.7
  minutes (160 to 282 s) is the `Publish Android Release AAB` step; one job failed in that step after 35 s (run
  42421785184). Whether the job should also run in PR runs (every code PR, Android-input PRs only, `main` pushes
  only, or manual only as today) is still under discussion with the maintainer, who indicated on 2026-11-08 that
  it should not run on every PR by default; the options and measurements are recorded on
  [#360](https://github.com/ahuelsmann/MOBAflow/issues/360).

## Independently reviewable delivery slices

1. **Inventory and Linux lane** (with this plan). The check name `Portable tests on Linux` is in the `main` ruleset,
   so the lane is a required check; this slice is complete.
2. **Android Release lane policy:** settled by the recorded maintainer decision to retain manual validation. Whether
   the lane also runs in PR runs is a separate open decision (see the Android design decision above).
3. **Mutation and formatting lanes** after RF-09 and RF-10, documentation and completion.

## Validation strategy

Slice 1: the local equivalent of the lane on Windows (`dotnet test … -p:TargetFrameworks=net10.0 -f net10.0
--configuration Release` with the same filter and the result guard); the first CI run of the PR proves the lane on
Linux; `actionlint` runs in the `instruction-consistency` lane.

## Risks

- **Linux-only failures:** tests that assume Windows paths or culture surface for the first time; they are fixed in
  the test or product code, not excluded, unless they need a Windows API.
