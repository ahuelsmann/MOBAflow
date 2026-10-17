# Tasks: Locomotive usage in train consists

**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/306

**Input**: [spec.md](spec.md), [plan.md](plan.md), [research.md](research.md), [data-model.md](data-model.md), [runtime contract](contracts/runtime-query.md), [presentation contract](contracts/presentation.md), [quickstart.md](quickstart.md).

**Status**: Implementation after the explicit green handoff on 2026-10-16 and the authorized focused desktop Light/Dark and keyboard acceptance were completed on product source 8e40f8a5. Ingenieur is the sole author. Actual evidence and the broader checks not performed (manual reload/receipt-race, model editing, screen reader, 200-percent system font/DPI, Android/TalkBack and hardware) are recorded in [quickstart.md](quickstart.md). Current-head CI/Sonar, independent review and final integration remain with Issue Master.

**Tests**: Meaningful regression tests below use NUnit/Moq and existing fakes. No app/hardware launch without separate approval; documentation-only planning checks are distinct from product verification.

## Phase 1: Analysis and Setup

- [x] T001 Refresh affected code against the then-current main, coordinate #219/#304 shared-UI ownership, and record changes to the evidence base in `specs/013-locomotive-train-usage/research.md` before product work. (FR-012, FR-013)
- [x] T002 Confirm exact build/test fixtures and platform prerequisites in `specs/013-locomotive-train-usage/quickstart.md`; do not count absent fixtures or zero tests as success. (FR-013)

## Phase 2: Foundational Work

- [x] T003 Define the pure input/result contract in `Backend/Interface/ILocomotiveTrainReferenceAnalyzer.cs`: project-local fixed type Locomotive, source-specific TargetState, immutable per-entry locations, `VehicleIndex` zero-based and display index+1, rule codes `LTU001`/`LTU002`, distinct RuleCode/FindingId, stable deterministic ordering. Empty/duplicate train identity means invalid/unknown scope, not fabricated uniqueness. (FR-001, FR-004, FR-006)
- [x] T004 Define `Backend/Interface/ILocomotiveTrainRuntimeUsageReader.cs` with ReadAsync/IsCurrentAsync, non-empty request/project IDs, expected project identity, optional non-empty target ID, cancellation, UTC observation time, opaque request-local receipt and explicit coverage/reasons. B/C are always Unknown/NotInspectedInSlice; A Complete never makes aggregate runtime Complete. No CanDelete/IsUnused field. (FR-005, FR-007, FR-008, FR-009)

**Checkpoint**: Small contracts express exactly this read relationship and the A/B/C limit; they contain no UI/network/lifecycle ownership.

## Phase 3: User Story 1 - Find uses in edited data (Priority: P1)

**Goal**: Exact uses from current edited data, including unsaved and repeated entries.

**Independent Test**: Compare the arranged source/project/type/position set with the complete copied result.

- [x] T005 [US1] Add failing cases in `Test/Backend/LocomotiveTrainReferenceAnalyzerTests.cs` for zero/one/multiple/repeated uses, missing requested target, equal IDs in foreign projects/wagon kinds, empty IDs and ambiguous train identities. (FR-001, FR-002, FR-004; SC-001)
- [x] T006 [US1] Implement explicit VehicleKind/VehicleId traversal and copied deterministic locations in `Backend/Service/Validation/LocomotiveTrainReferenceAnalyzer.cs`; one source/project at a time, no names/addresses for identity and no I/O. (FR-001, FR-002, FR-004, FR-006; SC-001, SC-004)
- [x] T007 [US1] Add editor-query cases in `Test/SharedUI/LocomotiveTrainUsageViewModelTests.cs`: unsaved edits, correct target/project, in-place reload, cancellation and zero saves/model mutations. (FR-002, FR-009, FR-011; SC-003, SC-005)
- [x] T008 [US1] Add read orchestration in `SharedUI/ViewModel/LocomotiveTrainUsageViewModel.cs`: capture edited findings on the established model thread, own one cancelable request context and use existing ISolutionSession load/model/selection events. Do not use Solution reference equality alone. (FR-002, FR-009, FR-013; SC-003)
- [x] T009 [US1] Forward selected target and Show usage through `SharedUI/ViewModel/LocomotivesPageViewModel.cs`; preserve existing Delete/Save/Reset behavior and cover that in `Test/SharedUI/LocomotivesPageViewModelTests.cs`. (FR-001, FR-011, FR-012; SC-005)

**Checkpoint**: US1 works independently with the runtime reader fake returning Unknown; no global non-use promise.

## Phase 4: User Story 2 - Find broken references (Priority: P1)

**Goal**: The same relation detects missing locomotive targets in existing diagnostics.

**Independent Test**: Diagnose, repair and refresh one broken entry; valid references stay neutral.

- [x] T010 [US2] Add missing/empty-target and repair cases in `Test/Backend/LocomotiveTrainReferenceAnalyzerTests.cs`; require one precise finding per affected entry from the same traversal used for uses. (FR-003, FR-004; SC-001)
- [x] T011 [US2] Implement missing-target findings in `Backend/Service/Validation/LocomotiveTrainReferenceAnalyzer.cs`, preserving source-specific existence and repeated positions. (FR-003, FR-004; SC-001)
- [x] T012 [US2] Add regression cases in `Test/Backend/ProjectDiagnosticsServiceTests.cs` and `Test/SharedUI/MainWindowDiagnosticsTests.cs` for Error mapping, neutral explicit uses, stable IDs, replacement/no accumulation and successful source-specific repair. (FR-003, FR-010; SC-004, SC-006)
- [x] T013 [US2] Adapt missing references into `Backend/Service/Validation/ProjectDiagnosticsService.cs`; preserve structured typed locations, existing validators and severity rules. Never parse Message for identity. (FR-003, FR-004, FR-010, FR-012; SC-004)

**Checkpoint**: US2 is independently verifiable through project diagnostics; it requires no runtime or Delete decision.

## Phase 5: User Story 3 - Compare runtime evidence (Priority: P1)

**Goal**: Real A capture, explicit B/C Unknown, and no stale response treated as current.

**Independent Test**: Test actual reader integration plus projection fakes for differing sources and delayed invalid responses.

- [x] T014 [US3] Add owner tests in `Test/Backend/LocomotiveTrainRuntimeUsageReaderTests.cs`: actual clone differs from editor/original ProjectRuntime.Project, non-selected/disconnected owner, missing/wrong-context owner and same GUID in a foreign load. (FR-005, FR-006, FR-008; SC-001, SC-002)
- [x] T015 [US3] Add race controls in `Test/Backend/LocomotiveTrainRuntimeUsageReaderTests.cs`: consistent capture, later definition change, successful answer followed by same-ID ReplaceAsync or Host.DisposeAsync before apply, and unchanged-owner positive control. (FR-006, FR-009; SC-003)
- [x] T016 [US3] Add integration regression cases in `Test/Backend/ProjectRuntimeHostTests.cs` and `Test/Backend/MobaRuntimeServiceProjectIsolationTests.cs` to prove matching actual owners and clones, cancellation/disposal handling and no alteration of existing lifecycle/commands. (FR-005, FR-009, FR-011, FR-012; SC-003, SC-005)
- [x] T017 [US3] Add owner-local consistent read/revalidation in `Backend/Service/MobaRuntimeService.RuntimeApi.cs` under existing activation synchronization; use the analyzer, copy values and expose no mutable Project/Train/Vehicle. (FR-005, FR-006, FR-011; SC-001, SC-005)
- [x] T018 [US3] Forward reader operations through the existing instance in `Backend/Service/ProjectRuntimes/ProjectRuntimeFactory.cs`; original editable Project is an identity guard only, never runtime occurrence data. (FR-005, FR-006, FR-013)
- [x] T019 [US3] Implement reader/membership/receipt handling on `Backend/Service/ProjectRuntimes/ProjectRuntimeHost.cs`: host-gate then activation-lock order, real A capture, independent B/C Unknown, structured unavailable/errors and cancellation; account for DisposeAsync bypassing the host gate. No new runtime/generation/lease or RequestFactory execution. (FR-005, FR-006, FR-007, FR-008, FR-009, FR-011; SC-002, SC-003, SC-005)
- [x] T020 [US3] Add projection cases in `Test/SharedUI/LocomotiveTrainUsageViewModelTests.cs`: editor-only/runtime-only findings, A Complete zero with B/C Unknown, failures/unsupported remote, successful response then replacement/disposal, in-place load, late receipt-check result and target change. (FR-007, FR-008, FR-009, FR-010; SC-002, SC-003)
- [x] T021 [US3] Extend `SharedUI/ViewModel/LocomotiveTrainUsageViewModel.cs` to await actual owner reads, revalidate receipts before apply, discard obsolete revalidation answers, observe existing all-project snapshot hints and marshal raw callbacks through IUiDispatcher. Cancel/unsubscribe on disposal. No operator/runtime/connection role in the ViewModel. (FR-005, FR-007, FR-008, FR-009, FR-013; SC-002, SC-003)
- [x] T022 [US3] Add Messages cases in `Test/SharedUI/MainWindowDiagnosticsTests.cs` for separate editor/A results, failed refresh replacing stale Complete with Unknown, no accumulation and always-visible coverage when Information rows are filtered. (FR-010; SC-004, SC-006)
- [x] T023 [US3] Merge scoped query/coverage through the existing projection in `SharedUI/ViewModel/MainWindowViewModel.Diagnostics.cs` and minimal existing-area bindings in `MOBAflow/View/MainWindow.xaml`; exact English source/location/time labels, no new page/navigation. (FR-004, FR-007, FR-010, FR-013; SC-004, SC-006)
- [x] T024 [US3] Register only the pure analyzer in `Backend/Extensions/MobaBackendServiceCollectionExtensions.cs` and cover shared resolution in `Test/Backend/MobaBackendServiceCollectionExtensionsTests.cs`; in `MOBAflow/Extensions/MobaWinUiServiceCollectionExtensions.cs` alias the reader to the same already registered ProjectRuntimeHost singleton alongside its existing aliases and compose the desktop-only focused ViewModel. Verify alias identity and required dependencies in `Test/MOBAflow/MobaWinUiServiceCollectionExtensionsTests.cs`. Do not add a reader/host dependency to common Backend or Android composition, which has no ProjectRuntimeHost. (FR-013)
- [x] T025 [US3] Use recording fakes in `Test/Backend/LocomotiveTrainRuntimeUsageReaderTests.cs` and `Test/SharedUI/LocomotiveTrainUsageViewModelTests.cs` to prove zero query-triggered Save/Delete/Reset, model changes, workflow/RequestFactory, Z21 and runtime Load/Add/Replace/Update/Start calls. (FR-011, FR-012; SC-005)

**Checkpoint**: Actual A capture works and is tested; B/C remain visibly Unknown. No deletion safety claim.

## Final Phase: Validation and Documentation

- [x] T026 Execute intended new fixtures then the relevant portable full runtime/DI suite from `Test/Test.csproj` using commands in `specs/013-locomotive-train-usage/quickstart.md`; report discovered counts, failures/skips and existing diagnostics/Delete regression outcomes. (FR-011, FR-012, FR-013; SC-001, SC-002, SC-003, SC-004, SC-005)
- [x] T027 Compile portable `MOBApi/MOBApi.csproj` and desktop `MOBAflow/MOBAflow.csproj`, run the Windows `Test/Test.csproj` target; compile `MOBAsmart/MOBAsmart.csproj` on a matching workload host if shared registration affects it. Record actual evidence in `specs/013-locomotive-train-usage/quickstart.md`. (FR-013)
- [x] T028 After explicit launch authorization, check `MOBAflow/View/MainWindow.xaml` Messages in Light/Dark and by keyboard, source/coverage filtering, target changes and precise locations. Record remaining manual gates in `specs/013-locomotive-train-usage/quickstart.md`; no hardware actions. (FR-010, FR-013; SC-006)
- [x] T029 Update applicable documentation in `docs/ARCHITECTURE.md` and `docs/wiki/MOBAFLOW-USER-GUIDE.md` for the read-only query/A-B-C limits, and public XML documentation in `Backend/Interface/ILocomotiveTrainReferenceAnalyzer.cs` and `Backend/Interface/ILocomotiveTrainRuntimeUsageReader.cs`; preserve no-Delete/no-format-change scope. (FR-012, FR-013)
- [x] T030 Run changed-file secrets, line endings, links, Spec Kit governance and diff checks using `scripts/Test-LineEndings.ps1`, `scripts/Test-DocumentationLinks.ps1` and `scripts/Test-SpecKitGovernance.ps1`; create the explicitly authorized draft PR, run code analysis exclusively through GitHub PR CI and verify a green Sonar quality gate on the current PR commit with zero OPEN/CONFIRMED issues. Never run local Sonar code analysis or mark ready/merge. Track evidence in `specs/013-locomotive-train-usage/quickstart.md`. (FR-012, FR-013)

## Dependencies and execution strategy

T001 -> T002 -> T003/T004. US1: T005 -> T006 -> T007 -> T008 -> T009. US2: after T006, T010 -> T011 -> T012 -> T013. US3: after T004/T006, T014 -> T015 -> T016 -> T017 -> T018 -> T019 -> T020 -> T021 -> T022 -> T023 -> T024 -> T025. Final T026-T030 follow all story checkpoints.

US2 can independently validate static errors; US1 can use an Unknown-runtime fake. The entire issue requires real US3 A integration. Do not report fake-only semantic tests as real owner coverage. Test tasks precede associated implementation tasks. Registration completion is required before reporting host integration complete.

No tasks carry [P] because shared paths and requested sole authorship make sequential execution the default. After foundations, a future authorized team could prepare US2 diagnostics tests and US3 owner tests in different files, but must serialize edits to the shared analyzer and perform integrations in dependency order. This parallelism example grants no additional authority; the assigned engineer starts only after the explicit green handoff. No subagents are authorized.

## Requirement and outcome coverage

| Requirement | Tasks |
| --- | --- |
| FR-001 | T003, T005, T006, T009 |
| FR-002 | T005, T006, T007, T008 |
| FR-003 | T010, T011, T012, T013 |
| FR-004 | T003, T005, T006, T010, T011, T013, T023 |
| FR-005 | T004, T014, T016, T017, T018, T019, T021 |
| FR-006 | T003, T014, T015, T017, T018, T019 |
| FR-007 | T004, T019, T020, T021, T023 |
| FR-008 | T004, T014, T019, T020, T021 |
| FR-009 | T004, T007, T008, T015, T016, T019, T020, T021 |
| FR-010 | T012, T013, T020, T022, T023, T028 |
| FR-011 | T007, T009, T016, T017, T019, T025, T026 |
| FR-012 | T001, T009, T013, T016, T025, T026, T029, T030 |
| FR-013 | T001, T002, T008, T018, T021, T023, T024, T026, T027, T028, T029, T030 |
| SC-001 | T005, T006, T010, T011, T014, T017, T026 |
| SC-002 | T014, T019, T020, T021, T026 |
| SC-003 | T007, T008, T015, T016, T019, T020, T021, T026 |
| SC-004 | T006, T012, T013, T022, T023, T026 |
| SC-005 | T007, T009, T016, T017, T019, T025, T026 |
| SC-006 | T012, T022, T023, T028 |

## Notes

Planning completion is not completion of any product task. Respect existing architecture, MVVM, DI, UI dispatch, safe startup and English UI conventions. No full workflow tracking, new remote endpoint, automatic cleanup, Delete policy or lifecycle framework belongs to this backlog.

## Phase 6: Convergence

Assessment on 2026-10-16 checked 13 functional requirements, 6 success criteria, 12 user-story acceptance scenarios, 11 architecture/contract decisions, 6 constitution principles and all 30 existing tasks. No additional product-code gap was identified. Two partial delivery/acceptance gaps remain; there are no missing, contradicting or unrequested buildable changes. Both are MEDIUM and retain the existing scope and authorization limits.

- [x] T031 Complete the focused Light/Dark, keyboard, source/coverage-filter and precise-location acceptance check in existing Messages after explicit app-launch authorization; record actual observations in `specs/013-locomotive-train-usage/quickstart.md` and close T028 only with that evidence, per FR-010, FR-013, SC-006 and Constitution II/III (partial).
- [x] T032 Finish changed-file/governance publication checks, independent fixed-comparison reviews, the authorized Draft PR and current-head GitHub CI/Sonar verification with zero OPEN/CONFIRMED issues; record concrete evidence in `specs/013-locomotive-train-usage/quickstart.md` and close T030 only when satisfied, per plan: Publication, T030 and Constitution VI (partial).

## Phase 7: Claude review corrections

The user authorized correcting both P3 findings from the independent Claude CLI review of 22346a7d..4c866c1b, testing the fixes, updating Draft PR #309 and running a fresh read-only Claude CLI review. The failed first CLI attempt is not review evidence; only claude-review-4c866c1b-retry.json is the valid prior report. T032 includes the independent review already completed at 4c866c1b and its current-head external gates; the user also requires a fresh CLI review after these corrections. Final post-commit CI/Sonar and fresh review results are recorded in the Draft PR against its actual published head, preserving that comparison. Scope remains read-only usage/diagnostics; no app/device/hardware start or readiness/merge action.

- [x] T033 Reproduce an inline/queued snapshot hint before first observation publication with stale and unchanged-owner controls in `Test/SharedUI/LocomotiveTrainUsageViewModelTests.cs`; preserve the existing requested-check flag until an observation can be checked and consume pending hints before publishing in `SharedUI/ViewModel/LocomotiveTrainUsageViewModel.cs`, without spinning, rescanning or creating a new runtime role. Validate cancellation, late replies and coalescing regressions. (FR-009, FR-011, FR-013; SC-003, SC-005)
- [x] T034 Reproduce invalid/duplicate train/locomotive identity in normal Messages with all severity filters off and no explicit usage query in `Test/SharedUI/MainWindowDiagnosticsTests.cs`; carry typed Unknown coverage from `Backend/Service/Validation/ProjectDiagnosticsService.cs` and keep that status visible through `SharedUI/ViewModel/MainWindowViewModel.Diagnostics.cs`. Preserve neutral use severity, no fabricated missing-reference findings and successful repair behavior. Run relevant/full target and consumer/analyzer gates; record actual local evidence in `specs/013-locomotive-train-usage/quickstart.md`. (FR-007, FR-008, FR-010; SC-002, SC-004, SC-006)
