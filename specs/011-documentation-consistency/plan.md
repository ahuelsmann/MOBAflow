# Implementation Plan: Documentation consistency

**Branch**: `codex/issue-266-subscription-documentation-workflow`
**Date**: 2026-10-21
**Spec**: [spec.md](spec.md)
**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/266
**Spec Kit**: Required
**Status**: Subscription-based delivery; acceptance tracked in the source issue

## Summary

Retain Python standard-library generation and validation of package/mutation
facts. Replace the separately activated API/App documentation bot with the
existing assigned Codex delivery, independent Claude plan and verified-main
handover. Necessary documentation and canonical wiki corrections belong in the
same issue's leading PR. No extra provider budget, credentials or automation.

## Constitution Check

- Product layers, EventBus, DI, runtime, UI, persistence and protocols are unchanged.
- Use the existing dedicated task worktree, one writer and one leading draft PR.
- Preserve deterministic facts, file guards, relevant regressions and current CI/Sonar.
- No application, device, hardware, provider request or subscription login in CI.
- Chief Project Manager alone qualifies Ready; the existing independent Claude plan
  owns review, separate fixes, re-review and SHA-bound squash merge.
- Preserve unrelated work, credentials and historical evidence; distinguish retired
  activation requirements from successful real acceptance.

## Design and analysis

1. Keep `documentation_consistency.py`'s source-derived facts, CLI and file guards.
   Remove helpers used only by the retired AI proposal/publication path.
2. Remove `run_documentation_consistency.py`, its model prompts and the privileged
   target-event documentation workflow. Remove tests for that deleted behavior;
   retain the ten applicable fact-generation/file-boundary regressions unchanged.
3. Preserve Quality's existing generator and offline-test entry points without
   changing SDK, package, build, baseline or mutation configuration files.
4. Update the documentation guide, AI development reference and PR impact template:
   source-grounded developer/wiki assessment, same-PR corrections, complete handover,
   independent review, reassessment after fixes and actual-main confirmation.
5. Record T007's new real-PR acceptance and T008's existing deterministic gates.
   Do not add a required API-bot check or administer branch protection.

The earlier API design was superseded by the explicit human decision on
2026-10-21. Its offline evidence remains historical. No API acceptance, provider
configuration or App auto-commit is reported as performed.

## Validation Strategy

- Run `scripts/Test-DocumentationConsistency.py` and the deterministic facts checker.
- Review removal of API/publication callers and stale configuration references.
- Run instruction consistency, Spec Kit governance and internal Markdown-link checks.
- Run changed/staged line-ending, diff and deterministic secrets checks.
- Keep current-head PR CI/Sonar with zero OPEN/CONFIRMED PR findings; no product
  build/test is required locally for these Python/docs/workflow boundaries.
- T007 requires a real PR's documented developer/wiki impact, independent review
  in the existing Claude plan and actual integrated-main verification.
- T008 confirms effective existing deterministic gates; the retired custom bot
  check is not required and no ruleset change is part of the implementation.
- Record final exact-commit acceptance, limitations and closure in the source issue.
