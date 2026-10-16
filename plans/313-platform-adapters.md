# RF-29: Platform adapters for sound and files

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/313
**Spec Kit**: Not applicable - structural refactoring without behavior change
**Status**: Planned; implementation starts with slice 1 after this plan is integrated

## Purpose and scope

[Issue #313](https://github.com/ahuelsmann/MOBAflow/issues/313) owns acceptance and live status.
The [umbrella plan](QUALITY-AND-REFACTORING-PLAN.md) owns programme dependencies. This is the single RF-29 child plan.

Target: the Windows sound player lives in the Windows host and `Sound` keeps only the platform-neutral contract;
each file role has its own implementation class, and consumers depend on the role interface they use. Speech
providers are already split into `Speech.Base`, `Speech.Piper` and `Speech.SystemSpeech` and are not part of RF-29.

## Source inventory and proposed owners

Counted on main `77157d13`.

| Current location | Content | Consumers | Proposed owner |
| --- | --- | --- | --- |
| `Sound/SoundManager.cs` `WindowsSoundPlayer` | `System.Media.SoundPlayer` playback | MOBAflow DI; two tests in `Test/Sound/NullSoundPlayerTests.cs` | `MOBAflow/Service/WindowsSoundPlayer.cs`; tests to `Test/MOBAflow` (Windows target) |
| `Sound/Sound.csproj` `System.Windows.Extensions` | Windows-only package needed only by `WindowsSoundPlayer` | `Sound` (built for Android through MOBAsmart) | removed from `Sound` |
| `Sound/ISoundPlayer.cs`, `Sound/NullSoundPlayer.cs` | Contract and null object | Backend, SharedUI, MOBAsmart, tests | stay |
| `SharedUI/Interface/IIoService.cs` | Role interfaces `ISolutionIoService`, `IFilePickerService`, `IPhotoStorageService` plus aggregate `IIoService` | ViewModels, `SolutionSession`, MOBAflow DI, tests | keep the role interfaces; remove `IIoService` |
| `MOBAflow/Service/IoService.cs` (575 lines) | Implements all three roles; window handle for pickers | MOBAflow DI, `MainWindow` | `SolutionFileService`, `FilePickerService`, `PhotoStorageService` in `MOBAflow/Service` |
| `SharedUI/Service/NullIoService.cs` | Null object for all roles | tests, MAUI/remote paths | one null object per role, or role-specific test doubles where only tests use it |

Consumers by role: `SolutionSession` (solution files); `LocomotivesPageViewModel`, `WagonsPageViewModel`,
`StationsPageViewModel` (photo picking and storage); `AudioViewModel` via `WorkflowActionViewModelFactory`,
`WorkflowViewModel`, `ProjectViewModel`, `SolutionViewModel` (audio file picking).

## Design decisions

- **Windows sound player in the host.** MOBAflow is the only host that plays WAV files; a separate
  `Sound.Windows` project would add a project for one class. MOBAsmart keeps `NullSoundPlayer`.
- **Role classes share the picker window.** `FilePickerService` owns the window handle (`SetWindowId`);
  `SolutionFileService` uses `IFilePickerService` for its open/save dialogs instead of duplicating picker code;
  `PhotoStorageService` keeps the `PhotoPathHelper` containment from RF-15.
- **No aggregate.** Each role interface is registered once; constructors request only the roles they use.

## Independently reviewable delivery slices

1. **Windows sound player.** Move `WindowsSoundPlayer` to MOBAflow, drop `System.Windows.Extensions` from
   `Sound`, move its tests to the Windows test target. Check: MOBAsmart Release/Android analyzer gate and build.
2. **File roles.** Split `IoService` and `NullIoService` by role, update DI and consumers, remove `IIoService`;
   tests for each role.
3. **Documentation and completion.** Update architecture docs and the umbrella plan, remove this plan.

## Validation strategy

Every slice: portable and Windows test suites, the cross-platform, Windows and Android analyzer gates, MOBAflow
FastDebug and MOBAsmart Android builds. Slice 2 additionally: DI container validation tests
(`MobaWinUiServiceCollectionExtensionsTests`) and the photo path tests. No app launch by the agent; manual
open/save/photo dialog checks are listed for the maintainer.

## Risks

- **Picker window handle:** all pickers must still receive the window handle after the split.
- **Android packaging:** removing the package must not change the MOBAsmart output beyond the dropped assembly.
- **Analyzer baselines:** moved diagnostics travel with their file.
