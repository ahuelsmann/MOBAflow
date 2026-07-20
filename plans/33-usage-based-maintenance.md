# Usage-Based Rolling-Stock Maintenance Implementation Plan

## Document status

- Status: In progress
- Primary issue: https://github.com/ahuelsmann/MOBAflow/issues/33
- Blocking issue: https://github.com/ahuelsmann/MOBAflow/issues/43
- Status and acceptance criteria source: GitHub issue #33

## Purpose

This plan defines the technical sequence for extending locomotive maintenance into a shared, usage-based maintenance capability for locomotives, passenger wagons, and goods wagons. GitHub issue #33 owns committed scope and acceptance criteria; this document owns implementation sequencing, dependencies, design decisions, risks, and validation.

## Current foundation

- `LocomotiveMaintenanceData` persists locomotive-only operating hours, optional distance, history, and recurring plans.
- `LocomotiveMaintenanceService` evaluates date, operating-hour, and distance thresholds, but has no due-soon state, trip counter, correction ledger, or shared wagon model.
- `Train.Vehicles` references rolling stock by stable vehicle ID and kind.
- Runtime locomotive state is keyed by DCC address, while journey snapshots do not identify an active train or consist.
- `MobaRuntimeService` executes against an isolated project clone, so durable usage checkpoints need an explicit synchronization boundary rather than incidental runtime model mutation.

## Technical decisions

1. Persist accumulated operating time as whole seconds. This is precise enough for maintenance while avoiding fractional-hour drift and false sub-second precision.
2. Persist completed trips as a non-negative integer lifetime counter.
3. Keep distance optional. Runtime code must not populate it until an explicit, testable distance provider is configured.
4. Store tracked lifetime totals separately from an append-only correction ledger. Effective totals are calculated from both; corrections never overwrite tracked totals.
5. Require a non-empty reason for every correction and reject corrections that would make an effective counter negative.
6. Attach the shared usage model additively to both `Locomotive` and `Wagon`. Missing usage data remains a valid empty state.
7. Keep the first slice platform-neutral and independent of EventBus or Z21 integration.
8. Use `TimeProvider` and monotonic elapsed-time measurement in the later attribution service. Wall-clock changes must not alter accumulated duration.
9. Treat every powered locomotive in an operating consist as independently accruing powered time; wagon time is attributed once per stable wagon ID even in malformed or repeated consist projections.
10. Count a completed trip only from one authoritative journey-completion transition. Resume, reconnect, and repeated terminal events must be idempotent by journey-run identity.
11. Do not begin runtime attribution or checkpoint integration until RF-04 in issue #43 is complete.

## Implementation progress

- Slice 1 completed locally: shared lifetime counters, auditable corrections, locomotive/wagon persistence, schema coverage, and validation service.
- Slice 2 completed locally: rolling-stock-neutral maintenance types, completed-trip intervals, deterministic combined-plan evaluation, configurable due-soon thresholds, plan completion baselines, shared DI registration, and sample data.
- Slices 3-5 remain blocked by issue #43 because runtime attribution requires its ordered bounded Z21 event pipeline.
- Slice 6 has not started; shared UI integration follows the runtime and checkpoint contracts so all three pages consume one authoritative projection.

## Delivery sequence

### Slice 1: Shared usage and correction foundation

Affected files:

- `Domain/VehicleUsage.cs` (new)
- `Domain/Locomotive.cs`
- `Domain/Wagon.cs`
- `Backend/Service/VehicleUsageService.cs` (new)
- `Test/Domain/VehicleUsageSerializationTests.cs` (new)
- `Test/Backend/VehicleUsageServiceTests.cs` (new)
- `MOBAflow/Build/Schemas/solution.schema.json`

Deliverables:

- shared tracked counters and correction records;
- effective-total calculation and validation;
- required correction reasons and non-negative effective totals;
- additive locomotive and wagon persistence;
- serialization and schema coverage.

### Slice 2: Shared maintenance model and deterministic due evaluation

Affected areas:

- consolidate locomotive-only maintenance types into rolling-stock-neutral types without parallel evaluators;
- add completed-trip intervals and baselines;
- define configurable due-soon thresholds for calendar and usage counters;
- define combined plans as due when the first configured threshold reaches its boundary;
- record maintenance completion by updating selected baselines without modifying lifetime totals;
- preserve existing locomotive behavior while adapting current ViewModels and tests.

### Slice 3: Active-operation and attribution contracts

Prerequisite: issue #43 merged.

Deliverables:

- stable journey-run identity;
- explicit active train/consist projection for journey and manual-driving contexts;
- locomotive operating-state rules based on authoritative runtime state;
- idempotent attribution state machine using `TimeProvider`;
- deterministic handling for double traction, consist edits, reconnect, resume, power loss, emergency stop, disconnect, and deleted vehicles.

### Slice 4: Durable checkpoints and recovery

Deliverables:

- atomic periodic checkpoints with a documented maximum loss interval;
- explicit synchronization from runtime-owned accumulation into editable solution state;
- idempotent recovery after restart or reconnect;
- structured diagnostics for rejected, duplicate, late, and recovered updates;
- no UI EventBus publication for every timing tick.

### Slice 5: Runtime projection and maintenance integration

Deliverables:

- coalesced structured usage updates;
- maintenance status refresh after usage, correction, and maintenance completion;
- lifetime totals remain stable when maintenance baselines reset;
- runtime snapshot payloads contain stable vehicle IDs rather than address-only identity where attribution requires them.

### Slice 6: Shared rolling-stock UI

Delivery order:

1. shared ViewModel/control projection and LocomotivesPage integration;
2. PassengerWagonPage integration;
3. GoodsWagonPage integration;
4. cross-fleet due-soon and overdue filters;
5. correction and history interactions, empty states, accessibility, and Light/Dark/High Contrast validation.

Page code-behind remains an input adapter only. Commands and behavior stay in ViewModels or platform-neutral services.

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Reordered or repeated Z21 events double count time | Block runtime slices on #43; use stable transition identities and idempotent state updates |
| Runtime clone changes never reach the saved solution | Introduce an explicit checkpoint synchronization contract and test editor/runtime isolation |
| DCC addresses are mistaken for stable vehicle identity | Resolve addresses only at the runtime boundary and persist counters by vehicle `Guid` |
| Wall-clock corrections change elapsed time | Use `TimeProvider` timestamps only for audit display and monotonic elapsed time for accumulation |
| Corrections erase audit history | Append corrections and calculate effective totals; never replace tracked totals from correction commands |
| Distance implies unsupported precision | Keep distance null until a configured provider supplies testable values |
| Three page implementations diverge | Establish shared types, services, and presentation projections before wagon UI integration |
| Autosave and checkpoint writes race | Serialize writes through one persistence boundary and add shutdown/concurrency tests |
| Large cross-layer change becomes difficult to review | Keep each slice independently buildable and testable; do not mix RF-04 implementation into #33 |

## Validation strategy

During Slice 1:

```powershell
dotnet test Test/Test.csproj --filter "FullyQualifiedName~VehicleUsage"
dotnet test Test/Test.csproj --filter "FullyQualifiedName~LocomotiveLifecycleSerializationTests"
dotnet build Backend/Backend.csproj
dotnet test Test/Test.csproj
```

Later runtime slices add deterministic tests for attribution, duplicate transitions, reconnect, consist changes, checkpoint recovery, cancellation, and shutdown. UI slices require the focused ViewModel tests, the WinUI FastDebug build, and manual Light, Dark, High Contrast, keyboard, and Narrator validation.

## Rollback strategy

- Slice 1 is additive: removing `Usage` properties and the new shared files restores the previous schema without rewriting existing maintenance data.
- Runtime accumulation remains disabled until its complete checkpoint and recovery boundary is present.
- Each later slice must preserve a single authoritative path; superseded locomotive-only behavior is removed only after equivalent shared tests pass.
- No compatibility adapter or migration framework is introduced for the current schema.

## Documentation and completion

- Keep implementation status and acceptance evidence in issue #33.
- Update current architecture and user documentation only when runtime behavior or UI becomes available.
- Delete this plan after issue #33 is completed and merged; the closed issue and Git history retain the implementation record.
