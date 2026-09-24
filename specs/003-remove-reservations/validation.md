# Validation: Remove route reservations

## Scope

Issue #146 removes route models, reservations, locks, release operations and automatic route signal effects. Direct turnout/signal commands, block observations, InPort counters and journey rules remain. No operator-owned layout file was read or modified. No application or live railway was started.

## Completed checks

- Portable Debug focused tests: 86 passed, zero failures/skips.
- Portable Release focused tests after runtime cancellation and existing markup-test adjustments: 95 passed, zero failures/skips. This includes ordered observations during pending dispatch, duplicate feedback, immutable snapshots, occupied/unknown blocks, activation isolation, cancellation after activation commit, disposal, page subscriptions, serialization, counters and journey event plans.
- Full portable Release suite initially found two obsolete markup expectations (interlocking product wording and route workbench fields). Both were corrected and passed in the focused run. The remaining 1,657 tests passed; four tests were skipped. A final full run remains required after integration.
- Portable Release analyzer rebuild compiled all 11 projects in the test graph, including MOBApi; zero compile errors. Analyzer differences are being checked before baseline refresh; no new diagnostic allowance is authorized.
- Spec Kit governance, changed XML/JSON syntax and line-ending checks passed. Changed files passed deterministic secrets scans; repeat scans apply to subsequent edits.
- Independent runtime design review covered queue cancellation, FIFO publication, project replacement and disposal. Its event-code/snapshot race finding was corrected.

## Pending gates

- Integrated main containing #133 before final validation/publication.
- Final portable and Windows full suites; Windows/Android consumer builds in coordinated host slots.
- Fresh complete analyzer comparison for each required target; baseline changes must only remove findings from deleted or simplified code.
- Final two-axis code review, residual-reference/diff/secrets checks and Draft PR publication.
- GitHub CI/SonarCloud for the published commit; zero OPEN/CONFIRMED PR findings before ready-for-review.
- Manual Light/Dark UI and real hardware acceptance are not performed. Build checks do not authorize starting MOBAflow. Operator layout configuration/packaging validation remains outside this compile-only task.
