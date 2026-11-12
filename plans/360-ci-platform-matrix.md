# RF-07: Complete the CI platform matrix

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/360
**Spec Kit**: Not applicable - CI coverage plus bounded fixes for portability defects exposed by the Linux lane
**Status**: Active; slice 1 is delivered (#361 plus the ruleset change of 2026-10-19). Slice 2 (Android lane policy)
is delivered: the 2026-11-08 extension (automatic run on PRs that change MOBAsmart) ships with the Android trigger
change for [#360](https://github.com/ahuelsmann/MOBAflow/issues/360); slice 3 (mutation and formatting lanes,
completion) waits for RF-09 and RF-10. Reconciled against main `7b1c3f96` on 2026-11-08.

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
| Android app (`MOBAsmart`) Release AAB | the clean Release AAB steps in [Build Performance](../docs/BUILD-PERFORMANCE.md); `scripts/Test-AndroidAppBundle.ps1` | `android-release` (AAB artifact kept 30 days) | on PRs that change `MOBAsmart/**` or `quality/analyzer-baseline.android.json` (maintainer decision of 2026-11-08, [#360](https://github.com/ahuelsmann/MOBAflow/issues/360)) and on manual `workflow_dispatch` with `validate_android` ([decision of 2026-10-19](https://github.com/ahuelsmann/MOBAflow/issues/360#issuecomment-6256088821)); the check name is required and passes as skipped on all other PR runs and on `main` runs |
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
  of 2026-10-19 kept the Release AAB lane manual via `workflow_dispatch` / `validate_android` and did not make it
  a mandatory PR lane; no workflow change was needed for that. Correction on main `7b1c3f96`: [Release
  Studio](../.github/workflows/release-studio.yml) publishes only `MOBAflow` and `MOBApi`; no workflow builds the
  AAB for a release. The only CI evidence for the AAB is the manual `android-release` job, whose artifact is kept
  for 30 days. Sample read on 2026-11-08 (19:30 UTC): the 15 Quality `workflow_dispatch` runs created between
  2026-11-07T01:08Z and 2026-11-08T19:25Z contain 13 completed `Android Release AAB` jobs on `windows-latest`
  (three runs were unfinished at read time; two of their Android jobs had not ended yet). Twelve passed in 4.7 to
  7.2 minutes (median about 6.2), of which 2.7 to 4.7 minutes (160 to 282 s) is the `Publish Android Release AAB`
  step; one job failed in that step after 35 s (run 42421785184).

  **Extension by the maintainer decision of 2026-11-08** (verbatim: "automatisch nur bei MOBAsmart-Änderungen.
  wenn das geht wäre es doch perfekt"): the job runs automatically when a change touches `MOBAsmart/**`. The
  team's reading of this for the workflow: it applies to PR runs, with `quality/analyzer-baseline.android.json` as
  a second trigger because only this job checks that baseline; the manual `validate_android` dispatch stays, and
  on all other PRs the required check stays skipped as today. Because `Android Release AAB` is a required check,
  a failing job blocks the merge of the PR that triggered it. Accepted trade-off: the following do not trigger
  the job automatically, and the manual dispatch remains for them:
  - the projects MOBAsmart compiles through its references (`SharedUI`, `Common`, `Sound`, `Backend`,
    `MOBAdisplay`, `TrackPlan.Renderer`, `TrackPlan.Editor`, `TrackLibrary.Base`, `Speech.Base`, `Domain`,
    `Contracts`);
  - repository-wide build inputs (`Directory.*.props`/`.targets`, `global.json`, `NuGet.Config`);
  - the job definition itself (`.github/workflows/quality.yml`), the scope rules and the scripts the job calls.

  Implemented with the Android trigger change for #360: `scripts/Get-QualityChangeScope.ps1` has an `android`
  output for the two trigger paths (an unknown path or an empty change set does not select it, unlike the other
  scopes), `quality.yml` runs the job for `pull_request` events unless that output is `false` or for the manual
  dispatch, so a failed change detection runs it like the other scoped jobs, and
  `Get-QualityChangeScope.Tests.ps1` covers both triggers, the SharedUI-only, desktop-only and
  documentation-only counter-cases and the fail-open job conditions. Pushes to `main` stay skipped. A skipped job satisfies the required check but
  is not evidence of an AAB build. The options and measurements are recorded on
  [#360](https://github.com/ahuelsmann/MOBAflow/issues/360).

## Independently reviewable delivery slices

1. **Inventory and Linux lane** (with this plan). The check name `Portable tests on Linux` is in the `main` ruleset,
   so the lane is a required check; this slice is complete.
2. **Android Release lane policy:** the 2026-10-19 decision retained manual validation; the 2026-11-08 decision
   extends it with an automatic run on PRs that change `MOBAsmart/**` (second trigger:
   `quality/analyzer-baseline.android.json`; see the Android design decision above). The workflow condition, scope-script
   output, tests and docs ship with the Android trigger change for #360; this slice is complete.
3. **Mutation and formatting lanes** after RF-09 and RF-10, documentation and completion.

## Validation strategy

Slice 1: the local equivalent of the lane on Windows (`dotnet test … -p:TargetFrameworks=net10.0 -f net10.0
--configuration Release` with the same filter and the result guard); the first CI run of the PR proves the lane on
Linux; `actionlint` runs in the `instruction-consistency` lane.

## Risks

- **Linux-only failures:** tests that assume Windows paths or culture surface for the first time; they are fixed in
  the test or product code, not excluded, unless they need a Windows API.
