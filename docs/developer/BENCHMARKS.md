# Benchmarks

Repeatable performance measurements for RF-18 ([issue #382](https://github.com/ahuelsmann/MOBAflow/issues/382),
[plan](../../plans/382-operational-verification.md)). The benchmarks use [BenchmarkDotNet](https://benchmarkdotnet.org/)
in the separate project [`Benchmarks/Moba.Benchmarks`](../../Benchmarks/Moba.Benchmarks/Moba.Benchmarks.csproj). It is not
part of the test suite, the solution file or the mandatory CI gates.

## What is measured

| Benchmark | Code under test | Parameters |
| --- | --- | --- |
| `EventBusBenchmarks.PublishFeedback` | publishing a feedback event on the shared `EventBus` | 1 and 10 subscribers |
| `Z21EventPipelineBenchmarks.EnqueueAndDrain` | queueing feedback events in the ordered Z21 event pipeline and draining them to the event bus; each operation pushes one burst through a freshly started pipeline, so the result includes pipeline start-up and shutdown and is a relative baseline on the same machine, not the pure enqueue and drain cost | 100 and 1,000 events |
| `RuntimeSnapshotSerializationBenchmarks.Serialize` / `Deserialize` | the JSON round trip of the runtime snapshot that MOBApi and SignalR send | 10 and 100 locomotives, journeys and signal-box elements |

Each benchmark also reports allocated memory.

## Run locally

Run from the repository root in Release, on an otherwise idle machine and on mains power:

```powershell
dotnet run --project Benchmarks/Moba.Benchmarks/Moba.Benchmarks.csproj -c Release -- --filter "*" --artifacts artifacts/benchmarks
```

Narrow the run with a filter such as `--filter "*EventBus*"`. Add `--job Dry` for a quick check that every benchmark
runs; dry timings are not measurements. Reports (Markdown, HTML and CSV) land in `artifacts/benchmarks/results`.

## Run in CI

The [Benchmarks workflow](../../.github/workflows/benchmarks.yml) runs only on manual dispatch (**Actions > Benchmarks >
Run workflow**) and archives the reports for 90 days. Shared runners vary strongly, so CI results are evidence, not a
pass/fail gate.

## Baselines and thresholds

The baseline for each benchmark comes from one recorded machine. Record the machine, the operating system, the .NET SDK
and the commit with the result on issue #382. A regression threshold is agreed per benchmark after the first baseline,
for example "no more than 20% slower than the recorded baseline on the same machine".

To find out *why* a result got worse, use the .NET diagnostic tools on a running process: `dotnet-counters` for live
counters, `dotnet-trace` for a trace, or the Visual Studio Performance Profiler.

## Recorded Baseline

- **Date:** `2026-11-28`
- **Baseline commit:** `90b5db751` (`main`)
- **BenchmarkDotNet:** `v0.15.8` (`DefaultJob`, Windows High Performance power plan `8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c`)
- **OS:** Windows 11 (`10.0.26200.9818` / 25H2 / 2025Update / HudsonValley2)
- **CPU:** AMD Ryzen AI 7 350 w/ Radeon 860M 2.00GHz, 1 CPU, 16 logical and 8 physical cores (`X64 RyuJIT x86-64-v4`)
- **.NET SDK & Runtime:** .NET SDK `10.0.404`; Runtime `.NET 10.0.15` (`10.0.1526.53008`), GC `Concurrent Workstation`

| Benchmark | Parameter | Mean | Error | StdDev | Gen0 | Gen1 | Allocated |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `EventBusBenchmarks.PublishFeedback` | `Subscribers = 1` | 25.65 ns | 2.188 ns | 6.451 ns | 0.0038 | - | 32 B |
| `EventBusBenchmarks.PublishFeedback` | `Subscribers = 10` | 78.61 ns | 1.567 ns | 3.440 ns | 0.0124 | - | 104 B |
| `Z21EventPipelineBenchmarks.EnqueueAndDrain` | `Events = 100` | 30.70 μs | 0.531 μs | 0.672 μs | 2.8687 | - | 23.57 KB |
| `Z21EventPipelineBenchmarks.EnqueueAndDrain` | `Events = 1000` | 275.50 μs | 5.490 μs | 12.280 μs | 25.3906 | 0.9766 | 207.77 KB |
| `RuntimeSnapshotSerializationBenchmarks.Serialize` | `Items = 10` | 5.104 μs | 0.1473 μs | 0.3907 μs | 1.0834 | - | 8.91 KB |
| `RuntimeSnapshotSerializationBenchmarks.Deserialize` | `Items = 10` | 9.007 μs | 0.1756 μs | 0.2404 μs | 1.0376 | 0.0153 | 8.55 KB |
| `RuntimeSnapshotSerializationBenchmarks.Serialize` | `Items = 100` | 35.917 μs | 0.7163 μs | 0.7355 μs | 8.9111 | - | 73.26 KB |
| `RuntimeSnapshotSerializationBenchmarks.Deserialize` | `Items = 100` | 76.220 μs | 1.4952 μs | 2.3279 μs | 9.2773 | 1.2207 | 76.58 KB |

> **Measurement & threshold note:** Recorded during normal workstation background load. Recommended regression investigation threshold on the same machine (pending final confirmation on [issue #382](https://github.com/ahuelsmann/MOBAflow/issues/382)): investigate any **>20% `Mean` time increase** or **unexpected `Allocated` memory growth (`0 B` unexpected growth)** relative to this baseline.
