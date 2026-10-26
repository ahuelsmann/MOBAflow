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
- [X] T009 Once a permitted common stand with PR #293 exists: reconcile the removed `RefreshProjectRuntimeSnapshot` without adding an owner parameter, and validate Journey add/delete, edits of project A while B is selected, editor changes and the retained wrappers through the corrected session path (combined portable and Windows test validation)
- [X] T010 Re-run CI and the Sonar quality gate on the combined head; update [analysis.md](analysis.md)
- [X] T011 Close #390 only after T009 and T010; until then the PR references #390 without closing it

## Review corrections (planned 2026-10-24 before the corrective code)

- [X] T012 Extend spec, plan, tasks and analysis for the two review findings (FR-007, FR-008, FR-009)
- [X] T013 Add deterministic regression tests: a queued save writes the state current when it may write; edits during a pending Save As are saved to the chosen file and the solution is not reported saved early; Save As runs on the UI dispatcher
- [X] T014 Take the snapshot inside the write lock; record and check the edit version for every save; request a follow-up save after a stale manual save; open the Save As picker on the UI thread (FR-007, FR-008, FR-009)
- [X] T015 Run validation: light checks now; portable and Windows test suites and analyzer gates only in a centrally allocated local window after the #293 build, otherwise through CI (done in the window allocated on PR #394, comment 6388299721)
- [X] T016 Push to PR #394; CI green and the Sonar quality gate at zero OPEN/CONFIRMED issues; resolve the two review threads with the evidence

## Second review corrections (2026-10-25)

- [X] T017 Extend spec, plan and analysis for the second review findings (FR-010)
- [X] T018 Add regression tests: an edit before the Save As completion reaches the UI thread is written to the chosen file; New writes the edits made while its picker was open before it replaces the solution; Save As completes only once the later edit is written
- [X] T019 Decide the completion on the UI thread and let a manual save write until its file holds the latest edit (FR-010)
- [ ] T020 Push to PR #394; CI green and the Sonar quality gate at zero OPEN/CONFIRMED issues; resolve the three review threads with the evidence
