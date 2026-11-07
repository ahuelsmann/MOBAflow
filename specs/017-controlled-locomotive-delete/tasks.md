# Tasks: A controlled locomotive cannot be deleted

**GitHub Issue**: #389

**Spec Kit**: Required

**Chronology**: The checked tasks T001-T006 were done in `0136b5d7` before these artifacts existed; they are recorded
here afterwards. T008 is the remaining practical acceptance.

Input: [spec.md](spec.md), [plan.md](plan.md).

## Delivered in PR #392 (`0136b5d7`)

- [X] T001 Add `RuntimeLocomotiveControl` over `IProjectRuntimeSnapshots` with UI-thread change notification (FR-001, FR-004, FR-005)
- [X] T002 Guard `CanDeleteLocomotive` and `DeleteLocomotive`; add `IsDeleteLocomotiveBlocked` and `DeleteLocomotiveBlockedReason` (FR-002, FR-003)
- [X] T003 Show the reason in an `InfoBar` on `LocomotivesPage.xaml`; register the service in MOBAflow DI
- [X] T004 Add the tests: `LocomotivesPageViewModelTests` (speed 0, driving, uncontrolled cases, another project, release) and `RuntimeLocomotiveControlTests` (initial snapshots, no event on speed changes, dispose)
- [X] T005 Run the validation: portable and Windows test suites, MOBAflow build, analyzer gates, documentation, line-ending and secrets checks; update the user wiki
- [X] T006 Draft PR #392 with CI green and the Sonar quality gate at zero OPEN/CONFIRMED issues on `0136b5d7`

## Remaining

- [X] T007 Record the Spec Kit artifacts (this directory) and link them from #389 and PR #392
- [ ] T008 Practical acceptance W6 in #365 with the maintainer (Z21, locomotive, launch approval), in Light and Dark; record the result on #389

## Review corrections (2026-10-25)

- [X] T009 Add regression tests that fail on the previous code: `AddressChange_UpdatesTheDeleteState_WithoutARuntimeSnapshot`, `LateEventOfAnOlderSnapshot_DoesNotReplaceTheCurrentState`, `SnapshotPublishedWhileTheCurrentOnesAreRead_IsNotLost`, `TrainControlViewModelConnectionTests.Connecting_RequestsTheLocomotiveSelectedWhileDisconnected`
- [X] T010 Notify the delete state on address edits; refresh from the current snapshots one at a time and subscribe first; request the selected locomotive on connect (FR-006, FR-007, FR-008)
- [X] T011 Run the validation (portable and Windows suites, analyzer gates); push; answer and resolve the five review threads
