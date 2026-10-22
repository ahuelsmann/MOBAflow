# Build Performance

Practical guidance for faster local build and deploy loops in MOBAflow and MOBAsmart.

## MOBAflow (WinUI)

**Fast daily compile check:**

```bash
dotnet restore MOBAflow/MOBAflow.csproj
dotnet build MOBAflow/MOBAflow.csproj -c FastDebug --no-restore \
  /p:BuildMOBApiDependency=false /p:CopyMOBApiToOutput=false
```

**Why the default build feels slow**

- WinUI XAML compilation and Windows App SDK (self-contained output is large).
- Default build also compiles MOBApi and copies it into the WinUI output folder.

**Tips**

- Use VS Code task `build` (FastDebug) for iteration.
- Use `dotnet watch run --project MOBAflow/MOBAflow.csproj -c FastDebug` while editing.
- Add Windows Defender exclusions for `bin/`, `obj/`, and `.nuget/`.
- Build a single `.csproj`, not the full solution.

## MOBAsmart (Android)

**AndroidX package pins**

- `Xamarin.AndroidX.Startup.StartupRuntime` is required for MAUI initialization providers.
- Older manual pins for `Tracing` and `Concurrent.Futures` were removed with MAUI 10.0.71; restore them only if a build fails with a missing AndroidX type.

**Fast daily build (recommended):**

```bash
dotnet restore MOBAsmart/MOBAsmart.csproj
dotnet build MOBAsmart/MOBAsmart.csproj -f net10.0-android -c FastDebug --no-restore
```

`dotnet restore` has no framework selector: its `-f` option means `--force`.
MOBAsmart targets only `net10.0-android`, so the project-scoped restore selects
the Android graph without an additional option.

Fast deploy is enabled by default for Debug and FastDebug (Visual Studio F5 and CLI).
Assemblies are pushed over adb on incremental deploys instead of rebuilding a full APK.

**Reliable deploy (opt-in, before release testing):**

```bash
dotnet build MOBAsmart/MOBAsmart.csproj -f net10.0-android -c FastDebug --no-restore \
  /p:MobaReliableDeploy=true -t:Run
```

Use reliable deploy when fast deploy behaves inconsistently on a device.

**Clean Release AAB:**

The .NET SDK band from `global.json` (10.0.4xx, at least 10.0.402) and the MAUI Android workload are
prerequisites. The workload set version is pinned in the install command, not in `global.json`: a
`workloadVersion` there would fail every build, including desktop-only builds, on machines without that
workload set. Install the pinned set explicitly instead of relying on `--skip-manifest-update`.
From a clean checkout, run the install, restore, publish, and bundle validation defined in CI:

```powershell
dotnet workload install maui-android --version 10.0.402
dotnet restore MOBAsmart/MOBAsmart.csproj `
  --property:Configuration=Release `
  --force-evaluate
dotnet publish MOBAsmart/MOBAsmart.csproj `
  --framework net10.0-android `
  --configuration Release `
  --no-restore `
  -m:1
./scripts/Test-AndroidAppBundle.ps1 `
  -BundlePath MOBAsmart/bin/Release/net10.0-android/com.mobaflow.mobasmart.aab
```

Release produces an AAB for the `android-arm64` and `android-x64` runtime
identifiers. The validation script requires the corresponding `arm64-v8a` and
`x86_64` native libraries and the base manifest, resources, and DEX entries.
The Release property on restore is required so both RID graphs are present;
single-node publish avoids concurrent writes from their shared project graph.

**Visual Studio**

- F5 with **Debug** or **FastDebug** uses fast deploy automatically.
- Project Properties > Android > Options should show fast deployment enabled for Debug.
- For a one-off reliable APK install: build with `/p:MobaReliableDeploy=true` or add
  `MobaReliableDeploy` to MSBuild properties in the VS build settings.

**Why Android deploy feels slow**

- First build compiles MAUI, Android resources, DEX, and packages an APK.
- ~125 function-symbol PNGs are processed at build time (now capped at 32x32).
- Sound workflow WAV files (~3.4 MB) are excluded from the Android package.
- USB install + app startup add time on top of compile time.

**Already enabled in `MOBAsmart/Build/AndroidDevPerformance.props`**

- `android-arm64` only (single device ABI)
- No AOT / no ProGuard in Debug and FastDebug
- Incremental manifest merge and native library build
- APK (not AAB) for local Debug/FastDebug
- Analyzers off in Debug/FastDebug

**Tips**

- Prefer **FastDebug** over **Debug** for UI iteration.
- Use VS Code tasks `build:mobasmart` (fast deploy default) or
  `build:mobasmart:reliable-deploy`.
- Keep the phone connected over USB 3; Wi-Fi deploy is slower.
- Close the Android emulator if you deploy to a physical device.
- Exclude `bin/`, `obj/`, and `.nuget/` from real-time antivirus scanning.

## Measuring locally

```bash
dotnet build <project>.csproj -bl:build.binlog
```

Open `build.binlog` with [MSBuild Structured Log Viewer](https://msbuildlog.com/).

## CI note

The separate [observational dependency review](DEPENDENCY-REVIEW.md) reports pull-request
dependency changes and graph limitations. It adds no required gate and does not replace the
resolved NuGet audit described below. A successful job is not a claim of complete graph coverage.

The public, authoritative pull-request check is `.github/workflows/quality.yml`.
It builds the explicit Windows desktop graph with `IncludeMobaSmartTests=false`,
runs NUnit with Cobertura coverage, audits the resolved transitive NuGet graph,
and retains the reports and packages. The Android Release AAB job is skipped on
regular PR and main runs while MOBAsmart is not distributed. Its required check
name remains in place. To validate SDK/workload changes on a clean Windows runner,
dispatch Quality with `validate_android=true` against the task branch:

```powershell
gh workflow run quality.yml --ref <task-branch> -f validate_android=true
```

The job installs and verifies workload set 10.0.402, publishes the Release AAB,
enforces the Android analyzer baseline and validates/archives the bundle. It does
not deploy to a device or publish a GitHub Release. The SDK follows the agreed
10.0.4xx feature-band policy in `global.json`: patches at least 10.0.402 are allowed,
so this is a feature-band pin rather than an exact SDK patch pin. Mobile tests
remain an explicit opt-in graph with `IncludeMobaSmartTests=true`.

The Linux display job also runs the MOBApi process integration tests
(`MobApiProcessTests`, `RuntimeHubLiveE2ETests`) in the `net10.0` target. Each
fixture starts the built MOBApi as an isolated process on a free local port, so
no running server or hardware is needed. `scripts/Test-TestRunResults.ps1`
fails the step when a selected test is skipped or too few tests run. Locally:

```powershell
dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0 --filter "FullyQualifiedName~Moba.Test.Integration.MobApiProcessTests|FullyQualifiedName~Moba.Test.Integration.RuntimeHubLiveE2ETests"
```

The `portable-linux` job (check `Portable tests on Linux`) runs the portable
suite on `ubuntu-latest` for every code-changing PR, every push to `main` and
manual runs. It is not yet a required check. The filter excludes the `Hardware`
category, the Windows file-lock test
`MobaApiRuntimeDeploymentTests.Create_CopyFailureRemovesPartialRuntimeCopyAndPreservesSource`
and the Skia fixtures `ClockRendererTests`, `Rgb565ConverterTests` and
`SkiaFrameRendererTests`, which run in the Windows desktop job.
`scripts/Test-TestRunResults.ps1` requires at least 2500 passed tests and no
skipped test; the TRX results are archived for 30 days. Locally:

```powershell
dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0 --configuration Release --filter "TestCategory!=Hardware&FullyQualifiedName!=Moba.Test.Common.MobaApiRuntimeDeploymentTests.Create_CopyFailureRemovesPartialRuntimeCopyAndPreservesSource&FullyQualifiedName!~ClockRendererTests&FullyQualifiedName!~Rgb565ConverterTests&FullyQualifiedName!~SkiaFrameRendererTests"
```

Pull requests that change only documentation paths (`docs/`, `plans/`, `specs/`,
`.specify/`, root Markdown files and GitHub guidance) skip the desktop, display,
Android and mutation jobs; `scripts/Get-QualityChangeScope.ps1` owns that list.
GitHub reports those skipped jobs as successful required checks. Pushes to
`main` and manual runs classify all scopes as enabled. Other PR changes enable
desktop, display, Common and Speech.Base mutation jobs; Domain mutation and ESP32 firmware
steps also depend on their input paths. Android runs only with the manual
`validate_android` input. A skipped check is not evidence that tests ran.

Code changes also select two expensive parts by the paths they read. The
ESP32 firmware steps (PlatformIO install, native tests, build and archive) run
when `MOBAdisplay/esp32/` changes or an unknown input path requires them. The
Common and Speech.Base mutation jobs run for every code-relevant PR. Domain mutation testing runs when
`Domain/`, its linked tests and fixture data (`Test/TestFile/`), `MutationTest/`
or the .NET build configuration change. A path outside the known .NET areas,
such as a workflow, a script or a new top-level folder, enables both firmware
and Domain mutation testing. The jobs keep their required check names;
skipped steps and a skipped job count as successful.
`scripts/Get-QualityChangeScope.Tests.ps1` checks these rules, including every
source and copied fixture input of the mutation project.

The repository consistency job also checks source-derived package and mutation
documentation facts and runs their offline boundary tests. Documentation and
wiki impact beyond these deterministic facts is recorded through the existing
delivery process and independent Claude PR review; see
[documentation consistency](DOCUMENTATION-CONSISTENCY.md).

The repository consistency job also lints every workflow with a pinned,
checksum-verified actionlint release and checks internal documentation links
and images with `scripts/Test-DocumentationLinks.ps1`. External links are
checked weekly and on demand by `.github/workflows/documentation-links.yml`,
so an unreachable foreign server does not block a pull request. Run the
internal check locally with `./scripts/Test-DocumentationLinks.ps1`; add
`-IncludeExternal` to request external links as well.
Temporary HTTP 408/500/502/503/504 and network failures get up to three attempts,
with one- and two-second retry delays. Persistent failures and HTTP 403/404 still
fail the check; the existing HTTP 429 tolerance is unchanged.

The workflow enforces a coverage ratchet from `Test/coverage-thresholds.json`.
The thresholds are per production assembly as well as global, so an improvement
in one project cannot hide a regression in another. Generated `obj` sources are
excluded; handwritten application code remains part of the measurement.

The analyzer baselines in `quality/` must match exactly, so fixed diagnostics
also fail the gate until the baseline is refreshed. When the only difference is
removed or decreased diagnostics, the failed run attaches the refreshed file as
the `refreshed-analyzer-baselines-desktop-*` artifact. Review it, copy it over
the matching file in `quality/` and run
`scripts/Test-LineEndings.ps1 -Path <file> -Fix`. New or increased diagnostics
never produce a refreshed file; fix them instead.

While the Android Release AAB job is disabled, CI does not check
`quality/analyzer-baseline.android.json`. When a change touches code that
MOBAsmart compiles, refresh and check it locally with the MAUI Android workload
installed:

```powershell
Remove-Item -Recurse -Force artifacts/analyzers -ErrorAction SilentlyContinue
dotnet build MOBAsmart/MOBAsmart.csproj -f net10.0-android -c Release --no-incremental -m:1 -p:MobaAnalyzerGate=true -p:UseSharedCompilation=false
./scripts/Test-AnalyzerBaseline.ps1 -SarifRoot artifacts/analyzers/Release -BaselinePath quality/analyzer-baseline.android.json
```

Add `-UpdateBaseline` only when the reported difference is removed or decreased
diagnostics; fix new or increased diagnostics instead.

Run the same checks locally after producing a Release Cobertura report:

```powershell
./scripts/Test-CoverageThresholds.ps1 -CoveragePath <coverage.cobertura.xml>
./scripts/Test-MutationLaneCoverage.ps1
```

### Active mutation lanes

[`mutation-lanes.json`](../MutationTest/mutation-lanes.json) registers every test
fixture. Lanes marked `planned` do not claim mutation coverage. The `architecture`
lane has no production project: its tests read project files and sources, so
production-code mutations do not exercise them. Active lanes additionally declare their Stryker
configuration. The registry guard evaluates each configured host's actual MSBuild `Compile` items
and production reference; registration alone is not enough. It rejects missing active fixtures and
test-case filters. Planned lanes remain explicitly outside the mutation guarantee.

<!-- documentation-facts:mutation:start -->
| Active lane | Production project | Test project | Stryker configuration | Break threshold |
| --- | --- | --- | --- | --- |
| domain | `Domain/Domain.csproj` | `MutationTest/Domain.MutationTests.csproj` | [`stryker-config.json`](../MutationTest/stryker-config.json) | 90% |
| common | `Common/Common.csproj` | `MutationTest/Common/Common.MutationTests.csproj` | [`stryker-common-config.json`](../MutationTest/stryker-common-config.json) | 63% |
| speech-base | `Speech.Base/Speech.Base.csproj` | `MutationTest/Speech.Base/Speech.Base.MutationTests.csproj` | [`stryker-speech-base-config.json`](../MutationTest/stryker-speech-base-config.json) | 65% |
<!-- documentation-facts:mutation:end -->

Domain runs when its inputs change; Common and Speech.Base run for every code-relevant PR.
All three also run on main pushes and manual Quality runs. Domain and Speech.Base run on
Linux. Common uses Windows because its existing fixtures include Windows-specific
photo paths. Its concurrency is limited to four test processes. The Common lane includes selected Backend test
fixtures for Common contracts and `SpeechSettingsTests.cs`; it does not mutate Backend production code.
Speech.Base mutates the entire assembly, including the audio cache, shared synthesis and WAV handling.
Its four portable Speech fixtures use fake providers/players and four Stryker processes. Windows-only
host integration cases and the separate Piper/SystemSpeech assemblies are outside this lane.
Thresholds come from the configuration files; the Common and Speech.Base minimums are baseline
safeguards, not targets for test quality. Speech.Base keeps scheduling-only `ConfigureAwait`
mutants visible rather than excluding them to inflate the score.

Run the ordinary tests successfully before interpreting a mutation score. Use
Windows for a local run of all lanes and stop on any failed command:

```powershell
dotnet tool restore
if ($LASTEXITCODE -ne 0) { throw "Tool restore failed." }
# Use new output directories; existing directories are rejected, not deleted.
./scripts/Invoke-MutationLane.ps1 -LaneName domain -OutputPath artifacts/mutation/domain-local
./scripts/Invoke-MutationLane.ps1 -LaneName common -OutputPath artifacts/mutation/common-local
./scripts/Invoke-MutationLane.ps1 -LaneName speech-base -OutputPath artifacts/mutation/speech-base-local
```

On Linux, run the Domain and Speech.Base lanes; omit the Windows-specific Common lane. Run
Stryker and ordinary builds/tests sequentially: Stryker recreates shared
intermediate files. CI uses the same runner, which requires a nonempty, fully passing ordinary-test
TRX baseline before starting Stryker. It validates the new JSON report's production root, registered
test files, test-case names/count and final mutant statuses. At least one mutant must actually have
run against tests; all-ignored, compile-error-only or uncovered-only reports cannot pass. Surviving
and uncovered mutants remain visible and subject to the unchanged Stryker score thresholds.
The jobs archive the baseline, report and Stryker logs below `artifacts/mutation/<lane>/`, including
on failure. `scripts/Test-MutationGuards.Tests.ps1` tests rejection paths in the Windows quality job.
Intentional job skips retain the path-selection rules above and are not execution evidence.
