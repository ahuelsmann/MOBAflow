# Issue #378 validation and practical acceptance preparation

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/378

## Current execution boundary: theme correction

The Senior Developer prepares the new candidate only. No MOBAflow, API host, debugger, workflow,
device or hardware may be started by this source/build phase. Root owns any separately authorized
normal start and the human acceptance. Preserve the existing JADE f5 program/configuration/data;
never copy a new build over that package. A separate solution file alone does not isolate startup.
The operator-owned MOBAflow/solution.json is excluded from every operation.

## Historical quality-phase evidence: d6aa332c -> f5eba239

The first validation build contains the bounded corrections on top of d6aa332c. Its assembly
file version is 0.2.1.0 and product version is
`0.2.1-alpha.0.263+d6aa332c10036172965845987143084fa881e66d`.
This pre-publication version is not the final acceptance build identity. The final source commit,
fresh build version and exact-head remote CI/Sonar receipts must accompany the Chief PM handoff.

Historical source-build location (separate from the isolated JADE program):
`C:/Repo/ahuelsmann/MOBAflow/artifacts/worktrees/issue-378-graphical-workflow-editor/MOBAflow/bin/x64/Release/MOBAflow.exe`.

- Before refactoring: WorkflowViewModelTests with the 14 additional boundary cases, 32 passed.
- After refactoring: portable workflow/library/defaults selection, 106 passed, zero failed/skipped.
- Windows workflow/library/defaults/XAML icon selection: 108 passed, zero failed/skipped.
- Release WinUI compilation: zero errors. Portable analyzer gate: 3211 diagnostics / 1125 groups,
  unchanged baseline. Windows complete-rebuild analyzer gate: 2456 diagnostics / 946 groups,
  unchanged baseline, zero build errors.
- Initial incremental Windows SARIF was incomplete; it is not evidence for removing baseline
  entries. A complete CI-shaped rebuild with DebugSymbols=true replaces that observation.
- Logs: `artifacts/issue378-validation/`; TRX: `Test/TestResults/issue378-before.trx`,
  `issue378-after-portable.trx` and `issue378-after-windows.trx` (local ignored artifacts).

Commands run from the task worktree:

```powershell
dotnet test Test/Test.csproj -c Release -p:TargetFrameworks=net10.0 -f net10.0 --no-restore --disable-build-servers -m:1 --filter "FullyQualifiedName~WorkflowViewModelTests|FullyQualifiedName~WorkflowLibraryViewModelTests|FullyQualifiedName~AppSettingsDefaultsTests"
dotnet build Test/Test.csproj -c Release -f net10.0 -p:TargetFrameworks=net10.0 -p:MobaAnalyzerGate=true --no-restore --disable-build-servers -m:1
./scripts/Test-AnalyzerBaseline.ps1 -SarifRoot artifacts/analyzers/Release/net10.0 -BaselinePath quality/analyzer-baseline.json
```

Windows commands use `-f net10.0-windows10.0.22621.0`,
`-p:IncludeMobaSmartTests=false -p:BuildMOBApiDependency=false -p:CopyMOBApiToOutput=false
-p:ValidateJsonConfiguration=false`, and
`-p:CustomAfterMicrosoftCommonTargets=C:/Repo/ahuelsmann/MOBAflow/artifacts/worktrees/issue-378-graphical-workflow-editor/artifacts/issue378-validation/ExcludeProtectedSolution.targets`.
The temporary target removes only MOBAflow's `solution.json` Content/None items before
AssignTargetPaths, so the protected input is neither read nor copied. It is not a shipped change.
The Windows test filter also includes `XamlIconMarkupTests`. The complete Windows analyzer build
uses `-t:Rebuild -p:MobaAnalyzerGate=true -p:DebugType=portable -p:DebugSymbols=true
-p:UseSharedCompilation=false`; check `quality/analyzer-baseline.windows.json` afterwards.

## Startup contracts and new-candidate prerequisites

Source inspection establishes these contracts:

- WinUiAppStartupService.Launch ignores launch arguments. No existing `--offline`, `--solution`
  or alternate-config command-line switch was verified; do not invent one.
- Release WinUiConfigurationBuilder and SettingsService read/write appsettings.json under
  AppContext.BaseDirectory. Release does not load DEBUG user secrets. An inherited
  AZURE_APPCONFIG_CONNECTION can still add Azure configuration; never read/copy its value and
  require its absence from any separately authorized test process environment.
- WinUiSerilogConfigurator writes logs below that same program directory. A disposable working
  directory alone does not relocate configuration or logs.
- Application.AutoLoadLastSolution=false and LastSolutionPath="" prevent the autoload branch.
  Application.AutoStartWebApp=false prevents PostStartupInitializationService's API-start call.
- The host nevertheless calls ISolutionSession.StartRuntimesAsync. ProjectZ21EndpointSource
  returns Unavailable for an empty project Z21.IpAddress and does not discover another device.
  Loading production data, assigning an endpoint, or reusing a connected runtime invalidates this
  limited evidence. RestApiProcessService can reuse an already reachable API and start discovery
  when invoked, so no API-related button may be used.

New-head candidate root (create only for the actual committed head):
`C:/Repo/ahuelsmann/MOBAflow/artifacts/worktrees/issue-378-graphical-workflow-editor/artifacts/issue378-acceptance/<final-head>/`.
Use separate `program/`, `data/`, `photos/` and `evidence/` directories. The intended executable is
`program/MOBAflow.exe`, fresh non-secret configuration is `program/appsettings.json`, and the only
test solution is `data/workflow-editor-test.json`. A final-head binary/asset allowlist and current
counter/cache/master-data path qualification are required before launch. Do not
copy the entire build directory: it can contain pre-existing operator data. No real settings,
secrets or protected solution are copied by this phase.

Fresh candidate settings must explicitly set Application.AutoLoadLastSolution=false,
Application.LastSolutionPath="", Application.AutoStartWebApp=false and Application.PhotoStoragePath
to the isolated photos directory. The disposable solution must contain one project with empty
Z21.IpAddress, no locomotives, journeys/events, assigned triggers or external scripts/audio. Use an
unassigned workflow with clearly named steps A/B/C; editing values never authorizes execution.
The existing startup counter and initial health-check constraints below also apply. A prepared
package does not authorize launch; Chief PM rechecks current prerequisites for that process.

## Manual evidence record for the new theme-correction head (all NOT RUN)

| Step | Action after separate launch authorization | Expected result | Result / evidence |
| --- | --- | --- | --- |
| 1 | Use an isolated empty test project and select a disposable workflow. | No production data, API, hardware connection or automatic workflow execution. | NOT RUN |
| 2 | Switch List/Graph; select a card, then the trigger/background. | Same action and properties selected in both views; trigger/background opens workflow settings. | NOT RUN |
| 3 | Edit properties of step A without invoking Test/Play/Preview. | Parameter summary changes live and still belongs to the selected action. | NOT RUN |
| 4 | Add step C, reorder A/B/C and delete B, using the existing list and graph controls. | Summary, Step #1..N, list order and connectors update together. | NOT RUN |
| 5 | Drag a node, use Auto layout, zoom/pan and reset to 100%. | Drag/connectors agree; layout resets positions; reset returns offsets 0/0 and zoom 100%; XAML zoom range is 50-200%. | NOT RUN |
| 6 | Set first-action delay to 1500 ms, then zero; inspect the unassigned trigger. | +1500 ms badge appears then disappears; Manual / Unassigned remains read-only. Assigned-trigger case needs separately qualified static test data, never a running journey. | NOT RUN |
| 7 | Use existing Alt+Up/Down and Delete with a disposable step selected. | The same reorder/delete behavior and selection update as the visible controls. | NOT RUN |
| 8 | Inspect these states in Light and Dark themes, including selected and invalid actions. | Text, status, connectors and grid remain readable with correct theme resources. | NOT RUN |

Record operator, Europe/Berlin timestamp, commit/build identity, theme, actual/expected result,
PASS/FAIL and screenshot reference for every step. A failed safety precondition prevents launch.

## Historical quality-phase documentation assessment

Reviewed docs/ARCHITECTURE.md, docs/PROJECT-REFERENCE.md and
docs/wiki/MOBAFLOW-USER-GUIDE.md. The existing wiki describes the List/Graph projection, read-only
trigger, delay badges and auto layout. The original quality corrections change no user-visible behavior, runtime
ownership or public architecture; those pages need no correction for this phase. The acceptance
preparation and planning provenance are recorded here. Reassess after any behavior-changing review
fix and against integrated main; this is not a claim of complete post-merge documentation currency.

## Historical f5 acceptance: 2026-10-24

Root's first isolated JADE normal start failed in Microsoft.UI.Xaml.dll. The separately authorized
local CDB observation identified HRESULT 0x80073B01 (no loaded MUI entry). The original allowlist
had omitted all 172 native WinUI MUI files. All 45 generated XBF files were already fully embedded
in the identical MOBAflow.pri, so adding loose XBF was not the supported correction.

After explicit human approval, the Senior added exactly 172 missing MUI files in 86 language
folders, 3,530,328 bytes. Every source/target SHA-256 matched; all 688 pre-existing run files and
685 immutable reference files retained their hashes. Root's one normal retry at 16:02:46 CEST
(PID 22696) was responsive with a MOBAflow window at 16:03:30 CEST. No MOBApi/CDB was running.
This proves the controlled f5 test-package startup correction, not general runtime acceptance.

During the joint f5 UI check, the human accepted Dark appearance and List/Graph item
synchronization. Light failed WF388-LIGHT-01: descriptions, parameters, step numbers, icons,
ports and connections remained white/nearly white on light cards/canvas. Other manual scenarios
have no supplied pass evidence and remain NOT RUN. These observations apply only to f5, including
its MUI-only package supplement; they are not passes on a later source head.

## Native MUI packaging contract

The resolved Microsoft.WindowsAppSDK 2.5.1 uses WinUI 2.3.9 and Base 2.0.4. The self-contained
SDK target `AddMicrosoftWindowsAppSDKPayloadFilesFromComponents` includes
`runtimes-framework/win-x64/native/**/*.mui` and preserves `RecursiveDir` in the copied path.
The qualified Release output contains 86 language folders, each with exactly:

- `Microsoft.ui.xaml.dll.mui`
- `Microsoft.UI.Xaml.Phone.dll.mui`

All 172 output files matched the corresponding resolved WinUI NuGet payload. A new candidate
must include these exact files with their language paths, in addition to the generated runtime,
PRI and shipped asset allowlist. Their absence is not detected by the deps.json DLL inventory.
Do not use localized FileVersion text alone to compare a DLL: MUI presence can change that text
for identical bytes. Compare SHA-256 and the numeric fixed version.

Use a new program/data/photos/evidence directory, new disposable project GUIDs and a fresh
program-local config. Counter initialization reads and queues a save even without feedback;
the startup-created untitled project's random GUID cannot be truthfully named before launch.
Only Root's separately authorized normal-host preflight may qualify current counter metadata.
Do not read/hash/copy existing operator counters. Speech-cache construction performs no file I/O;
Speak/cache-size/clear operations can create/lock/read/write that shared cache and stay excluded.
HealthCheck.Enabled=false disables the periodic timer only: startup still performs one local
System.Speech capability enumeration. Never invoke a speech probe during package preparation.

## WF388-LIGHT-01 source-phase checks and handoff

The actual new plan -> tasks -> read-only analysis preceded code: 9 functional requirements,
4 success criteria, 17 tasks, six constitution principles, no blocking findings. Two graph-local
WinUI files apply existing theme keys through local Style setters on the generated elements.
No shared logic, commands, persistence, geometry, runtime, dependency or analyzer baseline changes.

On 2026-10-24 the correction passed the following local checks before publication:

- Named Release WinUI compilation: zero warnings and zero errors, finished 16:44:41 CEST.
- Full portable analyzer rebuild: zero errors; unchanged baseline 3211 diagnostics / 1125 groups.
- Portable workflow/library/defaults regression selection: 106 passed, zero failed or skipped.
- Full Windows analyzer rebuild: zero errors; unchanged baseline 2456 diagnostics / 946 groups.
- Windows workflow/library/defaults/XAML icon selection: 108 passed, zero failed or skipped,
  finished 16:55:01 CEST.
- All graphs ran serially with -m:1, -nr:false, --no-restore, --disable-build-servers and
  UseSharedCompilation=false. Windows builds retained the protected solution exclusion.
- Static resource checks found 17 used local styles and 19 ThemeResource brush setters, no
  missing/unused style key. This is structural evidence, not a rendered-contrast result.
- Local logs and TRX are under artifacts/issue378-validation/light-20261024-* and light-tests/.

These checks cover the source inputs committed by this correction; the final committed-head
assembly identity, current CI/Sonar and new candidate manifest accompany the source handoff.
Publication, candidate preparation and handoff are post-commit delivery steps T016/T017, tracked
with head-bound receipts rather than inferred from this pre-publication source checkpoint.
Actual contrast remains a human Dark -> Light -> Dark check on that new candidate, including
selection, valid/invalid actions, delay badges and assigned/unassigned trigger display.

Documentation assessment: the wiki's List/Graph instructions and architecture/developer contracts
remain accurate because this phase restores theme presentation only. No wiki or shared
architecture edit is needed; PR #392 owns another section of the same wiki file. The startup
qualification, MUI contract and head-specific acceptance history are corrected here. Reassess
against actual integrated main and any behavior-changing review fix before claiming currency.
