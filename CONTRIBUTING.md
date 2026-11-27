# Contributing to MOBAflow

Thank you for your interest in MOBAflow! 🎉

This document is intentionally concise and primarily intended for developers.
User documentation is available in `docs/wiki/`.

## 1. Getting Started

For AI-assisted work, first read [AGENTS.md](AGENTS.md) and the
[AI development guide](docs/developer/AI-DEVELOPMENT.md). They explain how to select a worktree,
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
full app launches and release validation. See `docs/developer/BUILD-PERFORMANCE.md` for details.

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
and managed Spec Kit files) and the encoding are preserved. `-Fix` does not add or remove a final
newline; `.editorconfig` requires one in every file except vendored `MOBAdisplay/esp32/lib/**` and
Spec Kit-managed `.specify/**` files.
Binary files are skipped. The Git index continues to store normalized text files with LF.
CI complements this local check, but cannot detect mixed line endings in the original working
directory once Git has already normalized them during staging.

### C# formatting

C# whitespace (indentation, spacing, line breaks and the final newline) follows `.editorconfig`.
The required `Portable tests on Linux` check fails on drift. Check locally without a restore:

```powershell
dotnet format whitespace --folder . --verify-no-changes --exclude .nuget MOBAdisplay/esp32
```

Run it from the root of your task checkout or worktree: `--folder` does not apply Git ignore rules, so
a checkout that contains other worktrees (for example `.claude/worktrees/`) would be scanned too.
Without `--verify-no-changes` the command fixes the drift; keep the fix to the files you changed.
If an SDK update changes the formatter's output, the pull request that bumps the SDK fixes the gate;
a further repository-wide reformat is added to `.git-blame-ignore-revs` after it merges.
The one-time repository-wide reformat is listed in `.git-blame-ignore-revs`. To hide it from
`git blame`, optionally run `git config blame.ignoreRevsFile .git-blame-ignore-revs`; GitHub's
blame view uses the file automatically.

### Run tests

```bash
dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0
```

To collect coverage for the full Windows test suite, including desktop code:

```powershell
dotnet test Test/Test.csproj -f net10.0-windows10.0.22621.0 --settings Test/coverlet.runsettings --results-directory TestResults/Coverage
```

The report combines unit and integration tests; Android is excluded.
Line coverage shows which code was executed. Mutation tests also check whether tests detect
deliberately introduced faults. A surviving mutant may indicate a missing assertion or a code
change with no observable difference in behavior. Do not force tests to reject every mutant.

### Mutation testing with Stryker

The active test groups are `Domain`, `Common`, `Contracts`, `Speech.Base`, `Backend`, `MOBApi` and `SharedUI`. Their test projects include
the same files as the regular test suite; new tests under `Test/Domain/` and `Test/Common/` are included automatically.
The Common group also runs the existing Backend tests `Recording*.cs` and
`MultiplexerCommandResolverTests.cs` to verify shared Common contracts, and
`Test/Speech/SpeechSettingsTests.cs` to verify the Common speech settings.
It runs on Windows in CI because existing tests also cover Windows-specific photo paths.
The Common configuration limits parallel Stryker test processes to four.
The Backend group mutates the `Backend` assembly with all tests under `Test/Backend/`, the workflow
end-to-end fixture and `Test/Z21DccCommandDecoderTests.cs`; new tests under `Test/Backend/` are
included automatically. Methods that Stryker puts into Safe Mode are excluded from the score; see
[Safe Mode and methods outside the gate](docs/developer/BUILD-PERFORMANCE.md#safe-mode-and-methods-outside-the-gate).
It runs on Linux and takes about 35 minutes.
The portable Speech.Base group runs `AudioCacheTests.cs`, `CachedSpeechOutputTests.cs`,
`SpeechOutputTests.cs` and `WaveAudioTests.cs` against the entire `Speech.Base` assembly.
It runs on Linux in every code-relevant PR, using fake providers and players rather than
real speech engines or audio devices. Stryker concurrency is limited to four test processes.
Windows-only host integration cases stay in the ordinary Windows suite; Piper and SystemSpeech
production code remain separate planned lanes.
The MOBApi group mutates `MOBApi` with the in-process fixtures under `Test/MOBApi/` (about two minutes locally);
the MOBApi process fixtures stay in the planned `api-process` group. The SharedUI group mutates `SharedUI` with
every fixture under `Test/SharedUI/`, the track-layout architecture fixture parts that
`SignalBoxPropertiesViewModelTests` extends, and the shared test helpers; it is the largest group (about 12,000 mutants
and 63 minutes locally with four Stryker processes) and runs on Linux.
The Contracts group mutates `Contracts` with every fixture under `Test/Contracts/` and runs on Linux in under a minute.
Other groups are still marked as planned in [mutation-lanes.json](MutationTest/mutation-lanes.json)
and are not currently protected by Stryker;
[BUILD-PERFORMANCE.md](docs/developer/BUILD-PERFORMANCE.md#planned-lanes-with-measured-reasons) records why for the RF-09 candidates.
Triage rules for surviving mutants are in
[Working with surviving mutants](docs/developer/BUILD-PERFORMANCE.md#working-with-surviving-mutants).

```powershell
dotnet tool restore
if ($LASTEXITCODE -ne 0) { throw "Tool restore failed." }
./scripts/Test-MutationLaneCoverage.ps1
# Each output directory must be new. Choose a different path for a later run.
./scripts/Invoke-MutationLane.ps1 -LaneName domain -OutputPath artifacts/mutation/domain-local
./scripts/Invoke-MutationLane.ps1 -LaneName common -OutputPath artifacts/mutation/common-local
./scripts/Invoke-MutationLane.ps1 -LaneName contracts -OutputPath artifacts/mutation/contracts-local
./scripts/Invoke-MutationLane.ps1 -LaneName speech-base -OutputPath artifacts/mutation/speech-base-local
./scripts/Invoke-MutationLane.ps1 -LaneName backend -OutputPath artifacts/mutation/backend-local
./scripts/Invoke-MutationLane.ps1 -LaneName api -OutputPath artifacts/mutation/api-local
./scripts/Invoke-MutationLane.ps1 -LaneName shared-ui -OutputPath artifacts/mutation/shared-ui-local
```

A mutation score is valid only with a passing ordinary test baseline. The same runner is used
locally and in CI: it checks effective MSBuild `Compile` items against the active registry,
runs the ordinary tests, rejects skipped/failed/empty runs, then runs Stryker. It requires a fresh
JSON report for the correct production project, all registered test files, the same test cases as
the baseline, and completed mutant statuses with at least one mutant executed against tests.
Missing files, filters, stale reports and unexecuted mutations fail the job. A test that does not
cover a particular mutant is not forced to run against it; Stryker keeps its coverage-based selection.
See [Build Performance](docs/developer/BUILD-PERFORMANCE.md#active-mutation-lanes) for job selection.
The quality pipeline runs selected groups and archives the HTML/JSON reports. The minimum scores
are 90% for `Domain`, initially 65% for `Speech.Base`, 63% for `Common`, 68% for `Contracts`, 47% for `Backend`, 74% for `MOBApi` and 46% for
`SharedUI`. A `Contracts` score below its design floor of 70% is a finding to investigate, not a reason to lower a
threshold. The Speech.Base, Common, Contracts, Backend, MOBApi and SharedUI thresholds are baseline safeguards, not quality
targets: close meaningful gaps first, then raise the thresholds as results improve. Surviving mutations, including scheduling-only
`ConfigureAwait` changes, remain in the report and score; the lane does not hide them with exclusions.
Extending coverage to the rest of the solution remains separate, unfinished work.

Run Stryker and regular `dotnet build`/`dotnet test` commands sequentially in the same worktree.
Stryker recreates shared intermediate build files; concurrent runs may therefore fail because
reference files are missing.

## 2. Project overview and key documentation

- Architecture and layers: `docs/developer/ARCHITECTURE.md`
- JSON validation and solution format: `docs/developer/JSON-VALIDATION.md`
- Hardware and liability notices: `docs/user/HARDWARE-DISCLAIMER.md`
- Third-party licenses: `docs/user/THIRD-PARTY-NOTICES.md`
- User wiki: `docs/wiki/INDEX.md`

Read at least `README.md` and `docs/developer/ARCHITECTURE.md` before making larger changes.

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
  - Follow the tested [project dependency table](docs/developer/ARCHITECTURE.md#project-dependency-rules); `MOBApi` references only `Contracts` and `Common`
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
  - Update `docs/developer/ARCHITECTURE.md`, `docs/developer/JSON-VALIDATION.md`, etc. as needed
- Standalone project, quality, refactoring, and roadmap plans:
  - Store them as Markdown files under `plans/`, not `docs/`
  - Delete completed plans; Git history and closed GitHub issues preserve the record
  - GitHub issues, milestones, and Kanban remain the source of truth for status and progress
- Third-party dependencies / new packages:
  - Update `docs/user/THIRD-PARTY-NOTICES.md` with license information

## 6. Contributor License Agreement (CLA)

Contributions to MOBAflow are subject to the Contributor License Agreement:

- **Document:** `docs/legal/CLA.md`

By submitting a pull request, you confirm that you have read and accepted the terms of this CLA.
For questions about licensing or the CLA:

- See `docs/legal/CLA.md`
- Or open an issue with the `cla-question` label

---

Thank you for contributing to MOBAflow! 🚂✨

