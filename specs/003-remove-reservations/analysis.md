# Cross-artifact consistency

FR-001 -> T004/T007/T008/T010; FR-002 -> T003/T004/T005/T009; FR-003 -> T006/T007; FR-004 -> T005/T006/T011; FR-005 -> T009/T010; FR-006 -> T003/T005/T006/T008/T009/T011; FR-007 -> T013.

No unresolved constitution/scope conflict. Pure block occupancy remains. Retained naming is not a disabled reservation implementation. No workflow/counter/journey redesign.

Requirements checklist: 7 checked, 0 unchecked. No extension hooks configured. Manual UI checks remain a reported pending gate and do not authorize app start.

## Standards review

Compared `128a2225...120946aa`, then reviewed the corrections. One actionable finding: the deliberately non-cancellable fake in the project-switch regression needed bounded waits and guaranteed release before disposal. Added explicit time limits and `finally` cleanup. No other concrete standards violations or relevant new code smells were found.

## Specification review

One initial finding: early turnout confirmations could be discarded while dispatch was pending. The runtime now preserves those observations until dispatch completes, without blocking occupancy/disconnect handling. The follow-up review found that observation replay must not replace a dispatch failure with success; the result now preserves the original failure/rejection. All three regression cases (success, disconnect, dispatch failure) pass in the Windows Release focused suite. The final independent follow-up found no further objections.

Standards: one finding corrected, zero open. Specification: one initial and one follow-up finding corrected, zero open. Remaining platform, integration, CI and manual gates are tracked in `validation.md`.

## SonarCloud follow-up

PR #149 at `312fe50c` reported S3776, two S2583 findings and S8949 in `InterlockingRuntimeService`. The S2583 conditions were not constant at runtime: queued callbacks populated captured locals before the caller resumed. Queue operations now return typed results, retaining accepted-work completion and project identity guards. Dispatch start and completion are separate methods, reducing control-flow complexity without changing observation ordering or token lifetime. Disposal explicitly uses `CancellationToken.None` when waiting for a command that has already been cancelled, so resource cleanup cannot be skipped.

Existing dispatch/confirmation/failure/disposal and activation-after-commit tests remain; an additional regression checks that pre-cancelled activation preserves the project and leaves the queue usable. No rule suppression or baseline expansion was introduced. GitHub SonarCloud must confirm the corrected commit; local code analysis is not used.

SonarCloud closed those four findings on `1dc88864` and reported S4586 for the nullable dispatch-task sentinel. The start helper now always returns a task whose transition result may be absent after a project switch. The queue still returns that task without awaiting hardware; the caller awaits it outside the queue and state lock. All 49 focused regressions pass again, and the portable analyzer baseline remains unchanged.
