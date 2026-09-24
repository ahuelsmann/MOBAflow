# Data model: Journey event plans

## Persisted configuration

| Entity | Fields | Meaning |
| --- | --- | --- |
| `Journey` | `IsActive: bool`, `EventPlan`, stops, `BehaviorOnLastStop` (`None`, `BeginAgainFromFistStop`) | Active journeys evaluate their events on incoming feedback. |
| `JourneyEventPlan` | `Events: List<JourneyEvent>` | Stored presentation order also orders equal-condition events. |
| `JourneyEvent` | `Id: Guid`, `InPort: uint`, `Count: ulong`, `WorkflowId: Guid?`, `Enabled: bool` | One positive session count on one valid port with an existing workflow. |

Identity survives edit/reorder and changes on duplication. New rows default to enabled. Missing workflows are non-executable drafts. Validate the port range (1-512) and require a positive count.

## Runtime-only state and invariants

- Session counters map each port to its accepted activation total; absent ports read as zero.
- A reset sets all counters to zero and starts a new counter generation; activations from an older generation are ignored.
- Each journey's runtime state holds its current stop, run identity and last feedback time. The current stop is checkpointed and restored when the project is re-applied.
- The runtime evaluates a copy of the project. Changing the active flag or the event plan re-applies the project.
- Stop changes come only from explicit workflow actions.
- The runtime service supplies its session-counter instance to manager creation, even when the manager factory was provided externally.
