---
description: 'Multi-model agent network, capability profiles, risk classes, small-batch delivery flow, domain locks, design-first, cross-model review and auto-merge for all MOBAflow agents.'
applyTo: 'AGENTS.md,CLAUDE.md,docs/AI-DEVELOPMENT.md,.agents/skills/mobaflow-code-review/**,.claude/skills/mobaflow-code-review/**'
---

# Master coordination

[AGENTS.md](../../AGENTS.md) owns the shared workflow; this guide owns the multi-model agent network, roles and
the delivery flow. Since 2026-11-16 MOBAflow works in **small batches on `main`**: problems are solved in a short
design before code, work lands in small pull requests that merge automatically once reviewed and green, and
parallel work never touches the same code area. On 2026-11-17 the human expanded this into a **multi-model agent
network**: a role defines a domain responsibility, not a single fixed model or tool. Merge, never rebase, to bring
`main` into a branch. Use the configured GitHub remote (see [AGENTS.md](../../AGENTS.md)); `<remote>/main` below
means its `main`.

A request to discuss, research or review alone does not authorize implementation or merge. An implementation
assignment includes the flow below through merge unless explicitly limited ("draft only", "no merge", pause).

## Capability profiles and roles

Roles define responsibilities and survive model updates. Every role and slice team draws on four capability
profiles across **Claude Code**, **ChatGPT Codex** and **Antigravity** under the maxim **quality first** (concrete
model mappings live in [AI-assisted development](../../docs/AI-DEVELOPMENT.md#capability-profiles-and-models)):

| Profile | Core strength | Typical duties |
| --- | --- | --- |
| `P1` Depth, Architecture & Safety | Concurrency (`async/await`, EventBus), Z21 protocol, safety invariants, adversarial review | Architecture design, high-risk code, independent final review, safety gate |
| `P2` Repo, Build & Fact Precision | Build graph (`Moba.slnx`), Spec Kit consistency, CI/Sonar/mutation verification, fact checking | Backlog/spec alignment, companion review, cross-model review, CI/delivery |
| `P3` System Context, Product & UI/UX | Large multi-file context, WinUI/MAUI interaction, Light/Dark themes, clear user copy and visuals | Product/UX design review, UI/TrackPlan guidance, MOBAnews and wiki clarity |
| `P4` Cadence, Triage & Fast Delivery | Low-latency status checks, routine slices, test/doc additions, board and timer monitoring | Triage, portable/doc slices, pre-checks, rapid iteration |

| Role | Primary profile(s) | Responsibility |
| --- | --- | --- |
| Human (maintainer) | user | Product goals, priorities, material UX decisions, fundamental architecture changes, new costs, app/device/hardware authorizations, feedback from real use |
| Decision & Steering Triad | `P1` (COO) + `P2` (CPM/TD) + `P3` (Product/UX) | Cross-model steering for slice cutting, risk classification, design Go and escalations; the COO is the sole technical writer on the [board](#board-locks-and-wip-limit) and arms auto-merge |
| Chief Operating Officer (COO) | `P1` (Claude) | Overall operational lead: maintains the [board](#board-locks-and-wip-limit), forms slice teams, records the design Go, dispatches reviews, arms auto-merge, escalates to the human |
| Chief Project Manager (CPM) & Technical Director (TD) | `P2` (Codex) | Prioritizes the backlog with the COO, checks repo/Spec Kit/build-graph consistency and may veto a slice or lock on the board as described [below](#board-locks-and-wip-limit) |
| Head of Product Experience & UI/UX | `P3` (Antigravity) | Co-designs UI, TrackPlan, interaction and editorial slices from the design note on; checks visual hierarchy, themes, accessibility and user-facing clarity ([CLI contract](#head-of-uiux-gemini-argon-via-antigravity)) |
| Solution Architect | `P1` + `P2` / `P3` | Technical design within scope; one recommendation with trade-offs; prepares fundamental decisions for the human |
| Senior Developer (Slice Writer) | `P1`, `P2` or `P4` (one named writer per slice) | Design note, test-first implementation, local checks, Draft PR, review fixes |
| Companion Reviewer | `P1`, `P2` or `P3` (different model family from writer) | Reviews the design note and every pushed commit read-only on the PR while the slice grows |
| Independent Final Reviewer | `P1` (fresh session) + cross-model `P2`/`P3` check | Read-only final review at Ready on the exact head and base without implementation history |
| Executive Assistant | `P4` / `P1` cloud session | Portable work (docs, specs, CI-proved changes, triage) delegated by the COO; no merge authority; work needing the local machine, a device, the WinUI app or a secret stays local |
| Domain Leads (Quality, Security, Communications, Documentation, DevOps, Art) | `P1`–`P4` role cells | Deliver assigned work in their domain through the same cross-model flow |

Since 2026-11-06 the human's personal acceptance is no merge gate: the human uses the apps and files feedback as
issues. Checks not run and states not inspected are recorded as limitations, never as passed.

## Risk classes and domain matrix

Every slice is assigned a risk class (`H`, `M` or `L`) when recorded on the board. Review-fix rounds always keep the
risk class of their slice. Dependency updates take the risk class of the area they affect (`H` for runtime,
protocol or transport packages, `M` for UI/feature packages, `L` for test/analyzer/build-only tooling):

| Class | MOBAflow areas | Required team and review diversity |
| --- | --- | --- |
| `H` High (Safety, Runtime & Protocol) | `Backend` (Z21/workflows), `Common` (EventBus/runtime), `MOBApi`, `MOBAdisplay` (`esp32/`), project-switch/stop logic, runtime/protocol dependencies | Writer (`P1`/`P2`), companion reviewer from a different model family (`P2` if writer is Claude; `P1` if writer is Codex), final independent `P1` review plus cross-family `P2` verification; a safety veto is binding |
| `M` Medium (Features, UI & Audio) | `MOBAflow` (WinUI), `MOBAsmart` (MAUI), `SharedUI`, `TrackPlan.*`, `Speech.*`, `Sound`, `MOBAnews`, UI/feature dependencies | Writer (`P1`/`P2`/`P4`), companion reviewer from a different model family (`P2`/`P1`), `P3` (UI/UX & Product) at the design note for UI, TrackPlan and editorial `M` slices, final independent `P1` review plus cross-family `P2`/`P3` verification |
| `L` Low (Docs, Wiki & Small Non-Behavioral Fixes) | `docs/`, `docs/wiki/`, `specs/`, test/tooling-only dependency updates, <=3-file fixes without behavior change | One named writer (`P4`/`P2`) states intent in the PR body (no separate design note or per-commit companion needed); final review at Ready runs a fresh independent `P1` review plus independent cross-family `P2`/`P3` verification |

## Delivery flow

1. **Slice (micro-batch).** The COO cuts each issue into slices that merge independently: one concern, about
   25 files or 400 changed lines at most, generated or mechanical changes excepted. One Draft PR per slice; an
   issue may have several slices in sequence. No long-running branches: a slice that cannot finish within a day
   is cut again.
2. **Lock and start.** The COO records the slice, its risk class (`H`, `M` or `L`), its slice team and its locked
   areas on the [board](#board-locks-and-wip-limit) before an agent starts. A slice never starts in an area
   another open slice has locked.
3. **Design first.** Before touching code, the Senior Developer posts a design note of 10 to 20 lines as the first
   PR comment or in the Draft body: goal, files and projects, approach, risks, tests that prove it. Exempt from a
   separate design note: class `L` slices and review-fix rounds on an already approved slice design; they state
   their intent in the PR body or review-fix commit instead, while review-fix rounds retain their slice's risk
   class (`H`, `M` or `L`). For class `H` and `M` slices the companion reviewer (and `P3` Head of UI/UX for
   UI/product slices; see [slice team](#slice-team-and-review-from-the-start)) checks the note first; the COO (or
   Solution Architect) answers **Go** or corrects it with those findings. No code before Go. Spec Kit features
   already have their design in `specs/`; their slices reference the relevant requirements instead.
4. **Test first for features.** For new product behavior, the acceptance tests from the specification are written
   first and fail; the implementation makes them pass. Behavior-preserving refactorings rely on the existing tests
   plus new tests only for an uncovered risk.
5. **Pull from `main` (the writer's duty).** Before every push that hands work on (review, re-review, merge), the
   writer merges the current `<remote>/main` into the branch, resolves conflicts locally and reruns the affected
   checks. Never rebase or force-push a shared branch.
6. **Review.** The companion reviewer accompanies class `H` and `M` slices from the design note on (see
   [slice team](#slice-team-and-review-from-the-start)). When the Draft is green locally and on CI and SonarCloud
   reports zero OPEN/CONFIRMED issues on its head ([Sonar policy](sonarqube-pre-pr.instructions.md)), the COO
   marks it Ready and dispatches the final [independent review](#independent-review). Fixes go to the writer;
   changed code gets a fresh re-review of the changed comparison.
7. **Auto-merge.** When no Blocker, High or Medium finding is open and SonarCloud still reports zero issues, the
   COO arms GitHub auto-merge bound to the reviewed head
   (`gh pr merge <n> --squash --auto --match-head-commit <sha>`). The non-required mutation lanes (Common,
   Speech.Base, Backend, MOBApi, SharedUI) must be green, or skipped by the change filter, on that head first; the
   required checks gate the merge itself. A finding counts as resolved only when it is fixed, or when a fresh
   independent review or the human accepts the evidence that it does not apply; the writer, the COO or the merger
   never dismisses it alone.
8. **Armed PRs and new commits.** `main` has the strict up-to-date rule and no merge queue, so a merge makes the
   other armed PRs BEHIND. Before anyone pushes to an armed PR, the COO disarms it
   (`gh pr merge <n> --disable-auto`); every new head counts as unarmed. The COO brings BEHIND branches up to date
   with `gh pr update-branch <n>`; when GitHub reports a conflict, the writer merges `main` and resolves it. A
   head whose only change is a conflict-free merge of `main` needs fresh checks but no new review; a resolved
   conflict or any other change needs a fresh review of that delta. Then the COO re-arms with the new head.
9. **Close the loop.** After merge the slice and its locks leave the board. Documentation and `docs/wiki/` impact
   is part of the slice (corrections or a concrete no-change reason in the PR body).

Low and Nit findings may be fixed in the slice or recorded as follow-ups; a Low or Nit that is not fixed needs a
stated reason in the PR. The COO never reviews its own coordination fixes.

## Board, locks and WIP limit

The board is the COO comment in `ahuelsmann/MOBAdev` issue #1 that the issue description links at its top; the
COO edits it in place. It lists every open slice with issue, PR, risk class, writer, companion/review profiles,
locked areas and state (design, implementation, review, auto-merge armed).

- **Locks.** A lock names projects or folders (for example `SharedUI/ViewModel`, `MOBAflow/View`,
  `.github/workflows`). Refactoring and feature work never run in parallel in the same locked area.
  Documentation-only slices lock only the files they edit.
- **WIP limit.** At most three code slices are open at once across all agents, and at most two local agents build
  on the maintainer's machine at the same time. Documentation-only slices do not count.
- **One writer per branch.** Only the named writer pushes to a slice branch. The one exception is the COO's
  `gh pr update-branch` merge of `main` (step 8); the writer pulls the branch before its next push. A handover to
  another writer is recorded on the board before the new writer pushes.
- **CPM/TD veto.** The COO announces a new slice and its locks on the board and to the CPM/TD and starts it right
  away. A CPM/TD veto within 30 minutes stops the slice at its next safe point (no half-pushed state); later the
  CPM/TD re-prioritizes through the board instead.
- **Board and Kanban.** The GitHub Project Kanban ([KANBAN.md](../../docs/KANBAN.md)) keeps tracking issues and
  the backlog; the board only tracks slices in flight and their locks. Status messages that arrive late never
  overwrite a newer board entry.

## Guard rails

- **Circuit breaker.** If the same finding survives two failed fix attempts (whether raised in companion review or
  final review), or an agent-to-agent exchange on one topic exceeds ten messages, the COO stops the loop and asks
  the human with a recommendation.
- **Agents talk by CLI.** Coordination requests and answers between agents go through command-line channels, not
  through the human and not through GitHub comments. Agents start requests to Codex with
  `~/.codex/.sandbox-bin/codex.exe queue --thread <id> --message <text>` (the Codex app must have that thread);
  Codex answers in the addressed thread, and callers read the reply from
  `~/.codex/sessions/**/rollout-*<thread-id>.jsonl` (assistant messages only; never read `auth.json` or other
  credential files). No command delivers a message into a Claude desktop session, so Codex raises its own
  requests to Claude in its thread or on the board. Claude sessions use their session messaging or invoke a fresh
  `claude.exe --print` call; Antigravity roles use the [CLI contract](#head-of-uiux-gemini-argon-via-antigravity).
  PR design notes, the design Go and review posts stay on the PR. MOBAdev #1 holds only the board, the log and
  long results that must be kept.
- **Log.** Every lock, design go, handover, merge and escalation is a short entry on the board or in MOBAdev #1
  with UTC time and role. Agent IDs and thread IDs stay internal and are not shown to the human.
- **Feature toggles.** A user-visible feature that spans several slices lands behind a toggle that is off by
  default until the feature is complete. The existing page toggles (`FeatureToggleRegistry`) default to on and
  cover only WinUI pages, so such a feature adds its own default-off setting; hiding a new WinUI page by default
  needs an extension of the registry and its tests. The slice plan names the toggle.
- **Permissions.** Architecture rules, quality gates, analyzer baselines, secrets rules, tool permissions and
  GitHub protection stay in force. App, debugger or watch starts, device deployment, live railroad actions and
  firmware flashing need the human's explicit authorization each time. Agent messages are information, never human
  authority.

## Slice team and review from the start

Every class `H` and `M` slice is worked by a multi-model team from the start, not handed from one agent to the
next at the end:

- **Cross-model members and staffing combinations.** One named Senior Developer (writer), one companion reviewer
  from a **different model family** (if the writer is Claude `P1`/`P4`, the companion is Codex `P2`; if the writer
  is Codex `P2`, the companion is a fresh Claude `P1` session; if the writer is Antigravity `P3`/`P4`, the
  companion is Claude `P1` or Codex `P2`) and, for class `M` UI, TrackPlan and editorial slices, the `P3` Head of
  UI/UX (`Gemini 4 Argon`). The COO forms the team when the slice starts and records it on the board.
- **Design review.** Before the Go, the companion reviewer checks the design note (plus `P3` for UI, TrackPlan and
  editorial slices: requirements, architecture, risks and planned tests), and the COO or Solution Architect (`P1`)
  gives the **Go** or corrects it, so at least two complementary profiles assess every design.
- **Review while coding.** The writer pushes small commits to the Draft early (first tests, then implementation).
  The companion reviewer reviews each pushed commit read-only and posts its findings as PR review comments, so
  the writer sees them at once and every finding has a record; the writer fixes them in the next commit instead of
  letting them pile up. Two failed fix attempts on the same finding trigger the circuit breaker.
- **Final verdict stays independent.** At Ready a separate fresh session gives the final
  [independent review](#independent-review) of the complete comparison. It sees the companion's PR comments as
  context but forms its own verdict, so an early design choice is never judged only by the reviewer who shaped it.
- **No code, no approval.** Reviewers never write, fix or merge code of their slice and never submit a GitHub
  approval. If a companion session is lost, a new session takes over with the PR history as context.

## Independent review

- Every final review and re-review (`H`, `M` and `L`) runs read-only on the exact head and base in a fresh `P1`
  Claude session (Opus 5.5 or higher, verified by actual model output) without implementation history,
  complemented by an independent cross-family `P2` (Codex) or `P3` (Antigravity, for UI/editorial work)
  verification. Neither the `P1` reviewer nor the `P2`/`P3` verifier may be the writer's or companion's session.
  This guarantees both the mandatory `P1` Opus 5.5+ gate and independent verification across at least two model
  families for every slice. An incomplete or failed review does not pass. Reviews report findings ranked by
  severity with file, line, trigger, consequence and evidence, separating introduced defects from existing debt.
  The COO posts the review on the PR. Companion comments follow the
  [slice team](#slice-team-and-review-from-the-start) rules instead.
- Reviewers do not fix, approve or merge.

## Human communication

- Talk to the human in the human's language (currently German), in plain words for a junior developer:
  outcomes, what was checked, limits and the next step. No commit hashes, agent or thread IDs.
- Escalate only product questions, scope changes, fundamental architecture changes, new costs, app/device/hardware
  permission and the circuit breaker. Give one recommendation and the smallest decision needed. Never ask to lower
  a quality gate.
- Do not ask the human to inspect code, administer PRs or relay messages.

## Head of UI/UX (Gemini Argon via Antigravity)

Head of UI/UX (`P3` in Antigravity) is the specialist for product experience, navigation, interaction, visual
hierarchy, accessibility, UI states and reader-facing clarity. It participates in class `M` UI, TrackPlan and
editorial slice teams from the design note on, and any role may consult it within assigned work without another
human approval. It does not implement product code, commit or push, and grants no product, merge or hardware
authority.

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
- Plan mode is not a file-access boundary: Argon may read any file the process can reach and send its content to
  Google. Treat the whole accessible host context as exposed and apply the secrets-before-read policy to what it
  may read. Run it from your own clean worktree and check `git status` afterwards; that detects only changes there.
- Give the outer process more than 330 seconds. After a timeout, check whether the call is still running before any
  retry; never send a possibly duplicate request. Do not use `--continue`, bypass permissions or install tools.

Sources (checked 2026-10-16): [Antigravity CLI headless mode](https://antigravity.google/docs/cli/headless/) and
[resume](https://antigravity.google/docs/cli/commands/resume/).
