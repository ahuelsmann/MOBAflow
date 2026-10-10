# Issue #142: modular feedback preparation

**GitHub Issue**: #142
**Spec Kit**: Required
**Status**: Preparation only; no module architecture or interface is selected.

## Baseline and start conditions

Inspected on 2026-10-11 against `github/main` at `9b03676a9865275711d4031815f26a2d35e2474b`.
#142 and the architecture decision [#141](https://github.com/ahuelsmann/MOBAflow/issues/141) are open.
No #142 feature specification exists at this baseline. RF-23 [#191](https://github.com/ahuelsmann/MOBAflow/issues/191)
has open draft PRs #207, #211 and #217. The per-project factory and counter-store wiring from #207 are not in main.

Implementation requires the accepted #141 decision on feedback contracts, module boundaries, registration and lifetime,
and the integrated RF-23 per-project runtime/counter-store boundary. Complete and analyze the #142 Spec Kit artifacts
against that base before changing production code. #141 follows RF-24 [#219](https://github.com/ahuelsmann/MOBAflow/issues/219)
and the modularization analysis #135; this plan does not perform or decide that work.
Delete this standalone preparation plan once the feature is delivered and accepted.

## Confirmed behavior to preserve

- Each accepted feedback has a logical InPort, the newly reached count and a timestamp; RFID/camera may add vehicle identity.
- Counters belong to the project's runtime, persist in a separate project store and resume after restart.
  They do not belong in `solution.json` or solution Undo/Redo. New or explicitly reset counters start at zero;
  restarting an existing project does not reset its counts.
- An active journey matches the directly reached InPort count. Activation creates no start count or relative offset.
  Inactive journeys do not run event workflows but their project's InPorts continue counting.
- Setting a counter to 10 is a correction, not feedback. Two later accepted inputs reach 11 and 12.
  A configured event at 12 fires on the second input; corrections do not synthesize workflows.
- Workflow actions and journey stop transitions do not implicitly reset counters.
- Actual RFID hardware, camera/AI providers and deployment are separate follow-up work.

The zero-on-restart statements in #142 are superseded by the maintainer's explicit persistence requirement.
Do not use them to remove `IInPortCounterStore` or to initialize a recreated runtime with empty counts.

## Current processing seams

| Stage | Verified current source | Implication for the specification |
| --- | --- | --- |
| Z21 packet parsing | `Backend/FeedbackResult.cs`, `Test/Backend/Z21FeedbackParserTests.cs` | Packet-oriented feedback includes InPort, active group inputs and correlation; it is not yet a technology-neutral contract |
| Acceptance and counting | `Backend/Service/InPortCounterService.cs` | One increment owner, configured InPort range, timer filter, saturation protection, serial publication and stale-activation protection already exist |
| Restore and save | `Backend/Service/InPortCounterService.Persistence.cs`, `FileInPortCounterStore.cs`, `Backend/Interface/IInPortCounterStore.cs` | Load before counting, buffered arrivals, visible load/write failures, explicit reset recovery and atomic saves must survive the extension |
| Runtime projection | `Common/Runtime/InPortCounterSnapshot.cs` | Existing fields are uint InPort, ulong Count, nullable DateTimeOffset last-feedback time and lap duration; these are baseline facts, not a final interface decision |
| Journey matching | `Backend/Manager/JourneyManager.cs`, `Domain/JourneyEventPlan.cs` | Matches enabled event entries of active journeys by exact InPort/count and queues the referenced workflow |
| Workflow context | `Backend/Manager/JourneyManager.cs`, `Backend/Interface/WorkflowExecution.cs` | Existing source correlation and event-definition identity must remain available; end-to-end optional vehicle identity needs specification |
| Runtime ownership | `SharedUI/Service/SolutionSession.cs`, RF-23 `specs/008-project-runtimes/` | Adopt the final project-owned services and routing; do not create a second counter or runtime owner |

`FileInPortCounterStore.cs` in the table is under `Backend/Service/`.

## Questions reserved for #141 and the #142 specification

1. Where does the shared source contract live, how are modules registered, and which capabilities and disposal/cancellation
   obligations does #141 require? Separate libraries, dynamic plugins and runtime provider switching are not decided here.
2. How does each module instance map its technical addresses to logical project InPorts?
   Reject ambiguous mappings; define intentional sharing before combining observations of one physical event.
3. What constitutes one accepted occupancy transition, RFID reading or camera observation?
   Define duplicate/reconnect/late-event rules while preserving the actual Z21 acceptance behavior.
4. Which timestamp is authoritative: device event time or host receipt time? Define time zone/clock skew, missing device time
   and ordering independently from display formatting. Do not add speculative event IDs, reception fields or confidence metadata.
5. How is optional stable vehicle identity mapped and propagated? Unknown, ambiguous and confident identification must remain
   distinguishable where conditions need it; do not couple journey logic to provider-specific tag IDs.
6. Where are accepted observations normalized and counted exactly once?
   Providers must not increment a count again after the central owner has accepted the same observation.
7. How are persistence failures, overflow, cancellation, module errors and per-project reconnect represented to the operator?
   Stored counts may not silently become zero on load failure.

These questions form inputs to the responsible architecture/specification work, not accepted answers.

## Small delivery slices after the gates

1. Specify the baseline and selected contract using the #141 decision. Add the minimal normalization boundary and adapt
   existing Z21 occupancy feedback; preserve its acceptance and persistence tests. Remove superseded code in that slice.
2. Supply simulated RFID and camera sources using the same contract. Prove multiple instances, mapping, exactly-once counting
   and optional identity with no hardware or cloud credentials. No concrete device/AI integration is included.
3. Connect accepted metadata to the existing journey/workflow context and local/remote runtime projections as specified.
   Test project addressing and update operator documentation. Each slice remains a narrowly scoped draft PR.

## Acceptance test matrix to carry into Spec Kit

| Scenario | Required observation |
| --- | --- |
| Restart project A with stored count 10, then accept two inputs | A reaches 12; only its active event at 12 executes; project B is unchanged |
| Switch UI project / toggle journey activation / run workflow | No counter reset; only active journeys match |
| Two modules with equal technical addresses | Explicit unambiguous logical mapping; no accidental collision or doubled increment |
| Repeated reading, subsequent genuine reading, reconnect and delayed input | Follow the specified acceptance/order rules; preserve valid follow-up events |
| Known, unknown or ambiguous vehicle | Identity preserved; an identity-specific condition cannot match an unknown vehicle |
| Load still running / load failure / save failure | Preserve buffered-input ordering and failure reporting; do not overwrite unreadable counts |
| Set to 10 / reset / stale queued input / saturated count | Correction emits no feedback; obsolete work is rejected; ulong saturation does not wrap to zero |
| Local/remote command with project ID | Reaches only the named project's runtime and counter store |
| Solution Undo/Redo | Definition edits replay; current persisted counts and hardware state remain outside history |

Use `InPortCounterServiceTests`, `PersistentInPortCounterTests`, `JourneyEventPlanTests`,
`JourneyEventPlanAcceptanceTests` and the final RF-23 routing fixtures as regression anchors.
Run the relevant full target suites and affected consumers for this runtime/persistence/protocol change;
hardware checks remain separately authorized. Green current-head CI and SonarCloud with zero OPEN/CONFIRMED issues precede readiness.

## Preparation validation

Documentation only: secrets scan, line endings, internal links, Spec Kit governance and diff review.
No .NET behavior, package, schema, architecture decision or hardware action is changed by this plan.
