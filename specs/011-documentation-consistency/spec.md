# Feature Specification: Documentation consistency in pull requests

**Feature Directory**: `011-documentation-consistency`
**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/266
**Created**: 2026-10-13
**Revised**: 2026-10-21
**Status**: Subscription-based delivery; acceptance tracked in the source issue
**Spec Kit**: Required

## Governance and Traceability

**Affected Platforms**: Repository validation and developer/user documentation;
no product runtime changes.
**Out of Scope**: Paid API activation, a separate documentation bot, subscription
token export to CI, new automation, branch-protection administration, automatic
merges, product behavior changes and hardware acceptance.
**Sensitive Data**: Use existing approved native Codex/Claude sessions and the
repository's secrets policy. No credential inspection, transfer or deletion.
**Data and API Effects**: No product JSON, endpoints or protocol changes. Remove
the obsolete API/App publication path rather than activate or replace it.

The human decision on 2026-10-21 superseded API-specific live acceptance with
agent-assisted delivery and independent review in the existing issue/PR flow.
The earlier offline implementation is retained in Git history; no successful
live API acceptance is claimed.

## User Scenarios & Testing

### User Story 1 - Keep generated facts accurate (Priority: P1)

Package/reference or mutation configuration changes refresh source-derived
facts while preserving hand-authored purpose, licenses and surrounding prose.
Quality detects stale facts without model access or publication credentials.

**Independent Test**: Synthetic repositories cover active/unused references,
central-version upgrades, mutation thresholds, generator idempotency, malformed
input, path protections and adversarial package-purpose rows.

### User Story 2 - Deliver accurate developer and user documentation (Priority: P1)

The assigned Codex delivery owner assesses documentation impact against the
actual implementation, callers and host composition. Necessary developer and
canonical user-wiki corrections enter the same issue's leading PR.

**Acceptance Scenarios**:
1. Changed behavior contradicts a documented claim: correct every affected page.
2. The changed scope is already accurate: record the concrete reason for no change.
3. Evidence is missing: state the gap and retain owned follow-up; do not claim a pass.

### User Story 3 - Preserve independent review and verified completion (Priority: P1)

Chief Project Manager qualifies the complete draft handover and sets Ready.
The existing Claude plan independently reviews the exact comparison, including
documentation. Separate fixes receive fresh independent review. After merge,
confirm affected documentation against the actual integrated main commit.

**Independent Test**: A real assigned PR records affected pages or a justified
no-change result, current checks, independent review and exact-commit main
verification. Offline tests and green structural CI do not replace this acceptance.

## Requirements

- **FR-001**: Preserve deterministic package/mutation facts and their existing Quality gate.
- **FR-002**: Assess developer documentation and `docs/wiki/` for every integration,
  against current source, callers, configuration and actual base/head.
- **FR-003**: Keep corrections in the assigned issue's leading PR, with one writer
  per worktree and Documentation Lead input where relevant.
- **FR-004**: Record affected pages/corrections or a concrete no-change reason in
  the PR and complete Chief Project Manager handover.
- **FR-005**: Use the existing independent Claude plan after Chief Project Manager's
  Ready transition. Preserve current model, review-independence and quality requirements.
- **FR-006**: Reassess documentation after behavior-changing fixes and refresh
  affected evidence and independent review when the comparison changes.
- **FR-007**: Confirm affected documentation against actual integrated `main`;
  keep remaining drift owned and visible in the single central queue.
- **FR-008**: Remove the superseded API request, GitHub-App auto-commit and custom
  bot-check path. Do not create substitute credentials, automation or CI login wiring.
- **FR-009**: Use existing approved subscription sessions within their allowances;
  no automatic paid fallback, extra-usage purchase or new API budget.

## Success Criteria

- **SC-001**: Existing fact-generation and file-boundary regressions pass, including
  idempotency and preservation of licenses/purpose, with no model or network access.
- **SC-002**: A real leading PR demonstrates source-grounded developer/wiki impact,
  current CI/Sonar, independent review and verified integrated-main accuracy.
- **SC-003**: Missing evidence and unresolved drift remain explicit; neither
  structural success nor retired activation tasks become fictional semantic acceptance.
- **SC-004**: No obsolete API/App publication path remains and no existing required
  quality gate, branch protection or credential is weakened or deleted.

## Assumptions and limitations

Subscription authentication and available allowance are checked in the assigned
native-client phase, without exposing credentials. Limits may require waiting.
Semantic review is evidence-based assistance, not an automated correctness proof.
Chief Project Manager alone owns the central queue, Ready and verified overall
closure. No new required-check activation is necessary for this replacement.
