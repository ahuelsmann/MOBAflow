# RF-27: Move domain rules into the domain model

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/339
**Spec Kit**: Not applicable - structural refactoring without behavior change
**Status**: Active. Slices #341 and #346 are delivered. The `Backend`/`SharedUI` audit is done; its move and stay
proposals below still need to be recorded on #339. This plan was restored after the post-merge review of #351, which had removed it too early.

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

Audited on main `f37541d9`: all of `Backend` and `SharedUI`. The search covered static helpers whose first
parameter is a domain object, ID lookups on domain collections, `Validate` methods and their callers, and repeated
range checks. The inventory below is bounded. Each decision is a proposal until it is recorded on #339.

### Proposed moves

| Candidate | Current behavior | Proposed move | Needed in the slice |
| --- | --- | --- | --- |
| `Backend/Manager/JourneyManager.cs` `GetCurrentStation` (lines 329-340), the stop lookups at lines 154-156 and 379-381, and the checkpoint lookup at line 173 | Each place repeats the rule of `Journey.IndexOfStop`: find the stop by ID, otherwise use a fallback position (`-1` at lines 156 and 173) | Call `Journey.IndexOfStop(id, fallback)` in all four places. The checkpoint lookup with a missing ID already returns `-1`, so behavior does not change | Tests of the current-station lookup, sync after an edited journey and checkpoint restore, each with an unknown ID |
| `Backend/Service/AnnouncementService.cs` `ResolvePlatformNumber` (lines 221-233) and `SharedUI/TrainControl/JourneyStationProjection.cs` `ResolvePlatform` (lines 67-74) | Both resolve the platform of a `Station`: the platform named by `PlatformId`, otherwise the first platform | Add one `Station` method that returns the effective `Platform?`. The announcement uses its `Number` and the projection formats it | Characterization tests: platform found by ID, unknown ID falls back to the first platform, no platforms |
| `Backend/Service/LocomotiveWhistleAutomationService.cs` `Validate` (lines 45-60) | Checks one `LocomotiveWhistleRule` (locomotive ID, feedback port, function F0-F31, delay, active duration) and returns message strings. Its only production caller is the same service (line 132) | Move the checks to `LocomotiveWhistleRule` as issue records. The English messages stay in the Backend, like `WorkflowValidator` after #346 | Characterization tests of the current messages, one per check |
| `SharedUI/TrainControl/FunctionPanel.cs` `SetSymbol`, `SetAppearance` and `ClearAppearance` (lines 138-186), with the `"none"` marker read by `SharedUI/Appearance/LocomotiveFunctionAppearanceResolver.cs` | Edit the stored function symbol and color lists of one `Locomotive`: index range F0-F31, growing the list and marking a cleared slot | Move the storage rule (range, list growth, cleared marker) to `Locomotive`. The default assets and colors are presentation and stay in SharedUI | Tests of set, clear and out-of-range indexes on a locomotive with short or missing lists |

The F0-F31 range is checked separately in `Backend/Z21.Drive.cs` (line 61), `LocomotiveWhistleAutomationService`,
`LocomotiveFunctionAppearanceResolver` and `TrainControlViewModel` (lines 1069, 1079, 1151 and 1528). The
`Locomotive` slice should define the range once in the Domain and reuse it in those places. The Z21 protocol check
can keep its own bound if the slice finds that the protocol limit and the domain limit are different facts.

### Proposed stays

| Candidate | Reason it stays |
| --- | --- |
| `Backend/Service/DecoderCvService.cs` `Validate` (lines 38-57) | Checks an input list (editing or import) against a protocol before it becomes part of a snapshot. Its row numbers belong to that input, not to a domain object |
| `Backend/Service/WorkflowEffectPlanner.cs` `Plan(WorkflowAction)` | Produces Backend effect plans with effect categories, resource keys and effect requests. The payload checks are tied to building those requests; the action structure rules are already in `Workflow.FindStructureIssues` |
| `Backend/Service/Validation/ProjectDiagnosticsService.cs` `AddTrackPlanFeedbackDiagnostics` (lines 151-183) | Checks uniqueness across all feedback points of the project and produces user-facing project diagnostics, like the known stays above |
| `TimetablePageViewModel` platform and call lookups, and the ID lookups in other ViewModels | Navigation and selection by ID, without a rule of their own |

## Completion

RF-27 is complete when:

1. the `Backend`/`SharedUI` audit is done, the remaining inventory is bounded and each candidate has a justified
   move or stay decision recorded on #339;
2. approved moves are delivered with their characterization tests and current CI and Sonar gates;
3. the Domain layer documentation matches the result, and this plan is then removed.
