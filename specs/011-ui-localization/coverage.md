# Localization coverage

Source issue: [#140](https://github.com/ahuelsmann/MOBAflow/issues/140).
Baseline: `7c1d92422c0cbf675fddc03a8979990957733d15` (2026-10-14).
Inventory below describes scope, not completed implementation or manual acceptance.

| Platform/surface | Files | Scope | Delivery/acceptance |
| --- | --- | --- | --- |
| Desktop navigation | `MOBAflow/Service/NavigationItemFactory.cs`, `NavigationRegistration.cs` | Page titles/tooltips; preserve tags and custom badge suffixes | Slice 1; pending |
| Desktop UI language | `MOBAflow/View/SettingsPage.xaml`, new language area | Language heading, choices, save/restart/error text and accessible names only | Slice 1; pending |
| Desktop Info | `MOBAflow/View/InfoPage.xaml`, `.xaml.cs` | All prose, headings, links/accessibility names; version/product/URLs invariant | Slice 1; pending |
| Desktop Help | `MOBAflow/View/HelpPage.xaml`, `.xaml.cs` | View chrome and contents navigation; embedded/remote article bodies excluded | Slice 1; pending |
| Android tabs | `MOBAsmart/Controls/AppBottomTabBar.xaml` | Four labels and accessible names; tab IDs/routing invariant | Slice 1; pending |
| Android UI language | new `MOBAsmart/Controls/LanguageSettingsView.xaml`, hosted by `MOBAsmart/MainPage.xaml` | New self-contained settings area, not Counter behavior | Slice 1; pending |
| Shared dynamic text | new `SharedUI/Localization/`, new language ViewModel | Language save/restart/error templates; numeric/date formatting contract | Slice 1; pending |
| Desktop shell body | `MOBAflow/View/MainWindow.xaml`, related status/command partials | Menus, operational statuses, command dialogs | Follow-up in #140 |
| Other desktop Settings | `MOBAflow/View/SettingsPage.xaml`, `.xaml.cs`, `.Layout.cs`, `SharedUI/ViewModel/SettingsPageViewModel*` | Existing connection, display, speech, feature toggles/layout and reset sections | Follow-up in #140; speech coordination with #136 |
| Desktop pages | `MOBAflow/View/OverviewPage.xaml`, `SolutionPage.xaml`, `StationsPage.xaml`, `LocomotivesPage.xaml`, `GoodsWagonPage.xaml`, `PassengerWagonPage.xaml`, `TrainsPage.xaml` | Page bodies, dialogs, templates and corresponding shared ViewModels | Follow-up in #140 |
| Journey/Workflow/Counter | `MOBAflow/View/JourneysPage.xaml`, `WorkflowsPage.xaml`, `TimetablePage.xaml`, `EventManagerPage.xaml`, `JourneyMapPage.xaml`, `SharedUI/ViewModel/MainWindowViewModel.Journey.cs`, `.Workflow.cs`, `.Counter.cs`, `MOBAsmart/MainPage.xaml` | Existing operational texts only; no competing extraction | Follow-up after checking RF-24/#219 file ownership |
| Other desktop tools | `MOBAflow/View/TrainControlPage.xaml`, `TrackPlanPage.xaml`, `SignalBoxPage.xaml`, `MonitorPage.xaml`, `RecorderPage.xaml`, `MatrixPage.xaml`, `DisplayPage.xaml` | Bodies, controls, converters, dialogs and shared status text | Follow-up in #140 |
| Android operational UI | `MOBAsmart/MainPage.xaml`, `View/EnginePage.xaml`, `ControlPage.xaml`, `SignalBoxPage.xaml`, `SplashPage.xaml`, connection/control components and `SharedUI/Mobile/` | Existing body/status/dialog/empty-state text | Follow-up in #140 |

Each included surface must be checked for static text, dynamic text, full templates,
tooltips, accessible names, error/empty/status states, formatting and user-data boundaries.
Only update a row to implemented when resource and build checks exist; record manual acceptance separately.
