# RF-27: Move domain rules into the domain model

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/339
**Spec Kit**: Not applicable - structural refactoring without behavior change
**Status**: Active; slice 1 ships with this plan

## Purpose and scope

[Issue #339](https://github.com/ahuelsmann/MOBAflow/issues/339) owns acceptance and live status.
The [umbrella plan](QUALITY-AND-REFACTORING-PLAN.md) owns programme dependencies. This is the single RF-27 child plan.

Target: rules that need only domain data live in the domain class that owns the data. Services keep runtime
state, persistence, time, hardware and presentation, and delegate the rule. `Domain` gains no new dependency;
methods do not change the JSON shape, because `System.Text.Json` serializes properties only.

## Source inventory and decisions

Inventory on main `b9bf9d72`:

| Rule | Current owner | Decision |
| --- | --- | --- |
| Which stop a "next stop" / "go to station" transition reaches; which stop is current | `Backend/Service/JourneyStopTransitionService` (also writes `JourneySessionState`) | **Slice 1**: `Journey.ResolveStopTransition` and `Journey.IndexOfStop`; the service only reads and writes the runtime state |
| Workflow structure: unique non-empty workflow and action ids, at least one action, non-negative delay | `Backend/Service/WorkflowValidator` | **Slice 2**: `Workflow` reports its structure issues; `WorkflowValidator` keeps the payload/effect checks (`WorkflowEffectPlanner`) and maps both into `WorkflowValidationResult` |
| Digital address conflicts and ranges | `Backend/Service/Validation/DigitalAddressConflictDetector` | **Stays**: it needs the multiplexer catalogue from `Common` (`IMultiplexerProvider`) and builds presentation messages; `Domain` must not reference `Common` |
| Remaining candidates in SharedUI ViewModels and Backend services | various | **Slice 3** inventories them and moves only rules that pass the same test |

A rule qualifies when it reads only domain objects, has no I/O, time or hardware effect and returns a value
or a domain exception. Runtime session state (`JourneySessionState`) is not domain data and stays in Backend.

## Independently reviewable delivery slices

1. **Journey stop transitions** (with this plan). Characterization tests in `Test/Domain/JourneyTests.cs`;
   the existing handler, journey and workflow-sequence tests stay unchanged and green.
2. **Workflow structure rules.** Domain tests for the structure issues; existing `WorkflowValidatorTests`
   stay unchanged and keep codes, paths and messages.
3. **Remaining inventory and completion.** Further qualifying moves, `docs/ARCHITECTURE.md` (Domain layer),
   the umbrella plan delivery note, removal of this plan.

## Validation strategy

Every slice: portable and Windows test suites and the cross-platform, Windows and Android analyzer gates
(`Domain` is part of the Android build). The Domain mutation lane covers the moved rules. No app launch.

## Risks

- **Open PRs:** #330 (RF-25 slice 2) changes `JourneyManager` and its factories; slice 1 touches only
  `JourneyStopTransitionService` and `Journey`, so there is no overlap.
- **Behavior drift:** each move keeps the exact branch order of the old code; the characterization tests
  cover the edge cases (unknown current stop, last stop, foreign station).
