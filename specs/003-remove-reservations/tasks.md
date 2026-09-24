# Tasks: Remove route reservations

**Issue**: [#146](https://github.com/ahuelsmann/MOBAflow/issues/146) | **Spec Kit**: Required

## Foundation

- [x] T001 Inspect runtime/UI/schema and #133 overlap; record `specs/003-remove-reservations/research.md`.
- [x] T002 Complete specification/constitution/cross-artifact analysis in `specs/003-remove-reservations/`.

## US1 - Direct operation

- [x] T003 [US1] Add block-independent command, disconnect and activation-isolation regressions in `Test/Backend/InterlockingRuntimeServiceTests.cs`.
- [x] T004 [US1] Remove route coordinator/safety/conflict/gateway under `Backend/Service/Interlocking/`; adapt runtime service/state and `Backend/Interface/IInterlockingRuntime.cs`.
- [x] T005 [US1] Preserve semantic-turnout/direct-signal coverage under `Test/Backend/`; remove exclusive reservation tests.

## US2 - Observational feedback

- [x] T006 [US2] Preserve FIFO, duplicate feedback and disconnect tests in `Test/Backend/InterlockingRuntimeServiceTests.cs`; keep counters/journeys unchanged.
- [x] T007 [US2] Remove snapshot ownership in `Backend/Service/Interlocking/InterlockingRuntimeState.cs`; adapt affected `Test/` fixtures.

## US3 - Data and UI

- [x] T008 [US3] Remove route models/schema/validation in `Domain/Interlocking/InterlockingDefinition.cs`, `Domain/SignalBoxPlan.cs`, `Backend/Service/Validation/InterlockingDefinitionValidator.cs`, `MOBAflow/Build/Schemas/solution.schema.json`; adapt serialization/validation tests.
- [x] T009 [US3] Remove route commands/drafts/projection from `SharedUI/ViewModel/InterlockingControlViewModel*.cs`; preserve selection/subscription/direct-command tests in `Test/SharedUI/InterlockingControlViewModelTests.cs`.
- [x] T010 [US3] Remove route controls in `MOBAflow/Controls/SelectedObjectWorkbench.xaml*` and page bindings; update help, InfoPage, `docs/JSON-VALIDATION.md`, `plans/34-interlocking-control.md`, affected `specs/001-journey-event-plan/` claims.

## Validation and convergence

- [x] T011 Run focused/full `Test/Test.csproj` suites and host builds after coordination; record `specs/003-remove-reservations/validation.md`.
- [ ] T012 Generate fresh complete Release SARIF and justify reductions in `quality/analyzer-baseline*.json`.
- [x] T013 Run `scripts/Test-LineEndings.ps1`, secrets, governance, residual-reference and diff checks; update this task list with results.
- [ ] T014 Integrate main, commit/push and publish separate Draft PR; report CI/Sonar/manual gates without merging or cleanup.
