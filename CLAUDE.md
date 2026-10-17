# MOBAflow Claude Code entry point

@AGENTS.md

AGENTS.md owns the repository workflow for every coding agent. Follow
[master coordination](.github/instructions/master-coordination.instructions.md) for the standing delivery mandate.
Masters may exchange task-related messages with Claude CLI. Chief Project Manager (formerly Issue Master) owns
the single execution and integration queue across all teams and clients, with at most five allocated larger tasks.
Obtain its current allocation before starting or resuming substantial work, including Claude refactoring; preserve
one writer per worktree and serialize review/fix phases for the same comparison. The supplier prepares a verified
Draft; Chief Project Manager alone sets Ready for review. The existing Claude plan then owns fresh independent
Opus 5.5-or-higher review, separate fixes, independent re-review and SHA-bound squash merge. Do not dispatch a
second review or merge. Technical Director coordinates through that same queue. Each integration records its
documentation and `docs/wiki/` impact, including justified no-change results and reassessment after functional fixes.
Art Director, Chief Project Manager, Head of Quality, Communications Director, Documentation Lead and Head of
DevOps may independently consult the Head of UI/UX (Gemini Argon via Antigravity) within assigned work,
using that guide's synchronous CLI contract and the same central execution and caller coordination.
This advice does not replace the Claude review. Claude Code loads the MOBAflow review and
diagnosis skills from `.claude/skills/`, which mirror `.agents/skills/`; change both copies together
(`python scripts/Test-AiRepositorySetup.py` checks that they match). The prompt secrets hook in
`.claude/settings.json` shares its script with Codex. See [AI-assisted development](docs/AI-DEVELOPMENT.md).
