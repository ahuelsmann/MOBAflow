# Tasks: Fluent Design 2 Semantic Colors & Column Role Chrome (Slice 1A)

**GitHub Issue**: #398

**Spec Kit**: Required

Input: [spec.md](spec.md), [plan.md](plan.md).

## Slice 1A: ThemeDictionaries, Column Role Chrome & Converter Foundation

- [x] T001 Author `specs/022-fluent-design-accents/` (`spec.md`, `plan.md`, `tasks.md`) scoped to Slice 1A and referencing deferred Slice 1B+ work
- [x] T002 Add semantic column role brushes (`MobaColumnCollection*`, `MobaColumnHelper*`, `MobaColumnProperties*`) symmetrically across `Light`, `Dark`, and `HighContrast` in `MOBAflow/App.xaml`, preserving all existing RF-16 keys (FR-001, FR-002)
- [x] T003 Upgrade `CollapsibleColumnHelper`, `CollapsibleColumnCollection`, and `CollapsibleColumnProperties` templates (`ExpandedPanel` accent strip + header icon badge, `CollapsedTab` vertical pill + tooltip) in `MOBAflow/Themes/Generic.xaml` while preserving `x:Name` parts and `AutomationProperties.Name` (FR-003)
- [x] T004 Add backwards-compatible `TrueResourceKey` support to `MOBAflow/Converter/BoolToAccentBrushConverter.cs` and unit tests in `Test/WinUI/ThemeConverterTests.cs` (FR-004)

## Validation & Delivery

- [ ] T005 Run Spec Kit governance, portable and Windows Release builds, analyzer baselines, automated tests, whitespace formatting, mutation-lane coverage, line endings, documentation checks, and local secrets validation (SC-001, SC-002)
- [ ] T006 Open Draft PR (`Part of #398`, `antigravity/398-fluent-design-slice-1a`) and verify GitHub CI and Sonar quality gate at zero OPEN/CONFIRMED issues (SC-002)

## Deferred to Later Slices (Tracked under #398)

- [ ] T007 (Slice 1B+) `MainWindow.xaml` diagnostics/StatusBar chrome, `OverviewPage.xaml` KPI cards & health indicators, `TrainsPage.xaml`/`LocomotivesPage.xaml` rolling-stock category brushes, and `JourneysPage.xaml`/`EventManagerPage.xaml`/`MonitorPage.xaml` polish
