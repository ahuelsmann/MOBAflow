# Research and design decisions

**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/306

**Evidence base**: repository `6888b0d9`, read-only inspections by Lokfuehrer, Entdecker and Pruefer; Brain scope agreement. No external library facts or new dependencies are needed.

**Implementation refresh (2026-10-16)**: After the explicit green ownership handoff, the assigned engineer verified the task worktree/branch, sole ownership of the nine existing feature files and GitHub remote. Fetch and live main both resolve to `fb63e8fde5f6aa299a3f3478560e7277f8661a90`; the task branch was fast-forwarded there without touching those artifacts. Actual local owner, clone/activation lock, session events, diagnostics and desktop registration entry points were inspected at this base. #219 and #304 remain in their separate worktrees; no unmerged feature code is imported and only the assigned engineer writes this physical checkout.

**Later targeted check**: GitHub compare from this base to `fb63e8f` (#301) and Pruefer's matching local-file inspection show Common/Runtime/RuntimeHubContract.cs introduces typed host/remote methods and callbacks, replacing RuntimeHubMethods.cs. RuntimeHub, host/remote clients and SolutionController adopt those types. The wire operations still publish snapshots, notify solution changes and route commands; no owner-usage query, extra solution/runtime provenance or B/C coverage is added. ProjectRuntimeHost/Factory, RuntimeApi, ActiveProjectContext and their local interfaces are unchanged in that delta. Therefore the local A design remains applicable and remote evidence remains UnsupportedRemote. No whole-current-main verification, remote extension or product validation is claimed.

## R1 - Reuse the existing diagnostics path

Decision: compose the new relationship rule through Backend ProjectDiagnosticsService and existing desktop Messages.

Evidence: `Backend/Service/Validation/ProjectDiagnosticsService.cs:18` defines stable Id/Severity/Source/Message/TargetIds; `:52` aggregates specialized validators. `SharedUI/ViewModel/MainWindowViewModel.Diagnostics.cs:71` replaces active results; desktop MainWindow.xaml displays them. TargetIds alone do not encode an exact typed vehicle-entry location, and Messages has no existing click-through object navigation.

Rationale: keep one diagnostics presentation and add only necessary occurrence/source/coverage metadata.

Rejected: a new diagnostics platform, message-string parsing, a generic rule/plugin registry or claiming navigation already exists.

## R2 - One explicit relationship

Decision: scan locomotive-kind Vehicle entries against the same project's locomotive ID set; use this relation for both incoming uses and missing targets.

Evidence: `Domain/Vehicle.cs:8`/`:10` stores VehicleId/VehicleKind; `Domain/Train.cs:57` stores ordered Vehicles. `SharedUI/ViewModel/WorkflowLibraryViewModel.cs:655` is an explicit incoming-reference pattern, not a generic engine. `Backend/Service/ProjectValidator.cs:203` already checks missing workflow targets for journey entries.

Rationale: avoid duplicated knowledge and retain repeated occurrences with project/type isolation.

Rejected: reflection/graph machinery, resolving by train/locomotive name or address, adding workflow relationships to this slice, altering Delete policy.

## R3 - Actual runtime A, unknown B/C

Decision: query current loaded definitions at the existing runtime owner (A); label retained definitions (B) and operative usage (C) separately unknown.

Evidence: `MobaRuntimeService.RuntimeApi.cs:65` clones input; `:91` serializes definition changes; `ActiveProjectContext.cs:29` holds the actual definition. `ProjectRuntimeFactory.cs:182` exposes the original input project, not that clone. `ProjectRuntimeHost.cs:55` includes non-selected owners; connection status does not delimit loaded definitions. `Common/Runtime/MobaRuntimeSnapshot.cs` has fleet/telemetry but no complete train-use proof.

Brain explicitly confirmed the bounded A/B/C choice. No queued RequestFactory invocation, trace interpretation or fleet/speed inference is allowed. General B/C instrumentation would expand scope without solving an in-scope requirement.

## R4 - Capture and validate existing identities

Decision: host-gate -> runtime-activation synchronization; immutable copied findings and an opaque observation receipt; revalidate owner/definition identity before projection.

Evidence: host Load/Remove/Replace use `_gate`, while `ProjectRuntimeHost.cs:197` DisposeAsync does not. Runtime UpdateProjectAsync uses `_projectActivationLock`. A cancellation token or timestamp alone cannot reject a successfully answered query after same-ID owner replacement/disposal.

Rationale: a small receipt can compare existing object identities, with no persistent runtime generation, lease or lifecycle framework. Result time is observation time, never a continuous guarantee or transaction reservation.

Rejected: trusting the project GUID, Solution reference or only cancellation; exposing mutable owner models; holding locks while dispatching UI; probing RequestFactory.

## R5 - Session and UI invalidation

Decision: use existing SolutionReplacing/SolutionLoaded, model/selection changes and all-project runtime snapshot notifications as invalidation hints. Revalidation reads receipt validity, not another relationship scan on every telemetry event.

Evidence: `ISolutionSession.cs` documents one Solution instance replaced in place; `SolutionSession.cs:445` applies a load and `:481` signals replacement. Its model-change path `:416` updates runtimes asynchronously before notifying/saving. Existing all-runtime snapshot notifications are raw .NET callbacks, distinct from UI-dispatched EventBus handlers.

Rationale: handle same-object reloads and lagging runtime updates without new global lifecycle state. Marshal only raw callback UI work via existing IUiDispatcher.

## R6 - Preserve specialized validation boundaries

Decision: keep JSON/input/workflow execution/mutation checks in place; adapt structured results when needed, not all validators in this first slice.

Evidence: JsonValidationService validates basic JSON/version before deserialization; IoService invokes ProjectValidator after load. IWorkflowValidator already returns codes/action IDs/field paths; WorkflowService checks actual requests before execution. RuntimeCommandValidator guards input ranges. ITimetableEvaluationService is a useful pure evaluator, but TimetableStateStore.GetStatesAsync may release expired holds and save.

Rationale: neither a prior diagnostic nor an apparently read-like method is automatically safe at a different boundary. Do not call mutating timetable operations from this query.

## Planning outcome

Architecture correction on 2026-10-16: MobaWinUiServiceCollectionExtensions registers ProjectRuntimeHost and its aliases; the Android composition calls AddMobaBackendServices without that host. Therefore common Backend registration owns only the pure analyzer. The reader alias and focused usage projection belong in the Windows composition root, reuse the same existing host singleton and are checked by Windows DI tests; shared analyzer resolution is checked by Backend DI tests. No second host or Android dependency is introduced. Plan/T024 reflect this correction before implementation.

No unresolved in-scope clarification remains. The contract describes new future methods rather than pretending existing APIs cover A. B/C completeness and atomic Delete coordination are explicit non-goals, not hidden prerequisites. The constitution's old runtime examples are resolved against current AGENTS.md without editing governance.
