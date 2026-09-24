# Runtime and editor contract

Use existing runtime and gateway boundaries, preserving local/remote routing. Remote views observe the owning host's counters rather than creating separate counters.

Mobile counter reset executes locally only when no remote session owns the view. An active remote session returns a clear unsupported-operation error directing the user to the MOBAflow host; it must never silently invoke the local runtime.

| Operation | Behavior |
| --- | --- |
| Accepted activation | Increment the matching InPort once, then evaluate the events of all active journeys. |
| Read counter snapshot | Stable snapshot; unknown ports are zero. |
| Reset counters | Clear all counts on explicit user command; allowed at any time. |
| Reset journey | Cancel the journey's running workflows and return it to its first stop; counters are unchanged. |
| Activate project | Re-create journey evaluation from the saved active flags and event plans; keep counters and checkpointed stops. |

An enabled event runs its workflow when `session count(port) == event count`. Workflows of one journey run in order; different journeys are independent. No historical catch-up, phase inference or train attribution is performed. Equal-condition rows use stored order.

| Editor action | Result |
| --- | --- |
| Add / workflow drop on add target | Create row, preselecting the dropped workflow where present. |
| Select / drop workflow on row | Assign or replace only that row's workflow. |
| Move controls / row drag | Change display order, preserving trigger values. |
| Duplicate | Copy settings to a new identity. |
| Delete | Remove row. |
| Active switch | Persist the journey's active flag and re-apply the project to the runtime. |

All edits follow the existing observable/auto-save behavior. Unsupported drop payloads produce no mutation. Drop handlers translate input; ViewModels own behavior. Labels use English.
