# Validation guide: Journey event plans

Run from the dedicated worktree. These are required checks, not claims they have passed; see [tasks.md](tasks.md) for evidence status.

## Automated validation

First run discovered event-plan, domain, journey feedback and shared editor fixtures with a nonzero executed-test count. Cover five activations with only triggers 2/3/5, independent ports, concurrent baselines, stop/restart, workflow failure, reset races, drafts and legacy round trips. Then run required affected target suites and consumers:

```powershell
dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0
dotnet build MOBApi/MOBApi.csproj
dotnet restore MOBAflow/MOBAflow.csproj
dotnet build MOBAflow/MOBAflow.csproj -c FastDebug --no-restore -p:BuildMOBApiDependency=false -p:CopyMOBApiToOutput=false
dotnet test Test/Test.csproj -f net10.0-windows10.0.22621.0 -p:IncludeMobaSmartTests=false
```

If Android source is affected, use the matching target/workload and report unavailable checks explicitly.

## Focused UI acceptance

For an explicitly authorized focused acceptance run, use simulated feedback without live track operations. Positive feedback on the improved appearance does not complete all checks below:

1. Create an event plan with InPort 1 at 2, 3 and 5 linked to signal, announcement and stop-change workflows.
2. Assign/replace by dropping on a row, create by dropping on the add target and reorder by dragging. Check visible drop feedback and unchanged thresholds.
3. Use keyboard-accessible controls for add, assignment, duplication, movement and deletion; verify useful focus and clear count labels. Duplicate, move and delete are in the event toolbar overflow; reset counters is in the journey options menu.
4. Save/reload; verify settings, IDs and order. Explicitly adopt an empty plan on a legacy journey and verify its old reference remains.
5. Start at nonzero counts and simulate feedback: only relative 2/3/5 run. Stop changes leave totals/baselines intact.
6. Run three journeys with interleaved ports and different starts; verify independent deltas and idle-only reset.
7. Inspect empty, legacy, populated, invalid and running/read-only states in Light and Dark themes, including focus, disabled controls and drag affordances. Verify compact rows, aligned headers and a drop target immediately after the last event. Below 900 effective page pixels the library moves below the plan; below 720 the journey commands wrap below the selector. Include a short window, ensure both lists remain accessible, and verify restoring the window preserves the saved library width. Invalid count messages must appear only for invalid input.
8. While A runs, start unchanged B/C and verify A's state is preserved. Edit an inactive journey or shared workflow and attempt to start it: verify a clear rejection rather than stale execution. Stop every event-plan run, then start with the updated definitions.
9. Select different journeys/projects while a run is active; verify counters and the running journey stay intact. In the shared journey editor, event-plan journeys show the Event Manager guidance instead of legacy feedback controls.

## Recorded automated evidence

- Runtime baseline portable full suite including the GotoJourney active-target guard, before UI refinement: **1,710 passed, zero failed, four skipped; 1,714 total**, 45 seconds. Result file: `Test/TestResults/event-plan-portable.trx`.
- The four skips are three checks requiring a running MOBApi and one requiring bundled photos; none is reported as passed.
- Local static checks passed: `git diff --check`, line endings and deterministic secrets scans across 62 changed files, plus staged checks. Rerun staged checks after final staging of metadata updates.
- The portable build/test graph includes `MOBApi/MOBApi.csproj`; its compile evidence does not require a separate standalone build command. Additional Windows-specific runtime tests were not run. Manual UI, source-issue governance and Sonar/publication gates remain separate.
- The GotoJourney regression confirms that targeting an already running journey preserves its run identity and counter baselines.
- Visual refinement: the focused `EventManagerEventPlanTests`, `JourneyCounterProjectionTests` and `XamlIconMarkupTests` run passed **18 tests, zero failed, zero skipped**. Source review covered compact sizing, collapsed validation, responsive placement, library scrolling and drag feedback. These checks do not establish actual rendering or drag-and-drop behavior in Light or Dark themes.
- Final Windows FastDebug build after the visual refinement: **zero warnings, zero errors**, 1 minute 29 seconds. MOBAflow was not launched or restarted for this check; the running Debug instance was left untouched.

## Static and publication checks

- Authoritative [issue #124](https://github.com/ahuelsmann/MOBAflow/issues/124) is linked; the actual issue body and PR changes passed Spec Kit governance against `github/main`.
- Run `scripts/Test-LineEndings.ps1 -Path <changed paths> -Fix`, then check again; use `-Staged` before a commit.
- Changed files passed deterministic secrets scanning. Local Sonar ran against `github/main` at `698c4c8b5e35a4071e4acd81635e5a2dde3d3b41`: zero secrets issues, 62 agentic failures because the organization lacks agentic analysis capability (403 Forbidden), exit 1. This is an attempted analysis, not a green result.
- Any future PR stays draft until SonarCloud is green with zero OPEN/CONFIRMED issues. Manual/hardware checks remain open until actually performed.
