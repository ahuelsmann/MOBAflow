# AI-assisted development

Open the dedicated task worktree, then read [AGENTS.md](../AGENTS.md). It owns the shared workflow
and validation policy. [The instruction index](../.github/instructions/instructions-index.md) links
technical guidance by task. Use [Spec Kit](SPEC-KIT.md) for cross-cutting features.

## Work through the master chats

The user discusses goals and results with the responsible masters. An implementation assignment
includes delivery through merge under the [standing master mandate](../.github/instructions/master-coordination.instructions.md),
unless explicitly limited. Art Director owns routine product decisions inside the assigned scope and its
implementation, and hands work directly to Chief Project Manager (formerly Issue Master), the sole execution
and final integration coordinator. Technical Director coordinates resources and responsibilities through that
same queue. Documentation Lead and Head of DevOps deliver assigned documentation/user guidance and
GitHub/repository/CI infrastructure work respectively, under the same standing mandate. Technical
transitions, confirmed fixes and ordinary PR administration do not need repeated human approval. Important
product goals, priorities and material UX decisions, scope expansion, fundamental architecture changes, new
costs/external commitments and explicitly required practical permissions or acceptance still go to the user.

Across all MOBAflow teams, Codex, Claude and Antigravity, at most five larger tasks may be centrally allocated.
Before a substantial start or resume, obtain Chief Project Manager's current allocation in the existing queue;
available capacity alone is not a start assignment. Reconcile owners, actual processes and remote CI. Full
capacity or unclear ownership blocks another start. Keep one writer per worktree, serialize review/fix/publication
phases for the same comparison and preserve existing executions. Coordinate safe stopping points with owners.

The supplier prepares the Draft with current checks, CI/Sonar and required actual acceptance. Chief Project
Manager alone verifies handover readiness and sets Ready for review; independent review follows Ready, not the
reverse. The existing Claude plan owns fresh independent Opus 5.5-or-higher review, a separate fixer, independent
re-review after comparison changes and SHA-bound squash merge. Do not order a second review, create another
automation or merge concurrently. Chief Project Manager independently verifies actual merge and resulting main;
the delivery owner follows through to that outcome. Assigned work authorizes necessary team handovers, not
unrelated features or overrides of stops. Tool permissions and existing quality/acceptance gates remain in effect.

Apply the [handover evidence rules](../.github/instructions/master-coordination.instructions.md#handover-evidence-and-delivery)
to every team transition. Record the existing task, owner, one next recipient, observed phase/status, observation
time with time zone, next step and limits. Complete the handover before the supplier goes quiet and retain delivery
responsibility until actual recipient takeover. Sending proves neither takeover nor freshness; preserve newer
confirmed evidence when messages arrive late. Keep blocked delivery in the same task under existing permissions.

Every integration assesses architecture/developer documentation and the versioned user guides in `docs/wiki/`.
Record the pages corrected or a concrete no-change reason in the handover and completion evidence. Reassess after
behavior-changing review fixes and confirm against actual integrated main. Correct drift within the assigned
scope; coordinate any remaining work through the same Chief Project Manager queue and state the limitation. Do not
invent text changes or infer new user behavior from a refactoring. This does not authorize separate wiki publication.

Art Director, Chief Project Manager, Head of Quality, Communications Director, Documentation Lead and Head of
DevOps may independently consult the existing Head of UI/UX conversation (Gemini Argon via Antigravity CLI)
within assigned work, without another human approval. Coordinate task allocation and the active caller centrally;
contacts are serial and synchronous. This supersedes the earlier contact restriction for the two new masters.
Use the verified contact and synchronous request/response contract in the
[Head of UI/UX rules](../.github/instructions/master-coordination.instructions.md#head-of-uiux-gemini-argon-via-antigravity).
Its advice adds no approval gate and does not replace tests, UI acceptance or Claude PR reviews.

## Start in the correct checkout
Use Git, PowerShell 7 (`pwsh`), Python 3.11+ and `rg` for development checks.
Install product SDKs/workloads only for the affected build target.
```powershell
git status --short --branch
git remote -v
git worktree list --porcelain
./scripts/Resolve-GitHubRepository.ps1
./scripts/Test-InstructionConsistency.ps1
python scripts/Test-AiRepositorySetup.py
```
Run these from the task worktree root. The resolver emits the actual root, remote and GitHub repository.
Tracking selects a GitHub remote; without tracking exactly one GitHub remote is required.
Resolve ambiguity explicitly before creating issues. Never guess `origin`.

## Configuration and available tools
| Client | Repository entry | Behavior |
| --- | --- | --- |
| Codex | AGENTS.md, .agents/skills, .codex/config.toml, .codex/hooks.json | Project config/hooks require project trust; native file/search tools use the active working directory |
| Claude Code | CLAUDE.md (imports AGENTS.md), .claude/skills, .claude/settings.json | Settings and hooks apply after the workspace trust prompt; personal settings stay in .claude/settings.local.json (ignored) |
| GitHub Copilot | .github/copilot-instructions.md and matching scoped instructions | Links to the same policy; installed tools/authentication depend on the client |
| Clients reading .mcp.json | Empty mcpServers map | No extra server needed for this repository baseline; native tools remain available |

Filesystem, ripgrep and fetch MCP wrappers duplicated native tools. Their defaults and the unused Azure DevOps
connection were removed.
Personal plugins/connectors and credentials are not shared by these files or changed by repository updates.
A client lacking native capabilities needs an explicitly configured and tested alternative; empty MCP config
does not promise every client has the same tools.

The optional Sonar MCP entry invokes `sonar run mcp --project ahuelsmann_MOBAflow2`.
Install/authenticate Sonar CLI separately; `sonar auth status` verifies access.
Validated locally with Sonar CLI 1.4.0; use this version as the reproduction baseline. The repository does not install or silently update
that executable. The MCP is for remote findings; code analysis runs only through GitHub CI.

## Secrets hook and trust
The prompt hook locates the Git root, launches PowerShell 7 and anchors scanning to that hook's worktree.
It uses Sonar's deterministic prompt hook, not local Sonar/Vortex code analysis.
Missing Sonar prints a visible limitation; scanner failures propagate. Never paste tokens into prompts.
Project trust and actual hook execution must be checked in the client; valid JSON is not proof of activation.
The hook requires Git and pwsh in PATH. Windows and POSIX launch commands are recorded separately.
Claude Code runs the same script with `-Client claude` from `.claude/settings.json`; it calls
`sonar hook claude-prompt-submit` instead of `codex-prompt-submit`. The command resolves the script from
`$CLAUDE_PROJECT_DIR`, the project root Claude Code sets, so it still runs when the session's working directory
moves elsewhere. Claude Code runs hook commands through a POSIX shell (Git Bash on Windows), so one command
serves both platforms.

Repository policy scans likely secret-bearing inputs before reads and all changed files before publication.
A session/global instruction can require every file to be scanned first; the stricter active rule still applies.
Repository configuration cannot disable those global instructions.

## Active skills
Codex discovers repository skills in `.agents/skills/`. A fresh clone includes ten Spec Kit skills plus:
- `$mobaflow-code-review`: focused, read-only review against requirements and repository rules.
- `$mobaflow-diagnosing-bugs`: bounded reproduction, evidence and targeted regression validation.
- `$audit-code-quality`: explicitly requested whole-repository quality audit.

Examples: “Use $mobaflow-code-review to review this PR against Issue145”;
“Use $mobaflow-diagnosing-bugs to investigate rows from the previous project reappearing”.
A documentation edit should not trigger a whole-repository audit or a product build.
Claude Code loads the two MOBAflow skills from `.claude/skills/`. Those folders are copies of the
`.agents/skills/` sources, recorded as `mirrors` in the source registry; change both together, and
`python scripts/Test-AiRepositorySetup.py` fails when they differ. Spec Kit and audit skills are not mirrored.
Personal skills are separate. A globally installed `audit-code-quality` may duplicate the repository name;
check which source the client loads. This repo does not remove personal installations.
The [source registry](../.agents/skills/sources.json) records active origins and adaptations.
The [reference collection](../skills/README.md) is not another active skill directory.

## Validation and GitHub
Select checks from AGENTS.md. Documentation uses structural checks; tooling changes use isolated fixtures.
Do not launch the app or use hardware merely to validate development configuration.
Before publication, scan changed files, normalize/check line endings, inspect the diff and run applicable checks.
```powershell
python scripts/Test-AiRepositorySetup.Tests.py
./scripts/Test-SpecKitGovernance.Tests.ps1
./scripts/Test-SpecKitGovernance.ps1 -Mode PullRequest -BaseRef github/main
```
Replace the base ref when the configured remote differs.

For a PR, resolve its latest head SHA and inspect checks for that SHA:
```powershell
gh pr view <number> --json headRefOid,statusCheckRollup
gh pr checks <number>
gh run view <run-id> --json headSha,status,conclusion,jobs
```
Success, failure, skip, cancellation, queue and in-progress states are different outcomes.
A skipped job does not prove its checks ran. For a slow job, compare step timestamps and safe logs with
successful runs before assuming a hang. Avoid exposing secrets while reading/downloading logs.
Quality always runs repository consistency checks. Documentation-only PRs skip
product build/test and mutation jobs. Code-relevant PRs run desktop, display and
Common mutation checks; Domain mutation and firmware steps also depend on input
paths. Android Release AAB runs only on manual dispatch with `validate_android=true`.
Main pushes and manual runs enable the remaining scopes. See
[Build Performance](BUILD-PERFORMANCE.md#ci-note) and
[`Get-QualityChangeScope.ps1`](../scripts/Get-QualityChangeScope.ps1) for the exact selection.
[Documentation consistency](DOCUMENTATION-CONSISTENCY.md) describes the always-on
generated-fact checks and the separately activated semantic maintenance workflow.
See the [pipeline review](../specs/005-ai-repo-setup/pipeline-review.md) for observed runs and follow-ups.

Create Draft PRs. Chief Project Manager alone marks ready after checking the current comparison, applicable CI,
current-commit SonarCloud success with zero OPEN/CONFIRMED issues, actual acceptance, ownership and documentation
impact. The existing Claude plan performs the subsequent independent review and merge under
[master coordination](../.github/instructions/master-coordination.instructions.md). Main, local checks, prior PR
commits and manual acceptance are separate evidence. Non-GitHub work, including separately authorized Sites
review/publication and independent live verification, retains its own assigned route.

## Updating
Use the [Spec Kit update procedure](SPEC-KIT.md). For other skills, compare the exact source revision,
license and supporting resources, review local adaptations, then update source metadata and tests together.
Never activate the whole reference collection. Preserve unresolved origins rather than guessing an upstream.
Run structural checks plus representative invocation probes. Record missing authentication/platform checks.

## Official references
Reviewed 2026-09-25: [configuration](https://learn.chatgpt.com/docs/config-file/config-basic),
[skills](https://learn.chatgpt.com/docs/build-skills), [hooks](https://learn.chatgpt.com/docs/hooks),
[MCP](https://learn.chatgpt.com/docs/extend/mcp).
