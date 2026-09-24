# Validation guide

Source: https://github.com/ahuelsmann/MOBAflow/issues/147

Prerequisites: SDK from global.json; Windows tooling and Android workloads for their respective builds.
Run from this dedicated worktree. Coordinate substantial builds with the master task.

1. Run focused inventory, serialization, decoder/passport, DI and solution transport/cache regressions.
2. Run `dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0`.
3. Build MOBApi; restore/build MOBAflow with FastDebug, BuildMOBApiDependency=false and
   CopyMOBApiToOutput=false. Restore/build MOBAsmart net10.0-android with FastDebug.
4. Run relevant Windows-target regressions; inspect schema and documentation.
5. Run line-ending/governance checks, secrets scans and review the complete diff.
6. Publish a draft PR only after integrating main. Verify CI and current-commit SonarCloud with zero
   OPEN/CONFIRMED findings. Master coordinates the final merge after #146.

Manual acceptance (requires separate launch permission): on each vehicle page, search, add, select,
rename, delete, switch projects and reload saved data in Light/Dark. Verify photos, decoder backup and
passport export remain available and no maintenance control remains. No hardware action is required.

## Recorded results

Portable validation: 43 focused tests passed, followed by 1686 passed / 4 skipped / 0 failed in the full net10.0 suite. Skips: missing bundled-photo folder and three opt-in integration tests requiring MOBApi on localhost:5001. The incremental build (including MOBApi) passed without compiler warnings. Spec Kit governance, line endings and schema JSON checks passed. Windows/Android builds, analyzer baselines, CI/Sonar and Light/Dark acceptance remain pending. No app launch or hardware action is authorized.
