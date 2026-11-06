# RF-09: Coverage and mutation ratchets

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/366
**Spec Kit**: Not applicable - regression tests and CI lanes for existing contracts without product behavior
**Status**: Active; steps 1 and 2 are delivered (#367); step 3 is in its own issue PR

## Purpose and scope

[Issue #366](https://github.com/ahuelsmann/MOBAflow/issues/366) owns acceptance and live status.
The [umbrella plan](QUALITY-AND-REFACTORING-PLAN.md) owns programme dependencies. This is the single RF-09 child plan;
it replaces the earlier coverage and mutation strategy document.

Target: tests detect meaningful faults in risk-critical code. Signal state, commands and data come first. A higher
aggregate score must not hide critical untested contracts, and an activated lane never lowers its measured floor.

## Lanes

[`mutation-lanes.json`](../MutationTest/mutation-lanes.json) registers every test fixture. Only `active` lanes are
mutation-tested; `planned` lanes claim nothing. [`Invoke-MutationLane.ps1`](../scripts/Invoke-MutationLane.ps1) runs
a lane the same way locally and in [Quality CI](../.github/workflows/quality.yml).

| Lane | Status | Production project | CI job | Break threshold |
| --- | --- | --- | --- | --- |
| `domain` | active | `Domain` | `mutation-domain` (Linux) | 90% |
| `common` | active | `Common` | `mutation-common` (Windows) | 63% |
| `speech-base` | active | `Speech.Base` | `mutation-speech-base` (Linux) | 65% |
| `backend` | **active from step 1** | `Backend` | `mutation-backend` (Linux) | 47% (raised from 43%) |
| `api` | **active from step 3** | `MOBApi` | `mutation-api` (Linux) | 44% |
| `api-process` | planned | `MOBApi` | - | - |
| `shared-ui` | planned (step 4) | `SharedUI` | - | - |

The Backend host [`Backend.MutationTests.csproj`](../MutationTest/Backend/Backend.MutationTests.csproj) compiles all
fixtures under `Test/Backend/`, the workflow end-to-end fixture and the Z21 DCC decoder tests, with the shared fakes
they need. The MOBApi host [`Api.MutationTests.csproj`](../MutationTest/Api/Api.MutationTests.csproj) compiles the
in-process fixtures under `Test/MOBApi/`. The MOBApi process fixtures (`MobApiProcessTests`, `RuntimeHubLiveE2ETests`)
start real MOBApi processes and cannot detect mutants of the in-process assembly; they stay in the planned
`api-process` lane.

## Backend baseline (step 1)

Local full run on Windows, Stryker.NET 5.0.0, four test processes, 660 passing ordinary tests, main `e0f6b9f6` plus
the lane host; 35 minutes:

| Measurement | Count |
| --- | ---: |
| Mutation score | 43.18% |
| Killed | 2,585 |
| Detected through timeout | 74 |
| Survived | 2,020 |
| No coverage | 1,479 |
| Ignored (block already covered) | 1,245 |
| Compile errors | 0 reported (see below) |

The reported 0 may be incomplete: these counts add up to 7,403 scored and ignored mutants with a score denominator
of 6,158, the same aggregate as Linux CI job 119686366715 (head `ab25e67`), which excludes 770 compile-error mutants.
This aggregate match does not show that the same mutants or methods were excluded; whether the local run had the
same Safe-Mode exclusions is unverified.

Largest open areas (survived + no coverage), which feed step 2 and later:

| File | Survived | No coverage |
| --- | ---: | ---: |
| `Service/Recording/RecordingArtifactSerializer.Import.cs` | 145 | 125 |
| `Service/MobaRuntimeService.RuntimeApi.cs` | 93 | 90 |
| `Service/Validation/InterlockingDefinitionValidator.cs` | 81 | 93 |
| `Service/Recording/RecordingSessionService.cs` | 107 | 42 |
| `Service/Recording/CoreRecordingPayloadValidators.cs` | 101 | 38 |
| `Z21.Drive.cs`, `Z21.Commands.cs`, `Z21.cs`, `Z21.Receive.cs`, `Z21.Keepalive.cs` | 115 | 282 |
| `Protocol/Z21MessageParser.cs` | 65 | 31 |
| `Network/UdpWrapper.cs` | 12 | 75 |

These counts are a planning baseline, not a permanent status; later changes need a fresh full report.

Linux CI on the combined steps 1 and 2 (head `d1f26fe`, job 119707711457): 745 passing ordinary tests, score 47.43%
(2,859 killed, 63 timeouts, 2,073 survived, 1,166 no coverage, 1,245 ignored); of 8,176 created mutants, 770 are
compile errors outside the score, giving a score denominator of 6,161 (the `ab25e67` run, job 119686366715: 6,158).
The aggregate denominators are close to the local baseline, but the runs differ in platform and in reported compile
errors, and the mutant identities were not compared. The scores are therefore not shown to be comparable, and the
difference from the local 43.18% is not attributed to the step-2 tests. The floor stayed at 43% until a comparable
full run (same platform, same compile-error set).

**Floor raise to 47%** (RF-09 floor work, in the step 3 PR): seven Linux CI runs of the `mutation-backend` job on heads
over main `f2f388d3` are comparable. They use the same platform, Stryker.NET 5.0.0, 764-765 passing ordinary tests and
771 compile-error mutants each.

| Run | Head | Score |
| --- | --- | ---: |
| 40100277723 | `941806e6` (#395) | 47.53% |
| 40103237430 | `8f478154` (#396) | 47.48% |
| 40104956089 | `84412d1a` (#388) | 47.53% |
| 40111333629 | `9980041b` (#396) | 47.48% |
| 40113333329 | `c6aea928` (#394) | 47.54% |
| 40113416185 | `f4b9939c` (#392) | 47.56% |
| 40113496260 | `a4a638c1` (#395) | 47.56% |

The lowest score is 47.48%, so the floor is 47%. The mutant identities were not compared one by one; the equal
compile-error count, platform and tool version make these runs the comparable set the plan asked for. Local Windows
runs are not used for this floor.

### Methods outside the Backend mutation gate

Stryker.NET 5.0.0 reports CS0165 ("Use of unassigned local variable") at 12 sites during its initial compile and puts
the affected methods into Safe Mode: all their mutants count as compile errors, so no mutation result gates them.
The cause is a Stryker instrumentation limitation, not a Backend or test defect: mutating conditions around `out` or
pattern variables, or removing a `break` inside `try`/`catch`, breaks definite assignment. Paths relative to
`Backend/`:

- `Service/MobaRuntimeService.RuntimeApi.cs`: `ConnectCoreAsync`
- `Service/MobaRuntimeService.AutoConnect.cs`: `AttemptZ21ConnectionCoreAsync`
- `Network/UdpWrapper.cs`: `ReceiverLoopAsync`
- `Service/Recording/CoreRecordingEventMappers.cs`: `Map` (three overloads)
- `Service/Recording/RecordingJsonReader.cs`: `RequireInt32`, `RequireInt64`, `ReadGuid`
- `Service/Recording/RecordingArtifactSerializer.Import.cs`: `ReadEntry`
- `Manager/JourneyManager.cs`: `QueueMatchingEvents`
- `Service/TimetableStateStore.cs`: `RecordDepartureAsync`

The first three are core Z21 transport and connect paths; the step-2 protection does not extend to them. Open #366
floor work: rewrite these methods so that mutations stay definitely assigned, or change the Stryker version, then
remeasure.

The floor is the measured score rounded down to a whole percent. It is a safeguard, not a quality target: step 2
closes meaningful gaps, and a fresh full run raises it.

## Delivery steps

Since 2026-10-20 each worked issue has one active delivery PR, so these steps are commits of the RF-09 PR rather than
separate pull requests.

1. **Backend lane** (with this plan): host, registry, CI job, measured floor.
2. **Z21 ordering, overload and runtime commands:** negative and failure-path tests for the Z21 receive pipeline,
   overload handling and the runtime command paths; raise the Backend floor to the measured result. Delivered:
   `Z21ReceiveTests`, `Z21OperatorCommandTests`, the extended `Z21EventPipelineTests`, `MobaRuntimeServiceCommandTests`
   and `MobaRuntimeServiceZ21EventTests`. Expected packets and bits come from the Z21 LAN protocol, which exposed two
   defects, fixed here:
   - LAN_X_GET_STATUS sent DB0 `0x21` instead of `0x24`, so the Z21 discarded every status request, including the
     keepalive;
   - `SystemState.IsProgrammingMode` tested the undefined bit `0x08` instead of `0x20`.

   The floor was raised to 47% from comparable Linux CI runs; see the Backend baseline above.
3. **MOBApi lane:** command validation and remote command queue bounds; real registration paths, owned processes only
   where required. Delivered: the `api` lane with its host, Stryker configuration and the `mutation-api` CI job.
   - **Measurement:** the first local run (Windows, Stryker.NET 5.0.0, four processes) scored 38.42%. After the new tests
     it scored 44.63%: 187 killed, 81 survived, 151 without coverage, 124 ignored, 36 compile errors. Linux CI on head
     `8f478154` (run 40103237430) gives the same score with the same 36 compile errors, so the runs are comparable.
     The floor is 44%.
   - **New tests:** missing REST requests and every queued command field; null commands at the admission and the queue;
     the `queue_full` code; the debug log and the counter metadata; the hub's project and signal checks; the queue reason
     when queueing fails after validation; snapshots from a connection that is not the host.
   - **Equivalent survivors in the command path:**
     - `ConfigureAwait(false)` to `true` in the hub (scheduling only);
     - `BoundedChannelFullMode.Wait` removed (the channel default is `Wait`);
     - `!TryDequeue || command == null` to `&&` (a successful read never returns null, because the queue rejects null);
     - `parsed != Guid.Empty` in `RuntimeHub.ParseProject`, and `ThrowIfNull` in the admission's validation: the admission
       and the validator reject the same input with the same message or exception.
   - **Safe Mode:** Stryker reports CS0165 for `RuntimeHub.PushSnapshot` (the `entry` variable assigned in `try`), so its
     mutants count as compile errors outside the score.
   - **Open in the lane:** remote and host registration, disconnect handling, `Program`, the discovery service and the
     photo and solution controllers have no in-process tests yet. A later floor raise follows from new tests there.
4. **SharedUI lane:** train control, settings, project switches and stale asynchronous responses through the existing
   runtime interfaces.
5. **Completion:** further lanes only where the tooling is technically suitable (Sound, display, rendering by measured
   risk); RF-07 records the final mutation lanes in its deliverable inventory.

## Classify each surviving or uncovered mutant

| Category | Action | Evidence |
| --- | --- | --- |
| Relevant existing test not included | Include it and its helpers in the lane host | Tests discovered; fresh full-lane report |
| Meaningful change not detected | Strengthen an assertion or add the missing behavior case | Test passes the original code and detects the mutant |
| Confirmed production defect | Add a failing regression, then the smallest fix | Red-before/green-after test and affected-consumer checks |
| Proven equivalent mutant | Explain the valid input range and observable behavior | No relevant difference for all allowed inputs |
| Contract or ownership unclear | Keep open and inspect callers/requirements | No speculative fix or premature equivalence claim |

Equivalent does not mean that existing examples return the same result: messages and error precedence can be
observable. See [equivalent mutants](https://stryker-mutator.io/docs/mutation-testing-elements/equivalent-mutants/).
Record project, file, method, mutation kind, report commit, contract, risk and test or rationale; mutant IDs are not
stable between runs.

Do not exclude files, strings or mutation types, or remove production code to raise a score. Do not derive expected
values from the production tables under test. Coordinate asynchronous tests deterministically, not with sleeps.
Timeouts count as detected for Stryker but are no substitute for precise assertions.

## Coverage thresholds

[`coverage-thresholds.json`](../Test/coverage-thresholds.json) gates line and branch coverage in the Windows `desktop`
lane. Thresholds only rise after a measured improvement and never fall without an approved rationale. Keep portable,
Windows and Android measurements separate: a portable result excludes WinUI and Android and is not a whole-solution
figure. An empty coverage report with zero instrumented lines is an invalid measurement, not 0%.

## Validation strategy

Per step:

1. Run the affected ordinary tests; they must be discovered and pass.
2. Run a focused Stryker diagnosis (`--mutate "**/<File>.cs"`) while working; it is not a lane result.
3. Run the full lane with `Invoke-MutationLane.ps1` and compare killed, survived, uncovered and timeout counts with the
   previous report; raise the floor only to the measured result.
4. Prove the lane in CI on the pull request; configuration alone is no execution evidence.

Run Stryker and ordinary builds sequentially in one worktree; Stryker recreates shared intermediate files. No app
launch, train movement or layout action is part of this plan; tests use fakes and owned temporary data.

## Risks

- **CI duration:** the Backend lane is the largest; its job timeout follows the measured duration. If it outgrows the
  timeout, split the lane by namespace rather than excluding mutants.
- **Flaky timing tests:** a mutant that changes a delay can turn a wall-clock test into a timeout; such tests move to
  `TimeProvider`-based fakes instead of being excluded.
