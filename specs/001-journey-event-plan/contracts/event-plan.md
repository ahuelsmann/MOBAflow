# Runtime and editor contract

Use existing runtime and gateway boundaries. Each application owns its InPort counters for its own lifetime; MOBAflow and MOBAsmart do not synchronize their counts.

Mobile counter display and reset always use the local runtime, including while a MOBAflow connection is active. A reset waits for the runtime command and projects the confirmed snapshot; failures retain the displayed counts and show an error.

| Operation | Behavior |
| --- | --- |
| Accepted activation | Increment the matching InPort once, then evaluate the events of all active journeys. |
| Read counter snapshot | Stable snapshot; unknown ports are zero. |
| Reset counters | Clear all counts on explicit user command; allowed at any time. |
| Reset journey | Cancel the journey's running workflows and return it to its first stop; counters are unchanged. |
| Activate project | Re-create journey evaluation from the saved active flags and event plans; keep counters and checkpointed stops. |
| Update journey events | Copy the addressed journey's active flag and event plan, including current workflow definitions for future executions; preserve accepted workflows and their definitions, stop state and interlocking. |

An enabled event runs its workflow when `session count(port) == event count`. Workflows of one journey run in order; different journeys are independent. No historical catch-up, phase inference or train attribution is performed. Equal-condition rows use stored order.

| Editor action | Result |
| --- | --- |
| Add / workflow drop on add target | Create row, preselecting the dropped workflow where present. |
| Select / drop workflow on row | Assign or replace only that row's workflow. |
| Move controls / row drag | Change display order, preserving trigger values. |
| Duplicate | Copy settings to a new identity. |
| Delete | Remove row. |
| Active switch | Persist the journey's active flag and update only its event configuration in the runtime. |
| Edit count text | Validate the draft while typing; commit on Enter/focus loss as one undoable change. |

All edits follow the existing observable/auto-save behavior. Unsupported drop payloads produce no mutation. Drop handlers translate input; ViewModels own behavior. Labels use English.
