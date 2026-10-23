# Implementation plan: workflow editor quality corrections

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/378
**Spec Kit**: Required
**Branch**: codex/issue-378-graphical-workflow-editor
**Date**: 2026-10-23
**Spec**: [spec.md](spec.md)
**Status**: Planned correction phase; practical UI acceptance remains open

## Summary and provenance

Continue existing Draft PR #388 from d6aa332c10036172965845987143084fa881e66d against
main f2f388d3213b442a13169dd41c554ee1dbf1a3a6. Correct 13 current Sonar findings in four
production files. Preserve the existing graphical editor; do not recreate its implementation.
This package starts with this bounded phase and does not backdate original planning.

## Technical context

- Language/platform: repository SDK 10.0.402, C#, portable SharedUI and Windows WinUI 3.
- Dependencies: existing Microsoft.WindowsAppSDK, CommunityToolkit.Mvvm and NUnit; no additions.
- Storage/protocols: no JSON, configuration default, public API, Z21 or persisted layout changes.
- Performance: no new background work or lifecycle; resource keys remain constants.
- Scope: canvas callbacks/resources/status conditions and three parameter-summary getters.
- Exclusions: feature expansion, schema changes, protected operator solution, app/API/device launch,
  workflow execution, Ready, independent review dispatch and merge.

## Constitution check

All six principles pass before research and after design:

- I: SharedUI stays platform-neutral; no runtime, DI, gateway or EventBus changes.
- II: Commands still use the existing ViewModel; visual coordination stays in WinUI. No async
  changes. Existing theme keys and English display text remain identical. Actual Light/Dark
  inspection is an open acceptance gate, not a claimed design-time pass.
- III: Existing summary/layout/selection tests are reused; parameter-summary edge cases receive
  focused coverage. WinUI compiles on Windows. UI interaction is prepared without launching.
- IV: Spec and plan link Issue #378; artifacts remain here. Data/API/layout effects are explicit.
- V: Minimal refactoring, existing services and helpers, no new abstraction or dependency.
- VI: Scan before workspace reads and publication; use GitHub-only Sonar code analysis and require
  green exact-head checks plus zero OPEN/CONFIRMED findings before readiness.

## Phase 0: research

See [research.md](research.md). The live issue list confirms 13 findings: S1172 (2), S1192 (3),
S3358 (4), S1125 (3), S2325 (1). No material product ambiguity needs clarification.

## Phase 1: design and source map

1. `MOBAflow/Controls/Workflow/WorkflowGraphCanvasControl.xaml.cs`:
   - Adapt the ActualThemeChanged delegate to a parameterless visual refresh method.
   - Name the three repeated theme resource keys without changing their string values.
   - Replace nested tooltip selection with ordinary branches and nullable Boolean comparisons
     with explicit library guards; retain CanExecute, selection updates and event handling.
   - Make reset zoom's instance dependency explicit through this control's named ScrollViewer.
     Preserve the existing XAML Click binding and ChangeView arguments.
2. `SharedUI/ViewModel/Action/AnnouncementViewModel.cs`, `ChangeJourneyStopViewModel.cs` and
   `PowerShellActionViewModel.cs`: replace nested expressions with readable getters/early returns;
   retain every output branch and existing notifications.
3. `Test/SharedUI/WorkflowViewModelTests.cs`: cover previously unpinned summary branch boundaries;
   retain existing path, graph layout and property-notification tests.
4. `quickstart.md`: record the built artifact and audited startup constraints, the isolated
   acceptance recipe and numbered evidence fields. Do not start it or fabricate isolation.

See [data-model.md](data-model.md) and [contracts/editor.md](contracts/editor.md).

## Validation strategy

Run focused portable and Windows WorkflowViewModelTests/WorkflowLibraryViewModelTests/defaults/
XAML icon tests, WinUI compile and existing analyzer baseline checks with unchanged baselines.
Use a temporary validation-only MSBuild target to exclude the protected solution Content/None
items before AssignTargetPaths; disable JSON validation. Do not open, hash or copy that file.
Record the exact commands and results in quickstart.md. Review docs/wiki/MOBAFLOW-USER-GUIDE.md
and architecture/developer docs; update only inaccuracies caused by this correction phase.
Normalize/check changed text, scan all changed files, run governance and git diff checks, then
publish a normal fix commit to the existing branch. Verify new Linux/Windows CI and Sonar issue
count. Assess convergence before the source/process-END handoff to Chief PM.

## Complexity tracking

No constitutional exception or feature extension is needed. The temporary build exclusion is
local validation protection, not a shipped build-graph change. Practical acceptance stays open
until a separately authorized operator actually performs it.
