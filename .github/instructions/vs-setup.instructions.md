# Visual Studio setup for MOBAflow

This is optional environment guidance. The repository workflow and validation
requirements are defined in [AGENTS.md](../../AGENTS.md).

## Required project tooling

- Use the .NET SDK selected by [global.json](../../global.json).
- Install the Visual Studio workloads listed in [.vsconfig](../../.vsconfig),
  or equivalent command-line tooling for the target you are building.
- Open [Moba.slnx](../../Moba.slnx). Windows and Android projects need their
  respective platform workloads; not every task needs the complete solution.
- PowerShell 7 (`pwsh`) runs the repository's local validation scripts.

An editor extension is not a build prerequisite. Optional Copilot, refactoring,
profiling or formatting tools must not change the repository's analyzer baseline,
central NuGet versions or Sonar policy.

## Editor configuration

Use [.editorconfig](../../.editorconfig) and [.gitattributes](../../.gitattributes)
as the source of truth for formatting, encoding and line endings. Do not override
them with a personal formatter configuration.

Keep generated `bin`, `obj` and repository-local package directories out of
search and language-server project discovery.

## Build and tests

Run commands from the dedicated task worktree root. For a Windows compile check:

```powershell
dotnet restore MOBAflow/MOBAflow.csproj
dotnet build MOBAflow/MOBAflow.csproj -c FastDebug --no-restore -p:BuildMOBApiDependency=false -p:CopyMOBApiToOutput=false
```

FastDebug skips API build/copy and is not a release or host-integration check.
For focused portable tests, select an actual fixture:

```powershell
dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0 --filter "FullyQualifiedName~ActualFixtureName"
```

Replace `ActualFixtureName` with an existing fixture. Passing zero tests does not
validate the change. For platform builds, coverage and broader checks, use
[CONTRIBUTING.md](../../CONTRIBUTING.md) and the AGENTS.md validation table.

## Debugging and safety

Visual Studio debugging starts the application. Build/test authorization alone
does not authorize agents to press F5, launch MOBAflow or operate layout hardware.
Use the project configuration and current launch settings instead of inventing a
fixed executable path under `bin/Debug`.

## Before committing

Run the checks required by the changed behavior, scan changed files for secrets,
and check staged line endings:

```powershell
./scripts/Test-LineEndings.ps1 -Staged
```

Repository changes require a task branch and a draft pull request. Sonar code
analysis runs through the GitHub PR pipeline, not local analysis hooks.
