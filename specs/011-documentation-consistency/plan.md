# Implementation Plan: Documentation consistency

**Branch**: `codex/documentation-consistency` | **Date**: 2026-10-13 | **Spec**: [spec.md](spec.md)
**GitHub Issue**: #266
**Spec Kit**: Required
**Status**: Implemented offline; live activation pending

## Summary

Use Python standard-library controls for deterministic source facts, bounded
structured Responses API output, independent verification and guarded publication.
No new .NET dependencies or product changes. Quality runs generators and offline
regressions. A separate, initially inactive target-event workflow reads trusted
base controls and treats the head checkout strictly as data.

## Constitution Check

- Product layers, EventBus, DI, runtime behavior and UI themes are unaffected.
- Work uses an isolated task worktree and draft PR, with no application/hardware launch.
- Tests use synthetic repositories, fake model replies and fake GitHub responses.
- API credentials and publication credentials are separate; the model receives no tools.
- Spec/plan/tasks reference #266; feature records remain under this directory.
- Changed files are scanned and normalized; Sonar runs only in PR CI.
- Draft creation follows the maintainer's existing exception for missing local Sonar
  access. Current-head Sonar and zero open/confirmed findings remain review gates.

## Design and analysis

1. `documentation_consistency.py` owns central-version/reference inventory, active
   mutation configuration facts, path boundaries, source context and bounded edits.
2. `run_documentation_consistency.py` separates assessment, publication and reporting.
   Two independent API requests use strict JSON schemas, no tool access and no stored
   response. Gitleaks scans context before submission and proposals before publication.
3. `documentation-consistency.yml` executes base controls, uploads data-only results,
   then acquires a contents-write App token in a separate publishing job. The normal
   job token reports a check attached to the event head, not the target branch.
4. `Quality` continuously checks generated facts and offline regressions. Generated
   tables have explicit markers; AI cannot edit them or change license prose.
5. Avoid persistent bot state: inspect the latest commit's paths to stop another
   auto-update after a documentation-only commit. Manual correction is the fallback.

Official references reviewed on 2026-10-13:
- [Structured outputs](https://developers.openai.com/api/docs/guides/structured-outputs)
- [GitHub target events](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows)
- [GitHub token event behavior](https://docs.github.com/en/actions/concepts/security/github_token)
- [Check runs](https://docs.github.com/en/rest/checks/runs)

## Validation Strategy

- Run `python scripts/Test-DocumentationConsistency.py` and the facts checker.
- Parse workflows and lint with the same pinned actionlint version used by Quality.
- Run instruction consistency, documentation links, Spec Kit governance, changed/staged
  line endings and deterministic changed-file secrets scanning.
- Smoke-build the source context against the real task diff without calling the API.
- No .NET build validates these standalone Python/workflow boundaries; the existing
  product CI jobs remain enabled for the script/workflow change.
- Live acceptance must verify API access/model support, App permissions, one justified
  auto-commit, no loop, and rerun Quality/Sonar on the updated head before activation.
