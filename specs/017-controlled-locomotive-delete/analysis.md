# Analysis and convergence: A controlled locomotive cannot be deleted

**GitHub Issue**: #389

**Chronology**: Written on 2026-10-24 after the implementation, against PR #392 head `0136b5d7` over main `f2f388d3`.
It is a post-implementation consistency check, not an earlier `/analyze` run.

## Consistency of spec, plan and tasks

| Requirement | Plan step | Task | Evidence |
| --- | --- | --- | --- |
| FR-001 definition of "controlled" | 1, 2 | T001 | `RuntimeLocomotiveControlTests.ExistingSnapshots_AreKnownFromTheStart`; `UncontrolledLocomotives_CanBeDeleted` (disconnected) |
| FR-002 disabled and refused | 3 | T002 | `ControlledLocomotive_CannotBeDeletedAtSpeedZero_AndExplainsWhy`, `DrivingLocomotive_CannotBeDeleted` |
| FR-003 English reason | 3, 4 | T002, T003 | same test asserts the text; `InfoBar` binds it |
| FR-004 existing runtime interfaces | 1 | T001 | no new runtime query; DI uses `IProjectRuntimeSnapshots` |
| FR-005 UI thread, changes only | 1 | T001 | `SpeedChanges_DoNotRaiseChanged_ButANewLocomotiveDoes`; `ReleasedLocomotive_CanBeDeletedAgain_AndNotifiesTheCommand` |

Every requirement has a task and evidence. The open question about Train Control when its locomotive disappears through
another path is outside this scope and stays recorded in the spec.

## Review corrections (2026-10-25)

| Requirement | Test | Before (`fece7a2a`) | After (`76e044d0`) |
| --- | --- | --- | --- |
| FR-006 | `AddressChange_UpdatesTheDeleteState_WithoutARuntimeSnapshot` | failed | passed |
| FR-007 | `LateEventOfAnOlderSnapshot_DoesNotReplaceTheCurrentState` | failed | passed |
| FR-007 | `SnapshotPublishedWhileTheCurrentOnesAreRead_IsNotLost` | failed | passed |
| FR-008 | `Connecting_RequestsTheLocomotiveSelectedWhileDisconnected` | failed | passed |

The test helpers now replace the current snapshot before they publish it, as the runtime does.

Validation on the correction:
- Portable suite: 3042 passed. Windows suite: 3147 passed.
- Analyzer gates match (cross-platform 3210, Windows 2457). The only addition is one CA1812 entry per Test TFM for the
  new fixture.
- Two Windows-only CsWinRT1032 findings in the new test helpers were fixed in code.

The user wiki needs no change: it already says that Delete is unavailable while the runtime controls the locomotive.
The corrections make that statement hold in more cases; they add no new behavior for the user.

## Convergence (current state)

- **Implemented and verified locally on `0136b5d7`:**
  - portable suite 3038 passed and Windows suite 3140 passed;
  - MOBAflow FastDebug build: 0 warnings;
  - analyzer gates match; the only addition is one CA1812 entry per Test TFM for the new fixture.
- **CI on `0136b5d7`:** all checks green, SonarCloud zero OPEN/CONFIRMED issues.
- **Not converged yet:** T008, practical acceptance W6 in #365.
- **Overlap:** #293 also changes `MobaWinUiServiceCollectionExtensions.cs`, `TestRollingStockPages.cs` and
  `quality/analyzer-baseline.json`. Whichever PR integrates second merges main once.
