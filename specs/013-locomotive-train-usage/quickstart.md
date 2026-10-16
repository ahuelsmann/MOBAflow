# Validation guide: locomotive-to-train usage

**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/306

**Status**: Implementation, both code P3 corrections and the authorized focused desktop acceptance completed on 2026-10-16. The two subsequent documentation P3 findings are corrected here. Product evidence is fixed at 8e40f8a5; current-head CI/Sonar and the final independent Claude CLI review of this documentation closure are recorded in Draft PR #309 and the handoff. The PR remains Draft for Issue Master integration.

## Scope and evidence base

Use the existing `codex/issue-306-locomotive-usage-spec` worktree and `specs/013-locomotive-train-usage/`. The user's implementation/test/Draft-PR authorization supersedes the original planning-only issue wording. Ingenieur is the sole author; #304 remains separate. No extension hooks exist. Spec Kit prerequisites select this feature and its requirements checklist is 17/17 checked.

The original planning base was `6888b0d9`. Implementation first incorporated `fb63e8fd` (#301 typed runtime hub); current main `22346a7d523de58a6d97284f4afbace10d0c4969` (#288 effect isolation) was then merged normally as `e016fede`. The one DI-test insertion conflict retained both independent tests. No runtime query or protocol was added at the imported workflow-effect boundary.

SDK 10.0.402 and Android 36.1.118 / MAUI-Android 10.0.110 / MAUI-Windows 10.0.110 were verified. The first sandbox restore failed NU1301 with access denied; the same narrow elevated dotnet command succeeded. No dependency, warning threshold or analyzer baseline was changed.

## Initial automated delivery at 4c866c1b

All commands ran from the feature worktree. Tests used invented in-memory models, real runtime owners/clones over fake transports, and projection-specific fakes. No layout file, application, emulator or hardware was started during this initial automated phase. The later separately authorized desktop acceptance is recorded below.

| Check | Actual result |
| --- | --- |
| Initial new-fixture trio: LocomotiveTrainReferenceAnalyzerTests, LocomotiveTrainRuntimeUsageReaderTests, LocomotiveTrainUsageViewModelTests | Historical 4c866c1b result: 40 executed/passed; initial affected-suite run 115 executed/passed, zero failures |
| Full portable Release suite | 2,828 discovered, 2,827 executed/passed, zero failures; one explicit hardware test NotExecuted |
| Full Windows Release suite | 2,941 discovered, 2,940 executed/passed, zero failures; same explicit hardware test NotExecuted |
| Windows production DI alias test | UsageReaderAndProjection_ResolveTheExistingDesktopOwnerAndSharedPageInstance passed |
| Portable Release rebuild with MobaAnalyzerGate=true | Zero errors; strict unchanged baseline matches 3,248 diagnostics in 1,133 groups |
| Windows Release test graph with MobaAnalyzerGate=true | Zero errors; strict unchanged baseline matches 2,471 diagnostics in 947 groups |
| Normal desktop Debug build with API dependency/copy and XAML compilation | Passed with zero errors/warnings; final confirmation took 39 seconds |
| Portable MOBApi consumer build | Passed on the final source with zero errors/warnings |
| MOBAsmart restore and FastDebug Android consumer build | Passed on the installed workload; zero errors, 45 existing Android/XAML warnings. No device deployment, AAB publish or Android Release acceptance claim |

The explicit hardware case is `SustainedRefresh_MeetsAcceptanceThresholds`. Its OneTimeSetUp drives the real display and is maintainer-run only. Both TRX files contain a NotExecuted result even though their summary notExecuted counter is zero; the actual result nodes determine the counts above.

Local ignored evidence is in `Test/TestResults/issue306-*.trx` and `artifacts/issue306-validation/`. Initial full runs are `issue306-portable-release-final.trx` and `issue306-windows-release-final.trx`; initial affected cases are `issue306-quality-fixes-final.trx`. New fixtures are stateless internal static NUnit fixtures and all intended cases were discovered. No new analyzer suppression or baseline update was needed.

At product head 8e40f8a5, the same new-fixture trio contains 44 cases: analyzer 9, runtime reader 15 and usage ViewModel 20. The later P3-focused trio is a different selection: usage ViewModel 20, MainWindowDiagnosticsTests 11 and ProjectDiagnosticsServiceTests 6, totaling 37. The final full suites supersede the initial counts above: 2,834 portable and 2,947 Windows tests passed; see `issue306-review-p3-filter-portable.trx` and `issue306-review-p3-filter-windows.trx`.

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

## Original manual and publication gates

At the original delivery checkpoint, T028 required separate explicit app-launch authorization: inspect existing Messages in Light/Dark, keyboard access, source/coverage visibility with Information rows filtered, target changes and exact train positions. No such launch had then been authorized or performed. The subsequent direct authorization and actual acceptance below close T028/T031 for that focused scope; source/XAML review and compilation alone did not close them.

The explicitly authorized Draft PR must link #306 and report these limits. Before publication, scan changed files, normalize/check line endings, review the diff and run internal links plus Spec Kit governance. Obtain independent reviews on a fixed comparison after team messaging is directly authorized. Code analysis runs only through GitHub PR CI; a current-head green SonarCloud check and zero OPEN/CONFIRMED issues are required before any readiness claim. Keep the PR draft; ready/merge is outside this task.

## Final local gates

Changed-file secrets and line endings passed for all 39 task files. Internal documentation links passed (492 links), Spec Kit PullRequest governance passed and git diff whitespace checks passed. No analyzer-baseline, dependency, configuration or schema file changed. Convergence appended T031/T032 for the two remaining acceptance/publication gaps; no extra product-code task was found.

## Initial draft publication

[Draft PR #309](https://github.com/ahuelsmann/MOBAflow/pull/309) was created against main 22346a7d523de58a6d97284f4afbace10d0c4969 from implementation commit 0a14bdc52f9d36a866b030859e108d1d271bb494 and attached to the implementation chat. The task branch now tracks its own github branch. GitHub CI/Sonar was pending at publication; no readiness or merge claim is made. Independent reviews remain pending; local self-checks do not substitute for those reviews. T028/T031 and T030/T032 therefore remain open.

## PR analysis corrections

The first SonarCloud PR analysis returned a green quality gate but 11 OPEN maintainability findings. This did not satisfy the zero-issue rule. The scoped correction separates target/identity/reference traversal, coverage text and observation/receipt application into focused private methods, documents deliberate cancellation handling, and composes the page's two existing analysis projections as LocomotivePageAnalysis. No suppression, exclusion, baseline or gate change was made. Both complete Release analyzer graphs and unchanged strict baselines passed again. Fresh full Release tests passed: portable 2,827 and Windows 2,940, zero failures, the same one explicit hardware case NotExecuted per suite. All 40 new cases and the real Windows singleton/composition test passed. Evidence: issue306-portable-sonar-fixes.trx and issue306-windows-sonar-fixes.trx. The normal desktop/API/XAML build passed with zero errors/warnings; Android FastDebug passed with zero errors and the same 45 existing warnings. Remote verification for the correction commit is recorded below.

## Verified PR gates (2026-10-16)

Correction commit 7d7b4704e2adc588004d6dfdc6fd8e626e3a9168 was verified against unchanged main 22346a7d523de58a6d97284f4afbace10d0c4969. All started checks completed successfully: Windows desktop build/tests/coverage, Common and Speech.Base mutation lanes, display protocol/firmware checks, all five CodeQL languages plus aggregate CodeQL, instruction consistency and Spec Kit governance. Domain mutation and Android Release AAB were explicitly skipped by their workflow conditions; no corresponding validation is claimed.

SonarCloud Code Analysis on that exact commit succeeded. The separate server query reported quality-gate OK and zero OPEN/CONFIRMED PR issues. All gate conditions passed: reliability/security/maintainability A, new duplication 0 percent against a 3 percent ceiling, reviewed security hotspots 100 percent against a 100 percent floor. The earlier 11 issues were resolved through code changes. [Quality run](https://github.com/ahuelsmann/MOBAflow/actions/runs/38876807446) and [Sonar PR result](https://sonarcloud.io/dashboard?id=ahuelsmann_MOBAflow2&pullRequest=309) provide remote evidence.

At this earlier verification checkpoint, T030 was complete, T028/T031 remained open for separately authorized manual UI acceptance and T032 remained partial pending independent reviews. The PR stayed draft; no app/device/hardware start, ready marking or merge had occurred. Later review and desktop acceptance evidence follows below.

## Claude P3 correction validation

The valid independent CLI review of base 22346a7d523de58a6d97284f4afbace10d0c4969 through head 4c866c1bd811bdaa549ca23fe00cc358db60b2df reported no P1/P2 and two P3 findings. The user authorized fixing both, testing and a fresh read-only Claude CLI review. The first CLI attempt inspected no files and is invalid; only artifacts/issue306-validation/claude-review-4c866c1b-retry.json is prior review evidence.

Seven new regression cases failed on the original product code: four combinations of inline/queued snapshot dispatch and stale/unchanged receipt outcome, plus three invalid-identity/repair cases in normal Messages with all severity filters off and no explicit usage query. The corrections retain the existing pending receipt-check hint while the first observation is unavailable, revalidate pending hints before publishing any Complete result and avoid a null-observation requeue loop. Typed Coverage metadata keeps Unknown statuses visible independently of severity filtering; neutral uses and actual missing-reference errors retain their normal filters. No new owner, runtime state registry, lifecycle command or persisted data change.

The P3-focused trio passed 37 tests: LocomotiveTrainUsageViewModelTests (20), MainWindowDiagnosticsTests (11) and ProjectDiagnosticsServiceTests (6). Full Release suites passed 2,834 portable and 2,947 Windows tests, zero failures; each discovers the same one explicit real-display case NotExecuted. All seven new regressions executed. Evidence: issue306-review-p3-red.trx, issue306-review-p3-green.trx, issue306-review-p3-portable-full.trx and issue306-review-p3-windows-full.trx under Test/TestResults. Complete portable/Windows Release analyzer graphs again matched unchanged strict baselines (3,248 diagnostics/1,133 groups and 2,471 diagnostics/947 groups). Normal desktop/API/XAML Debug build passed with zero errors/warnings. Android FastDebug consumer passed with zero errors and 45 existing warnings; no AAB/device claim.

T033/T034 are complete. The published correction head must pass GitHub CI/Sonar with zero OPEN/CONFIRMED issues before its fresh independent Claude CLI review; final results, actual model/turn count, permission-denial audit and reviewed comparison are attached to the PR/handoff without subsequently changing the reviewed commit. The CLI uses a fresh session, safe mode, plan permissions, explicit read/scan/git-inspection tools, no MCP configuration, no delegated agents and no permission bypass. Its JSON output is scanned before reading and the checkout is checked before/after. T028/T031 were still open at that correction checkpoint and are closed by the later desktop evidence below. The PR stays Draft.

### Explicit filter inputs for remote analysis

The first P3 correction head 1a24ceb1 passed its local gates but remote Sonar reported S2325 for the instance filter reading generated Toolkit properties. The final filter is a pure static predicate receiving the three current generated-property values explicitly. No Toolkit field is read directly, no severity/Unknown behavior changes and no finding is accepted/suppressed. Both complete Release analyzer baselines passed again. Full suites passed again: portable 2,834 and Windows 2,947, zero failures, the same explicit real-display case NotExecuted per suite. Final evidence is issue306-review-p3-filter-portable.trx and issue306-review-p3-filter-windows.trx. Normal desktop/API build passed with zero errors/warnings; Android FastDebug again passed with zero errors and 45 existing warnings. The next published head is the fixed comparison for fresh CI/Sonar and the independent Claude CLI review.

## Independent CLI reviews at product head 8e40f8a5

Against unchanged base 22346a7d523de58a6d97284f4afbace10d0c4969, all started GitHub checks completed successfully; the current-head SonarCloud check succeeded, the server quality gate was OK and OPEN/CONFIRMED PR issues totaled zero. The subsequent independent direct Claude CLI code review used actual model claude-opus-5-5 for 48 turns: success, no permission denials, no delegated agents and no actionable P1/P2/P3 findings. Both original P3 corrections were confirmed. Its scope included changed product code, affected tests and the specification/contracts; it did not fully read plan/tasks/supporting documentation.

A separate fresh CLI supplement read the full plan/tasks/research/quickstart/checklist and changed documentation, with targeted code cross-checks: actual model claude-opus-5-5, 24 turns, success, no permission denials or delegated agents. It found two documentation P3s, corrected in this closure: the checklist's planning-only assertion is now explicitly historical, and the historical 40-case new-fixture trio is distinguished from the current 44-case trio and the different 37-case P3-focused trio. Neither review ran builds, tests, UI, hardware or repository writes. Reports: artifacts/issue306-validation/claude-review-p3-8e40f8a5.json and claude-review-p3-8e40f8a5-docs.json.

Static review does not establish continuous editor freshness when a legacy mutation fails to publish ModelChanged, or general runtime timing behavior. The existing owner lock wait remains cancellable; no deadlock was established. Unavailable owner/context evidence remains Unknown. The final documentation-only commit requires its own current-head CI/Sonar and fresh independent CLI review of the actual PR comparison; these changing publication results belong in the PR/handoff without another evidence-only source commit.

## Authorized focused desktop acceptance (2026-10-16)

The user explicitly authorized a targeted MOBAflow start for Light/Dark and keyboard acceptance, without hardware actions. UX Navigator drove exactly one isolated Release process on product head 8e40f8a568af01025ae6cd1b75cdf24acdc01687, base 22346a7d523de58a6d97284f4afbace10d0c4969, on Windows 10.0.26200.0. The displayed version was 0.2.1-alpha.0.231; binary ProductVersion includes +8e40f8a568af01025ae6cd1b75cdf24acdc01687. EXE SHA256: 34F5D6DD2867FE3A9FDA15887940B9A4FFBF071F4184A177720D64AD9D32E5FF; MOBAflow.dll SHA256: 156031A22B86140852A0A6947C0B51B6AFD227E0DAA7191215A38DA4ADBF63BB. This closure changes documentation only; it does not relabel the tested binary as a later documentation SHA.

The separate deployment under artifacts/issue306-validation/ui-offline-8e40f8a5 used invented usage-acceptance.json and freshly authored settings, blank Z21/display endpoints, no journeys/workflows, REST autostart disabled and an isolated local API port. DEBUG was not defined, so development settings/UserSecrets were not loaded. Only the child process used isolated APPDATA/LOCALAPPDATA and had AZURE_APPCONFIG_CONNECTION removed. The UI confirmed No Z21 assigned and REST API: Not started. No real layout or Issue #35 reference solution was used.

| Actual action/state | Observed result |
| --- | --- |
| Light and Dark, maximized 1440x851 and windowed 1069x615 | Coverage, sources, positions, filters and scrollable results readable; no blocking layout defect in these states |
| Shift+Tab / Return on Show usage; Space on Information; Tab / Return on Refresh | Controls reachable and operable with visible focus; Show usage has the accessible name Show usage for the selected locomotive |
| Harbor pilot usage and a broken Morning consist entry | Three exact occurrences per edited/A source: Evening position 1 and Morning positions 1/2; separate missing-target Error at Morning position 3, vehicles[2].vehicleId |
| All severity filters disabled | Explicit A/B/C coverage remains visible; B/C remain Unknown. In the invalid-identity project, normal Unknown (InvalidIdentity) remains visible without invoking Show usage |
| Harbor to Spare engine; project switch; repeated query | Old usage cleared; Spare zero limited to checked edited/A definitions; B/C Unknown; project-specific diagnostics replaced; repeated Harbor query has six information rows, no accumulation, a new observation time |

The process started at 2026-10-16T10:42:57.9849934Z (12:42 MESZ), PID 44592 / window 12454722, and was closed normally with Alt+F4 at about 12:49 MESZ. UX verified that PID and all MOBAflow windows were gone. No hardware actions or unrelated process termination occurred. Local UX evidence is acceptance-report.md plus 18 PNG/UIA pairs in C:/Users/andre/.codex/visualizations/2026/10/15/01a140e7-b851-7503-9ea9-91aa61e22526/pr309-desktop; these are local artifacts, not published repository files.

Two observations are explicitly bounded. Normal Messages Refresh retains Usage's observation time because RefreshProjectDiagnostics recomputes normal diagnostics and projects existing usage evidence; a Current receipt check retains that observation, while Show usage captures a new one. Also, the fixture was not byte-identical after exit: its manifest SHA256 6F4ACD995ECC90181DB2FF97739D9EE79B0FB265AAADD65C13C6220A65F7C28A became DA7A912E6EA62D346B223AC015FC8D9A6E39D7BDD1A25D873F80374A9CD9DC73, with empty trackPlan defaults added to both projects. The unchanged TrackPlanSolutionBinder.OnSolutionSaving calls PushPlanToSelectedProject before normal saves and can populate that empty document. This is a source-based explanation, not a causal UI trace. No no-write or byte-identity claim is made; the UI run did not isolate usage queries from existing host autosave/startup/shutdown behavior.

T028/T031 are complete for the recorded focused scope. No manual reload/receipt-race, model editing, screen-reader, 200-percent system-font/DPI, Android/TalkBack or hardware acceptance was performed. Those broader checks are not implied by this result. The PR stays Draft; final synchronization/readiness/merge belongs to Issue Master after the current comparison's gates and final review.
