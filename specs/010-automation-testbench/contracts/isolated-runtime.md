# Isolated Runtime Contract

**Source Issue**: #35
**Status**: Proposed internal feature contract; no new REST/SignalR endpoint or public runtime lifecycle API.

## Construction and authority

Only the isolated factory can create an execution handle accepted by the runner. Its identity distinguishes it from live
runtime handles; wrong/live targets are rejected before input/effects. Do not accept an arbitrary provider or root gateway.

The factory owns a scenario-free project clone, bus/input source, clock/scheduler, null-persistence counters, in-memory
journey checkpoint store, shared managers/executor, recording effects, private whistle gateway and complete safe trace.
The whistle seam accepts existing `Moba.Backend.Interface.ILocomotiveFunctionCommandGateway`; isolated composition supplies
its recording implementation instead of `MobaRuntimeLocomotiveFunctionCommandGateway`. No SharedUI interface is required.
Shared immutable code/services are allowed only when they cannot retain live state or perform I/O. No application bus,
connection registry/host, production logger/store, process launcher, real sound or display transport is resolved.
A private forwarding bus, if unavoidable, may forward only into another run-owned bus.

Construction validates every reference/state/payload and seeds counts/checkpoints before manager activation/capture.
Initial state publication does not inject feedback or arrival actions. Private interlocking activates definitions only;
connection synchronization is excluded. The whistle gateway is logically available without a physical connection.

## Logical feedback input

The MVP supports typed Feedback { InPort, IsActive } at nonnegative AtTicks/Order. It is an explicit logical test input,
not an RBus packet, recorder import or simulation of packet deduplication.
Injection occurs after physical parsing/deduplication. Production RBus unchanged-occupancy suppression remains unchanged.

- IsActive=true represents one raw activation: invoke private IZ21.Received consumers, publish private raw
  FeedbackReceivedEvent, then private FeedbackStateChangedEvent(active=true), with shared input correlation.
- IsActive=false publishes private FeedbackStateChangedEvent(active=false); it does not increment counts or trigger a
  raw activation whistle.
- Repeated active inputs are explicit pulses even without a preceding inactive input. Counter time filtering remains in
  the production counter; rejected pulses still reach raw whistle consumers.
- Dispatch order is synchronous and stable. Drain owned descendants after the full input callback sequence, without
  advancing time. No input invokes a workflow directly.
- Every matching enabled event entry of every explicitly active captured journey executes through the normal manager.
  Selected targets affect editing/assertion focus only.

## Shared effect adapter

Handlers share payload/reference validation and pure domain resolution before selecting an execution adapter.
Requests contain executable typed fields privately; recording projects only approved safe fields before capture/logging.

| Family | Common behavior | Production adapter | Recording adapter |
| --- | --- | --- | --- |
| Command/drive/function/turnout | Existing structural payload/reference validation | Existing Z21 command execution | Safe typed intent, no transport |
| Signal aspect | Existing multiplexer resolution | Existing resolved device command | Aspect plus resolved safe command |
| Audio | Required structural path | File availability and actual playback | Sanitized path/action identity; no existence probe |
| Announcement | Existing context/template rendering | Speaker/environment execution | Safe intended text/options; no speaker creation |
| Script | Required structural path/payload | Availability/process/output handling | Opaque identity, safe path, ArgumentsRedacted; no file/process probe or arguments/hash |
| Display | Existing structural intent | Current unconfigured behavior retained | Valid intended payload; no new production delivery |
| ChangeJourneyStop | Existing stop-transition service on shared context | Production domain state | Same operation on private domain state, not a fake effect |
| Raw-feedback whistle | Existing rule/coalescing algorithm | Existing function gateway | Run-owned recording function gateway |

A missing external resource does not invalidate structurally valid recording intent. Malformed payload, unresolved reference,
shared domain failure and cancellation still fail as production execution dictates. Adapter separation cannot swallow errors
or weaken production missing-file tests. No ordinary logging sink receives raw action/context/exception data from a run.

## Time and owned scheduling

Virtual clock uses UTC epoch 2000-01-01T00:00:00Z, UTC local timezone and monotonic TimeSpan ticks. Framework TimeProvider
UTC/timestamp/timer operations share that clock. Playback wall-time pacing is outside the graph and has no trace/state role.

Proposed internal IAutomationWorkScheduler operations:

| Operation | Contract |
| --- | --- |
| StartOwned(stableKey, owner, callback, token) | Register handle before dispatch; no callback while a manager lock is held |
| DeferTurn / Delay / JoinOwned awaitables | Native awaiters enqueue resumptions explicitly, including nested-result joins |
| DrainCurrentInstantAsync | Finish Ready/Running/due timers/terminal notifications at current time; future waits may remain |
| CancelAndDrainAsync | Stop admission, cancel waits and join all owned cleanup/notifications before completion |

Handle states are Ready, Running, WaitingTimer(deadline), WaitingChildren and Terminal. Expected joins reference owned
handles; no unclassified Task.Run/detached continuation is permitted to produce observable state. Recording handlers/private
stores complete synchronously where possible. Unexpected async escape or retained fault prevents successful completion.
Preserve production per-journey FIFO, context creation when invocation executes, and independent journey timing.

Next instant is minimum remaining input time or owned timer deadline. At that instant:
1. Drain existing due timers/continuations in stable work/registration order.
2. Dispatch inputs in explicit Order; after each full callback sequence drain immediately runnable descendants.
3. Drain all remaining same-time microtasks/terminal notifications before advancing.
New same-time timers join the current pump. A future delay in journey A cannot block ready work in journey B.
Pause completes only the current instant. Single step completes exactly the next instant and returns to Paused, unless
execution becomes terminal. An empty-input run still initializes and compares state; pending work prevents early completion.

Retain production zero-delay behavior; do not add arbitrary yields. A normal Task-returning delay wrapper or
SynchronizationContext is insufficient where ConfigureAwait(false) escapes it. Implement and prove the awaitable seam
across coordinator launch/predecessor joins, sequence waits/results, counted-feedback ownership and whistle pulse/cleanup.

## Failure, cancellation and disposal

Observe original owned task outcomes before error-logging wrappers. Private EventBus subscriber failures must signal the
runner even if the production bus normally logs them. Use a narrow private failure observer/dispatch seam, not a changed
global event policy. First executor failure retains its correlation; cancellation/internal error are separate outcomes.

Cancel closes input admission and interrupts pacing; then cancels waits and joins deterministic cleanup. Function-off
cleanup may be recorded while Cancelling. Only after all handles/timers/subscriptions terminate does it seal capture and
report Cancelled. Nothing may record afterward. Disposal joins the same operation; repeated cancellation is idempotent.
Drain failure blocks context replacement/restart and exposes a safe recoverable error; never discard ownership to claim completion.

Restart validates current definitions and creates entirely new counters/checkpoints/scheduler/clock/trace. No former work,
event subscription or mutable project object may survive into it.

## Evidence boundary

Trace stores controller timestamps, stable sequence, safe typed transitions/effects, source identities and outcomes.
Never serialize raw events (their CreatedUtc is wall time), raw project/action payloads, script arguments/derivatives,
audio/file contents, transport bytes or exceptions. Whistle attribution includes all contributing inputs/rules.
Any resource/capture failure retains partial trace and reports TestbenchError; silent eviction cannot produce Passed.

Acceptance must prove private references, forbidden dependency resolution and zero external calls for success, shared
failure, cancellation and restart. Comparing whole live snapshots is insufficient because unrelated telemetry may continue.
