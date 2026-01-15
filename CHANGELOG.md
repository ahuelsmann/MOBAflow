# Changelog

All notable changes to MOBAflow will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Segoe MDL2 icons for F0-F20 function buttons in Train Control
- Pan feature for TrackPlanEditor (right mouse button drag)
- VisualStateManager responsive layouts for TrainControlPage and TrackPlanEditorPage
- PropertyChanged-based Auto-Save pattern for all ViewModels
- App Shell architecture with DI-based navigation

### Changed
- Responsive breakpoints: Wide (1200px+), Medium (641-1199px), Compact (0-640px)
- Unified Auto-Save pattern using PropertyChanged instead of custom DataChanged events

### Fixed
- BacklightToggleButtonStyle VisualStates restored after VSM refactoring
- Function buttons now properly show hover, press, and disabled states

## [0.9.0] - 2025-02-04

### Added
- Initial public release
- Z21 Direct UDP Control for Roco Z21 Digital Command Station
- Journey Management with multi-station routes
- Text-to-Speech with Azure Cognitive Services and Windows Speech
- Workflow Automation with event-driven action sequences
- MOBAtps Track Plan System with visual track layout editor
- Track Libraries support (Piko A-Gleis)
- Multi-Platform support: WinUI (Windows), MAUI (Android), Blazor (Web)
- Plugin system for extensibility
- Lap Counter with statistics

### Security
- User Secrets support for API keys
- Environment variable configuration for sensitive data

---

## Version History Legend

- **Added**: New features
- **Changed**: Changes in existing functionality
- **Deprecated**: Soon-to-be removed features
- **Removed**: Removed features
- **Fixed**: Bug fixes
- **Security**: Security-related changes
