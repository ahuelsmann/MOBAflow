# Lifecycle, authority, gate and handover contracts

Source: [#337](https://github.com/ahuelsmann/MOBAflow/issues/337). Normative future interface contract; nothing executed here.

## Admission request and response

Request: taskKey, repository/issue, mandateRevision, original human AuthorityRecord references, Senior allocationId/queueRevision, named owner/context, requested phase/actions, dependency evidence, branch/workspace, exact base/head, acceptanceMap, required providers/models/scanner environment and cost bound.

Admission atomically validates actor/source, scope/precedence/revocation, dependencies, maximum-five occupancy, no conflicting writer or uncertain external operation, qualified mutation allowlist, required acceptance method and budget reservation. It returns either an immutable grant with epoch/revision/expiry/limits, or a structured blocker with reason/evidence/owner/next step. No worker is started by a rejected or partially validated request.

A per-issue grant is narrower than the standing role mandate. Labels, issue content, model suggestions and historical approvals cannot broaden either. Device, app, track-power and movement actions are absent from the autonomous allowlist.

## Command/result envelope

Required fields: schemaVersion, messageId, correlationId, taskKey, issue identity, mandateRevision, allocationId, controllerEpoch, owner/leaseEpoch, type (information/advice/execution), phase, expected deliverable, comparison (repo/branch/head/base), observedAt/timeZone, sentAt separately, result state, evidenceRefs, limits, nextStep and exactly one nextRecipient.

Evidence/result includes actual provider session/run ID, process identity and end, actual model identity from execution records, required tool restrictions, output completeness, test discovered/executed/skipped counts, command/native/wrapper exit and semantic result. Missing fields fail acceptance for the affected gate. Parent model metadata and a successful CLI exit alone are insufficient.

A handover adds dirty/workspace status, active/pending process and remote-CI/subscription state, manual acceptance map, doc/wiki impact and retained obligations. Recipient receipt binds the same scope/phase/comparison and acknowledges takeover once. Supplier responsibility persists until receipt; no ACK chains. Accepted does not mean execution ended or integration verified.

## Scan-before-read boundary

Before EACH rule/skill/source/evidence read, execute standalone sonar analyze secrets with exact paths and require success for all requested files. Record command/status/version/time and bind clean evidence to the bytes about to be read. Changed bytes or interrupted/failed reads require a fresh scan before reread. Include script-internal inputs and reviewer packets; a prior scan of another host/path is not cloud evidence.

Unavailable/unauthenticated/interrupted scanner: BlockedTechnical, no read, no balanced fallback. Credential-manager access failures may use narrow authorized standalone scan escalation; never general interpreter permission, credential read, installation or security-setting change. A positive scan blocks handling that file: report without values, require rotation/removal and rescan. Scanner/service credentials are injected only through an approved isolated auth path.

## Publication and ownership

Workers produce locally isolated changes with no controller/provider-write secrets. A trusted publication process accepts only scanned payloads correlated to active grants. It must prove old-controller/identity exclusion, not rely solely on a database lease. Every external action follows [mutations](mutations.md).

Owner transfer freezes new intents, checkpoints, confirms actual process/write exclusion and external outcomes, then obtains recipient acceptance and atomically advances owner epoch. The same applies to controller replacement. Unknown old operations prevent conflicting takeover. Immutable archived workspace evidence is retained; no blind reset, clean, copied dirty state or resumed competing session.

## Evidence and gates

Gate tuple: gateId, requiredBy, sourceHead, sourceBase, configuration/image/tool versions, executionId, producer/model, actualStart/end, nativeExit, wrapperExit, discovered/executed/skipped, semanticResult, outputHash/ref and limitations. Allowed results: Pass, Fail, SkippedByPolicy, Blocked, Unknown, NotRun. A skipped or absent check is never executed evidence.

Required gate inventory for later autonomous delivery:

| Boundary | Existing sources / planned negative oracle |
| --- | --- |
| Build/SDK/platform | global.json, affected project/props and Quality workflow; actual build failure must fail wrapper |
| Analyzer/baseline | scripts/Test-AnalyzerBaseline.ps1 and relevant baseline files; missing/empty/invalid SARIF or failed producer => failure, no baseline mutation |
| Text/index | scripts/Test-LineEndings.ps1 including Staged; meaningful mixed/incorrect staged content must produce failure |
| Specification/instructions | Test-SpecKitGovernance.ps1, Test-InstructionConsistency.ps1 where applicable; missing required issue/sections => failure |
| Links/documentation | Test-DocumentationLinks.ps1 and documentation-consistency workflow; broken relevant links => failure |
| Tests/mutation/firmware/security | Existing affected Quality jobs; zero intended tests, failed relevant mutation/firmware/security gate => no pass |
| Sonar | Actual current PR commit check success and OPEN/CONFIRMED count zero; missing/NONE/auth error => unknown/blocked |
| Independent review | Fresh actual Opus 5.5+ identity, context sufficiency, exact comparison, findings/response, actual end; self/stale/lower/absent model => failure |
| Actual acceptance | Admission map, designated visual/device/manual acceptor and evidence; missing required acceptance stays pending |

Before enabling any gate, inventory its actual reads, command semantics and exit behavior on the chosen image and prove negative samples; listing the script here is not validation. The original reported incidents are not confirmed defects.

## Decisions, costs and notifications

WaitingHuman stores decisionId/revision, evidenced state, reason, recommendation/options/consequences, responsible human, restrictions, safe waiting behavior, current actual workers and known remaining exposure. Existing channels render accessible plain text; color/icons are optional only.

Notification key is task+decision+revision+recipient+purpose. Durable outbox distinguishes queued/sent/accepted. Retry only a safely reconciled no-effect send; delivery ambiguity stays recorded, not duplicated blindly. The same pending decision produces one logical notice; updates with material changed evidence are versioned.

Budget admission reserves a conservative maximum for requested operation plus already-running exposure. Unknown billing, exhausted subscription or ceiling blocks new paid starts/retries. Running work stops only through qualified authorized cancellation and checkpoint; cancellation sent is not terminal end. Preserve storage/CI/infrastructure liability while waiting. No auto-increase or silent paid fallback.

## Documentation and closure

Each transition package records architecture/developer/canonical wiki pages changed or a concrete NoChange reason, remaining drift owner/task and manual acceptance. Senior independently confirms merged commit and resulting main after the existing Claude merge; changed behavior fixes trigger reassessment. Archive only after real execution end, evidence retention and accepted closure/remaining obligations. Product integration, external publication and practical acceptance remain separate facts.
