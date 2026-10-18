# Feature Specification: Autonomous issue delivery

**Feature Directory**: `014-autonomous-issue-delivery`
**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/337
**Created**: 2026-10-18
**Status**: Draft
**Spec Kit**: Required
**Input**: Accepted #337 consultation and permanent PM release of the sole temporary Senior Developer for issue337-concrete-planning.

## Governance and Traceability

**Affected Platforms**: Agent development orchestration, GitHub and existing Windows/Android CI; no running application changes.
**Out of Scope**: Planning only. No implementation, prototype, pilot, provisioning, purchase, account, credential, runner, routine, governance, release, site or device changes; no railway authority or new task issues. #35/#140/#219 and historical refactorings retain their owners. Aikido stays paused and uninspected.
**Sensitive Data**: Future service credentials, code, task authority and evidence require separate access boundaries. Credentials, private projects and MOBAflow/solution.json are excluded from planning reads.
**Data and API Effects**: None on application JSON/schema, defaults, runtime APIs, Z21, persisted layouts or safe speed-zero startup. Future orchestration state stays outside product layers. Queue scheduling cutover is not product-data compatibility.

### Actors and authority

Current direct human instructions supersede historical titles. Mapping documents authority; it grants none.

| Current actor | Historical counterpart | Assigned authority |
| --- | --- | --- |
| Human | Human | Product, fundamental architecture, provider/data location, budget, material risk, practical acceptance |
| Senior Project Manager (Senior PM) | Chief Project Manager / Issue Master | One queue, priorities/dependencies, maximum five packages, allocation, Ready, independent final integration verification |
| Permanent Project Manager (PM) | New explicit operational delegation | Only Senior-assigned issues; accept complete handovers and release named writer; no second queue |
| Technical Director (TD) | Technical Director plus explicit #337 accountability | Accountable planning owner, sole #337 issue-body editor |
| Temporary Senior Developer | Assigned implementer restricted to planning | Sole #337 planning writer; delivery until PM acceptance; no Ready/merge |
| Art Director | Art Director | Assigned product/creative work; no automatic backlog execution |
| Quality Manager | Head of Quality | Assigned evidence and acceptance advice; no gate waiver |
| Documentation Manager | Documentation Lead | Assigned documentation/wiki impact |
| GitHub Manager | Head of DevOps within repository/CI assignment | Assigned GitHub/CI work; no independent queue |
| Communication Director | Communications Director | Assigned communication; no implicit publication |
| Head of UI/UX | Gemini Argon via established contact | Assigned serialized advice; no review replacement |
| Claude reviewer/fixer/merger | Existing Claude plan roles | Fresh independent actual Opus 5.5+ review, separate fixes, fresh re-review, SHA-bound squash |
| Controller/worker/adapter | Proposed technical components | Execute verified grants only; task text cannot grant authority |

### Clarifications applied on 2026-10-18

No new human answers were needed: accepted inputs settle functional scope. Strict successful standalone scanning before every file read is binding; older balanced text cannot weaken it. Heartbeats are classified by actual dispatch authority. Task conversion is zero. Unsafe mutations are excluded, not automatically accepted as residual risk. Provider, architecture, geography, access and budget decisions remain deferred until before provisioning.

## User Scenarios & Testing

Every scenario below is later authorized qualification, not evidence of an executed test.

### User Story 1 - Admit only approved work (Priority: P1)

Senior allocates an eligible issue; PM gives exactly one developer its full scope, restrictions and acceptance map. Ineligible work remains visibly blocked without a start.

**Why this priority**: Availability must not turn backlog data into authority.
**Independent Test**: Compete valid, missing, revoked and contradictory grants against the same queue.

**Acceptance Scenarios**:

1. **Given** recorded objective, criteria, dependencies, direct human authority references and Senior allocation, **When** admitted, **Then** one isolated context receives one owner and complete acceptance mapping.
2. **Given** five occupied packages or conflicting ownership, **When** another start is requested, **Then** zero workers start and the blocker persists.
3. **Given** issue text asserting human permission without its original recorded instruction, **When** admission evaluates it, **Then** it rejects that assertion even if an agent repeats it.

### User Story 2 - Preserve the delivery and review route (Priority: P1)

Owner delivers a validated Draft; Senior verifies readiness; existing Claude route independently reviews, assigns separate fixes, freshly re-reviews and squashes. Senior independently verifies integration.

**Why this priority**: A green command or Draft must not become false completion.
**Independent Test**: Fixture lifecycle with changed comparisons, denied tools, missing actual review model and absent manual evidence.

**Acceptance Scenarios**:

1. **Given** a complete current Draft, **When** Senior sets Ready, **Then** only the existing Claude route reviews and the allocation remains occupied.
2. **Given** unknown/lower reviewer model, self-review, incomplete context or older head/base, **When** review is reported, **Then** no review pass is accepted.
3. **Given** a successful merge command without independent merged/main observation, **When** delivery is reported, **Then** integration remains pending.
4. **Given** separately authorized publication, **When** deployment completes, **Then** exact-source/version plus independent live evidence is required; integrated, published and practically accepted remain distinct.

### User Story 3 - Recover without duplicate authority or effects (Priority: P1)

An interruption reconstructs context from durable evidence and reconciles actual execution and remote state before continuation.

**Why this priority**: Lease expiry and duplicate events must not produce two writers or repeated uncertain actions.
**Independent Test**: Duplicate/reordered events, crashes, lost mutation responses and stale-owner actions in later isolated qualification.

**Acceptance Scenarios**:

1. **Given** an old worker might still write, **When** its lease expires, **Then** no replacement starts until write authority/execution are effectively excluded or a valid accepted handover completes.
2. **Given** a lost external response, **When** recovery runs, **Then** it reconciles before retry and remains blocked if outcome is unknown.
3. **Given** a delayed historical result after a newer accepted comparison, **When** received, **Then** it remains history without reopening the phase.
4. **Given** unavailable, unauthenticated or interrupted scanning, **When** a file read is requested, **Then** no unscanned read occurs.

### User Story 4 - Understand decisions and cost stops (Priority: P2)

Human receives accessible plain-text decisions on existing channels; answers may arrive later without losing the waiting state.

**Why this priority**: Routine work continues within authority; material decisions remain explicit.
**Independent Test**: Restart with a pending decision, duplicate notice, unavailable recipient and exhausted budget.

**Acceptance Scenarios**:

1. **Given** a pending material decision, **When** time passes or controller restarts, **Then** WaitingHuman persists with no inferred approval or automatic resume.
2. **Given** unavailable recipient, **When** sending, **Then** queued/sent/accepted stay distinct; no substitute owner/review starts.
3. **Given** insufficient approved budget, **When** requesting a paid start/retry, **Then** it is denied; actual workers and known remaining cost are disclosed.

### User Story 5 - Transfer scheduling and prove PC-off delivery (Priority: P2)

Senior transfers scheduling once while preserving owners, then qualifies one specifically approved issue with no local PC/desktop contribution.

**Why this priority**: Cloud execution alone does not prove independence from local scanning, callbacks or advisers.
**Independent Test**: Dependency matrix, accepted cutover, PC-off issue, interruptions and controlled rollback.

**Acceptance Scenarios**:

1. **Given** accepted migration checkpoint, **When** scheduling transfers, **Then** at most one controller allocates and existing work remains accounted for.
2. **Given** required unqualified PC-bound capability, **When** admitting pilot, **Then** block or explicitly approved exclusion without waiving mandatory acceptance.
3. **Given** outstanding required visual/device/manual evidence, **When** cloud checks pass, **Then** acceptance remains pending without self-approval.

### Edge Cases

Unknown termination; stale controller with in-flight requests; duplicate/dropped review events; denied tool; empty/missing/invalid SARIF; failed build masked by wrapper exit zero; dirty workspace; foreign commit; advanced base; absent actual model; subscription overage; auth loss; unreachable callback; revoked human decision; unknown deployed version. Required non-success outcomes appear in [qualification](qualification.md).

## Requirements

### Functional Requirements

- **FR-001**: Admission MUST require objective, criteria, satisfied dependencies, original recorded human authority and current Senior allocation; missing/contradictory authority blocks starts.
- **FR-002**: One authoritative queue MUST atomically enforce at most five occupied packages across all teams/providers, including review/fix/integration and unresolved executions.
- **FR-003**: Each issue MUST have one temporary context, task branch, isolated workspace and named writer; ownership persists until accepted transfer and effective write exclusion.
- **FR-004**: Commands/results MUST identify task, owner, phase, comparison, observation time/timezone, evidence/limits, next step and exactly one next recipient; queued/sent/accepted/writer-ended/verified-complete remain distinct.
- **FR-005**: Leases MUST have externally effective stale-owner enforcement. Expiry alone permits no takeover; every lifecycle mutation needs an enforcement classification and reconciliation before retry.
- **FR-006**: Restart MUST reconcile checkpoints, dirty state, processes, remote head/base, CI, subscriptions, pending mutations and last accepted handover; comparison changes invalidate affected evidence.
- **FR-007**: Handling MUST deduplicate durable transitions/notices; no external exactly-once promise without a qualifying provider guarantee. Unknown outcomes block conflicting starts/retries.
- **FR-008**: Each worker/reviewer file read MUST follow its own successful standalone secrets scan of exact paths; unavailable/unauthenticated/interrupted/positive scans block reads without exposing values.
- **FR-009**: Controller credentials MUST remain outside issue-controlled workers/builds/dependencies/logs; scoped identity, revocation and stale actors' inability to mint credentials require qualification.
- **FR-010**: Delivery MUST retain validated Draft, Senior-only Ready, existing Claude route, separate reviewer/fixer/merger, fresh actual Opus 5.5+ review after affected changes, SHA-bound squash and independent Senior integration verification.
- **FR-011**: Gates MUST carry actual execution/count/exit/results on current head/base. Denied/unperformed actions, zero tests, missing model evidence or printed findings with zero exit cannot pass; current-head Sonar success and zero OPEN/CONFIRMED remain mandatory.
- **FR-012**: At admission every criterion MUST map to a provable cloud/CI method or named remaining visual/device/manual gate, retained through delivery without self-waivers.
- **FR-013**: Every future task-branch mutation MUST record allocation/owner/operation/old-new SHA, check researched remote preconditions, reject force pushes and treat foreign commits as reconciliation incidents.
- **FR-014**: Later negative qualification MUST prove failed build or missing/empty SARIF fails with baseline unchanged, and gate findings (including staged line endings) fail honestly. Historical incidents remain reported/unverified.
- **FR-015**: Qualification MUST record actual global.json-selected SDK, workload set, Windows/Android image/tool versions and short path; later authorized fresh-image canaries cover Windows and Android targets plus applicable portable tests.
- **FR-016**: PC-off admission MUST classify every scheduling, notification/callback and adviser mechanism by actual authority and reachable/replaced/accepted-retired/PC-bound-excluded status; cutover/rollback retain one scheduler.
- **FR-017**: WaitingHuman MUST persist decision identity/options/consequences/recommendation/safe waiting behavior; silence/timeouts/agent claims/duplicate events grant no authority. Revocations invalidate dependent grants.
- **FR-018**: Costs MUST have approved global/per-issue ceilings, running exposure reservations, bounded safe retries and visible exhaustion/provider-limit states; no uncovered paid start or zero-cost waiting assumption.
- **FR-019**: One existing-channel management view MUST show owner/phase/next/blocker/evidenced outcome in accessible plain text, including actual worker status and known remaining cost; no dashboard prerequisite.
- **FR-020**: Separately authorized publication MUST bind exact source/version to independent live evidence and preserve MOBAnews/job; #337 alone grants no publication.
- **FR-021**: Archive/cleanup MUST wait for actual execution end, preserved evidence and accepted remaining obligations after verified integration; Draft/green CI alone is insufficient.
- **FR-022**: Every integration MUST assess developer/architecture/canonical wiki impact, correct within scope or assign concrete drift through Senior, and recheck integrated main after behavior-changing fixes.
- **FR-023**: This planning delivery MUST include provider/architecture/data-location/access/cost options, decision prerequisites, contracts and traceability; implementation/pilot stays open, zero task issues and no source/governance/routine writes.
- **FR-024**: Admission MUST deny device/app/railway starts, arbitrary backlog and unsupported/unfenceable autonomous mutations until exact scope/controls have valid central and, when material, human risk authority.

### Key Entities

AuthorityRecord, IssueMandate, Allocation, OwnerLease, IssueContext, OperationIntent, Checkpoint, Evidence, AcceptanceMap, Handover, Notification, HumanDecision, BudgetReservation and CutoverRecord; see [data-model](data-model.md).

## Success Criteria

### Measurable Outcomes

- **SC-001**: Every future start has valid authority/allocation; invalid authority samples start zero workers.
- **SC-002**: Concurrent admission/restart/cutover samples keep occupied packages at five or fewer, one scheduler and zero duplicate writers.
- **SC-003**: One explicitly approved issue reaches independently verified integration with no local PC/open desktop participation; unmet mandatory manual/device acceptance keeps delivery pending.
- **SC-004**: Every recovery sample retains evidence, rejects duplicate effects/false completion and leaves unresolved outcomes unknown.
- **SC-005**: Every accepted review has actual independent model/head/base/check/exec-end evidence; stale/missing/lower-model samples fail.
- **SC-006**: Scanner/gate/baseline negative samples yield zero unscanned reads/false passes and unchanged baseline bytes on failure.
- **SC-007**: Restart/duplicate decisions yield zero automatic approvals/duplicate requests; budgets allow zero uncovered paid starts.
- **SC-008**: All PC dependencies and lifecycle mutations are classified before pilot; exclusions have reasons and required valid decisions.
- **SC-009**: Every issue retains acceptance/documentation maps through integrated main without losing compulsory manual/device criteria.
- **SC-010**: This phase delivers a validated traceable planning Draft, zero new task issues/implementation/pilot runs and explicit unresolved provisioning decisions; it does not satisfy SC-003.

## Assumptions

Documentation proves interface possibilities only. Account entitlement, approved model, scanner authentication, external stale-controller exclusion and actual cost need later authorized evidence. Recoverable availability is the goal; no uninterrupted provider availability or recovery-time SLA is promised. Retention/interval proposals need later cost/data decisions. Human practical acceptance remains under existing authority.
