# Contributing to MOBAflow

Thank you for your interest in MOBAflow! 🎉

This document is intentionally concise and primarily intended for developers.
User documentation is available in `docs/wiki/`.

## 1. Getting Started

For AI-assisted work, first read [AGENTS.md](AGENTS.md) and the
[AI development guide](docs/AI-DEVELOPMENT.md). They explain how to select a worktree,
which project skills apply, which tools are required, and which checks to run.

### Clone and build the repository

```bash
git clone https://github.com/ahuelsmann/MOBAflow.git
cd MOBAflow

dotnet restore MOBAflow/MOBAflow.csproj
dotnet restore MOBApi/MOBApi.csproj
dotnet build MOBAflow/MOBAflow.csproj
```

Use `FastDebug` for quick local compilation checks of the WinUI app:

```bash
dotnet build MOBAflow/MOBAflow.csproj -c FastDebug --no-restore /p:BuildMOBApiDependency=false /p:CopyMOBApiToOutput=false
```

This configuration is intended for edit/build cycles. Use the normal build for
full app launches and release validation. See `docs/BUILD-PERFORMANCE.md` for details.

### Consistent line endings

PowerShell 7 (`pwsh`) is required for local checks. A fresh clone needs no setup steps;
the repository does not use Git hooks. CI checks line endings for every pull request.
Before committing, check the staged files:

```powershell
./scripts/Test-LineEndings.ps1 -Staged
```

The check covers staged files on disk and mixed line endings in the Git index.
It does not modify or stage files.

After changes made by patches, generators, or other tools:

```powershell
./scripts/Test-LineEndings.ps1 -Path Domain/Journey.cs -Fix
./scripts/Test-LineEndings.ps1 -Path Domain/Journey.cs
```

Without `-Path`, the script checks all tracked files and new files that are not ignored;
`-Fix` normalizes them. CRLF is the default. LF exceptions from `.gitattributes` (shell scripts
and managed Spec Kit files) are preserved, as are encoding and final-newline presence.
Binary files are skipped. The Git index continues to store normalized text files with LF.
CI complements this local check, but cannot detect mixed line endings in the original working
directory once Git has already normalized them during staging.

### Run tests

```bash
dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0
```

To collect coverage for the full Windows test suite, including desktop code:

```powershell
dotnet test Test/Test.csproj -f net10.0-windows10.0.22621.0 -p:IncludeMobaSmartTests=false --settings Test/coverlet.runsettings --results-directory TestResults/Coverage
```

The report combines unit and integration tests; Android is excluded.
Line coverage shows which code was executed. Mutation tests also check whether tests detect
deliberately introduced faults. A surviving mutant may indicate a missing assertion or a code
change with no observable difference in behavior. Do not force tests to reject every mutant.

### Mutation testing with Stryker

The active test groups are `Domain` and `Common`. Their test projects include the same files as
the regular test suite; new tests under `Test/Domain/` and `Test/Common/` are included automatically.
The Common group also runs the existing Backend tests `Recording*.cs` and
`MultiplexerCommandResolverTests.cs` to verify shared Common contracts.
It runs on Windows in CI because existing tests also cover Windows-specific photo paths.
The Common configuration limits parallel Stryker test processes to four.
This does not yet enable mutation testing of Backend production code.
Other groups are still marked as planned in [mutation-lanes.json](MutationTest/mutation-lanes.json)
and are not currently protected by Stryker.

```powershell
dotnet tool restore
Push-Location MutationTest
try {
    dotnet stryker --skip-version-check
    dotnet stryker --config-file stryker-common-config.json --skip-version-check
}
finally {
    Pop-Location
}
```

The quality pipeline runs both groups and archives the HTML/JSON reports. The minimum scores
are 90% for `Domain` and initially 63% for `Common`. The Common threshold is a baseline safeguard,
not a quality target: close meaningful gaps first, then raise the threshold as results improve.
Extending coverage to the rest of the solution remains separate, unfinished work.

Run Stryker and regular `dotnet build`/`dotnet test` commands sequentially in the same worktree.
Stryker recreates shared intermediate build files; concurrent runs may therefore fail because
reference files are missing.

## 2. Project overview and key documentation

- Architecture and layers: `docs/ARCHITECTURE.md`
- JSON validation and solution format: `docs/JSON-VALIDATION.md`
- Hardware and liability notices: `docs/HARDWARE-DISCLAIMER.md`
- Third-party licenses: `docs/THIRD-PARTY-NOTICES.md`
- User wiki: `docs/wiki/INDEX.md`

Read at least `README.md` and `docs/ARCHITECTURE.md` before making larger changes.

**Repository:** Code, issues, and pull requests are hosted on [GitHub](https://github.com/ahuelsmann/MOBAflow).
GitHub Actions hosts the public quality and release workflows.

## 3. How to contribute

Changes to `main` must always go through a pull request. Implementations, bug fixes, and other
repository changes must not be committed or pushed directly to `main`.
Work in a dedicated branch and worktree. Update local `main` only by fast-forwarding to changes
that have already been merged through GitHub.

GitHub requires pull requests even for administrators, requires passing checks before merging
(all jobs in `.github/workflows/quality.yml` and SonarCloud), and blocks force pushes and deletion
of `main`. Additional approval by a second person is not currently required.
Do not bypass branch protection. The Sonar and validation requirements in
[AGENTS.md](AGENTS.md) still apply.

- **Report bugs or suggest features**
  - GitHub Issues: `https://github.com/ahuelsmann/MOBAflow/issues`
  - Include, where possible:
    - Steps to reproduce
    - Expected behavior
    - Actual behavior
    - Relevant log excerpts without secrets

- **Pull requests**
  1. Fork the repository
  2. Create a feature branch (e.g., `feat/...`, `fix/...`)
  3. Implement the changes, including tests
  4. Run relevant project builds and `dotnet test Test/Test.csproj` locally
  5. Update relevant documentation (README / wiki / docs)
  6. Open a pull request against `main`

Briefly describe **what** you changed and **why** in the pull request.

## 4. Coding guidelines (summary)

The full rules are available in:

- `.github/copilot-instructions.md`
- `.github/instructions/*.instructions.md`

In brief:

- **Architecture**
  - Follow Clean Architecture (`Domain` → `Backend/Common` → `SharedUI` → `MOBAflow/MOBAsmart/MOBApi`)
  - Use MVVM with `CommunityToolkit.Mvvm` (`[ObservableProperty]`, `[RelayCommand]`)
  - Use constructor injection only, not a service locator

- **Async / threading**
  - Never use `.Result` / `.Wait()`; always use `async/await`
  - Update the UI only through the applicable dispatcher / EventBus mechanism

- **Style**
  - Follow `.editorconfig` (format using your IDE)
  - Use meaningful names instead of `data`, `tmp`, or `x`
  - Keep methods small and focused (20–25 lines where possible)
  - Document public APIs with XML comments (`/// <summary>`)

- **Tests**
  - Cover new logic with unit tests where possible (NUnit)
  - Keep existing tests passing

## 5. Update documentation

Write developer-facing Markdown documentation in English. Keep user-facing documentation
in its intended language.

- User-facing changes:
  - Update `README.md` and/or the relevant page under `docs/wiki/`
- Technical changes:
  - Update `docs/ARCHITECTURE.md`, `docs/JSON-VALIDATION.md`, etc. as needed
- Standalone project, quality, refactoring, and roadmap plans:
  - Store them as Markdown files under `plans/`, not `docs/`
  - Delete completed plans; Git history and closed GitHub issues preserve the record
  - GitHub issues, milestones, and Kanban remain the source of truth for status and progress
- Third-party dependencies / new packages:
  - Update `docs/THIRD-PARTY-NOTICES.md` with license information

## 6. Contributor License Agreement (CLA)

Contributions to MOBAflow are subject to the Contributor License Agreement:

- **Document:** `docs/legal/CLA.md`

By submitting a pull request, you confirm that you have read and accepted the terms of this CLA.
For questions about licensing or the CLA:

- See `docs/legal/CLA.md`
- Or open an issue with the `cla-question` label

---

Thank you for contributing to MOBAflow! 🚂✨

