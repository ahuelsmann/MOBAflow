# Tasks: Issue #378 quality corrections and acceptance preparation

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/378
**Input**: [spec.md](spec.md), [plan.md](plan.md)

## Phase 1: Analysis and setup

- [x] T001 Qualify owner release, d6aa332c/f2f comparison and the bounded plan in specs/015-workflow-editor-sonar/plan.md (FR-005).
- [x] T002 Analyze specs/015-workflow-editor-sonar/spec.md, plan.md and tasks.md against the constitution before code (FR-001 to FR-005).

## Phase 2: US1 - Preserve behavior and clear quality findings

Independent test: exact summary strings at branch boundaries, existing selection/layout/notification
tests, WinUI compile and unchanged analyzer baselines.

- [x] T003 [US1] Pin missing summary boundary cases in Test/SharedUI/WorkflowViewModelTests.cs before refactoring (FR-003).
- [x] T004 [US1] Simplify summary getters in SharedUI/ViewModel/Action/AnnouncementViewModel.cs, ChangeJourneyStopViewModel.cs and PowerShellActionViewModel.cs (FR-001, FR-003).
- [x] T005 [US1] Correct callbacks, resource-key literals, status selection and command guards in MOBAflow/Controls/Workflow/WorkflowGraphCanvasControl.xaml.cs (FR-001, FR-002).

## Phase 3: US2 - Prepare practical acceptance

Independent test: every acceptance step has a commit/build reference, verified safety prerequisites,
expected result and unexecuted evidence fields. Startup gaps are named, never guessed away.

- [x] T006 [US2] Inspect existing MOBAflow/App.xaml.cs, startup/solution-loading/configuration callers and document actual isolation constraints in specs/015-workflow-editor-sonar/quickstart.md; do not launch (FR-004).
- [x] T007 [US2] Complete numbered acceptance steps, build identity/path and evidence fields in specs/015-workflow-editor-sonar/quickstart.md (FR-004, SC-003).

## Phase 4: Validation and delivery

- [x] T008 Run focused tests, WinUI compile and unchanged analyzer gates for Test/Test.csproj and MOBAflow/MOBAflow.csproj; record exact results in specs/015-workflow-editor-sonar/quickstart.md (SC-001).
- [x] T009 Assess docs/ARCHITECTURE.md, docs/PROJECT-REFERENCE.md and docs/wiki/MOBAFLOW-USER-GUIDE.md against the correction and record corrections or a concrete no-change reason in specs/015-workflow-editor-sonar/quickstart.md (SC-003).
- [x] T010 Normalize/check text with scripts/Test-LineEndings.ps1, run scripts/Test-SpecKitGovernance.ps1, review scoped diffs and scan changed files before the historical f5eba239 fix commit to existing Draft #388 (FR-005).
- [x] T011 Verify historical f5eba239 Linux/Windows CI and Sonar green plus OPEN/CONFIRMED zero; complete the quality-phase convergence and handoff using specs/015-workflow-editor-sonar/quickstart.md (FR-005, SC-002). These receipts do not qualify the next head.

## Dependencies and execution order

T001 -> T002 -> T003 -> T004 -> T005 -> T006 -> T007 -> T008 -> T009 -> T010 -> T011.
One writer executes these tasks serially. Read-only test-case/acceptance research can be considered
independently, but no parallel agent or writer is needed for this narrow scope. The minimum useful
increment is US1 with its regression and compilation evidence; US2 prepares the still-open manual
acceptance gate. Completion here never means actual UI acceptance, Ready, review or integration.

## Phase 5: US3 - Correct effective graph theme contrast

Independent test: compile the theme-bound styles, retain the existing behavior regressions and
have the human inspect Dark -> Light -> Dark on the new exact-head candidate. The f5 Light
failure WF388-LIGHT-01 remains historical evidence, not a new-head result.

- [x] T012 [US3] Update specs/015-workflow-editor-sonar/spec.md, plan.md and tasks.md and perform actual pre-code cross-artifact analysis (FR-006 to FR-009). Checked 9 requirements, 4 success criteria, 17 tasks and all six principles before code; no blocking findings or unmapped requirements.
- [x] T013 [US3] Apply graph-local ThemeResource styles to generated visual roles and selection in MOBAflow/Controls/Workflow/WorkflowGraphCanvasControl.xaml and its .xaml.cs, preserving commands, geometry and labels (FR-006, FR-007).
- [x] T014 [US3] Compile MOBAflow/MOBAflow.csproj with protected-input exclusion, run affected Test/Test.csproj regressions and unchanged complete analyzer gates; record actual results in specs/015-workflow-editor-sonar/quickstart.md (SC-001, SC-004). Actual 2026-10-24: WinUI 0 warnings/errors, portable 106 and Windows 108 tests passed; unchanged analyzer comparisons 3211/1125 and 2456/946.
- [x] T015 [US3] Correct historical/current acceptance statements and document the native MUI payload and docs/wiki assessment in specs/015-workflow-editor-sonar/quickstart.md (FR-008, FR-009, SC-003).
- [ ] T016 [US3] Normalize/scan changed files, run scripts/Test-SpecKitGovernance.ps1, perform convergence and publish a normal fix to the same Draft #388 after fresh ownership/CI preflight; verify exact-head CI/Sonar (FR-005, SC-002).
- [ ] T017 [US3] Prepare the new committed-head candidate under artifacts/issue378-acceptance/ with fresh configuration/fixtures, all 172 native MUI and source/target hash evidence, then hand off explicit writer/process-END to Chief PM (FR-008, FR-009, SC-004).

Execution is serial: T012 -> T013 -> T014 -> T015 -> T016 -> T017. No parallel writer is needed.
Manual Light/Dark acceptance belongs to Root/the human after this handoff and is a separate gate;
T017 records preparation and handoff only, not passed visual acceptance or Ready.

This file is the pre-publication source checkpoint. T016/T017 close through the exact committed
head's CI/package/explicit-END receipts in the handoff; their unchecked state here does not grant
or claim successful publication, launch, acceptance or integration.

## Phase 6: Convergence

After the source implementation and local checks, no additional product code was missing.
The remaining committed-head delivery depends on publication and is tracked explicitly below;
the actual UI acceptance remains Root's separately authorized manual gate.

- [ ] T018 Complete the exact-head candidate and source/target MUI hash qualification under artifacts/issue378-acceptance/ and include current CI/Sonar and explicit source/process-END receipts in Chief PM's handoff per FR-008, FR-009 and SC-002 (partial; follow-through of T016/T017).

T016 to T018 were not completed on the Codex branch. Personal acceptance was abolished on
2026-11-06 (PR #417), so the exact-head acceptance candidate and the MUI package are no longer a
delivery gate; Light/Dark states that nobody inspected are recorded as a limitation in the PR.

## Phase 7: US4 - Claude takeover and review-thread fixes (2026-11-07)

The feature freeze was lifted on 2026-11-07. The Senior Developer continues #388 on
`claude/issue-378-graphical-workflow-editor` (cherry-picked 935db4e9, d6aa332c, f5eba239,
84412d1a from the Codex branch, original authorship kept) and supersedes Draft #388 with a new
Draft PR against `main`. The Codex branch and worktree stay untouched.

Independent test: the extent regression in Test/SharedUI/WorkflowViewModelTests.cs, the existing
workflow/library/defaults regressions, the FastDebug WinUI compile and the new PR's CI/Sonar.

- [x] T019 [US4] Resolve the #414 drift: `WorkflowViewModel`, `ProjectViewModel` and `WorkflowLibraryViewModel` now require their dependencies; the three new tests use `TestViewModelServices.Default` and `TestWorkflowLibraries.Create`. No shared ViewModel change was needed (FR-005).
- [x] T020 [US4] F1/Codex P2 (canvas extent): add `WorkflowViewModel.GetCanvasExtent()` with `TriggerNodeHeight`, `ActionNodeHeight`, `MinCanvasWidth/Height` and `CanvasMargin`; `WorkflowGraphCanvasControl.UpdateCanvasExtent()` sizes the host and its three layers in `RebuildGraph` and on `CanvasX`/`CanvasY` changes and re-renders the dot grid when the size changes. The drag clamp reads the same host size. Regression test with 24 actions and a dragged node (FR-001).
- [x] T021 [US4] F2 (keyboard focus): name the root Grid `GraphRoot` and focus it with `FocusState.Pointer` after a card press instead of the non-tab-stop UserControl (FR-002).
- [x] T022 [US4] F3 (automation name): `GetActionAutomationName` uses the title fallback and is reapplied on `Name`, `Number` and `StepLabel` changes (FR-002).
- [x] T023 [US4] Codex P2 (final delay): `RenderConnections` draws the shared delay badge below the last card's bottom port when `HasDelayAfter`; badge creation moved to `AddDelayBadge` (FR-002).
- [x] T024 [US4] F4 (docs): data-model.md lists the `WorkflowsPageLayoutSettings` changes of the whole feature instead of claiming unchanged defaults.
- [x] T025 [US4] Validate on Windows: portable and Windows focused tests, FastDebug WinUI compile, line endings, changed-file secrets scan; record results in quickstart.md (SC-001).
- [ ] T026 [US4] Publish the Draft PR superseding #388 with the finding table, checks, documentation/wiki assessment and unchecked Light/Dark limitation; Chief Project Manager decides Ready, the existing Claude plan reviews and merges (SC-002).

Execution is serial: T019 -> T020 -> T021 -> T022 -> T023 -> T024 -> T025 -> T026.
