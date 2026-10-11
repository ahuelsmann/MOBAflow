# Quickstart and Validation: Automation Testbench

**Source Issue**: #35
**Status**: Design validation guide. The feature and proposed new fixtures do not exist yet.

## Current planning change

Work in the dedicated task worktree for the relevant planning slice (currently codex/issue-35-tasks-analysis). Read AGENTS.md/applicable guidance with required
pre-read secrets scans. Use global.json for SDK selection. No app launch, hardware action or example solution is required.

For this documentation-only change: scan changed files, review diff/relative links and run the repository line-ending and
Spec Kit governance checks. PowerShell example from the worktree root:

```powershell
$featureDocs = @(git diff --name-only --diff-filter=ACMRT)
$featureDocs += @(git ls-files --others --exclude-standard -- specs/010-automation-testbench)
$featureDocs = @($featureDocs | Sort-Object -Unique)
foreach ($featureDoc in $featureDocs) { sonar analyze secrets $featureDoc }
./scripts/Test-LineEndings.ps1 -Path $featureDocs -Fix
./scripts/Test-LineEndings.ps1 -Path $featureDocs
./scripts/Test-SpecKitGovernance.ps1 -Mode PullRequest -ChangedFiles $featureDocs
```

Do not continue past a detected secret or interpret an unavailable scanner as a pass. Before committing, stage only this
feature's changed documents and run Test-LineEndings.ps1 -Staged. After commit, governance can compare against the recorded
integration base 39312ca93413eef13fa6d7bb8360d5bf5fdda4ae. Draft PR readiness requires current-head SonarCloud and zero
OPEN/CONFIRMED PR issues. Local Sonar code analysis is forbidden.

## After implementation tasks and consistency analysis

Use temporary test-owned project definitions, not the maintainer's real/example solution. New fixture names below are
proposed; do not run them before implementation or claim a zero-test filtered run as success.
Existing regression anchors are listed in [plan.md](plan.md).

```powershell
dotnet build MOBApi/MOBApi.csproj
dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0
dotnet restore MOBAflow/MOBAflow.csproj
dotnet build MOBAflow/MOBAflow.csproj -c FastDebug --no-restore -p:BuildMOBApiDependency=false -p:CopyMOBApiToOutput=false
dotnet test Test/Test.csproj -f net10.0-windows10.0.22621.0 -p:IncludeMobaSmartTests=false
```

These are future runtime/persistence/host-slice checks, not required for this planning PR.
For changed host integration, additionally build MOBAflow with normal dependencies and use Release/analyzer checks from
GitHub quality CI; FastDebug is only a compile check. Select named consumers for the actual slice and report failures/skips.
Focused filters must use an implemented fixture and discover/execute its intended cases.

## Acceptance scenarios

| Test-owned setup / action | Expected evidence / proposed fixture |
| --- | --- |
| One active journey, enabled InPort 1/count 12 target; seed 10; two accepted pulses | Count 12 and one invocation; inactive/disabled variants count normally without invoking. AutomationScenarioRunnerTests |
| Rapid pulse within counter filter; matching whistle rule | No second count/journey invocation; raw pulse still triggers/extends whistle. AutomationScenarioRunnerTests |
| All seven action families plus whistle; valid missing audio/script paths | Typed intents only; zero transport/file/probe/process/speaker/display calls. AutomationScenarioIsolationTests |
| Malformed action payload or stale target/stop ID | Rejected before effects or shared execution failure, distinct from missing external resource. AutomationScenarioValidationTests |
| Unselected journey explicitly active; omit another journey's initial state | Active journey reacts normally; omission invalid; selection never filters matching workflows. AutomationScenarioValidationTests |
| Second stop seeded at index 1; empty journey | Context index 2, no fake arrival; empty identity/index null with production fallback context 1. AutomationScenarioRunnerTests |
| Two same-time inputs, concurrent journeys, delays and whistle coalescing; ten runs at varied speed | Same state, normalized trace and results; explicit input order; source FIFO; no global serialization. AutomationScenarioDeterminismTests |
| Pause, then step through delayed action scheduling another delay | No wall-time progression; exactly next instant and all due descendants; future work remains. AutomationScenarioRunnerTests |
| Cancel during delay/pause/active whistle; restart | Cleanup joined, zero post-completion effects/timers; fresh state on restart. AutomationScenarioRunnerTests |
| Subscriber/detached-work fault or trace-capacity failure | Safe retained error/partial evidence, never Passed. AutomationScenarioRunnerTests |
| Positive predicate with one match plus unrelated outputs, duplicate match, inclusive range and absence | Default exact 1; duplicate fails; range/zero semantics as saved. AutomationScenarioComparisonTests |
| Ordered A(2),B(1): A,B,A versus A,A,B; zero slot and overlapping slot predicates | First fails, second passes; zero contributes no position; overlap cannot reuse an entry. AutomationScenarioComparisonTests |
| Secret-like script arguments, sensitive absolute paths and thrown exception | No raw arguments/hash/absolute path/exception in any generated output. AutomationScenarioIsolationTests |
| CRUD/deep duplicate/save/reopen in temporary directory | Definitions preserved; renewed owned IDs; no restored run state; no real/sample file touched. AutomationScenarioSerializationTests |
| Definition edits versus frequent runner notifications | Normal definition save behavior; zero run-triggered saves. AutomationTestbenchViewModelTests |
| Delayed/failed drain during navigation/back/select/add/delete/New/Load/shutdown | Old context/history retained until successful drain; direct setter cannot bypass it. AutomationTestbenchTransitionTests |

These scenarios map to FR-001 through FR-020 and SC-001 through SC-006, not executed outcomes. Include existing production
regressions for missing files, independent journeys, counters/persistence, host routing/disposal and architecture commands.
Acceptance cannot be satisfied by sorting traces, equality of whole live snapshots or mocks that omit an effect category.

## Authorized desktop acceptance

Only after explicit approval to launch MOBAflow: use test-owned definitions without a device connection. Create a scenario,
run/pause/step/cancel/restart, inspect first mismatch/full trace and confirm departure waits. Check Light/Dark/High Contrast,
keyboard, focus, Narrator, scaling and English isolated/synthetic labels. Builds/tests do not authorize launching the app.
No hardware power/movement, flashing, Android deployment or public release is part of #35's MVP validation.

## Next phase

Review [plan.md](plan.md), [data-model.md](data-model.md), [contracts](contracts/isolated-runtime.md) and [tasks.md](tasks.md).
Design #241 is merged. Task generation and read-only consistency analysis are complete as draft backlog work; integrate
the reviewed backlog before executing the narrow slices. No implementation/acceptance task is checked off by this documentation step.
CI/Sonar and future manual acceptance remain separate gates; this guide never claims them from document checks.
