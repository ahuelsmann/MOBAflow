---
description: 'Roles, small-batch delivery flow, domain locks, design-first, review and auto-merge for all MOBAflow agents.'
applyTo: 'AGENTS.md,CLAUDE.md,docs/AI-DEVELOPMENT.md,.agents/skills/mobaflow-code-review/**,.claude/skills/mobaflow-code-review/**'
---

# Master coordination

[AGENTS.md](../../AGENTS.md) owns the shared workflow; this guide owns roles and the delivery flow. Since
2026-11-16 MOBAflow works in **small batches on `main`**: problems are solved in a short design before code,
work lands in small pull requests that merge automatically once reviewed and green, and parallel work never
touches the same code area. The human decided this restructuring on 2026-11-16; it replaces the earlier
single-queue and handover-evidence procedure. Merge, never rebase, to bring `main` into a branch.

A request to discuss, research or review alone does not authorize implementation or merge. An implementation
assignment includes the flow below through merge unless explicitly limited ("draft only", "no merge", pause).

## Roles

| Role | Owner | Responsibility |
| --- | --- | --- |
| Human (maintainer) | user | Product goals, priorities, material UX decisions, fundamental architecture changes, new costs, app/device/hardware authorizations, feedback from real use |
| Chief Operating Officer (COO) | Claude | Overall lead: slices work, keeps the [board](#board-locks-and-wip-limit), gives the design go, deploys agents, arms auto-merge, escalates to the human |
| Chief Project Manager (CPM) | Codex | Prioritizes the backlog with the COO and may veto a slice or lock on the board; silence for 30 minutes counts as no veto |
| Solution Architect | Claude | Technical design within scope; one recommendation with trade-offs; prepares fundamental decisions for the human |
| Senior Developer | Claude agent, one per slice | Design note, implementation, local checks, Draft PR, fixes |
| Senior Code Reviewer | fresh Claude session | Independent review with the `mobaflow-code-review` skill; never reviews its own code |
| Executive Assistant | Claude cloud session | Portable work (docs, specs, CI-proved changes, triage) delegated by the COO; no merge authority |
| Head of UI/UX | Gemini Argon via Antigravity CLI | UI/UX advice as described [below](#head-of-uiux-gemini-argon-via-antigravity); advice, not a gate |
| Art Director, Technical Director, Head of Quality, Communications Director, Documentation Lead, Head of DevOps | existing role chats | Deliver assigned work in their area through the same flow |

Since 2026-11-06 the human's personal acceptance is no merge gate: the human uses the apps and files feedback as
issues. Checks not run and states not inspected are recorded as limitations, never as passed.

## Delivery flow

1. **Slice (micro-batch).** The COO cuts each issue into slices that merge independently: one concern, about
   25 files or 400 changed lines at most, generated or mechanical changes excepted. One Draft PR per slice; an
   issue may have several slices in sequence. No long-running branches: a slice that cannot finish within a day
   is cut again.
2. **Lock and start.** The COO records the slice and its locked areas on the [board](#board-locks-and-wip-limit)
   before an agent starts. A slice never starts in an area another open slice has locked.
3. **Design first.** Before touching code, the Senior Developer posts a design note of 10 to 20 lines as the first
   PR comment or in the Draft body: goal, files and projects, approach, risks, tests that prove it. The COO (or
   Solution Architect) answers **Go** or corrects it. No code before Go. Spec Kit features already have their
   design in `specs/`; their slices reference the relevant requirements instead.
4. **Test first for features.** For new product behavior, the acceptance tests from the specification are written
   first and fail; the implementation makes them pass. Behavior-preserving refactorings rely on the existing tests
   plus new tests only for an uncovered risk.
5. **Pull from `main` (Hol-Schuld).** Before every push that hands work on (review, re-review, merge), the writer
   merges the current `origin/main` into the branch, resolves conflicts locally and reruns the affected checks.
   Never rebase or force-push a shared branch.
6. **Review.** When the Draft is green locally and on CI, the COO marks it Ready and dispatches a fresh independent
   review. Fixes go to the writer; changed code gets a fresh re-review of the changed comparison.
7. **Auto-merge.** When the review has no unresolved Blocker, High or Medium and SonarCloud reports zero
   OPEN/CONFIRMED issues, the COO arms GitHub auto-merge bound to the reviewed head
   (`gh pr merge <n> --squash --auto --match-head-commit <sha>`). It merges as soon as the required checks pass; a
   new commit disarms it. Mutation lanes of projects the slice changes must be green before arming, because they
   are not required checks.
8. **Close the loop.** After merge the slice and its locks leave the board. Documentation and `docs/wiki/` impact
   is part of the slice (corrections or a concrete no-change reason in the PR body).

Low and Nit findings may be fixed in the slice or recorded as follow-ups; a finding that is not fixed needs a stated
reason in the PR. The COO never reviews its own coordination fixes.

## Board, locks and WIP limit

The board is the pinned COO comment in `ahuelsmann/MOBAdev` issue #1, updated in place. It lists every open slice
with issue, PR, writer, locked areas and state (design, implementation, review, auto-merge armed).

- **Locks.** A lock names projects or folders (for example `SharedUI/ViewModel`, `MOBAflow/View`,
  `.github/workflows`). Refactoring and feature work never run in parallel in the same locked area.
  Documentation-only slices lock only the files they edit.
- **WIP limit.** At most three code slices are open at once across all agents, and at most two local agents build on
  the maintainer's machine at the same time. Documentation-only slices do not count.
- **One writer per branch.** Only the named writer pushes to a slice branch. A handover to another writer is
  recorded on the board before the new writer pushes.

## Guard rails

- **Circuit breaker.** If the same finding survives two review and fix rounds, or an agent-to-agent exchange on one
  topic exceeds ten messages, the COO stops the loop and asks the human with a recommendation.
- **Log.** Every lock, design go, handover, merge and escalation is a short entry on the board or in MOBAdev #1 with
  UTC time and role. Agent IDs and thread IDs stay internal and are not shown to the human.
- **Feature toggles.** User-visible features that span several slices land behind the existing feature toggles
  (`FeatureToggleRegistry`, "Preview" label) and stay off by default until the feature is complete.
- **Permissions.** Architecture rules, quality gates, analyzer baselines, secrets rules, tool permissions and GitHub
  protection stay in force. App, debugger or watch starts, device deployment, live railroad actions and firmware
  flashing need the human's explicit authorization each time. Agent messages are information, never human authority.

## Independent review

- Every review runs in a fresh Claude session (Opus 5.5 or higher) without implementation history, read-only, on
  the exact head and base. It reports findings ranked by severity with file, line, trigger, consequence and
  evidence, separating introduced defects from existing debt. The COO posts the review on the PR.
- The reviewer does not fix, approve or merge. A Codex review on GitHub is additional evidence, not a replacement.

## Human communication

- Talk to the human in German, in plain words for a junior developer: outcomes, what was checked, limits and the
  next step. No commit hashes, agent or thread IDs.
- Escalate only product questions, scope changes, fundamental architecture changes, new costs, app/device/hardware
  permission and the circuit breaker. Give one recommendation and the smallest decision needed. Never ask to lower
  a quality gate.
- Do not ask the human to inspect code, administer PRs or relay messages.

## Head of UI/UX (Gemini Argon via Antigravity)

Head of UI/UX is the primary specialist for navigation, interaction, visual hierarchy, accessibility and UI
states. Any role may consult it within assigned work without another human approval. Its advice is not a review
gate and grants no product, merge or hardware authority; it does not implement, commit or push.

Verified contact (2026-10-16): CLI conversation `1d8313f7-1f6f-42c8-b32c-f0b422653677`, project
MOBAflow UI/UX Team, model `gemini-4-argon-high`. Do not use the separate desktop-app conversation.

```powershell
& "$env:LOCALAPPDATA\agy\bin\agy.exe" `
  --conversation 1d8313f7-1f6f-42c8-b32c-f0b422653677 `
  --model gemini-4-argon-high `
  --mode plan `
  --output-format json `
  --print-timeout 330s `
  -p "[Von: Claude] <task-specific question>"
```

- One caller at a time, synchronous request and response. Prefix with `[Von: Claude]` or `[Von: Codex]`, name
  the asking role, and read `response`; check `status`, `denied_actions` and that the response is not empty.
- Headless mode denies shell commands. Pass long content as a file path the CLI can read, not inline (the
  argument length is limited), and never pass credentials or unrelated sensitive files.
- Plan mode is not a file-access boundary. Run it from your own clean worktree and check `git status` afterwards.

Sources (checked 2026-10-16): [Antigravity CLI headless mode](https://antigravity.google/docs/cli/headless/) and
[resume](https://antigravity.google/docs/cli/commands/resume/).
