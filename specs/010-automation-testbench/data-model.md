# Data Model: Isolated Automation Testbench

**Source Issue**: #35
**Status**: Proposed; no serialized model/schema changes are implemented in this planning PR.
**Related**: [specification](spec.md), [runtime contract](contracts/isolated-runtime.md).

## Saved definitions

Add `Project.AutomationScenarios : List<AutomationScenario> = []`. The containing project is authoritative ownership;
do not persist a redundant project ID. Empty/missing collection means no scenarios. Use existing camelCase Domain JSON
options, explicit typed discriminators for input/expectation variants and the current solution schema version 4.
No run state/result/history, compatibility code, migration or example data is stored.

| Entity | Fields / relationship | Validation |
| --- | --- | --- |
| AutomationScenario | Id: Guid; Name: string; selected journey/workflow GUID lists; InitialState; Inputs; Expectations; PlaybackSpeed: double, default 1.0 | Nonempty unique identity; trimmed nonempty project-unique name (ordinal ignore-case); no duplicate selections; speed finite and positive |
| InitialState | CounterSeeds; JourneyStates | Exactly one state for every captured journey; no inherited activation or unrecognized references |
| CounterSeed | InPort: uint; Count: ulong | Supported nonzero InPort; unique port; unspecified ports initialize to zero; no reset/feedback emitted |
| JourneyInitialState | JourneyId; IsActive; CurrentStopId: Guid?; CurrentStopIndex: int? | Nonempty journey: both stop fields resolve to the same zero-based stop; empty journey: both null |
| SyntheticInput | Id: Guid; AtTicks: long; Order: int; Kind: Feedback; payload { InPort: uint, IsActive: bool } | Identity unique within scenario; nonnegative ticks/order; unique (AtTicks, Order); payload structurally valid |
| EffectExpectation | Id; typed effect predicate; optional inclusive time window; optional occurrence constraint | Predicate fields legal for its kind; referenced IDs resolve; default positive exact count 1 |
| InvocationExpectation | Id; workflow/journey predicate; optional window/count | Known workflow; runnable selection rules are separate; default positive exact count 1 |
| StateExpectation | Id; Initial/Final/Transition scope; journey/counter reference; selected expected fields; optional window/count for transitions | State field/type valid; snapshot assertions evaluate once, not once per repeated projection |
| AbsenceExpectation | Id; effect/invocation/transition predicate; optional window | Exactly zero matches; cannot also specify a positive count |
| OrderedGroupExpectation | Id; ordered slots referencing occurrence expectations | Distinct scenario-owned references; no cycles/nested groups; positive or zero-allowing counts; disjoint matches across slots |

AtTicks uses 100-nanosecond TimeSpan ticks; editor shows readable milliseconds/seconds without floating-point scheduling.
Use checked deadline arithmetic; values that cannot map to the virtual clock's supported DateTimeOffset range are invalid.
Do not restrict speed through a new persisted application setting; 1.0 is the scenario editor's default.

Runnable selected workflows require an enabled event entry in a selected initially active journey. All captured active
journeys still react, including unselected ones. Absence expectations may name known, unassigned workflows.
Assignments, locomotive addresses and train definitions come from the captured project; no train runtime state is seeded.

For nonempty journeys, stored index 1 maps to session CurrentPos 1 and action CurrentStationIndex 2. For empty journeys,
stop identity/index are null, internal CurrentPos is 0 and action context has no current station with fallback index 1.
Derive stop names; never persist a second name or invent a nonempty journey's no-stop-reached state.

## Capture and duplication

Capture only runtime project definitions, excluding all scenario collections. Deep-clone with existing Domain JSON options
and retain stable project/journey/workflow/action IDs. Seed private state separately. The clone may contain configured script
arguments required by shared validation, but no run-output projection may copy or hash them.

Duplicate scenario definitions deeply, assigning fresh scenario/input/expectation/group identities and remapping references
among those owned items. Preserve project/journey/workflow/action/stop identities. Source and copy share no mutable lists.
An added or removed journey makes an old initial-state definition invalid until explicitly reconciled; do not infer state.
Capture at run start is immutable for that run. Restart validates/captures afresh from current definitions.

## Run-owned objects (never solution data)

| Entity | Fields / purpose |
| --- | --- |
| AutomationRun | Opaque RunId; captured scenario/project; virtual clock; scheduler; cancellation ownership; state |
| WorkHandle | StableWorkKey; owner; Ready/Running/WaitingTimer/WaitingChildren/Terminal; retained outcome |
| TraceEntry | Global logical sequence; virtual ticks; typed kind/outcome; input IDs; journey/workflow/action/rule IDs; safe payload/state delta |
| ComparisonOutcome | ExpectationId; Passed/Mismatch; matched trace sequences; safe expected/actual differences |
| RunResult | Terminal category; all comparison outcomes; first failed expectation ID; full available trace; safe error code/correlation |

StableWorkKey derives from input order, captured journey/event/rule order, invocation ordinal, action/effect positions and
parent/child registration ordinal. It must not depend on random IDs or thread completion order.
GUIDs provide references/correlation, not tie-breaking. Normalize only opaque run/correlation identities across repeated runs.

Initial/final counter/journey snapshots are typed, distinct from transition entries. Coalesced whistle effects retain all
contributing inputs/rules, including extensions. Script effects expose opaque action identity, sanitized project-relative
or leaf path and a constant ArgumentsRedacted marker; no raw argument or derivative. Announcement/text assertions must
use the safe projection policy, never full context/project serialization. Errors expose codes/IDs, not raw exceptions.

## Runner state transitions

| Current | Operation / event | Next |
| --- | --- | --- |
| Idle or terminal | Play / Restart | Validating, then Running if valid |
| Idle or terminal | Single step | Validating, then process initial/next instant, then Paused or terminal |
| Validating | Invalid definition | InvalidScenario; no graph/input/effect |
| Running | Pause | Finish current instant, then Paused (or terminal if already complete) |
| Paused | Play | Running; resume future progression |
| Paused | Single step | Process exactly next instant, then Paused or terminal |
| Validating / Running / Paused | Cancel / departure | Cancelling, then Cancelled after owned drain |
| Running / stepping | No remaining inputs/work | Compare; CompletedWithoutExpectations, Passed or Mismatch |
| Any executing state | Shared executor failure | Drain/capture, then ExecutionFailed |
| Any executing state | Subscriber/scheduler/capture/internal failure | Drain/capture, then TestbenchError |

Invalid controls return a recoverable control error without changing state. Serialize control admission, but do not hold the
control gate while waiting for future time; Cancel must interrupt playback waits. Repeated Cancel joins the same drain.
Restart first disposes the former run completely. CompletedWithoutExpectations never implies verified expected behavior.
Execution/cancellation/internal errors take precedence over comparisons and never become Passed.

## Comparison semantics

A predicate specifies kind and optionally identities/selected safe fields. Missing fields are wildcards, not implicit
defaults. Time windows are inclusive and use virtual ticks; no window covers the entire trace. Counts cover every matching
entry in that window. Positive effect/invocation/transition expectations default to exactly one; explicit Exact(n) or
Range(min,max) replaces it. Range bounds are nonnegative/inclusive and min <= max; absence is zero.

Evaluate Initial/Final state once at their named boundary. Transition assertions count actual changes, not UI snapshots.
Independent expectations may refer to the same trace entry. Within an ordered group, all matches for each slot are
disjoint from every other slot. After counts pass, every match in a preceding nonempty slot must precede every match in
the next nonempty slot by logical sequence. Zero-match slots have no ordering position. Thus A(count=2),B(count=1)
rejects A,B,A and accepts A,A,B; unrelated entries between slots are allowed. Overlapping slot predicates that select the
same entry fail rather than using a greedy subset. Report first failure in saved expectation order and retain all outcomes.

No silent truncation: reserve a complete run-owned trace rather than using the bounded production WorkflowTraceStore.
Any capture/work resource ceiling failure is TestbenchError with retained partial evidence, never Passed.
