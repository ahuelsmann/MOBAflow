# Analysis and convergence: The Piper timeout covers the whole run

**GitHub Issue**: #391

**Chronology**: Written on 2026-10-24 after the implementation, against PR #395 head `941806e6` over main `f2f388d3`.
It is a post-implementation consistency check, not an earlier `/analyze` run.

## Consistency of spec, plan and tasks

| Requirement | Plan step | Task | Evidence |
| --- | --- | --- | --- |
| FR-001 timeout covers the whole run | 1, 2 | T002 | `ProcessThatNeverReadsTheText_TimesOutWhileTheTextIsWritten` (hung until its 20 s guard on the previous code) |
| FR-002 kill on timeout or cancellation | 2 | T002 | same test ends; `CallerCancellation_WhileTheTextIsWritten_Throws` |
| FR-003 unchanged results | 3 | T002 | timeout message asserted; `ProcessThatReadsTheText_ReturnsItsOutput` |

Every requirement has a task and evidence; `ProbeAsync` shares `RunAsync` and is covered by the same code path.

## Convergence (current state)

- **Implemented and verified locally on `941806e6`:**
  - Windows suite 3135 passed, with a hang guard;
  - analyzer gates match; the only addition is one Windows CA1812 entry for the new fixture;
  - lane coverage passed (268 registered); mutation guard self-tests passed (40 checks).
- **CI on `941806e6`:** all checks green, SonarCloud zero OPEN/CONFIRMED issues.
- **Converged.** No practical acceptance gate is defined; T006 is an optional check with a configured Piper.
- **Overlap:** #392 also adds one entry to `quality/analyzer-baseline.windows.json`; whichever integrates second merges
  main once.
