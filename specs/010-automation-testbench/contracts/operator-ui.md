# Operator UI Contract

**Source Issue**: #35
**Status**: Proposed Windows MVP; controls and tests are not implemented.

## Editing and controls

Register a transient AutomationTestbenchPage and singleton CommunityToolkit ViewModel, using existing PageMetadata/navigation
registration. The VM uses project/session/dialog services and isolated feature control, never IMobaRuntime, live simulation,
railway runtime surfaces or concrete gateway construction. The private whistle boundary stays in Backend/Common.

Show persistent English labels "Isolated test run" and "Synthetic input". Present scenario CRUD/selection, explicit counts
and every journey's activation/current stop, timed logical feedback, typed expectations and readable chronological results.
Use existing ThemeResource brushes, focus/keyboard commands and star units for any persisted resizable star columns.
No new feature-toggle/configuration default is needed.

| Control | Behavior |
| --- | --- |
| Play | Validate/capture and run, or resume Paused |
| Pause | Settle current virtual instant, then prevent future progression |
| Single step | Initialize if necessary; settle the next instant only; return to Paused or terminal |
| Cancel | Await owned cancellation/cleanup and retain trace |
| Restart | Dispose former run, revalidate and create fresh state |
| Playback speed | Finite positive, default 1.0; wall-time pacing only, no execution-order change |

Lock scenario execution edits during Validating, Running, Paused and Cancelling. Terminal states allow correction/retry.
Invalid controls produce a recoverable error and preserve the current state. Results distinguish InvalidScenario,
CompletedWithoutExpectations, Passed, Mismatch, ExecutionFailed, Cancelled and TestbenchError.
Show first failed expectation and all outcomes/full trace; do not imply unspecified behavior was verified.

Definition wrappers propagate changes through the existing save pattern. Run projection/trace/control state never attaches
to Project/SolutionSession change tracking. Deep duplicate renews owned IDs; run updates cause zero definition saves.

Private runner notifications are outside the decorated application EventBus: marshal typed notifications through the
existing UI dispatcher. EventBus-based definition handlers continue updating UI directly through its existing decorator.
Attach/detach page subscriptions on activation/departure; generation checks suppress stale callbacks only after joined
disposal, never as a replacement for it.

## Awaited departure barrier

Add a narrow optional testbench run guard to existing transition paths. Await cancel/drain before the first context/history
mutation. Failed drain preserves old page/project/solution and exposes a recoverable error. No general lifecycle registry.

| Existing boundary | Required insertion |
| --- | --- |
| NavigationService.NavigateToPageAsync | Before history push and Frame.Content replacement |
| NavigationService.GoBackAsync | Before history pop and Frame.Content replacement |
| SolutionSession.NewSolutionAsync | Before clearing Projects |
| SolutionSession.LoadSolutionAsync / LoadSolutionFromPathAsync | After successful read, before ApplyLoadResult / ApplyLoadedSolution |
| MainWindowViewModel.SelectedProject / SolutionPage TwoWay selection | Replace UI selection path with awaited command; commit only after drain; direct synchronous setter cannot bypass guard |
| MainWindowViewModel.AddProject | Await guard before changing selected project/context |
| MainWindowViewModel.DeleteProjectAsync | Before ClearAllSelections/removal |
| MainWindowViewModel.PrepareForShutdownAsync | Before TryBeginShutdown/context teardown |

Current evidence: NavigationService.cs:58/120; SolutionSession.cs:218/256/266/445;
MainWindowViewModel.Solution.cs:65; SolutionPage.xaml:62; MainWindowViewModel.cs:384/393/450.
Use awaited commands, not async-void property hooks or post-mutation replacing events.
Ordinary file read may precede drain, but no loaded data is applied until it completes. A failed load leaves the active run
untouched. Prompt for an unsaved definition through existing dialog/save behavior, without adding a new approval flow.

## Acceptance

Use a controllable delayed guard to prove selection/page/history/session remain unchanged until drain completes, and that
failure preserves them. Cover direct selection bypass, add/delete/New/Load/back/shutdown, repeated Cancel and late callbacks.
Verify editing causes normal saves while running/trace updates cause none. Compile active XAML without Page Remove exclusions.
After explicit launch approval, inspect Light/Dark/High Contrast, keyboard/focus/Narrator and disconnected create-run-inspect.
No physical layout connection or example solution is needed.
