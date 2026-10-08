# Implementation Plan: Solution session with one runtime per project

**GitHub Issue**: #191

**Spec Kit**: Required

**Branch**: `codex/issue-191-project-runtimes` | **Date**: 2026-10-06 | **Spec**: [spec.md](spec.md)

## Summary

Introduce a solution session that owns the loaded solution and creates one runtime per project. Each runtime is a DI
scope with its own Z21 connection, counters, interlocking and workflow context. The runtime stops copying the project
and keeps only runtime values. MOBAflow shows the selected project's runtime, MOBAsmart selects one project and MOBApi
carries a project identifier. Research: [research.md](research.md).

## Technical Context

**Language/Version**: C# on .NET 10 (`global.json`)
**Primary Dependencies**: Microsoft.Extensions.DependencyInjection, CommunityToolkit.Mvvm, WinUI 3, .NET MAUI,
ASP.NET Core SignalR
**Storage**: solution JSON (`Project` gains its Z21 endpoint); app settings lose the Z21 section fields; feedback
counters persist per project
**Testing**: NUnit, Moq, fake `IZ21`; portable and Windows suites; architecture tests in `Test/Architecture`
**Target Platform**: Windows desktop (MOBAflow, MOBApi), Android (MOBAsmart)
**Constraints**: no migration (AGENTS.md), safe locomotive startup unchanged, UI-thread event bus contract unchanged,
no hardware actions without separate approval

## Constitution Check

- **I. Architecture and threading**: session and runtime scope live in `Backend`/`SharedUI` without WinUI or MAUI
  types; the UI keeps receiving events through the UI-thread event bus. Editor and runtime threads never share
  mutable master data (definitions snapshot, slice 2). PASS.
- **II. Async and UI behavior**: switch and close sequences are async, cancellable before confirmation and never block
  on `.Result`. PASS.
- **III. Tests**: every slice adds characterization or behavior tests with fake Z21 endpoints; an architecture test
  guards FR-001. PASS.
- **IV. Traceability**: #191 is the source; each slice PR references it; tasks below. PASS.
- **V. Simple changes**: one runtime type per project (a scope) instead of project identifiers on every event; pages
  stay until RF-24. PASS.
- **VI. Quality gates**: secrets scan, line endings, analyzer baselines, SonarCloud and CI per slice. PASS.

## Project Structure

### Documentation (this feature)

```text
specs/008-project-runtimes/
├── spec.md
├── research.md
├── plan.md
└── tasks.md
```

### Source Code

```text
SharedUI/Service/SolutionSession.cs          # owns solution, selection, dirty state, auto-save, runtimes (new)
SharedUI/Interface/ISolutionSession.cs       # session contract incl. IProjectContext (new)
Backend/Service/ProjectRuntimeFactory.cs     # creates one DI scope per project (new)
Backend/Service/Z21ConnectionRegistry.cs     # app-lifetime connections keyed by endpoint (new)
Backend/Service/MobaRuntimeService*.cs       # no project copy; runtime values only
Domain/Project.cs                            # Z21 endpoint per project
Common/Configuration/AppSettings*.cs         # Z21 address/port/recent list removed
MOBApi/**                                    # project identifier in commands, snapshots, runtime settings
MOBAsmart/**, SharedUI/ViewModel/Maui*       # project selection
docs/wiki/MOBAFLOW-USER-GUIDE.md, INSTALLATION.md, MOBASMART-USER-GUIDE.md
```

## Delivery slices

Each slice is one reviewable PR on its own branch from `main`; this branch carries the specification and slice 1.

1. **Solution session (no behavior change)**: move solution ownership, selection, dirty state and auto-save from
   `MainWindowViewModel` into `SolutionSession`; inject it into host services and ViewModels; architecture test for
   FR-001 with the WinUI page exemption list. Characterization tests for load, save, auto-save and selection first.
2. **Live definitions snapshot**: the runtime reads master data from a snapshot refreshed by
   `IConnectionRuntime.UpdateProjectAsync` after every saved change (synchronization rule in
   [research.md](research.md)); journey progress, running workflows and signal aspects are kept by id;
   `UpdateJourneyEventsAsync`, `UpdateSignalBoxAsync` and the re-activations after adding journeys, stations or
   trains are removed.
3. **Runtime and Z21 per project**: Z21 endpoint in `Project`, `Z21ConnectionRegistry`, `ProjectRuntimeFactory`,
   one runtime scope per project, per-project counters, conflict diagnostics, selected-project event forwarding,
   switch/close warning with speed 0. Fixes #190.
4. **MOBApi and MOBAsmart**: project identifier in commands, snapshots and runtime settings; MOBAsmart project
   selection and per-project Z21 address.
5. **Documentation**: user wiki, `docs/ARCHITECTURE.md`, CHANGELOG.

## Validation Strategy

- **Automated tests**: characterization of load/save/auto-save/selection (slice 1); runtime values kept across editor
  changes without cancelling workflows (slice 2); two projects with two fake Z21 endpoints, conflict, takeover and
  switch sequence (slice 3); MOBApi project routing and MOBAsmart selection (slice 4).
- **Architecture tests**: no service or ViewModel depends on `MainWindowViewModel` (FR-001, SC-004).
- **Builds and analyzers**: cross-platform, Windows and Android Release analyzer gates from full rebuilds per slice.
- **Suites**: `dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0` and the Windows suite.
- **Manual checks** (need separate approval to start the apps): two layouts with two Z21, switch warning, MOBAsmart
  project selection.
- **Gates**: secrets scan, line endings, Spec Kit governance, SonarCloud with zero open issues, green CI.

## Complexity Tracking

| Item | Why needed | Simpler alternative rejected because |
| --- | --- | --- |
| DI scope per project runtime | Runtime, Z21, counters, interlocking and workflow context form one object graph per layout | Keyed singletons would need a manual lifetime for every type |
| Event forwarding of the selected runtime | UI subscribers keep their contract and show the selected project | A project identifier on every event touches every event type and subscriber |
