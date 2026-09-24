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
   OPEN/CONFIRMED findings. Master coordinates the final merge after #146.

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

Portable validation: 43 focused tests passed, followed by 1686 passed / 4 skipped / 0 failed in the full
net10.0 suite. Skips: missing bundled-photo folder and three opt-in integration tests requiring MOBApi
on localhost:5001. After the review fixes for stable selection and trimmed search, 32 focused tests
passed with no skips. Those regression methods were subsequently moved into existing fixtures;
their final Windows validation passed: 1750 tests passed, with zero failures and zero skips (58 seconds).
The Windows Release test build, including WinUI XAML and normal MOBApi dependencies, passed. The
tested state was commit `dd18c8f6b6e85424718290400d441a5799d20784` plus the unchanged review/test/docs patch
with SHA256 `E02A20B1DAD714A075AE79DE51CCD83FC4666A30EF635D8AB79625B6753E0FAF`.
The Windows analyzer comparison contains only expected maintenance-related decreases, with no new
or increased diagnostic groups. Android Release rebuild/publication passed; `scripts/Test-AndroidAppBundle.ps1`
validated both published AAB files, including the required arm64-v8a and x86_64 libraries. Spec Kit
governance, line endings and schema JSON checks passed. Final analyzer baselines,
CI/Sonar remain pending. The final baselines require complete fresh outputs
after integrating the centrally coordinated main changes; the separate Android output folder alone
does not contain SARIF for shared projects reused incrementally.

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

After integrating main `29f56176aa699ffd14a2b96c9b98a54a5c342f90` (workflow action sequences), the portable
Release test-project rebuild passed. All 56 focused vehicle, persistence, DI and workflow boundary tests
passed with no failures or skips. The complete fresh portable SARIF comparison showed only expected
maintenance-related decreases, with no new or increased groups. Final Windows/Android runs and baseline
updates are deliberately deferred until the additional #146 integration, as centrally coordinated.
