# Implementation Plan: Autonomous issue delivery

**Branch**: `codex/issue-337-cloud-planning` | **Date**: 2026-10-18 | **Spec**: [spec.md](spec.md)
**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/337
**Spec Kit**: Required
**Status**: Draft
**Input**: specs/014-autonomous-issue-delivery/spec.md and the accepted planning-only PM handover.

## Summary

Plan one durable controller for the Senior's existing queue, isolated issue workers, enforceable authority boundaries and recovery through the existing Claude route. This PR implements none of those components. [Research](research.md) compares managed orchestration with a small self-hosted service; [decisions](decisions.md) keeps provider, data location, budget and material risk choices open before provisioning.

## Technical Context

**Language/Version**: Repository .NET 10; candidate control service C#/.NET 10 outside product assemblies. This is a proposed implementation shape, contingent on the architecture decision.
**Primary Dependencies**: Existing GitHub CI, Codex noninteractive execution, existing Claude cloud review routine and serialized Gemini Argon adviser; no new dependency now.
**Storage/Protocols**: Candidate transactional state + event history + evidence objects, HTTPS/Git and provider session APIs. Managed durable service or PostgreSQL-backed service are alternatives; no store selected.
**Testing**: Future isolated control-plane functional/contract/negative tests; NUnit where .NET is selected. Existing product CI stays authoritative for its covered boundaries.
**Target Platforms**: Linux-capable control/ordinary code workers; Windows/WinUI and Android compilation through existing CI unless a demonstrated gap justifies a separately approved worker.
**Affected Layers**: New development tooling only; Domain/Common/Backend/SharedUI and application hosts unchanged.
**Performance Goals**: Safety invariants are absolute (one scheduler, maximum five packages, no duplicate writers). Proposed event reconciliation at up to five-minute intervals, bounded by provider limits and budget; not an uptime/recovery SLA. Observe actual latency in qualification.
**Data and API Effects**: No product JSON/schema/default/API/Z21/layout effects. Separate orchestration contracts only.
**Scale/Scope**: One repository, one central queue, maximum five packages. No general-purpose backlog executor or parallel review routine.

## Constitution Check

Checked before research and re-evaluated after design; applicable direct instructions take precedence over historical guidance. All checks refer to planning/design, not executed qualification.

- [x] Product layer/platform boundaries, centralized EventBus UI dispatch and safe startup are untouched.
- [x] Proposed asynchronous tooling uses await; no sync-over-async or product ViewModel/UI changes.
- [x] English artifacts; no shipped UI change, hence Light/Dark checks do not apply to this PR.
- [x] Data/API effects are explicit; no product compatibility/migration layer.
- [x] Documentation-only validation uses structural/link/command/secrets/line-ending checks, no .NET builds/tests.
- [x] Future behavior has meaningful positive/negative functional and boundary qualification tasks.
- [x] Reuse existing CI/review/adviser routes and avoid speculative product abstractions.
- [x] Specification and plan reference authoritative #337; artifacts remain together under specs/.
- [x] Strict per-read scanning supersedes older balanced examples; reviewer inputs use the same rule.
- [x] Sonar code analysis runs only in PR CI; no local code analysis or hooks.
- [x] PR starts/remains Draft; Senior alone may set Ready after current-head Sonar success and zero OPEN/CONFIRMED issues.
- [x] Existing Claude plan alone owns independent review/fix/re-review/squash; this planning writer cannot implement or merge.
- [x] No provider, budget, credential, runner, routine or governance commitment in this package.

Historical constitution references to IMobaRuntime and balanced scanning are not changed here. No runtime contract is being implemented; current direct runtime/scanning instructions prevail. Later governance inventory appears below.

## Project Structure

This PR changes only specs/014-autonomous-issue-delivery/: spec, plan, research, decisions, data-model, contracts, pc-dependencies, qualification, traceability, quickstart and requirements checklist.

Proposed later source paths, conditional on approved .NET architecture:

- Automation/IssueDelivery/: Authority, State, Admission, Ownership, Operations, Recovery, Providers, Evidence, Notifications, Costs and Migration modules.
- Automation/IssueDelivery.Tests/: isolated contract/functional/negative fixtures.
- docs/AI-DEVELOPMENT.md, docs/PROJECT-REFERENCE.md and docs/ARCHITECTURE.md: later workflow/tool boundary documentation.
- docs/wiki/: only pages whose user-facing acceptance/workflow actually changes.
- Existing CI/gate/baseline files remain unchanged until a specifically allocated, evidence-backed task.

The controller is development infrastructure, not a product runtime service. No new project/solution entry is created now.

## Architecture and invariants

1. Existing Senior queue is the sole authority. The chosen store hosts its controlled successor after accepted cutover; events, outbox and projections are parts of that queue, not another scheduling queue.
2. Only authenticated authority ingestion and Senior allocation may issue a grant. Issue bodies, comments, build output, model output and relayed claims are untrusted task data.
3. A transaction validates queue revision, capacity, dependencies, issue eligibility, owner and budget reservation, then records grant/event/outbox. Constraint conflict aborts; serializable retries re-read before internal retry.
4. Isolated workers have local workspace write scope, no central write credentials or ability to mint them. They return scanned patches/results to a trusted publication boundary. Repository-controlled code cannot reach that credential process.
5. A proposed trusted operation boundary validates active controller epoch, allocation/owner/phase/comparison and the endpoint-specific precondition at dispatch. It is NOT itself proof of external fencing: in-flight calls, old credentials and replacement broker exclusion must be qualified.
6. Lease loss stops new dispatch. No replacement starts until the old process, pending requests, writable workspace and external identities are effectively excluded and actual outcomes reconciled. A timeout alone never proves that.
7. Only classified, qualified operations enter the autonomous allowlist. Unfenceable/unsupported operations stay disabled; a concrete material risk decision is necessary where applicable and cannot waive repository quality gates.
8. Store inbox/outbox dedup gives one accepted internal transition per event/operation identity. External effects remain at-least-once or uncertain where provider behavior requires it; no generic exactly-once claim.
9. Workers/checks are immutable comparison observations. Head/base changes invalidate affected evidence and review. Senior Ready hands the same occupied package to the existing Claude plan.
10. Independent reviewer, fixer and merger identities/contexts remain separate. The proposed publication boundary is technical enforcement of named ownership, not a new manager, queue or merge permit.

See [contracts](contracts/lifecycle.md), [external mutations](contracts/mutations.md) and [data model](data-model.md).

## Phases and dependencies

### Phase A - Current planning delivery

Specify/clarify, evidence research, plan/contracts, future tasks, read-only cross-artifact analysis, focused doc validation and Draft PR. No task conversion or future execution. No Spec Kit extensions.yml exists at this base, so before/after hooks for these phases are absent. Explicit feature environment variables avoid changing .specify metadata outside the assigned document scope.

### Phase B - Decisions and authorized qualification setup

Senior accepts planning integration. Human resolves D1-D6 as applicable; Senior allocates a later implementation/qualification slice and approved pilot candidate. Export recorded human authority with original message/decision identity; verify provider/account/model/scanner/access separately. No source task can treat this plan as its start grant.

### Phase C - Implement isolated contracts and qualify boundaries

Build admission/state/identity/publication/recovery with fakes, then separately authorized provider tests. Prove stale-worker/controller exclusion for every enabled mutation. Gate and fresh-image canaries precede pilot admission. No historical incident becomes an automatic bugfix.

### Phase D - PC-off pilot, recovery and controlled transfer

Qualify the [PC matrix](pc-dependencies.md), transfer only after recorded source-owner acceptance, exercise [scenarios](qualification.md) and deliver one already-approved issue. Existing owners/manual gates survive. Cost/incident stop can block further starts at any point.

### Phase E - Operational acceptance

Senior independently verifies pilot integration and documentation against main. Record actual performance/cost, remaining limitations and whether controlled wider use is authorized. No automatic next issue, pilot expansion or task issue conversion.

## Cutover and rollback

1. Senior records the sole current scheduler, its actual dispatch-capable paths and all active packages. Snapshot queue revision/hash, owner grants, dirty workspaces, processes, CI, subscriptions, pending operations/handoffs/decisions/budgets.
2. Prepare a read-only target import. Validate counts/identities and evidence accessibility without dispatch credentials. Do not refresh unrelated branches or stop foreign runs.
3. Coordinate safe points with each owner; accept explicit obligations. Disable old allocation dispatch at its actual enforcement boundary. Monitoring-only heartbeats may remain read-only; names do not establish dispatch rights.
4. Prove old scheduler/controller cannot allocate, old provider requests are reconciled and no old publication identity remains usable for conflicting work. If this cannot be proved, stop migration; do not start the target.
5. Activate exactly one target epoch using atomic ownership transfer; accept the imported state. Replay deduplicated events and reconcile external reality before starts. Record source/target receipts and observed times.
6. Prove PC-off path without local wakeups/callbacks/scans or adviser calls; dependencies marked excluded must match human-approved pilot scope.
7. Retire replaced dispatch/callback paths only after accepted handoff and retention. Current planning edits none.
8. Rollback freezes new target dispatch, reconciles in-flight work/cost and neutralizes its write identity first. Import the latest accepted checkpoint to the former route and activate one new epoch only after Senior acceptance. Never run both to improve availability. Unknown effects remain blocked under the current owner.

## Validation Strategy

**This PR**: Own successful secrets scan before reads; review diff; explicit feature prerequisite checks; tasks format and full traceability; Spec Kit governance with only changed artifacts plus scanned issue templates; focused local-link/anchor checks; line-ending fix/recheck and staged check; git diff checks. No .NET build/test/canary. No local Sonar code analysis.

**PR CI**: Observe actual current head/base and required checks. Document-only paths are classified by scripts/Get-QualityChangeScope.ps1; executed structural jobs and legitimate build/test skips must be distinguished. Sonar current-head check must be success and zero OPEN/CONFIRMED before Ready; absent/NONE is unknown, not pass.

**Future qualification**: [qualification.md](qualification.md) supplies positive/negative oracle, evidence/owners and remaining manual gates. Existing Quality Windows/portable/analyzer checks remain relevant; Android Release AAB is currently manual workflow_dispatch with validate_android, so ordinary PR success is NOT Android release validation. Any later dispatch must be separately scoped/allocated.

**Issue traceability**: #337; zero generated TNNN/pilot issues now. Markdown T001 identifiers in tasks.md are local plan items, not GitHub issues.

## Documentation and canonical wiki impact

Planning-only NoChange: no product/runtime/contributor process has been activated, so current user wiki and architecture descriptions are not rewritten to claim cloud operation. This feature folder explicitly records proposed behavior and limitations.

Later implementation owner + Documentation Manager must assess docs/AI-DEVELOPMENT.md, docs/PROJECT-REFERENCE.md, docs/ARCHITECTURE.md, docs/SPEC-KIT.md and canonical docs/wiki/INSTALLATION.md / docs/wiki/MOBAFLOW-USER-GUIDE.md where affected. Developer recovery/cost/authority runbook belongs with tooling documentation; no speculative end-user instructions. Record exact affected pages or concrete NoChange per integrated slice; reassess after behavior-changing fixes and Senior checks actual main. Any remaining drift needs named owner and concrete follow-up through the same queue. No external wiki/site publication is implied.

## Later governance delta inventory (no edits now)

| Source/rule | Potential later delta | Boundary |
| --- | --- | --- |
| AGENTS.md and master-coordination: role/responsibility table | Document current Senior/PM delegation and restricted cloud component roles | Current human mapping already governs; no authority invented |
| master-coordination: local codex queue callback and synchronous Argon path | Supported cloud receipt/serialization route after qualification | No new advisor session or installation now |
| master-coordination: existing heartbeats and queue state | Record accepted single-scheduler cutover/recovery | Preserve cadence/monitoring unless separately instructed |
| AGENTS.md, Sonar policy, docs/SPEC-KIT.md, constitution principle VI: balanced examples | Reflect already-binding strict per-read instruction when centrally assigned | Not a strict-versus-balanced decision |
| docs/AI-DEVELOPMENT.md and operational runbook | Accepted identity/recovery/budget/evidence rules | No reopening completed leadership consultation |

Routine wording alignment can be centrally assigned without a new blanket human approval. Material authority, architecture, risk, cost or operational-scope changes still require their proper recorded decisions.

## Complexity Tracking

No constitutional exception is requested. Durable state, event history and a trusted operation boundary are justified by restart/authority requirements; a single polling script cannot exclude stale external writers. Additional dashboards, distributed worker pools, new CI runners or event brokers are not baseline requirements. Provider-dependent mechanisms remain gated options, never unproven guarantees.
