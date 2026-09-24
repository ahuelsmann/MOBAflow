# Reusable workflow action sequences

## Governance and Traceability

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/132
**Spec Kit**: Required
**Status**: Implemented; visual acceptance and remote quality gates pending

## Confirmed requirements

A workflow is a reusable, ordered list of actions. An event triggers it and supplies the context for its execution. Event Manager assigns existing workflows; the existing Workflows page edits the shared definitions. Multiple event assignments retain the same workflow ID.

### Clarifications - 2026-09-23

- The user explicitly chose to replace conditions, parallel branches and nested workflows completely, and will recreate existing graph workflows manually.
- No automatic graph flattening or compatibility execution engine is required. Retain workflow identity and metadata when reading earlier graph definitions, but do not infer actions from their graph. Such definitions have an empty action list and cannot execute until recreated.
- The operator-owned MOBAflow/solution.json must not be read, copied, edited or reset. No application launch or hardware operation is authorized.
- Reuse ActionExecutionContext and its factory. Do not introduce a second WorkflowExecutionContext abstraction.

## User scenarios and acceptance

1. Create a workflow, add different typed actions, edit their settings and reorder them. The displayed order, saved list and execution order agree. Actions are awaited sequentially; DelayAfterMs pauses after an action. No start, successor, branch, join or termination wiring is exposed.
2. Assign one workflow to two events. Both retain the same workflow ID. Each invocation receives its own context, containing the source event and available related journey/stop information. A halt-changing action updates the context seen by subsequent actions in that invocation.
3. Save and reopen a workflow. Names, typed payloads, IDs and list order survive. Duplicating creates independent workflow/action IDs and payloads. Deleting an assigned workflow is blocked with its references.
4. Validate or dry-run a workflow. Empty or malformed definitions cannot run. Dry-run invokes no external action handlers and performs no real waiting. Cancellation and the first failed action stop the sequence and produce one terminal trace event.
5. Open the Workflows page with no project, no workflows or no selected action. Clear empty states guide creation/selection. Workflow settings are reachable; diagnostics are collapsed and retain a visible issue count. Add/delete/reorder and property edits preserve valid selection and auto-save behavior.

## Scope and edge cases

Shared Domain/Backend/SharedUI behavior and Windows WinUI authoring are affected. Existing typed action executors, event assignment and source FIFO/cancellation ownership remain in place. The context accepts any IEvent; this does not add configuration screens or subscribers for event types not currently offered by Event Manager. Missing related information remains optional.

List order is authoritative even if legacy action Number values disagree. Duplicate/empty action IDs, missing payloads, negative delays, unsupported action types, stale selections and reentrant selection changes require regression coverage. Concurrent calls never store invocation data in a reusable Workflow or WorkflowAction definition.

## Success criteria

An ordinary workflow requires zero connection IDs or graph controls. Automated tests prove order, context isolation/propagation, persistence, cancellation/failure, dry-run isolation and editor commands. Relevant builds/tests pass. Personal Light/Dark, keyboard, drag-and-drop and restart checks remain explicit acceptance gates.
