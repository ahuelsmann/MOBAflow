# Feature Specification: Documentation consistency in pull requests

**Feature Directory**: `011-documentation-consistency`
**Source Issue**: #266
**Created**: 2026-10-13
**Status**: Implemented offline; live activation pending
**Spec Kit**: Required

## Governance and Traceability

**Affected Platforms**: GitHub CI and developer documentation; no product runtime changes.
**Out of Scope**: Automatic merges, branch-protection administration, paid API activation,
live hardware, product behavior changes and maintenance of unrelated historical prose.
**Sensitive Data**: OpenAI API key and GitHub App private key stay in GitHub secrets;
the model receives scanned repository data, never credentials or execution tools.
**Data and API Effects**: No product JSON, endpoints or protocol changes. New CI variables,
secrets, structured assessment artifacts and a head-specific GitHub check.

## User Scenarios & Testing

### User Story 1 - Keep generated facts accurate (Priority: P1)

A package or mutation configuration update refreshes the corresponding documentation.
Quality detects stale generated facts even when AI maintenance is not activated.

**Independent Test**: Synthetic repositories cover active/unused references, upgrades,
mutation threshold changes, missing central versions and generator idempotency.

### User Story 2 - Update semantic documentation in the existing PR (Priority: P1)

For an activated, trusted same-repository PR, compare the complete PR diff and
current implementation/caller/host evidence with relevant existing documentation.
Verified updates become one documentation-only commit on that PR's branch.
An independent model request checks the candidate without the writer's reasoning.

**Acceptance Scenarios**:
1. A behavior change needs a unique, source-grounded replacement: publish it in the PR.
2. The changed scope is already accurately described: do not create a commit.
3. Evidence is missing or verification contradicts the writer: fail the check visibly.

### User Story 3 - Preserve publication boundaries (Priority: P1)

Controls execute from the base branch. PR scripts, workflows and instructions are
input data, never executable control logic. A separate job receives the scoped
writer credential only after the proposal is verified.

**Independent Test**: Reject source/instruction edits, traversal, symlinks, missing
evidence, duplicate passages, generated-region edits, stale heads and fork publication.

## Requirements

- **FR-001**: Always enforce deterministic facts in Quality; generate only source-derived
  values and preserve hand-authored purpose, license and surrounding prose.
- **FR-002**: When activated, assess every opened/synchronized/reopened/ready PR against
  its complete base-to-head diff. Unknown or oversized context must not silently pass.
- **FR-003**: AI has no tools. Changes require exact source quotes and unique existing
  passages in allowed Markdown files. Agent policies, specifications and code are excluded.
- **FR-004**: Independently verify both updated and unchanged semantic documentation.
- **FR-005**: Before publication, recheck PR head/base, document hashes, allowed paths,
  secrets, links and generated facts. Use a push lease against the assessed head.
- **FR-006**: Use a repository-scoped GitHub App token for pushes so existing checks
  rerun. Never merge or weaken CI/Sonar gates automatically.
- **FR-007**: A documentation-only latest commit cannot trigger another autofix. Remaining
  findings need manual correction; this bounds rewriting after the bot's first commit.
- **FR-008**: Report on the exact event head with a custom check, since target-event job
  checks are not a substitute for a check on the PR commit. Superseded heads remain separate.
- **FR-009**: Default AI activation is off. Report that explicitly, not as semantic success.
  Enabled-but-incomplete configuration fails. Fork/untrusted PRs require maintainer review.

## Success Criteria

- **SC-001**: Offline regressions establish generator stability and rejection of unsafe,
  ungrounded, ambiguous, stale and recursively generated changes.
- **SC-002**: A live acceptance PR gains only justified documentation edits, then passes
  documentation, Quality and Sonar checks on its current head before review.
- **SC-003**: API refusals/errors, scanner failures, missing configuration and independent
  review failures cannot produce a successful semantic check.

## Assumptions and limitations

GitHub App installation and API model/budget configuration require maintainer setup.
The required-check rule is configured after live acceptance, with branches required
to be up to date. AI consistency is evidence-based assistance, not a correctness proof.
The context limit is one megabyte and model output is capped at 8,000 tokens per request;
large changes require manual review. Existing-document replacements are supported;
new pages and fork write-back remain manual.
