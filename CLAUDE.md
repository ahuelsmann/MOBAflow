# MOBAflow Claude Code entry point

@AGENTS.md

AGENTS.md owns the repository workflow for every coding agent. Follow
[master coordination](.github/instructions/master-coordination.instructions.md) for the standing delivery mandate.
Masters may exchange task-related messages with Claude CLI. Chief Project Manager (formerly Issue Master) owns
the single execution and integration queue across all teams and clients. Deliver the prioritized issue through
integration in one leading PR, including documentation/wiki corrections. Other issues need its explicit allocation
after qualifying independence in purpose, files, behavior, baselines, build graph and execution resources; there
is no global one-task or five-task ceiling and no automatic PR per master. Within that allocation the Chief
Operating Officer (COO, Claude) deploys the agents since 2026-11-05, one Senior Developer agent per issue as
needed; a separate Project Manager is optional for large or complex work. Use the role names defined in master
coordination. Obtain the current allocation before starting or resuming substantial work, including Claude
refactoring; preserve one writer per worktree and serialize review/fix phases for the same comparison. The
supplier prepares a verified Draft; Chief Project Manager verifies the handover and the COO decides Ready and
merge. The existing Claude plan then owns fresh independent Opus 5.5-or-higher review, separate fixes,
independent re-review and SHA-bound squash merge. Do not dispatch a second review or merge. Technical Director
coordinates through that same queue. The Executive Assistant is the COO's cloud session for portable work without
authority of its own. The Senior Code Reviewer reviews PRs, branches and diffs independently with the
`mobaflow-code-review` skill, reports findings ranked by severity, fixes or merges nothing unless asked and never
reviews code written in the same session. Product goals, priorities and material UX decisions stay with the user;
since 2026-11-06 the user's personal acceptance is no Ready or merge gate, and feedback from real use arrives as
ordinary issues at the user's priority. Each integration records its documentation and `docs/wiki/` impact, including
justified no-change results and reassessment after functional fixes.
Use the guide's [handover evidence rules](.github/instructions/master-coordination.instructions.md#handover-evidence-and-delivery)
for observed timestamps, actual takeover and delayed or blocked messages; sender success alone proves no handover.
Solution Architect (Claude) owns the technical solution design within assigned scope; fundamental architecture
changes remain with the user. Art Director, Chief Project Manager, Solution Architect, Head of Quality,
Communications Director, Documentation Lead and Head of DevOps may independently consult the Head of UI/UX
(Gemini Argon via Antigravity) within assigned work,
using that guide's synchronous CLI contract and the same central execution and caller coordination.
This advice does not replace the Claude review. Claude Code loads the MOBAflow review and
diagnosis skills from `.claude/skills/`, which mirror `.agents/skills/`; change both copies together
(`python scripts/Test-AiRepositorySetup.py` checks that they match). The prompt secrets hook in
`.claude/settings.json` shares its script with Codex. See [AI-assisted development](docs/AI-DEVELOPMENT.md).
