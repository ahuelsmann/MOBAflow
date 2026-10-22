# Benchmarks

Repeatable performance measurements for RF-18 ([issue #382](https://github.com/ahuelsmann/MOBAflow/issues/382),
[plan](../plans/382-operational-verification.md)). The benchmarks use [BenchmarkDotNet](https://benchmarkdotnet.org/)
in the separate project [`Benchmarks/Moba.Benchmarks`](../Benchmarks/Moba.Benchmarks/Moba.Benchmarks.csproj). It is not
part of the test suite, the solution file or the mandatory CI gates.

## What is measured

| Benchmark | Code under test | Parameters |
| --- | --- | --- |
| `EventBusBenchmarks.PublishFeedback` | publishing a feedback event on the shared `EventBus` | 1 and 10 subscribers |
| `Z21EventPipelineBenchmarks.EnqueueAndDrain` | queueing feedback events in the ordered Z21 event pipeline and draining them to the event bus | 100 and 1,000 events |
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

The [Benchmarks workflow](../.github/workflows/benchmarks.yml) runs only on manual dispatch (**Actions > Benchmarks >
Run workflow**) and archives the reports for 90 days. Shared runners vary strongly, so CI results are evidence, not a
pass/fail gate.

## Baselines and thresholds

The baseline for each benchmark comes from one recorded machine. Record the machine, the operating system, the .NET SDK
and the commit with the result on issue #382. A regression threshold is agreed per benchmark after the first baseline,
for example "no more than 20% slower than the recorded baseline on the same machine".

To find out *why* a result got worse, use the .NET diagnostic tools on a running process: `dotnet-counters` for live
counters, `dotnet-trace` for a trace, or the Visual Studio Performance Profiler.
