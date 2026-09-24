# Design decisions

- Reuse SemanticTurnoutCommandService and SemanticTurnoutRuntimeCoordinator; remove the reservation engine/coordinator rather than disable them. Existing services own mapping, ordering, feedback, duplicate commands and disconnects.
- Retain runtime FIFO and immutable snapshots. Ignore completions from replaced coordinators, cancel before disposal waits, and publish outside state locks. Reject duplicate observations before changing the block-feedback cache.
- Remove global synchronization as a direct-command gate; keep connection, target mapping and pending-command checks. Unknown/occupied blocks remain visible.
- Retain direct signals and pure block observations. Delete route-only automatic signal effects; introduce no new command path or expiry timer.
- No workflow reservation action/external reservation endpoint exists in the inspected baseline. #133 changes shared DI/schema/fixtures; preserve its action-list model on integration.

Independent read-only runtime research confirmed concurrency risks and existing test coverage. No unresolved technical/product questions.
