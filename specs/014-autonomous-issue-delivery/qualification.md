# Later qualification and acceptance map

Source: [#337](https://github.com/ahuelsmann/MOBAflow/issues/337). Every row is PLANNED / NOT RUN in this planning PR. A task completion checkbox or documentation review cannot convert it into operational evidence.

## Admission method map

| Criterion / spec outcome | Method capable of proving it later | Remaining acceptance / responsible role |
| --- | --- | --- |
| Eligible work and one queue, SC-001/002 | Isolated authority/concurrency tests, protected store audit, actual dispatch/owner observations | Senior accepts source-of-authority and allocation evidence; no model self-grant |
| Full PC-off issue, SC-003/008 | Actual approved issue run with local PC/desktop unavailable; dependency matrix and cloud run/CI receipts | Human confirms PC-off condition/approved scope; Senior independently verifies integrated main |
| Recovery/no duplicates, SC-004 | Fault injection, provider readback, stale identity rejection and in-flight outcome reconciliation | Chosen provider must support observation; unknown result blocks qualification |
| Review/gates, SC-005/006 | Actual reviewer-model/context/exec records and exact head/base CI/Sonar; negative gate fixtures | Existing Claude route; required GitHub approvals cannot be manufactured |
| Waiting/cost, SC-007 | Restart/duplicate/revocation/budget tests and billing exposure reconciliation | Human decides D3/material scope; known remaining workers/cost disclosed |
| Acceptance/docs, SC-009 | Criterion ledger and actual integrated-main documentation comparison | Designated visual/device/manual acceptor; Documentation Manager plus delivery owner |
| Planning, SC-010 | Structural/prerequisite/traceability/link/secrets/line-ending/diff and actual PR checks | Current PR has no runtime/manual acceptance; Senior alone Ready |

The same criterionId/method/acceptor/status follows admission, Draft, review/fixes, integration and closure. A portable test cannot stand in for Windows/Android build, a fake cannot prove railway operation, and a compile cannot prove TalkBack or device behavior. Remote visual acceptance is permissible only when the actual criterion permits that method.

## Qualification scenarios and oracles

| ID | Later setup / fault | Required oracle and retained evidence | Owner / execution gate |
| --- | --- | --- | --- |
| Q01 | Missing original human authority, forged relay, revoked/superseded/contradictory record | Zero starts; blocker with original decision/allocation refs and precedence | Senior admission owner after authorized test allocation |
| Q02 | Six concurrent admissions plus duplicate issue/context request | <=5 occupied packages, one owner/context per issue, no duplicate paid start | Control-state tests; isolated store |
| Q03 | Worker lease expires while writable process or provider session persists | No replacement; neutralization evidence required; old actor publication rejected | Identity/operation qualification, D1/D4 |
| Q04 | Controller restart/partition and old controller in-flight mutation | One allocatable epoch; no credential reissue to obsolete actor; reconcile late request before transfer | Provider/issuer/network tests, no prompt-only proof |
| Q05 | Duplicate/reordered/delayed events, completed phase receives old result | One internal transition; history retained; no duplicate review or reopened completed phase | Inbox/state tests |
| Q06 | Lost response for each enabled mutation M01-M18 | Readback establishes same effect or no effect before retry; unresolved remains blocked; external duplicates never called exactly-once | Endpoint-specific authorized qualification |
| Q07 | Scanner missing/auth loss/interruption/positive finding, including script/reviewer input | No read of affected unscanned bytes; redacted blocker; fresh scan before reread | Every proposed environment, approved auth; synthetic secret handling isolated |
| Q08 | Issue-controlled code attempts to read controller secret or call publication outside grant | Access denied by actual boundary; no credential in workspace/env/logs; no accepted operation | Security boundary tests before any live worker |
| Q09 | Dirty workspace, unknown process, remote CI/subscription still active at restart | Full checkpoint reconciled; current owner retained; no destructive reset/restart or blind copy | Recovery fixture plus later provider observation |
| Q10 | Foreign branch commit or remote head moves between planning and write | Recorded old/new SHA/allocation mismatch blocks; external conditional write rejects stale head; incident retained, no force push | Publisher qualification with real safe preconditions |
| Q11 | Base advances or head changes after review/checks | Affected evidence invalidated, fresh checks/review required; exact-base merge boundary proved or operation excluded | Existing Claude integration owner + Senior |
| Q12 | Missing/lower reviewer model, self-review/fix, incomplete context, exit-zero empty output | No review pass; actual model/head/base/semantic result/native end required; no fallback/second review route | Existing Claude route |
| Q13 | Failed Windows/other build; missing/empty/invalid/truncated SARIF | Nonzero gate outcome, no baseline rewrite, before/after baseline hash identical in isolated fixture | Later gate qualification; original incident reported/unverified |
| Q14 | Real finding in staged line-ending fixture; failed native command behind successful wrapper; zero selected tests | Nonzero/semantic failure accepted as failure; executed count >0 for intended tests; never pass printed finding with zero exit | Later isolated gate qualification, no fix assumed |
| Q15 | Fresh image Windows and Android builds/analyzers | Actual SDK/global.json/workload/image/tool manifest, short path, successful applicable Windows net10.0-windows10.0.22621.0 and Android net10.0-android gates; portable net10.0 suite where applicable | Separately approved canary/resources; no canary now |
| Q16 | Provider outage/429/expired credentials/exhausted subscription with possible overage | Bounded safe recovery only; budget reservation and visible blocker; no fallback model/account or blind non-idempotent retry | Provider + cost qualification |
| Q17 | WaitingHuman survives restart, duplicate notice, silence/timeouts and later revocation | One logical decision notice, unchanged wait until authentic answer, revoke dependent grants, worker/cost state visible | Human-decision/notification tests |
| Q18 | Cost near/exceeds cap, delayed bills, running worker cancelled | No uncovered new start/retry; conservative running reserve; cancel request distinct from actual end; remaining costs stated | D3 approval, budget tests |
| Q19 | Callback unavailable or send receipt lost; recipient receives older handover | Queued/sent/accepted distinct; supplier retains responsibility; no substitute/ACK loop or historical-state overwrite | Existing-channel adapter qualification |
| Q20 | PC off with required local scanner/adviser/callback absent | No hidden local participation; block incompatible admission or explicitly scoped exclusion; one complete approved issue only | D5, full dependency map accepted by Senior |
| Q21 | Cutover or rollback interrupted at each step | Never two dispatch-capable schedulers; preserve owners/CI/decisions/budgets/evidence; unknown effects remain blocked | D6, accepted source/target owner receipts |
| Q22 | Required visual/device/manual criterion missing | Integrated/published/practically accepted stay separate; no self-waiver; only actual method evidence closes criterion | Designated human/device acceptor; no railway permission inferred |
| Q23 | Authorized external deployment with wrong/unknown live source/version | No publication success without exact deployed source/version and independent live evidence; MOBAnews/job preserved | Outside pilot unless separate publication assignment |
| Q24 | Archive requested after Draft/green CI but run/obligation remains | Reject archive; retain evidence/context until actual end, verified integration and accepted remaining obligations | Delivery owner -> PM -> Senior |
| Q25 | Documentation changes in behavior-fix round | Reassess developer/wiki map, correct or name owner/follow-up; Senior checks actual main | Documentation Manager + delivery owner |

## Existing CI versus candidate workers

At the planning base: SDK requested 10.0.402 with latestPatch roll-forward; Android workload set explicitly 10.0.402; Windows uses moving windows-latest. Existing ordinary Quality Windows/portable checks can provide their actual configured build/test/analyzer evidence. Android Release AAB currently needs workflow_dispatch plus validate_android and cannot be inferred from a normal PR.

Later fresh-image manifest must capture resolved SDK, VS/MSBuild/Windows SDK, JDK/Android SDK/build tools, workload set/manifests, CLI/scanner versions, action revisions, relevant project/package/lock/source hashes, actual image ID and worktree path. Do not change machine long-path settings; qualify a short path such as C:/w/i337 and record maximum observed generated path. No baseline refresh follows a failed producer. A dedicated Windows cloud worker needs an evidenced CI gap and D1/D3 approval.

## Measurement baseline

Before an authorized pilot, record comparable previously accepted issues only from existing queue/evidence: verified completion, allocation-to-integration time, rework rounds, handoff wait, human interventions and attributable cost. Missing historical measurements stay unknown; no new tracking queue or invented averages. Record pilot actuals and reasons for differences, including waits/provider failures; one pilot cannot establish statistically reliable speed/cost gains. Senior approves wider use only on its actual evidence and remaining limits.
