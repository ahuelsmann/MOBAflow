---
name: mobaflow-code-review
description: Review a MOBAflow PR, branch or uncommitted diff against its requirements and repository rules. Use for focused change reviews; use audit-code-quality only for a requested repository-wide audit.
---

# MOBAflow change review
Read [AGENTS.md](../../../AGENTS.md) and the relevant entries in the
[instruction index](../../../.github/instructions/instructions-index.md).

For a PR, follow the [delivery flow](../../../.github/instructions/master-coordination.instructions.md#delivery-flow):
a companion reviewer accompanies each code slice from its design note on and posts per-commit findings as PR
review comments; at Ready the Chief Operating Officer (COO, Claude) dispatches one separate fresh independent
Opus 5.5-or-higher review of the exact comparison, posts it, and arms head-bound auto-merge once no Blocker, High or
Medium remains and all gates are green. As companion, comment read-only on each new commit; as final reviewer, use
the companion's comments as context but form an independent verdict.
Check that the slice stays within its design note and locked areas and flag a slice that is
too large to review well. Do not dispatch another PR review or merge concurrently. Coordination/implementation
sessions and Codex self-review do not satisfy the independent PR review. Every review session (PR, branch or local diff) is read-only; the reviewer never edits, merges or
approves its own review. Confirm actual model use and preserve all current quality gates and explicit stops.

Establish the requested base and exact commit/diff. For a PR use its actual base;
for work in progress include staged/unstaged changes when requested. Infer the base from
the task/PR context; ask only if it cannot be established. An empty diff means no changes to review.
Use the linked issue or specs as requirements; report missing requirements without inventing them.
Assess architecture/developer documentation and canonical `docs/wiki/` impact for the exact change; require
necessary corrections or a concrete no-change reason. Behavior-changing fixes need renewed assessment and
independent review of the changed comparison; completion evidence refers to actual integrated main.

Review both requirement fulfillment and project constraints. Inspect callers and relevant tests
for concrete risks in runtime ownership, project switching, EventBus threading, persistence,
protocols and platform boundaries. Distinguish defects from preferences and existing unrelated debt.
The normal review is read-only. Subagents, installations and repository-wide audits are not required.

Report only actionable findings, highest impact first: file/line, trigger, consequence and evidence.
Mention actual tests separately from static inspection. A clean review should say so without invented findings.
Use GitHub CI for Sonar analysis and read its current-commit results; never run local Sonar/Vortex code analysis.
App starts and hardware actions still require explicit authorization.
Source and adaptations: [sources.json](../../../.agents/skills/sources.json); upstream license: [LICENSE](LICENSE).
