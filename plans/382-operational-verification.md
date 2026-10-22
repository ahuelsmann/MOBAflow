# RF-18: Performance and operational verification

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/382
**Spec Kit**: Not applicable - measurement and verification of existing behavior; telemetry gaps are closed as observability of existing paths, not as new product behavior
**Status**: Active; steps 1 and 2 and the first part of step 4 are delivered in the single issue PR

## Purpose and scope

[Issue #382](https://github.com/ahuelsmann/MOBAflow/issues/382) owns acceptance and live status.
The [umbrella plan](QUALITY-AND-REFACTORING-PLAN.md) owns programme dependencies. This is the single RF-18 child plan.
Since 2026-10-20 each worked issue has one active delivery PR, so the steps below are commits of the RF-18 PR.

Target: every critical scenario has a recorded environment, baseline, limit, agreed regression threshold, and normal
and failure results. RF-18 measures the guarantees of RF-04 (ordered Z21 events), RF-07 (CI platform matrix) and
RF-19 (MOBApi without control-plane security); it does not reopen their implementation.

## Scenario inventory

Recorded on main `f37541d9`.

| Scenario | Code under test | Existing bound or signal | Gap |
| --- | --- | --- | --- |
| EventBus publish throughput | `Common/Events/EventBus` | handler failure count (`IEventBusDiagnostics`) | no benchmark or baseline |
| Z21 event ordering under load | `Backend/Service/Z21EventPipeline` | capacity 1,024; snapshot with queue depth, peak, enqueued, published, rejected, dispatch and subscriber failures, shutdown timeouts; an overload test is on main since #367 (`Z21ReceiveTests`) | no throughput baseline |
| Runtime snapshot serialization | `Contracts/Runtime/MobaRuntimeSnapshot` (JSON over REST and SignalR) | none | no size or time baseline |
| Remote command queue bounds | `MOBApi/Service/RuntimeCommandQueue`, `RuntimeCommandAdmission` | capacity 128; a full queue rejects with `queue_full` | rejection is neither logged nor counted |
| SignalR runtime hub load | `MOBApi/Hubs/RuntimeHub` | broadcast state on the Overview page ("Hub broadcast") | no multi-client load test or limit |
| MAUI startup and binding cost | `MOBAsmart` | `MauiViewModelPerformanceTests` covers refresh behavior, not timing | no device measurement |
| ESP32 endurance | `MOBAdisplay/esp32` | length-safe UDP parser (RF-01), provisioning (RF-02) | no packet-loss or Wi-Fi reconnection run |
| Recovery and failed shutdown | Z21 reconnect, project runtime disposal, pipeline drain | `ShutdownTimeouts`, fail-safe state in the runtime snapshot | not measured end to end |

## Decisions

- **Benchmarks** (user decision on #382, 2026-10-22): BenchmarkDotNet 0.15.8 in the separate portable project
  `Benchmarks/Moba.Benchmarks`, outside the solution, the test suite and the mandatory CI gates. The manual-only
  [Benchmarks workflow](../.github/workflows/benchmarks.yml) archives the reports; the maintainer's machine records the
  baseline. Telemetry uses the built-in `System.Diagnostics.Metrics`, readable with `dotnet-counters`; `dotnet-trace`
  or the Visual Studio profiler investigate a measured regression. [Benchmarks](../docs/BENCHMARKS.md) explains how to run
  them.
- **Load tests:** deterministic NUnit tests where the bound itself is the contract (queue capacity, rejection, order);
  timing-based load (SignalR clients) runs as a benchmark or a manual scenario, never as a flaky mandatory test.
- **Thresholds:** a regression threshold is agreed per scenario with the maintainer after the first baseline, for example
  "no more than 20% slower than the recorded baseline on the same machine".
- **Hardware:** MAUI startup on a device and ESP32 endurance need the maintainer's devices and an explicit hardware task;
  they are recorded as evidence on #382, not automated.

## Delivery steps

1. **Inventory** (with this plan).
2. **Benchmarks** (delivered): EventBus publish, Z21 event pipeline enqueue and drain, runtime snapshot JSON round
   trip; the first baseline on the maintainer's machine is still open and will be recorded on #382.
3. **Load tests:** remote command queue under concurrent producers (capacity, rejection, order); SignalR runtime hub
   with several clients against an owned MOBApi process.
4. **Telemetry:** rejected remote commands (delivered): the counter `moba.runtime_commands.rejected` of the meter
   `Moba.MOBApi` (`System.Diagnostics.Metrics`, tags `reason` and `command`) and a structured warning for a full
   queue. Still open: check the existing pipeline, recovery and shutdown signals end to end.
5. **Device and hardware measurements** with the maintainer: MAUI startup and binding cost, ESP32 endurance with packet
   loss and Wi-Fi reconnection.
6. **Thresholds, documentation and completion.**

## Validation strategy

- Steps 2-4: build the affected projects, run the portable and Windows suites and the analyzer gates; benchmark results
  are archived, not asserted in CI.
- Step 5 is manual and needs the maintainer; no device deployment or hardware action without an explicit hardware task.

## Dependencies and coordination

- RF-04 and RF-19 are complete. RF-07 is active; its Linux lane already runs the portable suite, and the benchmark job
  follows its manual-lane pattern.
- Open PRs: #367 (RF-09) adds Z21 receive and overload tests that step 2 reuses, and #330 (RF-25) changes runtime
  composition. Steps that touch the same files wait for their integration.

## Risks

- **Noisy measurements:** shared CI runners vary strongly; baselines therefore come from one recorded machine, and
  CI results are evidence only.
- **Hardware availability:** endurance and device evidence depend on the maintainer's setup and time.
