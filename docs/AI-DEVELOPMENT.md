# AI-assisted development

Open the dedicated task worktree, then read [AGENTS.md](../AGENTS.md). It owns the shared workflow
and validation policy. [The instruction index](../.github/instructions/instructions-index.md) links
technical guidance by task. Use [Spec Kit](SPEC-KIT.md) for cross-cutting features.

## Work through the master chats

The user discusses goals and results with the responsible masters. An implementation assignment includes
delivery through merge under the flow in
[master coordination](../.github/instructions/master-coordination.instructions.md#delivery-flow), unless
explicitly limited. Since 2026-11-16 MOBAflow works in small batches on `main`:

1. The Chief Operating Officer (COO, Claude) cuts an issue into small slices and records each slice with the code
   areas it locks on the board in `ahuelsmann/MOBAdev` issue #1. At most three code slices are open at once.
2. The Senior Developer posts a short design note; code starts after the COO's or Solution Architect's Go. New
   features write their acceptance tests first.
3. Before handing work on, the developer merges the current `main` into the branch and resolves conflicts locally.
4. A fresh independent Claude session reviews the Draft. With a clean review and all gates green, the COO arms
   head-bound auto-merge.
5. Repeated review/fix loops on the same finding stop after two rounds and go to the user.

The user decides product goals, priorities, material UX, fundamental architecture, new costs and every app,
device or hardware action, and does not relay messages, inspect code or administer PRs. Since 2026-11-06 the
user's personal acceptance is no merge gate; feedback from real use arrives as ordinary issues. Every slice
records its documentation and `docs/wiki/` impact. Any role may consult the Head of UI/UX (Gemini Argon via
Antigravity CLI) under the
[Head of UI/UX rules](../.github/instructions/master-coordination.instructions.md#head-of-uiux-gemini-argon-via-antigravity);
its advice adds no approval gate.

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

Repository policy scans likely secret-bearing inputs before reads and all changed files before publication
in local sessions; cloud sessions without a scanner avoid such files and record the missing scan (see `AGENTS.md`).
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
the Common, Speech.Base, Backend, MOBApi and SharedUI mutation checks; Domain mutation
and firmware steps also depend on input paths. Android Release AAB runs on PRs that change
`MOBAsmart/` or `quality/analyzer-baseline.android.json` and on manual dispatch with
`validate_android=true`; shared-project changes do not start it.
Main pushes and manual runs enable the remaining scopes. See
[Build Performance](BUILD-PERFORMANCE.md#ci-note) and
[`Get-QualityChangeScope.ps1`](../scripts/Get-QualityChangeScope.ps1) for the exact selection.
[Documentation consistency](DOCUMENTATION-CONSISTENCY.md) describes the always-on
generated-fact checks and source-grounded developer/wiki maintenance in the same
issue/PR through existing Codex delivery, independent Claude review and actual-main
verification. No separate documentation API or App bot is activated.
See the [pipeline review](../specs/005-ai-repo-setup/pipeline-review.md) for observed runs and follow-ups.

Create Draft PRs, one per slice. The COO verifies current CI, current-commit SonarCloud success with zero
OPEN/CONFIRMED issues and the documentation impact, dispatches the independent review and arms head-bound
auto-merge under [master coordination](../.github/instructions/master-coordination.instructions.md#delivery-flow). Main, local checks and prior PR
commits are separate evidence. Non-GitHub work, including separately authorized Sites
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
