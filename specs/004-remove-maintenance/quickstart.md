# Validation guide

Source: https://github.com/ahuelsmann/MOBAflow/issues/147

Prerequisites: SDK from global.json; Windows tooling and Android workloads for their respective builds.
Run from this dedicated worktree. Coordinate substantial builds with the master task.

1. Run focused inventory, serialization, decoder/passport, DI and solution transport/cache regressions.
2. Run `dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0`.
3. Build the Windows Release test graph with normal MOBAflow/MOBApi dependencies and
   MobaAnalyzerGate=true. Restore/publish MOBAsmart net10.0-android in Release with the analyzer gate.
4. Run relevant Windows-target regressions; inspect schema and documentation.
5. Run line-ending/governance checks, secrets scans and review the complete diff.
6. Publish a draft PR only after integrating main. Verify CI and current-commit SonarCloud with zero
   OPEN/CONFIRMED findings. Master coordinates the final merge.

The authorized isolated synthetic-data inspection was completed without layout connectivity.
The following checklist defines the vehicle-page states; see the recorded scope and results below.
Inspect `LocomotivesPage`,
`PassengerWagonPage` and `GoodsWagonPage` in both Light and Dark themes:

- Empty inventory/no selection and an inventory with a selected vehicle: the list and Properties pane
  remain usable, with no maintenance filter, panel, calendar or empty space left by their removal.
- Search with matches, no matches and surrounding spaces; add and delete while a search is active.
- Rename a selected vehicle while it still matches the search: selection and the editor remain visible.
  Rename it so it no longer matches: the filtered list updates correctly.
- Switch projects and reload saved data: no selection from the previous project remains, and vehicle
  edits persist. Verify the remaining master-data and photo controls on all three pages.
- On the locomotive page, inspect decoder CV import/export, feedback whistle rule editing and printable
  locomotive passport export. The passport contains no maintenance rows. Do not exercise live functions.

Only the isolated test copy was launched for this acceptance check. No hardware action was performed.

## Recorded results

Final integration includes main `d0ffddc41efdd9aa422c1a2f7a48de67b5445e8b` (including the workflow,
reservation, timetable and dependency changes). The final application code is
`706fc3a9a43ef948a13688dde811d216e4acd889`.

- Portable Release: 1685 passed / 4 skipped / 0 failed. The four skips are the missing bundled-photo
  folder check and three opt-in MOBApi integration tests requiring a server on localhost:5001.
- Focused inventory/photo regressions: 29 passed, including all three vehicle kinds during delayed
  photo copying with a changed selection, changed project, replaced solution or a persistence failure.
- Windows Release build passed; all 1743 Windows tests passed, with no skips or failures.
- Android Release publication produced both AAB files; both passed the bundle structure and ABI checks.
- Portable analyzer baseline matches 3999 diagnostics in 1268 groups. Its changes only remove or
  reduce maintenance-related allowances. Windows and Android comparisons likewise contain only
  reductions; their baselines were updated without new or increased allowances.
- Independent Standards and Spec reviews are complete with zero open findings on the final code.
  The photo persistence and error-observation findings were fixed and regression-tested.
- JSON schema parsing, Spec Kit governance and the changed-file secrets/line-ending checks passed.
  Draft publication and current-commit GitHub CI/Sonar evidence remain pending.

Native UI acceptance on the code committed in `441cdb2170533a88bf7ff77a5f8e177dee61afd3` passed:

- All three vehicle pages were inspected in Light/Dark with and without a selected vehicle. No maintenance
  filters, panels or calendar remained, and no layout gap from their removal was observed.
- Each vehicle kind was searched with surrounding spaces, renamed while still matching, and searched with
  no results. Matching rename preserved selection and the editor; no-result search cleared them.
- Adding and deleting a temporary vehicle under an active matching search worked on all three pages.
- Switching to the second synthetic project refreshed all three lists without a previous selection.
- Restarting the test copy restored all three edited names; the temporary deleted vehicles stayed absent.
- Photo/master-data controls were present; the synthetic decoder CV backup and its import/export controls
  were visible, as was the printable passport control. File-picker/import/export dialog flows and actual
  photo assignment were not exercised in this UI pass; the retained automated regressions cover their logic.

Isolation used a separate Release binary copy, own settings/photos/logs, two synthetic projects, six
synthetic vehicles and inactive journeys. The invalid nonblank Z21 address prevented connection and discovery;
REST auto-start, REST connection and health checks were disabled. No operator data or secrets were copied.
The native Computer Use provider confirmed the test instance absent after shutdown; its slot was released.
