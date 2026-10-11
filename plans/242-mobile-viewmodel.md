# RF-12: Decompose MauiViewModel

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/242
**Spec Kit**: Not applicable - behavior-preserving decomposition; no product, protocol or persisted-format change
**Status**: Plan for review; slice 1 follows in its own PR
**Evidence**: main commit `31d2d3a635b4d8ea244ffbd99af29809a536d31e`, inspected on 2026-10-11

## Purpose and scope

[Issue #242](https://github.com/ahuelsmann/MOBAflow/issues/242) owns acceptance and live status.
The [umbrella plan](QUALITY-AND-REFACTORING-PLAN.md) owns programme dependencies.
This is the single RF-12 child plan; it transfers the umbrella's extraction order and acceptance anchor.

The outcome is a mobile root ViewModel that binds the MOBAsmart pages and composes focused collaborators.
Preserve bindings, commands, defaults, settings persistence, the local/remote command routing through
`IRuntimeCommandGateway`, the phone's project selection from RF-23 and the UI dispatcher boundaries.
Remove superseded code after each extraction rather than moving partial files.

No protocol, MOBApi, persisted-format or new user-visible behavior is included; RF-11, RF-24 and the
solution-wide constructor policy of RF-25 stay out of scope.

## Source inventory

`SharedUI/ViewModel/MauiViewModel*.cs` has 3,534 lines in nine partial files and 22 constructor
parameters (eight optional). Responsibilities by file:

| File | Lines | Responsibilities |
| --- | ---: | --- |
| `MauiViewModel.cs` | 1,480 | construction, startup, MOBApi discovery and cadence, REST health loop, SignalR hub connect retries, Z21 connect commands, feedback statistics, settings load/save, runtime snapshot handlers, photo upload |
| `MauiViewModel.MobaflowConnection.cs` | 655 | MOBAflow toggle, stored-endpoint connect, discovery helpers, coordinator state, solution catalog sync after connect, `SolutionUpdated` |
| `MauiViewModel.ControlTab.cs` | 368 | locomotive fleet sources, caching and publishing, project-context wiring |
| `MauiViewModel.TabUpdates.cs` | 353 | heavy-update pausing, signal-box caches, mobile cache restore |
| `MauiViewModel.SignalBox.cs` | 293 | signal-box element ViewModels and aspect commands |
| `MauiViewModel.BackgroundService.cs` | 116 | Android foreground service keep-alive, resume |
| `MauiViewModel.Session.cs` | 109 | hub/remote Z21 session flags and derived availability |
| `MauiViewModel.Project.cs` | 95 | project picker and the selected project's Z21 |
| `MauiViewModel.ConnectionStatus.cs` | 65 | status texts and indicator resource keys |

Cross-cutting state that the slices must hand over explicitly instead of sharing fields:

- `AppSettings.RestApi` (endpoint, enabled flag, recent addresses) and `ISettingsService` saves;
- `IsMobaflowConnectionEnabled`, `IsRestApiReachable`, `RestApiIpAddress`/`RestApiPort` and the
  selected project's `Z21IpAddress` used as discovery anchor;
- `_mobaflowConnectInProgress`, `_mobaflowInitialCatalogApplied`, `_applicationLifetimeCts`;
- `RunInBackground`, `IUiDispatcher` marshalling and the `IEventBus` subscriptions.

Existing tests: `MauiViewModelControlTabTests` (13), `MauiViewModelInitializationTests` (8),
`MauiViewModelMobaflowOfflineTests` (23), `MauiViewModelPerformanceTests` (16) and
`MobileRuntimeCoordinatorTests` (9) in `Test/SharedUI`.

## Target collaborators

All live in `SharedUI/Mobile` (namespace `Moba.SharedUI.Mobile`), depend only on existing interfaces and
`TimeProvider`, raise plain events or return results, and never touch MAUI types or bindable properties.
The root ViewModel maps their results to its observable properties on the UI thread.

1. `MobaflowEndpointLocator`: stored endpoint, fast/full LAN discovery anchored on the selected project's
   Z21, startup retries (0/5/15 s), periodic rediscovery cadence (10 s in the first 90 s, then 25 s),
   persisting a found endpoint and the recent-address history, health-check delay.
2. `MobaflowHubSession`: REST health check, hub connect with retries and forced reconnect, session state
   (`IsRuntimeHubConnected`, `IsRemoteZ21Connected`), client registration cadence, project registration.
3. `MobileRuntimeProjection`: local and remote snapshot application rules, locomotive fleet and
   signal-box caches with their project filters and refresh requests.
4. `MobileSolutionSynchronizer`: initial catalog after connect, `SolutionUpdated`, cache restore and
   `RequestSolutionSyncAsync`.
5. `MobilePhotoUploader`: capture, upload and status texts.
6. `MobileAppLifecycle`: background-service keep-alive, resume, stopping, network-change debounce.
7. The remaining root moves to `SharedUI/Mobile/MauiViewModel.cs`.

## Alternatives considered

- Keep partial files and only move methods: rejected, the shared fields keep every concern coupled.
- One `MobaflowConnectionService` for slices 1, 2 and 4: rejected, discovery cadence, hub retries and
  catalog sync change for different reasons and need separate time and failure tests.
- Move the ViewModel to `MOBAsmart`: rejected by the umbrella plan, the portable test run excludes
  `MOBAsmart`.

## Risks

- Threading: today some state changes run on background tasks and are marshalled per property. Each
  collaborator returns results; the root keeps the existing `InvokeOnUi`/`InvokeOnUiAsync` calls.
- Timing-based behavior (discovery cadence, health delays) is currently tied to `DateTime.UtcNow`;
  collaborators take `TimeProvider` so tests control time.
- Startup order (settings load, runtime start, discovery, stored-endpoint connect) must stay unchanged;
  `MauiViewModelInitializationTests` guard it and get characterization cases where gaps exist.
- Concurrent work: RF-24 and Codex branches do not touch these files at the inspection commit;
  re-check `git worktree list` and recent pushes before each slice.

## Validation per slice

- Characterization tests for the extracted behavior before moving it; collaborator tests with fakes and
  `FakeTimeProvider` afterwards; superseded ViewModel tests move to the collaborator.
- Portable and Windows test suites, the MAUI DI validator, the Android Release build and the
  cross-platform, Windows and Android analyzer gates.
- No device or hardware check is needed for a behavior-preserving slice; manual checks on a phone need
  separate approval.

## Delivery slices

Each slice is one reversible PR against `main`, in this order:

1. Endpoint locator (target 1).
2. Hub session (target 2).
3. Runtime projection (target 3).
4. Solution synchronizer (target 4).
5. Photo uploader (target 5).
6. App lifecycle (target 6).
7. Namespace move and constructor cleanup of the root (target 7); delete this plan when #242 closes.
