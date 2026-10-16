# Runtime query contract: locomotive-to-train references

**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/306

**Status**: Implemented internal read contract; final platform/review evidence is recorded separately.

## Purpose and placement

The `Backend/Interface/ILocomotiveTrainRuntimeUsageReader.cs` contract is a narrow read role implemented by the existing `ProjectRuntimeHost`. Backend performs identity resolution and reads the actual runtime owner. SharedUI never acquires a runtime/connection/command role to perform this query. It does not acquire a mutable runtime Project.

The public semantics are two operations, not implementation bodies:

- `ReadAsync(request, cancellationToken)` returns one immutable source observation for one expected project and optional target locomotive.
- `IsCurrentAsync(observation, cancellationToken)` returns Current, Stale or Unavailable after checking the opaque receipt. It neither scans relationships nor refreshes a runtime.

These are separate new read contracts; IProjectRuntimeHost/IProjectRuntimeSnapshots retain their existing roles. No HTTP/SignalR endpoint is added.

## Request

| Member | Meaning and constraints |
| --- | --- |
| RequestId | Non-empty request-local correlation ID; never persisted or treated as runtime identity |
| ExpectedProject | The project object belonging to the caller's current loaded solution; used only for membership/identity checks, never as runtime definition data |
| ProjectId | Must be non-empty and equal ExpectedProject.Id |
| LocomotiveId | Optional non-empty target ID; absent means analyze all direct locomotive entries in this one project, including missing targets |
| Cancellation | Linked to the caller's current query/load context; replacement, target changes and disposal cancel it |

The caller captures the editor's immutable reference result on its established model/UI thread before awaiting. Existing session events bind the request to the loaded solution; Solution object equality alone is insufficient. The reader resolves only a matching existing owner whose original editable-project identity matches ExpectedProject. ProjectRuntime.Project may establish that identity but MUST NOT supply runtime occurrence data. A foreign/reloaded object with the same GUID is not a match.

For a solution-wide pass, the caller issues one scoped request for each project in that loaded solution. Equal target IDs across projects remain distinct. The reader never discovers projects from UI selection, ConnectedProjectIds or remote fleet lists. Requesting a non-selected or disconnected existing owner is valid.

## Observation

| Member | Meaning |
| --- | --- |
| RequestId, ProjectId | Echo validated request identity |
| ObservedAtUtc | Time of the copied observation, or check time for Unknown evidence; not a freshness guarantee |
| TargetState | NotRequested, Exists, Missing or Ambiguous in this source |
| Findings | Immutable copied occurrence/finding values; never Project/Train/Vehicle/runtime objects |
| LoadedDefinitionCoverage | Complete or Unknown, with reason and observation receipt |
| RetainedDefinitionsCoverage | Unknown / NotInspectedInSlice |
| OperationalUsageCoverage | Unknown / NotInspectedInSlice |
| Receipt | Opaque backend-owned, short-lived binding to the existing host, owner and active context/definition objects; not serializable or user-editable |

An empty findings list is allowed with A Complete, but means only that the checked current loaded definition has no matching train entries. B/C remain unknown, so aggregate runtime evidence is at most Partial. A Unknown makes the aggregate Unknown. There is no CanDelete, IsUnused or safety/free flag.

## Capture algorithm and synchronization

1. Reject invalid request identity; honor cancellation before accessing a runtime. No state is changed to make a request valid.
2. Use the existing host gate for the owner lookup/capture sequence, with host membership checks. Acquire locks in host-gate then runtime-activation order. No UI dispatch or callbacks while held.
3. Resolve the already existing matching owner; do not call Load/Add/Replace/Update/Start or use an unconnected-owner shortcut. An owner can be disconnected yet have valid loaded definitions.
4. In the existing MobaRuntimeService, acquire `_projectActivationLock`, verify active context/project identity and copy findings from `ActiveProjectContext.ActiveProject` through the pure analyzer. Materialize names, IDs, positions and target states before releasing it.
5. Store only the existing object identities in the receipt and copied values in the response. No new generation field, persistent stamp, per-run registry or lease is introduced. Do not execute queued RequestFactory delegates.
6. Recheck host membership/owner identity and cancellation before returning. A replaced/removed/disposed owner is not Complete. Existing Host.DisposeAsync is not serialized by `_gate`, so gate acquisition alone is not proof: disposal/detachment races must be caught and downgraded or the observation discarded.
7. Release only locks actually acquired. Cancellation propagates as cancellation; known disposal/unavailability has a structured Unknown reason. Other unexpected failures are observed/logged through the existing error path and surfaced as QueryFailed, not silently converted into successful emptiness.

The shared analyzer reads one source definition only. It does not inspect retained older workflow projects or active physical movement. A consistent capture is a historical observation, not a transaction reservation.

## Revalidation and projection

`IsCurrentAsync` checks the same host, current dictionary membership, owner identity, active context/definition identity and accessibility against the receipt using the existing synchronization. It does not trust matching ProjectId, timestamp, TargetId, connection state, fleet or a trace. A foreign/reused receipt is Stale; disposal or unsupported access is Unavailable. It performs no relationship scan, I/O or lifecycle action.

The projection also verifies that its request-local context, target and current project still match. It discards stale answers, including stale revalidation answers, and must revalidate immediately before applying an otherwise successful observation. A later change can make even a checked observation obsolete; labels therefore state its observation time, and existing load/model/runtime-change invalidation triggers removal/refresh rather than promising continuous freshness.

Use SolutionReplacing/SolutionLoaded, ModelChanged and selected project/locomotive changes to cancel/replace the request context. In-place load reuses the Solution object, so the load event must invalidate even with equal object references/IDs. Observe the existing all-project runtime snapshot event as an invalidation hint: revalidate the affected receipt, rather than assuming the snapshot proves usage or blindly rescanning on every telemetry packet. Do not treat the selected-runtime-only event as coverage of all owners.

Raw all-runtime .NET callbacks are background callbacks; cancellation can occur directly, but collection/property changes are marshalled through the existing `SharedUI/Interface/IUiDispatcher.cs`. Existing UI-dispatched EventBus handlers receive no extra dispatch. Coalesce pending receipt checks for a source; only the latest query context can replace its result. Unsubscribe and cancel when the projection is disposed.

## Failure and coverage table

| Situation | A | B/C | Presentation |
| --- | --- | --- | --- |
| Matching owner captured and receipt current | Complete | Unknown | Exact occurrences or source-qualified zero |
| No corresponding owner | Unknown: OwnerUnavailable | Unknown | Visible unavailable evidence |
| Same GUID but different loaded context/project object | Unknown: ContextMismatch | Unknown | Never reuse the other context |
| Owner/definition changed after success | Stale receipt | Unknown | Discard old Complete; request refresh |
| Host/owner disposed during capture/validation | Unknown: OwnerUnavailable | Unknown | No successful empty result |
| Remote route has no owner-query contract | Unknown: UnsupportedRemote | Unknown | No endpoint or local-runtime substitution |
| Query fails | Unknown: QueryFailed | Unknown | Keep failure visible; successful editor result remains separate |
| Query cancelled/target changed/reload | No result applied | No result applied | Old response discarded; new context checked independently |

## Required race controls

- Successful capture, then ReplaceAsync with the same project ID before UI apply: old receipt rejected.
- Successful capture, then Host.DisposeAsync before UI apply: old receipt rejected even though disposal bypasses `_gate`.
- Same Solution instance loaded again, same project/target IDs: prior query context cancelled and old results discarded.
- Definitions replaced during/after capture: immutable old or new result, never a mixed result; changed definition receipt rejected before apply.
- Unchanged owner/context/definition: positive control accepted.

Tests of fake result semantics do not replace the real owner-adapter integration tests. A-Complete is allowed only after the real capture path succeeds. B/C remain unknown even if fakes could fabricate complete results. Full retained/operative-use detection and later atomic Delete validation are separate work.
