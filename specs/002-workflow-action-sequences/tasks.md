# Tasks

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/132

- [x] T001 Confirm list model, existing context and graph replacement policy; inspect consumers and record specification/plan/consistency analysis.
- [x] T002 Replace graph domain/execution/validation with ordered actions and context propagation; update serialization and schema.
- [x] T003 Simplify shared workflow editor commands and adapt Workflows page, templates and Event Manager integration.
- [x] T004 Update meaningful regression tests and run shared tests plus affected consumer/WinUI builds.
- [x] T005 Review final diff, update architecture documentation, run line-ending and Spec Kit validation, and record convergence evidence.
- [x] T006 Attempt secrets and local Sonar checks; record capability limitations.
- [ ] T007 Operator verifies Light/Dark, keyboard, drag-and-drop and adding actions after restart.
- [ ] T008 SonarCloud passes with zero OPEN/CONFIRMED issues before review readiness.

## Validation and convergence evidence

- SharedUI and WinUI FastDebug compiled successfully; WinUI reported zero warnings/errors. Windows Release and the portable Release analyzer graph also compile. WinUI compilation explicitly excludes the operator-owned solution file from validation and copying.
- Portable Release suite: 1,664 passed, 4 skipped, 0 failures. Skips are the bundled-photo check and three live REST/SignalR integration tests. Workflow execution tests use fakes; MOBAflow was not launched.
- All five acceptance scenarios are implemented and covered at the shared-model/runtime boundary. The remaining visual acceptance is T007, including native drag-and-drop and restart behavior.
- Local Sonar authentication succeeds and changed-file secret scans are clean. Local analysis against github/main was attempted but the organization rejects agentic analysis with HTTP 403: Vortex agentic analysis is not available for this organization. Remote SonarCloud remains a review gate.
- Existing CA1812 baseline entries for NUnit fixtures instantiated through reflection follow the two replacement test filenames. Other baseline changes only remove or decrease findings; no new production finding is accepted.
- Integration with journey-event-plan PR #125 remains separate: when combining changes, preserve its ApplyJourneyStopTransition callback in ActionExecutionContextState/Create/CreateForExecution, and retain QueuedWorkflowExecution.OnCompleted after the whole action sequence. No code was imported from the other worktrees.
- Portable and Windows analyzer baseline checks pass after reductions of 75 and 48 findings respectively. Android baseline changes only propagate 28 removals in the same shared assemblies; the Android app build remains a CI check. Line endings, instruction consistency, Spec Kit governance and final diff checks pass.
