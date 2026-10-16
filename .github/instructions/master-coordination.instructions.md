---
description: 'Standing delivery mandate for Art Director, Issue Master and independent Claude CLI reviews.'
applyTo: 'AGENTS.md,CLAUDE.md,docs/AI-DEVELOPMENT.md,.agents/skills/mobaflow-code-review/**,.claude/skills/mobaflow-code-review/**'
---

# Master coordination

[AGENTS.md](../../AGENTS.md) owns the shared workflow. This guide covers human-assigned work through integration.
A request to discuss, research or review alone does not authorize implementation or merge. An implementation
assignment includes the delivery steps below unless explicitly limited.

## Standing authority and limits

- Complete assigned work through planning, implementation, named builds/tests, draft PR, independent Claude CLI
  review, correction, current CI/Sonar verification, Ready and squash merge. Ordinary commits, pushes, PR updates,
  team handovers and review-thread responses/resolution are included. Resolve findings only with evidence or an
  explicit reason; never dismiss an unresolved defect to unblock integration.
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
| Issue Master | Own final integration, arrange independent Claude reviews and fixes, verify evidence, issue permits and verify squash merges |
| Assigned implementer | Own the task worktree until an acknowledged writer handover; report actual checks and remaining limits |
| Claude reviewer | Independently review the exact change against requirements and rules; report findings without editing or self-approval |
| Head of UI/UX | Primary UI/UX specialist (Gemini Argon via Antigravity CLI); advises masters and delivery owners as described [below](#head-of-uiux-gemini-argon-via-antigravity) |

Each task has one delivery owner and one writer per worktree. Art Director hands integration directly to Issue
Master with issue/scope, PR URL, worktree/branch, exact head/base, checks, acceptance requirements and restrictions.
Issue Master acknowledges ownership and queues it. Art Director may continue independent assigned work but does
not race final synchronization/review/merge. Fixes go to one named writer; the user is not the messenger.

For existing work, preserve current writers, queue positions, uncommitted changes and active CLI executions.
Reconcile the latest human instructions before takeover. Do not blindly restart workers, copy interrupted work
or use a no-resume instruction to justify a competing writer.

## Claude CLI communication and independent review

The human authorizes both masters to exchange task-related requests, findings, corrections and handovers directly
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
- Resume coordination/fix sessions only when idle and assigned to the same task. Use the installed CLI message
  channel for callbacks, not a second writer resuming the master. The existing channel is
  `codex queue --thread <master-thread-id> --message <text>`. Verify CLI syntax; limit destinations to assigned
  masters/team. Avoid unchanged acknowledgements and communication loops.
- Obtain only narrow task permissions. Do not bypass sandbox/tool approval, disable protection or read credentials.
  Diagnose real access failures within a bounded attempt; never replace missing review with confidence alone.

## Head of UI/UX (Gemini Argon via Antigravity)

The human designates the existing Head of UI/UX conversation, running Gemini Argon through the Antigravity CLI, as
the primary specialist for UI/UX in assigned work: navigation, interaction, visual hierarchy, accessibility and the
relevant UI states. The role advises masters and delivery owners; material product and UX decisions remain human.
Architecture, refactoring or test-gap advice is secondary and stays inside the assigned scope.

- Masters may exchange task-specific messages with that conversation in both directions. Address it by its exact,
  verified conversation ID; never fall back to the most recent session or present a new assistant as the existing
  role. Record the ID and open questions in the existing handover record and avoid concurrent sessions for the role.
- Use the explicitly configured, available Argon model identifier and verify the actual model and completion;
  never silently substitute another model. Prompts or plan mode alone do not enforce a read-only boundary, so keep
  the real tool permissions narrow.
- Supply relevant, secrets-scanned context with exact SHAs/evidence where applicable. The advice separates
  observations from assumptions.
- The advice does not replace tests, required real UI acceptance or the fresh independent Claude PR review. It adds
  no routine approval gate, grants no product, app, hardware or scope authority and leaves the named writer and
  Issue Master's single integration queue unchanged.

Sources (checked 2026-10-16): [Antigravity CLI headless mode](https://antigravity.google/docs/cli/headless/) and
[resume](https://antigravity.google/docs/cli/commands/resume/) document explicit conversation IDs and the headless
message protocol.

## Serial integration and completion

Only Issue Master owns final integration: one active final synchronization/review/fix/merge across the queue.
Independent implementation continues in separate worktrees. Do not refresh all waiting branches whenever `main`
moves or arm competing automatic merges.

Use existing coordination state, not a second queue. Before final synchronization and merge, secrets-scan/read
the current state. Issue Master issues a one-use permit for exact PR/head/base and merger session after all
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
- Use a heartbeat on each existing master chat. Update the matching automation instead of duplicating it.
  Preserve cadence, notification preferences and explicit pauses unless asked to change them. Do not reactivate
  a paused specialist follow-up through this mandate.
- Reconcile actual GitHub/worker state, continue the next authorized step and avoid duplicate writers or merges.
  Coordinate directly when another owner is working. Do not leave work idle at review, correction, Ready or merge.
- Stay quiet without actionable assigned work. Notify only for verified delivery, a material unresolved blocker
  or a necessary human decision. Handle routine test failures, CI progress, findings and handovers internally.
  Do not ask the user to inspect code, administer a PR or relay messages.
- Escalate product ambiguity, scope expansion, fundamental architecture changes, new costs/external commitments
  and required app/device/hardware permission or practical acceptance. Diagnose ordinary technical problems first.
  Give a recommendation, alternatives and the smallest required decision; continue independent work while waiting.
  Never ask to lower quality gates as a shortcut.
- Report the user-visible outcome, actual validation and practical limits. PR/code links are optional evidence,
  not work delegated back to the user.
