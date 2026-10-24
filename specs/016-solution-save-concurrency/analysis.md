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

## Convergence (current state)

- **Implemented and verified locally on `142e6b69`:**
  - portable suite 3034 passed and Windows suite 3136 passed, both with a hang guard;
  - MOBAflow FastDebug build: 0 warnings;
  - analyzer gates match without baseline changes;
  - Backend mutation host: 765 passed.
  - Each of the four characterization tests failed on the previous code.
- **CI on `142e6b69`:** all checks green, SonarCloud zero OPEN/CONFIRMED issues.
- **Not converged yet:**
  - T009: combined validation with PR #293.
  - T010: CI on the combined head.
  - T011: closure of #390.
- **Known costs:** every save serializes twice (snapshot and file).
- **Practical acceptance:** none required beyond the automated tests. An optional manual check: open another solution
  while an auto-save is pending, and confirm that both files keep their own projects.
