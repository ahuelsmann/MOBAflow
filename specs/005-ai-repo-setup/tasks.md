# Tasks: AI repository setup
**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/145
**Input**: [spec.md](spec.md), [plan.md](plan.md), [research.md](research.md)
**Status**: Implementing

## Phase 1: Setup and policy foundation
- [x] T001 Verify branch/worktree/main and scope in plans/ai-repo-setup.md (V1).
- [x] T002 Create specs/005-ai-repo-setup/spec.md, plan.md, research.md, data-model.md, quickstart.md and requirements checklist (V2-V4).
- [x] T003 Align .specify/memory/constitution.md and .specify/templates/overrides with AGENTS.md validation (P1; FR-001).

## Phase 2: US1 - Isolated work
Independent test: temporary roots and remote fixtures; documentation task selects structural validation.
- [x] T004 [US1] Correct source-linked examples and scope in .github/instructions/{auto-save-pattern,fluent-design,naming-conventions,self-explanatory-code-commenting}.instructions.md (P1; FR-001).
- [x] T005 [US1] Simplify .codex/config.toml and .mcp.json; make .codex/hooks.json and secrets hook portable (P2; FR-002, FR-003).
- [x] T006 [US1] Add negative and isolation fixtures in scripts/Test-AiRepositorySetup.Tests.py; implement scripts/Resolve-GitHubRepository.ps1 (P3; FR-005).
- [x] T007 [US1] Validate secrets-hook failure/missing tool/root behavior in scripts/Test-AiRepositorySetup.Tests.py (P2; FR-003).

## Phase 3: US2 - Skills and updates
Independent test: pinned integration, discoverable metadata, reviewed provenance and functional resolver.
- [x] T008 [US2] Upgrade .specify and managed skills to 1.0.11, preserve local common.ps1 adaptation and configure taskstoissues remote helper (P3; FR-004, FR-005).
- [x] T009 [US2] Add .agents/skills/mobaflow-code-review/SKILL.md and mobaflow-diagnosing-bugs/SKILL.md with source/license; narrow audit-code-quality (P4; FR-006).
- [x] T010 [US2] Register active skill provenance in .agents/skills/sources.json; document reference collection in skills/README.md (P5; FR-007).
- [x] T011 [US2] Add README/CONTRIBUTING links and docs/AI-DEVELOPMENT.md; update docs/SPEC-KIT.md (P5; FR-001, FR-004, FR-007).

## Phase 4: US3 - Validation evidence
Independent test: malformed fixtures fail offline; real setup passes; pipeline report identifies exact runs.
- [x] T012 [US3] Add negative structural fixtures in scripts/Test-AiRepositorySetup.Tests.py and implement scripts/Test-AiRepositorySetup.py (P6; FR-008).
- [x] T013 [US3] Add checks to existing .github/workflows/quality.yml instruction job (P6; FR-008).
- [x] T014 [US3] Record pipeline matrix, actual run evidence and scoped follow-up tasks in specs/005-ai-repo-setup/pipeline-review.md (P7; FR-009).
- [x] T015 [US3] Record fresh clone/worktree agent probes or access limits in specs/005-ai-repo-setup/validation.md (P6; FR-010).

## Final phase
- [x] T016 Run all relevant local checks from quickstart.md, secrets/line endings and final diff review; record validation.md (FR-010).
- [ ] T017 Publish Draft PR; record exact commit CI/Sonar results and open limits in validation.md and Issue145 (FR-010).
- [x] T018 Converge spec/plan/tasks and preserve plans/ai-repo-setup.md until outstanding acceptance is resolved (FR-001-FR-010).

## Dependencies and delivery
T001-T003 precede implementation. US1 precedes US2 integration; T014 research is independent.
T012 validates T004-T011; T013 follows passing offline checks; T015-T018 finish delivery.
No concurrent edits to shared files. Read-only pipeline and instruction research can run independently.
MVP is US1; each subsequent story has its own independent check.

## Phase 5: Convergence
- [ ] T019 Complete authenticated fresh-client documentation/review/diagnosis probes and hook activation on supported clients; record specs/005-ai-repo-setup/validation.md (SC-003, FR-006, FR-010). Supported clients are Codex CLI and Claude Code; the agent runs the Claude Code probe, the maintainer runs the Codex CLI probe from the checklist (T020); personal credentials are not copied.
- [x] T020 Write the probe checklist for Codex CLI and Claude Code in specs/005-ai-repo-setup/quickstart.md (Q1, Q2).
- [x] T021 Correct CI-01 in specs/005-ai-repo-setup/pipeline-review.md with the chronology (unprotected when observed on 2026-09-25, ruleset "main" created later that day) and record the owners of CI-02 to CI-06 established by T022 (Q3, Q4).
- [x] T022 Hand CI-05/CI-06 to #197 and open one follow-up issue for CI-02 to CI-04 plus a required-check name check after #197 (Q3).
- [x] T023 Make the AGENTS.md introduction neutral for every coding agent and run scripts/Test-InstructionConsistency.ps1 for validation (Q5).
- [ ] T024 Re-run the local secrets scan, line-ending checks (paths and -Staged), instruction consistency and setup checks on all files changed since T016 for validation; open the final documentation PR as draft with the Sonar quality gate (SonarCloud) green and zero open issues; record that commit's CI and Sonar results for T017; close #145 and delete plans/ai-repo-setup.md.
- [x] T025 Add the Claude Code entry points: CLAUDE.md importing AGENTS.md, the MOBAflow review and diagnosis skills under .claude/skills kept in sync with .agents/skills, the secrets hook in .claude/settings.json, and checks for them in scripts/Test-AiRepositorySetup.py (Q1).

Order in Phase 5: T020 and T025 before T019; T022 before T021; T024 last.
