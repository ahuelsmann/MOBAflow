# Implementation plan: AI repository setup

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/145
**Spec Kit**: Required
**Status**: Historical implementation record; issue closed
**Date**: 2026-09-25
**Planning base**: `github/main` at `2b65379fb4f4c6cb23394ea9d44467c6801f027f`
**Planning branch**: `codex/issue-145-ai-repo-setup`

## Purpose and scope

A fresh clone and each isolated Git worktree should provide the same clear project
rules, relevant skills and reliable validation workflow. Setup must be understandable
without Andreas' personal skill installation. File access must follow the active worktree.

This plan records the approved implementation, not a new work assignment. PR #150
published the original plan. On 2026-09-25, the user added pipeline review as P7,
clarified P2 and approved implementation. Issue #145 was closed on 2026-10-09.
The specification, tasks and evidence remain under `specs/005-ai-repo-setup/`.

The unchecked acceptance items below are historical records, not proof that the
checks passed. The retained [validation evidence](../specs/005-ai-repo-setup/validation.md)
still distinguishes completed probes from pending Codex-client evidence. This
documentation update does not perform those probes or reopen the issue.

Scope: AGENTS.md, README and contributor onboarding, specialized instructions,
Spec Kit integration, repository skills, MCP configuration, structural validation
and GitHub pipeline review.

Excluded: product features, journey/workflow behavior changes, app launches,
hardware actions, replacing CI, or blanket community-skill upgrades. Personal Codex
settings and global skills are not changed automatically. Adding `domain-modeling`
remains a separate decision.

## Historical baseline

These findings describe the September audit, not the current implementation:

- The 2026-09-24 audit inspected the main checkout at `698c4c8b`; the initial plan
  used `57b96c1f`. On 2026-09-25 it was compared with GitHub main at `d0ffddc4`.
  The later pipeline extension used `010caeb5`; it did not repeat the entire audit.
- PR #138 already required GitHub-only Sonar code analysis. Local Sonar/Vortex
  code analysis and analysis hooks were excluded; local secrets scans remained required.
- PR #134 already prohibited new legacy/migration paths for superseded product
  models and unrequested mechanisms. P1 checked consistency rather than reintroducing
  those rules. Development-tool upgrades are not product-data migrations.
- Eleven project skills existed under `.agents/skills/`: ten Spec Kit skills and
  `audit-code-quality`. Another 42 SKILL.md files were reference material under `skills/`,
  not a standard Codex discovery path.
- CLI and managed Spec Kit integration were at 1.0.4, including at `d0ffddc4`.
  Integration reported no missing or changed managed files. The pinned upgrade target
  found during the audit was 1.0.11.
- Comparing 41 Matt Pocock skills after line-ending normalization found 30 differences,
  three identical files and eight no longer present upstream, against
  `c55ee46073ed923f86ce59a5eb3b6d895095d1b7`. Differences did not authorize updates.
  The proposed external source for `windows-app-developer` could not be retrieved.
- The Filesystem MCP pointed at the main checkout. `speckit-taskstoissues` assumed
  `origin`, while this repository used `github`; the 1.0.11 template had the same assumption.
- Constitution/templates used blanket test commands despite scope-based AGENTS.md rules.
- Unrelated edits to `.codex/config.toml` and `MOBAflow/solution.json` were not copied
  into the planning worktree. The 2026-09-25 main checkout was clean. Never reconstruct
  or overwrite historical user edits from this record.

## Preparation

- [x] V1: Check issue #145, current main and overlapping changes. Use a clean,
  dedicated `codex/` branch/worktree; do not copy personal configuration.
- [x] V2: Create a collision-free Spec Kit feature directory, reference #145 and
  record the actual path. Cover skill discovery, worktree isolation, instruction
  consistency, reproducible updates, offline checks and GitHub validation.
- [x] V3: Complete specification, technical plan, tasks and consistency analysis.
  Resolve technical questions with documentation and small probes; ask the user
  only about material scope decisions.
- [x] V4: Recheck official Codex/Copilot configuration schemas and imported skill
  provenance. Keep the audited 1.0.11 target; do not silently select a newer version.

## Package order

| Package | Priority | Depends on | Result |
| --- | --- | --- | --- |
| P1 Consolidate instructions | High | V1-V4 | One project/validation policy |
| P2 Worktree-aware AI tools | High | P1 | Required tools use the active worktree |
| P3 Update Spec Kit | High | P1 | Pinned integration with correct remote resolution |
| P4 Add project skills | Medium | P1, P3 | Two focused skills without name collisions |
| P5 Document onboarding/provenance | Medium | P2-P4 | Reproducible setup and traceable sources |
| P7 Review GitHub pipelines | High | P1 | Evidence for triggers, required checks and runtime |
| P6 Final validation | High | P1-P5, P7 | Fresh-clone, worktree and CI evidence |

Checks accompany each package. P6 collects evidence and repeats passed checks only
after changes or for unresolved risk. P7 retains its later-added number and precedes P6 completion.

## P1: Consolidate instructions

Files: `AGENTS.md`, `.github/copilot-instructions.md`, `.github/instructions/`,
`.specify/memory/constitution.md`, template overrides and `CONTRIBUTING.md`.

- [x] P1.1: Keep AGENTS.md as the shared workflow, architecture and validation source.
  Copilot/index entry points remain short links, not duplicate rule sets.
- [x] P1.2: Align Constitution and plan/task templates with scope-based validation.
  Retain meaningful regression tests; update version/impact notes according to governance.
- [x] P1.3: Preserve PR #138 CI-only Sonar and PR #134 legacy/scope boundaries;
  inspect Spec Kit guidance and imported skills for conflicting instructions.
- [x] P1.4: Focus Fluent/naming guidance on MOBAflow decisions. Link existing references
  for repeated examples; retain architecture/security rules without arbitrary size limits.
- [x] P1.5: Align auto-save examples with real subscription/unsubscription behavior.
  Use correct source links or complete patterns; repair Markdown comment-example fences.
- [x] P1.6: Explain global versus repository scan rules. Local guidance cannot disable
  global rules; identify any outside-repository change separately without making it a prerequisite.

Acceptance: Documentation, backend fixes and UI changes each have a consistent check
selection. Scopes and links are valid; history is not presented as current requirements.
Do not add tests that merely reproduce documentation text.

## P2: Make required AI tools worktree-aware

MCP is the interface used by additional assistant tools. Determine which clients
actually need repository-configured tools, how those tools start, and which directory
they access. A file server fixed to main may read or edit the wrong revision.
Remove redundant tools only after checking usage; retain required access.

Files: `.codex/config.toml`, `.mcp.json`, `.codex/hooks.json`, the existing secrets
hook, and a launcher only if a required client needs one.

- [x] P2.1: Record configuration loaded by each client and its native tools.
  Separate repository setup from personal plugin/connector installation.
- [x] P2.2: Remove Azure DevOps from default repository MCP setup when no current
  workflow needs it. Legacy build definitions remain outside this package.
- [x] P2.3: Prefer native Codex file/search/web tools. Retain redundant MCP servers
  only for a demonstrated client need; add none speculatively.
- [x] P2.4: Derive remaining file-server roots from the active worktree. Use supported
  variables or a small verified launcher; missing context must fail visibly, not fall back to main.
- [x] P2.5: Pin retained external servers and document prerequisites. Do not portray
  unsupported variables as security controls or put tokens in repository files.
- [x] P2.6: Check secrets-hook paths, supported platforms, missing tools and visible
  failures. Preserve scanner-unavailable behavior. A hook file alone does not prove local trust/activation.

Acceptance: Artificial markers in temporary repositories prove isolation across two
worktrees without writing to main. JSON/TOML are valid; required launch commands work
on supported platforms. Unavailable platform checks remain explicitly open.

## P3: Update Spec Kit safely

Files: `.specify/`, `.agents/skills/speckit-*/`, `docs/SPEC-KIT.md` and focused helpers.

- [x] P3.1: Record existing versions, managed files, overrides and `common.ps1`
  adaptations. Start with an isolated pinned installation, not a hidden personal CLI change.
- [x] P3.2: Upgrade integration to 1.0.11 through supported commands. Preserve/reapply
  Constitution, templates and source adaptations using the intended manifest process.
  Do not rewrite hashes to pretend an unchanged status.
- [x] P3.3: Resolve the actual GitHub remote for `speckit-taskstoissues`: prefer the
  tracking remote, otherwise one matching GitHub remote. Fail clearly if missing/ambiguous;
  never assume `origin` or a repository identity. Document local adaptation and provenance.
- [x] P3.4: Test `github`/`origin`, SSH/HTTPS, absent and multiple remotes in temporary
  Git repositories. Do not create real GitHub issues for tests.
- [x] P3.5: Check status, PowerShell syntax, template resolution and governance.
  Probe the workflow in isolation without overwriting active feature metadata.

Acceptance: Versions/adaptations are documented; no unexplained missing/modified files
or unchecked manifests. Registered adaptations are allowed. Remote probes and P1 check selection pass.

## P4: Add two focused MOBAflow skills

New directories: `.agents/skills/mobaflow-code-review/` and
`.agents/skills/mobaflow-diagnosing-bugs/`. Existing skill: `audit-code-quality`.

- [x] P4.1: Select reviewed community templates and companions; record source,
  license and adaptations. Distinct names avoid collisions with personal review/diagnosis skills.
- [x] P4.2: Review a fixed diff read-only against requirements/rules. Report supported
  findings with impact and evidence; do not invent issues or require multiple agents/full audits.
- [x] P4.3: Diagnose through reproduction, isolation, hypotheses and focused checks.
  Use runtime/Z21 fakes and .NET 10/NUnit/PowerShell conventions; do not launch hardware without approval.
- [x] P4.4: Write clear triggers/boundaries; remove routine confirmations and foreign
  tool assumptions. Ask only for genuinely unresolved behavior or scope decisions.
- [x] P4.5: Align audit-code-quality with CI-only Sonar and narrow triggers.
  Explain the global name duplicate without changing the global installation.

Acceptance: A fresh clone finds both skills without personal installations.
Companions exist; typical/non-trigger tasks and safe artificial diagnosis cases are checked.

## P5: Document onboarding and provenance

Files: README, CONTRIBUTING, `docs/AI-DEVELOPMENT.md`, `docs/SPEC-KIT.md`,
`.agents/skills/sources.json` and `skills/README.md`.

- [x] P5.1: Add short README links to AGENTS.md and the AI guide; avoid duplicated prerequisites.
- [x] P5.2: Explain setup, skill calls, personal versus bundled skills, worktrees,
  validation and updates with real tools; keep hardware authorization explicit.
- [x] P5.3: Record active-skill name/path, source URL or local origin, pinned revision,
  review date and adaptations/license. Reference Spec Kit manifests instead of duplicating hashes.
- [x] P5.4: Mark `skills/` as reference material and describe update state.
  Do not activate/delete personal, experimental or deprecated skills blindly.
  Mark unverified sources such as windows-app-developer as unresolved.

Acceptance: New contributors can discover skills/provenance and select checks from
the linked documents alone. Machine-specific user paths are not general installation instructions.

## P6: Validation and delivery

Use existing scripts/CI first; add a small setup checker only when required.

- [x] P6.1: Extend structural checks for links, metadata, unique active names,
  provenance and parseable configuration. Scope active guidance; do not promise automatic semantic consistency.
- [x] P6.2: Add negative fixtures for missing links, duplicate names, invalid config
  and wrong worktree roots. Extend existing checkers before introducing infrastructure/dependencies.
- [x] P6.3: Integrate automation into instruction-consistency CI without personal
  credentials, local Sonar code analysis or running MCP servers.
- [ ] P6.4: Probe documentation, focused review and simulated diagnosis in a fresh
  clone, isolated user context and second worktree. Record discovery, rules, paths
  and check selection; report missing authentication as an evidence gap.
- [ ] P6.5: Review changes/adaptations, secrets and line endings. Deliver a draft PR
  against actual GitHub main; require current-commit green Sonar and zero OPEN/CONFIRMED issues before readiness.

Acceptance: Applicable structure/function checks pass. CI/manual probes are reported
separately. Static hook/MCP validity alone does not prove a running service.

## P7: Review GitHub pipelines

Sources: workflows, invoked scripts/build configuration, actual Actions runs,
branch protection/rulesets and required checks. Record unavailable permissions as open evidence.

- [x] P7.1: Inventory jobs, triggers, filters, dependencies and runners; map expected
  checks for documentation, shared .NET, WinUI and Android Release/AAB changes.
- [x] P7.2: Compare job/check names and conditions with required checks. Look for
  permanently pending or bypassed gates; retain Sonar and analyzer/quality thresholds.
- [x] P7.3: Inspect representative runs by commit, timestamps and logs. Distinguish
  queue wait, active work and lack of progress; check limits, cancellation and retries.
  Include the [PR #154 Android job](https://github.com/ahuelsmann/MOBAflow/actions/runs/36130988570/job/108057952187?pr=154).
  Its URL alone proves neither a hang nor current status.
- [x] P7.4: Check SDK/workload versions, restore/build/publish, caches, permissions,
  secret usage and pinned actions. Validate Android Release/AAB and bundle checks;
  FastDebug is not a substitute. Do not print credentials.
- [x] P7.5: Record findings with job/configuration evidence and recommendations.
  Explain pending/failed pipeline results in the AI guide. Necessary workflow changes
  are separate reviewable tasks, not automatic changes or run cancellations.

Acceptance: Change types map to expected checks. Configuration/runs support findings;
"still running" alone does not prove a defect. Passed, failed, skipped and pending
checks are distinguished for the current PR commit, with explicit follow-up criteria.

## Validation commands

Run applicable entry points from the dedicated worktree:

```powershell
pwsh -NoProfile -File scripts/Test-InstructionConsistency.ps1
pwsh -NoProfile -File scripts/Test-SpecKitGovernance.Tests.ps1
pwsh -NoProfile -File scripts/Test-SpecKitGovernance.ps1 -Mode PullRequest -ChangedFiles plans/ai-repo-setup.md
specify version
specify integration status
specify check
```

Pass all changed plan/spec paths or the real PR base to governance. Invoke new
checkers only after implementation. Normalize/check changed text paths, check
`-Staged` before a requested commit, and scan changed files with `sonar analyze secrets`.

These planned changes do not alter product logic. They do not require product builds
or app launches. If the actual diff changes behavior/builds, disclose the scope
extension and apply the AGENTS.md validation table.

## Reviewable delivery and recovery

Keep cohesive commits for rules, portable setup, Spec Kit, project skills,
onboarding/provenance and CI wiring. Each package owns its checks; a draft PR
keeps dependencies visible.

If an upgrade fails, recover only task-owned changes in the dedicated worktree
against the recorded baseline. Do not reset personal installations or others'
changes. An unavailable external source leaves the old version with a documented
limitation; do not install an unknown replacement.

Remove the completed standalone plan only when acceptance evidence is reconciled.
Durable guidance remains in AGENTS.md, the AI guide and skills; Git history and
the closed issue preserve the implementation record. Closing the issue alone
does not turn an unchecked probe into a passed result.

## Sources

- [Codex project instructions](https://learn.chatgpt.com/docs/agent-configuration/agents-md)
- [Codex skills/discovery](https://learn.chatgpt.com/docs/build-skills)
- [Codex hooks/local trust](https://learn.chatgpt.com/docs/hooks)
- [Skill/instruction guidance used by the original audit](https://developers.openai.com/blog/rethinking-skills-and-prompts-for-gpt-6-astra)
- [GitHub Copilot configuration](https://docs.github.com/en/copilot/reference/customization-cheat-sheet)
- [Spec Kit 1.0.11](https://github.com/github/spec-kit/releases/tag/v1.0.11)
- [Compared community revision](https://github.com/mattpocock/skills/tree/c55ee46073ed923f86ce59a5eb3b6d895095d1b7)
- [Existing legacy/scope policy, PR #134](https://github.com/ahuelsmann/MOBAflow/pull/134)
- [Existing Sonar policy, PR #138](https://github.com/ahuelsmann/MOBAflow/pull/138)
