# RF-30: Repository and test layout

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/452
**Spec Kit**: Not applicable - repository and project layout without product behavior or persisted-format change; reclassify before implementation if a renamed namespace is part of a persisted or wire format
**Status**: Slices 1 and 2 delivered; Slice 3A in progress (`tools/FadrEncodingAnalysis/` extracted and pre-split test baseline recorded); remaining Slice 3B+ test-project split and Slices 4–6 follow

## Purpose and scope

[Issue #452](https://github.com/ahuelsmann/MOBAflow/issues/452) owns acceptance and live status.
The [umbrella plan](QUALITY-AND-REFACTORING-PLAN.md) owns programme dependencies. This is the single RF-30 child plan.

Target: a new developer finds the architecture, build and test entry points from `README.md`; each test project
builds for its own targets; CI runs every test project; root namespaces and folder names follow one convention.
No product behavior, setting, persisted format or wire contract changes.

## Inventory

Recorded on main `4ae44d75` (2026-11-14).

| Area | Today | Sequence step |
| --- | --- | --- |
| Agent tooling inside `docs/` | Resolved by slice 1: `docs/engineering/` (17 files) and `docs/productivity/` (5 files) moved to `.agents/references/` | 1 |
| Office files in `docs/` | Resolved by slice 1: `FeedbackWorkflow.pptx`, `Journey-Editor.xlsx`, `MOBAnews.docx` moved to `assets/office/` | 1 |
| `docs/` top level | Resolved by slice 2: 14 developer Markdown files moved to `docs/developer/` and 2 user/legal Markdown files (`HARDWARE-DISCLAIMER.md`, `THIRD-PARTY-NOTICES.md`) to `docs/user/`; `docs/wiki/` (10 files) remains the canonical user wiki; `docs/index.html` with `docs/assets/` remains the site entry page; `README.md` links architecture, build and test entry points | 2 |
| Test project | one `Test/Test.csproj` that targets `net10.0` and `net10.0-windows10.0.22621.0`, removes files per target with `Compile Remove`, references MOBAflow only for Windows and MOBAsmart only with `IncludeMobaSmartTests=true` | 3 |
| Test folders (tracked files) | `SharedUI` 83, `Backend` 73, `MOBAdisplay` 22, `Domain` 22, `Common` 21, `Helpers` 19, `MOBAflow` 14, `Speech` 11, `MOBApi` 11, `TrackPlanRenderer` 10, `WinUI` 7, `Contracts` 7, `Architecture` 5, `Unit` 4, `Integration` 4, others 11 | 3 |
| `Test/Analysis` | Resolved by slice 3A: `FadrEncodingAnalysis.csproj` and `Program.cs` moved to `tools/FadrEncodingAnalysis/`; `Analysis` exclusions removed from `Test/Test.csproj` | 3 |
| Root namespaces that differ from the project name | `MOBAflow` uses `Moba.WinUI`, `MOBAsmart` uses `Moba.MAUI`, `MOBAdisplay` uses `Moba.Display`; of the 18 projects in `Moba.slnx`, 3 differ and 15 match | 4 |
| Folder names | `MOBAflow/Converter` (26 files) but `MOBAsmart/Converters` (9); `Backend/Interface` (25) and `SharedUI/Interface` (27) are consistent with each other | 4 |
| Single-host types in `SharedUI` | desktop only: `ViewModel/DisplayViewModel.cs`, `ViewModel/RecorderPageViewModel.cs`, `ViewModel/InterlockingControlViewModel*.cs`; mobile: `SharedUI/Mobile/` already holds 20 files; a complete host-usage list is part of slice 5 | 5 |

Configuration that names paths and therefore follows every move: the three analyzer baselines under `quality/`
(18,215 lines in total), `Test/coverage-thresholds.json`, `MutationTest/mutation-lanes.json` with six Stryker
configurations, `scripts/Get-QualityChangeScope.ps1`, [Quality CI](../.github/workflows/quality.yml) (16 lines that
name `Test`), the architecture tests under `Test/Architecture`, and the repository map in
[AGENTS.md](../AGENTS.md) with the instruction files that carry `applyTo` path patterns.

## Decisions

- **Order.** Documentation first (slices 1 and 2), because it has no build impact; then the test split; namespaces
  and folders last, because they touch the most files.
- **Test split (proposal, confirmed in slice 3).** Three projects: portable tests (`net10.0`: Domain, Contracts,
  Common, Backend, SharedUI, MOBApi unit tests, architecture tests), Windows desktop tests (`net10.0-windows`:
  MOBAflow, WinUI, Speech providers, Skia rendering) and integration tests (owned MOBApi process, live hub). Shared
  helpers move to one test-support library that all three reference. The `IncludeMobaSmartTests` switch and
  `Test/MOBAsmart/MobiLanHttpClientFactoryTests.cs`, which describe a graph that cannot build (noted at RF-25
  closure), are removed here.
- **No test is lost.** Slice 3 records the discovered and executed test counts per target before the split and
  proves the same totals afterwards; the result guard `scripts/Test-TestRunResults.ps1` keeps its minimum counts.
  Pre-split baseline recorded on `main` `6e3fe309` (2026-11-26, `-c Release -p:IncludeMobaSmartTests=false`):
  - `net10.0` (portable + `MOBAdemo` + `MOBApi` integration): `3,723` discovered, `3,722` executed (`3,722` passed, `0` failed; `[Explicit]` `SustainedRefresh_MeetsAcceptanceThresholds` skipped by test runner). Analyzer baseline: `3,135` diagnostics in `1,136` groups.
  - `net10.0-windows10.0.22621.0` (Windows desktop): `3,686` discovered, `3,685` executed (`3,685` passed, `0` failed; `[Explicit]` `SustainedRefresh_MeetsAcceptanceThresholds` skipped by test runner). Analyzer baseline: `2,426` diagnostics in `965` groups.
- **Namespaces.** Root namespaces become `Moba.MOBAflow`, `Moba.MOBAsmart` and `Moba.MOBAdisplay` only if slice 4
  confirms that no persisted or wire format carries the old names (type names in JSON, XAML class names in
  resources, SignalR hub or discovery identifiers). If one does, that name is kept and recorded, without a
  compatibility layer for the rest.
- **No compatibility shims.** Moved types take the namespace of their new folder; no type forwarding, aliases or
  duplicate files (no released users).
- **Baselines move, they are not refreshed.** Analyzer baseline entries, coverage thresholds and mutation lane
  inputs follow their files with unchanged counts and floors. A slice that would need a raised count stops.

## Independently reviewable delivery slices

1. **Agent tooling and Office files out of `docs/`.** Delivered 2026-11-21: `docs/engineering` and `docs/productivity`
   now live in `.agents/references/`, the three Office files in `assets/office/` (maintainer decision
   2026-11-21). Scope was: move `docs/engineering` and `docs/productivity` to an agent tooling location outside
   product documentation and move the three Office files to a non-documentation folder
   (or remove those that are obsolete, by the maintainer's decision); fix every link and the checks that read
   these paths (`scripts/Test-AiRepositorySetup.py`, documentation link check).
2. **Developer and user documentation.** Delivered 2026-11-22: the 14 developer Markdown files now live in
   `docs/developer/` and `HARDWARE-DISCLAIMER.md` and `THIRD-PARTY-NOTICES.md` in `docs/user/`; `docs/wiki/`
   remains the canonical user wiki, and `README.md` links to architecture, build and tests. Scope was: group the
   top-level files of `docs/` into developer and user sections, keep `docs/wiki/` as the canonical user wiki, and
   make `README.md` the entry point to architecture, build and tests. Links, `docs/index.html` and the
   documentation consistency facts follow.
3. **Test projects by target.** Split `Test/Test.csproj` as decided, move `Test/Analysis` out, update Quality CI,
   the change-scope script, coverage thresholds and mutation lanes. One pull request per moved group if the diff
   becomes too large to review.
4. **Namespaces and folder names.** Align the three root namespaces and `Converter`/`Converters`; update XAML
   `x:Class` and `xmlns` declarations, analyzer baselines and architecture tests.
5. **Single-host types in `SharedUI`.** Move desktop-only and mobile-only ViewModels and services into `Desktop`
   and `Mobile` folders with matching namespaces; add an RF-20 architecture rule that MOBAsmart does not use
   `Desktop` types and MOBAflow does not use `Mobile` types.
6. **Documentation and completion.** Repository map in [AGENTS.md](../AGENTS.md), `docs/developer/ARCHITECTURE.md`,
   instruction `applyTo` patterns, the delivery note in the umbrella plan, removal of this plan.

## Validation strategy

- Slices 1 and 2: documentation link check, instruction consistency, AI repository setup check, documentation
  consistency; no .NET build.
- Slices 3 to 5: portable and Windows suites with recorded test counts, MOBAflow compile, Android Release lane,
  the three analyzer gates with unchanged totals, the mutation lanes whose inputs moved, and the architecture tests.
- No application, device or hardware is started; XAML namespace changes are proven by compilation, and the visual
  states are recorded as not inspected.

## Dependencies and coordination

- RF-20 (architecture rules) is complete and gains the rules of slice 5.
- RF-26 ([#290](https://github.com/ahuelsmann/MOBAflow/issues/290)) decides the final layout of `Common`,
  `Contracts` and `SharedUI`. It is the hard start condition of slices 3 to 5 (as the umbrella plan's dependency table
  records), because they would otherwise move the same files twice. Slices 1 and 2 do not
  depend on it.
- RF-10 ([#451](https://github.com/ahuelsmann/MOBAflow/issues/451)) is complete; its whitespace gate in
  `Portable tests on Linux` also checks the files that the code slices of this package move.
- RF-07 ([#360](https://github.com/ahuelsmann/MOBAflow/issues/360)) is complete; when slice 3 changes the CI lanes, it
  updates the deliverable inventory in [Build Performance](../docs/developer/BUILD-PERFORMANCE.md#deliverables-lanes-and-required-checks).
- RF-09 ([#366](https://github.com/ahuelsmann/MOBAflow/issues/366)) added tests and mutation lanes and is complete.
- Slices 3 and later edit workflow files. A cloud session cannot push workflow changes; those slices need a local
  session.

## Risks

- **Quiet window.** Slices 3 to 5 move or rename a large share of the files. They require that no other code
  pull request is open, including Codex branches such as #294 (localization); The COO reserves the
  window on the board (a repository-wide lock). Slices 1 and 2 only need that nobody else edits `docs/` at the same time.
- **Lost or silently skipped tests.** A split can drop a fixture from every project. The before and after counts
  and the result guard address this.
- **Path-bound gates.** A moved file with an analyzer baseline entry under its old path would appear as new
  diagnostics. Entries move in the same commit as their file.
- **XAML and resources.** WinUI and MAUI resolve `x:Class` and resource keys by namespace; a miss is a compile
  error in most cases but a runtime failure for string-based lookups. Slice 4 searches for string-based type and
  resource references before renaming.
- **Agent instructions.** Skills and instruction files are mirrored between `.agents/skills` and `.claude/skills`;
  a move must change both copies and keep `scripts/Test-AiRepositorySetup.py` green.
