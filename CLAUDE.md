# MOBAflow Claude Code entry point

@AGENTS.md

AGENTS.md owns the repository workflow for every coding agent. Since 2026-11-16 all agents deliver in small
batches on `main` as defined in [master coordination](.github/instructions/master-coordination.instructions.md):
small slices with their own Draft PR, a board with code-area locks and a WIP limit in `ahuelsmann/MOBAdev`
issue #1, a short design note and Go before code, test-first for new features, merging (never rebasing) the
current `main` into the branch before every hand-on push, a fresh independent Opus 5.5-or-higher review and
head-bound auto-merge once the review is clean and all gates are green. Two failed review/fix rounds on the
same finding or more than ten agent messages on one topic go to the user. Use the role names defined there.

Any role may consult the Head of UI/UX (Gemini Argon via Antigravity) within assigned work under that guide's
synchronous CLI contract; this advice does not replace the Claude review. Claude Code loads the MOBAflow review
and diagnosis skills from `.claude/skills/`, which mirror `.agents/skills/`; change both copies together
(`python scripts/Test-AiRepositorySetup.py` checks that they match). The prompt secrets hook in
`.claude/settings.json` shares its script with Codex. See [AI-assisted development](docs/AI-DEVELOPMENT.md).
