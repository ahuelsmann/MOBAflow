# Implementation plan: workflow action sequences

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/132
**Spec Kit**: Required
**Branch**: codex/issue-132-workflow-action-sequences
**Base**: github/main (698c4c8b)

## Constitution Check

Pass before design and implementation: reuse platform-neutral action handlers, execution context/factory, event coordinator, MVVM wrappers, cancellation and trace services. Keep commands in SharedUI and visual input coordination in WinUI. No app launch or real layout actions. The user explicitly approved the schema/behavior break for graph workflows and will recreate them; tests must prove that discarded graph structure is never executed as a guessed linear sequence. The protected operator solution is excluded from all handling.

## Design

- Domain: Workflow contains Id, Name, Description and Actions. Remove graph steps, conditions, error policies and sequential/parallel mode. Keep existing typed action payloads and per-action DelayAfterMs. Array order is authoritative; Number is a display ordinal only.
- Backend: replace graph traversal/validation with sequence validation and a sequential executor. Preserve cancellation, fail-fast semantics, effect planning, trace retention and caller interfaces. Retain lifecycle event field names for recorder compatibility; StepId identifies an action. Use a fresh context for each invocation and pass source IEvent data through the existing context/factory. Existing halt-change handler updates subsequent action context.
- SharedUI: simplify WorkflowViewModel to actions. WorkflowLibrary owns selected action and stable add/delete/reorder commands; only names refresh the filtered workflow catalog. Duplicate action payloads and IDs, retain reference-safe deletion and diagnostics.
- WinUI: library/actions/properties layout; typed Add action menu, real list reordering, general settings, empty-state guidance and collapsed diagnostics. Remove graph templates/selectors and obsolete contextual graph authoring from Event Manager without importing the other worktree.
- Persistence: update schema and repository-owned test fixtures. Earlier graph definitions retain identity/metadata but load without actions and fail validation until explicitly rebuilt. Never inspect or modify MOBAflow/solution.json.

## Validation Strategy

Regression tests cover serialized ordered actions, prior graph rejection by empty-list validation, typed payloads, actual sequential awaiting, cancellation, first failure, dry-run side-effect isolation, generic event context, concurrent invocation isolation, stop changes, duplicate/reference safety, selection reentrancy and saved reorder behavior. Run affected portable tests and then the relevant portable suite; compile WinUI FastDebug and the API consumer. Tests use fakes only. Prevent WinUI build targets from copying or validating the protected solution file. Check line endings, diffs and Spec Kit governance. Attempt changed-file secrets scans and local Sonar against github/main; authentication and remote gates remain explicit if unavailable. No Draft PR while required Sonar authentication is unavailable.

## Cross-artifact analysis

Every acceptance scenario maps to a task and behavioral regression test. The data break is explicitly approved; no unresolved migration decision remains. Existing unrelated event-plan and UI worktrees are read-only references and are not copied. No dependency upgrades, architecture-layer changes or new event catalogue are required.

## Complexity Tracking

No additional orchestration abstraction is introduced. Existing event/trace contracts are retained to avoid unrelated recorder changes.
