# Tasks: Solution save and runtime refresh without races

**GitHub Issue**: #390

**Spec Kit**: Required

**Chronology**: The checked tasks T001-T007 were done in `142e6b69` before these artifacts existed; they are recorded
here afterwards. T008-T011 are the remaining work.

Input: [spec.md](spec.md), [plan.md](plan.md).

## Delivered in PR #394 (`142e6b69`)

- [X] T001 Write characterization tests that fail on the previous code: `QueuedSaveOfAnEarlierSolution_NeitherWritesTheLoadedProjectsNorRestoresItsPath`, `Save_TakesItsSnapshotOnTheUiThread_AndWritesThatSnapshot`, `LateEditOfAnEarlierProject_RefreshesThatProjectsRuntime`, `UpdateProjectAsync_CopiesTheProjectWhenCalled_NotAfterWaitingForActivation`
- [X] T002 Add the solution generation and the completion guard in `SolutionSession` (FR-001, FR-002)
- [X] T003 Take the save snapshot and raise `SolutionSaving` on the UI thread; write the copy (FR-003)
- [X] T004 Record the owning project at tracking time and refresh its runtime (FR-005)
- [X] T005 Copy the editor project before the activation lock in `MobaRuntimeService`; keep the signal-box chain on the UI context (FR-004)
- [X] T006 Run the validation: portable and Windows test suites with a hang guard, MOBAflow build, analyzer gates, Backend mutation host, documentation, line-ending and secrets checks
- [X] T007 Draft PR #394 with CI green and the Sonar quality gate at zero OPEN/CONFIRMED issues on `142e6b69`

## Remaining

- [X] T008 Record the Spec Kit artifacts (this directory) and link them from #390 and PR #394
- [ ] T009 Once a permitted common stand with PR #293 exists: reconcile the removed `RefreshProjectRuntimeSnapshot` without adding an owner parameter, and validate Journey add/delete, edits of project A while B is selected, editor changes and the retained wrappers through the corrected session path (combined portable and Windows test validation)
- [ ] T010 Re-run CI and the Sonar quality gate on the combined head; update [analysis.md](analysis.md)
- [ ] T011 Close #390 only after T009 and T010; until then the PR references #390 without closing it
