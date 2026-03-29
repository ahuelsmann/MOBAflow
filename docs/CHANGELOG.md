# Changelog

All notable changes to MOBAflow will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Added a first runtime boundary with `IMobaRuntime`, `MobaRuntimeService`, `IMobaClient`, and `InProcessMobaClient`.
- Added runtime projection models `MobaRuntimeSnapshot`, `JourneyRuntimeSnapshot`, and `LocomotiveRuntimeSnapshot`, plus runtime-side feedback forwarding for UI consumers.
- Added `ActiveProjectContext` and `ProjectRuntimeFactory` as the first activation layer for the running project.

### Changed
- `MainWindowViewModel` no longer orchestrates Z21 connection handling and `JourneyManager` directly. It now consumes runtime snapshots and delegates control commands through `IMobaClient`.
- Traffic monitor access, track power commands, feedback simulation, journey reset, multiplex signal commands, locomotive control, and locomotive info requests are now routed through the runtime boundary.
- `JourneyViewModel` now accepts projected runtime state updates from snapshots instead of depending on direct runtime manager ownership.
- `TrainControlViewModel` and `MauiViewModel` now consume `IMobaClient` instead of using `IZ21` directly.
- WinUI DI registration now wires the shell through `IMobaClient`/`IMobaRuntime`.

### Notes
- The runtime split now covers the shared shell plus the remaining shared Z21-facing ViewModels.
- The active runtime still uses the live `Project` reference from the loaded `Solution`.
- A later step will introduce a true runtime copy so editor state and execution state are fully separated.


