# Localization validation guide

Source issue: [#140](https://github.com/ahuelsmann/MOBAflow/issues/140).
Commands target planned artifacts and become runnable as tasks finish. Run from the branch
root. These commands do not launch apps.

## Automated checks

```powershell
scripts/Test-LocalizationResources.ps1
scripts/Test-LocalizationResources.Tests.ps1
dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0 --filter "FullyQualifiedName~SettingsPageViewModelTests"
dotnet build SharedUI/SharedUI.csproj
dotnet restore MOBAflow/MOBAflow.csproj
dotnet build MOBAflow/MOBAflow.csproj -c FastDebug --no-restore -p:BuildMOBApiDependency=false -p:CopyMOBApiToOutput=false
dotnet restore MOBAsmart/MOBAsmart.csproj
dotnet build MOBAsmart/MOBAsmart.csproj -f net10.0-android -c FastDebug --no-restore
dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0
dotnet test Test/Test.csproj -f net10.0-windows10.0.22621.0 -p:IncludeMobaSmartTests=false
scripts/Test-InstructionConsistency.ps1
scripts/Test-SpecKitGovernance.ps1 -Mode PullRequest -BaseRef github/main
```

Expected: intended tests execute, fallback/preference/isolation pass, malformed fixtures are
rejected, projection matches source, hosts compile. Missing workload/device is not a pass.

## Manual matrix - after explicit launch authorization

Record commit/OS/device and results for English/German/System on each host.

1. German OS System -> German slice; unsupported system language -> English.
2. Save opposite language: current UI unchanged, restart explanation. Fully exit/reopen
   manually: saved choice persists and covered text changes.
3. Inspect all Slice 1 [coverage.md](coverage.md) rows, including accessible names, dynamic
   restart/error messages, navigation tooltips and Help/Info links.
4. Check fallback in isolated automated fixtures, not by changing production files.
5. Compare solution/speech before/after language-only save: no value or undo-entry changes.
6. Light/Dark, keyboard/focus, desktop 200% and Android >=1.5 font scale: controls usable, text wraps.
7. Android offline startup works; preferences remain independent between hosts.

Use persistence fakes for errors. No automatic restart, hardware action or paid service.

## Contributor workflow

Author English/German text once in SharedUI/Localization catalogues with context comments.
Add WinUI mapping, generate and check. Planned `docs/LOCALIZATION.md` will document commands.
New languages also need supported-language policy and both-host tests; a file alone is insufficient.
