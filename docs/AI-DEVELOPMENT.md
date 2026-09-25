# AI-assisted development

Open the dedicated task worktree, then read [AGENTS.md](../AGENTS.md). It owns the shared workflow
and validation policy. [The instruction index](../.github/instructions/instructions-index.md) links
technical guidance by task. Use [Spec Kit](SPEC-KIT.md) for cross-cutting features.

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
| GitHub Copilot | .github/copilot-instructions.md and matching scoped instructions | Links to the same policy; installed tools/authentication depend on the client |
| Clients reading .mcp.json | Empty mcpServers map | No extra server needed for this repository baseline; native tools remain available |

Filesystem, ripgrep and fetch MCP wrappers duplicated native tools. Their defaults and the unused Azure DevOps
connection were removed. Retained legacy Azure build definitions are historical files, not required tool connections.
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
Quality currently runs the full matrix on PRs, including documentation-only PRs.
See the [pipeline review](../specs/005-ai-repo-setup/pipeline-review.md) for observed runs and follow-ups.

Create Draft PRs. Mark ready only after current-commit SonarCloud passes and remote issue results show zero
OPEN/CONFIRMED findings. Main, local checks, prior PR commits and manual acceptance are separate evidence.

## Updating
Use the [Spec Kit update procedure](SPEC-KIT.md). For other skills, compare the exact source revision,
license and supporting resources, review local adaptations, then update source metadata and tests together.
Never activate the whole reference collection. Preserve unresolved origins rather than guessing an upstream.
Run structural checks plus representative invocation probes. Record missing authentication/platform checks.

## Official references
Reviewed 2026-09-25: [configuration](https://learn.chatgpt.com/docs/config-file/config-basic),
[skills](https://learn.chatgpt.com/docs/build-skills), [hooks](https://learn.chatgpt.com/docs/hooks),
[MCP](https://learn.chatgpt.com/docs/extend/mcp).
