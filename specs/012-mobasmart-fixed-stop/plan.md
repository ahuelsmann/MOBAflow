# Implementation Plan: MOBAsmart fixed selected-locomotive STOP

**Branch**: `codex/issue-304-mobasmart-fixed-stop` | **Date**: 2026-10-15 | **Spec**: [spec.md](spec.md)
**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/304
**Spec Kit**: Required
**Status**: Implemented and locally validated; Android device acceptance remains open
**Input**: Feature specification from specs/012-mobasmart-fixed-stop/spec.md

## Summary

Use an Auto/* Grid: only wrapping locomotive name, dedicated DCC address and the existing selected-locomotive STOP remain fixed. One bounded ScrollView retains speed/direction/status/All off and the existing F0-F31 BindableLayout. No new command or state owner.

## Technical Context

**Language/Version**: .NET SDK 10.0.402, C#/MAUI XAML.
**Primary Dependencies**: Existing MAUI Controls 10.0.110, CommunityToolkit.Maui 15.0.1, MVVM toolkit 8.4.2; no additions.
**Storage/Protocols**: Existing IRuntimeCommandGateway/local/remote routing unchanged.
**Testing**: NUnit/Moq, existing command/selection/mobile/debounce fixtures and a focused selected-target STOP regression.
**Target Platforms**: Android net10.0-android.
**Affected Layers**: MOBAsmart UI and Test/SharedUI; no shared production runtime change.
**Performance Goals**: Preserve 32 non-recycled function rows, no added timers/subscriptions.
**Data and API Effects**: None to JSON, schema, config, defaults, public APIs, protocol or persisted layouts.
**Scale/Scope**: Narrow portrait through 200% system text, Light/Dark. No desktop, new picker/global stop, browser UI or unmerged localization files.

## Constitution Check

Pre-research and post-design gates pass. Constitution examples naming aggregate IMobaRuntime/CurrentSnapshot are stale; authoritative AGENTS.md/architecture.instructions.md require narrow runtime roles and IRuntimeCommandGateway. Preserve the current path; do not amend governance in this feature.

- [x] Layers, platform boundaries and central EventBus UI dispatch preserved.
- [x] Async commands remain awaited; no blocking or extra dispatch.
- [x] Existing ViewModel commands, English text and theme resources reused.
- [x] No data/API/default/startup change or migration layer.
- [x] Meaningful command regressions plus Android compile selected; no XAML text-mirror tests.
- [x] Existing services, DI and helpers reused without new abstractions.
- [x] Issue #304 in spec/plan; feature artifacts under specs/.
- [x] Strict per-read and changed-file secrets scans; no local Sonar code analysis.
- [x] Draft PR only; current-head CI/Sonar and zero OPEN/CONFIRMED issues required before readiness.

## Project Structure

Feature files: spec.md, plan.md, research.md, data-model.md, contracts/control-tab.md, quickstart.md, tasks.md, analysis.md, checklists/requirements.md.

**Structure Decision**:

- MOBAsmart/View/ControlPage.xaml: Auto/* layout; name wraps to two visual lines then ellipsis with full accessible name, dedicated DCC address; one unchanged EmergencyStopCommand binding; local HeightRequest=-1 overrides inherited 48, minimum target 48. STOP's accessible name includes the full locomotive name and DCC. Complete-row acceptance applies within the function section.
- MOBAsmart/Controls/FunctionToggleView.xaml: >=48-unit touch area (above the repository's 44 minimum); wrapping scaled description. Existing tap-command forwarding stays unchanged; actual TalkBack activation remains a manual gate.
- MOBAsmart/Controls/FunctionToggleView.xaml.cs: refresh existing semantic description on meaning/state changes, retaining command forwarding and visual cache.
- Test/SharedUI/TrainControlViewModelPerformanceTests.cs: current-target STOP regression using existing Moq helper.
- docs/wiki/MOBASMART-USER-GUIDE.md: explain selected-locomotive STOP.

## Validation Strategy

**Automated tests**: TrainControlViewModelPerformanceTests, TrainControlHostSelectionTests, MobileRuntimeCoordinatorTests, RuntimeCommandGatewayRoutingTests, MauiViewModelControlTabTests and RuntimeCommandPathArchitectureTests. Record actual discovery/counts. Test nonzero speed and target/state, without inventing an exactly-once transport guarantee.
**Builds**: dotnet restore MOBAsmart/MOBAsmart.csproj; dotnet build MOBAsmart/MOBAsmart.csproj -f net10.0-android -c FastDebug --no-restore. Installed workloads: android 36.1.118 and maui-android 10.0.110. Portable workarounds are not Android evidence.
**Manual checks**: Actual Android narrow portrait with host chrome, Light/Dark x100/150/200%, long names, five list positions, F31 switching/STOP/disabled states/accessibility. The user subsequently authorized the UI/UX expert's isolated emulator run at 200%, Light/Dark and TalkBack; real layout control remains forbidden. Record unexecuted matrix cases separately.
**Regression checks**: Controls, Functions source, ControlRoot forwarding, routing and safe startup unchanged. Existing direct STOP plus speed debounce is a baseline behavior, not redesigned.
**Secrets scan**: Every explicit workspace read; every changed file before commit/PR.
**Sonar gates**: Auth before draft publication; pipeline-only analysis, keep draft.
**Issue traceability**: #304 only; no duplicate/task issues.

## Complexity Tracking

No exception/new abstraction required. Semantic-description refresh is directly necessary accessibility work.
