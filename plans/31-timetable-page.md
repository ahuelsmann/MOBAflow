# Timetable Coordination and Dispatching Implementation Plan

## Document status

- Status: Implemented; awaiting review
- Primary issue: https://github.com/ahuelsmann/MOBAflow/issues/31
- Completed prerequisite for live projection: https://github.com/ahuelsmann/MOBAflow/issues/43
- Implementation baseline: `main` at `84b1deda5774b35842768e8981658c518d1e6375`
- Status and acceptance criteria source: GitHub issue #31

Issue #31 carries the `plan-required` label and links this plan. RF-04 in issue #43 is merged, its regression guards are on `main`, and the implementation prerequisites are satisfied.

## Purpose

This plan defines the technical sequence for adding a dispatcher-oriented `TimetablePage` that coordinates user-authored services across trains, journeys, stations, platforms, and runtime journey progress. GitHub issue #31 owns committed scope, priority, and acceptance criteria; this document owns architecture, sequencing, dependencies, risks, persistence boundaries, and validation.

The MVP remains advisory. No timetable state transition or dispatcher action may directly command locomotives, turnouts, signals, routes, workflows, or other external effects.

## Current foundation

- `Project` persists trains, journeys, project stations, and platform definitions, but has no timetable collection.
- `Journey.Stations` contains ordered embedded station objects, while `Project.Stations` separately owns project stations and their platforms.
- Journey stops expose stable IDs and optional `PlatformId` values, but there is no explicit mapping from a journey stop to a project-station ID.
- `Station.Arrival` and `Station.Departure` are placeholder fields on shared station objects and cannot represent multiple dated services safely.
- `MobaRuntimeSnapshot.JourneyStates` is keyed only by `JourneyId`; it has no train, timetable-service, or journey-run identity.
- `JourneyRuntimeSnapshot` exposes the current station, feedback progress, last-feedback timestamp, and active flag, but no explicit arrival or departure event.
- `MobaRuntimeService` executes against an isolated clone of the editable project. Durable operator state therefore needs an explicit synchronization and persistence boundary.
- Runtime snapshots already reach SharedUI through `RuntimeSnapshotChangedEvent` and the UI-dispatched EventBus decorator.
- `NavigationRegistration` provides the established singleton page registration and navigation metadata pattern.
- RF-04 in issue #43 replaced per-event `Task.Run` publication with the ordered bounded `Z21EventPipeline` and deterministic asynchronous shutdown.

## Technical decisions

1. Model an MVP timetable service as one explicitly dated operating instance. Planned timestamps use `DateTimeOffset`; recurring templates, automatic timetable generation, and calendar rules remain out of scope.
2. Add a project-owned `TimetableServices` collection. Each service has a stable ID, display name or service number, `TrainId`, `JourneyId`, and an ordered collection of planned calls.
3. Each planned call references three identities explicitly: the journey stop ID, the project station ID, and the platform ID. Validation must prove that the stop belongs to the selected journey, the station belongs to the project, and the platform belongs to that station.
4. Do not reuse `Station.Arrival` or `Station.Departure` for timetable data. Planned and actual timestamps belong to timetable call and session-state types.
5. Persist timetable definitions in the current solution model and JSON schema. Increment the current solution schema version and update `solution.json`, schema validation, and serialization tests together. Do not add a compatibility migration or parallel legacy model.
6. Persist mutable operating state outside the editable solution through a project-scoped timetable session store, following the existing separation used for journey runtime checkpoints. Use atomic asynchronous writes and an explicit recovery contract.
7. Treat normal timetable editing as definition changes. Treat hold, cancellation, actual arrival or departure, and operational journey or platform reassignment as session decisions that preserve the authored plan and survive an interrupted session.
8. Use `TimeProvider` for all current-time access. Delay, ordering, conflict detection, and state transitions must not call `DateTime.Now` or `DateTimeOffset.Now` directly.
9. Represent hold as an advisory overlay on a non-terminal service state. Holding a service never pauses journey processing or sends a hardware command.
10. Treat cancellation and completion as terminal states. Repeated manual commands and repeated runtime projections must be idempotent.
11. Infer actual arrival only from an unambiguous journey transition to a configured stop. Keep actual departure manual in the MVP because the current runtime does not expose a reliable departure event.
12. Allow at most one non-terminal timetable service to own live projection for a `JourneyId` at a time. Ambiguous overlapping assignments produce a conflict and suppress automatic projection until the operator resolves the assignment.
13. Evaluate occupancy windows as half-open intervals so one service may release a platform exactly when another begins using it. Planned calls must satisfy `Arrival <= Departure` when both values are present.
14. Store a project-level minimum-turnaround duration with an explicit default. Turnaround conflicts use the effective train assignment and this persisted policy; they are not hardcoded in the UI.
15. Recalculate conflicts from immutable effective inputs after every definition edit, operator decision, clock tick that crosses a relevant boundary, and accepted runtime transition. Do not incrementally mutate conflict results.
16. Keep timetable coordination in focused platform-neutral services and a dedicated SharedUI ViewModel. Do not add timetable behavior to `MainWindowViewModel` or page code-behind.
17. EventBus handlers update observable state directly and never call `InvokeOnUi`; the decorator remains the only UI-thread marshalling boundary.

## Proposed domain and runtime model

### Persisted definitions

- `TimetablePolicy`
  - minimum turnaround duration;
  - optional platform occupancy margins if introduced by the issue scope;
  - deterministic defaults covered by configuration and serialization tests.
- `TimetableService`
  - stable `Id`;
  - English display name or service number;
  - authored `TrainId` and `JourneyId`;
  - ordered `Calls`;
  - no mutable live status or actual timestamps.
- `TimetableCall`
  - stable `Id`;
  - `JourneyStopId`, `StationId`, and `PlatformId`;
  - optional planned arrival and departure `DateTimeOffset` values;
  - deterministic authored order used together with journey order validation.

### Session state

- `TimetableServiceSessionState`
  - service ID and current lifecycle state;
  - advisory held flag;
  - cancellation metadata;
  - effective journey override when reassigned operationally;
  - per-call actual arrival and departure timestamps;
  - per-call platform overrides;
  - last accepted runtime projection identity or station transition for idempotency.
- `TimetableCallSessionState`
  - call ID;
  - actual timestamps;
  - optional platform override;
  - source of each transition (`Manual` or `Runtime`).

### Derived projections

- effective service and call projections combine authored definitions with session overrides without modifying the definitions;
- delay values are calculated, never persisted as independent mutable truth;
- conflicts contain a stable conflict kind, severity, explanation, involved entity IDs, and navigation-ready references;
- board rows expose planned and actual values separately and derive upcoming, active, delayed, held, completed, or cancelled presentation state.

## State-transition rules

1. A new service begins in `Scheduled`.
2. A first accepted arrival, departure, or explicit release into operation moves it to `Active`.
3. `Hold` and `Release` toggle the advisory held flag without changing the underlying lifecycle state.
4. `Cancel` moves a non-terminal service to `Cancelled`; later arrival or departure updates are rejected unless cancellation is explicitly reversed by a separately reviewed scope change.
5. Marking a call arrived or departed records the supplied or current `TimeProvider` timestamp exactly once. Repeating the same transition is a no-op.
6. Departure before arrival is rejected when the call requires an arrival; exceptions for origin calls must be explicit in the model.
7. Completing the final required call moves the service to `Completed`.
8. Journey or platform reassignment changes only the effective session assignment and triggers complete revalidation and conflict recalculation.
9. Runtime projection may mark an arrival only when service, journey, and journey-stop correlation is unique.
10. Offline runtime state does not erase actual values or operator decisions. It changes availability messaging and stops automatic projection.

## Conflict model

The platform-neutral evaluator returns all applicable conflicts in deterministic order:

1. missing or deleted train, journey, stop, station, or platform references;
2. call order contradicting the selected journey stop order;
3. platform not belonging to the selected project station;
4. invalid or decreasing planned timestamps within one service;
5. overlapping effective platform occupancy intervals;
6. overlapping effective assignments for the same train;
7. insufficient turnaround time between consecutive services for the same train;
8. overlapping non-terminal services assigned to the same journey, which makes live projection ambiguous;
9. session overrides that invalidate a previously valid definition.

Every result identifies the affected service and call IDs plus train, journey, station, or platform IDs needed for focused views and later navigation. Explanations are generated from structured conflict data rather than stored as the source of truth.

## Delivery sequence

### Slice 1: Timetable definition and schema foundation

Affected files and areas:

- `Domain/Timetable.cs` or focused timetable domain files (new);
- `Domain/Project.cs`;
- `Domain/Solution.cs`;
- `MOBAflow/Build/Schemas/solution.schema.json`;
- `MOBAflow/solution.json` and relevant test fixtures;
- domain serialization and JSON validation tests.

Deliverables:

- persisted policy, service, and call definitions;
- stable cross-entity references;
- direct schema-version update without migration code;
- validation for required identities, ordering, timestamps, and platform ownership;
- old placeholder station timestamps are not used by the new feature.

### Slice 2: Deterministic evaluation and conflict engine

Affected areas:

- platform-neutral timetable resolver and validator;
- immutable effective-service projection;
- delay and presentation-state calculation using `TimeProvider`;
- complete conflict evaluation and stable ordering;
- focused Backend and Domain tests.

Deliverables:

- planned-versus-actual calculations;
- platform, train, journey, order, reference, and turnaround conflicts;
- actionable structured conflict references;
- no dependency on WinUI, MAUI, Z21, or EventBus.

### Slice 3: Session state machine and durable recovery

Affected areas:

- timetable session-state and transition service;
- asynchronous project-scoped session store;
- atomic write, corrupt-file handling, and recovery behavior;
- explicit definition/session merge boundary;
- state-transition and persistence tests.

Deliverables:

- idempotent hold, release, cancel, arrive, depart, and operational reassignment actions;
- durable actual timestamps and overrides;
- restart recovery without mutating the authored timetable;
- cancellation, shutdown, and concurrent-save behavior.

### Slice 4: Read-only TimetablePage dispatch board

Affected files and areas:

- `SharedUI/ViewModel/TimetablePageViewModel.cs` and focused row, call, filter, and conflict ViewModels (new);
- `MOBAflow/View/TimetablePage.xaml` and minimal input-adapter code-behind (new);
- `MOBAflow/Service/NavigationRegistration.cs`;
- `Common.Configuration` layout settings and default tests;
- WinUI page/ViewModel registration tests.

Deliverables:

- board views for service, station, train, and time window;
- separate planned and actual values;
- deterministic ordering and filtering;
- conflict explanations and navigation-ready commands;
- usable empty, offline, corrupt-session, and partially configured states;
- persisted star-sized layout values and accessible non-color-only status indicators.

### Slice 5: Editing and manual dispatcher actions

Deliverables:

- create, edit, duplicate if approved by the issue, and delete timetable definitions;
- select existing train, journey, journey stop, station, and platform references;
- hold, release, cancel, mark arrived or departed, and reassign journey or platform commands;
- immediate full conflict recalculation after every accepted change;
- dialogs and confirmations through `IDialogService`, never page behavior in code-behind;
- command-state, validation, persistence, and ViewModel tests.

### Slice 6: Live journey projection and operational polish

Prerequisite satisfied by `main` commit `08c196878c5d5ee30b7765638a1f535e64a3e01c`.

Deliverables:

- consume `RuntimeSnapshotChangedEvent` through the existing decorated EventBus;
- detect unambiguous journey-stop transitions and project actual arrivals idempotently;
- suppress automatic updates for ambiguous journey assignments and explain the conflict;
- preserve manual departure semantics until an explicit departure runtime contract exists;
- recover correctly after reconnect, project activation, journey reset, and repeated snapshots;
- focused operational timeline, status refresh, accessibility, and Light/Dark/High Contrast validation.

## Dependency and coordination rules

- RF-04 is complete. Consume the ordered pipeline as an existing boundary and do not modify its capacity, overload policy, metrics, or shutdown behavior in issue #31.
- Workflow 2.0 in issue #32 is optional. Timetable state may later observe workflow lifecycle events, but the MVP cannot depend on them.
- Route, block, and interlocking work in issue #34 is optional. The MVP detects planned station, platform, train, and journey conflicts without controlling routes or signals.
- RecorderPage in issue #30 may later consume timetable transitions; recording support is not part of the MVP.
- Coordinate the `TimeProvider` abstraction with issue #35 to avoid parallel virtual-clock frameworks, but neither issue blocks the other's platform-neutral model.
- Issue #33's schema foundation is present on the implementation baseline. Preserve its rolling-stock usage and maintenance additions while extending the current schema.
- Avoid combining implementation slices with RF-10 repository-wide formatting to reduce merge noise.

## Expected affected files

The exact split must be confirmed against the post-dependency baseline before implementation. Expected areas are:

- `Domain/Project.cs`, `Domain/Solution.cs`, and new timetable domain types;
- `Common/Runtime/` only if timetable runtime projections need to cross process or API boundaries;
- `Backend/Service/` timetable validation, conflict, state, projection, and persistence services;
- `Backend/Extensions/MobaBackendServiceCollectionExtensions.cs` for shared DI registrations;
- `SharedUI/ViewModel/` dedicated timetable presentation and commands;
- `MOBAflow/View/TimetablePage.xaml` and `.xaml.cs`;
- `MOBAflow/Service/NavigationRegistration.cs`;
- `Common/Configuration/AppSettings.Sections.cs` for layout persistence;
- `MOBAflow/Build/Schemas/solution.schema.json` and `MOBAflow/solution.json`;
- focused tests under `Test/Domain`, `Test/Backend`, `Test/Common`, `Test/SharedUI`, and `Test/WinUI`.

Do not modify `MOBAsmart` or expose timetable commands through MOBApi unless issue #31 is explicitly expanded in GitHub.

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Journey stops and project stations are treated as the same entity accidentally | Persist and validate journey-stop, project-station, and platform IDs separately |
| Multiple services share one journey and receive the wrong live update | Enforce one unambiguous live owner per journey and surface overlaps as conflicts |
| Reordered events produce incorrect actual timestamps | Block live projection on #43 and make projection idempotent |
| Manual and automatic updates overwrite each other | Record transition source and accept each state transition exactly once |
| Authored plans are destroyed by operational reassignment | Store session overrides separately and derive effective assignments |
| Session state is lost or corrupt after interruption | Use atomic asynchronous writes, project-scoped keys, backups where appropriate, and corrupt-file tests |
| Wall-clock changes make delay nondeterministic | Use injected `TimeProvider` and explicit `DateTimeOffset` values |
| Existing `Station.Arrival` and `Departure` fields become competing truth | Exclude them from timetable calculations and consider removal only in separately reviewed cleanup |
| Schema changes conflict with issue #33 | Rebase after #33, merge additive properties carefully, and run full serialization/schema tests |
| UI logic grows inside `MainWindowViewModel` | Use a dedicated TimetablePage ViewModel and platform-neutral collaborators |
| Advisory actions are mistaken for hardware authority | Keep command interfaces free of hardware/workflow calls and use explicit operator messaging |
| Large board updates reduce UI responsiveness | Recompute immutable results off domain collections, publish coalesced changes, and benchmark representative service counts |
| Status is communicated only by color | Pair theme resources with text, icons, automation names, and accessible state descriptions |

## Validation strategy

### Domain and schema

- default and populated timetable serialization round trips;
- direct current-schema validation for `solution.json` and test fixtures;
- missing, deleted, or mismatched train, journey, stop, station, and platform references;
- cross-midnight timestamps represented explicitly by `DateTimeOffset`;
- schema-version mismatch behavior remains intentional and tested.

### Deterministic engine

- delay before plan time, at plan time, late arrival, early arrival, and manual departure;
- stable service, call, and conflict ordering for identical timestamps;
- half-open platform occupancy boundaries;
- train overlap and configurable turnaround boundaries;
- contradictory journey-stop order and invalid platform ownership;
- ambiguous journey assignments;
- TimeProvider-driven boundary changes without sleeping tests.

### Session transitions and persistence

- every valid and invalid manual transition;
- idempotent repeated commands and repeated runtime snapshots;
- cancellation terminal behavior and held overlay behavior;
- definition edits versus operational overrides;
- atomic save, concurrent save, cancellation, restart recovery, corrupt file, and project switching;
- no mutation of the editable timetable from runtime clone state.

### ViewModel and UI

- board ordering, station/train/time filters, selection preservation, and conflict navigation commands;
- create/edit validation and manual action command enablement;
- empty solution, missing active project, offline runtime, missing references, and corrupt recovered session;
- no manual UI dispatch in EventBus handlers;
- navigation registration and page/ViewModel DI resolution;
- layout star-value round trip;
- English UI strings, keyboard operation, visible focus, Narrator labels, text scaling, Light, Dark, and High Contrast.

### Validation commands

Use the narrowest filters during each slice, then run the full affected graph:

```powershell
dotnet test Test/Test.csproj --filter "FullyQualifiedName~Timetable"
dotnet build Backend/Backend.csproj
dotnet build SharedUI/SharedUI.csproj
dotnet test Test/Test.csproj

dotnet restore MOBAflow/MOBAflow.csproj
dotnet build MOBAflow/MOBAflow.csproj -c FastDebug --no-restore /p:BuildMOBApiDependency=false /p:CopyMOBApiToOutput=false
```

Before merge, also validate the repository JSON schema command used by the WinUI build, run `git diff --check`, and complete the manual accessibility and theme checklist. Do not use solution-level restore as a substitute for project-scoped validation.

## Rollback and compatibility strategy

- Keep every slice independently buildable and reversible until the new schema is used to save production project data.
- Back up representative solution files before the schema-version increment is exercised manually.
- Do not introduce legacy deserializers, migrations, dual-write paths, or fallback timetable models.
- Runtime projection remains disabled until its correlation, persistence, and recovery tests are complete.
- Manual dispatcher actions remain local to the session-state service; removing the page must not leave hardware-affecting behavior behind.
- After files are saved with the new schema version, rollback requires restoring both the earlier application version and a matching solution-file backup.

## Documentation and completion

- Keep implementation status, priority, product decisions, and acceptance evidence in issue #31.
- Link this plan from issue #31 and add `plan-required` before the issue moves to `Ready` or implementation begins.
- Document the persisted timetable schema and operator-session recovery behavior when the corresponding slices ship.
- Update user documentation only when the page is usable; all shipped UI examples and strings remain English.
- Record any future workflow or hardware integration as separate GitHub scope with explicit confirmation and safety review.
- Delete this plan after issue #31 is completed and merged; the closed issue and Git history retain the implementation record.
