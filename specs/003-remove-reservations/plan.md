# Implementation Plan: Remove route reservations

**Branch**: `codex/issue-146-remove-reservations` | **Date**: 2026-09-24 | **Spec**: [spec.md](spec.md)

**GitHub Issue**: #146

**Spec Kit**: Required

## Summary

Remove the route reservation model and commands. Reuse the existing semantic turnout services directly from the runtime service; retain block observations, direct signals, counters and journeys.

## Technical Context

.NET 10, existing C# defaults, CommunityToolkit MVVM, NUnit/Moq; no dependencies added. JSON project data and Z21 UDP retain their existing transport/serializer. Shared libraries and WinUI change; Android and MOBApi consumers compile. Remove route definitions, ownership/locks and route-only contracts without migration or schema-version change. No new performance target; FIFO and immutable publication remain.

## Constitution Check

Reviewed before research and after design:

- [x] Platform layers, asynchronous execution and centralized EventBus UI dispatch remain intact.
- [x] ViewModel commands own behavior; English text and existing theme resources remain.
- [x] Superseded contracts/models and exclusive tests are removed without compatibility paths.
- [x] Regression tests cover direct commands, observations, persistence, DI and UI selection.
- [x] Existing serializers, services, singleton DI and fake hardware are reused.
- [x] Issue traceability and artifacts remain under this feature directory.
- [x] Secrets scans precede reads/commit; Sonar code analysis runs only in GitHub CI.
- [x] Draft PR and current-commit green CI/Sonar with zero open/confirmed issues remain required.
- [x] Theme/hardware checks stay explicitly unperformed without launch authorization.

## Project Structure

- `Domain/Interlocking/InterlockingDefinition.cs`, `Domain/SignalBoxPlan.cs`: remove routes/requirements and route-only states.
- `Backend/Service/Interlocking/`: remove route coordinator, conflict analyzer, safety engine and route-only signal gateway. Retain semantic turnout services; simplify runtime state/service.
- `Backend/Interface/IInterlockingRuntime.cs`, `Backend/Events/InterlockingRuntimeEvents.cs`: direct operation and immutable observations only.
- `Backend/Service/Validation/InterlockingDefinitionValidator.cs`: retain mapping/block/binding validation, remove route rules.
- `SharedUI/ViewModel/InterlockingControlViewModel*.cs`: selection, turnout commands and information; remove route draft autosave.
- `MOBAflow/Controls/SelectedObjectWorkbench.xaml*`, operating pages: remove route controls, preserve canvas/editor content.
- Schema, affected tests, user help and specification claims follow the current model.

## Validation Strategy

Focused InterlockingRuntimeService/ControlViewModel/DefinitionValidator/Serialization, SemanticTurnout, InPortCounter and Journey tests, then full portable/Windows Release suites. Compile WinUI, MOBApi and Android after host-load coordination. Generate fresh complete SARIF for analyzer comparisons; only justified reductions. Verify schema, line endings, secrets, Spec Kit governance and final diff. Incorporate integrated main/#133 before publication. Operator-owned layout data is not read/modified. Manual Light/Dark and live hardware remain unperformed.

## Complexity Tracking

No design exceptions. Retain existing operational definition/runtime naming to avoid unrelated JSON renames. Block occupancy is observational and never a direct turnout command prerequisite.
