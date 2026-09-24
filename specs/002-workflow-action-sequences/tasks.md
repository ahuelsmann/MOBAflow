# Tasks

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/132

- [x] T001 Confirm list model, existing context and graph replacement policy; inspect consumers and record specification/plan/consistency analysis.
- [x] T002 Replace graph domain/execution/validation with ordered actions and context propagation; update serialization and schema.
- [x] T003 Simplify shared workflow editor commands and adapt Workflows page, templates and Event Manager integration.
- [x] T004 Update meaningful regression tests and run shared tests plus affected consumer/WinUI builds.
- [x] T005 Review final diff, update architecture documentation, run line-ending and Spec Kit validation, and record convergence evidence.
- [x] T006 Run local secrets checks; keep Sonar code analysis in GitHub CI.
- [ ] T007 Operator verifies Light/Dark, keyboard, drag-and-drop and adding actions after restart.
- [ ] T008 Sonar quality gate in SonarCloud passes with zero OPEN/CONFIRMED issues before review readiness.

## Validation and convergence evidence

- Integrated against github/main at 128a2225, including #125 and #148. EventManagerPage and EventManagerViewModel retain the accepted main implementation; the Workflows page edits action lists.
- Portable and Windows Release builds pass. Validation runs in a separate checkout created only from tracked Git commits; no operator-owned working file is imported. MOBAflow and real hardware were not started.
- Portable Release suite: 1,717 passed, 4 skipped, 0 failures. Windows Release suite: 1,775 passed, no skips or failures. The portable skips cover the bundled-photo check and three live REST/SignalR integration tests.
- Regression coverage includes ordered awaiting, delays, cancellation/failure, elapsed failure traces, dry-run isolation, source event/assignment context, concurrent invocations, manager-owned stop changes, invalid-data round trips, removable null/unknown actions, reference-safe deletion and editor ordering.
- The context factory preserves ApplyJourneyStopTransition. Queued invocations resolve the stop after their predecessor; events match absolute InPort counts. No automatic journey completion or following-journey mechanism is introduced.
- Portable analyzer baseline matches 4,172 diagnostics in 1,299 groups; Windows matches 2,976 in 1,010. Existing CA1812 reflection-only fixture entries follow the two replacement filenames; no new production findings are accepted. Android baseline changes only remove 28 shared-code findings; the Android application build is validated in CI.
- Changed-file secrets, line endings, instruction consistency, Spec Kit governance and diff checks pass. Sonar code analysis runs only in GitHub CI; a green check with zero OPEN/CONFIRMED issues at the current PR commit remains required by T008.
- Native Light/Dark, keyboard, drag-and-drop and restart acceptance remains open under T007.
