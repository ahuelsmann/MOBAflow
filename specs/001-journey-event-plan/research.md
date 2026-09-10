# Design decisions: Journey event plans

**Date**: 2026-09-10

| Decision | Rationale | Alternatives considered |
| --- | --- | --- |
| Session counters plus per-run baselines | Continuous counts and simultaneous journeys need independent starting values. | Per-stop resets contradict the accepted scope; shared baselines mix journeys. |
| Independent event rows | Sparse counts and different ports need no placeholders or nested phases. | Sequential gating would reintroduce coupling; a graph adds unnecessary editor complexity. |
| Nullable new plan alongside legacy sequence | Old repeat counts have different semantics. | Reusing, summing or silently copying fields would change behavior. |
| Explicit legacy adoption creates an empty plan | User controls the semantic change and retains source data. | Deleting the legacy path breaks existing projects. |
| Workflow drops, row reorder and equivalent commands | These gestures directly reduce assignment/organization work and remain accessible. | Drag-only editing excludes keyboard users; counter dragging obscures relative-count semantics. |
| User owns railway coordination | Every activation counts regardless of train identity. | Train detection/collision avoidance are outside the request. |

The accepted conversation resolves material behavior. Remaining implementation choices use existing repository services and contracts; future counting modes are deferred.
