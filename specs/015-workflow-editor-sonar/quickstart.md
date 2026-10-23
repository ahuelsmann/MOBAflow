# Issue #378 validation and practical acceptance preparation

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/378

## Execution boundary

This phase prepares acceptance only. No MOBAflow, API host, debugger, workflow, device or hardware
may be started. A separate project file is not by itself evidence of safe host startup.
The operator-owned MOBAflow/solution.json is excluded from every operation.

## Build identity and automated evidence

The first validation build contains the bounded corrections on top of d6aa332c. Its assembly
file version is 0.2.1.0 and product version is
`0.2.1-alpha.0.263+d6aa332c10036172965845987143084fa881e66d`.
This pre-publication version is not the final acceptance build identity. The final source commit,
fresh build version and exact-head remote CI/Sonar receipts must accompany the Chief PM handoff.

Actual compiled executable (not started):
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

## Startup qualification: PREPARATION / NOT READY TO LAUNCH

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

Candidate isolated staging root, not created or launched:
`C:/Repo/ahuelsmann/MOBAflow/artifacts/worktrees/issue-378-graphical-workflow-editor/artifacts/issue378-acceptance/<final-head>/`.
Use separate `program/`, `data/`, `photos/` and `evidence/` directories. The intended executable is
`program/MOBAflow.exe`, fresh non-secret configuration is `program/appsettings.json`, and the only
test solution is `data/workflow-editor-test.json`. A final-head binary/asset allowlist and remaining
counter/cache/master-data path qualification are still required before staging or launch. Do not
copy the entire build directory: it can contain pre-existing operator data. No real settings,
secrets or protected solution are copied by this phase.

Fresh candidate settings must explicitly set Application.AutoLoadLastSolution=false,
Application.LastSolutionPath="", Application.AutoStartWebApp=false and Application.PhotoStoragePath
to the isolated photos directory. The disposable solution must contain one project with empty
Z21.IpAddress, no locomotives, journeys/events, assigned triggers or external scripts/audio. Use an
unassigned workflow with clearly named steps A/B/C; editing values never authorizes execution.
Configuration-only evidence has not yet established every startup path's isolation. Chief PM and
Art Director receive this concrete remaining qualification, not a launch-ready claim.

## Manual evidence record (all NOT RUN)

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

## Documentation impact

Reviewed docs/ARCHITECTURE.md, docs/PROJECT-REFERENCE.md and
docs/wiki/MOBAFLOW-USER-GUIDE.md. The existing wiki describes the List/Graph projection, read-only
trigger, delay badges and auto layout. These corrections change no user-visible behavior, runtime
ownership or public architecture; those pages need no correction for this phase. The acceptance
preparation and planning provenance are recorded here. Reassess after any behavior-changing review
fix and against integrated main; this is not a claim of complete post-merge documentation currency.
