# Tasks: Remove vehicle maintenance

**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/147
**Input**: [spec.md](spec.md), [plan.md](plan.md), research, data model and solution contract.
**Tests**: Required for inventory updates and retained persistence/decoder/passport behavior.

## Phase 1: Analysis and Setup

- [x] T001 Inspect affected sources and document scope in `specs/004-remove-maintenance/spec.md`.
- [x] T002 Resolve implementation boundaries and checks in `specs/004-remove-maintenance/plan.md`.

## Phase 2: Foundation

- [x] T003 Check spec/plan/task consistency in `specs/004-remove-maintenance/tasks.md` before code changes.

Analysis: FR-001 maps to T005-T007; FR-002 to T004/T006/T008; FR-003 to T004/T005;
FR-004 to T009; FR-005 to T005/T009. All outcomes and edge cases have a check. No unresolved issue.

## Phase 3: US1 - Manage inventory without maintenance

Independent test: all three vehicle kinds can be searched, selected, added, renamed, deleted and saved;
project switches refresh lists; decoder backups and passports survive; no maintenance is emitted.

- [x] T004 [US1] Add inventory and JSON regressions in `Test/SharedUI/RollingStockInventoryTests.cs` and `Test/Domain/RollingStockSerializationTests.cs`.
- [x] T005 [US1] Remove maintenance models/service/DI and schema in `Domain/`, `Backend/Service/VehicleMaintenanceService.cs`, `Backend/Extensions/MobaBackendServiceCollectionExtensions.cs`, `MOBAflow/Build/Schemas/solution.schema.json`.
- [x] T006 [US1] Remove maintenance from `SharedUI/ViewModel/LocomotiveManagementViewModel.cs`, `Backend/Service/LocomotiveLibraryService.cs` and `Backend/Service/LocomotivePassportHtmlRenderer.cs`; adapt mixed tests under `Test/`.
- [x] T007 [US1] Delete `SharedUI/ViewModel/RollingStockMaintenanceViewModel.cs`, `MOBAflow/Controls/RollingStockMaintenancePanel.xaml` and code-behind; reconnect `MOBAflow/View/LocomotivesPage.xaml`, `PassengerWagonPage.xaml`, `GoodsWagonPage.xaml` and their code-behind; remove WinUI DI registration.
- [x] T008 [US1] Verify and preserve filtered-list updates/autosave in `SharedUI/ViewModel/MainWindowViewModel.Train.cs`, `MainWindowViewModel.Wagons.cs` and the existing project-change handlers.

## Final Phase: Validation and Documentation

- [x] T009 Update `README.md`, `CHANGELOG.md`, `MOBAflow/View/InfoPage.xaml`, `docs/PROJECT-REFERENCE.md`, `docs/wiki/MOBAFLOW-USER-GUIDE.md` and `specs/001-journey-event-plan/`.
- [ ] T010 Run focused/full tests in `Test/Test.csproj`, affected host builds and static checks; record results in `specs/004-remove-maintenance/quickstart.md`.
- [ ] T011 Verify Light/Dark vehicle pages using the manual checklist in `specs/004-remove-maintenance/quickstart.md` only after app-start authorization.
- [ ] T012 Scan changed files, integrate current main, review diff, create draft PR and verify current-commit CI/Sonar; record convergence in `specs/004-remove-maintenance/quickstart.md`. Final merge remains centrally coordinated after #146.

## Dependencies and Execution Order

T001-T003 precede implementation. T004-T008 are one cohesive removal; T009 follows source changes;
T010-T012 validate delivery. T011 is gated by explicit launch permission, not assumed passed.
Documentation T009 could be prepared alongside source edits, but execution here is sequential.
No independent subagents or task issues are needed. All of US1 is the MVP scope.
