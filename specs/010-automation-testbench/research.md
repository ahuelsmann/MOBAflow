# Research: Isolated Automation Testbench

**Source Issue**: #35
**Date**: 2026-10-11
**Status**: Resolved design decisions; no product implementation or executed acceptance.
**Baseline**: `39312ca93413eef13fa6d7bb8360d5bf5fdda4ae`, after #207/#211 integration.

Three read-only research lanes examined composition/effects, time/ownership and definitions/UI. Source files were
secrets-scanned before reading. This document resolves the planning unknowns without introducing another automation engine.

## 1. Reuse child composition without starting a live runtime

**Decision**: Extract an internal child-graph composition helper from `ProjectRuntimeFactory`; production retains its
connection lease/host. A dedicated isolated factory passes private collaborators and owns awaited cleanup.

**Rationale**: `Backend/Service/ProjectRuntimes/ProjectRuntimeFactory.cs:29-98` already constructs counters, interlocking,
workflow/context, journeys and whistles, but `Create` acquires a registry lease. Its `StartAsync:196-200` invokes live
runtime startup. `MobaRuntimeService.cs:139-161` and `MobaRuntimeService.AutoConnect.cs:33-48` start auto-connect timers;
`Backend/Z21.cs:144-171` opens UDP and performs connection work. No existing factory testbench overload is assumed.

**Alternatives considered**: Disconnecting/substituting Z21 in the live host still constructs timers/lifecycle dependencies.
A mode flag on the root provider risks resolving live adapters. Replay projection does not run journey/workflow algorithms.

## 2. Seed production state without production persistence

**Decision**: Private counters use `store: null`; a private in-memory checkpoint store and cloned activation values seed all
journeys before their manager is created. Use the existing Domain JSON options for scenario-free project cloning.

**Rationale**: `InPortCounterService.cs:46-67` is initialized without a store; `Set:139-152` emits snapshot change, not
counted feedback, and clears timing history. Persistent initialization/saves use detached work, avoided by null storage.
`JourneyManager.cs:99-126,170-183` consumes checkpoints and activation. Validate identity/index beforehand instead of
relying on first-stop fallback. Empty journey position/context mapping follows current production, not an invented state.

**Alternatives considered**: Reading production counter files violates isolation; fake persistent stores add unnecessary
async tasks. Seeding via feedback invents arrival/workflow effects. Live simulation routes into production state.

Whistle construction accepts existing `Moba.Backend.Interface.ILocomotiveFunctionCommandGateway`
(`Backend/Interface/ILocomotiveFunctionCommandGateway.cs:5-19`). Its production adapter wraps the runtime; isolated
composition supplies a recording implementation of that same Backend interface, preserving the layer boundary.

## 3. Share structural validation and separate real effects

**Decision**: Extend the existing effect vocabulary into validated typed requests, with production and recording adapters.
Stop changes remain shared domain operations. Recording never probes files/resources or logs raw configuration.

**Rationale**: `WorkflowEffectPlanner.cs:17-173` validates structure but its planned effect only describes category/resources,
not an executable typed payload. `WorkflowActionHandlers.cs` executes commands (27-40), audio (65-79), announcements
(98-129), display intent (138-152), signal resolution (166-183), script processes (206-244) and stop changes (284-300).
Move availability checks/real execution into production adapters; reuse pure template/signal resolution. Display delivery
is currently unconfigured, so capture intent without secretly adding production transport.

**Alternatives considered**: Replacing entire handlers duplicates payload/failure behavior. `DryRun` skips both handlers
and delays (`WorkflowService.Sequence.cs:139-150`). A recording Z21 alone leaves process/audio/file paths live.

## 4. Preserve raw, accepted and occupancy feedback separately

**Decision**: MVP logical feedback inputs carry InPort and active/inactive state. Each active input represents an explicit
activation, delivering `Received`, raw feedback and then occupancy state in production callback order. Inactive input
delivers only occupancy deactivation. This is logical injection, not packet replay or transport-cache simulation.

**Rationale**: `Z21.Receive.cs:197-205` delivers raw activation independently of counter acceptance.
`InPortCounterService.cs:219-242` filters against the last accepted timestamp and rejects saturation without wrapping.
`JourneyManager.cs:207-212,248-279` matches exact counts of all enabled entries in active journeys.
Whistles consume raw feedback; rejecting a count must not suppress them. No initial train occupancy/speed model is added.

**Alternatives considered**: Injecting only counted feedback bypasses filters and whistles. Treating selected workflows as
an execution filter suppresses valid production reactions. Feeding wire packets would expand the MVP into transport replay.

## 5. Control async ownership, not just time

**Decision**: Internal `IAutomationWorkScheduler` registers owned work before launch and exposes scheduler-native deferred,
delay and child-join awaitables. A deterministic pump controls every observable continuation; production keeps independent
journeys/source FIFO. Fixed UTC epoch/timezone; next instant is minimum input or timer deadline.

**Rationale**: `WorkflowExecutionCoordinator.cs:110` uses `Task.Yield`. Journey and whistle callbacks start observed tasks
without a drain (`JourneyManager.cs:187-235`; whistle service 271-301). Delay continuations use
`ConfigureAwait(false)`; changing SynchronizationContext is insufficient. Future-timer waits cannot be awaited as part
of current-instant completion. Owned joins must also control observable returns from nested work.

The framework provides injectable UTC/timestamps/timers:
[Microsoft TimeProvider overview](https://learn.microsoft.com/en-us/dotnet/standard/datetime/timeprovider-overview).
Explicit continuation registration is supported by
[IValueTaskSource.OnCompleted](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.sources.ivaluetasksource.oncompleted?view=net-10.0).
These primitives support the proposed seam; they do not already supply the full deterministic pump.

**Alternatives considered**: FakeTimeProvider alone controls time, not ownership/order. A new testing package is unnecessary.
Sorting completed traces cannot repair divergent state. Serializing entire journeys changes timing. Task.WhenAll(all pending)
blocks on future delays; sleep/Task.Yield polling provides no quiescence proof.

## 6. Join cleanup before closing a run

**Decision**: Stop input admission, cancel owned waits, drain cleanup and terminal notifications, then seal capture and publish
Cancelled/disposal completion. Initial cancellation may capture cleanup function-off; none may occur after completion.

**Rationale**: Factory disposal (`ProjectRuntimeFactory.cs:219-231`) and coordinator/whistle disposal request cancellation
without joining every task. Whistle cleanup sends function-off in `finally`. Error-observation wrappers/EventBus logging
can hide a fault, so private execution must retain owned-task and subscriber failure signals.

**Alternatives considered**: Detaching callbacks or ignoring stale generations does not release work. Awaiting while holding
manager locks risks deadlock; dispatch must stay outside those locks. A public runtime lifecycle API is unnecessary.

## 7. Complete typed trace and pure comparison

**Decision**: Run-owned trace uses controller time, stable work/source identities, logical sequence and allow-listed payloads.
Retain all assertion outcomes; first failed expectation follows saved definition order. Complete data is required for Passed.

**Rationale**: `WorkflowTraceStore.cs:11-55` trims at 10,000 entries/100 executions. `EventBase.CreatedUtc` is wall-clock
(`Common/Events/IEvent.cs:25`); raw event serialization is unsuitable. Workflow lifecycle correlation can be projected;
journey transitions need owned-turn attribution, whistles retain all contributing input IDs for coalesced rules.

**Alternatives considered**: Whole-object JSON/exception logging exposes arguments/paths and unstable timestamps.
Hashing script arguments still derives sensitive output. Persisted run history/fingerprints add unrequested mechanics.

## 8. Existing persistence and narrow UI transition guard

**Decision**: Project-owned optional definition collection; schema version 4, existing Domain JSON options and atomic solution
save. New page transient/VM singleton. Await one narrow testbench guard at concrete transition commands before mutations.

**Rationale**: `MOBAflow/Service/SolutionFileService.cs` owns atomic saving; runtime transport JSON is not solution serialization.
`WorkflowLibraryViewModel` supplies wrapper/save patterns. Architecture guidance requires transient views; TimetablePage
registration is a current example. Navigation swaps Frame content before unload; project selection and replacing events
are synchronous. Therefore cancellation in Unloaded/PropertyChanged cannot satisfy pre-replacement disposal.
Detailed call sites are in [operator-ui.md](contracts/operator-ui.md).

**Alternatives considered**: Auto-save run state would update the live solution. A generic lifecycle registry exceeds scope.
Async-void setters cannot delay a context commit. Modifying old page lifetimes is unrelated cleanup.

## Planning outcome

All research unknowns have a chosen design. Implementation must first prove the shared scheduling seam and production
regressions; this is a validation dependency, not an unresolved requirements placeholder. Follow-up [tasks](tasks.md) and
read-only consistency analysis are complete as draft backlog work; design #241 is merged. Backlog review/integration and
product implementation remain.
No example solution, runtime code, app launch or hardware operation occurred.
