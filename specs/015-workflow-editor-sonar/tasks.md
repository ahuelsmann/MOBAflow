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
- [ ] T010 Normalize/check text with scripts/Test-LineEndings.ps1, run scripts/Test-SpecKitGovernance.ps1, review scoped diffs and scan changed files before a normal fix commit to existing Draft #388 (FR-005).
- [ ] T011 Verify exact new Linux/Windows CI and Sonar green plus OPEN/CONFIRMED zero; perform convergence assessment and source/process-END handoff using specs/015-workflow-editor-sonar/quickstart.md (FR-005, SC-002).

## Dependencies and execution order

T001 -> T002 -> T003 -> T004 -> T005 -> T006 -> T007 -> T008 -> T009 -> T010 -> T011.
One writer executes these tasks serially. Read-only test-case/acceptance research can be considered
independently, but no parallel agent or writer is needed for this narrow scope. The minimum useful
increment is US1 with its regression and compilation evidence; US2 prepares the still-open manual
acceptance gate. Completion here never means actual UI acceptance, Ready, review or integration.
