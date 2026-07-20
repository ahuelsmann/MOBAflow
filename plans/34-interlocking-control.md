# Issue #34 Interlocking Control Implementation Plan

## Document status

- Status: In progress
- Primary issue: [#34 - Add turnout, route, block, and interlocking control to SignalBoxPage and TrackPlanPage](https://github.com/ahuelsmann/MOBAflow/issues/34)
- Status source: GitHub Issue #34
- Plan ownership: one plan for one issue
- Priority recommendation: P1 (high), after the P0 safety foundations
- Baseline reviewed: `985f29263f47fb34c767827655599fd5ae5d6e78`
- Plan-required: yes

GitHub owns scope, status, and acceptance criteria. This plan owns technical sequencing, dependencies, risks, validation, and delivery boundaries. Delete this file after Issue #34 is accepted and closed; Git history and the closed issue remain the durable record.

## Implementation progress

- Slice 1 completed on 2026-07-20: shared definitions and bindings, operational topology, schema version 4, structured validation, canonical address allocation, sample data, and serialization/characterization coverage.
- Slice 2 completed on 2026-07-20: derived conflict matrix, immutable revisioned state, atomic resource reservation, fail-safe observations, deterministic transitions, conservative full-route release, and cancellation lock retention.
- Slice 3 in progress: semantic turnout command sequences, live Z21 effect boundary, and recording/simulation adapter are implemented; confirmation timeout orchestration remains part of the next coordinator increment.

## Outcome

Deliver one shared, platform-neutral operational control foundation for turnouts, blocks, routes, and interlocking. TrackPlanPage and SignalBoxPage remain separate user experiences, but both read and command the same persisted definitions and live runtime state.

The implementation must fail safe. Unknown, contradictory, stale, disconnected, or partially confirmed state is never treated as safe, and protected signals remain at stop unless all route prerequisites are confirmed.

## Scope boundaries

### In scope

- Stable operational identities and bindings for turnouts, signals, blocks, routes, physical track segments, and signal-box symbols.
- Persisted route and block definitions in the current solution schema.
- Semantic turnout commands and confirmation state.
- Deterministic route validation, conflict detection, locking, setting, cancellation, release, and failure handling.
- Structured runtime events and immutable snapshots shared by both pages.
- Direct turnout control on both pages.
- Route definition, preview, operation, and release on TrackPlanPage.
- Shared route, block, turnout, and interlocking state on SignalBoxPage.
- A recording/simulation adapter that executes the production engine without hardware.
- Automated tests and maintainer-led validation on the real layout.

### Out of scope

- Merging SignalBoxPage and TrackPlanPage.
- Automatic timetable dispatching.
- Replacing the interlocking engine with Workflow 2.0.
- Live-hardware replay from RecorderPage or AutomationTestbenchPage.
- Automatic discovery of feedback, turnout, or signal mappings.
- Legacy compatibility adapters or migration pipelines.
- Progressive route release in the first delivery unless the maintainer explicitly approves its sensor semantics and safety rules.

## Verified starting point

The reviewed baseline already contains:

- Separate `Project.SignalBoxPlan` and `Project.TrackPlan` persisted aggregates.
- Signal-box elements, connections, simple route definitions, switch positions, and signal aspects in `Domain/SignalBoxPlan.cs`.
- A physical Piko track-plan document with stable segment IDs, port connections, and optional feedback inputs.
- Raw Z21 turnout set/query operations and a runtime raw-turnout command.
- Multiplex signal resolution and semantic signal commands.
- Runtime-only track occupancy, switch, and signal projection in `RailroadState`.
- A platform-neutral digital-address conflict detector covering locomotives, accessories, multiplex ranges, and feedback addresses.
- Characterization tests for signal-box aggregate invariants and track-plan runtime projection.

The baseline does not contain a block model, semantic turnout lifecycle, route state machine, conflict matrix, interlocking coordinator, resource locks, deterministic release policy, or route-oriented runtime API.

## Architectural decisions

### AD-01: Separate representations from operational authority

`TrackPlanDocument` remains the physical editing/rendering document. `SignalBoxPlan` remains the logical signal-box diagram. Neither page-specific aggregate owns interlocking truth.

Add one platform-neutral operational aggregate to `Project`, provisionally named `InterlockingDefinition`, as the sole persisted authority for:

- turnout definitions and decoder mappings;
- block definitions and feedback mappings;
- route definitions and required resource positions;
- protected signal references;
- bindings from operational objects to TrackPlan segment IDs and SignalBox element IDs.

Both page representations reference stable operational IDs. They must not duplicate route, lock, occupancy, or confirmation state. The existing `SignalBoxPlan.Routes` path is replaced by the shared operational route definition in the current schema rather than maintained as a competing compatibility model.

### AD-02: Persist definitions, not live safety state

Persist configuration and operator-authored definitions only. Requested, pending, confirmed, occupied, reserved, established, releasing, failed, unknown, fault, lock ownership, command correlation, and connection health are runtime state.

The runtime publishes immutable snapshots keyed by stable `Guid` identities. Page visibility never affects state or command execution.

### AD-03: Single-writer, revisioned state transitions

All interlocking state transitions run through one serialized coordinator. Every accepted input receives a monotonic runtime revision and correlation ID. Duplicate or older observations are ignored without regressing state. Simultaneous commands are evaluated against one snapshot in deterministic order.

RF-04 must provide the ordered Z21 publication boundary before live event integration. The pure domain engine and simulator may be implemented earlier.

### AD-04: Fail-safe occupancy semantics

The current pulse-plus-timeout projection is not sufficient for interlocking decisions. A block becomes `Free` only from an explicitly configured, current, non-contradictory clear observation. Missing data, disconnect, stale data, or incompatible observations yield `Unknown` or `Fault`, never `Free`.

Every feedback mapping declares its semantics. The first implementation supports explicit asserted/cleared occupancy sources; pulse-only inputs may drive diagnostics but cannot independently prove a protected block free.

### AD-05: Semantic turnout commands

UI and interlocking code command a stable turnout ID and requested `TurnoutPosition`; they never send decoder address/output tuples directly. A backend adapter validates the definition and maps it to the existing raw Z21 operation.

The minimum turnout lifecycle is:

`Unknown -> Requested -> Pending -> Confirmed | Failed | Unknown`

Two-way and three-way positions are modeled explicitly. A Boolean left/right event is not the semantic contract. A turnout belonging to an established, occupied, releasing, or faulted route remains locked until the coordinator releases it safely.

### AD-06: Conservative route lifecycle

The first implementation uses:

`Available -> Selected -> Setting -> Established -> Occupied -> Releasing -> Available`

Any state may enter `Failed` or `Conflicting` when applicable. Cancellation before hardware dispatch returns to `Available`. Cancellation during or after setting invokes fail-safe recovery.

The first release policy is full-route release: retain all route locks until all configured protected blocks are explicitly clear and the configured exit condition is confirmed. Progressive release is deferred.

### AD-07: No unsafe automatic rollback

After a partial command failure, the system does not automatically move already confirmed turnouts back. The protected signal remains or is commanded to stop, confirmed physical positions remain recorded, uncertain resources remain locked, and the route enters `Failed`. Reconciliation or explicit operator recovery is required before reuse.

### AD-08: Production engine with replaceable effects

Validation and state-transition decisions are pure and platform-neutral. The asynchronous coordinator depends on narrow interfaces for turnout commands, signal commands, feedback observations, time, and event publication.

Simulation uses the same coordinator with recording adapters and controllable time. It must not duplicate interlocking rules and must have no path to live Z21 or external effects. AutomationTestbenchPage may consume these interfaces later.

### AD-09: Protected signal ownership

Only the interlocking coordinator may clear a signal assigned as a protected route signal. Manual UI commands may always request a safe stop aspect. A manual proceed request for a protected signal is rejected unless it is issued through a valid established route transition.

### AD-10: Current-schema delivery

Update `Domain`, `MOBAflow/Build/Schemas/solution.schema.json`, and shipped `MOBAflow/solution.json` together. Do not add legacy readers, migrations, or parallel route representations.

## Domain model outline

### Persisted definitions

- `InterlockingDefinition`
  - `Turnouts`
  - `Blocks`
  - operational topology connections
  - `Routes`
  - representation bindings
- `TurnoutDefinition`
  - stable ID, name, decoder address, supported positions, command mapping, confirmation source
- `BlockDefinition`
  - stable ID, name, boundary/track IDs, direction, occupancy sources, safety policy
- `RouteDefinition`
  - stable ID, name, entry, ordered path, exit, turnout requirements, protected blocks, protected signals, explicit conflicts
- `OperationalBinding`
  - operational ID, optional TrackPlan segment IDs, optional SignalBox element IDs

### Runtime state

- `TurnoutRuntimeState`: requested position, confirmed position, lifecycle, lock owner, last observation, failure.
- `BlockRuntimeState`: occupancy, reservation owner, route owner, observation revision, fault.
- `RouteRuntimeState`: lifecycle, locked resources, correlation, failure/conflict explanation.
- `InterlockingSnapshot`: immutable revisioned aggregate exposed through the runtime boundary.

### Structured results

- Validation findings contain a stable code, severity, affected IDs, and actionable English message.
- Command results distinguish accepted, rejected, cancelled, timed out, failed, and reconciliation-required outcomes.
- Events carry correlation ID, runtime revision, entity ID, previous state, new state, and reason code.

## Safety invariants

1. Two routes with conflicting resources cannot both hold reservations or locks.
2. A turnout cannot move while locked by an established, occupied, releasing, failed-uncertain, or reconciliation-required route.
3. A protected signal cannot clear until the route is valid, all resources are reserved, required turnouts are confirmed and locked, protected blocks are explicitly safe, and the connection is healthy.
4. Unknown, stale, missing, contradictory, or disconnected observations cannot satisfy a safety prerequisite.
5. A command failure cannot silently advance route state.
6. Duplicate and out-of-order observations cannot roll back the accepted runtime revision.
7. Disconnect retains conservative locks and changes affected state to `Unknown` or `Fault`.
8. UI state is a projection only; closing a page cannot cancel, release, or mutate a route.
9. Simulation and validation cannot invoke a live hardware adapter.
10. Every automatic follow-up is represented by a structured event and correlation ID.

## Dependencies and start gates

| Dependency | Gate |
| --- | --- |
| Closed Issue #12 address conflict detector | Reuse in definition validation; already satisfied |
| RF-04 ordered Z21 event pipeline | Required before Slice 5 live runtime integration |
| RF-06 effective analyzer gate | Required before the UI-heavy slices |
| RF-13 track-plan editor extraction | Relevant selection/command/render-state extraction required before Slice 6 |
| RF-14 signal-box property refactoring | Relevant command/property extraction required before Slice 7 |
| RF-09 coverage and mutation ratchets | Coordinate with engine test delivery; not a domain-start blocker |
| RF-16 accessibility and theme completion | Coordinate validation; #34 still owns accessible state indicators |
| Issue #30 RecorderPage | Soft consumer of structured events; no blocker |
| Issue #31 TimetablePage | Downstream consumer for future live dispatch; no blocker for advisory MVP |
| Issue #32 Workflow 2.0 | May run in parallel; later actions must call the interlocking API |
| Issue #35 AutomationTestbenchPage | May consume simulation interfaces later; no blocker |

## Delivery slices

### Slice 1: Characterization and shared operational schema

Purpose: establish one persisted authority and protect existing signal behavior before changing it.

Implementation:

- Add characterization tests for current semantic signal commands, offline behavior, invalid mapping, and runtime/editor projection.
- Introduce the persisted operational definitions and representation bindings.
- Move route ownership from `SignalBoxPlan.Routes` to the shared definition.
- Update project validation, JSON schema, sample solution, snapshot cloning, and serialization tests.
- Extend the existing address conflict detector through the new definitions rather than duplicating validation.

Likely areas:

- `Domain/Project.cs`
- `Domain/SignalBoxPlan.cs`
- new `Domain/Interlocking/` types
- `Backend/Service/Validation/`
- `MOBAflow/Build/Schemas/solution.schema.json`
- `MOBAflow/solution.json`
- `Test/Domain/`, `Test/Backend/`, and serialization fixtures

Exit criteria:

- One persisted route/turnout/block authority exists.
- Invalid and contradictory definitions report stable actionable findings.
- Solution/schema validation and round-trip tests pass.
- Existing semantic signal tests pass unchanged or with explicitly approved current-schema updates.

### Slice 2: Pure block, turnout, route, and conflict engine

Purpose: implement all safety decisions without network, UI, timers, or hardware.

Implementation:

- Add pure validators for paths, endpoints, duplicates, contradictory positions, missing mappings, unsupported positions, and block safety policy.
- Derive conflicts from shared blocks, shared signals, incompatible turnout requirements, and explicit conflicts.
- Add immutable runtime state and deterministic transition functions.
- Add reservation and lock acquisition as one atomic domain decision.
- Return structured rejection reasons without side effects.

Tests:

- Complete conflict-matrix tests.
- Property-based or exhaustive small-topology tests for mutual exclusion and deterministic results.
- Duplicate, missing, unreachable, contradictory, disconnected, and unknown-state cases.
- Revision/idempotency tests.

Exit criteria:

- All safety invariants can be demonstrated without hardware.
- Re-running the same input sequence produces the same snapshot and results.

### Slice 3: Semantic turnout runtime and simulation adapter

Purpose: replace raw address-oriented feature calls with stable operational commands.

Implementation:

- Add a narrow semantic turnout gateway keyed by turnout ID.
- Map two-way and three-way requested positions to validated Z21 commands.
- Represent requested, pending, confirmed, failed, and unknown explicitly.
- Add command correlation, timeout, cancellation, and confirmation reconciliation.
- Add a recording gateway and controllable time source for simulation.

Tests:

- Address/mapping validation and offline rejection.
- Two-way and three-way mappings.
- Confirmation, duplicate confirmation, out-of-order confirmation, timeout, cancellation, disconnect, and failure.
- Proof that simulation invokes no live adapter.

Exit criteria:

- The same semantic command produces the same state transitions in simulation and live-adapter contract tests.
- No new UI or domain code sends decoder tuples directly.

### Slice 4: Interlocking coordinator and fail-safe route operation

Purpose: orchestrate the pure engine and replaceable effects through one serialized command path.

Implementation:

- Add select, preview, set, cancel, release, reconcile, and safe-stop commands.
- Reserve route resources atomically before hardware commands.
- Command and confirm turnouts before locking and signal clearance.
- Implement conservative full-route release.
- Implement disconnect, feedback loss, partial failure, timeout, cancellation, and operator recovery.
- Publish structured lifecycle events and immutable snapshots.

Tests:

- Successful route establishment and release.
- Conflicting concurrent requests.
- Occupied/unknown/faulted blocks.
- Turnout lock rejection.
- Signal-clear prerequisites.
- Cancellation at every state boundary.
- Partial command failure without unsafe rollback.
- Reconnect and reconciliation.
- Coordinator shutdown without deadlock or orphaned commands.

Exit criteria:

- The engine cannot clear a protected signal through any tested unsafe path.
- Every route transition is observable and correlated.

### Slice 5: Ordered Z21 integration and shared runtime boundary

Gate: RF-04 complete.

Purpose: connect the coordinator to production feedback and commands while preserving the EventBus threading boundary.

Implementation:

- Translate ordered Z21 observations into semantic turnout and block observations.
- Add an `IInterlockingRuntime` role or equivalent narrow runtime contract; keep `IMobaRuntime` as the compatibility aggregate only where required.
- Publish snapshots/events through the existing EventBus decorator boundary.
- Define initial synchronization and reconnect queries.
- Mark affected state unknown/faulted on disconnect before accepting new operations.
- Add DI registrations in backend and both relevant UI hosts.

Tests:

- FIFO observation projection, duplicates, stale revisions, reconnect snapshot, disconnect during setting, shutdown, and subscription disposal.
- Architecture tests for platform independence and absence of manual UI dispatch in EventBus handlers.

Exit criteria:

- Production and simulation use the same engine.
- Both pages can subscribe to the same immutable runtime snapshot.

### Slice 6: TrackPlanPage route editor and operation

Gate: relevant RF-13 extraction complete.

Purpose: make the physical plan the route authoring and preview surface without placing behavior in page code-behind.

Implementation:

- Add turnout selection and semantic commands.
- Add route editor mode for entry, ordered path, exit, turnout requirements, blocks, and signals.
- Add validation results and navigation to affected elements.
- Add route preview, conflict explanation, set, cancel, release, and reconciliation commands.
- Project turnout, block, occupancy, reservation, route, and fault state into renderer-neutral visual state.
- Use English text, ThemeResource values, keyboard access, accessible names, and non-color-only indicators.

Tests and validation:

- ViewModel command-state and validation tests.
- Renderer-neutral state projection tests.
- Keyboard, focus, Narrator, Light, Dark, and High Contrast checks.
- FastDebug WinUI build.

Exit criteria:

- Route authoring and operation contain no feature behavior in page code-behind.
- Unsafe commands remain disabled or return actionable rejection results.

### Slice 7: SignalBoxPage shared operation

Gate: relevant RF-14 extraction complete.

Purpose: preserve the logical signal-box workflow while sharing all operational definitions and runtime state.

Implementation:

- Bind signal-box elements to shared operational IDs.
- Add semantic turnout selection and commands.
- Show shared route, block, reservation, lock, and fault state.
- Route protected signal requests through the interlocking coordinator.
- Remove competing route state and direct operational model mutations from controls.
- Preserve separate navigation and the existing semantic signal workflow.

Tests and validation:

- ViewModel projection and command tests.
- Protected-signal rejection tests.
- Shared-state consistency tests across both page ViewModels.
- Keyboard, focus, Narrator, Light, Dark, and High Contrast checks.
- FastDebug WinUI build.

Exit criteria:

- A turnout commanded from either page immediately projects the same state to both.
- Both pages show the same route/block/lock revision.

### Slice 8: Hardware acceptance and documentation

Purpose: prove the conservative model against the real layout before accepting the issue.

Implementation and validation:

- Configure at least one normal route, one conflicting route, a protected signal, all required turnouts, and protected block feedbacks.
- Validate semantic signals, two-way/three-way turnouts as applicable, confirmation behavior, occupancy, release, disconnect, reconnect, cancellation, and partial failure.
- Capture the exact firmware/Z21/decoder configuration and observed results without credentials.
- Update current user and architecture documentation.
- Run the full affected test graph and Release/FastDebug build checks required by repository policy.

Exit criteria:

- Maintainer records successful real-layout validation in Issue #34.
- No acceptance criterion remains open.
- The implementation plan is deleted in the closing change.

## Pull-request sequence

| PR | Contents | Dependency |
| --- | --- | --- |
| 1 | Characterization tests and operational schema | Plan approved |
| 2 | Validators, conflict matrix, and pure state engine | PR 1 |
| 3 | Semantic turnout runtime and recording adapter | PR 2 |
| 4 | Interlocking coordinator and failure/release behavior | PR 2-3 |
| 5 | Ordered Z21 integration and runtime snapshots/events | RF-04 and PR 4 |
| 6 | TrackPlanPage editor/operation | RF-13 and PR 5 |
| 7 | SignalBoxPage integration | RF-14 and PR 5 |
| 8 | Accessibility polish, documentation, and hardware acceptance fixes | PR 6-7 |

PRs 6 and 7 may proceed in parallel after the shared runtime API is stable. Each PR must remain independently buildable and must not weaken fail-safe defaults to simplify UI delivery.

## Validation matrix

| Area | Automated validation | Manual validation |
| --- | --- | --- |
| Definitions/schema | validation codes, JSON schema, serialization, cloning | sample solution inspection |
| Route topology | path, endpoint, duplicate, reachability, contradiction tests | representative layout review |
| Conflicts/locks | conflict matrices, concurrent commands, invariant/property tests | conflicting route attempts |
| Turnouts | mapping, confirmation, timeout, cancellation, three-way tests | real decoder operation and feedback |
| Blocks | asserted/cleared, duplicate, stale, contradictory, reconnect tests | real detector occupancy/clear behavior |
| Signals | protected-clear prerequisite and safe-stop tests | every configured semantic aspect |
| Failure/recovery | disconnect, partial failure, shutdown, reconciliation tests | cable/network interruption and recovery |
| Shared UI | both ViewModels receive identical revision/state | operate the same object from both pages |
| Accessibility | platform-neutral presentation-state tests | keyboard, Narrator, Light/Dark/High Contrast |
| Architecture | dependency and EventBus boundary tests | code review against MOBAflow instructions |

Minimum commands before merge of affected slices:

```powershell
dotnet test Test/Test.csproj
dotnet restore MOBAflow/MOBAflow.csproj
dotnet build MOBAflow/MOBAflow.csproj -c FastDebug --no-restore /p:BuildMOBApiDependency=false /p:CopyMOBApiToOutput=false
```

Use narrower affected tests during development. Run the complete required graph before every merge and before hardware acceptance.

## Risks and mitigations

### Dual-topology drift

Risk: physical and logical pages reference different objects or duplicate route definitions.

Mitigation: one operational aggregate, explicit bindings, validation for missing/duplicate bindings, and cross-page state-consistency tests.

### False-free occupancy

Risk: a pulse timeout or missing message releases a route unsafely.

Mitigation: explicit clear semantics only; stale or missing observations become unknown/faulted.

### Ambiguous turnout confirmation

Risk: raw decoder output is treated as a stable two-way domain position or cannot represent a three-way turnout.

Mitigation: explicit supported positions and mapping per turnout, plus semantic confirmation events.

### Partial hardware failure

Risk: some turnouts move while later commands fail.

Mitigation: protected signal remains at stop, no automatic physical rollback, uncertain resources remain locked, and reconciliation is explicit.

### Event reordering

Risk: dependent feedback and command-result events arrive out of order.

Mitigation: RF-04 gate, serialized coordinator, revisions, correlation IDs, and stale-event rejection.

### UI hotspot expansion

Risk: route editing adds more domain behavior to large pages and controls.

Mitigation: RF-13/RF-14 gates, command-oriented ViewModels, renderer-neutral presentation state, and architecture tests.

### Scope expansion into automation or dispatch

Risk: Workflow 2.0, timetable, recorder, or testbench concerns delay the safety core.

Mitigation: expose stable events and interfaces, but keep their UI and orchestration integrations in their own issues.

## Maintainer decisions required before live integration

These decisions do not block the pure domain slices, but they block Slice 5 and hardware acceptance:

1. Which installed feedback modules provide explicit occupied and clear states, and which are pulse-only?
2. Which turnouts provide trustworthy position confirmation, including any three-way turnout wiring?
3. What is the safe stop command/aspect for every protected signal type on the validation layout?
4. Which representative real routes and conflicts form the acceptance fixture?
5. Is full-route release acceptable for the first version? This plan assumes yes.

Record the answers in Issue #34 or an approved linked design record before Slice 5 begins.

## Definition of done

Issue #34 is complete only when:

- all GitHub acceptance criteria are demonstrated;
- both pages operate the same persisted definitions and live revisioned state;
- no unsafe route or protected-signal transition exists in the automated test suite;
- simulation cannot reach live hardware;
- ordered event, disconnect, reconnect, cancellation, timeout, and partial-failure behavior is deterministic;
- all changed behavior has automated tests and required builds are green;
- Light, Dark, High Contrast, keyboard, and Narrator checks pass;
- the maintainer documents successful real-layout validation;
- architecture and user documentation are current;
- this completed plan is deleted.
