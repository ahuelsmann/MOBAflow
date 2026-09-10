# Runtime and editor contract

Use existing runtime and gateway boundaries, preserving local/remote routing. Remote views observe the owning host's counters rather than creating separate counters.

In this increment, mobile start/stop/counter-reset execute locally only when no remote session owns the view. An active remote session returns a clear unsupported-operation error directing the user to the MOBAflow host; it must never silently invoke the local runtime. Existing remote command routes are unchanged.

| Operation | Behavior |
| --- | --- |
| Accepted activation | Increment matching InPort once, then evaluate active runs against their baselines. |
| Read counter snapshot | Stable snapshot; unknown ports are zero. |
| Start journey | Atomically capture counters and register active run; reject changed journey/shared-workflow definitions while another event-plan run is active. |
| Stop journey | Prevent further dispatch; preserve totals and existing workflow cancellation behavior. |
| Reset counters | Reject during any active journey; otherwise clear only on explicit user command. |

For each enabled row, `relative count = session count(port) - starting count(port)`. Dispatch once when its positive threshold is reached, marking dispatch before awaiting work. No historical catch-up, phase inference or train attribution is performed. Equal-condition rows use stored order.

| Editor action | Result |
| --- | --- |
| Add / workflow drop on add target | Create row, preselecting the dropped workflow where present. |
| Select / drop workflow on row | Assign or replace only that row's workflow. |
| Move controls / row drag | Change display order, preserving trigger values. |
| Duplicate | Copy settings to a new identity. |
| Delete | Remove row. |
| Adopt event plan | Explicitly create an empty plan and preserve legacy reference. |

All edits obey the active-run guard and existing observable/auto-save behavior. Unsupported drop payloads produce no mutation. Drop handlers translate input; ViewModels own behavior. Labels use English and explain `Count since start` and independent triggers.

Project/editor activation is deferred while an event-plan run remains active. Starting an unchanged journey still works; changed or newly added definitions become startable after all event-plan runs stop and pending definitions are applied. Existing active runs and session counters are preserved throughout. The shared journey template hides its legacy feedback editor for event-plan journeys and directs the operator to Event Manager.
