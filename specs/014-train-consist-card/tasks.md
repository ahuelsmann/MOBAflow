# Tasks: Train consist card

**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/321

**Input**: [spec.md](spec.md), [plan.md](plan.md), [research.md](research.md), [data-model.md](data-model.md), [presentation contract](contracts/presentation.md), [quickstart.md](quickstart.md).

**Status**: Planned only. All 24 tasks are unstarted. This planning assignment does not authorize their execution.

**Tests**: Meaningful synthetic regression checks are required by #321 and the repository constitution. Actual UI, digital output and physical print evidence are separate. No railroad hardware test or Android gate is added.

**Organization**: Single allocated writer; no parallel agents or second queue. Paths below are future implementation targets unless already present.

## Phase 1: Analysis and setup

- [ ] T001 Refresh the assigned scope, current source/comparison and applicable instructions, verify the planned Windows integration against `MOBAflow/View/TrainsPage.xaml.cs` and `MOBAflow/Extensions/MobaWinUiServiceCollectionExtensions.cs`, and record any material change in `specs/014-train-consist-card/research.md` before implementation (FR-001, FR-009, FR-016).

## Phase 2: Foundational contracts

- [ ] T002 Define immutable snapshot/position/diagnostic records colocated in `SharedUI/Service/TrainCardBuilder.cs` and the narrow `SharedUI/Interface/ITrainCardPrintService.cs` contract: ProjectId/TrainId "Preserve the observed GUIDs; never generate replacement source identities."; ProjectName/TrainName "Copy user-authored text; blank values get explicit unnamed display labels without writing back."; CreatedAt "Capture once as DateTimeOffset with a visible offset; never recapture for printing."; SourceLabel 'Exactly "Planned consist · Edited Solution data".'; SourceState "Available or Unavailable; an unavailable occurrence collection is not an empty consist."; SourceDiagnostics "Immutable copied list; invalid project/train identity is disclosed."; Positions "Immutable copied sequence; one element per source occurrence, including malformed/null slots." (FR-002 through FR-008, FR-015).

## Phase 3: User Story 1 - Read the planned train (Priority: P1)

**Goal**: Produce an honest read-only card for the selected edited train.

**Independent test**: Seven-position fixture plus typed/project/identity/error cases, with no I/O/runtime effects.

- [ ] T003 [US1] Add `Test/SharedUI/TrainCardBuilderTests.cs` with the seven-position fixture and empty/unavailable, missing locomotive/wagon, wrong-kind/project, empty/duplicate IDs, undefined kind, null slot, blank names, source ambiguity (including duplicate project IDs in the owning Solution) and repeated occurrence cases; assert no mutations and deep-copy isolation after source edits, preserving the provided creation time (FR-002 through FR-009, FR-015; SC-001, SC-002, SC-005).
- [ ] T004 [US1] Implement copying and typed project-local resolution in `SharedUI/Service/TrainCardBuilder.cs`: SourceIndex "Zero-based original index; never compact after an invalid reference."; Position "Exactly SourceIndex + 1, including repeated references."; VehicleId/VehicleKind "Preserve the raw reference values when present; null slots retain their index and a null-entry diagnostic."; Name/KindLabel/IdentityText "Copied display strings; full GUID/type remain available and are not claimed to be a physical identity."; ConfiguredDccAddress 'Copy the nullable locomotive DigitalAddress only; null displays "DCC not configured", never a fabricated number.'; Diagnostics "Immutable position-local list, including reference location and a text explanation."; require exactly one same-kind match, retain invalid positions, and reuse compatible Messages coordinates without changing the locomotive analyzer (FR-002 through FR-009, FR-015).
- [ ] T005 [US1] Add `Test/SharedUI/TrainCardViewModelTests.cs` for current/stale/no selection, source identity, time capture, frozen card after source edits, Close/reopen and strict zero-save/runtime/operator-command behavior using session and print fakes (FR-001, FR-007 through FR-009; SC-002, SC-005).
- [ ] T006 [US1] Implement Open/Close and current-card state in `SharedUI/ViewModel/TrainCardViewModel.cs` using ISolutionSession, the pure builder, injected TimeProvider and print contract; copy without awaiting mutable source reads, validate actual project membership, retain no history and avoid all editor mutation/sync callbacks (FR-001, FR-002, FR-007 through FR-009).
- [ ] T007 [US1] Wire the separate card ViewModel into `MOBAflow/View/TrainsPage.xaml` and `MOBAflow/View/TrainsPage.xaml.cs`; add the read-only `MOBAflow/View/TrainCardDialog.xaml`/`.xaml.cs` reading surface and Windows-only DI in `MOBAflow/Extensions/MobaWinUiServiceCollectionExtensions.cs`, preserving existing TrainViewModel/editor duplicate policy and host ownership (FR-001, FR-003 through FR-007, FR-009, FR-015).

**Checkpoint**: A generated card preserves every source occurrence and an honest source state; this is not full issue acceptance without US2 and US3.

## Phase 4: User Story 2 - Use the card accessibly (Priority: P1)

**Goal**: Read and control the card using keyboard, themes and larger text.

**Independent test**: Keyboard open/close and focus return at normal/200% text in both themes.

- [ ] T008 [US2] Complete wrapped/scrollable names, full identity and diagnostic text, accessible names, ThemeResource styles, initial focus, keyboard action traversal and safe focus return in `MOBAflow/View/TrainCardDialog.xaml`, `MOBAflow/View/TrainCardDialog.xaml.cs` and `MOBAflow/View/TrainsPage.xaml.cs`; keep code-behind limited to visuals/lifecycle and no editor bindings (FR-010, FR-011).
- [ ] T009 [US2] Validate card action/state tests in `Test/SharedUI/TrainCardViewModelTests.cs` and refine exact keyboard/focus, no-selection, narrow viewport, Light/Dark and 200% text checks in `specs/014-train-consist-card/quickstart.md`; execute actual UI acceptance only under the later assigned launch permission and record it in T018 (FR-001, FR-010, FR-011, FR-016; SC-003).

**Checkpoint**: Data tests and eventual actual UI checks agree on the reading behavior; a compile or fake test does not prove focus/visual acceptance.

## Phase 5: User Story 3 - Print the same planned card (Priority: P2)

**Goal**: Readable A5 pages from exactly the open card's content.

**Independent test**: Conserved ordered fragments in pure tests, exact snapshot handoff in fakes, then actual multipage and overheight output.

- [ ] T010 [US3] Add `Test/SharedUI/TrainCardPaginatorTests.cs` for content conservation/order, exact page count, fit boundaries, 60 positions, overheight continuations, long headers/footers, nonpositive printable area and a fragment larger than an empty usable page; assert consumed fragments or explicit failure, never a blank-page loop (FR-012, FR-013; SC-004).
- [ ] T011 [US3] Implement pure measured-fragment pagination in `SharedUI/Service/TrainCardPaginator.cs` with occurrence/field/range identity, measured header/footer reserve, same-position continuation labels and final page count; ensure no lost/duplicated fragments and preserve all snapshot data (FR-008, FR-012, FR-013).
- [ ] T012 [US3] Extend `Test/SharedUI/TrainCardViewModelTests.cs` to validate Print passes the exact immutable card/time, prevents overlapping print jobs, retains contents across cancellation/unsupported/failure and explicit retry, and never saves/mutates/starts/updates/sends commands (FR-008, FR-009, FR-014; SC-002, SC-005).
- [ ] T013 [US3] Implement `MOBAflow/Service/TrainCardPrintService.cs`: window-bound PrintDocument/PrintManagerInterop, capability/error handling, A5 portrait request with actual supported media/imageable area, measured wrapped line fragments, at least 11-point body text and larger requested text, reusable finalized preview/submission pages, host callback marshalling and complete unregistration/disposal; expose Completed/Cancelled/Unsupported/Failed without claiming physical output (FR-008, FR-009, FR-012 through FR-014, FR-016).
- [ ] T014 [US3] Add awaited Print/status/cancellation behavior in `SharedUI/ViewModel/TrainCardViewModel.cs`, bind it in `MOBAflow/View/TrainCardDialog.xaml` and compose the transient host adapter in `MOBAflow/Extensions/MobaWinUiServiceCollectionExtensions.cs`; repaginate option changes from the existing card without regenerating its timestamp or reading the edited source (FR-008, FR-011, FR-014).

**Checkpoint**: Synthetic pagination and exact snapshot tests pass; actual A5 output and physical paper remain T019/T020.

## Final phase: Validation and documentation

- [ ] T015 Run the affected SharedUI build and nonzero TrainCard-filtered tests in `SharedUI/SharedUI.csproj` and `Test/Test.csproj` using `specs/014-train-consist-card/quickstart.md`; record scope and results (FR-016; SC-001, SC-002, SC-004, SC-005).
- [ ] T016 Restore and compile `MOBAflow/MOBAflow.csproj` on Windows using the documented FastDebug command, verify new XAML remains compiled and no active Page is removed, and complete applicable current-head CI/analyzer checks; do not launch as part of build (FR-010 through FR-014, FR-016).
- [ ] T017 Validate cross-boundary no-mutation/no-save/no-owner/no-command behavior in `Test/SharedUI/TrainCardBuilderTests.cs` and `Test/SharedUI/TrainCardViewModelTests.cs`, and run existing `Test/SharedUI/TrainsPageViewModelTests.cs` and `Test/Backend/LocomotiveTrainReferenceAnalyzerTests.cs` if touched integration affects them; preserve their existing editor/diagnostics contracts (FR-009, FR-015; SC-005).
- [ ] T018 Under actual assigned launch permission, execute keyboard/focus, repeated open/close, print-cancel-return, Light/Dark and normal/200% text scenarios from `specs/014-train-consist-card/quickstart.md`; record exact commit, setup, results and limits separately from automated checks (FR-010, FR-011, FR-014, FR-016; SC-003, SC-006).
- [ ] T019 Under the assigned print acceptance scope, execute native callback/driver and actual digital A5 output scenarios in `specs/014-train-consist-card/quickstart.md` for seven positions, 60 positions, option changes, long header and an overheight single occurrence; compare the full content/time/diagnostics and page associations with the screen, recording actual media/margins/scale (FR-008, FR-012 through FR-014, FR-016; SC-002, SC-004, SC-006).
- [ ] T020 Record the separate actual physical A5 print/readability/content-completeness result, or the concrete remaining acceptance limitation, in `specs/014-train-consist-card/quickstart.md`; never treat preview/digital output/spooler completion as a paper pass or invent layout/hardware evidence (FR-016; SC-006).
- [ ] T021 Update `docs/wiki/MOBAFLOW-USER-GUIDE.md`, `docs/PROJECT-REFERENCE.md` and `docs/ARCHITECTURE.md` for actual feature availability, edited-data provenance, keyboard/print flow and limitations; reassess after behavior-changing review fixes and check against integrated main (FR-007, FR-015, FR-016).
- [ ] T022 Review the final scoped diff, public XML docs and Constitution Check in `specs/014-train-consist-card/plan.md`; run changed-file secrets scans and `scripts/Test-LineEndings.ps1 -Path <changed-paths>` (Fix then recheck if needed), stage only assigned files, run `scripts/Test-LineEndings.ps1 -Staged`, and validate links and `scripts/Test-SpecKitGovernance.ps1` without lowering gates (FR-009, FR-016).
- [ ] T023 Deliver a Draft PR for the actual implementation with exact head/base and `specs/014-train-consist-card/quickstart.md` evidence; verify current CI, green Sonar quality gate (SonarCloud) and zero OPEN/CONFIRMED PR issues, and hand over writer stop and remaining acceptance limits to Chief Project Manager; only Chief Project Manager sets Ready and the existing Claude plan then performs independent review/fix/re-review/merge (FR-016; SC-006).
- [ ] T024 After the existing integration route, verify actual merged status/resulting main and documentation/wiki accuracy through the responsible owner, and update `specs/014-train-consist-card/tasks.md` and acceptance evidence only for actually verified work; no product implementation is claimed from this planning PR (FR-016; SC-006).

## Dependencies and execution order

T001 -> T002 -> US1 (T003-T007) -> US2 (T008-T009) -> US3 (T010-T014) -> final validation (T015-T024).

Tests precede their behavioral implementations: T003 before T004, T005 before T006, T010 before T011, T012 before T014. T007 establishes the reading dialog; T008 completes accessibility. T013 requires T011; T014 requires T012/T013. T018-T020 require the appropriate later launch/print assignment; permission is not inferred from the plan.

No [P] tasks are enabled under this single-writer allocation. Conceptually independent examples for later central allocation: US1 builder tests versus host reading layout after the contract stabilizes; US2 evidence preparation versus styles; US3 pure paginator versus adapter binding after fragment contracts stabilize. These are not authority to start parallel agents/writers. The smallest internal increment is US1's truthful screen card, but #321 is not deliverable without US2/US3 and the actual acceptance record.

## Requirement coverage

| Requirement | Tasks |
| --- | --- |
| FR-001 | T001, T005-T009 |
| FR-002 | T002-T004, T006 |
| FR-003 | T002-T004, T007 |
| FR-004 | T002-T004, T007 |
| FR-005 | T002-T004, T007 |
| FR-006 | T002-T004, T007 |
| FR-007 | T002-T007, T021 |
| FR-008 | T002-T006, T011-T014, T019 |
| FR-009 | T001, T003-T007, T012-T013, T017, T022 |
| FR-010 | T008-T009, T016, T018 |
| FR-011 | T008-T009, T014, T016, T018 |
| FR-012 | T010-T013, T016, T019 |
| FR-013 | T010-T013, T016, T019 |
| FR-014 | T012-T014, T016, T018-T019 |
| FR-015 | T002-T004, T007, T017, T021 |
| FR-016 | T001, T009, T013, T015-T016, T018-T024 |
| SC-001 | T003-T004, T015 |
| SC-002 | T003-T006, T012-T014, T015, T019 |
| SC-003 | T008-T009, T018 |
| SC-004 | T010-T013, T015, T019 |
| SC-005 | T003-T006, T012, T015, T017 |
| SC-006 | T018-T024 |

## Planning boundary

24/24 task checkboxes are intentionally open. Planning quality checklist marks specification review only. No extra task issues, workers, automations, prototype, app/device/hardware actions or independent review runs are created here.
