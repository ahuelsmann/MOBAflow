# MOBAflow Claude Code entry point

@AGENTS.md

AGENTS.md owns the repository workflow for every coding agent. Follow
[master coordination](.github/instructions/master-coordination.instructions.md) for the standing delivery mandate.
Both masters may exchange task-related messages with Claude CLI; every PR review uses a fresh independent
Claude session. Claude Code loads the MOBAflow review and
diagnosis skills from `.claude/skills/`, which mirror `.agents/skills/`; change both copies together
(`python scripts/Test-AiRepositorySetup.py` checks that they match). The prompt secrets hook in
`.claude/settings.json` shares its script with Codex. See [AI-assisted development](docs/AI-DEVELOPMENT.md).
