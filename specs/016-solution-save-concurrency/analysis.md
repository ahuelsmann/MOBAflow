# Analysis and convergence: Solution save and runtime refresh without races

**GitHub Issue**: #390

**Chronology**: Written on 2026-10-24 after the implementation, against PR #394 head `142e6b69` over main `f2f388d3`.
It is a post-implementation consistency check, not an earlier `/analyze` run.

## Consistency of spec, plan and tasks

| Requirement | Plan step | Task | Evidence |
| --- | --- | --- | --- |
| FR-001 own generation and path | 1, 2 | T002 | `QueuedSaveOfAnEarlierSolution_...`: only `a.json` with A's projects |
| FR-002 no state change for an earlier generation | 3 | T002 | same test: path `b.json`, saved, not dirty |
| FR-003 snapshot on the UI thread | 2 | T003 | `Save_TakesItsSnapshotOnTheUiThread_...` with a queuing dispatcher |
| FR-004 runtime copy before the lock | 5 | T005 | `UpdateProjectAsync_CopiesTheProjectWhenCalled_...` |
| FR-005 owner routing | 4 | T004 | `LateEditOfAnEarlierProject_...` |
| FR-006 unchanged file format | 2 | T002 | the copy uses the file's own `JsonOptions.Default`; no schema change |

No requirement lacks a task, and no task lacks a requirement. The `MainWindowViewModel.Solution.cs` part is out of scope
by the CPM disposition and tracked by T009.

## Review findings on `eeba64f3` (2026-10-24)

| Finding | Cause | Requirement | Planned correction |
| --- | --- | --- | --- |
| P1 save order (`PRRT_kwDOMgU7j86v6uLP`) | snapshot before the write lock; continuations race for it | FR-007 | plan step 6 |
| P2 Save As edit loss (`PRRT_kwDOMgU7j86v6uLV`) | snapshot before the picker; manual completion ignores later edits; pathless auto-saves write nothing | FR-008 | plan step 7 |
| Related: picker off the UI thread | the save continuation runs on the thread pool after the snapshot await | FR-009 | plan step 8 |

Both findings are real; the earlier tests did not cover queued saves racing each other or edits during Save As.

### Correction and evidence (2026-10-25)

The correction follows plan steps 6-8 without deviation. It was validated in the serial local window that the Chief
Project Manager allocated on PR #394 (comment 6388299721), after the #293 build window ended. All runs used
`-nodeReuse:false` and `-p:UseSharedCompilation=false`.

| Requirement | Test | Before the correction (`eeba64f3` `SolutionSession.cs`) | After |
| --- | --- | --- | --- |
| FR-007 | `QueuedSave_WritesTheStateCurrentWhenItMayWrite_NotAnOlderSnapshot` | failed | passed |
| FR-008 | `EditDuringSaveAs_IsSavedToTheChosenFile_AndTheSolutionIsNotReportedSavedEarly` | failed | passed |
| FR-009 | `SaveAs_OpensThePickerThroughTheUiDispatcher` | failed | passed |

- **The red-before check:** the earlier `SolutionSession.cs` was put back only for the test run; afterwards the file was
  restored and checked to be identical to the commit.
- **A flaw in the first version of the FR-009 test:** it compared against the test's start thread, which changes after
  its first `await`. It now compares against the thread that runs the UI action. It failed again on the old code and
  passed in 10 of 10 runs of `SolutionSessionTests` with the correction.
- **Unrelated failure:** one portable run failed in `RecordingSessionServiceTests.Stop_Should_BeSingleShotAndReturnTheSameArtifact`
  (Backend recording code that this PR does not touch). It passed 10 of 10 times in isolation and in the next full run.

## Second review findings (2026-10-25)

| Finding | Cause | Requirement | Correction |
| --- | --- | --- | --- |
| Codex P2 on `44a1b23f` (`PRRT_kwDOMgU7j86wExO_`) | the edit version was compared before the UI completion was queued; an edit in between, without a path yet, was reported as saved and not written | FR-010 | plan step 9 |
| Claude review on `f849f92f` (`PRRT_kwDOMgU7j86wExWR`) | the follow-up of a manual save was a separate save; New could replace the solution before its snapshot ran | FR-010 | plan step 9 |
| Claude review on `f849f92f` (`PRRT_kwDOMgU7j86wExSx`) | the Save As test read state that the concurrent follow-up could already clear | test only | already corrected in `f849f92f` (the follow-up write is held); Portable tests on Linux passed on that head |

| Test | Before (`f849f92f` `SolutionSession.cs`, 5 runs) | After (10 runs) |
| --- | --- | --- |
| `EditBeforeTheSaveCompletesOnTheUiThread_IsWrittenToTheChosenFile` | failed 5 of 5 | passed 10 of 10 |
| `EditDuringSaveAs_IsSavedToTheChosenFile_AndTheSolutionIsNotReportedSavedEarly` (now also asserts that Save As completes only after the follow-up write) | failed 5 of 5 | passed 10 of 10 |
| `NewSolution_SavesTheEditsMadeWhileItsPickerWasOpen_BeforeReplacingTheSolution` | failed 2 of 5: the old order depends on a thread-pool race | passed 10 of 10 |

The New test runs the test thread as the UI thread: dispatcher actions and UI continuations share one queue, as on a
real UI thread. With the correction the order is serial, so the test is deterministic.

Validation on the correction (2026-10-25):
- `SolutionSessionTests` passed 24 of 24.
- Portable suite: 3039 passed. Windows suite: 3144 passed. Both ran with a hang guard.
- Analyzer gates match without baseline changes (cross-platform 3208, Windows 2455).

## Convergence (current state)

- **Review corrections, validated locally on the correction head:**
  - `SolutionSessionTests` 22 passed on both target frameworks;
  - portable suite 3037 passed and Windows suite 3142 passed, both with a hang guard;
  - MOBAflow FastDebug build: 0 warnings;
  - analyzer gates match without baseline changes (cross-platform 3208, Windows 2455), after an NUnit2045 finding was fixed in code.
- **CI and Sonar on the pushed correction head:** pending, see PR #394.

- **Implemented and verified locally on `142e6b69`:**
  - portable suite 3034 passed and Windows suite 3136 passed, both with a hang guard;
  - MOBAflow FastDebug build: 0 warnings;
  - analyzer gates match without baseline changes;
  - Backend mutation host: 765 passed.
  - Each of the four characterization tests failed on the previous code.
- **CI on `142e6b69`:** all checks green, SonarCloud zero OPEN/CONFIRMED issues.
- **Combined with PR #293 (integrated as #401, 2026-10-26):**
  - T009: #401 removed `MainWindowViewModel.RefreshProjectRuntimeSnapshot` without an owner parameter. Journey add and
    delete in `JourneysPageViewModel` change the tracked project's collections; `ProjectViewModel` raises its count
    properties, and `SolutionSession.OnTrackedModelPropertyChanged` refreshes the runtime of the owning project and
    auto-saves. `JourneysPageViewModelTests.JourneyCrudPreservesDefaultsSessionSelectionAndSavedModels` and
    `StationAndJourneyEditsAutoSaveAndRefreshTheCurrentRuntime` cover add, delete and editor changes;
    `SolutionSessionTests.LateEditOfAnEarlierProject_RefreshesThatProjectsRuntime` covers an edit of project A while B
    is selected. The station and train pages still refresh the selected project, but only from synchronous commands
    on that same project, so they cannot target another project.
  - T010: on the combined head `cf861879` the portable and Windows suites, SonarCloud and the other required checks
    passed. The Windows suite first hit its time limit because a new #401 DI test used an empty dispatcher mock for
    the shutdown save; the test now runs dispatcher work and the suite passes.
  - T011: #390 is closed by the PR that records this convergence.
- **Known costs:** every save serializes twice (snapshot and file).
- **Practical acceptance:** none required beyond the automated tests. An optional manual check: open another solution
  while an auto-save is pending, and confirm that both files keep their own projects.
