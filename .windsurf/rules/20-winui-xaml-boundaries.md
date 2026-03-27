description: >
  WinUI and XAML guardrails for MOBAflow, including ThemeResource
  usage and UI-thread-safe patterns.
trigger: model_decision
---

<!-- markdownlint-disable MD003 MD041 -->

# WinUI and XAML Boundaries

- Treat `MOBAflow` as the WinUI project in this repository.
- Follow existing WinUI and XAML patterns before inventing new ones.
- Do not hardcode colors or brushes in XAML. Use `ThemeResource`-based resources.
- Keep code-behind minimal. Prefer ViewModel-driven behavior when practical.
- Respect CommunityToolkit.Mvvm patterns such as `[ObservableProperty]`
  and `[RelayCommand]` where the surrounding code already uses them.

## EventBus and UI Threading

- Respect the existing EventBus threading model.
- Do not add extra UI dispatch inside EventBus handlers when the
  existing decorator already guarantees UI-thread-safe delivery.
- Be careful when changing code around `AddEventBusWithUiDispatch()`
  and the UI-thread EventBus decorator.

## Layout and UI Changes

- Keep WinUI page changes small and locally consistent with the surrounding page.
- Reuse established controls, resources, and page structure when possible.
- Do not introduce layout-wide changes outside the scope of the requested task.
- Use the shared splitter and collapsible-column architecture only on:
  - `GoodsWagonPage`
  - `JourneysPage`
  - `LocomotivesPage`
  - `PassengerWagonPage`
  - `SolutionPage`
  - `SignalBoxPage`
  - `TrackPlanPage`
  - `WorkflowsPage`
- Keep these pages on a standard grid layout without
  `GridColumnResizeBehavior`, splitter columns, or collapsible-column
  side panels unless the user explicitly requests otherwise:
  - `TrainControlPage`
  - `SettingsPage`
  - `OverviewPage`
  - `MonitorPage`
  - `MainWindow`
  - `JourneyMapPage`
  - `InfoPage`
  - `HelpPage`
  - `DockingPage`
- For approved splitter pages, prefer the established explicit
  splitter-column pattern instead of layout heuristics.
- Do not mix docking layout behavior with the normal page-splitter architecture.

## Safety Checks for WinUI Work

- Check affected XAML and ViewModel files together.
- If behavior changes, suggest targeted validation for the impacted
  project and relevant tests where feasible.
