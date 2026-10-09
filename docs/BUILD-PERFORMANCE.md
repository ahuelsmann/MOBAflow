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

The pinned .NET SDK and the MAUI Android workload are prerequisites. From a
clean checkout, run the restore, publish, and bundle validation defined in the disabled CI job:

```powershell
dotnet workload restore MOBAsmart/MOBAsmart.csproj --skip-manifest-update
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

The public, authoritative pull-request check is `.github/workflows/quality.yml`.
It builds the explicit Windows desktop graph with `IncludeMobaSmartTests=false`,
runs NUnit with Cobertura coverage, audits the resolved transitive NuGet graph,
and retains the reports and packages. The Android Release AAB job is disabled
while MOBAsmart is not distributed. Its required check name remains in place,
but the job is skipped without allocating a runner. Re-enable its code-change
condition when Android release distribution starts. Mobile tests remain an
explicit opt-in graph with `IncludeMobaSmartTests=true`.

The Linux display job also runs the MOBApi process integration tests
(`MobApiProcessTests`, `RuntimeHubLiveE2ETests`) in the `net10.0` target. Each
fixture starts the built MOBApi as an isolated process on a free local port, so
no running server or hardware is needed. `scripts/Test-TestRunResults.ps1`
fails the step when a selected test is skipped or too few tests run. Locally:

```powershell
dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0 --filter "FullyQualifiedName~Moba.Test.Integration.MobApiProcessTests|FullyQualifiedName~Moba.Test.Integration.RuntimeHubLiveE2ETests"
```

Pull requests that change only documentation paths (`docs/`, `plans/`, `specs/`,
`.specify/`, root Markdown files and GitHub guidance) skip the desktop, display,
Android and mutation jobs; `scripts/Get-QualityChangeScope.ps1` owns that list.
GitHub reports those skipped jobs as successful required checks. Pushes to
`main`, manual runs and every other change run all enabled jobs.

The repository consistency job also lints every workflow with a pinned,
checksum-verified actionlint release and checks internal documentation links
and images with `scripts/Test-DocumentationLinks.ps1`. External links are
checked weekly and on demand by `.github/workflows/documentation-links.yml`,
so an unreachable foreign server does not block a pull request. Run the
internal check locally with `./scripts/Test-DocumentationLinks.ps1`; add
`-IncludeExternal` to request external links as well.

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

The mutation lane registry is `MutationTest/mutation-lanes.json`. It fails when
a test fixture is added outside a registered lane. Lanes marked `planned` are
visible but do not yet claim mutation coverage. The `architecture` lane has
no production project: its tests read project files and sources, so code
mutations do not exercise them. The active Domain lane runs in
CI with a 60 percent break threshold:

```powershell
dotnet tool restore
Set-Location MutationTest
dotnet stryker --skip-version-check
```
