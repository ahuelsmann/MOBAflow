# RF-31: Reduce the remaining oversized classes

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/349
**Spec Kit**: Not applicable - structural refactoring without behavior change
**Status**: Active; slice 1 ships with this plan

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
| `MOBAflow/View/HelpPage.xaml.cs` | 663 | navigation and section state, panel layout persistence | **Slice 4**: navigation/section state into a ViewModel; the layout persistence follows RF-24 slice 6 |
| `MOBAflow/View/MainWindow.xaml.cs` | 514 | shell lifecycle plus remaining feature wiring | **Slice 5** after RF-24 (#219, #293), then documentation and completion |

## Independently reviewable delivery slices

Slices 1 and 2 were integrated as separate pull requests. Since 2026-10-20 each worked issue has one active delivery
PR, so slices 3 and 4 and the later ones are commits of that single RF-31 PR.

1. **Recording session validation and drop bookkeeping** (with this plan). The existing
   `RecordingSessionServiceTests`, `RecordingArtifactSerializerTests`, `RecordingEventCaptureTests` and
   `RecordingReplayServiceTests` (129 tests) stay unchanged and green; the extracted classes are internal and
   covered through them.
2. **Recording control entries and artifact import.** Round-trip and import-limit tests unchanged.
3. **Signal-box properties code-behind.** Compile MOBAflow; maintainer check in Light and Dark.
4. **Help page ViewModel.** Compile MOBAflow; maintainer check in Light and Dark.
5. **Main window code-behind, documentation and completion** after RF-24.

## Validation strategy

Backend slices: portable and Windows test suites and the cross-platform, Windows and Android analyzer gates.
WinUI slices: additionally the MOBAflow compile and a maintainer check of the affected view in Light and Dark,
recorded on #349. No app launch by the agent.

## Risks

- **Concurrency:** the session's lock, channel and consumer stay unchanged. The stateless validator runs before
  `Start` and `TryRecord` acquire the session lock. The mutable drop tracker is called under that lock and owns
  no lock of its own.
- **Open PRs:** #293 (RF-24) touches `MainWindow`; slice 5 waits for it.
