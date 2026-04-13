# Changelog

All notable changes to MOBAflow will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `MasterDataStore` as the CLR type for shared master JSON (`data.json`: cities,
  locomotives, Viessmann multiplex catalogue). Replaces the former `DataManager`
  name; JSON shape and keys are unchanged.
- `WorkflowExecutionOptions` (`StopOnFirstActionFailure`) on
  `IWorkflowService.ExecuteAsync` for optional fail-fast sequential workflows.
- `FeatureToggleRegistry` page-availability get/set without reflection on
  `FeatureToggleSettings`.
- `WinUiGridInterop` in SharedUI to access WinUI `Grid.ColumnDefinitions` without
  referencing WinUI from the cross-platform library.
- `MobaRuntimeService` implemented as a **partial** class split across
  `MobaRuntimeService.cs` (ctor, snapshot, dispose),
  `MobaRuntimeService.RuntimeApi.cs`, `MobaRuntimeService.Z21Handlers.cs`,
  `MobaRuntimeService.AutoConnect.cs`, and `MobaRuntimeService.StatusFormatting.cs`.

### Changed

- **Runtime boundary:** Removed `IMobaClient` and `InProcessMobaClient`.
  `MainWindowViewModel`, `TrainControlViewModel`, and `MauiViewModel` inject
  `IMobaRuntime` (`MobaRuntimeService`) directly from DI.
- **WinUI DI:** City, locomotive, and settings services register without
  try/catch fallbacks to null implementations; misconfiguration fails at startup.
- **EventBus:** Handler exceptions are logged at **error** severity; when a
  debugger is attached, `Debug.WriteLine` includes the failure for visibility.
- **Column layout:** `ColumnViewModel` builds `GridLength` via the runtime
  `Width` property type (double ctor) instead of a hard-coded type name string.
- Runtime projection models `MobaRuntimeSnapshot`, `JourneyRuntimeSnapshot`, and
  `LocomotiveRuntimeSnapshot` remain the UI-facing state; feedback is forwarded
  from the runtime for MAUI and WinUI consumers.
- `JourneyViewModel` continues to consume projected runtime state rather than
  owning `JourneyManager` directly.

### Removed

- `IMobaClient`, `InProcessMobaClient`.
- `ProjectRuntimeFactory` (superseded by `MobaRuntimeService.ActivateProjectAsync`
  and `ActiveProjectContext`).

### Notes

- A **remote** runtime client could still be introduced later as another
  `IMobaRuntime` implementation; the extra UI facade layer was removed as redundant
  for the in-process hosts.
- The active runtime still uses the live `Project` from the loaded `Solution`;
  separating editor state from execution state remains a possible follow-up.

