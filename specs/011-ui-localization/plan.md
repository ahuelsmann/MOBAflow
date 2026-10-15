# Implementation Plan: UI localization first slice

**Branch**: `codex/issue-140-localization` | **Date**: 2026-10-14 | **Spec**: [spec.md](spec.md)

**GitHub Issue**: #140

[GitHub issue](https://github.com/ahuelsmann/MOBAflow/issues/140)

**Spec Kit**: Required

**Status**: Draft - design ready for task analysis

**Input**: `specs/011-ui-localization/spec.md`; user approved first slice on both hosts.

## Summary

Provide local System/English/German selection with restart semantics for [coverage.md](coverage.md).
Store the preference in existing application settings. Own translations once in SharedUI `.resx`;
produce WinUI `.resw` property resources from an explicit map. MAUI uses the shared catalogue
through a markup adapter. Shared ViewModels remain free of platform resource/control types.

## Technical Context

**Language/Version**: .NET 10 / C#, SDK 10.0.402 from `global.json`.
**Primary Dependencies**: Existing CommunityToolkit.Mvvm, System.Resources, CultureInfo,
Windows App SDK MRTCore and MAUI. No new package or translation service.
**Storage/Protocols**: Existing per-host appsettings JSON; `Application.UiLanguage` only.
**Testing**: NUnit/Moq, isolated PowerShell resource fixtures and platform builds.
**Target Platforms**: Windows/WinUI, Android/MAUI; SharedUI/Common remain net10.0.
**Affected Layers**: Common configuration, SharedUI localization/ViewModel, host startup/views,
resource tooling, documentation/governance.
**Performance Goals**: Local resource lookup without per-lookup I/O/network or new background work.
**Data and API Effects**: Default `system`; no solution schema, undo, API, TTS, protocol or layout changes.
**Scale/Scope**: Slice 1 rows only; remaining pages stay tracked in #140.

## Constitution Check

Checked before research and after design against Constitution 5.0.0, amended explicitly for #140.

- [x] Layer/platform boundaries preserved; platform adapters live in hosts.
- [x] EventBus dispatch and runtime command/snapshot ownership unchanged.
- [x] Async saves use await without task blocking.
- [x] Language behavior uses a shared ViewModel; theme resources retained.
- [x] Complete English source/fallback and German key/template parity within slice.
- [x] Domain/sample/user data, serialization and TTS independent.
- [x] Data/API effects explicit; no migration/compatibility mechanism.
- [x] Meaningful regression/tool tests and platform/manual checks specified.
- [x] Existing settings services, constructor injection and service lifetimes reused.
- [x] Spec/plan reference #140; feature artifacts remain under specs.
- [x] This session scans every workspace file before reads and changed files before publication.
- [x] Only PR CI performs Sonar code analysis; current-commit draft gates retained.

The old English-only rule is replaced in AGENTS, WinUI/MAUI guidance and the active plan template.
Existing specifications retain their scope; no data translation is implied. Constitution 5.0.0
is a major policy change, not a refresh of unrelated historical runtime wording.

## Project Structure

```text
specs/011-ui-localization/     # spec, plan, tasks, research, contracts, coverage, validation
Common/Configuration/AppSettings.Sections.cs
SharedUI/Localization/         # authored .resx catalogues and IUiLocalizer
SharedUI/ViewModel/LanguageSettingsViewModel.cs
MOBAflow/Strings/              # generated .resw projections
MOBAflow/Service/              # startup/navigation host adapters
MOBAflow/View/                 # included language area, Help and Info
MOBAsmart/Localization/        # MAUI resource markup adapter
MOBAsmart/Controls/LanguageSettingsView.xaml
scripts/                      # resource generator/checker/isolated fixtures
Test/SharedUI/                 # meaningful localization/preference tests
```

**Structure Decision**: Common owns preference only. SharedUI owns text and formatting through
IUiLocalizer. MAUI markup resolves the shared catalogue with startup UI culture. WinUI static
text uses x:Uid projections; dynamic navigation/messages use the platform-neutral service.

## Phase 0: Research

[research.md](research.md) records primary Microsoft sources and alternatives. The research
subagent only read official web documentation. No unresolved technical clarification remains.

## Phase 1: Design

- [data-model.md](data-model.md) defines preference normalization and state transitions.
- [contracts/localization.md](contracts/localization.md) fixes lookup/fallback/template semantics.
- [quickstart.md](quickstart.md) defines automated/manual validation.
- An explicit mapping projects semantic catalogue keys into unique x:Uid property names.
  Generated files are checked in and checked for freshness; translations are never edited twice.
- Resolve saved preference through each host's existing settings path/loading rules before
  covered views/resource lookup. Desktop applies MRTCore override before MainWindow/pages.
  Android applies UI culture before App/Shell/views. Do not introduce sync-over-async;
  a minimal synchronous preference read may reuse existing path resolution.
- Set CurrentUICulture/DefaultThreadCurrentUICulture for resource selection. Keep CurrentCulture
  unchanged; IUiLocalizer.Format explicitly uses its display culture to avoid incidental
  protocol/TTS behavior changes.
- Await language save using existing persistence; modify only UiLanguage, roll back in-memory
  preference on failure, and expose localized error/restart text through the shared ViewModel.
- Add a separate mobile control below the connection header; no Counter lifecycle/command,
  tab routing or RF-24 extraction changes.

## Validation Strategy

- **Automated**: `Test/SharedUI/UiLocalizerTests.cs`, `LanguageSettingsViewModelTests.cs`,
  relevant existing settings/default tests; isolated resource fixtures for missing/duplicate/empty
  keys, malformed/different placeholders and stale output.
- **Builds**: SharedUI, desktop and Android restore/FastDebug; full applicable Test targets
  for config/DI/startup/resource integration; normal dependency build and PR CI cover build graph.
- **Manual**: English/German/System, full restart/save failure, accessibility/keyboard,
  Light/Dark, desktop 200% and Android >=1.5 text scale. App launch requires explicit authorization.
- **Regression**: Preference/default roundtrip, unchanged Solution/undo/speech, host independence,
  no resource network use; prefer explicit-culture instance tests over global culture mutation.
- **Structural**: Resource checker/fixtures, line endings, staged line endings, instruction
  consistency, Spec Kit governance, links and final diff check.
- **Secrets/Sonar**: Changed-file secrets scans; draft PR/current-commit SonarCloud with zero OPEN/CONFIRMED issues.
- **Traceability**: #140 stays open; no task issues or cross-chat messages without authorization.

## Dependencies and Concurrent Work

RF-24/#219 is not a hard blocker. Journey/Workflow/Counter extraction and monolithic partials
remain untouched. Help/Info/resources are independent; check ownership/current main before
integrating settings/startup hunks. TTS #136/PR #278 and AutomationTestbench #35 remain separate.

## Complexity Tracking

No exception to the amended constitution. Generated projection is justified by required x:Uid
plus single translation ownership; freshness and placeholder checks compensate for drift risk.
