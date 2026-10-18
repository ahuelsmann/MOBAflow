# Tasks: Autonomous issue delivery

**Input**: [spec](spec.md), [plan](plan.md), [research](research.md), [contracts](contracts/lifecycle.md), [qualification](qualification.md)
**Source Issue**: [#337](https://github.com/ahuelsmann/MOBAflow/issues/337)
**Status**: Future execution backlog inside this specification; ALL OPEN, none started.
**Prerequisites**: Verified planning integration and accepted handover, applicable recorded D1-D6 decisions, fresh Senior allocation for each later slice. This planning assignment authorizes none of these executions.
**Tests**: Meaningful functional/contract/negative qualification is explicitly required by FR-001-FR-024. Fakes first; live provider/image/pilot operations require their separate scoped grants.

## Path conventions and constraints

Proposed later implementation root Automation/IssueDelivery/ and tests Automation/IssueDelivery.Tests/ are conditional on approval of the .NET architecture in D1. They are outside product assemblies; no project is created now. Evidence/runbook paths below are future deliverables. A decision selecting a different shape requires updating this plan/tasks before source work, not silently inventing paths at execution.

T task IDs are Markdown plan identifiers, not GitHub issues. Zero taskstoissues conversion now; any later conversion needs a separate Senior queue decision and grants no start authority.

## Phase 1: Analysis and Setup

- [ ] T001 Resolve applicable D1-D6 choices with their named decision owners and record original decision references/options/limits in specs/014-autonomous-issue-delivery/decisions.md; no provisioning before D1-D3 and no pilot/cutover before D5/D6.
- [ ] T002 Export and validate original human actor/message ID/link or authenticated export, scope/restrictions/revocation plus separate Senior allocation into the authority contract in specs/014-autonomous-issue-delivery/contracts/lifecycle.md; free-text relays cannot grant authority.
- [ ] T003 Qualify approved Codex/Claude/Argon access, actual required models and strict authenticated standalone scanner in each selected environment; record proof/limits in specs/014-autonomous-issue-delivery/qualification.md without creating a second Claude route.
- [ ] T004 Inventory actual CI/gate/toolchain/image/short-path inputs and current PC mechanisms in specs/014-autonomous-issue-delivery/qualification.md and specs/014-autonomous-issue-delivery/pc-dependencies.md; distinguish existing CI coverage and remaining Windows/Android evidence.

## Phase 2: Foundational Work

- [ ] T005 Define records from data-model.md in Automation/IssueDelivery/State/Records.cs: preserve required fields/relationships/validation verbatim, including "opaque nonempty strings", "monotonically increasing nonnegative integers", full Git OIDs and explicit Unknown; credentials are references, never values.
- [ ] T006 Implement atomic queue revision, capacity, owner, operation intent, event/inbox/outbox and cost reservation in Automation/IssueDelivery/State/TaskStore.cs; enforce "total occupied <=5 atomically" and unique active allocation/semantic operation identities.
- [ ] T007 Define isolated worker/publication/issuer boundaries in Automation/IssueDelivery/Operations/OperationBoundary.cs; no controller credentials in repository-controlled code/env/logs, stale actors cannot mint credentials, unknown in-flight requests prevent transfer.
- [ ] T008 Define gate/evidence contracts in Automation/IssueDelivery/Evidence/GateEvidence.cs using the complete lifecycle.md tuple and Pass/Fail/SkippedByPolicy/Blocked/Unknown/NotRun semantics; no exit-only success. Add atomic rollback/uniqueness/credential-boundary tests in Automation/IssueDelivery.Tests/TaskStoreBoundaryTests.cs and run them before story/provider work.

**Checkpoint**: Foundational isolated functional tests must prove transaction rollback/uniqueness and identity isolation before any user-story/provider execution.

## Phase 3: User Story 1 - Approved admission (Priority: P1)

**Goal**: Eligible work alone receives one complete isolated context.
**Independent Test**: Q01/Q02/Q08; no invalid/over-capacity start.

- [ ] T009 [US1] Add invalid/revoked/contradictory/forged authority and dependency tests in Automation/IssueDelivery.Tests/AdmissionTests.cs, referencing Q01 and original human decision/allocation.
- [ ] T010 [US1] Implement authority/role/precedence resolution in Automation/IssueDelivery/Authority/AuthorityResolver.cs; issue-controlled text remains task data, app/device/railway and unqualified mutations are denied.
- [ ] T011 [US1] Implement admission, per-issue context/branch/workspace and criterion-method map in Automation/IssueDelivery/Admission/AdmissionService.cs with one named writer and approved budget reservation.
- [ ] T012 [US1] Add atomic max-five, duplicate issue/context, old lease and unsupported-capability admission tests in Automation/IssueDelivery.Tests/AllocationTests.cs; include Q02/Q03/Q08/Q22.

## Phase 4: User Story 2 - Evidenced delivery (Priority: P1)

**Goal**: Validated Draft through existing independent review to independently verified integration.
**Independent Test**: Q11/Q12/Q22/Q23/Q25; stale/missing evidence never completes delivery.

- [ ] T013 [US2] Implement versioned envelopes and accepted handover in Automation/IssueDelivery/Evidence/HandoverService.cs; exactly one recipient and distinct observed/send/accepted/writer-end/verified-complete facts.
- [ ] T014 [US2] Implement actual run/count/model/head/base gate ingestion in Automation/IssueDelivery/Evidence/GateCollector.cs; consume trusted CI/Sonar evidence, never synthesize successful checks.
- [ ] T015 [US2] Integrate only the existing Claude owner/route in Automation/IssueDelivery/Providers/ClaudeRoute.cs after T003 and endpoint qualification; Senior Ready, distinct reviewer/fixer/merger, fresh actual Opus 5.5+ review after affected changes; no new routine/dispatch.
- [ ] T016 [US2] Add lower/absent model, self-review, incomplete packet, empty successful output and changed comparison rejection tests in Automation/IssueDelivery.Tests/ReviewEvidenceTests.cs for Q11/Q12.
- [ ] T017 [US2] Implement criterion/documentation-obligation propagation in Automation/IssueDelivery/Evidence/AcceptanceLedger.cs through Draft/fix/integration; preserve required visual/device/manual gates and later integrated-main assessment.
- [ ] T018 [US2] Implement only qualified source/PR/merge/publication operation routes in Automation/IssueDelivery/Operations/GitHubDelivery.cs; M01-M10/M16, no force push, expected-head and exact-base/protection proof, existing Claude merger only, publication M17 excluded absent separate grant.
- [ ] T019 [US2] Add handover/Ready/current-head Sonar zero-issues/merge-readback/actual acceptance/documentation tests in Automation/IssueDelivery.Tests/DeliveryLifecycleTests.cs; Q11/Q12/Q19/Q22/Q23/Q25, no final completion from command exit alone.

## Phase 5: User Story 3 - Safe recovery (Priority: P1)

**Goal**: Restart or duplicate events cannot create conflicting writers or effects.
**Independent Test**: Q03-Q16 under scoped grants, unknown outcome remains blocked.

- [ ] T020 [US3] Add stale worker/controller, in-flight call, failed revocation and lost response cases for each enabled mutation in Automation/IssueDelivery.Tests/OperationFencingTests.cs; Q03/Q04/Q06/Q08/Q10, no lease-only takeover.
- [ ] T021 [US3] Implement accepted owner/controller transfer and qualified identity/egress exclusion in Automation/IssueDelivery/Ownership/OwnershipService.cs; preserve old owner until actual neutralization/reconciliation and recipient receipt.
- [ ] T022 [US3] Implement operation classification, endpoint preconditions and reconciliation in Automation/IssueDelivery/Operations/MutationExecutor.cs using M01-M18; unsupported rows disabled, no clientMutationId idempotency assumption or blind fire/merge retry.
- [ ] T023 [US3] Implement checkpoint/restart reconciliation in Automation/IssueDelivery/Recovery/RecoveryService.cs covering dirty state/processes/head/base/CI/subscriptions/decisions/budget/last accepted handover; invalidate affected evidence.
- [ ] T024 [US3] Add duplicate/reordered/historical events and unknown-process/dirty-workspace/provider-limit recovery tests in Automation/IssueDelivery.Tests/RecoveryTests.cs; Q05/Q06/Q09/Q16, no destructive cleanup or duplicated review.
- [ ] T025 [US3] Qualify failed-build and missing/empty/invalid SARIF with unchanged baseline hashes in Automation/IssueDelivery.Tests/GateQualificationTests.cs against scripts/Test-AnalyzerBaseline.ps1; Q13, reported historical event not assumed defect and no baseline write on failure.
- [ ] T026 [US3] Qualify all autonomously used gate exits with meaningful negative samples in Automation/IssueDelivery.Tests/GateExitTests.cs, including scripts/Test-LineEndings.ps1 -Staged and zero selected tests; Q14, printed finding plus exit zero never Pass; propose any confirmed fix separately.
- [ ] T027 [US3] After separate image/resource approval, run fresh-image Windows/Android canaries and applicable portable suite, recording actual SDK/workload/image/tools/short path and both required platform targets in specs/014-autonomous-issue-delivery/qualification.md; Q15, no device launch.

## Phase 6: User Story 4 - Persistent decisions and cost (Priority: P2)

**Goal**: Material decisions survive interruption and no uncovered paid work starts.
**Independent Test**: Q16-Q19; authentic answers only, one logical notice and honest cost.

- [ ] T028 [US4] Add restart/duplicate/silence/revocation/unavailable-recipient tests in Automation/IssueDelivery.Tests/DecisionNotificationTests.cs; Q17/Q19 and original authority provenance.
- [ ] T029 [US4] Implement persistent decisions/outbox/existing-channel plain-text summaries in Automation/IssueDelivery/Notifications/DecisionService.cs; reason/recommendation/options/consequences/waiting behavior and actual worker/known remaining cost, no dashboard prerequisite.
- [ ] T030 [US4] Implement approved ceiling/reservation/running-exposure accounting and existing-evidence metrics in Automation/IssueDelivery/Costs/BudgetService.cs; "Nonnegative amounts, no mixed-currency arithmetic", no silent overage/fallback, reserve before paid start.
- [ ] T031 [US4] Add exhausted budget/subscription, delayed billing, cancel-sent-versus-ended and safe stop tests in Automation/IssueDelivery.Tests/BudgetTests.cs for Q16/Q18; zero uncovered starts, waiting does not imply zero cost.

## Phase 7: User Story 5 - PC-off qualification and controlled transfer (Priority: P2)

**Goal**: One scheduler preserves owners and delivers one explicitly approved issue without local PC participation.
**Independent Test**: Q20/Q21/Q24 plus all applicable earlier qualification; unmet manual gates block completion.

- [ ] T032 [US5] Obtain Senior's approved candidate and full admission/method/remaining-acceptance map, recording human objective and D5 allocation in specs/014-autonomous-issue-delivery/qualification.md; no automatic task/pilot issue creation.
- [ ] T033 [US5] Implement accepted checkpoint/import/freeze/epoch-transfer/rollback in Automation/IssueDelivery/Migration/CutoverService.cs from the existing queue; snapshot owners/processes/CI/subscriptions/pending effects/decisions/budgets, never two dispatchers.
- [ ] T034 [US5] Add interruption-at-each-cutover-step and rollback tests in Automation/IssueDelivery.Tests/CutoverTests.cs for Q21; actual source exclusion and target receipt required before activation.
- [ ] T035 [US5] After D5/D6 and all relevant qualifications, execute exactly one authorized PC-off issue, recording cloud/controller/worker/review/CI IDs, local nonparticipation window and remaining gates in specs/014-autonomous-issue-delivery/qualification.md; Q20, existing Claude integration route.
- [ ] T036 [US5] Implement closure/retention/archive guard in Automation/IssueDelivery/Evidence/ClosureService.cs and Q24 tests in Automation/IssueDelivery.Tests/ClosureTests.cs; no archive on Draft/CI alone and no deletion of unaccepted evidence.
- [ ] T037 [US5] Complete independent Senior integration/cost/baseline comparison and operational acceptance in specs/014-autonomous-issue-delivery/qualification.md; preserve unknown historical metrics, no inferred wider pilot authorization.

## Final Phase: Validation and Documentation

- [ ] T038 Write the qualified authority/recovery/escalation/budget/acceptance runbook in docs/AI-DEVELOPMENT.md and update docs/PROJECT-REFERENCE.md and docs/ARCHITECTURE.md where actual implemented boundaries change.
- [ ] T039 Assess canonical docs/wiki/INSTALLATION.md and docs/wiki/MOBAFLOW-USER-GUIDE.md plus docs/SPEC-KIT.md per integrated slice; record exact corrections or concrete NoChange, named remaining drift and recheck after behavior fixes against actual main.
- [ ] T040 Run allocated meaningful functional/contract/negative tests in Automation/IssueDelivery.Tests/ and affected consumers/CI on matching platforms; zero intended tests is not validation; retain configuration/evidence map in specs/014-autonomous-issue-delivery/qualification.md.
- [ ] T041 Run changed-file secrets, applicable structural/link/line-ending and staged checks for future Automation/IssueDelivery/ and specs/014-autonomous-issue-delivery/ changes; create Draft PR, verify current-head CI and Sonar success with zero OPEN/CONFIRMED issues before Senior Ready, no local Sonar code analysis.
- [ ] T042 Re-evaluate Constitution Check in specs/014-autonomous-issue-delivery/plan.md and hand exact scope/head/base/actual checks/provenance/options/limits/dirty status/writer-end to PM; Senior alone Ready and independent integration verification, existing Claude plan alone review/fix/fresh-review/SHA squash.

## Dependencies and Execution Order

T001-T004 precede applicable source/provisioning work; T005-T008 block stories. US1 -> US2/US3 -> US4 -> US5 -> final acceptance. Within each phase tasks execute in listed order unless a later centrally allocated plan explicitly proves independence. T018 depends on T020-T022 qualification before enabling any real external route; T015 similarly depends on existing-route qualification. T027 requires explicit image/resources grant; T035 depends on T003/T012/T019-T031/T032-T034 and required manual evidence. No test fixture grants permission for provider calls.

T001 decisions are staged: D1-D3 before provisioning; D4 before affected unsupported/risky operation; D5 before pilot; D6 before cutover. Early implementation cannot assume future D5/D6 answers.

## Parallel opportunities and implementation strategy

No parallel agents/writers are authorized by this planning assignment. Future disjoint test fixtures (AdmissionTests/DecisionNotificationTests or GateExitTests/GateQualificationTests) could be separately allocated after common contracts stabilize, within Senior's maximum-five ceiling. They are not marked [P] now because dependencies and actual future allocation are not yet satisfied.

Smallest safe increment: authority/state/admission with isolated fakes (US1), then complete safety/delivery/recovery/cost qualification before any PC-off pilot. A successful admission-only increment is not pilot readiness. User stories have independent test oracles; live delivery remains dependent on all safety prerequisites.
