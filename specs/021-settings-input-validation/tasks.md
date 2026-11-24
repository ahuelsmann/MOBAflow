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
- [ ] T006 Wire field-local errors and drafts in `MOBAflow/View/SettingsPage.xaml`. Exercise `NumberBox.ValidationMode`, add distinct names and help text for REST API port, photo path and Browse, and verify Windows bindings plus #471 and theme preservation on reset. Run head-bound Sonar, analyzer and secrets checks (AC-01–AC-03).

## Slice 2: Z21, health checks and counters

- [ ] T007 Test and enforce the polling values 0 and 1–30. Set the polling control bounds to 0 and 30. Test retry, timer-safe health and allocation-safe counter boundaries. Check accessible names, help text and field-local errors for these controls (FR-007).
- [ ] T008 Verify invalid input preserves runtime and counter state, and accepted updates and reset preserve the existing behavior. Verify the SharedUI mutation lane and run head-bound Sonar, analyzer and secrets checks (AC-03, AC-04).

## Slice 3: Speech and Piper

- [ ] T009 Test and implement speech and Piper input rules, including default voice, test text, provider/voice choices, Browse and Reset. Put the three path errors below the rows and give the Browse buttons distinct accessible names and useful help text (FR-002, FR-008, FR-009).
- [ ] T010 Review the user wiki, capture Windows practical acceptance and verify #471 and theme behavior after reset. Record head-bound Sonar, analyzer, secrets and applicable SharedUI mutation results (AC-05).
