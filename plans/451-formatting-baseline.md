# RF-10: Formatting baseline and gate

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/451
**Spec Kit**: Not applicable - mechanical formatting and a CI gate without product behavior, persisted-format or architecture change
**Status**: Slice 1 (rules and exclusions) delivered: both decisions taken, `.editorconfig` and AGENTS.md updated, Windows inventory recorded. Slice 2 (baseline) needs a quiet window; slice 3 (gate) follows

## Purpose and scope

[Issue #451](https://github.com/ahuelsmann/MOBAflow/issues/451) owns acceptance and live status.
The [umbrella plan](QUALITY-AND-REFACTORING-PLAN.md) owns programme dependencies. This is the single RF-10 child plan.

Target: one agreed formatting baseline for the repository's C# code and a mandatory CI step that rejects drift with
`dotnet format --verify-no-changes`. The baseline change is mechanical: no functional or architecture work, no rule
tightening beyond what the gate needs, no analyzer baseline reduction.

## Inventory

Measured on main `4ae44d75` (2026-11-14) in a Linux cloud session with .NET SDK 10.0.114 (the pinned SDK of
`global.json` was not available there; slice 1 repeated it on Windows, see the next section).

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

### Windows measurement (slice 1, 2026-11-19)

Measured on main `27e77678` on Windows with SDK 10.0.404 (`global.json` pins 10.0.402 with `rollForward: latestPatch`,
so 10.0.404 is the pinned line's latest patch). Command, the same before and after the rule change:

```text
dotnet format whitespace --folder . --verify-no-changes --exclude .nuget MOBAdisplay/esp32 --report <scratch dir>
```

| State | Files with findings | Findings | Final newline | Indentation and spacing |
| --- | --- | --- | --- | --- |
| `insert_final_newline = false` (before) | 716 | 793 | 713 (all "delete the final newline") | 80 |
| `insert_final_newline = true` (after) | 404 | 460 | 380 (all "add the final newline") | 80 |

- Tracked C# files: 1,093 (all non-empty); 380 lack a final newline, 713 have one. The counts match the findings of the
  two runs exactly, so the final-newline rule alone decides 713 versus 380 files.
- The 80 whitespace findings do not depend on the rule. Whitespace findings by top-level folder after the change:
  `Test` 132, `MOBAflow` 72, `SharedUI` 57, `Backend` 39, `TrackLibrary.PikoA` 28, `MOBAsmart` 24, `Domain` 22,
  `MOBAdisplay` 22, `Common` 21, `TrackLibrary.Base` 17, `TrackPlan.Editor` 15, `MOBApi` 5, `Sound` 2,
  `TrackPlan.Renderer` 2, `Contracts` 1, `Speech.Piper` 1 (findings, not files).
- Exclusion check: no tracked `.cs` file lies under `.nuget`, `MOBAdisplay/esp32`, `obj` or `bin`, and none is named
  `*.g.cs`, `*.g.i.cs` or `*.Designer.cs`; no finding in either report lies under an excluded folder. The exclusions
  therefore remove no tracked product C# file. Generated code exists only in untracked `obj`/`bin` output, which a
  folder-based run on a clean checkout does not contain. Spec Kit managed assets are Markdown and scripts, outside
  `dotnet format whitespace`. Keep the two excludes (`.nuget`, `MOBAdisplay/esp32`) in the gate command.
- The tool wrote no file in either run (`git status` clean apart from the intended edits).
- The Linux cloud numbers (768 findings in 691 files) differ from Windows (793 in 716); the cause (main commit, SDK
  patch, checkout state) was not isolated. Slice 2 re-measures on the day of the baseline.

Not measured: style and analyzer findings of the other projects. `MOBAflow` (WinUI) and `MOBAsmart` (MAUI Android)
need Windows with their workloads for a workspace-based run; a cloud session can only run the folder-based
whitespace check on them.

Existing rules and tools:

- [`.editorconfig`](../.editorconfig): spaces, width 4, UTF-8, `end_of_line = crlf`, `insert_final_newline = true`
  (changed in slice 1; it was `false`),
  maximum line length 140.
- [`.gitattributes`](../.gitattributes): text is stored with LF and checked out with CRLF, with LF exceptions for
  shell scripts and Spec Kit assets.
- `scripts/Test-LineEndings.ps1` checks the line-ending state (CRLF or LF as `.gitattributes` requires) and mixed
  endings in the index; `-Fix` rewrites line endings only. It has no final-newline check. [AGENTS.md](../AGENTS.md)
  requires it after generated or scripted edits and, since slice 1, asks for a final newline in every file written.
- [Quality CI](../.github/workflows/quality.yml) has no formatting step.

## Decisions

Both decisions were taken by the maintainer on 2026-11-19 (comment on #451), on the COO's recommendation.

- **Final newline (decided: b).** `insert_final_newline = false` makes `dotnet format` delete the final newline of 688
  files, while the repository rule asks editors to preserve whatever a file has. Options: (a) keep `false` and let
  the baseline remove the newlines; (b) set `true`, the common convention of Git and most editors, and let the
  baseline add the missing ones; (c) remove the key, so the gate ignores final newlines; then nothing checks them,
  because `Test-LineEndings.ps1` has no final-newline check. Decision: (b), because diffs and tools then agree.
  `insert_final_newline = true` is set in the single `[*]` section of `.editorconfig` and the AGENTS.md sentence is
  reworded (slice 1). 380 tracked C# files lack a final newline today; slice 2 adds it.
- **Gate scope (decided: a).** Options: (a) whitespace only, folder-based, which runs on Linux for every project and needs
  no restore; (b) whitespace plus style for the portable projects, with `MOBAflow` and `MOBAsmart` checked for
  whitespace only; (c) whitespace, style and analyzers for every project, which needs the Windows lane and the
  Android workload. Decision: (a) is the mandatory gate, because it is the only variant that every
  platform can reproduce locally, and (b) is reconsidered after a few weeks without drift. Style and analyzer drift
  stays covered by the existing analyzer baseline gates.
- **Exclusions.** Generated code (`obj`, `bin`, `*.g.cs`, `*.g.i.cs`, XAML-generated files), the local package
  folder `.nuget`, the ESP32 firmware and its libraries under `MOBAdisplay/esp32`, and Spec Kit managed assets.
  Slice 1 verified that no tracked product file is excluded by accident; the gate excludes `.nuget` and
  `MOBAdisplay/esp32` explicitly (see the Windows measurement).
- **Mechanical change only.** The baseline commit is produced by the tool with the decided rules. A finding that
  the tool cannot fix without changing meaning is left out and recorded, not fixed by hand in the same change.
- **Line endings.** The baseline must not change line endings: the working tree uses CRLF, the index LF.
  `scripts/Test-LineEndings.ps1 -Staged` runs before the commit.

## Independently reviewable delivery slices

1. **Rules and exclusions (delivered).** Repeat the inventory with the pinned SDK on Windows, including the files without a
   final newline; take the two decisions; apply them to `.editorconfig`; document the local command in
   [AGENTS.md](../AGENTS.md). The developer documentation (`CONTRIBUTING.md`, `docs/BUILD-PERFORMANCE.md`) was locked
   by another open pull request and is covered in slice 3. No source file is reformatted.
2. **Baseline.** One pull request that contains only the tool's output for the decided scope. It starts when no
   other code pull request is open (quiet window, see risks) and is merged before any other code work resumes.
   Review compares the diff statistics with the measured inventory and spot-checks files; the test suites and
   analyzer gates must be unchanged.
3. **Gate, documentation and completion.** Add the `dotnet format ... --verify-no-changes` step to a mandatory
   lane of Quality CI, show that it fails on a deliberately misformatted file in a throwaway branch, replace the
   RF-10 anchor in the umbrella plan with a delivery note and remove this plan. It also documents the local command in
   `CONTRIBUTING.md` or `docs/BUILD-PERFORMANCE.md` once those files are free.

## Validation strategy

- Slice 1: instruction consistency and documentation link checks; the gate command run locally with the result
  recorded on #451.
- Slice 2: portable and Windows suites, the cross-platform, Windows and Android analyzer gates with baselines that
  are unchanged or refreshed with lower counts (the gate also fails when a count drops without a refresh), the Android Release lane (the change touches `MOBAsmart/**`), `Test-LineEndings.ps1 -Staged`, and a
  second run of the format command that reports no change.
- Slice 3: `actionlint`, a failing run on a misformatted file and a passing run on main.
- No application, device or hardware is started.

## Dependencies and coordination

- RF-06 (analyzer baseline) is complete. A baseline entry identifies diagnostics by project, target framework,
  rule and file path with a count; messages and line numbers are not part of it. A pure whitespace change keeps
  paths and should keep the counts. Because the gate fails on a lower count as well as on a higher one, slice 2
  states its baselines as unchanged or refreshed with lower counts, and verifies that instead of assuming it.
- RF-07 ([#360](https://github.com/ahuelsmann/MOBAflow/issues/360)) records the formatting lane in its last
  slice; RF-07 therefore completes after RF-09 and this package.
- RF-30 ([#452](https://github.com/ahuelsmann/MOBAflow/issues/452)) moves files and renames namespaces. The two
  packages must not run at the same time; whichever runs second starts from the other's merged result.
- Slice 3 edits a workflow file. A cloud session cannot push workflow changes; that slice needs a local session.

## Risks

- **Quiet window.** The baseline touches most C# files. Every open code branch would conflict, including Codex
  branches such as #294 (localization). Slice 2 therefore requires that no other code pull request is open, and
  the COO reserves that window on the board with a repository-wide lock.
- **Hidden behavior change.** Whitespace inside verbatim or raw string literals and in test data is content, not
  formatting. The tool does not touch string contents, but slice 2 reviews every changed file under `Test/TestData`
  and `Test/TestFile` and any file with raw string literals in the diff.
- **Blame noise.** The baseline commit is listed in a `.git-blame-ignore-revs` file so that history tools can
  skip it.
- **Tool version drift.** `dotnet format` ships with the SDK; a different SDK can format differently. The gate
  uses the SDK pinned in `global.json`, and an SDK update re-runs the baseline check in its own pull request.
