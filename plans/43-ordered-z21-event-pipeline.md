# Ordered Z21 Event Pipeline Implementation Plan

## Document status

- Status: In progress
- Primary issue: https://github.com/ahuelsmann/MOBAflow/issues/43
- Status source: GitHub Kanban
- Baseline: `main` at `6dc5faea3d62c5e35aaabb4e88807507eaad4312`

## Technical objective

Replace the per-event `Task.Run` dispatch in `Z21` with one bounded FIFO
consumer while preserving the existing synchronous `IEventBus` contract and
the `UiThreadEventBusDecorator` threading boundary.

GitHub issue #43 owns scope, priority, status, and acceptance criteria. This
plan owns implementation sequence, technical policy, risks, and validation.

## Event-path inventory

The pipeline accepts the following events emitted by `Z21`:

- connection established and lost;
- X-Bus status;
- locomotive information;
- system state;
- feedback activation;
- version information; and
- simulated feedback used by diagnostics and tests.

The legacy .NET events on `IZ21` remain synchronous and unchanged. Only
application-wide `IEventBus` publication moves through the ordered pipeline.

## Queue and overload policy

- Use `System.Threading.Channels` with one reader and multiple permitted writers.
- Capacity is 1024 waiting events. This bounds memory while absorbing short
  bursts of feedback and locomotive telemetry.
- Configure `BoundedChannelFullMode.Wait`, but use only non-blocking `TryWrite`.
- When capacity is exhausted, reject the incoming event. Never evict an older
  accepted event because that would break the causal prefix already promised to
  consumers.
- Record queue depth, peak depth, accepted, published, rejected, dispatch-failed,
  subscriber-failed, and shutdown-timeout counts. Emit structured logs for
  rejection, dispatch failure, subscriber failure, and shutdown timeout.

## Failure and shutdown policy

- The existing `EventBus` continues isolating and logging individual subscriber
  failures. The pipeline additionally catches failures from the event-bus
  boundary so its consumer cannot terminate silently.
- `DisposeAsync` completes the writer and allows up to five seconds for draining.
  If the timeout expires, it records the timeout and cancels the consumer.
- Synchronous `Dispose` cannot wait safely; it stops accepting work and cancels
  immediately.
- `DisconnectAsync` does not stop the pipeline, because the same `Z21` instance
  may reconnect. Final disposal owns pipeline shutdown.

## Affected files

- `Backend/Service/Z21EventPipeline.cs`
- `Backend/Z21.cs`
- `Backend/Z21.Receive.cs`
- `Backend/Z21.Diagnostics.cs`
- `Backend/Z21.Keepalive.cs`
- `Backend/Interface/IZ21.cs`
- `Common/Events/IEventBus.cs`
- `SharedUI/Service/UiThreadEventBusDecorator.cs`
- `Test/Backend/Z21EventPipelineTests.cs`
- `Test/Backend/Z21UnitTests.cs`
- `Test/Backend/Z21WrapperTests.cs`
- `Test/Common/EventBusTests.cs`

## Implementation sequence

1. Add the bounded dispatcher and immutable diagnostic snapshot.
2. Route every `Z21` event-bus publication through the dispatcher.
3. Add asynchronous drain and synchronous cancellation lifecycle paths.
4. Add unit tests for FIFO, saturation, dispatch failure, draining, timeout, and
   EventBus subscriber isolation.
5. Add a Z21 integration stress test using simulated feedback.
6. Run focused tests, the complete cross-platform test project, and relevant
   Release builds.

## Risks and rollback

- A capacity that is too small can reject valid bursts. Peak depth and rejection
  metrics provide evidence for later tuning without changing the policy.
- A blocking subscriber can exceed the drain timeout. Timeout cancellation keeps
  application shutdown bounded, while the EventBus rules continue to require
  short non-blocking handlers.
- The change is reversible by restoring direct publication because public event
  contracts and payload types are unchanged.

## Validation commands

```powershell
dotnet test Test/Test.csproj -f net10.0 --filter "FullyQualifiedName~Z21EventPipelineTests|FullyQualifiedName~Z21UnitTests|FullyQualifiedName~EventBusTests"
dotnet test Test/Test.csproj
dotnet build Backend/Backend.csproj -c Release
dotnet build MOBApi/MOBApi.csproj -c Release
git diff --check
```
