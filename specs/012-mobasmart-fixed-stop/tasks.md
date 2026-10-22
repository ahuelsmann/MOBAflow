# Tasks: MOBAsmart fixed selected-locomotive STOP

**Input**: [plan.md](plan.md), [spec.md](spec.md), research/data-model/contracts/quickstart.
**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/304
**Tests**: Meaningful command regressions and Android compile; no XAML text-mirror tests.

## Phase 1: Analysis and Setup

- [x] T001 Inspect MOBAsmart/View/ControlPage.xaml, MOBAsmart/Controls/FunctionToggleView.xaml.cs, SharedUI/TrainControl/TrainControlViewModel.cs and applicable rules.
- [x] T002 Complete specification, clarification, plan, research, data model and contracts in specs/012-mobasmart-fixed-stop/ before product changes.

## Phase 2: Foundational Work

- [x] T003 Add focused current-target/nonzero-speed STOP state regression in Test/SharedUI/TrainControlViewModelPerformanceTests.cs, preserving baseline transport semantics.
- [x] T004 Run the planned shared-command baseline from Test/Test.csproj and record actual discovery/execution in specs/012-mobasmart-fixed-stop/quickstart.md.

No shared model/service prerequisites or package changes.

## Phase 3: User Story 1 - Reach STOP while inspecting functions (P1)

**Independent test**: Five scroll positions, selected identity/address/one scoped STOP, all old controls reachable.

- [x] T005 [US1] Reorganize MOBAsmart/View/ControlPage.xaml into Auto header/* bounded ScrollView, preserving EmergencyStopCommand and other bindings (FR-001/002/004).
- [ ] T006 [US1] Perform authorized Android scrolling/selection/STOP checks from specs/012-mobasmart-fixed-stop/quickstart.md; leave pending without launch permission (SC-001/002).

## Phase 4: User Story 2 - Identify current target (P2)

**Independent test**: A/B current-target commands and unchanged local/remote routing.

- [x] T007 [US2] Verify current identity/address and command source in MOBAsmart/View/ControlPage.xaml and execute Test/SharedUI/TrainControlHostSelectionTests.cs, MobileRuntimeCoordinatorTests.cs and RuntimeCommandGatewayRoutingTests.cs (FR-003/007).

## Phase 5: User Story 3 - Enlarged text and accessible operation (P2)

**Independent test**: Android Light/Dark x100/150/200%, long names, >=44 targets, accessible meaning/state and an operable complete function row.

- [x] T008 [US3] Adapt local STOP sizing (minimum 48) and identity (two visual lines then ellipsis, full accessible name plus DCC) in MOBAsmart/View/ControlPage.xaml using existing theme resources without disabling font autoscaling (FR-005/006); require a whole row within the function section.
- [x] T009 [US3] Preserve FunctionToggleView ICommand forwarding while updating wrapping/minimum 48-unit touch area in MOBAsmart/Controls/FunctionToggleView.xaml and meaning/state refresh in FunctionToggleView.xaml.cs (FR-004/006); leave actual TalkBack activation as device acceptance.
- [ ] T010 [US3] Perform authorized Android text/theme/accessibility/touch acceptance in specs/012-mobasmart-fixed-stop/quickstart.md; leave pending without launch permission (FR-005/006).

## Phase 6: Validation and Documentation

- [x] T011 Restore and FastDebug-compile MOBAsmart/MOBAsmart.csproj for net10.0-android and rerun affected Test/Test.csproj regressions after edits (FR-008).
- [x] T012 [P] Update docs/wiki/MOBASMART-USER-GUIDE.md and actual evidence/limits in specs/012-mobasmart-fixed-stop/quickstart.md.
- [x] T013 Run scripts/Test-SpecKitGovernance.ps1, documentation links, scripts/Test-LineEndings.ps1 including staged mode, changed-file secrets and git diff checks; record convergence in specs/012-mobasmart-fixed-stop/tasks.md.
- [x] T014 Commit only owned files, explicitly push codex/issue-304-mobasmart-fixed-stop and create/attach a draft PR linked to #304; record URL in specs/012-mobasmart-fixed-stop/quickstart.md. Completed at the historical 2026-10-15 publication; evidence restored on 2026-10-22. This checkbox does not claim a new push or current-head CI/acceptance.
- [ ] T015 Verify current-head GitHub CI/Sonar and zero OPEN/CONFIRMED PR issues; preserve draft and pending Android acceptance in specs/012-mobasmart-fixed-stop/quickstart.md.

## Dependencies and Execution Order

T001-T004 precede product code. US1/T005 precedes US2 source inspection and US3 sizing. T008-T009 edit different files but remain sequenced under one author. T011 follows all product edits; T012 may run independently of build execution. T013 precedes commit/publication; T015 follows T014. T006/T010 are manual acceptance gates, not permission to launch.

## Implementation Strategy

Deliver the fixed header first, then directly necessary accessibility and validation. No new infrastructure, runtime fixes or broad refactor. Preserve one selected-locomotive command activation, without claiming exactly-once network sends. Complete local reviewable work while external/manual gates remain explicitly open.

## Phase 7: Convergence

Assessment after implementation: 8 FRs, 4 SCs, 7 acceptance scenarios, all 15 existing tasks, 7 plan decision groups and all 6 constitution principles checked. Source inspection and the 39-test/Android-compile evidence show no additional missing, contradicting or unrequested product work. Remaining findings are partial validation/delivery, not a claim of full acceptance.

- [ ] T016 Complete the remaining actual Android scrolling, selection, system-text/theme, short-height and TalkBack evidence in specs/012-mobasmart-fixed-stop/quickstart.md per FR-005/006/008, SC-001/002/003 and T006/T010 (partial, HIGH); preserve specific unexecuted cases and offline command limits.
- [ ] T017 Complete draft PR publication and current-commit CI/Sonar verification in specs/012-mobasmart-fixed-stop/quickstart.md per plan: Sonar gates and T014/T015 (partial, MEDIUM); keep the PR draft and do not infer readiness from local checks.
