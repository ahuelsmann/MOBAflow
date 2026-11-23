# Feature Specification: Fluent Design 2 Semantic Colors & Column Role Chrome (Slice 1A)

**Feature Directory**: `022-fluent-design-accents`

**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/398

**Created**: 2026-11-23

**Status**: Slice 1A in progress on branch `antigravity/398-fluent-design-slice-1a` (replaces superseded reference draft `codex/issue-398-fluent-design-accents` / `019-fluent-design-accents`).

**Spec Kit**: Required

## Governance and Traceability *(mandatory)*

**GitHub Issue**: #398

**Affected Platforms**: `MOBAflow` (WinUI 3 desktop app) and its Windows unit tests in `Test`. `Domain`, `Common`, `Backend`, `Contracts`, `SharedUI`, `MOBApi`, `MOBAdemo`, and `MOBAsmart` are not changed.

**Scope (Slice 1A)**:
- `MOBAflow/App.xaml` (`ThemeDictionaries` only: `Light`, `Dark`, `HighContrast`)
- `MOBAflow/Themes/Generic.xaml` (`CollapsibleColumnHelper`, `CollapsibleColumnCollection`, `CollapsibleColumnProperties`)
- `MOBAflow/Converter/BoolToAccentBrushConverter.cs`
- `Test/WinUI/ThemeConverterTests.cs`
- `specs/022-fluent-design-accents/`

**Deferred to Later Slices (Out of Scope for Slice 1A)**:
- Slice 1B+: `MainWindow.xaml` (Diagnostics severity filter colors and StatusBar status pills), `OverviewPage.xaml` (Z21 telemetry KPI cards, green health indicators via `BoolToSuccessBrushConverter`, English labels), `TrainsPage.xaml` and `LocomotivesPage.xaml` (rolling-stock category brushes `MobaLoco*`, `MobaPassengerWagon*`, `MobaGoodsWagon*`, DCC address badges), `JourneysPage.xaml`, `EventManagerPage.xaml`, `MonitorPage.xaml`, `SharedUI/*`, and `docs/wiki/PAGE-OVERVIEW.md`.
- Existing RF-16 / RF-10 baseline resources (`MobaPanelAccentHelperBrush`, `MobaPanelAccentCollectionBrush`, `MobaPanelAccentPropertiesBrush`, `HighContrast` `SystemColor*` entries, and `AutomationProperties.Name` values on `CollapseButton`) MUST be preserved.

**Related Work**: #374 / #375 / #432 (RF-16 accessibility and High Contrast dictionaries), #451 / #500 (RF-10 formatting baseline), #399 (Track Plan Editor).

## User Scenarios & Testing

### User Story 1 - Immediate visual orientation across multi-column workspaces (Priority: P1)

An operator works on multi-column WinUI pages (`SolutionPage`, `JourneysPage`, `LocomotivesPage`, `TrainsPage`, `WorkflowsPage`, `EventManagerPage`, `SignalBoxPage`, `TrackPlanPage`, `StationsPage`, `MatrixPage`, `MonitorPage`, `HelpPage`, `GoodsWagonPage`, `PassengerWagonPage`) across `Light`, `Dark`, and Windows `HighContrast` themes.

**Acceptance Scenarios**:

1. **Given** a multi-column workspace page in `Light` or `Dark` theme, **When** `CollapsibleColumnCollection`, `CollapsibleColumnHelper`, or `CollapsibleColumnProperties` is expanded, **Then** its left accent strip (`3.5px`) and tinted header icon badge (`26x26`) use the semantic `ThemeResource` column role brushes (`MobaColumnCollection*`, `MobaColumnHelper*`, `MobaColumnProperties*`) with at least 3:1 icon-to-badge contrast.
2. **Given** a column is collapsed into `CollapsedTab`, **When** the column shrinks to its `32px` auto-width column, **Then** `CollapsedTab` renders a vertical Fluent pill (`MinHeight="48"`, `Width="32"`) displaying the semantic role left accent bar, tinted icon badge, and `ToolTipService.ToolTip="{TemplateBinding Header}"`.
3. **Given** Windows runs a `HighContrast` theme, **When** `App.xaml` theme dictionaries are resolved, **Then** every `MobaColumn*` brush key exists in `Light`, `Dark`, and `HighContrast`, and every `HighContrast` entry maps exclusively to `{ThemeResource SystemColor...Color}` resources.
4. **Given** UI automation or `CollapsibleColumnBase` interacts with a collapsible column, **Then** template part names (`CollapsedTab`, `CollapseButton`) and RF-16 `AutomationProperties.Name` values (`"Unpin panel"`, `"Collapse list"`, `"Collapse panel"`) remain unchanged.

### User Story 2 - Extensible semantic true-state brush converter (Priority: P2)

Future slices (`OverviewPage.xaml` in Slice 1B) need `BoolToAccentBrushConverter` to optionally resolve a semantic `ThemeResource` key (such as `SystemFillColorSuccessBrush`) when `True`, without altering any existing `BoolToAccentBrushConverter` caller.

**Acceptance Scenarios**:

1. **Given** `BoolToAccentBrushConverter` with default settings, **When** `Convert` receives `true` or `false`, **Then** it resolves `AccentFillColorDefaultBrush` for `true` and `TextFillColorSecondaryBrush` for `false`, preserving 100% backwards compatibility for `JourneyMapPage.xaml` and `OverviewPage.xaml`.
2. **Given** `BoolToAccentBrushConverter` with a custom `TrueResourceKey`, **When** `ResolveResourceKey` evaluates `true`, **Then** it returns `TrueResourceKey` (falling back to `AccentFillColorDefaultBrush` when null or whitespace).

## Requirements

- **FR-001**: `MOBAflow/App.xaml` MUST define semantic column role brushes (`MobaColumnCollectionAccentBrush`, `MobaColumnCollectionBadgeBackgroundBrush`, `MobaColumnCollectionBadgeBorderBrush`, `MobaColumnHelperAccentBrush`, `MobaColumnHelperBadgeBackgroundBrush`, `MobaColumnHelperBadgeBorderBrush`, `MobaColumnPropertiesAccentBrush`, `MobaColumnPropertiesBadgeBackgroundBrush`, `MobaColumnPropertiesBadgeBorderBrush`) identically across `Light`, `Dark`, and `HighContrast` `ThemeDictionaries`.
- **FR-002**: Every `HighContrast` brush in `MOBAflow/App.xaml` MUST use `{ThemeResource SystemColor...Color}` and MUST preserve existing `MobaPanelAccent*Brush` and all RF-16 keys.
- **FR-003**: `MOBAflow/Themes/Generic.xaml` MUST upgrade `CollapsibleColumnHelper`, `CollapsibleColumnCollection`, and `CollapsibleColumnProperties` (`ExpandedPanel` left accent strip + header icon badge chrome, `CollapsedTab` vertical Fluent pill with role accent strip, icon badge, and header tooltip) while preserving `x:Name` template parts and RF-16 `AutomationProperties.Name` values.
- **FR-004**: `MOBAflow/Converter/BoolToAccentBrushConverter.cs` MUST expose `TrueResourceKey` defaulting to `"AccentFillColorDefaultBrush"` without changing existing callers or default resolution behavior.
- **FR-005**: All UI text, tooltips, and newly added XAML/C# comments MUST be in English.

## Success Criteria

- **SC-001**: `MOBAflow.csproj` and `Test.csproj` compile cleanly in Windows `Release` (`net10.0-windows10.0.22621.0`) and portable `Release` (`net10.0`) with zero analyzer baseline regressions (`scripts/Test-AnalyzerBaseline.ps1`).
- **SC-002**: Architecture tests (`ThemeDictionaryArchitectureTests`, `AccessibleNameArchitectureTests`), WinUI converter unit tests (`ThemeConverterTests`), portable & Windows test suites, `scripts/Test-SpecKitGovernance.ps1`, `scripts/Test-LineEndings.ps1`, documentation consistency checks, and local secrets scan all pass.
