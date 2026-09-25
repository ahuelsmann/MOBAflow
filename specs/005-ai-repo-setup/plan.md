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
Passed before research and after design against constitution 3.1.0, amended by P1.
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

## Complexity Tracking
No exception. Python standard library avoids adding a TOML parser dependency.
Only the existing instruction CI job receives new checks; other pipeline findings become scoped follow-ups.
