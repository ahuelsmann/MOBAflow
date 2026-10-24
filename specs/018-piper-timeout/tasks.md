# Tasks: The Piper timeout covers the whole run

**GitHub Issue**: #391

**Spec Kit**: Required

**Chronology**: The checked tasks T001-T004 were done in `941806e6` before these artifacts existed; they are recorded
here afterwards.

Input: [spec.md](spec.md), [plan.md](plan.md).

## Delivered in PR #395 (`941806e6`)

- [X] T001 Write the tests with a batch-file stand-in (`PiperProcessRunnerTests`); confirm that the timeout test hangs on the previous code
- [X] T002 Start the timeout before the stdin write and kill the process tree on timeout or cancellation (FR-001, FR-002, FR-003)
- [X] T003 Run the validation: Windows test suite with a hang guard, analyzer gates, mutation lane registration and guards, documentation, line-ending and secrets checks
- [X] T004 Draft PR #395 with CI green and the Sonar quality gate at zero OPEN/CONFIRMED issues on `941806e6`

## Remaining

- [X] T005 Record the Spec Kit artifacts (this directory) and link them from #391 and PR #395
- [ ] T006 Optional practical check with a configured Piper: announcements behave as before (no acceptance gate is defined for this defect)
