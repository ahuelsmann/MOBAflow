# Validation guide: locomotive-to-train usage

**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/306

**Status**: Implementation and P3 corrections have completed local validation on 2026-10-16. Final post-commit CI/Sonar and fresh Claude CLI review evidence is recorded in Draft PR #309 against its actual published head. Manual desktop acceptance remains open.

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

## Draft publication

[Draft PR #309](https://github.com/ahuelsmann/MOBAflow/pull/309) was created against main 22346a7d523de58a6d97284f4afbace10d0c4969 from implementation commit 0a14bdc52f9d36a866b030859e108d1d271bb494 and attached to the implementation chat. The task branch now tracks its own github branch. GitHub CI/Sonar was pending at publication; no readiness or merge claim is made. Independent reviews remain pending; local self-checks do not substitute for those reviews. T028/T031 and T030/T032 therefore remain open.

## PR analysis corrections

The first SonarCloud PR analysis returned a green quality gate but 11 OPEN maintainability findings. This did not satisfy the zero-issue rule. The scoped correction separates target/identity/reference traversal, coverage text and observation/receipt application into focused private methods, documents deliberate cancellation handling, and composes the page's two existing analysis projections as LocomotivePageAnalysis. No suppression, exclusion, baseline or gate change was made. Both complete Release analyzer graphs and unchanged strict baselines passed again. Fresh full Release tests passed: portable 2,827 and Windows 2,940, zero failures, the same one explicit hardware case NotExecuted per suite. All 40 new cases and the real Windows singleton/composition test passed. Evidence: issue306-portable-sonar-fixes.trx and issue306-windows-sonar-fixes.trx. The normal desktop/API/XAML build passed with zero errors/warnings; Android FastDebug passed with zero errors and the same 45 existing warnings. Remote verification for the correction commit is recorded below.

## Verified PR gates (2026-10-16)

Correction commit 7d7b4704e2adc588004d6dfdc6fd8e626e3a9168 was verified against unchanged main 22346a7d523de58a6d97284f4afbace10d0c4969. All started checks completed successfully: Windows desktop build/tests/coverage, Common and Speech.Base mutation lanes, display protocol/firmware checks, all five CodeQL languages plus aggregate CodeQL, instruction consistency and Spec Kit governance. Domain mutation and Android Release AAB were explicitly skipped by their workflow conditions; no corresponding validation is claimed.

SonarCloud Code Analysis on that exact commit succeeded. The separate server query reported quality-gate OK and zero OPEN/CONFIRMED PR issues. All gate conditions passed: reliability/security/maintainability A, new duplication 0 percent against a 3 percent ceiling, reviewed security hotspots 100 percent against a 100 percent floor. The earlier 11 issues were resolved through code changes. [Quality run](https://github.com/ahuelsmann/MOBAflow/actions/runs/38876807446) and [Sonar PR result](https://sonarcloud.io/dashboard?id=ahuelsmann_MOBAflow2&pullRequest=309) provide remote evidence.

T030 is complete. T028/T031 remain open for separately authorized manual UI acceptance. T032 remains partial because independent fixed-comparison team reviews are pending. The PR stays draft; no app/device/hardware start, ready marking or merge occurred. This final evidence-only update does not change the tested product code; its own current-head CI/Sonar is verified separately at handoff.

## Claude P3 correction validation

The valid independent CLI review of base 22346a7d523de58a6d97284f4afbace10d0c4969 through head 4c866c1bd811bdaa549ca23fe00cc358db60b2df reported no P1/P2 and two P3 findings. The user authorized fixing both, testing and a fresh read-only Claude CLI review. The first CLI attempt inspected no files and is invalid; only artifacts/issue306-validation/claude-review-4c866c1b-retry.json is prior review evidence.

Seven new regression cases failed on the original product code: four combinations of inline/queued snapshot dispatch and stale/unchanged receipt outcome, plus three invalid-identity/repair cases in normal Messages with all severity filters off and no explicit usage query. The corrections retain the existing pending receipt-check hint while the first observation is unavailable, revalidate pending hints before publishing any Complete result and avoid a null-observation requeue loop. Typed Coverage metadata keeps Unknown statuses visible independently of severity filtering; neutral uses and actual missing-reference errors retain their normal filters. No new owner, runtime state registry, lifecycle command or persisted data change.

The affected three-fixture run passed 37 tests. Final full Release suites passed 2,834 portable and 2,947 Windows tests, zero failures; each discovers the same one explicit real-display case NotExecuted. All seven new regressions executed. Evidence: issue306-review-p3-red.trx, issue306-review-p3-green.trx, issue306-review-p3-portable-full.trx and issue306-review-p3-windows-full.trx under Test/TestResults. Complete portable/Windows Release analyzer graphs again matched unchanged strict baselines (3,248 diagnostics/1,133 groups and 2,471 diagnostics/947 groups). Normal desktop/API/XAML Debug build passed with zero errors/warnings. Android FastDebug consumer passed with zero errors and 45 existing warnings; no AAB/device claim.

T033/T034 are complete. The published correction head must pass GitHub CI/Sonar with zero OPEN/CONFIRMED issues before its fresh independent Claude CLI review; final results, actual model/turn count, permission-denial audit and reviewed comparison are attached to the PR/handoff without subsequently changing the reviewed commit. The CLI uses a fresh session, safe mode, plan permissions, explicit read/scan/git-inspection tools, no MCP configuration, no delegated agents and no permission bypass. Its JSON output is scanned before reading and the checkout is checked before/after. T028/T031 remain open: manual Light/Dark/keyboard acceptance requires separate launch authorization. The PR stays Draft.

### Explicit filter inputs for remote analysis

The first P3 correction head 1a24ceb1 passed its local gates but remote Sonar reported S2325 for the instance filter reading generated Toolkit properties. The final filter is a pure static predicate receiving the three current generated-property values explicitly. No Toolkit field is read directly, no severity/Unknown behavior changes and no finding is accepted/suppressed. Both complete Release analyzer baselines passed again. Full suites passed again: portable 2,834 and Windows 2,947, zero failures, the same explicit real-display case NotExecuted per suite. Final evidence is issue306-review-p3-filter-portable.trx and issue306-review-p3-filter-windows.trx. Normal desktop/API build passed with zero errors/warnings; Android FastDebug again passed with zero errors and 45 existing warnings. The next published head is the fixed comparison for fresh CI/Sonar and the independent Claude CLI review.
