# RF-10: Formatting baseline and gate

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/451
**Spec Kit**: Not applicable - mechanical formatting and a CI gate without product behavior, persisted-format or architecture change
**Status**: Planned; nothing is implemented. Slice 1 needs the two decisions below, slice 2 needs a quiet window

## Purpose and scope

[Issue #451](https://github.com/ahuelsmann/MOBAflow/issues/451) owns acceptance and live status.
The [umbrella plan](QUALITY-AND-REFACTORING-PLAN.md) owns programme dependencies. This is the single RF-10 child plan.

Target: one agreed formatting baseline for the repository's C# code and a mandatory CI step that rejects drift with
`dotnet format --verify-no-changes`. The baseline change is mechanical: no functional or architecture work, no rule
tightening beyond what the gate needs, no analyzer baseline reduction.

## Inventory

Measured on main `4ae44d75` (2026-11-14) in a Linux cloud session with .NET SDK 10.0.114 (the pinned SDK of
`global.json` was not available there; slice 1 repeats the measurement with the pinned SDK on Windows).

| Check | Command | Result |
| --- | --- | --- |
| Whitespace, all tracked C# files | `dotnet format whitespace --folder . --verify-no-changes --exclude .nuget MOBAdisplay/esp32` | 768 findings in 691 of 1,077 files |
| of which final newline | same | 688 findings, all "delete the final newline" |
| of which indentation and spacing | same | 80 findings |
| Style, sample `Domain` | `dotnet format style Domain/Domain.csproj --verify-no-changes` | 1 (`IDE2002`) |
| Style, sample `Backend` | `dotnet format style Backend/Backend.csproj --verify-no-changes` | 9 import-ordering findings |
| Analyzers, sample `Backend` | `dotnet format analyzers Backend/Backend.csproj --verify-no-changes` | 2 (`IDE2000`, `IDE0005`) |

Whitespace findings by top-level folder: `Test` 301, `SharedUI` 127, `Backend` 103, `MOBAflow` 68, `Domain` 28,
`TrackPlan.Editor` 23, `MOBAsmart` 21, `MOBApi` 21, `Common` 17, `Speech.Base` 16, `Contracts` 16, `MOBAdisplay` 12,
others 15.

Not measured: style and analyzer findings of the other projects. `MOBAflow` (WinUI) and `MOBAsmart` (MAUI Android)
need Windows with their workloads for a workspace-based run; a cloud session can only run the folder-based
whitespace check on them.

Existing rules and tools:

- [`.editorconfig`](../.editorconfig): spaces, width 4, UTF-8, `end_of_line = crlf`, `insert_final_newline = false`,
  maximum line length 140.
- [`.gitattributes`](../.gitattributes): text is stored with LF and checked out with CRLF, with LF exceptions for
  shell scripts and Spec Kit assets.
- `scripts/Test-LineEndings.ps1` checks line endings and mixed endings and preserves final-newline presence;
  [AGENTS.md](../AGENTS.md) requires it after generated or scripted edits.
- [Quality CI](../.github/workflows/quality.yml) has no formatting step.

## Decisions

Two decisions are open and belong to slice 1. Each has a recommendation; neither is taken by this plan.

- **Final newline (open).** `insert_final_newline = false` makes `dotnet format` delete the final newline of 688
  files, while the repository rule preserves whatever a file has. Options: (a) keep `false` and let the baseline
  remove the newlines; (b) set `true`, the common convention of Git and most editors, and let the baseline add the
  missing ones; (c) remove the key, so the gate ignores final newlines and `Test-LineEndings.ps1` stays the only
  guard. Recommendation: (b), because diffs and tools then agree, with `Test-LineEndings.ps1` and the AGENTS.md
  sentence on final newlines adjusted in the same change. The count of files that lack a final newline is measured
  in slice 1 before the decision.
- **Gate scope (open).** Options: (a) whitespace only, folder-based, which runs on Linux for every project and needs
  no restore; (b) whitespace plus style for the portable projects, with `MOBAflow` and `MOBAsmart` checked for
  whitespace only; (c) whitespace, style and analyzers for every project, which needs the Windows lane and the
  Android workload. Recommendation: start with (a) as the mandatory gate, because it is the only variant that every
  platform can reproduce locally, and decide on (b) after the first weeks without drift. Style and analyzer drift
  stays covered by the existing analyzer baseline gates.
- **Exclusions.** Generated code (`obj`, `bin`, `*.g.cs`, `*.g.i.cs`, XAML-generated files), the local package
  folder `.nuget`, the ESP32 firmware and its libraries under `MOBAdisplay/esp32`, and Spec Kit managed assets.
  Slice 1 lists them explicitly and checks that no tracked product file is excluded by accident.
- **Mechanical change only.** The baseline commit is produced by the tool with the decided rules. A finding that
  the tool cannot fix without changing meaning is left out and recorded, not fixed by hand in the same change.
- **Line endings.** The baseline must not change line endings: the working tree uses CRLF, the index LF.
  `scripts/Test-LineEndings.ps1 -Staged` runs before the commit.

## Independently reviewable delivery slices

1. **Rules and exclusions.** Repeat the inventory with the pinned SDK on Windows, including the files without a
   final newline; take the two decisions; apply them to `.editorconfig`; document the local command in
   [AGENTS.md](../AGENTS.md) and the developer documentation. No source file is reformatted.
2. **Baseline.** One pull request that contains only the tool's output for the decided scope. It starts when no
   other code pull request is open (quiet window, see risks) and is merged before any other code work resumes.
   Review compares the diff statistics with the measured inventory and spot-checks files; the test suites and
   analyzer gates must be unchanged.
3. **Gate, documentation and completion.** Add the `dotnet format ... --verify-no-changes` step to a mandatory
   lane of Quality CI, show that it fails on a deliberately misformatted file in a throwaway branch, replace the
   RF-10 anchor in the umbrella plan with a delivery note and remove this plan.

## Validation strategy

- Slice 1: instruction consistency and documentation link checks; the gate command run locally with the result
  recorded on #451.
- Slice 2: portable and Windows suites, the cross-platform, Windows and Android analyzer gates with unchanged
  totals, the Android Release lane (the change touches `MOBAsmart/**`), `Test-LineEndings.ps1 -Staged`, and a
  second run of the format command that reports no change.
- Slice 3: `actionlint`, a failing run on a misformatted file and a passing run on main.
- No application, device or hardware is started.

## Dependencies and coordination

- RF-06 (analyzer baseline) is complete; its baselines are path- and message-based, so a pure whitespace change
  should leave them untouched. Slice 2 verifies that instead of assuming it.
- RF-07 ([#360](https://github.com/ahuelsmann/MOBAflow/issues/360)) records the formatting lane in its last
  slice; RF-07 therefore completes after RF-09 and this package.
- RF-30 ([#452](https://github.com/ahuelsmann/MOBAflow/issues/452)) moves files and renames namespaces. The two
  packages must not run at the same time; whichever runs second starts from the other's merged result.
- Slice 3 edits a workflow file. A cloud session cannot push workflow changes; that slice needs a local session.

## Risks

- **Quiet window.** The baseline touches most C# files. Every open code branch would conflict, including Codex
  branches such as #294 (localization). Slice 2 therefore requires that no other code pull request is open, and
  Chief Project Manager allocates the window in the single queue.
- **Hidden behavior change.** Whitespace inside verbatim or raw string literals and in test data is content, not
  formatting. The tool does not touch string contents, but slice 2 reviews every changed file under `Test/TestData`
  and `Test/TestFile` and any file with raw string literals in the diff.
- **Blame noise.** The baseline commit is listed in a `.git-blame-ignore-revs` file so that history tools can
  skip it.
- **Tool version drift.** `dotnet format` ships with the SDK; a different SDK can format differently. The gate
  uses the SDK pinned in `global.json`, and an SDK update re-runs the baseline check in its own pull request.
