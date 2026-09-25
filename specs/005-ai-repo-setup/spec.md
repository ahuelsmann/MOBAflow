# Feature Specification: AI repository setup
**Feature Branch**: `codex/issue-145-ai-repo-setup`
**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/145
**Spec Kit**: Required
**Created**: 2026-09-25
**Status**: Approved for implementation
**Input**: Implement the agreed repository setup plan, including GitHub pipeline review.

## Governance and Traceability
Source: Issue145 and the approved plans/ai-repo-setup.md. This is development tooling and guidance work.
Product JSON, configuration defaults, public APIs, Z21 behavior and persisted layouts stay unchanged.
Windows and Linux development checks apply; no UI, application launch or hardware checks are needed.
Follow AGENTS.md and constitution 3.1.0; local evidence, agent probes and current-commit CI remain separate.

## User Scenarios & Testing

### User Story 1 - Reliable isolated work (Priority: P1)
A contributor opens a fresh clone or separate worktree and gets consistent project guidance and tools without personal skill installations.
**Why this priority**: Wrong-directory access can affect unrelated work.
**Independent Test**: Two temporary repositories with distinct markers resolve their own roots and GitHub destinations.
**Acceptance Scenarios**:
1. Given a fresh clone, when a contributor follows the README, then active skills and required tools are discoverable.
2. Given two worktrees, when development helpers run, then each uses its own root and never silently falls back to the primary checkout.
3. Given a documentation task, when guidance selects validation, then structural checks suffice without starting product builds.

### User Story 2 - Maintainable skills and updates (Priority: P2)
A maintainer can update the pinned workflow integration and identify every active skill's source and local changes.
**Why this priority**: Untracked customizations break future updates.
**Independent Test**: Inspect integration status and run remote-resolution fixtures without creating GitHub issues.
**Acceptance Scenarios**:
1. Given the approved update target, when integration is refreshed, then local policy overrides remain intact and managed modifications are explained.
2. Given a review or diagnosis request, when the corresponding project skill is used, then it stays within the requested scope and repository launch/hardware rules.
3. Given ambiguous remotes, when issue destination resolution runs, then it fails visibly rather than selecting an arbitrary repository.

### User Story 3 - Understand validation results (Priority: P2)
A contributor can tell which local and GitHub checks ran and which acceptance evidence remains open.
**Why this priority**: Pending or skipped CI must not be mistaken for success.
**Independent Test**: Trace the documented pipeline matrix to workflow definitions and actual runs.
**Acceptance Scenarios**:
1. Given a PR commit, when checks are reported, then success, failure, skip and pending states remain distinct.
2. Given a long-running Android job, when investigated, then timings and logs distinguish runner wait from actual build work.
3. Given missing branch enforcement, when reviewed, then the gap has an explicit follow-up without silently changing repository administration.

### Edge Cases
- Missing CLI, unavailable credentials, untrusted project, unsupported OS: explicit limitation, no false pass.
- No remote, non-GitHub tracking remote, ambiguous GitHub remotes, SSH URLs, differently named remotes.
- Missing skill reference, duplicate skill name, malformed configuration and absent source metadata.
- Global scan instructions can be stricter than repository guidance and retain precedence.

## Requirements

### Functional Requirements
- **FR-001**: Active guidance MUST select checks by change scope and platform with one shared policy source.
- **FR-002**: Repository tool configuration MUST avoid machine-specific checkout paths and unneeded servers.
- **FR-003**: The secrets hook MUST resolve its own worktree and expose missing-tool/failure states without outputting secrets.
- **FR-004**: The workflow integration MUST use the approved 1.0.11 release with documented local customizations.
- **FR-005**: Issue destination resolution MUST prefer a GitHub tracking remote, otherwise require one unambiguous GitHub remote.
- **FR-006**: Two distinctly named project review/diagnosis skills MUST be available in a fresh clone.
- **FR-007**: All active skills MUST have traceable sources, versions, licenses and local-change records.
- **FR-008**: Structural validation MUST detect missing links, duplicate names, malformed configuration and missing provenance offline.
- **FR-009**: GitHub pipeline review MUST cover triggers, required checks, runner/timing behavior, versions, caches, permissions and Android bundle validation.
- **FR-010**: Delivery MUST keep local validation, agent probes and current-commit CI/Sonar evidence separate.

### Key Entities
- Active skill: unique name, path, origin, fixed revision, license, local changes.
- Validation evidence: scope, command/run, commit, outcome, limitation.
- Repository destination: GitHub owner/repository derived from the active checkout.

## Success Criteria
- **SC-001**: All active skills pass structural checks and have resolvable source records.
- **SC-002**: Isolation and remote-resolution fixtures pass for two independent working roots and reject ambiguous destinations.
- **SC-003**: Documentation, review and simulated diagnosis each have a recorded fresh-context probe or explicit outstanding access limitation.
- **SC-004**: Every workflow and required-check gap has an evidence-backed disposition; current-commit CI is reported accurately.

## Assumptions
The approved plan determines scope. No product behavior, persisted data, APIs, Z21 behavior, layout or startup defaults change.
No app start, hardware use, global tool replacement or branch-protection mutation is required.
Windows and Linux development guidance is supported; other clients/platforms need separate verified evidence.
