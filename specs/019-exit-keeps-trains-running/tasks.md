# Tasks: Trains keep running when MOBAflow closes

**GitHub Issue**: #426

**Spec Kit**: Required

Input: [spec.md](spec.md), [plan.md](plan.md).

- [X] T001 Record the behavior criteria (spec.md) and the callers of the shared teardown (plan.md) before implementing
- [X] T002 Add `IProjectRuntimeHost.ReleaseAsync`; give `RemoveCoreAsync` an explicit `stopLocomotives` argument; host disposal sends no stop (FR-002, FR-003, FR-004)
- [X] T003 Replace `ISolutionSession.StopRuntimesAsync` with `ReleaseRuntimesAsync` (FR-002)
- [X] T004 Remove the stop-trains confirmation from `MainWindowViewModel.PrepareForShutdownAsync` (FR-001, FR-005)
- [X] T005 Tests with fake Z21s: `ProjectRuntimeHostTests` (release and disposal send no stop, connections closed) and `MainWindowViewModelShutdownTests` (no dialog, one release) (SC-001, SC-002, SC-003)
- [X] T006 Update `docs/wiki/MOBAFLOW-USER-GUIDE.md`, `docs/wiki/INSTALLATION.md`, `docs/ARCHITECTURE.md` and the note in `specs/008-project-runtimes/spec.md` (FR-006)
- [X] T007 Run the validation: FastDebug build, portable and Windows tests, analyzer gates, line-ending and secrets checks
- [ ] T008 Draft PR with CI and the Sonar quality gate at zero OPEN/CONFIRMED issues
- [ ] T009 Practical check on a real Z21 by the maintainer while using the app: trains keep running after MOBAflow closes (not part of this delivery)
