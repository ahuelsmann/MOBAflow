# Tasks: UI localization first slice

**Input**: Design documents in `specs/011-ui-localization/`.
**Source Issue**: #140

[GitHub issue](https://github.com/ahuelsmann/MOBAflow/issues/140)
**Prerequisites**: spec, clarified scope, plan, research, model, contracts and coverage.
**Tests**: Meaningful regression and resource-tool fixture tests are required by this specification.
**Status**: Slice 1 implemented and locally validated; manual acceptance and publication gates remain separate.

## Phase 1: Analysis and setup

- [x] T001 Verify dedicated branch/worktree and base; inspect applicable guidance in `AGENTS.md` and `.github/instructions/instructions-index.md`.
- [x] T002 Specify and clarify scope in `specs/011-ui-localization/spec.md` and `coverage.md`; record Microsoft research/design in `research.md`, `plan.md`, `data-model.md`, `contracts/localization.md` and `quickstart.md` (FR-001, FR-011).
- [x] T003 Explicitly amend English-only policy in `.specify/memory/constitution.md`, `AGENTS.md`, `.github/instructions/winui.instructions.md`, `maui.instructions.md` and `.specify/templates/overrides/plan-template.md` (FR-001, FR-009).
- [x] T004 Run cross-artifact analysis for `specs/011-ui-localization/spec.md`, `plan.md` and `tasks.md` before product code.

## Phase 2: Foundations

- [x] T005 Add canonical UiLanguage to `Common/Configuration/AppSettings.Sections.cs`: string, default `system`; persisted values `system`, `en-US`, `de-DE`; null/empty/malformed/unsupported normalize to `system` (FR-002, FR-004).
- [x] T006 [P] Add `SharedUI/Localization/IUiLocalizer.cs`, `UiLocalizer.cs` and policy for explicit startup language/display culture, complete-template formatting and English fallback (FR-004, FR-005, FR-006, FR-007).
- [x] T007 Author nonempty English/German shared catalogues with translator context in `SharedUI/Localization/UiStrings.resx` and `UiStrings.de-DE.resx`; declare neutral en-US in `SharedUI/SharedUI.csproj` (FR-001, FR-007).
- [x] T008 Add `Test/SharedUI/SettingsPageViewModelTests.Localization.cs` for German family/system/invalid preference, explicit override, known-key fallback, reordered templates and decimal/date formatting without changing process CurrentCulture (FR-004, FR-005, FR-006, FR-009).

Checkpoint: shared resource/policy tests pass without host types or network/hardware.

## Phase 3: US1 - Choose UI language (P1)

Independent test: fake store validates save/reload/failure, active language remains unchanged until new startup.

- [x] T009 [US1] Add `Test/SharedUI/SettingsPageViewModelTests.Language.cs` for selection/save/retry, rollback after failed save, unchanged speech/solution/undo and active language (FR-002, FR-003, FR-009, FR-012).
- [x] T010 [US1] Implement `SharedUI/ViewModel/LanguageSettingsViewModel.cs` using Toolkit commands/properties, existing ISettingsService and active IUiLocalizer; no automatic restart (FR-002, FR-003, FR-012).
- [x] T011 [US1] Preserve UiLanguage on load/save in `MOBAflow/Service/SettingsService.cs`, `MOBAsmart/Service/SettingsService.cs`; extend `Test/MOBAflow/SettingsServiceTests.cs` and `Test/Common/AppSettingsDefaultsTests.cs` for roundtrip/default boundaries (FR-002, FR-009).
- [x] T012 [US1] Apply saved language before covered view/resource construction in `MOBAflow/App.xaml.cs` and `MOBAsmart/MauiProgram.cs`; register localizer/ViewModel in both existing service extensions; use existing settings path, UI culture only and WinUI PrimaryLanguageOverride (FR-002, FR-003, FR-004, FR-009).
- [x] T013 [US1] Add isolated language settings area in `MOBAflow/View/SettingsPage.xaml`, new `MOBAsmart/Controls/LanguageSettingsView.xaml`/`.xaml.cs`, and its host in `MOBAsmart/MainPage.xaml`; no speech settings or Counter behavior changes (FR-001, FR-002, FR-003, FR-010, FR-012).

## Phase 4: US2 - Read covered interface (P1)

Independent test: all Slice 1 catalogue entries resolve in both cultures, both hosts compile;
manual acceptance covers all included surfaces separately from build evidence.

- [x] T014 [US2] Add explicit WinUI property map in `SharedUI/Localization/WinUiResourceMap.json` and deterministic projection in `scripts/Update-LocalizationResources.ps1`; generate `MOBAflow/Strings/en-US/Resources.resw` and `de-DE/Resources.resw` (FR-001, FR-007).
- [x] T015 [US2] Resource Help chrome/contents navigation and all Info text/accessibility names via x:Uid in `MOBAflow/View/HelpPage.xaml`, `InfoPage.xaml` and dynamic text in their `.xaml.cs`; preserve products/version/URLs (FR-001, FR-006, FR-009, FR-010).
- [x] T016 [US2] Localize navigation titles/tooltips through `MOBAflow/Service/NavigationItemFactory.cs`; preserve tags/custom badge suffixes and lookup ownership (FR-001, FR-009, FR-010).
- [x] T017 [US2] Add `MOBAsmart/Localization/LocalizeExtension.cs` and resource labels/accessibility names in `MOBAsmart/Controls/AppBottomTabBar.xaml`; preserve routing and existing operational pages (FR-001, FR-007, FR-009, FR-010).
- [x] T018 [US2] Build `SharedUI/SharedUI.csproj`, `MOBAflow/MOBAflow.csproj`, `MOBAsmart/MOBAsmart.csproj`; run affected/full Test targets in `Test/Test.csproj` and record unavailable checks in `specs/011-ui-localization/coverage.md` (FR-001, FR-009; SC-001, SC-002, SC-004).

## Phase 5: US3 - Extend safely (P2)

Independent test: malformed isolated fixtures are rejected with offending key; clean catalogues
and fresh projection pass. Contributor follows documented single-source workflow.

- [x] T019 [US3] Implement `scripts/Test-LocalizationResources.ps1` to reject duplicate/missing/empty keys, unknown used keys, malformed/mismatched placeholders and stale projection (FR-005, FR-007, FR-008).
- [x] T020 [US3] Add isolated functional fixtures in `scripts/Test-LocalizationResources.Tests.ps1`; cover each checker failure and valid escaped/reordered templates (FR-008; SC-003).
- [x] T021 [US3] Document language/text addition in `docs/LOCALIZATION.md` and update `specs/011-ui-localization/coverage.md` with implemented/build/manual evidence independently (FR-011).

## Phase 6: Cross-cutting validation/publication

- [ ] T022 Perform authorized manual matrix from `specs/011-ui-localization/quickstart.md`: themes, keyboard, accessible names, restart and desktop 200%/Android >=1.5 text scale; keep unchecked until actually executed (FR-010; SC-001, SC-005).
- [x] T023 Run secrets, line-ending/staged, instruction, governance and diff/link checks on changed paths in `AGENTS.md`, `.github/instructions/`, `.specify/` and `specs/011-ui-localization/`; remove temporary constitution sync comment before commit.
- [ ] T024 Publish task branch and draft PR linked to #140; record current-commit CI/Sonar results in `specs/011-ui-localization/coverage.md` without ready/merge or premature issue closure.
- [x] T025 Converge `specs/011-ui-localization/tasks.md` against implemented scope; keep manual, unavailable checks and #140 follow-up coverage explicit.

## Dependencies and implementation strategy

T001-T004 precede product code. T005-T008 establish shared state/resources; US1 precedes host
resource consumption in US2. US3 may prepare checker fixtures after catalogue/map shape exists,
but checker/freshness checks must pass before T024. T022 needs explicit app-launch authorization.
T024 does not imply manual acceptance or Ready approval. RF-24/#219 and #136 are ownership
coordination boundaries, not hard prerequisites for independent resources/Help/Info.

MVP is US1 on both hosts; the agreed Slice 1 also includes US2/US3 before review handoff.
Remaining page bodies belong to later #140 slices, not implicit completion of this task list.

## Parallel opportunities

T006/T007 can prepare distinct service/catalogue files after T005 model decisions. US1 tests
and host adapters require shared contracts first. US2 Help/Info and mobile tab markup touch
different hosts after map/localizer preparation; US3 fixture work is independent of page edits.
These are scheduling opportunities, not authorization to start extra agents or chats.

## Requirement coverage

| Requirement | Tasks |
| --- | --- |
| FR-001 | T002, T003, T007, T013-T018 |
| FR-002 | T005, T009-T013 |
| FR-003 | T009, T010, T012, T013 |
| FR-004 | T005, T006, T008, T012 |
| FR-005 | T006, T008, T019 |
| FR-006 | T006, T008, T015 |
| FR-007 | T006, T007, T014, T017, T019 |
| FR-008 | T019, T020 |
| FR-009 | T003, T008, T009, T011, T012, T015-T018 |
| FR-010 | T013, T015-T017, T022 |
| FR-011 | T002, T021 |
| FR-012 | T009, T010, T013 |

## Phase 7: Convergence

Assessment on 2026-10-15: 12 functional requirements, 5 success criteria, 10 acceptance
scenarios, 6 design decisions and all 6 constitution principles checked. No missing or
contradictory product behavior found within Slice 1. Two partial evidence gaps remain;
this is not a claim that the manual acceptance or publication gates have passed.

- [ ] T026 Complete the still-pending T022 manual matrix in `specs/011-ui-localization/quickstart.md` and record host restart/system-language, themes, font scaling, keyboard and screenreader results in `coverage.md` after explicit app-launch authorization per SC-001/SC-005/FR-010 (partial, HIGH).
- [ ] T027 Complete T024 publication evidence in `specs/011-ui-localization/coverage.md`: draft PR, current-head required CI, green SonarCloud at the same SHA and zero OPEN/CONFIRMED issues per Constitution VI (partial, MEDIUM).
