# Data model: Journey event plans

## Persisted configuration

| Entity | Fields | Meaning |
| --- | --- | --- |
| `Journey` | Nullable `EventPlan`; unchanged `FeedbackSequence` and stops | Absent plan uses legacy behavior; present empty plan has no event triggers. |
| `JourneyEventPlan` | `Events: List<JourneyEvent>` | Stored presentation order also orders equal-condition events. |
| `JourneyEvent` | `Id: Guid`, `InPort: uint`, `Count: ulong`, `WorkflowId: Guid?`, `Enabled: bool` | One positive count since start on one valid port with an existing workflow. |

Identity survives edit/reorder and changes on duplication. New rows default to enabled. Missing workflows are non-executable drafts. Validate port range using the existing supported range and require a positive threshold.

## Runtime-only state and invariants

- Session counters map each port to its accepted activation total; absent ports read as zero.
- Each run stores baseline counts, active state and dispatched event identities. Keep the run's plan stable against later configuration changes.
- Preserve authoritative current-stop runtime projection; do not serialize a run as resumable movement.
- Start captures fresh baselines; stop prevents further dispatch and releases reset protection without clearing totals.
- Coordinate start/reset so no reset can invalidate an active baseline.
- Each event dispatches at most once per run, including after workflow failure.
- Stop changes come only from explicit workflow actions in event-plan mode.
- Legacy feedback fields remain intact through load/save/adoption.
- Deferred project updates never replace active runs. While another event plan runs, changed journey/shared-workflow definitions cannot be started from a stale runtime copy; unchanged journeys can start normally.
- The owning RuntimeService explicitly supplies its session-counter instance to manager creation, even when the manager factory was provided externally.
