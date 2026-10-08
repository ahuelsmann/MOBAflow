# Implementation Plan: AI repository setup
**Branch**: `codex/issue-145-ai-repo-setup` | **Date**: 2026-09-25 | **Spec**: [spec.md](spec.md)
**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/145
**Spec Kit**: Required
**Status**: Implementing

## Summary
Implement P1-P7 from [the approved plan](../../plans/ai-repo-setup.md).
Use native file/search/web capabilities instead of redundant MCP servers.
Preserve Sonar remote result access, secrets scanning and local integration customizations.

## Technical Context
**Language/Version**: PowerShell 7, Python 3.11+ standard library (TOML/JSON validation), Markdown.
**Primary Dependencies**: Git, existing Sonar CLI, isolated Specify CLI 1.0.11 through uv.
**Storage/Protocols**: Development configuration and skill source records only.
**Testing**: Isolated subprocess fixtures, structural checks, fresh checkout probes, GitHub CI.
**Target Platforms**: Windows and Linux development; no product host changes.
**Affected Layers**: .codex, .agents, .specify, instructions, docs, scripts and instruction CI.
**Performance Goals**: Offline structural checks fit the existing five-minute instruction job.
**Data and API Effects**: No change to product JSON, defaults, APIs, Z21 or persisted layouts.
**Scale/Scope**: Ten managed Spec Kit skills, local audit skill, two new project skills.
Global settings, live railroad actions and repository administration changes are excluded.

## Constitution Check
Passed before research and after design against constitution 4.0.0, amended by P1.
- Architecture, EventBus, async, UI and data constraints: no product changes.
- Scope-based validation: isolated helper regression tests and structural checks.
- Traceability: Issue145 and specs/005-ai-repo-setup.
- Secrets: stricter session rule requires scans before reads; scan changed files before publication.
- Sonar: GitHub only; Draft until current-commit green and zero OPEN/CONFIRMED issues.
- No app or hardware launch.

## Project Structure
- .specify/templates/overrides and memory: validation policy.
- .specify/scripts/powershell and .agents/skills/speckit-*: pinned upstream, documented adaptations.
- scripts/Resolve-GitHubRepository.ps1: read-only remote selection.
- scripts/Test-AiRepositorySetup.py and .Tests.py: offline structural checks and fixtures.
- .agents/skills/sources.json: provenance referencing upstream manifests.
- docs/AI-DEVELOPMENT.md: setup, update, validation and pending gates.
- specs/005-ai-repo-setup/pipeline-review.md: timestamped CI review and follow-ups.

## Validation Strategy
Instruction consistency, governance self-tests and artifact checks, remote/config/hook fixtures,
skill validation, secrets, line endings and diff review. Temporary repositories only for tests.
Fresh-context agent probes stay open if isolated authentication is unavailable.
CI evidence belongs to the exact published commit; existing main findings remain separate.

## Remaining work (2026-10-08)

Main at `d45e14c5` contains P1 to P5 and P7 (PR #156, #157, #158). Open: T017 (record final CI/Sonar evidence) and
T019 (authenticated fresh-client probes), plus the disposition of CI-01 to CI-06. Drafted with the recommended
answers in spec.md Clarifications Q1 to Q5.

1. Write a probe checklist (`specs/005-ai-repo-setup/quickstart.md`) for the Q1 clients: fresh clone and worktree, skill discovery, instruction
   loading, review and diagnosis skill run, file access stays in the worktree, remote resolution with `origin`
   and `github`. No credentials in the repository.
2. Maintainer runs the probe (Q2); results go into `validation.md` (T019).
3. Record the latest `main` CI and Sonar results for the setup files in `validation.md` (T017).
4. Move CI-05/CI-06 to #197 and open one follow-up issue for CI-01 to CI-04 (Q3, Q4); update
   `pipeline-review.md` with the new owners.
5. Apply Q5 to `AGENTS.md` if accepted; run `scripts/Test-InstructionConsistency.ps1`.
6. Close #145 and delete `plans/ai-repo-setup.md` in the final documentation-only PR.

Independent of RF-23 (#191): no product code is touched. Overlap only with #197 (CI), resolved by Q3.

## Complexity Tracking
No exception. Python standard library avoids adding a TOML parser dependency.
Only the existing instruction CI job receives new checks; other pipeline findings become scoped follow-ups.
