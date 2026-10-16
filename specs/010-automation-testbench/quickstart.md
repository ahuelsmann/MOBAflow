# Quickstart and Validation: Automation Testbench

**Source Issue**: #35
**Status**: First foundation slice implemented; the scenario runner and desktop feature remain unimplemented.

## Current main synchronization (2026-10-16)

Merge `7f67c990` integrates main `fb63e8fde5f6aa299a3f3478560e7277f8661a90` (#301, typed runtime hub contract) after the
earlier conflict-free merge `93b34020` of main `6888b0d9` (#300, RF-28 slices 1 and 2). Both merges preserve history and
were conflict-free; RF-28's `TrackPlan.Editor` project and #301's `RuntimeHubContract` are taken unchanged. Against main
the PR still changes the same 19 foundation, test and document files. Its only baseline changes remain the
WorkflowActionHandlers reductions in the portable and Android baselines. Both review corrections below are retained.
Local evidence on merge `7f67c990`, in this order:

- Android Release `MOBAsmart` `-t:Rebuild -m:1` analyzer graph: 0 errors, all 11 SARIF files rewritten (including
  `TrackPlan.Editor`); the strict guard matches 1,522 diagnostics in 556 groups. This is an analyzer/compile check,
  not a Release AAB, device or hardware acceptance.
- Portable Release `-t:Rebuild -m:1`: 0 errors, 13 fresh SARIF files; guard matches 3,248 diagnostics in 1,133 groups.
- Windows Release `--no-incremental`: 0 errors, 4 fresh SARIF files; guard matches 2,471 diagnostics in 947 groups, with
  no PR288 change to the Windows baseline.
- The focused fixtures below plus whistle, Z21, journey, every `*ArchitectureTests` fixture and the in-process typed hub
  boundary fixtures `RuntimeCommandAdmissionTests` and `SolutionControllerTests`: **289 tests passed, 0 failures, 0 skips**
  on both Release `net10.0` and `net10.0-windows10.0.22621.0`; both review-correction tests ran explicitly (8 of 8 cases
  on each target). Process-starting `MobApiProcessTests` and `RuntimeHubLiveE2ETests` were not run.

The same analyzer counts were measured on the `6888b0d9` merge. Current-head CI and SonarCloud results are pending after
the push and are not claimed here.
The two sections below are the earlier, superseded evidence for main `6e7c6116`.

## Main synchronization (2026-10-15)

PR #288 retains its published foundation commit `7e7c26bb` and the earlier merge `4371fc2f` of main `d4d3d2b1`, then
merges main `6e7c6116551b3abae547aa7499d3a861e0f2ca85` (#281, #267, #296, #295) without rewriting history.
Both merges were conflict-free. Against main the code and test diff is still the original foundation; the only baseline
changes are the effect-boundary reductions CA1062 12 to 8, CA1848 12 to 3 and CA1873 6 to 0 for WorkflowActionHandlers,
now applied to both the portable and the Android baseline.
No additional product behavior or backlog task is introduced by synchronization.

Local evidence on the merged tree (SDK 10.0.402 with installed workloads; no workload-resolver workaround was needed):

- Portable Release analyzer rebuild: baseline matches 3,320 diagnostics in 1,154 groups (main 3,339/1,155 minus those reductions).
- Windows Release analyzer build with `--no-incremental`: 2,493 diagnostics in 953 groups, unchanged from main.
- The focused fixtures below plus every `*ArchitectureTests` fixture: **232 tests passed, 0 failures, 0 skips** on both
  Release `net10.0` and `net10.0-windows10.0.22621.0`.
- Named Backend, SharedUI and MOBApi Debug builds had 0 warnings/errors; MOBAflow compiled in the Windows Release test graph.
  MOBAsmart compiled with `-c FastDebug` (0 errors, 45 existing warnings). This is a compile check, not a Release AAB.

Android analyzer baseline: the manual-only Android job reads the whole `artifacts/analyzers/Release` root, which includes
Backend `net10.0`, but `quality/analyzer-baseline.android.json` still held the pre-change counts 12/12/6. A fresh
`MOBAsmart/MOBAsmart.csproj -f net10.0-android -c Release -t:Rebuild -m:1 -p:MobaAnalyzerGate=true` build (0 errors, empty
SARIF root) made the strict guard report exactly those three decreases and no other mismatch. Only those reductions were
applied (CA1062 12 to 8, CA1848 12 to 3, CA1873 group removed); the guard then matched 1,577 diagnostics in 573 groups.
This is an analyzer/compile check, not a Release AAB, device or hardware acceptance.
Current-head CI/Sonar evidence is recorded in the PR after execution; old green checks are not reused.

## Review corrections (2026-10-15)

An independent review of `6dad974e` found that the filtered-counter whistle regression drove a mocked `IZ21` and published
a separate bus event by hand, so it did not connect the counter, raw feedback and whistle. It now feeds two real R-Bus
activations (with a release between them) through `FakeUdpClientWrapper` and `Z21` into one shared `EventBus`.
`InPortCounterService` and `LocomotiveWhistleAutomationService` share a `ManualTimeProvider`: the count stays 1 under the
timer filter, both raw `FeedbackReceivedEvent`s reach the bus, and each activation produces its own whistle on/off pair.
The planner no longer attaches a context-free announcement request; announcements, like journey-stop transitions, have a
null plan `Request` because station, index and rendered text exist only in the owning invocation. Handlers and the
production adapter are unchanged.

Fresh local evidence for the corrected tree, in this order:

- Android Release `-t:Rebuild -m:1` analyzer graph: 0 errors, 11 SARIF files; guard matches 1,577 diagnostics in 573 groups.
- Portable Release rebuild: guard matches 3,320 diagnostics in 1,154 groups. The first build found CA2000 and NUnit2045 in
  the new test; both were fixed in source, not accepted into the baseline.
- Windows Release `--no-incremental` build: guard matches 2,493 diagnostics in 953 groups.
- Focused whistle, Z21, counter, planner, handler, workflow, DI, end-to-end and architecture fixtures:
  **261 tests passed, 0 failures, 0 skips** on both Release `net10.0` and `net10.0-windows10.0.22621.0`.
  Backend, SharedUI, MOBApi and MOBAflow compiled in these Release graphs.

## Foundation slice evidence (2026-10-14/15)

Dedicated branch `codex/issue-35-automation-testbench`, base `7c1d92422c0cbf675fddc03a8979990957733d15`.
GitHub #241 and #245 are merged; all 16 requirements-quality checklist items are checked. No extension hooks exist.
T001-T004 and T009-T010 cover revalidation, production characterization and the shared effect boundary.
T005-T008 model contracts remain a separate foundation slice before any executable private graph.

Existing handlers now send typed, validated execution requests through `IWorkflowEffectAdapter`.
`ProductionWorkflowEffectAdapter` owns file probes, process/audio/speech and Z21 output. Pure signal/template resolution
and journey stop transitions remain shared; the announcement handler builds its request from the invocation context. Serialized plans exclude executable requests and script formatting omits
arguments/paths. This does not implement the future safe trace or an isolated factory.

Final local verification on 2026-10-15: portable Release run discovered/executed **169 tests, 0 failures, 0 skips**.
The complete portable Release analyzer build passed; the strict baseline matches 3,324 diagnostics in 1,152 groups.
Only three proven reductions in WorkflowActionHandlers were updated (CA1062 12 to 8, CA1848 12 to 3, CA1873 6 to 0);
there are no new/increased accepted diagnostics, rule changes or threshold changes. Existing diagnostics remain tracked.
Current-head CI/Sonar results are recorded in the draft PR and remain separate from this local evidence.
The 46-test pre-change anchor run passed. The new isolation tests first failed compilation on the absent adapter contract.
Named Debug builds of `SharedUI/SharedUI.csproj` and `MOBApi/MOBApi.csproj` passed with zero warnings/errors.
The initial SDK workload resolver failed with MSB4242 (missing .NET 10.0.402 workload manifests). Portable checks used
`MSBuildEnableWorkloadResolver=false` for that process only. This is not Windows or Android validation.

| Production boundary | Executed fixture mapping |
| --- | --- |
| Shared payloads, effect requests, missing audio/script files, signal mapping and unconfigured display | WorkflowEffectPlannerTests, WorkflowActionHandlersTests, ActionExecutorTests, MultiplexerCommandResolverTests |
| Ordered/awaited actions, stop-change context and cancellation | WorkflowSequenceExecutionTests, WorkflowServiceTests, WorkflowExecutionCoordinatorTests |
| Source-local FIFO, context built after predecessor, other journey remains independent | WorkflowExecutionCoordinatorTests, JourneyEventPlanTests, JourneyEventPlanAcceptanceTests |
| Seeds/persistence/filtering and independent counts | InPortCounterServiceTests, PersistentInPortCounterTests |
| Real R-Bus receive path: filtered count with raw feedback and whistle per activation; coalescing/function-off | LocomotiveWhistleAutomationServiceTests |
| Real DI override/idempotence and fake Z21 transport chain | MobaBackendServiceCollectionExtensionsTests, WorkflowExecutionEndToEndTests |

The full focused filter is reproducible from the repository root:

```powershell
$env:MSBuildEnableWorkloadResolver = 'false'
$fixtures = @('WorkflowActionHandlersTests', 'WorkflowEffectPlannerTests', 'WorkflowSequenceExecutionTests',
    'WorkflowExecutionCoordinatorTests', 'LocomotiveWhistleAutomationServiceTests', 'ActionExecutorTests',
    'WorkflowServiceTests', 'JourneyEventPlan', 'InPortCounterServiceTests', 'PersistentInPortCounterTests',
    'MultiplexerCommandResolverTests', 'WorkflowExecutionEndToEndTests', 'MobaBackendServiceCollectionExtensionsTests')
$filter = ($fixtures | ForEach-Object { "FullyQualifiedName~$_" }) -join '|'
dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0 --filter $filter -v minimal
dotnet build SharedUI/SharedUI.csproj -v minimal
dotnet build MOBApi/MOBApi.csproj -v minimal
```

Use only test-owned definitions and fakes. No real/shipped/example solution was read, generated or edited.
No app/device/process-script execution was used for these tests. Complete private-runtime zero-I/O acceptance,
deterministic scheduling/joined disposal and authorized manual UI acceptance remain open; the focused Windows run and
desktop compilation are recorded in the main synchronization above. Each implementation PR stays draft until current-head
CI/Sonar and an independent review pass; ready marking and merge go only through the central serial merge queue.

## Planning-only structural checks

Work in the dedicated task worktree for the relevant slice (currently codex/issue-35-automation-testbench). Read AGENTS.md/applicable guidance with required
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
Design #241 and backlog/analysis #245 are merged. Continue with the open foundation tasks before private graph execution;
the first effect-boundary slice does not satisfy runner, comparison, persistence or desktop acceptance.
CI/Sonar and future manual acceptance remain separate gates; this guide never claims them from document checks.
