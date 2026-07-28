# Issue #34 Interlocking Control Implementation Plan

## Document status

**GitHub Issue**: #34
**Spec Kit**: Required

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
- Slice 3 completed on 2026-07-20: semantic turnout command sequences, live Z21 effect boundary, recording/simulation adapter, correlated lifecycle state, confirmation reconciliation, deterministic timeout handling, cancellation, disconnect, duplicate/out-of-order handling, and stale-completion protection.
- Slice 4 completed on 2026-07-20: serialized preview, select, set, cancel, safe-stop, release, reconciliation, disconnect, timeout, and shutdown operations; atomic reservation before effects; semantic turnout confirmation before signal clearance; configurable route-specific proceed aspects; conservative recovery with retained locks; late-feedback reconciliation without implicit route reactivation; structured correlated lifecycle events for internal and final transitions; and in-flight shutdown cancellation without deadlock.
- Slice 5 completed on 2026-07-21: RF-04 FIFO observations now project turnout and R-Bus active/inactive state into a narrow `IInterlockingRuntime`; immutable correlated snapshots are published through the existing EventBus boundary; project activation, DI, initial/reconnect queries, synchronization gating, duplicate handling, disconnect/not-switched invalidation, shutdown, and subscription disposal are covered. Live turnout and configured Viessmann multiplexer signal effects reuse the production coordinator and fail closed when offline or unmapped.
- Integration checkpoint on 2026-07-20: restored the missing `Project.Interlocking` persistence boundary, removed duplicate persisted signal-box route/runtime state, and reconnected schema, sample data, validation, diagnostics, dependency injection, and canonical address allocation. The complete `net10.0` and Windows test targets pass.
- Slice 5 validation on 2026-07-21: `net10.0` passed 1,099 tests with 4 expected skips; the Windows target passed all 1,149 tests; the MOBAflow FastDebug build completed with 0 warnings and 0 errors; deterministic per-file Sonar secret scans passed for every Slice 5 file read or changed.
- Slice 6/7 gate check on 2026-07-21: GitHub Issue #47 still lists only RF-01 through RF-05 child work and states that later RF packages receive individual issues only when their dependency gates are satisfied. No RF-13 or RF-14 child is listed yet, so TrackPlanPage and SignalBoxPage integration must not start from Issue #34 at this checkpoint.
- Slice 6/7 gate check on 2026-07-24: RF-06/#90, RF-13/#91, and RF-14/#92 are closed after PRs #94, #95, and #98 merged. TrackPlanPage and SignalBoxPage integration may now proceed from the current `main` baseline.
- Slices 6 and 7 implemented on 2026-07-24 from baseline `22397b87`: page-scoped control ViewModels project one shared interlocking runtime into independent TrackPlanPage and SignalBoxPage selections; representation bindings resolve to shared operational identities; direct turnout commands use the semantic coordinator; route preview, set, cancel, release, and reconciliation share the same revisioned state; the TrackPlan route editor captures entry, ordered path, exit, turnout, block, and protected-signal requirements and validates before persistence; textual lock, reservation, lifecycle, and fault descriptions provide non-color-only state.
- Slice 6/7 validation on 2026-07-24: all 30 focused interlocking coordinator/runtime/ViewModel tests and the complete `net10.0` suite (1,468 passed, 4 expected skips) pass; the MOBAflow FastDebug build completes with 0 warnings and 0 errors; every changed file passes the deterministic Sonar secret scan. The Windows test target exceeded the five-minute local runner limit without producing a result, so hardware/UI acceptance and the remote SonarCloud gate remain open before Issue #34 can close.
- UX remediation planned on 2026-07-26 against merged baseline `2ee7611f`: the always-visible, horizontally scrolling interlocking row is not accepted as the final operator experience. The shared runtime and fail-safe commands remain valid, but their presentation must move into a contextual selected-object surface before UI acceptance.

## UX remediation plan

### Problem statement

The first page integration places status, runtime revision, turnout selection and commands, block and signal state, route selection and six route actions, and the route editor entry point in one full-width horizontal row above the canvas.

This implementation proves that both pages can consume the shared runtime, but it has the following usability problems:

- The row has no visible section title or clear relationship to the currently selected canvas element.
- A horizontal scrollbar is required even at a large physical window size and high display scaling; content can open or remain scrolled so the leading status text is clipped.
- Unrelated selection, command, status, diagnostics, and configuration concerns compete at the same visual level.
- The row permanently reduces the vertical canvas area even when the operator does not need interlocking controls.
- Long safety state is ellipsized while the internal runtime revision is given primary-screen space.
- Turnout, block, signal, and route controls appear as one sequence without grouping or progressive disclosure.
- Multiple mutually exclusive or lifecycle-dependent route commands appear as equally weighted buttons, creating a disabled-button wall rather than showing the next valid action and its prerequisites.
- The TrackPlan route editor is hidden in a flyout launched from the same dense row, although route authoring is a separate configuration task.
- Keyboard focus order and screen-reader announcements must traverse unrelated controls and may announce revision churn instead of meaningful state changes.

## Clarifications

### Session 2026-07-28

- Q: What is the primary role of TrackPlanPage and SignalBoxPage? -> A: Both pages support editing and live operation as equal first-class workflows.
- Q: Which interaction model should make editing and live operation equally available? -> A: Use one Selected Object Workbench without a global Edit/Operate mode.
- Q: How should the Workbench protect against accidental live commands? -> A: Routine semantic commands execute from explicit named buttons without confirmation; exceptional recovery actions require confirmation.

### UX outcome

Remove the permanent interlocking row from both pages. Replace the separate Properties and Operations surfaces with one reusable **Selected Object Workbench** that preserves the shared `InterlockingControlViewModel`, the fail-safe command boundary, and the canvas as the shared primary workspace.

The Workbench shows safety state, relevant live actions, definition editing, details, and diagnostics for the same selected object. Editing and live operation remain equal first-class workflows without a global Edit/Operate mode or page navigation.

The experience has three levels:

1. **Global availability**: one `Context` command in the page `CommandBar` opens or focuses the Workbench and includes a concise state such as `Synchronized`, `Offline`, `Unknown`, or `Fault`.
2. **Selected object**: the Workbench presents clearly separated Safety, Live action, and Definition sections for the current turnout, block, signal, route, or representation.
3. **Progressive detail**: additional properties, validation, correlation/revision data, and diagnostics remain available through explicitly expanded sections.

Opening, closing, pinning, resizing, or changing selection in the Workbench must never start, cancel, release, apply a draft, or otherwise mutate runtime or persisted definition state.

### Information architecture

#### Command bar

- Add a single `Context` `AppBarButton` to TrackPlanPage and SignalBoxPage.
- Use the button to open or focus the Selected Object Workbench.
- Show a non-color-only availability indicator through icon plus text or accessible description.
- Keep page-global commands such as new and open in the CommandBar; selected-object definition and live commands belong in their explicitly labelled Workbench sections.
- Do not show the raw runtime revision in the command bar.
- Do not add a global Edit/Operate toggle.

#### Selected Object Workbench

Evolve the existing collapsible Properties column into one Selected Object Workbench rather than adding tabs, page modes, or a fourth permanent pane.

- The column stays collapsible and resizable.
- Safety, Live action, and Definition are peer sections for the same selected object.
- Selecting a canvas representation updates the Workbench context but does not unexpectedly steal focus.
- Invoking `Context` explicitly opens or focuses the Workbench.
- An explicitly pinned Workbench may be persisted; the redesign must not infer a pinned/open state from the removed toolbar.
- On compact windows, the same content opens as an overlay pane so the canvas retains a useful minimum width.
- The page must not introduce horizontal scrolling for commands at any supported width or display scale.

#### Workbench content

The reusable content is provisionally named `SelectedObjectWorkbench`. It is a WinUI input adapter only: it binds to commands and presentation state owned by page and interlocking ViewModels and contains no definition or operational behavior in code-behind.

The content is vertically scrollable and grouped as follows:

1. **Safety status**
   - Concise connection/synchronization state.
   - Wrapped, non-ellipsized fault or rejection explanation.
   - `InfoBar`-style emphasis for warning, error, reconciliation-required, or offline states.
   - Runtime revision and correlation details only in an expandable Diagnostics section.
2. **Current object**
   - Entity type, name, and binding source.
   - Full textual state, including occupancy, lock owner, reservation, lifecycle, confirmation, and fault details as applicable.
   - A searchable fallback selector when no canvas representation is selected or when the operator intentionally changes context.
3. **Live action**
   - Turnout context shows only supported positions.
   - Block and signal contexts are read-only unless an already-authorized semantic command exists.
   - Route context shows one emphasized next valid lifecycle action and only the currently relevant secondary or recovery actions.
   - Disabled actions include a visible or accessible reason; unsupported actions are omitted.
   - Routine semantic commands execute only from an explicit, clearly named button and do not add a confirmation step.
   - Exceptional recovery actions require a confirmation that states the affected object, retained or released safety state, and expected consequence.
   - Selection, focus, opening, closing, and keyboard navigation never execute the primary action implicitly.
4. **Definition**
   - Selected-object properties, representation binding, and validation are edited as an explicit draft.
   - Draft state, `Apply changes`, and `Discard changes` are visually and semantically separate from Live action.
   - Applying a definition draft never selects, previews, reserves, sets, cancels, releases, or reconciles a runtime entity.
5. **Details and diagnostics**
   - Revision, correlation, timestamps, and detailed structured state.
   - Collapsed by default and excluded from routine screen-reader live announcements.

Safety-critical state must never rely on color alone and must not be truncated without another immediately available full-text representation. Live actions and definition actions require distinct headings, button hierarchy, focus groups, and accessible descriptions.

#### Route authoring

Route authoring appears as a task-focused Definition section in the same Workbench:

- Preserve entry, ordered path, exit, turnout, block, protected-signal, validation, and save capabilities.
- Present the draft as grouped form sections with a persistent validation summary and explicit `Validate`, `Apply changes`, and `Discard changes` actions.
- A short-lived, field-specific `Pick from canvas` interaction may collect route elements without introducing a global page mode.
- Warn before discarding a dirty draft.
- Keep the Definition and Live action sections visibly separate even though both refer to the same route.
- A saved route is not automatically selected, reserved, or set.

### Page-specific behavior

#### TrackPlanPage

- Selecting a bound track representation continues to call `SelectTrackRepresentation`.
- The Workbench resolves that representation to its operational turnout, block, signal, or route context while retaining track geometry and binding properties.
- If a selected track has no operational binding, show `No operational binding` and keep live commands unavailable.
- Workbench section boundaries keep geometry and configuration distinct from live state and semantic commands.
- Route editing uses the existing TrackPlan operational-element selection and draft commands without placing editor fields in the command bar or a transient toolbar.

#### SignalBoxPage

- Selecting a bound signal-box element continues to call `SelectSignalBoxRepresentation`.
- The Workbench follows the selected element and combines its definition with the shared runtime state also visible on TrackPlanPage.
- Selection changes update context without rebuilding the selected visual during the pointer event.
- Unbound elements show a clear read-only explanation instead of retaining stale commands from the prior selection.

### Presentation-model changes

Keep the runtime, coordinator, domain safety engine, and EventBus threading boundary unchanged. Extend only page-scoped presentation state where required:

- `SelectedOperationalContext` or equivalent discriminated presentation state for turnout, block, signal, route, or none.
- Concise `AvailabilityText` and accessible availability description for the command bar.
- Full, wrapped context state separate from the existing concise status message.
- Explicit selected-object draft state that remains separate from immutable runtime snapshots.
- `PrimaryRouteActionLabel`, availability, command, and disabled reason derived from the route lifecycle.
- Visibility and disabled-reason properties for secondary route recovery actions.
- Supported turnout-position actions derived from the definition instead of always showing all three buttons.
- Diagnostics presentation state that does not trigger primary live-region announcements on every revision.

Do not duplicate interlocking decisions in the ViewModel. Command availability remains a conservative projection; the coordinator still performs the authoritative validation at execution time.

### Responsive and accessibility contract

- Wide layout: the Workbench is inline with the canvas and respects the existing user-resizable width.
- Compact layout: the Workbench uses an overlay pane with a practical maximum width and an explicit close action.
- Very narrow layout: controls stack vertically; no command group depends on horizontal scrolling.
- Validate at 100%, 150%, and 200% Windows display scaling and at representative effective widths around 800, 1024, 1200, and 1440 device-independent pixels.
- Preserve a logical focus sequence: Context command, safety status, current object, live action, definition, details, diagnostics.
- Provide `AutomationProperties.Name`, help text, and keyboard access for every command.
- Announce meaningful accepted, rejected, failed, and reconciled transitions politely; do not announce raw revision increments.
- Validate Light, Dark, High Contrast, disabled, hover, focus, selected, warning, and fault states with `ThemeResource` values only.

### Alternatives considered

- **Keep and visually group the horizontal row**: rejected because it still consumes canvas height, requires overflow behavior, and mixes four entity types with configuration.
- **Move all controls into a command-bar overflow menu**: rejected because complex safety state and lifecycle explanations do not fit transient menus and would be difficult to scan and access.
- **Add a second permanent right-side pane beside Properties**: rejected because it would reduce canvas width and compete with the existing toolbox and properties layout.
- **Use only direct manipulation on canvas**: rejected because keyboard users, unbound definitions, route recovery, and detailed fail-safe explanations still need an explicit operational surface.
- **Use an explicit global Edit/Operate mode**: rejected because both workflows must remain simultaneously available for the selected object and mode switching would add friction.
- **Use an anchored Canvas Lens card**: rejected because overlay collision, spatial keyboard navigation, and global recovery access add complexity.
- **Use one Selected Object Workbench**: selected because one deep surface can keep Safety, Live action, and Definition simultaneously available while hiding runtime, draft, responsive, and accessibility complexity behind a small contextual interface.

### Expected implementation areas

- `MOBAflow/View/TrackPlanPage.xaml`
- `MOBAflow/View/SignalBoxPage.xaml`
- `MOBAflow/View/TrackPlanPage.xaml.cs`
- `MOBAflow/View/SignalBoxPage.xaml.cs`
- a shared Selected Object Workbench input adapter under `MOBAflow/Controls/`
- `SharedUI/ViewModel/InterlockingControlViewModel.cs`
- page and definition ViewModels required to expose explicit selected-object draft state
- page layout settings only if explicit Workbench pinning is approved for persistence
- `Test/SharedUI/InterlockingControlViewModelTests.cs`
- focused WinUI structure/selection tests under `Test/WinUI/`

No Domain, Backend, coordinator, Z21, persistence-schema, or hardware-effect changes are expected. Any required change in those areas is a scope expansion and must be justified before implementation.

### Delivery sequence

#### UX Slice A: Characterize the current integration

- Add focused tests for selection-to-operational-context mapping on both pages.
- Characterize existing command enablement, command dispatch, and status projection before changing layout.
- Add a structural regression test proving the pages no longer require an always-visible interlocking row.

Exit criteria:

- Existing semantic commands and fail-safe rejections are protected by tests.
- Selection mapping remains independent for each page-scoped ViewModel.

#### UX Slice B: Contextual presentation state

- Add the operational-context and next-valid-action presentation properties.
- Separate concise availability, full state explanation, disabled reasons, and diagnostics.
- Distinguish routine semantic actions from confirmation-required recovery actions without duplicating coordinator safety decisions.
- Add unit tests for none, unbound, synchronized, locked, offline, failed, and reconciliation-required contexts.

Exit criteria:

- The ViewModel can drive the target UI without operational decisions in XAML or code-behind.
- Every unavailable action has a deterministic reason.

#### UX Slice C: Selected Object Workbench

- Build the reusable vertically grouped Workbench input adapter.
- Implement contextual turnout, block, signal, route, and unbound templates.
- Separate Safety, Live action, and Definition with distinct focus groups and accessible descriptions.
- Add the diagnostics expander and accessible live-region boundaries.
- Integrate route authoring as an explicit Definition draft without coupling it to live route commands.

Exit criteria:

- The control has no command behavior beyond forwarding input to ViewModel commands.
- Definition draft actions cannot invoke runtime commands, and live actions cannot mutate definition drafts.
- Full safety state remains readable without horizontal scrolling or ellipsis.

#### UX Slice D: Page integration and responsive behavior

- Remove the full-width interlocking Borders and horizontal ScrollViewers from both pages.
- Add the Context command and replace the existing Properties surface with the shared Workbench.
- Preserve TrackPlan and SignalBox selection synchronization and layout persistence semantics.
- Add wide, compact, and overlay visual states.

Exit criteria:

- The canvas starts directly below the page command bar.
- No interlocking command row or horizontal toolbar scrollbar remains.
- Both pages expose the same runtime truth through their own contextual selection.

#### UX Slice E: Validation and acceptance

- Run focused ViewModel and WinUI structural tests.
- Run the complete `net10.0` test suite.
- Build MOBAflow in FastDebug.
- Run changed-file secret scans and local Sonar analysis against the actual base before a draft PR.
- Perform keyboard, screen-reader, theme, High Contrast, scaling, and compact-window checks.
- Perform maintainer-led live UI and hardware acceptance only after explicit approval to launch MOBAflow.

Exit criteria:

- Automated validation is green.
- The maintainer accepts the revised information hierarchy and interactions.
- Hardware/manual gates are reported separately and are not treated as passed without evidence.

### UX acceptance criteria

1. Neither page shows an always-visible interlocking row after navigation.
2. The canvas loses no vertical space to interlocking controls while the Workbench is closed.
3. All existing definition, turnout, and route capabilities remain reachable from the Selected Object Workbench.
4. Selecting a bound canvas element updates the operational context on the same page.
5. An unbound or stale selection cannot leave an actionable command for the previous entity.
6. The next valid route action is visually primary; recovery actions appear only when relevant.
7. Offline, unknown, locked, rejected, failed, and reconciliation-required states include full text and do not rely on color.
8. Runtime revisions are available in Diagnostics but absent from the primary toolbar.
9. No horizontal scrollbar is required for the Workbench at supported widths or display scaling.
10. Route authoring and live route operation remain visibly and behaviorally separate sections of the same selected-route context.
11. Opening, closing, pinning, resizing, or changing selection in the Workbench cannot mutate runtime or definition state.
12. TrackPlanPage and SignalBoxPage continue to project the same runtime revision and entity state when observing the same shared runtime snapshot.
13. Light, Dark, and High Contrast visuals, keyboard focus, and screen-reader announcements meet the project accessibility contract.
14. MOBAflow is not launched for manual validation without explicit prior user approval.
15. Applying or discarding a definition draft cannot invoke preview, set, cancel, release, reconcile, turnout, or signal commands.
16. Executing a live command cannot mark, apply, or discard a definition draft.
17. Routine semantic commands require one explicit activation and no confirmation dialog; selection or focus cannot activate them.
18. Every exceptional recovery action presents a consequence-specific confirmation before invoking its existing semantic command.

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

## Maintainer decisions required before hardware acceptance

Slice 5 uses conservative configuration-driven defaults and remains fail closed. These layout-specific decisions may be refined while trying the system, but they must be settled before hardware acceptance:

1. Which installed feedback modules provide explicit occupied and clear states, and which are pulse-only?
2. Which turnouts provide trustworthy position confirmation, including any three-way turnout wiring?
3. What is the safe stop command/aspect for every protected signal type on the validation layout?
4. Which representative real routes and conflicts form the acceptance fixture?
5. Is full-route release acceptable for the first version? This plan assumes yes.

Record all answers in Issue #34 or an approved linked design record before Slice 8 hardware acceptance.

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
