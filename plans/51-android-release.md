# Issue 51: Reproducible Android Release build

## Status and traceability

- Status: In progress
- Primary issue: [#51](https://github.com/ahuelsmann/MOBAflow/issues/51)
- Parent programme: [#47](https://github.com/ahuelsmann/MOBAflow/issues/47)
- Delivery branch: `codex/issue-51-android-release`
- Scope boundary: RF-05 only; RF-06 analyzer enforcement and RF-08 compiled MAUI bindings are excluded.

## Outcome

Produce the same unsigned MOBAsmart Android Release AAB from a clean checkout locally and in a mandatory pull-request CI job while preserving the existing WinUI x64 build and cross-platform deliverables.

## Analysis

On Windows, `Directory.Build.props` currently maps an empty or `AnyCPU` `$(Platform)` to `x64` based only on `$(OS)`. MSBuild imports this property before project-local properties, so MOBAsmart evaluates as `Platform=x64` even though its project declares `PlatformTarget=AnyCPU`. Android restore and build can then select an x64 platform graph that does not match `net10.0-android` assets.

The .NET 10 MAUI targets currently infer `RuntimeIdentifiers=android-arm64;android-x64` for Release. Relying on that implicit SDK default makes the supported ABI contract vulnerable to workload changes. MOBAsmart should state the intended 64-bit device and emulator RIDs explicitly for Release and continue using `android-arm64` only for Debug/FastDebug iteration.

Repository documentation incorrectly uses `dotnet restore -f net10.0-android`. For `dotnet restore`, `-f` is `--force`; framework selection belongs on `dotnet build` or `dotnet publish`. MOBAsmart is single-targeted, so its clean restore command needs no framework selector.

## Implementation

1. Change the shared `Platform=x64` condition to test the target platform identifier and apply only to Windows target frameworks. Keep `PlatformTarget=x64` for Windows TFMs and allow Android and platform-neutral TFMs to retain `AnyCPU`.
2. In `MOBAsmart.csproj`, explicitly set Release `RuntimeIdentifiers` to `android-arm64;android-x64`, retain `PlatformTarget=AnyCPU`, and request AAB-only Release packaging.
3. Add a PowerShell validation script that opens the generated AAB as a ZIP, verifies the required base manifest, resources and DEX entries, and requires exactly the `arm64-v8a` and `x86_64` native ABI directories.
4. Correct Android restore/build commands in `AGENTS.md`, `docs/BUILD-PERFORMANCE.md`, and `docs/PROJECT-REFERENCE.md`. Document the MAUI workload prerequisite, clean restore, Release publish, expected AAB path, and validation command.
5. Add a mandatory `android-release` job to `.github/workflows/quality.yml`: set up the pinned SDK, cache the NuGet package directory with keys derived from package/project inputs, restore the MAUI Android workload, perform an explicit clean NuGet restore, publish Release with `--no-restore`, validate the AAB, and upload it as a retained artifact.

## Validation matrix

| Surface | Command or assertion | Expected result |
| --- | --- | --- |
| Property precedence | `dotnet msbuild MOBAsmart/MOBAsmart.csproj -p:Configuration=Release -getProperty:Platform,PlatformTarget,RuntimeIdentifiers,AndroidPackageFormats` | `AnyCPU`, `AnyCPU`, `android-arm64;android-x64`, `aab` |
| Android workload | `dotnet workload restore MOBAsmart/MOBAsmart.csproj --skip-manifest-update` | Required MAUI Android workload is available from the pinned SDK manifests |
| Clean Android restore | `dotnet restore MOBAsmart/MOBAsmart.csproj --property:Configuration=Release --force-evaluate` | Both Release RID dependency graphs restore without platform mismatch |
| Release AAB | `dotnet publish MOBAsmart/MOBAsmart.csproj --framework net10.0-android --configuration Release --no-restore -m:1` | A Release AAB is produced without parallel multi-RID output collisions |
| AAB contents | `./scripts/Test-AndroidAppBundle.ps1 -BundlePath <bundle.aab>` | Required base entries exist; ABIs are exactly `arm64-v8a` and `x86_64` |
| WinUI | Existing Windows Release/FastDebug restore and build commands | `Platform=x64`, `PlatformTarget=x64`, output paths unchanged |
| Cross-platform | Existing MOBApi build and `Test/Test.csproj` test commands | AnyCPU deliverables build and test behavior remains unchanged |
| CI | Required Quality workflow matrix | Desktop, mutation, and Android Release jobs are green |

The local Android publish is required when the MAUI Android workload is available. GitHub Actions remains the clean hosted-runner evidence for the complete Android lane. No analyzer-baseline or XAML-binding cleanup is permitted to make the lane green; such failures remain RF-06 or RF-08 work.

## CI caching

- Cache only the repository-local NuGet package directory, never `bin`, `obj`, SDK workloads, generated AABs, or signing material.
- Key the cache by runner OS plus hashes of `global.json`, `NuGet.Config`, `Directory.Packages.props`, `Directory.Build.props`, and all project files in the Android dependency graph.
- Treat a cache miss as normal and always run explicit restore so the lane proves reproducibility rather than reuse of build outputs.

## Risks and mitigations

- **MSBuild import precedence:** command-line and project-local properties can override shared defaults. Validate evaluated properties directly for Android, WinUI, and a platform-neutral project.
- **Hosted workload drift:** pin the SDK through `global.json`, restore the workload from the project, and do not cache workload installations.
- **Parallel multi-RID output collisions:** publish the shared project graph on one MSBuild node while Android packages both Release RIDs.
- **False-positive AAB success:** fail if the bundle is missing, ambiguous, structurally incomplete, or contains an unexpected ABI.
- **Unsigned CI artifact:** CI validates build reproducibility and bundle structure only. Production signing remains outside this issue and no keystore or password is introduced.
- **Scope creep:** do not change analyzer policy, MAUI XAML bindings, UI behavior, or release signing.

## Rollback

Revert the RF-05 commit as one unit. This restores the prior Windows-wide platform mapping, implicit Android SDK RID selection, existing documentation, and CI job set without data migration or runtime compatibility impact. If only the hosted Android job is unstable, disable that job in a dedicated follow-up revert rather than weakening AAB validation or changing product code.

## Completion

Before the pull request becomes ready, the relevant GitHub Actions matrix must be green and the PR must link #51 with validation evidence. After #51 is merged and closed, delete this standalone plan in accordance with the repository plan lifecycle policy.
