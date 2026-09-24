# Validation: Remove route reservations

## Scope

Issue #146 removes route models, reservations, locks, release operations and automatic route signal effects. Direct turnout/signal commands, block observations, InPort counters and journey rules remain. No operator-owned layout file was read or modified. An explicitly authorized isolated Release copy was started with synthetic data, an invalid Z21 address, API autostart disabled and external Azure configuration removed from its process environment. No live railway connection or command was performed.

## Completed checks

- Portable Debug focused tests: 86 passed, zero failures/skips.
- Portable Release focused tests after runtime cancellation and existing markup-test adjustments: 95 passed, zero failures/skips. This includes ordered observations during pending dispatch, duplicate feedback, immutable snapshots, occupied/unknown blocks, activation isolation, cancellation after activation commit, disposal, page subscriptions, serialization, counters and journey event plans.
- Windows Release focused tests after independent review corrections: 98 passed, zero failures/skips. Additional cases retain early turnout confirmations, discard them on disconnect and preserve a dispatch error even when the physical position is observed.
- Final full portable Release suite after integrating #133: 1,673 passed, four skipped, zero failures (1,677 total). Earlier obsolete markup expectations were corrected. Skips cover the optional bundled-photo location and three local runtime-host integration scenarios; they are not counted as passes.
- Final full Windows Release suite after integrating #133: 1,731 passed, zero failures/skips, including normal WinUI/MOBApi dependencies.
- Portable Release analyzer rebuild compiled all 11 projects in the test graph, including MOBApi; zero compile errors. The portable baseline decreased from 4,247 to 4,112 diagnostics (1,329 to 1,310 groups), exclusively for removed/simplified reservation code and tests.
- Windows Release test graph rebuilt successfully with normal WinUI/MOBApi dependencies and analyzer checks. The Windows baseline matches 2,927 diagnostics in 1,016 groups after removal-only reductions. The local compile-only target excludes operator-owned `MOBAflow/solution.json` from output copying; `ValidateJsonConfiguration=false` excludes operator configuration validation. No project or workflow gate was changed.
- Android Release full consumer rebuild passed with zero errors and ten fresh SARIF files. Its baseline decreased from 1,947 to 1,909 diagnostics (664 remaining groups), exclusively from removed reservation code. No application deployment or start was performed.
- After integration, portable and Windows analyzer baselines match 4,037 diagnostics/1,280 groups and 2,879 diagnostics/1,001 groups respectively. The additional reductions belong to integrated #133. An incremental Android consumer compile passed; a complete rebuild is required to replace pre-integration SARIF reports for dependencies that were already compiled by the Windows run.
- Synthetic UI fixture round-tripped through the production Domain serializer and passed `InterlockingDefinitionValidator` with zero findings: one turnout, block and signal, four track segments and four signal-box elements; no locomotives, journeys or workflows. The existing invalid-IP startup guard, API autostart setting and Release configuration source were inspected before preparing isolated settings.
- Spec Kit governance, changed XML/JSON syntax and line-ending checks passed. Changed files passed deterministic secrets scans; repeat scans apply to subsequent edits.
- Independent runtime design review covered queue cancellation, FIFO publication, project replacement and disposal. Its event-code/snapshot race finding was corrected. Standards/specification reviews and their resolved findings are recorded in `analysis.md`.
- Main including #133 is integrated at `29f56176aa699ffd14a2b96c9b98a54a5c342f90`, producing feature commit `1c5198a887c90b3b92cbcdafe5ebf626072c84e0`. The ordered workflow action schema remains present and route definitions remain absent.

## Pending gates

- Complete Android rebuild and fresh integrated analyzer baseline comparison; no baseline expansion is authorized.
- Final residual-reference/diff/secrets checks and Draft PR publication.
- GitHub CI/SonarCloud for the published commit; zero OPEN/CONFIRMED PR findings before ready-for-review.
- Manual Light/Dark UI remains pending: the user authorized an isolated visual check of track-plan/signal-box selection, status/diagnostics and Info/Help with synthetic data and no Z21 connection or real commands. The exclusive desktop slot was provided and the isolated window was identified, but its first native access failed with `Computer Use app approval timed out`. No visual state has been checked. Coordination is awaiting the user's response before another native access. Live hardware remains unauthorized and untested. Operator layout configuration/packaging validation remains outside this task.
