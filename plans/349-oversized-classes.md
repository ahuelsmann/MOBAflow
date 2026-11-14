# RF-31: Reduce the remaining oversized classes

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/349
**Spec Kit**: Not applicable - structural refactoring without behavior change
**Status**: Complete with slice 5; slices 1 (#350), 2 (#353) and 3-4 (#354) are integrated

## Purpose and scope

[Issue #349](https://github.com/ahuelsmann/MOBAflow/issues/349) owns acceptance and live status.
The [umbrella plan](QUALITY-AND-REFACTORING-PLAN.md) owns programme dependencies. This is the single RF-31 child plan.

Target: each resulting class has one reason to change; code-behind keeps only view lifecycle and visual adaptation;
the recording round-trip tests pass unchanged. No recording format, user-visible behavior or setting changes.

## Source inventory and decisions

Inventory on main `de4af109`:

| Class | Lines | Responsibilities today | Decision |
| --- | --- | --- | --- |
| `Backend/Service/Recording/RecordingSessionService.cs` | 896 | lifecycle and single-consumer pipeline; request/projection validation; bookkeeping of entries dropped at ingestion capacity; control-entry content | **Slice 1**: validation → `RecordingRequestValidator`, drop bookkeeping → `RecordingDropTracker` (both internal); stateless validation runs before the session lock, while mutable drop-tracker state is accessed under that lock; **slice 2**: control-entry content. The lock, channel and consumer stay in the session |
| `Backend/Service/Recording/RecordingArtifactSerializer.Import.cs` | 802 | file-format parsing, limit enforcement, payload validation | **Slice 2**: separate parsing from validation behind the existing serializer API |
| `MOBAflow/Controls/SignalBox/SignalBoxPropertiesControl.xaml.cs` | - | property editing behavior in code-behind (partly moved by RF-14) | **Slice 3**: remaining behavior into the signal-box ViewModel |
| `MOBAflow/View/HelpPage.xaml.cs` | 663 | navigation and section state, panel layout persistence | **Slice 4**: navigation/section state into a ViewModel; the layout persistence goes through `LayoutPersistenceService` since RF-24 slice 6 (#442) |
| `MOBAflow/View/MainWindow.xaml.cs` | 514 | shell lifecycle plus remaining feature wiring | **Slice 5** (509 lines before, 381 after): host services, health checks, runtime observers and their shutdown → `MainWindowLifecycleService`; the track power switch decision → `MainWindowViewModel.ApplyTrackPowerSwitchCommand` |

## Independently reviewable delivery slices

Slices 1 and 2 were integrated as separate pull requests. Since 2026-10-20 each worked issue has one active delivery
PR; slices 3 and 4 were integrated together through #354, and slice 5 followed after RF-24 was complete.

1. **Recording session validation and drop bookkeeping** (with this plan). The existing
   `RecordingSessionServiceTests`, `RecordingArtifactSerializerTests`, `RecordingEventCaptureTests` and
   `RecordingReplayServiceTests` (129 tests) stay unchanged and green; the extracted classes are internal and
   covered through them.
2. **Recording control entries and artifact import.** Round-trip and import-limit tests unchanged.
3. **Signal-box properties code-behind.** Compile MOBAflow; maintainer check in Light and Dark.
4. **Help page ViewModel.** Compile MOBAflow; maintainer check in Light and Dark.
5. **Main window code-behind, documentation and completion.** Delivered after RF-24 (#219) was complete.
   Moved without behavior change:
   - `MainWindowLifecycleService` (`MOBAflow/Service/`, internal, one host singleton) owns the start of REST API
     status polling after the deferred UI startup, its pause and resume on window activation, the speech
     health checks with their forwarding to `SystemStatus.UpdateHealthStatus`, the
     release of the runtime observers (counters, traffic monitor, Solution page, signal-box projection) and the
     shutdown sequence: shell workflow, then REST API status service, MOBApi process and health checks, each
     failing step logged and the next one run. `PostStartupInitializationService` starts the health checks
     through it instead of through the window. The composition root hands it the shell's shutdown workflow with the observers (`ShellShutdownWorkflow`) and
     `SystemStatusViewModel`, so it does not depend on `MainWindowViewModel` and the allow-list of
     `SolutionSessionArchitectureTests` is unchanged.
   - `MainWindowViewModel.ApplyTrackPowerSwitchCommand` decides whether a position of the status bar switch is
     operator input or only the runtime synchronization; the window forwards the position.
   - The theme switch has the automation name `Dark theme` (RF-16 finding).

   Kept in `MainWindow.xaml.cs` because it is window lifecycle or visual adaptation: title bar, icon, minimum
   size and maximizing; the version text of the title bar; building the navigation menu, the content frame and
   the item-invoked forward to `NavigationService`; reporting the shown page tag to the shell; executing the
   shell's Z21 status refresh command when the window becomes active; scheduling the
   deferred UI startup on the dispatcher queue; subscribing and detaching window, activation and binding events;
   the requested theme of the root grid; the opacity of the track power icon; and turning the window's closing
   into a shutdown request. Its shutdown method only orders the steps: confirm through the service, detach the
   window events, stop the host services, detach the bindings.

   Tests: `MainWindowLifecycleServiceTests` (host registration, health forwarding until stop, observers released
   once on a confirmed shutdown, kept on a cancelled one, released by the stop after a failed shell workflow) and
   `MainWindowViewModelTrackPowerSwitchTests`. Not covered by a test: the pause and resume of the status polling,
   which has no observable state without a running REST API.

## Validation strategy

Backend slices: portable and Windows test suites and the cross-platform, Windows and Android analyzer gates.
WinUI slices: additionally the MOBAflow compile. No app launch by the agent; since 2026-11-06 (#417) personal UI
acceptance no longer gates a slice, so each pull request records the Light and Dark states it did not inspect in a
running app and problems found in everyday use are reported as bugs.

## Risks

- **Concurrency:** the session's lock, channel and consumer stay unchanged. The stateless validator runs before
  `Start` and `TryRecord` acquire the session lock. The mutable drop tracker is called under that lock and owns
  no lock of its own.
- **Shutdown order:** the window still detaches its own events after the shell confirmed shutdown and before the
  host services stop, so no activation event reaches a stopped status service.
