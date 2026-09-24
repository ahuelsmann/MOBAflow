# Validation: Remove route reservations

## Scope

Issue #146 removes route models, reservations, locks, release operations and automatic route signal effects. Direct turnout/signal commands, block observations, InPort counters and journey rules remain. No operator-owned layout file was read or modified. No application or live railway was started.

## Completed checks

- Portable Debug focused tests: 86 passed, zero failures/skips.
- Portable Release focused tests after runtime cancellation and existing markup-test adjustments: 95 passed, zero failures/skips. This includes ordered observations during pending dispatch, duplicate feedback, immutable snapshots, occupied/unknown blocks, activation isolation, cancellation after activation commit, disposal, page subscriptions, serialization, counters and journey event plans.
- Windows Release focused tests after independent review corrections: 98 passed, zero failures/skips. Additional cases retain early turnout confirmations, discard them on disconnect and preserve a dispatch error even when the physical position is observed.
- Full portable Release suite initially found two obsolete markup expectations (interlocking product wording and route workbench fields). Both were corrected and passed in the focused run. The remaining 1,657 tests passed; four tests were skipped. A final full run remains required after integration.
- Portable Release analyzer rebuild compiled all 11 projects in the test graph, including MOBApi; zero compile errors. The portable baseline decreased from 4,247 to 4,112 diagnostics (1,329 to 1,310 groups), exclusively for removed/simplified reservation code and tests.
- Windows Release test graph rebuilt successfully with normal WinUI/MOBApi dependencies and analyzer checks. The Windows baseline matches 2,927 diagnostics in 1,016 groups after removal-only reductions. The local compile-only target excludes operator-owned `MOBAflow/solution.json` from output copying; `ValidateJsonConfiguration=false` excludes operator configuration validation. No project or workflow gate was changed.
- Android Release full consumer rebuild passed with zero errors and ten fresh SARIF files. Its baseline decreased from 1,947 to 1,909 diagnostics (664 remaining groups), exclusively from removed reservation code. No application deployment or start was performed.
- Synthetic UI fixture round-tripped through the production Domain serializer and passed `InterlockingDefinitionValidator` with zero findings: one turnout, block and signal, four track segments and four signal-box elements; no locomotives, journeys or workflows. The existing invalid-IP startup guard, API autostart setting and Release configuration source were inspected before preparing isolated settings.
- Spec Kit governance, changed XML/JSON syntax and line-ending checks passed. Changed files passed deterministic secrets scans; repeat scans apply to subsequent edits.
- Independent runtime design review covered queue cancellation, FIFO publication, project replacement and disposal. Its event-code/snapshot race finding was corrected. Standards/specification reviews and their resolved findings are recorded in `analysis.md`.

## Pending gates

- Integrated main containing #133 before final validation/publication.
- Final portable and Windows full suites and integration checks after main changes.
- Recheck required analyzer baselines after integration; baseline changes must only remove findings from deleted or simplified code.
- Final residual-reference/diff/secrets checks and Draft PR publication.
- GitHub CI/SonarCloud for the published commit; zero OPEN/CONFIRMED PR findings before ready-for-review.
- Manual Light/Dark UI remains pending the coordinated desktop slot; the user explicitly authorized an isolated visual check of track-plan/signal-box selection, status/diagnostics and Info/Help with synthetic data and no Z21 connection or real commands. Live hardware remains unauthorized and untested. Operator layout configuration/packaging validation remains outside this task.
