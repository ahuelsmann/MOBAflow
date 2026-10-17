---
description: 'Preserve model-wrapper notifications and auto-save subscription lifetimes.'
applyTo: 'SharedUI/ViewModel/**/*.cs'
---

# Auto-save
Model wrappers must notify when persistent values change. See the Name property in
[JourneyViewModel](../../SharedUI/ViewModel/JourneyViewModel.cs): SetProperty updates the wrapped model
and raises PropertyChanged. Preserve nested propagation and computed-property notifications.

Selection/project changes must detach obsolete subscriptions and attach current ones.
Use [journey station editing](../../SharedUI/ViewModel/JourneysPageViewModel.cs) and
[session save filtering](../../SharedUI/Service/SolutionSession.cs) as concrete references: list selection and
the valid open properties editor share station tracking. An ItemsSource reset can clear list selection while
the editor still owns its station. Release tracking when neither owns it; genuine station/journey/project/
solution changes, wrapper replacement and disposal detach obsolete editors. The session tracks the canonical
journey, suppresses bulk-load saves, filters transient state and observes errors.
Do not assume every existing selection hook already follows the same lifecycle.

For nested changes, inspect [WorkflowViewModel](../../SharedUI/ViewModel/WorkflowViewModel.cs).
For project switching and disposal, inspect
[WorkflowLibraryViewModel](../../SharedUI/ViewModel/WorkflowLibraryViewModel.cs).
Preserve error observation and path/shutdown handling in
[MainWindowViewModel.Solution](../../SharedUI/ViewModel/MainWindowViewModel.Solution.cs).

Validate edits, selection changes and project switching with existing fakes and relevant tests.
Avoid duplicate saves, subscriptions to old projects and discarded asynchronous save failures.
