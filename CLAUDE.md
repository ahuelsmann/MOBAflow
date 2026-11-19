# MOBAflow Claude Code entry point

@AGENTS.md

AGENTS.md owns the repository workflow for every coding agent. Since 2026-11-16 all agents deliver in small
batches on `main`, expanded on 2026-11-17 into a multi-model agent network across Claude Code, ChatGPT Codex and
Antigravity as defined in [master coordination](.github/instructions/master-coordination.instructions.md):
roles map to capability profiles (`P1`–`P4`) rather than a single fixed model; each slice has its own Draft PR, a
risk class (`H`, `M` or `L`; review-fix rounds keep their slice's risk class and dependency updates take the class
of the affected area), a cross-model slice team for class `H` and `M` (writer + companion reviewer from a different
model family + `P3` Head of UI/UX for UI/product work), a Kanban item with locks and a PR slice header,
a short design note and Go before code (class `L` and review-fix rounds state intent in the PR body/commit), test-
first for new features, merging (never rebasing) the current `main` into the branch before every hand-on push, a
fresh independent `P1` (Opus 5.5 or higher, never the writer's or companion's session) plus cross-family `P2`/`P3`
review at Ready for every slice, and head-bound auto-merge once all gates are green. Two failed fix attempts on the
same finding or more than ten agent messages on one topic go to the user.

Any role may consult or include the Head of UI/UX (`Gemini 4 Argon` via Antigravity) within assigned work under that
guide's synchronous CLI contract. Claude Code loads the MOBAflow review and diagnosis skills from `.claude/skills/`,
which mirror `.agents/skills/`; change both copies together (`python scripts/Test-AiRepositorySetup.py` checks that
they match). The prompt secrets hook in `.claude/settings.json` shares its script with Codex. See
[AI-assisted development](docs/AI-DEVELOPMENT.md).
