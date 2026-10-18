# Data model: authoritative orchestration records

Source: [#337](https://github.com/ahuelsmann/MOBAflow/issues/337). Proposed logical model, no schema or product state implemented.

## Records and constraints

All identifiers are opaque nonempty strings, timestamps are RFC3339 UTC plus original zone/offset, hashes identify scanned content only, and revisions/epochs are monotonically increasing nonnegative integers. Full Git OIDs are retained, never abbreviated in enforced comparisons. Credentials are references to a protected issuer, never values in these records. Unknown evidence is explicit, not false/success.

| Entity / unique identity | Required fields and relations | Validation and ownership |
| --- | --- | --- |
| AuthorityRecord / decisionId+revision | Original human actor/message ID/source link or authenticated export, scope/actions/restrictions, effectiveAt, supersedes/revokes | Verify source human and precedence; agent relay is not authority; retain original record |
| IssueMandate / repository+issue+mandateRevision | taskKey, objective, criteria, dependency refs, authority refs, exclusions, phase scope, PM/TD/delivery owner | Must match current Senior allocation and not exceed original human scope |
| Allocation / allocationId | taskKey, queueRevision, owner, phase, controllerEpoch, occupancy, budget reservation, acceptedAt | One active allocation per task; total occupied <=5 atomically; Senior controls updates |
| OwnerLease / allocationId+leaseEpoch | writerId, workspaceId, branchRef, expiresAt, identity refs, external exclusion evidence, pending op IDs | One writable owner per workspace; expired does not mean fenced or transferable |
| IssueContext / contextId | mandate/allocation/role refs, repo/base/head, paths, phase, limits, evidence and acceptanceMap refs | Reconstruct without chat memory; per issue; no private project/credential data |
| OperationIntent / operationId | allocation/owner/controller epochs, kind/target, expected revision/head/base, payloadHash, authority refs, reservedCost, dispatch/outcome state, externalId/requestId | Unique task+phase+comparison+semantic operation key; durable before dispatch; unknown blocks conflict |
| Checkpoint / taskKey+sequence | accepted event cursor, dirty paths/patch evidence, head/base, actual processes, remote CI, subscriptions, pending ops/decisions/handover, budget | Preserve prior immutable snapshots; do not omit unknown process/outcome state |
| Evidence / evidenceId | producer identity/model/run, observedAt+zone, comparison/config, method, source URI/hash, actual count/exit/end/result, limits | Immutable; classify static/simulated/CI/visual/device/manual/reported; invalidate affected uses when comparison changes |
| AcceptanceMap / taskKey+mandateRevision+criterionId | criterion, required method/platform, cloud capability evidence, designated acceptor, remaining obligation and status | Carry admission to final delivery; required device/manual gate cannot be relabeled optional |
| Handover / handoverId | type, task/phase/comparison, supplier, exactly one recipient, deliverable, evidence/processes/limits/docs, observation and send times, acceptance receipt | Supplier responsible until explicit receipt on full same scope/comparison; send != acceptance |
| Notification / taskKey+decisionId+revision+recipient+purpose | queuedAt, attempts, externalId, sentAt, acceptedAt, latest status/source revision | Outbox part of same state store; dedup key prevents repeated decision notices; uncertain send must reconcile |
| HumanDecision / decisionId+revision | prompt/options/recommendation/consequences, original authority ref once answered, WaitingHuman state, safe waiting behavior | Silence/timeouts never approve; new revision for material change; revocation invalidates linked grants |
| BudgetReservation / reservationId | currency, approved cap ref, actual spend, estimated running exposure, request bound, billing uncertainty, global/per-issue links | Nonnegative amounts, no mixed-currency arithmetic; reserve atomically before paid start |
| CutoverRecord / migrationId+epoch | source/target revisions/hashes, owners, pending operations, source dispatch exclusion, target acceptance, rollback checkpoint | Exactly one allocatable scheduler; no activation on unknown exclusion |

## State transitions

Issue phases: Ineligible -> Eligible -> Allocated -> Accepted -> Working -> DraftValidated -> ReadyBySenior -> Reviewing -> Fixing -> ReviewingFresh -> MergePending -> IntegratedUnverified -> VerifiedIntegrated -> ClosureAccepted -> Archived.

Reviewing may go directly to MergePending only with independent passing review of the unchanged comparison. Fixing always requires new affected checks and fresh independent review. This is a permitted state graph, not permission to perform any phase.

BlockedTechnical, WaitingHuman, BudgetStopped and Recovering are explicit suspended states carrying resumePhase; they keep occupied capacity while execution/obligations remain unresolved. Senior may release capacity only after evidenced quiescence and accepted remaining obligations, never because a lease timed out or a Draft exists.

Operation: Prepared -> Dispatching -> SucceededObserved / FailedObserved / Unknown -> ReconciledSuccess / ReconciledNoEffect / BlockedUnknown. Only a proved no-effect result plus still-valid grant and operation-specific policy permits a new attempt. Semantic operation identity is stable across attempts; external request IDs are per attempt.

Delivery receipt: Queued -> Sent -> Accepted -> WriterEnded -> VerifiedComplete are separate facts, not one success flag. WriterEnded may precede recipient acceptance; VerifiedComplete requires both relevant evidence and an independent verifier. Older observations cannot regress a newer accepted phase.

## Atomicity boundaries

One transaction owns queue revision, allocation capacity, owner grant, operation intent, cost reservation, event and outbox entry. Store rollback changes none; uniqueness/serialization conflicts re-read. No distributed transaction is assumed across store, GitHub and providers.

Incoming provider/webhook ID plus source identity is unique in inbox. If unavailable, a derived semantic key is a documented local dedup aid, not proof of one provider effect. Mutation outcome reconciliation compares actual external identity/state, not just message text.

Authority records and operational state must be protected from worker write access. Event/evidence retention and encryption/location follow D2. Deletion/archival requires preserved evidence and accepted obligations, not an unattended TTL.
