# Validation guide: locomotive-to-train usage

**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/306

**Status**: Implementation and automated local validation completed on 2026-10-16. Independent review, manual desktop acceptance and current-head PR CI/Sonar remain distinct delivery gates.

## Scope and evidence base

Use the existing `codex/issue-306-locomotive-usage-spec` worktree and `specs/013-locomotive-train-usage/`. The user's implementation/test/Draft-PR authorization supersedes the original planning-only issue wording. Ingenieur is the sole author; #304 remains separate. No extension hooks exist. Spec Kit prerequisites select this feature and its requirements checklist is 17/17 checked.

The original planning base was `6888b0d9`. Implementation first incorporated `fb63e8fd` (#301 typed runtime hub); current main `22346a7d523de58a6d97284f4afbace10d0c4969` (#288 effect isolation) was then merged normally as `e016fede`. The one DI-test insertion conflict retained both independent tests. No runtime query or protocol was added at the imported workflow-effect boundary.

SDK 10.0.402 and Android 36.1.118 / MAUI-Android 10.0.110 / MAUI-Windows 10.0.110 were verified. The first sandbox restore failed NU1301 with access denied; the same narrow elevated dotnet command succeeded. No dependency, warning threshold or analyzer baseline was changed.

## Executed automated checks

All commands run from the feature worktree. Tests use invented in-memory models, real runtime owners/clones over fake transports, and projection-specific fakes. No layout file, application, emulator or hardware was started.

| Check | Actual result |
| --- | --- |
| Three new fixtures: LocomotiveTrainReferenceAnalyzerTests, LocomotiveTrainRuntimeUsageReaderTests, LocomotiveTrainUsageViewModelTests | 40 executed/passed; final affected-suite run 115 executed/passed, zero failures |
| Full portable Release suite | 2,828 discovered, 2,827 executed/passed, zero failures; one explicit hardware test NotExecuted |
| Full Windows Release suite | 2,941 discovered, 2,940 executed/passed, zero failures; same explicit hardware test NotExecuted |
| Windows production DI alias test | UsageReaderAndProjection_ResolveTheExistingDesktopOwnerAndSharedPageInstance passed |
| Portable Release rebuild with MobaAnalyzerGate=true | Zero errors; strict unchanged baseline matches 3,248 diagnostics in 1,133 groups |
| Windows Release test graph with MobaAnalyzerGate=true | Zero errors; strict unchanged baseline matches 2,471 diagnostics in 947 groups |
| Normal desktop Debug build with API dependency/copy and XAML compilation | Passed with zero errors/warnings; final confirmation took 39 seconds |
| Portable MOBApi consumer build | Passed on the final source with zero errors/warnings |
| MOBAsmart restore and FastDebug Android consumer build | Passed on the installed workload; zero errors, 45 existing Android/XAML warnings. No device deployment, AAB publish or Android Release acceptance claim |

The explicit hardware case is `SustainedRefresh_MeetsAcceptanceThresholds`. Its OneTimeSetUp drives the real display and is maintainer-run only. Both TRX files contain a NotExecuted result even though their summary notExecuted counter is zero; the actual result nodes determine the counts above.

Local ignored evidence is in `Test/TestResults/issue306-*.trx` and `artifacts/issue306-validation/`. Final full runs are `issue306-portable-release-final.trx` and `issue306-windows-release-final.trx`; affected cases are `issue306-quality-fixes-final.trx`. New fixtures are stateless internal static NUnit fixtures and all intended cases were discovered. No new analyzer suppression or baseline update was needed.

## Reproducible commands

Restore as required for the selected graph; do not use a portable-only restore immediately before the Windows build.

```powershell
dotnet build Test/Test.csproj -c Release -f net10.0 --no-restore -m:1 -nr:false -t:Rebuild -p:IncludeMobaSmartTests=false -p:MobaAnalyzerGate=true -p:UseSharedCompilation=false
./scripts/Test-AnalyzerBaseline.ps1 -SarifRoot artifacts/analyzers/Release/net10.0 -BaselinePath quality/analyzer-baseline.json
dotnet test Test/Test.csproj -c Release -p:TargetFrameworks=net10.0 -f net10.0 --no-build --no-restore -m:1 -nr:false

dotnet build Test/Test.csproj -c Release -f net10.0-windows10.0.22621.0 --no-restore -m:1 -nr:false -p:IncludeMobaSmartTests=false -p:DebugType=portable -p:DebugSymbols=true -p:MobaAnalyzerGate=true -p:UseSharedCompilation=false
./scripts/Test-AnalyzerBaseline.ps1 -SarifRoot artifacts/analyzers/Release/net10.0-windows10.0.22621.0 -BaselinePath quality/analyzer-baseline.windows.json
dotnet test Test/Test.csproj -c Release -f net10.0-windows10.0.22621.0 -p:IncludeMobaSmartTests=false --no-build --no-restore -m:1 -nr:false

dotnet restore MOBAflow/MOBAflow.csproj
dotnet build MOBAflow/MOBAflow.csproj -c Debug --no-restore -m:1 -nr:false
dotnet restore MOBAsmart/MOBAsmart.csproj
dotnet build MOBAsmart/MOBAsmart.csproj -f net10.0-android -c FastDebug --no-restore -m:1 -nr:false
```

A focused run can filter the three fixture names above; zero discovered tests is never successful validation. The full suites cover runtime/DI boundaries, existing validators, commands, persistence/defaults and existing Delete regressions. Release analyzer warnings are assessed by the complete strict baselines, not by an incremental WinUI SARIF refresh.

## Acceptance coverage

Automated cases cover zero/one/multiple/repeated references, missing/empty targets, equal IDs in other projects/types, unsaved edits, deterministic copied locations, source-specific repair and existence. Real owner tests establish actual active-clone reads for non-selected/disconnected projects, missing/wrong-context owners, consistent capture, update/replacement/disposal races, request validation, cancellation and receipt identity. Projection tests cover in-place reload, late replies, snapshot coalescing, Information filtering and zero query-triggered saves/model/lifecycle/workflow/Z21 operations.

Successful A evidence has an observation time and is revalidated before display. Unavailable evidence uses an explicit reason and check time, without borrowing the editor name as a runtime observation. B (retained definitions) and C (operative use) always remain Unknown; zero A references is no global non-use or deletion permission.

## Remaining manual and publication gates

T028 requires separate explicit app-launch authorization: inspect existing Messages in Light/Dark, keyboard access, source/coverage visibility with Information rows filtered, target changes and exact train positions. Use invented data and no hardware. No such launch has been authorized or performed. Source/XAML review and compilation do not replace this acceptance check.

The explicitly authorized Draft PR must link #306 and report these limits. Before publication, scan changed files, normalize/check line endings, review the diff and run internal links plus Spec Kit governance. Obtain independent reviews on a fixed comparison after team messaging is directly authorized. Code analysis runs only through GitHub PR CI; a current-head green SonarCloud check and zero OPEN/CONFIRMED issues are required before any readiness claim. Keep the PR draft; ready/merge is outside this task.

## Final local gates

Changed-file secrets and line endings passed for all 39 task files. Internal documentation links passed (492 links), Spec Kit PullRequest governance passed and git diff whitespace checks passed. No analyzer-baseline, dependency, configuration or schema file changed. Convergence appended T031/T032 for the two remaining acceptance/publication gaps; no extra product-code task was found.
