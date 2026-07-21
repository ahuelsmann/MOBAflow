# Workflow 2.0 Implementation Plan

## Document status

- Status: In progress (Slices 0-5 complete)
- Primary issue: https://github.com/ahuelsmann/MOBAflow/issues/32
- Runtime integration prerequisite: Satisfied by https://github.com/ahuelsmann/MOBAflow/issues/43
- Status and acceptance criteria source: GitHub issue #32

## Purpose

This plan defines the technical design, delivery sequence, dependencies, risks, and validation strategy for Workflow 2.0. GitHub issue #32 owns committed scope, priority, status, and acceptance criteria. This document owns implementation details and remains one-to-one with that issue.

Workflow 2.0 replaces the current action-list executor with a validated, deterministic, cancellable, observable workflow graph. EventManagerPage becomes the primary contextual authoring surface while WorkflowsPage remains a library-oriented view over the same workflow models and ViewModels.

## Current foundation

- `Domain/Workflow.cs` stores an ordered `List<WorkflowAction>` and one workflow-wide sequential or parallel execution mode.
- `Domain/WorkflowAction.cs` uses typed payload properties, but `WorkflowActionJsonConverter` still contains legacy `parameters` merge behavior.
- `IWorkflowService`, `IActionExecutor`, and `IWorkflowActionHandler` do not accept `CancellationToken`.
- `WorkflowService` handles only sequential and staggered parallel action lists. Its delays use `Task.Delay` without cancellation or `TimeProvider`.
- Workflow errors are exposed through a local .NET event rather than structured, correlated EventBus lifecycle events.
- Action handlers directly reach Z21, sound, speech, PowerShell, display, and journey state, so calling them during dry-run cannot be made safe by a flag alone.
- `JourneyManager` holds a processing semaphore while applying feedback delay and executing the complete workflow.
- `ProjectValidator` validates feedback-to-workflow references but not workflow structure, action payloads, reachability, recursion, or execution conflicts.
- `EventManagerViewModel` edits journey feedback occurrences and assigns existing workflows. Workflow CRUD and action commands are owned by `MainWindowViewModel` and WorkflowsPage.
- `ProjectViewModel` creates the authoritative `WorkflowViewModel` collection for a project; both workflow pages must continue to use those instances rather than rebuilding competing wrappers.
- Existing tests cover typed action payloads, basic sequential execution, one stop-on-failure option, action handlers, and feedback-triggered execution. They do not cover graph validation, branches, retry bounds, nesting, cancellation, dry-run isolation, traces, or EventBus lifecycle contracts.

## Scope boundaries

### Included

- the current Workflow 2.0 domain and JSON schema;
- typed action, delay, condition, parallel, nested-workflow, and termination steps;
- graph and reference validation before execution;
- deterministic sequential and parallel execution semantics;
- bounded retry and nested-workflow policies;
- cancellation and `TimeProvider` propagation;
- dry-run effect planning without invoking side-effect handlers;
- correlated EventBus lifecycle events and a bounded read-only trace projection;
- safe workflow CRUD, duplication, assignment, and deletion-reference checks;
- one shared workflow library/editor ViewModel used by EventManagerPage and WorkflowsPage;
- autosave and reopen coverage for graph order and references;
- accessibility, keyboard, drag-and-drop, and theme validation for affected UI;
- direct updates to the current schema and sample solution.

### Excluded

- implementation of the ordered Z21 pipeline from issue #43;
- RecorderPage, TimetablePage, AutomationTestbenchPage, interlocking, or ESP32 protocol UI;
- a general-purpose scripting or string expression language for workflow conditions;
- persistence of execution traces inside `solution.json`;
- unbounded loops, recursion, retries, branches, or trace retention;
- a second Workflow 1.x executor, compatibility adapter, or new schema migration framework;
- remote workflow-control APIs unless separately added to issue #32 scope and secured by the MOBApi authentication work package.

## Technical decisions

### 1. Persist workflows as ordered directed graphs

Each workflow contains:

- a stable workflow ID;
- a stable entry-step ID;
- an editor-ordered list of steps;
- a workflow-level default error policy;
- explicit execution limits.

The list order is the canonical editor and serialization order. Execution follows stable step-ID edges rather than list position. This preserves exact authoring order while allowing reachability, branches, explicit termination, and deterministic serialization.

General graph cycles are invalid. Repetition is expressed only through the bounded retry policy. Nested workflow calls are checked separately for cross-workflow recursion.

### 2. Use typed step kinds

The initial step kinds are:

| Step kind | Required data | Normal successor behavior |
| --- | --- | --- |
| Action | Existing typed `WorkflowAction` payload | Continue to `NextStepId` |
| Delay | Non-negative duration | Wait through `TimeProvider`, then continue |
| Condition | Typed condition and true/false target IDs | Select exactly one branch |
| Parallel | Ordered branch entry IDs and one join target | Launch branches in declared order, join deterministically |
| Nested workflow | Referenced workflow ID | Execute child, then continue |
| Terminate | Success, cancelled, or failed result | End explicitly |

Step polymorphism must use an explicit discriminator and schema definitions. Unknown discriminators or payloads are validation errors and cannot execute.

### 3. Define deterministic branch and parallel semantics

- Sequential edges execute one step at a time.
- A condition evaluates once against the immutable execution context captured for that step.
- Parallel branches launch in their persisted order.
- A join waits for every branch unless cancellation or the declared error policy terminates the workflow.
- Aggregate branch results are reduced in persisted branch order, not task-completion order.
- Branch-local execution cannot leave the declared branch subgraph or bypass its join.
- Empty branches, duplicate branch entries, missing joins, unreachable joins, and branch overlap that makes ownership ambiguous are invalid.

### 4. Keep conditions typed and extensible

Use an `IWorkflowConditionEvaluator` registry keyed by a condition discriminator. Each condition has one typed payload and returns a deterministic boolean result from the execution context. The MVP condition set is limited to context already available through project, journey, station, feedback source, and runtime snapshot data.

Arbitrary C#, PowerShell, JSON-path, or text expressions are not permitted. Unsupported conditions fail validation before execution.

### 5. Make error-policy precedence explicit

- A step policy overrides the workflow default; absence means inherit.
- Terminal behavior is `Stop`, `Continue`, or `FailureBranch`.
- Retry is an optional bounded modifier with an additional-attempt count, a fixed delay, and an exhaustion behavior.
- Retry count is constrained to `0..10`; zero means no retry.
- Retry delay is non-negative, bounded by schema and validator limits, and uses `TimeProvider`.
- A failure branch must identify a valid step in the same workflow.
- `StopOnFirstActionFailure` maps to the current schema's default `Stop` policy during the direct schema cutover. It does not survive as an alternate runtime flag or legacy adapter.

### 6. Propagate cancellation and time through every async boundary

`CancellationToken` is added to workflow execution, action execution, every action handler, condition evaluation where asynchronous work is required, delays, nested calls, and external-effect abstractions.

Cancellation rules:

- no new step starts after cancellation is observed;
- the active handler receives the token;
- child workflows receive a linked token;
- parallel branches receive the same linked execution token;
- cancellation produces one terminal lifecycle event and a defined cancelled result;
- cancellation is not converted into a retryable action failure;
- PowerShell and other long-running external operations must be terminated or abandoned through their native cancellable boundary within a documented timeout.

Use injected `TimeProvider` for delays, retry timing, duration measurement, and deterministic tests. Wall-clock time is used only for trace timestamps.

### 7. Bound nested workflows

- Nested calls resolve by stable workflow ID from the active project snapshot.
- Static validation rejects direct and indirect call cycles across all project workflows.
- Runtime maintains a call stack as a defense in depth.
- Maximum nested execution depth is 16, including the root workflow.
- Child execution receives the parent correlation ID, its own execution ID, the invoking step ID, and the captured execution context.
- Child cancellation and failure return through the invoking step's error policy.

### 8. Reject ambiguous concurrent writes before execution

Action handlers expose a side-effect descriptor that includes controlled resource keys and effect categories. Examples include Z21 command station, locomotive address, signal or turnout identity, audio output, script process, display device, and journey state.

The validator rejects parallel branches that may write the same exclusive resource without an explicit coordination contract. This contract must remain extensible for issue #34 interlocking resources and issue #36 display capabilities without adding page-specific logic to the executor.

### 9. Make validation a mandatory execution gate

Introduce a dedicated `IWorkflowValidator` returning stable validation codes, severity, workflow ID, step ID, field path, and an English message. Validation covers at least:

- missing or duplicate workflow and step IDs;
- missing entry, successor, branch, join, failure-branch, condition, action, and nested-workflow references;
- unreachable steps and invalid termination paths;
- graph cycles and nested-workflow recursion;
- empty or overlapping parallel branches;
- retry and depth limits;
- invalid or unsupported typed payloads;
- ambiguous parallel resource writes;
- invalid deletion references from feedback steps or nested workflows.

`IWorkflowService` validates before live or dry-run execution. Invalid workflows return a non-started result and publish validation lifecycle information; no handler is invoked.

`ProjectValidator` composes the workflow validator for whole-solution diagnostics instead of duplicating workflow rules.

### 10. Separate effect planning from effect execution

Dry-run must never call `IWorkflowActionHandler.ExecuteAsync`.

Each action type provides:

- payload validation;
- an effect descriptor/planned-intent projection;
- live execution through its handler.

Dry-run evaluates graph structure, references, typed conditions, branch choices, nested calls, error-policy validity, and planned effects. Delay steps record planned duration without waiting. Live handlers remain unreachable from the dry-run path by construction, and tests assert zero calls to Z21, sound, speech, process, display, filesystem-script, and mutable journey-effect implementations.

### 11. Publish correlated lifecycle events and retain bounded traces

Lifecycle events are platform-neutral `EventBase` records published through `IEventBus`. They include:

- source event correlation ID;
- workflow execution ID and optional parent execution ID;
- workflow and step IDs;
- monotonically assigned trace sequence;
- attempt number;
- execution mode;
- UTC timestamp and elapsed duration where applicable;
- result, branch decision, validation code, cancellation, or sanitized failure detail.

Events cover workflow started/completed/cancelled/failed, step started/completed/failed, condition decision, retry scheduled, nested workflow entered/exited, and dry-run planned effect.

A platform-neutral bounded trace store keeps recent execution summaries in memory. Default retention is 100 executions and 10,000 trace entries per active project; oldest completed executions are evicted first. Trace persistence belongs to RecorderPage issue #30, not `solution.json`.

ViewModels subscribe directly to the UI-dispatched EventBus and never call `InvokeOnUi` from lifecycle handlers.

### 12. Share one editor state between both pages

Create a singleton `WorkflowLibraryViewModel` for project workflow selection, search, CRUD, duplication, validation, and save coordination. It uses the `WorkflowViewModel` instances from the selected `ProjectViewModel.Workflows` collection.

Create typed step ViewModels and a factory for property editing. Nested ViewModels propagate `PropertyChanged` to the workflow wrapper, and the existing `IProjectContext.SaveSolutionInternalAsync` boundary remains the autosave coordinator.

- EventManagerPage hosts the contextual workflow library/editor next to feedback occurrence assignment.
- WorkflowsPage is retained as a library-focused editor and uses the same `WorkflowLibraryViewModel` and selected workflow instance.
- Navigating between pages must not rebuild wrappers, duplicate selection state, or maintain competing undo stacks.
- Page code-behind remains limited to WinUI input and drag/drop adaptation. Commands, confirmation, validation, duplication, deletion, and model mutation remain in ViewModels/services.

### 13. Use safe deletion and direct schema cutover

- Deleting a referenced workflow is blocked and reports every feedback-step and nested-workflow reference.
- The user must remove or reassign references explicitly; deletion never cascades silently.
- Duplication creates new workflow and step IDs, remaps all internal graph edges, and preserves external action payload references.
- The schema and sample solution switch atomically from `actions` plus `executionMode` to `entryStepId`, `steps`, and the Workflow 2.0 policy fields.
- Do not add a Workflow 1.x deserializer, migration layer, or dual executor.
- Remove legacy converter behavior that is superseded by the Workflow 2.0 current schema; retain only typed action serialization still required by action steps.

### 14. Consume the ordered event boundary delivered by issue #43

Issue #43 is merged and establishes ordered Z21 delivery. Workflow 2.0 removes long-running delay and workflow execution from `JourneyManager`'s global feedback-processing critical section while preserving per-source ordering through a workflow execution coordinator.

Issue #32 must consume the ordered event boundary from #43 and must not implement a second Z21 queue.

## Domain and service shape

| Type or contract | Responsibility | Likely location |
| --- | --- | --- |
| `Workflow` | Entry ID, ordered steps, default policy, limits | `Domain/` |
| `WorkflowStep` hierarchy | Typed graph nodes and edges | `Domain/Workflow/` or focused `Domain/*.cs` files |
| `WorkflowCondition` hierarchy | Typed condition payloads | `Domain/` |
| `WorkflowErrorPolicy` | Retry modifier and terminal behavior | `Domain/` |
| `WorkflowExecutionRequest` | Workflow, mode, context, source correlation, token | `Backend/Interface/` |
| `WorkflowExecutionResult` | Terminal status and identifiers | `Backend/Interface/` |
| `IWorkflowValidator` | Project-wide and single-workflow graph validation | `Backend/Interface/`, `Backend/Service/Validation/` |
| `IWorkflowConditionEvaluator` | Typed condition evaluation | `Backend/Interface/`, `Backend/Service/` |
| `IWorkflowEffectPlanner` or handler descriptor contract | Payload checks, planned effects, resource keys | `Backend/Interface/`, `Backend/Service/` |
| `IWorkflowExecutionCoordinator` | Per-source ordering outside feedback critical section | `Backend/Interface/`, `Backend/Service/` |
| Workflow lifecycle event records | Observable correlated execution | `Common/Events/` |
| `IWorkflowTraceStore` | Bounded in-memory read model | `Backend/Interface/`, `Backend/Service/` |
| `WorkflowLibraryViewModel` | Shared workflow catalog/editor state | `SharedUI/ViewModel/` |
| Step ViewModels and factory | Typed editing and autosave propagation | `SharedUI/ViewModel/Workflow/` |

Exact file grouping may be adjusted during implementation, but platform-neutral types must remain outside WinUI and MAUI projects.

## Dependencies and sequencing

| Dependency | Type | Required handling |
| --- | --- | --- |
| Issue #43 / RF-04 ordered Z21 pipeline | Satisfied prerequisite for feedback runtime integration and final end-to-end acceptance | Consume the merged ordering boundary without duplicating it |
| RF-03 authenticated MOBApi control plane | Conditional | Required before exposing remote workflow-control writes; not required for the local EventManager MVP |
| RF-06 analyzer gate | Quality coordination | Do not block model research; ensure public contracts are clean under the active analyzer baseline |
| RF-09 coverage and mutation ratchets | Quality coordination | Add Workflow 2.0 tests in a form that Backend/Common/SharedUI mutation lanes can consume |
| Issue #30 RecorderPage | Downstream consumer | Coordinate event envelope and correlation fields; do not implement recording here |
| Issue #31 TimetablePage | Downstream consumer | Keep lifecycle and invocation contracts decoupled from EventManager UI |
| Issue #34 interlocking | Shared resource contract | Use extensible resource keys; do not embed route or interlocking logic in Workflow 2.0 |
| Issue #35 AutomationTestbenchPage | Downstream consumer | Expose reusable `TimeProvider`, dry-run, effect, and trace contracts; avoid a second executor |
| Issue #36 ESP32 display interface | Shared action contract | Let display actions describe device resource keys and capabilities without absorbing protocol work |

## Delivery sequence

### Slice 0: Characterization and executable contract tests

Affected tests and areas:

- current workflow and action serialization;
- sequential and parallel execution behavior;
- error reporting and `StopOnFirstActionFailure` behavior;
- feedback-triggered execution and JourneyManager locking behavior;
- WorkflowViewModel autosave propagation and project wrapper identity;
- current EventManagerPage and WorkflowsPage command ownership.

Deliverables:

- characterization tests that protect behavior intentionally retained by Workflow 2.0;
- explicit tests demonstrating current cancellation, dry-run, lifecycle, validation, and locking gaps;
- no production behavior change.

### Slice 1: Workflow graph, schema, and validator

Primary affected files:

- `Domain/Workflow.cs`;
- new focused Workflow 2.0 domain types under `Domain/`;
- `Domain/WorkflowAction.cs` and its serializer where retained by action steps;
- `Backend/Service/ProjectValidator.cs`;
- new workflow validator contracts and implementation;
- `MOBAflow/Build/Schemas/solution.schema.json`;
- `MOBAflow/solution.json` and `Test/TestFile/solution.json`;
- Domain, serializer, schema, and validator tests.

Deliverables:

- ordered directed graph model and explicit polymorphic schema;
- direct current-schema sample update;
- complete graph, reference, payload, retry, resource, reachability, and recursion validation;
- deterministic error codes with workflow/step locations;
- no executable Workflow 2.0 path until validation is complete.

### Slice 2: Cancellable action, condition, and effect contracts

Primary affected files:

- `Backend/Interface/IWorkflowService.cs`;
- `Backend/Interface/IActionExecutor.cs`;
- `Backend/Interface/IWorkflowActionHandler.cs`;
- `Backend/Service/ActionExecutionContext.cs`;
- `Backend/Service/ActionExecutor.cs`;
- `Backend/Service/WorkflowActionHandlers.cs`;
- condition evaluator and effect-planning contracts;
- handler, cancellation, resource-descriptor, and dry-run isolation tests.

Deliverables:

- `CancellationToken` through every handler and external operation;
- payload validation and planned-effect descriptions for every action type;
- resource keys for concurrency validation;
- dry-run path that cannot call live action handlers;
- injected `TimeProvider` for all orchestration timing.

### Slice 3: Deterministic executor, lifecycle events, and trace

Primary affected files:

- `Backend/Service/WorkflowService.cs` or focused executor collaborators replacing its monolithic behavior;
- `Backend/Interface/WorkflowExecutionOptions.cs`, replaced by the current execution request/policy contracts;
- new execution coordinator, call-stack, and trace-store services;
- new workflow lifecycle records under `Common/Events/`;
- `Backend/Extensions/MobaBackendServiceCollectionExtensions.cs`;
- executor, branch, retry, nesting, cancellation, EventBus, and trace tests.

Deliverables:

- validated live and dry-run execution;
- deterministic sequential, branch, parallel, join, retry, termination, and nested-call semantics;
- bounded recursion, retries, and trace retention;
- correlated lifecycle EventBus events;
- one terminal result and event for every started execution.

### Slice 4: Ordered feedback integration

Prerequisite satisfied: issue #43 is merged.

Primary affected files:

- `Backend/Manager/JourneyManager.cs`;
- runtime execution-context and correlation builders;
- journey feedback and end-to-end workflow tests;
- runtime shutdown and project-activation paths where coordinator cancellation is required.

Deliverables:

- source correlation from ordered feedback into workflow execution;
- per-source workflow ordering without holding the global feedback semaphore through delays or external effects;
- immutable captured execution context for queued work;
- deterministic cancellation on reset, project replacement, disconnect, and shutdown;
- no duplicate Z21 event queue.

### Slice 5: Shared library/editor ViewModels

Primary affected files:

- new `SharedUI/ViewModel/WorkflowLibraryViewModel.cs`;
- `SharedUI/ViewModel/WorkflowViewModel.cs`;
- new typed workflow-step ViewModels and factory;
- `SharedUI/ViewModel/EventManagerViewModel.cs`;
- `SharedUI/ViewModel/JourneyFeedbackStepViewModel.cs`;
- `SharedUI/ViewModel/MainWindowViewModel.Workflow.cs`, reduced or removed after command ownership moves;
- `SharedUI/ViewModel/ProjectViewModel.cs` and `IProjectContext` only where required for identity and save coordination;
- SharedUI ViewModel tests.

Deliverables:

- one project workflow collection and one shared editor selection;
- create, select, edit, duplicate, safely delete, validate, and assign commands;
- internal ID remapping on duplicate;
- reference-aware deletion confirmation through `IDialogService`;
- nested property changes propagate to autosave;
- exact graph order and references survive save/reopen.

### Slice 6: EventManagerPage, trace UI, and WorkflowsPage consolidation

Primary affected files:

- `MOBAflow/View/EventManagerPage.xaml` and `.xaml.cs`;
- `MOBAflow/View/WorkflowsPage.xaml` and `.xaml.cs`;
- `MOBAflow/Resources/EntityTemplates.xaml` and template selector only where typed property templates are needed;
- layout settings only if the contextual editor introduces a new resizable star-sized region;
- WinUI page resolution and binding tests.

Deliverables:

- contextual Workflow 2.0 authoring and assignment entirely within EventManagerPage;
- library-focused WorkflowsPage over the same editor state;
- validation navigation to the affected step and non-color-only accessible feedback;
- read-only correlated execution trace and dry-run controls;
- keyboard creation, selection, reorder, branch navigation, deletion, and cancellation;
- drag/drop remains a view input adapter and invokes ViewModel commands;
- English strings, `ThemeResource`, responsive layout, and persisted star sizing;
- Light, Dark, High Contrast, keyboard, focus, Narrator, and text-scaling validation.

## Pull-request sequence

| PR | Contents | Merge gate |
| --- | --- | --- |
| 1 | Slice 0 characterization tests | Existing behavior documented and green |
| 2 | Slice 1 graph model, schema, sample, validator | Schema and round-trip tests green together |
| 3 | Slice 2 cancellation and effect-planning contracts | Every action type has validation, resource, live, and dry-run tests |
| 4 | Slice 3 executor and lifecycle core | Branch/retry/nesting/cancellation/EventBus suites green |
| 5 | Slice 4 feedback integration | Existing #43 ordering boundary consumed; ordering and shutdown suites green |
| 6 | Slice 5 shared editor ViewModels | CRUD, identity, autosave, reference, and reopen tests green |
| 7 | Slice 6 WinUI integration and consolidation | WinUI build plus accessibility/theme checklist complete |

Do not combine schema cutover, executor replacement, and UI redesign in one pull request. Do not retain the superseded executor or duplicate editor path after the corresponding cutover is proven.

## Test strategy

| Area | Required automated coverage |
| --- | --- |
| Domain and serialization | Defaults, every step discriminator, stable IDs, exact order, every edge, policy limits, malformed payloads, round trip, direct schema sample |
| Validator | Missing references, duplicates, unreachable steps, graph cycles, nested cycles, empty/overlapping branches, join rules, invalid policies, unsupported payloads, resource conflicts |
| Executor | Sequential order, true/false decisions, explicit termination, parallel launch/join, deterministic reduction, error policy precedence, retry exhaustion, nested depth, cancellation at every boundary |
| Dry-run | Zero live-handler and external-effect calls, planned effects, branch decisions, nested calls, no real delay, invalid workflow rejection |
| Lifecycle and trace | Correlation hierarchy, monotonic sequence, single terminal event, sanitized failures, bounded eviction, subscriber failure isolation |
| Action handlers | Token propagation, cancellation, PowerShell termination, audio/speech/display/Z21 behavior, resource descriptors, payload validation |
| Journey integration | Ordered source correlation, repeated feedback, per-source serialization, reset, project replacement, disconnect, shutdown, no long-held global semaphore |
| SharedUI | Shared instance identity, CRUD, duplicate ID remap, deletion reference blocking, undo/redo ownership, validation navigation, autosave propagation, save/reopen |
| WinUI | Page and ViewModel resolution, compiled bindings where supported, keyboard commands, drag/drop adapters, trace and validation projection |

Use NUnit AAA tests, Moq for narrow contracts, `FakeUdpClientWrapper` for Z21 effects, fake effect planners/handlers for dry-run isolation, and a deterministic `TimeProvider` for all timing behavior. Tests must never execute real UDP, sound, speech, PowerShell, display, or filesystem-script effects.

## Validation commands

During focused development:

```powershell
dotnet test Test/Test.csproj --filter "FullyQualifiedName~Workflow"
dotnet test Test/Test.csproj --filter "FullyQualifiedName~ProjectValidator"
dotnet test Test/Test.csproj --filter "FullyQualifiedName~JourneyManagerFeedback"
dotnet build Backend/Backend.csproj
dotnet build SharedUI/SharedUI.csproj
```

Before every slice merge:

```powershell
dotnet test Test/Test.csproj
dotnet build MOBApi/MOBApi.csproj
dotnet restore MOBAflow/MOBAflow.csproj
dotnet build MOBAflow/MOBAflow.csproj -c FastDebug --no-restore /p:BuildMOBApiDependency=false /p:CopyMOBApiToOutput=false
```

Before issue closure:

- complete the applicable Windows Release build and schema-validation lane;
- run the repository coverage command and ensure thresholds do not regress;
- run active analyzer and mutation gates for affected Domain, Backend, Common, and SharedUI code;
- validate EventManagerPage and WorkflowsPage with keyboard-only input, Narrator, text scaling, Light, Dark, and High Contrast themes;
- perform a disconnected dry-run and prove zero network, hardware, audio, script, display, and mutable journey effects;
- perform an isolated fake-Z21 end-to-end run and verify source, workflow, nested execution, step, retry, and terminal correlations.

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| A broad graph model becomes an unreviewable rewrite | Deliver model, effects, executor, integration, and UI as separate buildable slices with focused contracts |
| Dry-run accidentally invokes hardware or external state | Use a separate effect-planning path; never call live handlers from dry-run; assert zero calls for every effect category |
| Cancellation stops orchestration but leaks an external process or sound operation | Propagate tokens through native APIs, add bounded termination, and test cancellation per handler |
| Parallel branches write the same controlled object | Require resource descriptors and reject exclusive-write overlap before execution |
| Retry, graph, or nested cycles create unbounded execution | Forbid graph cycles, validate nested call cycles, cap retries at 10 and nesting depth at 16, retain runtime guards |
| Completion order makes parallel results nondeterministic | Reduce results in persisted branch order and serialize trace sequencing through one execution trace sink |
| Long workflows block feedback processing | Consume the pipeline delivered by #43, capture immutable requests, and execute through a per-source coordinator outside the global feedback critical section |
| EventBus traces overwhelm UI subscribers | Publish structured lifecycle transitions only, keep bounded retention, and let trace ViewModels project filtered updates |
| EventManagerPage and WorkflowsPage mutate different wrappers | Reuse `ProjectViewModel.Workflows` and one singleton library/editor ViewModel; test reference identity across navigation |
| Autosave captures a partially edited graph | Apply graph mutations as commands, update model and wrapper atomically, validate snapshots, and serialize saves through the existing save semaphore |
| Deleting a workflow leaves dangling references | Block deletion, enumerate feedback and nested references, and require explicit reassignment/removal |
| Direct schema cutover loses old action-list data | Update schema, sample, fixtures, and code atomically; document that no migration path is provided; do not create a dual model |
| Concurrent issues also edit the shared solution schema | Rebase each schema slice, preserve unrelated definitions, and run the complete schema/sample validation lane before merge |
| Error details leak command or script data into traces | Publish stable error codes and sanitized messages; keep sensitive payloads and process output out of lifecycle events |

## Rollback strategy

- Slice 0 is tests only and can be reverted independently.
- The graph/schema cutover is one atomic change: revert domain, schema, sample, fixtures, and serializer together.
- Workflow 2.0 execution remains unreachable until validator and executor slices are both complete.
- Dry-run stays on its separate effect-planning boundary and cannot fall back to live handlers.
- Feedback integration can be disabled by reverting its coordinator wiring without reverting the platform-neutral graph or editor.
- UI consolidation occurs only after shared ViewModel tests prove identity and autosave; remove superseded MainWindow workflow commands and old page bindings in the same cutover PR.
- Do not preserve a failed slice through a hidden compatibility flag or permanent duplicate implementation.

## Documentation and completion

- Record stakeholder decisions, status, scope changes, and acceptance evidence in issue #32.
- Update `docs/ARCHITECTURE.md`, `docs/PROJECT-REFERENCE.md`, JSON schema documentation, and user guidance when the corresponding behavior is implemented.
- Document workflow graph semantics, error-policy precedence, cancellation, dry-run guarantees, correlation fields, trace retention, and resource-conflict rules as current reference documentation before closure.
- Maintain the existing `plan-required` label and one-to-one plan link in issue #32 throughout implementation.
- Close issue #32 only after its GitHub acceptance criteria and all affected validation gates pass.
- Delete this plan after issue #32 is complete and merged; the closed issue, pull requests, tests, current documentation, and Git history retain the permanent record.
