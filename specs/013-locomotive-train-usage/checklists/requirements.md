# Specification Quality Checklist: Locomotive usage in train consists

**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/306

**Purpose**: Check requirements and design completeness, not executed product behavior.

**Created**: 2026-10-15

**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] User scenarios describe operator value and exclude implementation bodies.
- [x] Required governance, scope, data effects and platform sections completed.
- [x] Detailed API/locking choices live in plan/contracts, not the user stories.
- [x] Existing use is distinguished from a broken reference and future Delete policy.

## Requirement Completeness

- [x] No unresolved in-scope clarification marker remains; Brain A/B/C and read-only decisions recorded.
- [x] Requirements and source-specific zero/unknown outcomes are testable.
- [x] Success criteria are measurable through observable results and recording fakes.
- [x] Repeated occurrences, foreign IDs/types, missing targets and invalid identity cases identified.
- [x] Same-ID owner replacement, disposal after success, definition change and in-place reload covered.
- [x] Unchecked B/C coverage cannot become global non-use or delete permission.
- [x] Dependencies/default assumptions and future implementation authorization are explicit.

## Feature Readiness

- [x] All 13 functional requirements and 6 success criteria map to future tasks.
- [x] Existing validators/diagnostics/DI are reused rather than comprehensively reorganized.
- [x] Actual runtime A capture and pre-apply revalidation are concrete planned additions, not claimed existing APIs.
- [x] Backend owner/query and SharedUI projection responsibilities are separated.
- [x] No persisted schema/config/layout or remote-protocol change is planned.
- [x] Product tasks remain unchecked; no product test execution/implementation claim.

## Notes

This checklist records design review only. Planning validation outcomes and the cross-artifact report are reported separately after running their checks. Future warning/blocking policy and atomic Delete coordination are deliberately out of scope, not requirements silently left open.
