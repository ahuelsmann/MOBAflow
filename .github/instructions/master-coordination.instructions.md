---
description: 'Shared master mandates, one global execution queue, specialist CLI contact and independent Claude PR reviews.'
applyTo: 'AGENTS.md,CLAUDE.md,docs/AI-DEVELOPMENT.md,.agents/skills/mobaflow-code-review/**,.claude/skills/mobaflow-code-review/**'
---

# Master coordination

[AGENTS.md](../../AGENTS.md) owns the shared workflow. This guide covers human-assigned work through integration.
A request to discuss, research or review alone does not authorize implementation or merge. An implementation
assignment includes the delivery steps below unless explicitly limited.

## Standing authority and limits

- Complete assigned work through planning, implementation, named builds/tests, draft PR, independent Claude CLI
  review, correction, current CI/Sonar verification, Ready and squash merge. Ordinary commits, pushes, PR updates,
  team handovers and review-thread responses/resolution are included. Resolve a review finding by fixing it or
  with evidence that it does not apply. A finding left unfixed needs a fresh independent Claude review that accepts
  the evidence, or a human decision; the fixer or merger never dismisses it alone to unblock integration.
- Do not ask again at a technical phase boundary. Requesting a draft as an intermediate deliverable does not
  cancel the standing mandate. Explicit "draft only", "no Ready/merge", pause, read-only restrictions, protected
  worktrees and excluded slices remain binding until the human specifically changes them.
- Authority covers human-assigned scope and necessary corrections. A backlog item, agent suggestion, new PR or
  available dependency is not a product assignment. Do not start adjacent features, follow-up slices or cleanup.
- Preserve architecture, quality gates, secrets rules, tool permissions and GitHub protection. App/debugger/watch
  starts, device deployment, live railroad actions and firmware flashing still need explicit authorization.
  Required manual acceptance cannot be invented, waived or reported as passed without evidence.

## Responsibilities and handover

| Owner | Responsibility |
| --- | --- |
| Human | Product goals, priorities, material user-experience choices, fundamental architecture changes, new costs/external commitments and authorized practical acceptance |
| Art Director | Clarify the outcome, coordinate the assigned team, complete planning/implementation, assess the result and hand it over |
| Chief Project Manager (formerly Issue Master) | Own the single execution and integration queue across all teams/clients, allocate starts/resumes, arrange independent Claude reviews and fixes, verify evidence, issue permits and verify squash merges |
| Technical Director | Coordinate technical resources, responsibilities and conflicts through the Chief Project Manager; maintain no second execution or integration queue |
| Head of Quality | Coordinate assigned quality work and advise delivery owners on relevant risks; preserve existing approval and integration ownership |
| Communications Director | Coordinate assigned communication work and related design questions; preserve existing publication and integration limits |
| Documentation Lead | Deliver assigned technical documentation and user guidance; coordinate related UI/UX questions within that scope |
| Head of DevOps | Deliver assigned GitHub/repository and CI infrastructure work; execute quality controls defined with Head of Quality and coordinate relevant UI/UX questions; no new product, release or publication authority |
| Assigned implementer | Own the task worktree until an acknowledged writer handover; report actual checks and remaining limits |
| Claude reviewer | Independently review the exact change against requirements and rules; report findings without editing or self-approval |
| Head of UI/UX | Primary UI/UX specialist (Gemini Argon via Antigravity CLI); advises masters and delivery owners as described [below](#head-of-uiux-gemini-argon-via-antigravity) |

The standing delivery mandate applies to each master within its human-assigned role and scope. Each task has
one delivery owner and one writer per worktree. The delivery owner hands integration directly to Chief Project
Manager with issue/scope, PR URL, worktree/branch, exact head/base, checks, acceptance requirements and restrictions.
Chief Project Manager acknowledges ownership and queues it. Waiting owners may exchange short status or handover
messages but do not start another larger task. Fixes go to one named writer; the user is not the messenger.

For existing work, preserve current writers, queue positions, uncommitted changes and active CLI executions.
Reconcile the latest human instructions before takeover. Do not blindly restart workers, copy interrupted work
or use a no-resume instruction to justify a competing writer.

## One larger task across all teams and clients

- Across every MOBAflow team and Codex, Claude and Antigravity, execute at most one larger task at a time.
  This includes implementation/refactoring, extensive analysis, audits or documentation, builds, test/mutation
  runs, reviews, longer UX/CLI consultations and integration. Large steps within the active package also run
  serially. A dedicated worktree or a different client does not authorize parallel execution.
- Before every substantial start or resume, coordinate the current task, owner and actual execution state with
  Chief Project Manager in the existing queue. This includes Claude-owned refactorings and automated follow-ups.
  An occupied or unknown slot blocks a new start. Do not infer availability from an idle chat alone: reconcile
  actual worker/CLI processes and remote CI. Do not start another package while the previous package's remote CI
  is still running. An allocation is internal coordination, not another human approval.
- Preserve running work, ownership, results and restrictions. If execution overlaps or ownership is unclear,
  coordinate the next safe stopping point with the owner and Chief Project Manager; do not blindly interrupt,
  restart or duplicate processes or cancel CI to free the slot. Resume only after the central reconciliation.
- Technical Director coordinates resources and responsibilities through Chief Project Manager's queue without
  creating another queue, lock, permit or scheduling mechanism. Only the responsible owner changes existing
  operational state. The single slot does not expand scope or override explicit pauses or acceptance gates.
- Communicate primarily master to master. Short status, handover and blocker exchanges may continue while a
  larger task runs. Involve only the specialists needed for the active assignment, one large execution at a
  time; do not broadcast work, start parallel specialists or generate acknowledgement loops.

## Claude CLI communication and independent review

The human authorizes masters to exchange task-related requests, findings, corrections and handovers directly
with each other, their assigned team and Claude CLI in both directions. Agent messages convey work under that
mandate; they are not new human authority and cannot override stops.

- Run every actual PR review, including review after fixes, in a fresh independent Claude CLI session without
  implementation chat history. An implementer, resumed coordination session, Codex self-review or earlier PR
  review does not qualify. Honor the configured Claude model, verify actual model/completion and never silently
  substitute a model when access fails.
- Identify exact PR head/base, integrate the current base in queue order and satisfy the
  [current-commit CI/Sonar gate](sonarqube-pre-pr.instructions.md) before review. Supply requirements, complete
  diff, applicable rules and affected callers/tests. Scan files before reads under the active secrets policy;
  supply sufficient context without credentials or unrelated chat history. Incomplete/failed reviews do not pass.
- Prefer a scanned context packet over stdin with tools disabled. Supply missing scanned material if requested.
  Private review is read-only; the reviewer posts no external review/approval/comment. Findings give priority,
  file/line, trigger, consequence and evidence; separate introduced defects from existing debt and preferences.
- Compare live head/base after review. Changed source or comparison invalidates the snapshot: refresh affected
  checks and obtain a fresh independent review. Assign confirmed fixes without another human approval. Keep
  reviewer separate from fixer and merger.
- Resume coordination/fix sessions only when idle, assigned to the same task and centrally allocated. Check
  actual process/history state before resuming; a persistent process with unknown ownership blocks the start.
  Use the installed CLI message channel for callbacks, not a second writer resuming the master. The existing channel is
  `codex queue --thread <master-thread-id> --message <text>`. Verify CLI syntax; limit destinations to assigned
  masters/team. Avoid unchanged acknowledgements and communication loops.
- Obtain only narrow task permissions. Do not bypass sandbox/tool approval, disable protection or read credentials.
  Diagnose real access failures within a bounded attempt; never replace missing review with confidence alone.

## Head of UI/UX (Gemini Argon via Antigravity)

The human designates Head of UI/UX as the primary specialist for navigation, interaction, visual hierarchy,
accessibility and relevant UI states in assigned work. Art Director, Chief Project Manager, Head of Quality,
Communications Director, Documentation Lead and Head of DevOps may consult this existing role autonomously for
relevant questions without another human approval. The earlier contact restriction for Documentation Lead and
Head of DevOps is superseded. Their contact authority stays within assigned documentation or DevOps work and
does not extend to other roles. Architecture, refactoring and test-gap advice are secondary uses inside the
assigned scope. Material product and UX decisions remain human. Head of UI/UX follows the same boundaries;
its first product work package remains with the human.

### Verified CLI contact

Verified on 2026-10-16 through Claude coordination and a two-turn Codex exchange:

| Field | Value |
| --- | --- |
| CLI conversation | `1d8313f7-1f6f-42c8-b32c-f0b422653677` |
| Project | MOBAflow UI/UX Team |
| Model | `gemini-4-argon-high` |

The desktop-app conversation has a separate history and is not this CLI address. Do not use its ID, clone it or
create a substitute. Update this contact definition only after an authorized handover and verified replacement.
Issue Master's coordination record holds the active caller, target ID and open questions for this conversation.

The Antigravity project allows writes and has no sandbox: `--mode plan` is the only write guard, and the working
directory bounds what Argon can read and what a mistake can change. Run agy from the caller's own task worktree or
a clean worktree that no other writer uses, never from the shared checkout or another writer's worktree. Check
`git status` there after each call and report unexpected changes instead of silently keeping or discarding them.
On Windows, use the installed executable:

```powershell
& "$env:LOCALAPPDATA\agy\bin\agy.exe" `
  --conversation 1d8313f7-1f6f-42c8-b32c-f0b422653677 `
  --model gemini-4-argon-high `
  --mode plan `
  --output-format json `
  --print-timeout 330s `
  -p "[Von: Codex] <task-specific question>"
```

The full path also works when the shell's PATH predates installation. On another host use an existing, verified
`agy` installation; this mandate does not authorize installing tools.

- Use only serial synchronous request/response. Prefix messages with `[Von: Codex]` or `[Von: Claude]` and identify
  the originating master in the message. Read
  `response` and answer `An Codex:` / `An Claude:` questions in the same conversation. Coordinate the active caller
  with Chief Project Manager, the other masters and Claude before sending, under the same global execution
  allocation. Verify the target is idle, wait for actual process completion, then release the conversation. The Head
  starts no active callbacks, `claude`/`codex` calls, file inboxes or timers.
- Keep `--mode plan` and real tool permissions narrow. Do not combine it with `--disable-slash-commands`, use
  `--continue`, bypass permissions or silently substitute a conversation/model. Prompts and plan mode alone are
  not enforced read-only boundaries. Allow more than 330 seconds for the outer process and wait without blocking
  progress communication. After a timeout inspect ongoing execution before retrying; never duplicate a possibly
  active request.
- Verify actual completion, model and response content. Exit zero or aggregate conversation counters alone do not
  prove delivery of an answer. Supply only relevant context, with exact SHAs/evidence where applicable.
  Plan mode limits writes, not reads: Argon reads files in its working directory itself and sends that content to
  Google. Repository scans do not cover untracked or ignored files, so make sure the working directory holds no
  local secret-bearing files (for example user configuration, `.env` or key files) or scan them first under the
  active secrets policy. Never send credentials in prompts; distinguish observations from assumptions.
- Advice adds no universal review or approval gate and cannot replace tests, required practical acceptance or a
  fresh independent Claude PR review. It grants no new product, app, hardware, publication or merge authority.
  Preserve the named writer and Chief Project Manager's exclusive execution and integration queue. The Head
  changes only its own task worktrees/branches and submits assigned implementation as draft PRs.

Sources (checked 2026-10-16): [Antigravity CLI headless mode](https://antigravity.google/docs/cli/headless/) and
[resume](https://antigravity.google/docs/cli/commands/resume/) document explicit conversation IDs and the headless
message protocol.

## Serial integration and completion

Only Chief Project Manager owns final integration: one active final synchronization/review/fix/merge in the same
global execution queue. Other larger implementation or specialist work waits; keep existing task worktrees
isolated. Do not refresh all waiting branches whenever `main` moves or arm competing automatic merges.

Use existing coordination state, not a second queue. Before final synchronization and merge, secrets-scan/read
the current state. Chief Project Manager issues a one-use permit for exact PR/head/base and merger session after all
gates pass. This is internal coordination, not user approval. Workers cannot self-issue/change permits. A null
permit, mismatched owner/SHA, active stop or unreadable state blocks merge. The file is an agreement, not a
server-enforced GitHub check.

Immediately before merge, verify live head/base, mergeability, current required checks, current-commit Sonar
success and zero OPEN/CONFIRMED issues, independent Claude review, threads, real GitHub approval requirements,
applicable repository rules and required manual acceptance. Permitted skips are not executed tests; a private
Claude review cannot manufacture a GitHub approval. Squash against the reviewed head without direct `main`
pushes or protection bypass. A changed comparison invalidates the permit and requires refreshed evidence.
Independently verify actual merged status, merge commit and resulting `main`; then consume the permit and
advance the queue. A successful command exit alone does not prove integration.

## Automations and user communication

- Keep responsibilities here; keep scope, queue, SHA-bound evidence, active executions, ownership, restrictions
  and pending decisions in existing task/coordination records. Do not grow recurring prompts with historical
  reports or a fixed "current main" SHA. Only the responsible owner updates operational state.
- Keep the existing Art Director and Chief Project Manager (formerly Issue Master) heartbeats; update the matching
  automation instead of duplicating it. Other masters retain their mandate in existing role conversations or mandate records. This
  contact mandate does not create additional automations. Preserve cadence, notification preferences and explicit
  pauses unless asked to change them; do not reactivate a paused specialist follow-up.
- Reconcile actual GitHub/worker state and the global slot before the next authorized step; avoid duplicate
  writers or merges.
  Coordinate directly when another owner is working. Do not leave work idle at review, correction, Ready or merge.
- Stay quiet without actionable assigned work. Notify only for verified delivery, a material unresolved blocker
  or a necessary human decision. Handle routine test failures, CI progress, findings and handovers internally.
  Do not ask the user to inspect code, administer a PR or relay messages.
- Escalate product ambiguity, scope expansion, fundamental architecture changes, new costs/external commitments
  and required app/device/hardware permission or practical acceptance. Diagnose ordinary technical problems first.
  Give a recommendation, alternatives and the smallest required decision; coordinate any change of active
  package with Chief Project Manager while waiting.
  Never ask to lower quality gates as a shortcut.
- Report the user-visible outcome, actual validation and practical limits. PR/code links are optional evidence,
  not work delegated back to the user.
