---
name: mobaflow-code-review
description: Review a MOBAflow PR, branch or uncommitted diff against its requirements and repository rules. Use for focused change reviews; use audit-code-quality only for a requested repository-wide audit.
---

# MOBAflow change review
Read [AGENTS.md](../../../AGENTS.md) and the relevant entries in the
[instruction index](../../../.github/instructions/instructions-index.md).

Establish the requested base and exact commit/diff. For a PR use its actual base;
for work in progress include staged/unstaged changes when requested. Infer the base from
the task/PR context; ask only if it cannot be established. An empty diff means no changes to review.
Use the linked issue or specs as requirements; report missing requirements without inventing them.

Review both requirement fulfillment and project constraints. Inspect callers and relevant tests
for concrete risks in runtime ownership, project switching, EventBus threading, persistence,
protocols and platform boundaries. Distinguish defects from preferences and existing unrelated debt.
The normal review is read-only. Subagents, installations and repository-wide audits are not required.

Report only actionable findings, highest impact first: file/line, trigger, consequence and evidence.
Mention actual tests separately from static inspection. A clean review should say so without invented findings.
Use GitHub CI for Sonar analysis and read its current-commit results; never run local Sonar/Vortex code analysis.
App starts and hardware actions still require explicit authorization.
Source and adaptations: [sources.json](../sources.json); upstream license: [LICENSE](LICENSE).
