# Usage-Based Rolling-Stock Maintenance Implementation Plan

## Document status

- Status: In progress
- Primary issue: https://github.com/ahuelsmann/MOBAflow/issues/33
- Fulfilled prerequisite: https://github.com/ahuelsmann/MOBAflow/issues/43 (merged via PR #45)
- Status and acceptance criteria source: GitHub issue #33

## Purpose

This plan defines the technical sequence for extending locomotive maintenance into a shared, usage-based maintenance capability for locomotives, passenger wagons, and goods wagons. GitHub issue #33 owns committed scope and acceptance criteria; this document owns implementation sequencing, dependencies, design decisions, risks, and validation.

## Current foundation

- `VehicleUsageData` persists operating seconds, completed trips, optional distance, and an auditable correction ledger for locomotives and wagons.
- `VehicleMaintenanceService` evaluates shared calendar and usage plans, including deterministic due-soon, due, and overdue states.
- `Train.Vehicles` references rolling stock by stable vehicle ID and kind.
- Runtime locomotive state is still keyed by DCC address, while journey snapshots do not yet identify an active train or consist.
- `MobaRuntimeService` executes against an isolated project clone, so durable usage checkpoints need an explicit synchronization boundary rather than incidental runtime model mutation.
- Issue #43 replaced per-event fire-and-forget dispatch with an ordered bounded Z21 event pipeline and is available on `main`.

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
11. Consume authoritative Z21/runtime state transitions synchronously inside `MobaRuntimeService`. Issue #43 remains the upstream ordering foundation, but usage correctness must not depend on EventBus dispatch completion or UI-queue drain semantics.
12. Persist absolute per-vehicle counters in the crash-recovery checkpoint. Recovery merges by stable project and vehicle IDs using the greater durable value, making repeated recovery idempotent.
13. Keep the runtime clone authoritative during operation. A coalesced checkpoint event transfers absolute counters to the editable solution on the UI thread; the local checkpoint remains durable if that later solution save is delayed or fails.
14. Infer the active consist from a uniquely matching moving locomotive and retain an explicit runtime command for ambiguous manual contexts. Editor page selection never changes attribution.

## PR #45 boundary assessment

- The review observation that `UiThreadEventBusDecorator.Publish` posts and returns means PR #45's bounded channel does not prove end-to-end boundedness through the UI dispatcher. Therefore #33 never drives attribution, trip completion, or checkpoint durability from EventBus handlers.
- The review observation that `Z21EventPipelineSnapshot.SubscriberFailures` exposes a process-wide EventBus count means it cannot diagnose failures attributable to usage tracking. #33 exposes dedicated rejected-update, duplicate-completion, recovery, checkpoint-success, and checkpoint-failure counters instead.
- These findings do not require RF/#43 changes inside #33: direct backend callbacks establish the counting boundary, and EventBus is limited to coalesced immutable projections and a best-effort solution-save trigger.

## Implementation progress

- Slices 1-2 merged via PR #44: shared lifetime counters, auditable corrections, rolling-stock-neutral maintenance, completed-trip intervals, deterministic combined-plan evaluation, schema coverage, DI registration, and sample data.
- Issue #43 was completed via PR #45; the ordered bounded Z21 event pipeline prerequisite is fulfilled.
- Slices 3-5 are implemented and validated on the follow-up branch: runtime attribution, durable checkpoints/recovery, and authoritative runtime projection.
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

Prerequisite: issue #43 merged via PR #45 (fulfilled).

Deliverables:

- stable journey-run identity;
- explicit active train/consist projection for journey and manual-driving contexts;
- locomotive operating-state rules based on authoritative runtime state;
- idempotent attribution state machine using `TimeProvider`;
- deterministic handling for double traction, consist edits, reconnect, resume, power loss, emergency stop, disconnect, and deleted vehicles.

### Slice 4: Durable checkpoints and recovery

Deliverables:

- atomic periodic checkpoints with a documented maximum loss interval of 30 seconds;
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
| Reordered or repeated Z21 events double count time | Settle monotonic elapsed time at direct authoritative runtime transitions; use stable journey-run identities and idempotent absolute checkpoints |
| PR #45 queue drain completes before UI dispatch | Do not put counting or checkpoint correctness behind EventBus/UI dispatch; publish only coalesced projections and save triggers |
| PR #45 subscriber failures are process-wide | Publish usage-owned diagnostics and never infer usage health from the global EventBus counter |
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

Runtime-slice validation completed with the focused attribution/Journey/SharedUI tests, the full multi-target `dotnet test Test/Test.csproj` run, and the WinUI FastDebug build. Slice 6 retains its own UI behavior and accessibility validation.

## Rollback strategy

- Slice 1 is additive: removing `Usage` properties and the new shared files restores the previous schema without rewriting existing maintenance data.
- Runtime accumulation remains disabled until its complete checkpoint and recovery boundary is present.
- Each later slice must preserve a single authoritative path; superseded locomotive-only behavior is removed only after equivalent shared tests pass.
- No compatibility adapter or migration framework is introduced for the current schema.

## Documentation and completion

- Keep implementation status and acceptance evidence in issue #33.
- Update current architecture and user documentation only when runtime behavior or UI becomes available.
- Delete this plan after issue #33 is completed and merged; the closed issue and Git history retain the implementation record.
