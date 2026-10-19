# RF-27: Move domain rules into the domain model

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/339
**Spec Kit**: Not applicable - structural refactoring without behavior change
**Status**: Active. Slices #341 and #346 are delivered; the remaining inventory below has no recorded move or stay
decision yet. This plan was restored after the post-merge review of #351, which had removed it too early.

## Purpose and scope

[Issue #339](https://github.com/ahuelsmann/MOBAflow/issues/339) owns acceptance and live status.
The [umbrella plan](QUALITY-AND-REFACTORING-PLAN.md) owns programme dependencies. This is the single RF-27 child plan.

Target: a rule that needs only the data of one domain object lives on that object, without I/O, time or runtime
state; it returns values or domain issue records, or throws a domain exception for an invalid request. The Backend
turns results into user-facing findings (see [the Domain layer](../docs/ARCHITECTURE.md#1-domain-layer-domain)).

This plan only tracks the work. It does not authorize code changes; each move needs its own allocation.

## Delivered

| Slice | Delivered rule | Limit |
| --- | --- | --- |
| #341 | `Journey.IndexOfStop` and `Journey.ResolveStopTransition`; `JourneyStopTransitionService` applies the result to `JourneySessionState` | Covers the stop transition path only; see `JourneyManager.GetCurrentStation` below |
| #346 | `Workflow.FindStructureIssues` (`IReadOnlyList<WorkflowStructureIssue>`) and `Project.FindWorkflowsWithInvalidIds` (`IReadOnlySet<int>` of positions); `WorkflowValidator` produces the user-facing issues | Covers workflow and workflow-ID structure only |

## Known stays

These validators check consistency across many entities or need runtime or provider data and produce user-facing
findings. They stay in `Backend/Service`:

- `InterlockingDefinitionValidator`
- `DigitalAddressConflictDetector` (uses `IMultiplexerProvider`)
- `TimetableEvaluationService` (evaluates against runtime states)
- `ProjectValidator`

## Remaining inventory

Found on main `3871a6d0` after #351. No move or stay decision is recorded for these candidates.

| Candidate | Current behavior | Open qualification | Needed before a move |
| --- | --- | --- | --- |
| `Backend/Service/LocomotiveWhistleAutomationService.cs` `Validate` (lines 45-59) | Checks one `LocomotiveWhistleRule` (locomotive ID, feedback port, function F0-F31, delay, active duration) and returns message strings | Single-object rule, but it returns user-facing strings; decide whether the checks move to `LocomotiveWhistleRule` as issue records with the messages kept in the Backend | Characterization tests of the current messages and their callers |
| `Backend/Service/DecoderCvService.cs` `Validate` (lines 38-56) | Checks a list of `DecoderCvValue` for one `DecoderProtocol` (protocol-specific CV range, value 0-255, duplicates) and returns row/message records | Rule over a value list plus protocol, not one domain object; decide whether it qualifies and which domain type would own it | Characterization tests of ranges, duplicates and row numbers |
| `Backend/Manager/JourneyManager.cs` `GetCurrentStation` (lines 329-339) | Finds the current stop by scanning `Journey.Stations` IDs with a position fallback | Overlaps `Journey.IndexOfStop`; existed before #351. Decide whether it should call the domain method | Tests of the current-station lookup, including unknown IDs |

This inventory is bounded to the candidates above. A complete audit of `SharedUI` and `Backend` for further
single-object rules has not been done; record any further candidate here before acting on it.

## Completion

RF-27 is complete when:

1. the remaining inventory is bounded and each candidate has a justified move or stay decision recorded on #339;
2. approved moves are delivered with their characterization tests and current CI and Sonar gates;
3. the Domain layer documentation matches the result, and this plan is then removed.
