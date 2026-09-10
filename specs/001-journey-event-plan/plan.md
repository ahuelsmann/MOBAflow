# Implementation Plan: Journey event plans

**Branch**: `codex/journey-event-plan` | **Date**: 2026-09-10 | **Spec**: [spec.md](spec.md)

**GitHub Issue**: #124

**Spec Kit**: Required

**Status**: Implemented locally; issue linked and portable tests/Windows compile passed; manual acceptance and remote quality gate open

## Summary

Keep session InPort counters separate from virtual stops. Each explicit run snapshots counters, evaluates independent start-relative events once and reuses the workflow runner. Preserve an explicit legacy path. Simplify Event Manager to plan rows with a workflow palette, drag and drop and equivalent commands.

## Technical Context

**Language/Version**: C#/.NET 10 and existing WinUI 3; repository SDK/package files remain authoritative.

**Primary Dependencies**: Existing EventBus, runtime/state store, workflow runner, CommunityToolkit.Mvvm and serializers; no new packages.

**Storage/Protocols**: Additive project JSON and existing Z21/runtime transport; no persisted counters or run baselines.

**Testing**: NUnit, Moq, fake feedback and existing runtime fixtures.

**Target Platforms**: Portable logic and Windows editor; adapt existing mobile/API runtime consumers where necessary.

New start/stop/counter-reset commands execute on the local owning runtime. In an active remote mobile session this increment explicitly rejects them and directs the operator to the MOBAflow host; it never falls back to mutating the mobile runtime. Existing remote commands remain unchanged.

**Affected Layers**: Domain, Common, Backend, SharedUI, MOBAflow and runtime adapters.

**Performance Goals**: Increment once per accepted activation and evaluate running plans without polling or replay; verify three concurrent journeys.

**Compatibility**: Nullable `Journey.EventPlan` selects legacy behavior when absent. New-journey factories initialize a plan. Explicit legacy adoption creates an empty plan and preserves old data. No changes to Z21 wire behavior, config defaults, safe startup or persisted layouts.

**Scale/Scope**: Small user-authored plans on connected tracks; no autonomous collision avoidance or train identification.

## Constitution Check

Before-research and after-design checks describe intended design, not delivered test evidence:

- [x] Preserve platform/layer boundaries and existing runtime routing.
- [x] Keep UI dispatch centralized; use `await` and existing cancellation behavior.
- [x] Keep editing in ViewModel commands; drop handlers translate input only.
- [x] Use English labels and Light/Dark theme resources.
- [x] Specify additive compatibility and explicit adoption; reuse established services/serializers.
- [x] Plan tests for every behavior, relevant `Test/Test.csproj` suites and consumer builds.
- [x] Keep feature documents under `specs/`; follow supplied pre-read scans and changed-file scans.
- [x] Attempt local Sonar against the actual base and document its capability limitation; keep the PR draft until remote quality checks pass.
- [x] Reference authoritative GitHub issue #124 and pass issue/PR Spec Kit governance.

Authoritative [issue #124](https://github.com/ahuelsmann/MOBAflow/issues/124) is created and governance passes. Local Sonar ran against `github/main` but agentic analysis is unavailable for the organization (403 Forbidden); this is not a green analysis. The remote SonarCloud gate remains open for the draft PR.

## Project Structure

- `Domain/Journey.cs`, `Domain/JourneyEventPlan.cs`: additive plan and event model.
- `Backend/Manager/JourneyManager.EventPlans.cs`, `Backend/Service/InPortCounterService.cs`, `Backend/Service/JourneySessionState.cs`, `Backend/Service/JourneyRuntimeStateStore.cs`: run lifecycle, baseline/dispatch state and counter coordination.
- `Backend/Interface/IMobaRuntime.cs`, `Common/Runtime/`, `Backend/Service/MobaRuntimeService*`: runtime commands/projections and adapters.
- `SharedUI/ViewModel/JourneyViewModel.cs`, `SharedUI/ViewModel/EventManagerViewModel.cs`: observable plan editing and commands.
- `SharedUI/Service/RecordingRuntimeCommandGateway.cs`, `SharedUI/Service/MobileRuntimeCoordinator.cs` and `Backend/Service/Recording/`: lifecycle adapter and recording/replay compatibility.
- `MOBAflow/View/EventManagerPage.xaml` and code-behind: slim editor and drop translation.
- `MOBAflow/Resources/EntityTemplates.xaml`: hide legacy feedback editing for event-plan journeys and point to Event Manager.
- `Test/Domain/`, `Test/Backend/`, `Test/SharedUI/`: serialization, lifecycle, dispatch and command regressions.

## Design and Delivery

1. Add plan contracts and application-session counters without repurposing legacy fields.
2. Coordinate start/reset, capture baselines and isolate active runs. Stop preserves counters; restart resets only run state. RuntimeService passes its counter explicitly into every manager creation, including an externally supplied factory.
3. Match enabled events against positive deltas, mark dispatch before async execution and reuse arbitrary workflow actions. Never infer stop movement.
4. Add compact rows, workflow palette, row moves/workflow drops and accessible command alternatives. Protect active-run configuration. While a run exists, defer project replacement and reject starting a changed journey or changed workflow definitions rather than executing stale settings; unchanged journeys remain startable. After all runs stop, the next activation/start applies pending definitions.
5. Converge with tests/builds, compatibility, governance and static checks; record remaining UI/Sonar/PR gates honestly.

## Validation Strategy

- **Automated tests**: Counts 2/3/5, gaps, independent ports, equal conditions, concurrent baselines, stop/restart, failure, reset rejection, serialization and editor commands. Preserve existing legacy journey tests.
- **Builds**: Relevant portable consumers plus Windows FastDebug compile-only build; validate source-changing Android adapters on matching workloads if affected.
- **Manual checks**: [quickstart.md](quickstart.md) covers pointer/keyboard, Light/Dark, focus and feedback. Application launch/hardware need explicit authorization.
- **Regression checks**: Legacy data/behavior, no persisted run state, runtime snapshots and local/remote command routing.
- **Regression fixtures**: `Test/Backend/MobaRuntimeEventPlanTests.cs` covers deferred activation, stale-definition rejection and shared-counter ownership; `Test/SharedUI/JourneyCounterProjectionTests.cs` covers counter projection. `Test/Backend/JourneyEventPlanAcceptanceTests.cs` covers sparse 2/3/5 events and three parallel journeys.
- **Static checks**: Changed-file secrets, line endings and Spec Kit governance. Instruction consistency only if instructions change.
- **Sonar gates**: Local analysis was attempted against `github/main` at `698c4c8b5e35a4071e4acd81635e5a2dde3d3b41`; the organization lacks agentic analysis capability. Draft PR requires green SonarCloud and zero OPEN/CONFIRMED issues before ready status.
- **Issue traceability**: Authoritative issue #124 is linked in the specification and plan; both issue and PR governance checks pass.

## Complexity Tracking

No exceptions. A preserved explicit legacy path is necessary because old per-step repeat counts cannot safely become start-relative thresholds.
