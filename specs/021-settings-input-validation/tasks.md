# Tasks: Validate Settings inputs

**GitHub Issue**: #470
**Spec Kit**: Required
**Input**: [spec.md](spec.md), [plan.md](plan.md)

## Specification

- [x] T001 Record the issue, approved behavior, field inventory, acceptance criteria and sequential delivery plan.
- [ ] T002 Run Spec Kit governance checks, verify the head-bound Sonar quality gate, obtain companion review and integrate this spec-only PR.

## Slice 1: Drafts, numeric safety, endpoints and photo folder

- [ ] T003 Write failing portable tests for invalid draft, valid correction, no-op, and Browse/Reset behavior (FR-001, FR-002, FR-009).
- [ ] T004 Cover the nine NumberBoxes with non-finite and overflow-before-conversion cases, including the feedback-count event. Enforce the raw-number guard before casts or writes (FR-006).
- [ ] T005 Test and implement `SettingsInputValidator`, display literal/UDP and REST port validation, and the fully qualified existing-directory rule. Preserve blank defaults and avoid network calls (FR-003–FR-005).
- [ ] T006 Wire drafts and English polite field errors, add missing accessible names/help text, and verify the Windows bindings (AC-01–AC-03).

## Slice 2: Z21, health checks and counters

- [ ] T007 Test and enforce the polling values 0 and 1–30. Set the control bounds to 0 and 30. Test the reconnect and health intervals and feedback-point range (FR-007).
- [ ] T008 Verify that invalid input leaves runtime and counter state alone and that accepted updates keep the existing behavior (AC-03, AC-04).

## Slice 3: Speech and Piper

- [ ] T009 Test and implement speech and Piper input rules, including optional/default paths, provider/voice choices, Browse and Reset (FR-008, FR-009).
- [ ] T010 Review the user wiki and record the Windows practical acceptance and head-bound technical evidence (AC-05).
