# Implementation Plan: Fluent Design 2 Semantic Colors & Column Role Chrome (Slice 1A)

**Source Issue**: #398

**Spec Kit**: Required

Input: [spec.md](spec.md).

## Approach

1. **Semantic Column Role ThemeDictionaries ([MOBAflow/App.xaml](../../MOBAflow/App.xaml)):**
   - Preserve all existing RF-16/RF-10 keys in `Light`, `Dark`, and `HighContrast`, including `MobaPanelAccentHelperBrush`, `MobaPanelAccentCollectionBrush`, `MobaPanelAccentPropertiesBrush`, and top-level `TrackPlanSectionColor1..8`.
   - Add 9 semantic column role brushes (`AccentBrush`, `BadgeBackgroundBrush`, `BadgeBorderBrush` for `Collection`, `Helper`, and `Properties`) symmetrically across `Light`, `Dark`, and `HighContrast`:
     - **Collection (Signal Blue):** Light `#FF0078D4` (badge bg `#1A0078D4`, border `#400078D4`); Dark `#FF4CC2FF` (badge bg `#244CC2FF`, border `#4D4CC2FF`).
     - **Helper (Warm Amber Gold):** Light `#FF9E6B00` (badge bg `#1A9E6B00`, border `#4D9E6B00`, chosen for >= 3.8:1 icon contrast inside the tinted badge); Dark `#FFFFCA28` (badge bg `#24FFCA28`, border `#4DFFCA28`).
     - **Properties (Copper Mandarin):** Light `#FFC24600` (badge bg `#1AC24600`, border `#4DC24600`, >= 4.1:1 icon contrast inside the tinted badge); Dark `#FFFF8C3A` (badge bg `#24FF8C3A`, border `#4DFF8C3A`).
     - **HighContrast:** `AccentBrush` and `BadgeBorderBrush` map to `{ThemeResource SystemColorWindowTextColor}`; `BadgeBackgroundBrush` maps to `{ThemeResource SystemColorWindowColor}` (verified by `ThemeDictionaryArchitectureTests`).
2. **Column Role Chrome & Collapsed Pill ([MOBAflow/Themes/Generic.xaml](../../MOBAflow/Themes/Generic.xaml)):**
   - Upgrade `CollapsibleColumnHelper`, `CollapsibleColumnCollection`, and `CollapsibleColumnProperties` templates:
     - `ExpandedPanel`: `3.5px` crisp left accent strip bound to `MobaColumn*AccentBrush` and `26x26` rounded (`CornerRadius="6"`) header icon badge bound to `MobaColumn*BadgeBackgroundBrush`, `MobaColumn*BadgeBorderBrush`, and `MobaColumn*AccentBrush`.
     - `CollapsedTab`: `32px` wide vertical Fluent pill (`MinHeight="48"`, `Padding="0,6"`, `CornerRadius="8"`, `ToolTipService.ToolTip="{TemplateBinding Header}"`, `AutomationProperties.Name="{TemplateBinding Header}"`) with a 2-column layout placing the `3px` role accent strip beside a `22x22` tinted icon badge.
   - Preserve `x:Name="CollapsedTab"`, `x:Name="CollapseButton"`, `x:Name="ExpandedPanel"`, `ExpansionStates`, `PanelContent` padding, and exact RF-16 `AutomationProperties.Name` values (`"Unpin panel"`, `"Collapse list"`, `"Collapse panel"`).
3. **Backwards-Compatible Converter Extension ([MOBAflow/Converter/BoolToAccentBrushConverter.cs](../../MOBAflow/Converter/BoolToAccentBrushConverter.cs)):**
   - Add `public string TrueResourceKey { get; set; } = "AccentFillColorDefaultBrush";` and an internal `ResolveResourceKey(bool isTrue, string? trueResourceKey = null)` helper covered by NUnit test cases in [Test/WinUI/ThemeConverterTests.cs](../../Test/WinUI/ThemeConverterTests.cs).
4. **Deferred Slices (Slice 1B+):**
   - Rolling-stock category brushes (`MobaLoco*`, `MobaPassengerWagon*`, `MobaGoodsWagon*`), `BoolToSuccessBrushConverter`, `MainWindow.xaml` StatusBar/Diagnostics chrome, page-level upgrades (`OverviewPage.xaml`, `TrainsPage.xaml`, `LocomotivesPage.xaml`, `JourneysPage.xaml`, `EventManagerPage.xaml`, `MonitorPage.xaml`), and `docs/wiki/PAGE-OVERVIEW.md` remain untouched in Slice 1A and will land in subsequent non-overlapping slices.

## Constitution Check

- `Domain`, `Common`, `Backend`, `Contracts`, and `SharedUI` remain free of WinUI/MAUI types.
- All new theme-dependent brushes reside in `MOBAflow/App.xaml` `ThemeDictionaries` with symmetric `Light`, `Dark`, and `HighContrast` (`SystemColor*`) entries.
- All UI strings, tooltips, and new comments are in English; CRLF line endings are preserved.
- No application, debugger, or hardware launch is performed during validation.

## Validation Strategy

- `scripts/Test-SpecKitGovernance.ps1 -Mode PullRequest -BaseRef github/main`.
- Portable (`net10.0`) Release rebuild of `Test/Test.csproj` with `-p:MobaAnalyzerGate=true`, `scripts/Test-AnalyzerBaseline.ps1` against `quality/analyzer-baseline.json`, and portable `dotnet test`.
- Windows (`net10.0-windows10.0.22621.0`) Release rebuild of `Test/Test.csproj` (which includes `MOBAflow/MOBAflow.csproj`) with `-p:MobaAnalyzerGate=true`, `scripts/Test-AnalyzerBaseline.ps1` against `quality/analyzer-baseline.windows.json`, and Windows `dotnet test` (covering `ThemeDictionaryArchitectureTests`, `AccessibleNameArchitectureTests`, and `ThemeConverterTests`).
- `dotnet format whitespace Moba.slnx --verify-no-changes --include ...`, `scripts/Test-MutationLaneCoverage.ps1`, `scripts/Test-LineEndings.ps1`, `scripts/Test-DocumentationLinks.ps1`, `scripts/documentation_consistency.py`, `scripts/Test-DocumentationConsistency.py`, and `sonar analyze secrets`.
- Open Draft PR for `#398` Slice 1A (`Part of #398`) and confirm GitHub CI and SonarCloud quality gate.
