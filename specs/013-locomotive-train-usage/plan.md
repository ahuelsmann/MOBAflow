# Implementation Plan: Locomotive usage in train consists

**Branch**: `codex/issue-306-locomotive-usage-spec` | **Date**: 2026-10-15 | **Spec**: [spec.md](spec.md)

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/306

**Spec Kit**: Required

**Status**: Implemented after green same-worktree handoff; final platform validation, review and authorized Draft-PR delivery in progress

**Input**: Specification, Brain agreement and source inspection at `6888b0d9239bee3b623fbca0f6ae9d71fe7ec185`.

**Targeted main delta check**: Compared that base with `fb63e8fde5f6aa299a3f3478560e7277f8661a90` (#301). The typed SignalR contract in Common/Runtime/RuntimeHubContract.cs replaces string method constants but adds no usage query/provenance. Local owner/session/query attachment points are unchanged. UnsupportedRemote remains explicit; no network API change is planned. This is a targeted delta check, not a whole-current-main review or rebase.

## Summary

Add one explicit locomotive-to-train-entry analyzer; use it for incoming references and missing-target diagnostics. Reuse ProjectDiagnosticsService/Messages. A narrow reader on the existing project runtime host asks matching existing owners for immutable copied findings from actual active project clones. A is real loaded-definition evidence; B/C remain unknown. No Delete changes.

The original planning-only delivery was superseded by the user's implementation/test/Draft-PR instruction on 2026-10-16. [Runtime contract](contracts/runtime-query.md) now describes the implemented internal read API; task checkboxes record actual implementation and remaining evidence.

## Technical Context

**Language/Version**: .NET 10/C# repository defaults, SDK 10.0.402 at the base; global.json remains authoritative.

**Primary Dependencies**: Existing Domain models, Backend validators/owners, SharedUI session/dispatcher/CommunityToolkit, DI and NUnit/Moq fakes; no packages added.

**Storage/Protocols**: Loaded editor models and runtime clones; no file/network/Z21/REST/SignalR operations for queries.

**Testing**: NUnit/Moq, existing runtime-isolation/diagnostics/DI fixtures and new focused query fixtures.

**Target Platforms**: Portable Backend/SharedUI; Windows desktop Messages. Other hosts compile without new screens/endpoints.

**Affected Layers**: Backend/SharedUI plus minimal desktop composition/binding. No persisted Domain/Common model change.

**Performance Goals**: One materialized scan per project/source, linear in train entries plus locomotive IDs. Receipt validation compares identities without I/O or rescanning. No timing SLA before measurement.

**Data and API Effects**: Internal read contracts and structured finding metadata only; no schema/default/layout/protocol change or migration.

**Scale/Scope**: Exactly locomotive-to-train-entry references; no generic engine/full runtime-consumer tracking.

## Constitution Check

All checks pass for the proposed design before research and after contracts; these are not executed product checks.

- [x] Backend owns platform-neutral analysis/runtime reads; SharedUI owns projection; hosts own visuals.
- [x] Existing UI-dispatched EventBus handlers remain direct; raw runtime .NET notifications use the existing IUiDispatcher for UI work.
- [x] Await/cancellation preserved; no sync-over-async.
- [x] Commands/projection in ViewModel; future Light/Dark/keyboard checks planned; English labels.
- [x] Explicitly no JSON/default/protocol/layout/migration effects.
- [x] Meaningful identity, race, runtime, DI and zero-side-effect regression tasks planned.
- [x] Existing validators/owners/DI reused; no second runtime or service locator.
- [x] Spec/plan reference issue #306; artifacts remain below specs/.
- [x] Pre-read/changed-file secrets scans; invented inputs only.
- [x] Sonar code analysis CI-only; future PR draft until current-head success and zero OPEN/CONFIRMED issues.
- [x] No persistent generation, lease or additional lifecycle framework.

**Authority reconciliation**: Constitution 4.0.0 has older IMobaRuntime/CurrentSnapshot examples. Current AGENTS.md and architecture instructions prevail: narrow ViewModel read roles, IRuntimeCommandGateway for operator commands, snapshot property Current. Do not amend governance incidentally or copy stale examples.

## Project Structure

### Documentation (this feature)

`specs/013-locomotive-train-usage/`: spec.md, plan.md, research.md, data-model.md, quickstart.md, tasks.md, checklists/requirements.md and contracts/runtime-query.md plus contracts/presentation.md.

### Source paths

| Path | Responsibility |
| --- | --- |
| Backend/Interface/ILocomotiveTrainReferenceAnalyzer.cs | Pure relationship contract/results |
| Backend/Service/Validation/LocomotiveTrainReferenceAnalyzer.cs | Explicit VehicleKind/VehicleId traversal |
| Backend/Interface/ILocomotiveTrainRuntimeUsageReader.cs | ReadAsync/IsCurrentAsync and immutable observation/opaque receipt |
| Backend/Service/ProjectRuntimes/ProjectRuntimeHost.cs | Reader implementation on existing host; owner matching/revalidation |
| Backend/Service/ProjectRuntimes/ProjectRuntimeFactory.cs | Forward reads to existing runtime; Project is identity only, not runtime data |
| Backend/Service/MobaRuntimeService.RuntimeApi.cs | Consistent copy under existing activation synchronization |
| Backend/Service/Validation/ProjectDiagnosticsService.cs | Missing-target diagnostic adapter |
| Backend/Extensions/MobaBackendServiceCollectionExtensions.cs | Pure analyzer registration only; no runtime-host or reader alias |
| SharedUI/ViewModel/LocomotiveTrainUsageViewModel.cs | Read command, cancellation, validation and scoped projection |
| SharedUI/ViewModel/LocomotivesPageViewModel.cs | Selected-locomotive read integration; Delete unchanged |
| SharedUI/ViewModel/MainWindowViewModel.Diagnostics.cs | Existing Messages result merge, no parallel diagnostics platform |
| MOBAflow/View/MainWindow.xaml | Minimal existing Messages binding/labels if needed |
| MOBAflow/View/LocomotivesPage.xaml | Existing header/context-menu binding to the authorized Show usage command |
| MOBAflow/Extensions/MobaWinUiServiceCollectionExtensions.cs | Reader alias to the same existing ProjectRuntimeHost singleton, alongside existing host aliases; session/reader/dispatcher projection composition |
| Test/Backend/LocomotiveTrainReferenceAnalyzerTests.cs | Pure relationship matrix |
| Test/Backend/LocomotiveTrainRuntimeUsageReaderTests.cs | Runtime provenance/coverage/disposal matrix |
| Test/SharedUI/LocomotiveTrainUsageViewModelTests.cs | Async invalidation and Messages behavior |

SharedUI already references Backend; Backend references Domain. No new project/backwards dependency. Required services remain required. The pure analyzer is registered through AddMobaBackendServices. ProjectRuntimeHost is currently registered only in the Windows composition root; register the reader alias there beside its existing aliases and resolve the same singleton instance. Do not register that alias in common Backend registration, where Android has no ProjectRuntimeHost, or create a second host. The focused usage projection is composed only for the desktop consumer in this slice. The host passes the existing management and usage singleton projections together as LocomotivePageAnalysis to the locomotive page; this presentation pair introduces no runtime role, owner or service lifetime. Unsupported remote evidence is a result when a query is requested, not an optional missing DI dependency or an instruction to add a host to Android.

## Research and design

[research.md](research.md) records evidence/alternatives. [data-model.md](data-model.md) and [contracts/runtime-query.md](contracts/runtime-query.md) resolve identity/capture/validity/coverage. [contracts/presentation.md](contracts/presentation.md) defines the read interaction. [quickstart.md](quickstart.md) lists future validation. Scope clarifications are recorded in spec.md; no unresolved in-scope clarification remains.

Future implementation order: contracts/analyzer and tests; missing-target adapter; actual runtime A capture/revalidation; read-only projection/desktop integration; full relevant checks. No comprehensive reorganization of existing validators.

## Validation Strategy

- **Planning now**: successful changed-file secrets scan, line-ending fix/check, internal-link check, Spec Kit prerequisites, issue/feature governance, cross-artifact coverage and diff/path review. No .NET/app launch for Markdown-only work.
- **Future tests**: the three new fixtures above; existing ProjectDiagnosticsServiceTests, ProjectValidatorTests, ProjectRuntimeHostTests, MobaRuntimeServiceProjectIsolationTests, LocomotivesPageViewModelTests, MainWindowDiagnosticsTests and real DI tests. Include successful response followed by same-ID replacement/disposal, in-place reload, zero side effects and unchanged-owner controls.
- **Future portable**: filtered intended fixtures, then relevant full runtime/DI suite: `dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0`; portable consumer `dotnet build MOBApi/MOBApi.csproj`.
- **Future Windows**: AGENTS.md restore/FastDebug build of MOBAflow and Windows Test target. Compile MOBAsmart on an Android-workload host if shared registration affects that consumer; no Android screen work.
- **Future manual**: existing Messages Light/Dark, keyboard, source/unknown labels and target changes. App launch requires separate explicit authorization.
- **Regressions**: serialization/defaults/safe startup, command routing, cancellation, runtime isolation, singleton identity and existing Delete behavior.
- **Publication**: issue #306, changed-file scans/line endings and CI-only current-head Sonar gates. The user explicitly authorized tests and a Draft PR on 2026-10-16; keep the PR draft, with no ready/merge action.

## Complexity Tracking

No exception required. Request-local receipts bind existing objects, not new runtime identities. Receipt revalidation and existing lifecycle invalidation cannot create a delete permission or guarantee continuous freshness.
