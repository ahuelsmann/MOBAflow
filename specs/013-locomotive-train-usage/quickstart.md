# Validation guide: locomotive-to-train usage

**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/306

**Status**: Implementation in progress. Executed results below supersede the original future-check headings; pending platform/manual/publication gates are not reported as passed.

## Implementation entry (2026-10-16)

Explicit green handoff received. No extension hooks exist. Spec Kit prerequisites select this feature; requirements checklist is 17/17 checked, zero unchecked. Nine original planning files are preserved. Task branch is based on live main `fb63e8fde5f6aa299a3f3478560e7277f8661a90`.

SDK 10.0.402 is installed. Installed workloads include android 36.1.118, maui-android 10.0.110 and maui-windows 10.0.110 (workload inventory required read-only elevation for the Windows Service Control Manager). Existing fixture names verified: ProjectDiagnosticsServiceTests, ProjectValidatorTests, ProjectRuntimeHostTests, MobaRuntimeServiceProjectIsolationTests, LocomotivesPageViewModelTests, MainWindowDiagnosticsTests and Backend/Windows registration tests. The three new fixtures remain implementation tasks and have not run. Test.csproj contains portable and Windows targets; MOBAsmart participation is opt-in. All planned app builds are compile checks, never launch permission.

## Executed implementation checks (before current-main resynchronization)

- Initial sandbox NuGet restore failed NU1301 with path access denied. The same narrow dotnet-test command with approved elevation restored successfully; no package, warning or analyzer gate was lowered.
- Analyzer and initial editor-query cases compiled only after their planned implementations were added. The first integrated US1 run passed 21 tests (0 failures/skips), including existing locomotive-page regressions and the new read-only Show usage command.
- Missing-reference and diagnostics/repair cases were run red before their corresponding behavior was added, then the combined analyzer/editor/page/diagnostics run passed 35 tests.
- The actual owner reader and runtime integration run passed 56 tests (0 failures/skips): new reader fixture plus existing ProjectRuntimeHostTests and MobaRuntimeServiceProjectIsolationTests. This uses real owners/clones with fake Z21, not only fabricated observations. An initial replacement classification mismatch was corrected in production membership-check ordering; no test was weakened.
- Three new read-path fixtures passed 31 tests together. Messages/usage and common-DI integration subsequently passed 28 tests (0 failures/skips), covering source replacement, Information filters, stale pre-apply answers, raw snapshot revalidation/coalescing, real owner replacement/disposal before apply and required common analyzer resolution without a desktop reader.
- All runs use net10.0 with TargetFrameworks=net10.0, serial MSBuild (-m:1 -nr:false); TRX files under Test/TestResults/issue306-*.trx are local validation artifacts. The Windows alias identity test exists but has not executed yet. Desktop XAML and final broad suites remain pending.
- Show usage is exposed by the existing locomotive header and context menu, opens existing Messages and does not navigate to a new page. The coverage summary is outside the severity-filtered list. Light/Dark/keyboard inspection requires separate launch permission and remains open.

## Planning checks now

From the dedicated feature worktree, scan the feature files and validation scripts before reads, then run:

```powershell
$env:SPECIFY_FEATURE_DIRECTORY='specs/013-locomotive-train-usage'
./.specify/scripts/powershell/check-prerequisites.ps1 -Json -RequireSpec -RequireTasks -IncludeTasks
./scripts/Test-LineEndings.ps1 -Path specs/013-locomotive-train-usage
$featureFiles=Get-ChildItem specs/013-locomotive-train-usage -Recurse -File | ForEach-Object { [IO.Path]::GetRelativePath((Get-Location).Path,$_.FullName) }
./scripts/Test-SpecKitGovernance.ps1 -Mode PullRequest -ChangedFiles $featureFiles
git diff --check
git status --short
```

An untracked file is not included in plain git diff. Review the explicit feature-file list and scan/check those files; do not stage only to make them visible. Local internal Markdown links must resolve, including heading fragments.

## Future product prerequisites

The user conditionally authorized product implementation on 2026-10-16. The assigned engineer starts only after all planning gates pass and the explicit writing-ownership handoff, using approved Spec Kit artifacts and matching SDK/workloads; the real new fixtures must exist before their commands are run. Use fake owners/transports and invented in-memory Domain objects; do not create a real sample solution.json or connect a layout.

## Future acceptance matrix

| Case | Expected |
| --- | --- |
| Existing target has zero/one/many/repeated train entries | Exact source/project/type/position list; zero is source-qualified |
| Missing/empty locomotive target | Missing target/entry error, never existing-unused success |
| Same GUID in another project or wagon type | No false match |
| Unsaved edited reference | Edited source updated without save/reload |
| Editor-only and runtime-only references | Both independent source results retained |
| Non-selected/disconnected existing owner | Actual loaded clone still queried |
| A Complete with zero occurrences | B/C Unknown; aggregate never Complete or delete permission |
| Owner absent/unsupported remote/query failure | A Unknown and concrete reason, editor result preserved |
| Duplicate/empty train identity | Invalid/unknown precise scope, no fabricated occurrence ID |
| Definition changes during capture | One immutable version; stale receipt rejected before apply |
| Query succeeds, then same-ID owner replacement or host disposal | Old response/revalidation not applied |
| In-place reload into same Solution object | Old context cancelled/results removed |
| Target changes during pending validation | Old target response cannot replace current target |
| Repeated refresh and repaired reference | No accumulation; source-specific repair removes old finding |
| Information filter hides neutral rows | Coverage summary still states unknown runtime categories |
| Analysis invocation | Zero saves, model changes, commands, Delete/Reset, RequestFactory/workflow calls or lifecycle operations |

## Future automated commands

After the intended new fixtures actually exist, first run:

```powershell
dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0 --filter 'FullyQualifiedName~LocomotiveTrainReferenceAnalyzerTests|FullyQualifiedName~LocomotiveTrainRuntimeUsageReaderTests|FullyQualifiedName~LocomotiveTrainUsageViewModelTests'
dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0
dotnet build MOBApi/MOBApi.csproj
```

The first run must discover and execute the intended tests; zero tests is a failure of verification. Broaden because runtime/DI boundaries change. Use AGENTS.md Windows restore/FastDebug/Test commands for desktop; Android compile on a workload-equipped host where shared registration changes require it. Report actual failures/skips rather than treating environment limitations as passes. If workload resolution fails, diagnose it explicitly; no workaround proves Android/Windows validation.

## Future manual and publication gates

Only after explicit app-launch authorization: inspect existing Messages with invented data in Light/Dark, keyboard use, selected-target changes and visible A/B/C coverage with Information rows filtered. No hardware operation is necessary. Check that Delete remains unchanged by this slice.

Before any later publication: changed-file secrets scans, line endings, diff, links and governance. Draft PR and current-head GitHub CI/SonarCloud with zero OPEN/CONFIRMED issues; no local Sonar code analysis. This planning task creates no commit, push, PR or product validation claim.
