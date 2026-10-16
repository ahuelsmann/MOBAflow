# MOBAflow Claude Code entry point

@AGENTS.md

AGENTS.md owns the repository workflow for every coding agent. Follow
[master coordination](.github/instructions/master-coordination.instructions.md) for the standing delivery mandate.
Masters may exchange task-related messages with Claude CLI; every PR review uses a fresh independent Claude
session. Chief Project Manager (formerly Issue Master) owns the single execution and integration queue across
all MOBAflow teams and clients. Obtain its current allocation before starting or resuming substantial work,
including Claude refactoring; run larger steps serially. Technical Director coordinates through that same queue.
Art Director, Chief Project Manager, Head of Quality, Communications Director, Documentation Lead and Head of
DevOps may independently consult the Head of UI/UX (Gemini Argon via Antigravity) within assigned work,
using that guide's synchronous CLI contract and the same central execution and caller coordination.
This advice does not replace the Claude review. Claude Code loads the MOBAflow review and
diagnosis skills from `.claude/skills/`, which mirror `.agents/skills/`; change both copies together
(`python scripts/Test-AiRepositorySetup.py` checks that they match). The prompt secrets hook in
`.claude/settings.json` shares its script with Codex. See [AI-assisted development](docs/AI-DEVELOPMENT.md).
