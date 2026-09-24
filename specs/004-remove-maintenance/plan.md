# Implementation Plan: Remove vehicle maintenance

**Branch**: `codex/issue-147-remove-maintenance` | **Date**: 2026-09-24 | **Spec**: [spec.md](spec.md)
**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/147
**MVP Issue**: https://github.com/ahuelsmann/MOBAflow/issues/144
**Spec Kit**: Required
**Status**: Ready for implementation

## Summary

Remove all calendar maintenance while retaining rolling-stock editing, decoder/CV data and passport export.
One removal story spans Domain, Backend, SharedUI, WinUI and JSON contracts.

## Technical Context

- .NET 10 / C#, CommunityToolkit.Mvvm, System.Text.Json, NUnit and Moq; no dependency additions.
- Shared libraries and Windows UI change; Android and MOBApi consume the shared project data.
- Remove Locomotive/Wagon.Maintenance, VehicleMaintenance types (including maintenance-only MoneyAmount),
  MaintenanceCategory, VehicleMaintenanceService and its registration.
- Delete RollingStockMaintenanceViewModel/Panel; reconnect all three lists to the existing
  MainWindowViewModel filtered libraries and ensure notifications for project/collection/name changes.
- Remove maintenance members and commands from LocomotiveManagementViewModel and library/passport contracts.
- Remove JSON schema definitions; retain Solution.CurrentSchemaVersion=4. Normal deserialization discards
  unknown fields. There is no migration, sanitizer or recovery path.
- Config defaults, Z21 commands, runtime routing, persisted column layouts, master data and safe startup are unchanged.
- No additional performance target: the change removes work rather than adding a runtime subsystem.

## Constitution Check

All gates checked before and after design:
- [x] Layering, EventBus dispatch and asynchronous boundaries remain unchanged.
- [x] Existing ViewModel commands, serializers and DI lifetimes are reused.
- [x] English UI remains; existing theme resources/layouts are retained.
- [x] Superseded fields/types are removed without compatibility code or migrations.
- [x] Meaningful automated regressions cover inventory, persistence, DI and retained decoder/passport behavior.
- [x] Validation includes target builds and Test/Test.csproj; Light/Dark checks stay pending until launch authorization.
- [x] Issue/spec traceability is explicit; all feature artifacts are below specs/.
- [x] Pre-read and pre-publication secrets scans apply; Sonar code analysis is GitHub-only.
- [x] Draft PR remains draft until current-commit SonarCloud is green with zero OPEN/CONFIRMED findings.

## Project Structure

Feature artifacts: spec.md, plan.md, research.md, data-model.md, contracts/solution.md, quickstart.md,
tasks.md and checklists/requirements.md in this directory.
Implementation: Domain/, Backend/Service and Extensions/, SharedUI/ViewModel/, MOBAflow/View,
Controls, Extensions and Build/Schemas/, Test/, README.md, docs/ and the superseded Journey spec.

## Validation Strategy

- Regression fixtures: MainWindowViewModelPhotoAssignmentTests (list/search/selection/autosave), rolling-stock serialization,
  LocomotiveLifecycleSerializationTests, LocomotiveLibraryServiceTests,
  LocomotivePassportHtmlRendererTests, LocomotiveManagementViewModelTests,
  MobaBackendServiceCollectionExtensionsTests, SolutionControllerTests, MobileSolutionStoreTests and SolutionRemoteLoaderTests.
- Run portable Test/Test.csproj suite; WinUI FastDebug and Android FastDebug builds, MOBApi build,
  relevant Windows tests and required CI baselines. Coordinate large builds with the master task.
- Inspect schema/sample data, docs and remaining maintenance references. Preserve TrainType.Maintenance
  and Z21 Maintenance Tool help because neither is vehicle servicing.
- Run Test-LineEndings.ps1, Test-SpecKitGovernance.ps1, changed-file secrets scans and final diff review.
- Manually check three vehicle pages in Light/Dark only after explicit app-start approval.
- Integrate current main before publication and again after #146 before the centrally managed merge.
- Record actual results and remaining gates in quickstart.md; never infer platform or CI success.

## Complexity Tracking

No design exceptions or new abstractions are required.
