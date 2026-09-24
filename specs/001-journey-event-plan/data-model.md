# Data model: Journey event plans

## Persisted configuration

| Entity | Fields | Meaning |
| --- | --- | --- |
| `Journey` | `IsActive: bool`, `EventPlan`, stops | Active journeys evaluate their events on incoming feedback. |
| `JourneyEventPlan` | `Events: List<JourneyEvent>` | Stored presentation order also orders equal-condition events. |
| `JourneyEvent` | `Id: Guid`, `InPort: uint`, `Count: ulong`, `WorkflowId: Guid?`, `Enabled: bool` | One positive session count on one valid port with an existing workflow. |

Identity survives edit/reorder and changes on duplication. New rows default to enabled. Missing workflows are non-executable drafts. Validate the port range (1-512) and require a positive count.

## Runtime-only state and invariants

- Each application owns its session counters independently. They map each port to its accepted activation total; absent ports read as zero. Mobile counters are never synchronized with the PC.
- A reset sets all counters to zero and starts a new counter generation; activations from an older generation are ignored.
- Each journey runtime state holds its current stop, correlation identity and last feedback time. The checkpoint stores only stop and correlation identities. Re-applying a project resolves the stop by identity, preserving it across reordering; a removed stop falls back to the first stop.
- There is no completed state or automatic restart. Advancing beyond the last stop is a no-op and does not affect rule matching. Counter reset does not change the current stop.
- The runtime evaluates a copy of the project. Changing the active flag or event plan updates only that journey configuration without cancelling accepted workflows.
- Stop changes come only from explicit workflow actions.
- The runtime service supplies its session-counter instance to manager creation, even when the manager factory was provided externally.
