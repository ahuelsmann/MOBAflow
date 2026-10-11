# Issue #35 Automation Testbench Readiness

**GitHub Issue**: #35

**Spec Kit**: Required

**Status**: Technical design proposed on the integrated runtime base; tasks/analysis and implementation remain.

## Integrated baseline

Rechecked on 2026-10-11 against `github/main` at `39312ca93413eef13fa6d7bb8360d5bf5fdda4ae`.
[#211](https://github.com/ahuelsmann/MOBAflow/pull/211) merged at 15:17:08Z;
[#207](https://github.com/ahuelsmann/MOBAflow/pull/207) merged at 16:18:47Z.
The project-runtime factory and persistent per-project counters/disposal changes are integrated.
Merged [#233](https://github.com/ahuelsmann/MOBAflow/pull/233) supplies the clarified requirements.

This is the single standalone readiness tracker for #35. The authoritative feature design is
[specs/010-automation-testbench/plan.md](../specs/010-automation-testbench/plan.md), with
[requirements](../specs/010-automation-testbench/spec.md),
[research](../specs/010-automation-testbench/research.md),
[data model](../specs/010-automation-testbench/data-model.md),
[runtime contract](../specs/010-automation-testbench/contracts/isolated-runtime.md),
[operator contract](../specs/010-automation-testbench/contracts/operator-ui.md) and
[validation guide](../specs/010-automation-testbench/quickstart.md).
Delete this standalone tracker after accepted feature completion; preserve Spec Kit artifacts and Git history.

## Gates

| Gate | Current evidence / next action |
| --- | --- |
| G1 | Early RF-01 through RF-05 readiness was resolved in the prior preparation audit; no old observation work is reopened |
| G2 | Integrated source uses ordered workflow actions and direct InPort/count event plans, not the historical graph/retry model |
| G3 | Isolated construction, effects, owned scheduling, full trace and comparisons are now concrete proposed contracts; review before implementation |
| G4 | Schema version 4 confirmed on integrated base; add scenario definitions directly without migrations, legacy paths or example solution |
| G5 | Dedicated codex task branch/worktree from verified main; pre-read and pre-publication secrets scanning required |
| G6 | Issue #35 remains the source; feature artifacts/draft design PR supply traceability; tasks belong to the next phase |
| G7 | Requirements and Phase 0/1 design exist; task generation and consistency analysis remain before implementation |
| G8 | Satisfied: #207 and #211 are merged; the existing live factory still needs the proposed isolated composition seam |

RF-24/#219 and #141 are not additional hard prerequisites for #35. Do not modify other sessions' branches.

## Scope and next steps

The MVP is a Windows scenario editor with platform-neutral execution/comparison. Reuse production journey/workflow/whistle
algorithms; provide private state and recording effects. Prove no transport/file/process/audio/display access, deterministic
current-instant scheduling and joined disposal before enabling UI execution. Target selection never filters active journeys.
Persist definitions through normal solution saving, never runtime traces/results. Andreas creates the example solution;
no real or shipped sample solution is read, created or edited by this work.

1. Review the technical design, generate dependency-ordered tasks and run cross-artifact consistency analysis.
2. Characterize existing behavior, introduce shared effects and prove production regressions.
3. Prove deterministic scheduling/joined cleanup and isolated composition with zero external calls.
4. Add runner/comparison, definition persistence and finally the page/awaited context-transition guard.
5. Validate each narrow draft PR through applicable suites/CI and authorized manual desktop acceptance.

The full delivery sequence, source ownership, risks and checks live in the linked feature plan; this tracker does not
duplicate them. These are planning artifacts, not an implementation or permission to launch MOBAflow/hardware.
Current validation is documentation diff/link/command review, secrets, line endings and Spec Kit governance.