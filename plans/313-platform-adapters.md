# RF-29: Platform adapters for sound and files

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/313
**Spec Kit**: Not applicable - structural refactoring without behavior change
**Status**: Implementation delivered (slice 1 #315, slice 2 #317) and documented (platform adapter rule in
[ARCHITECTURE.md](../docs/ARCHITECTURE.md#platform-adapter-rule)); open: the maintainer desktop acceptance below,
recorded on #313. This plan is removed only after that acceptance is recorded.

## Purpose and scope

[Issue #313](https://github.com/ahuelsmann/MOBAflow/issues/313) owns acceptance and live status.
The [umbrella plan](QUALITY-AND-REFACTORING-PLAN.md) owns programme dependencies. This is the single RF-29 child plan.

Target: the Windows sound player lives in the Windows host and `Sound` keeps only the platform-neutral contract;
each file role has its own implementation class, and consumers depend on the role interface they use. Speech
providers are already split into `Speech.Base`, `Speech.Piper` and `Speech.SystemSpeech` and are not part of RF-29.

## Source inventory and proposed owners

Counted on main `77157d13`; consumers re-checked on current main.

| Current location | Content | Consumers | Proposed owner |
| --- | --- | --- | --- |
| `Sound/SoundManager.cs` `WindowsSoundPlayer` | `System.Media.SoundPlayer` playback | MOBAflow DI; two tests in `Test/Sound/NullSoundPlayerTests.cs` | `MOBAflow/Service/WindowsSoundPlayer.cs`; tests to `Test/MOBAflow` (Windows target) |
| `Sound/Sound.csproj` `System.Windows.Extensions` | Windows-only package needed only by `WindowsSoundPlayer` | `Sound` (built for Android through MOBAsmart) | removed from `Sound` |
| `Sound/ISoundPlayer.cs`, `Sound/NullSoundPlayer.cs` | Contract and null object | Backend, SharedUI, MOBAsmart, tests | stay |
| `SharedUI/Interface/IIoService.cs` | Role interfaces `ISolutionIoService`, `IFilePickerService`, `IPhotoStorageService` plus aggregate `IIoService` | ViewModels, `SolutionSession`, MOBAflow DI, tests | keep the role interfaces; remove `IIoService` |
| `MOBAflow/Service/IoService.cs` (575 lines) | Implements all three roles; window handle for pickers | MOBAflow DI, `MainWindow` | `SolutionFileService`, `FilePickerService`, `PhotoStorageService` in `MOBAflow/Service` |
| `SharedUI/Service/NullIoService.cs` | Null object for all roles | tests, MAUI/remote paths | one null object per role, or role-specific test doubles where only tests use it |

Consumers by role (checked on current main): `SolutionSession` (solution files); `LocomotivesPageViewModel`,
`WagonsPageViewModel` (photo picking and storage); `AudioViewModel` via `WorkflowActionViewModelFactory`,
`WorkflowViewModel`, `ProjectViewModel`, `SolutionViewModel` (audio file picking); `LocomotiveManagementViewModel`
and `RecordingFileService` (`IFilePickerService` only). `MainWindow` and `WinUiDiContainerValidator` also reference
the interfaces and are updated with the DI registration.

## Design decisions

- **Windows sound player in the host.** MOBAflow is the only host that plays WAV files; a separate
  `Sound.Windows` project would add a project for one class. MOBAsmart keeps `NullSoundPlayer`.
- **Role classes share the main window.** A host-internal `WinUiWindowContext` holds the window id and XAML
  root; the main window sets it once. `FilePickerService` and `SolutionFileService` read it, so the solution
  dialogs (suggested file name, unsaved-changes prompt) move unchanged instead of being rebuilt on
  `IFilePickerService`. `PhotoStorageService` keeps the `PhotoPathHelper` containment from RF-15.
- **Null objects per role.** `NullSolutionIoService` (`CanPersist` is false) and `NullFilePickerService`
  replace `NullIoService`; photo storage has no null object because only the desktop pages use it.
- **No aggregate.** Each role interface is registered once; constructors request only the roles they use.

## Independently reviewable delivery slices

1. **Windows sound player.** Move `WindowsSoundPlayer` to MOBAflow, drop `System.Windows.Extensions` from
   `Sound`, move its tests to the Windows test target. Check: MOBAsmart Release/Android analyzer gate and build.
2. **File roles.** Split `IoService` and `NullIoService` by role, update DI and consumers, remove `IIoService`;
   tests for each role.
3. **Documentation and completion.** Update architecture docs, mark RF-29 complete in the umbrella plan (its RF-29
   section already links this plan) and remove this plan.

## Validation strategy

Every slice, from the repository root:

| Check | Command | Expected |
| --- | --- | --- |
| Portable tests | `dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0` | all discovered tests pass |
| Windows tests | `dotnet test Test/Test.csproj -f net10.0-windows10.0.22621.0 -p:IncludeMobaSmartTests=false` | all pass; the moved sound tests and slice 2 role tests are discovered and executed |
| Desktop compile | `dotnet build MOBAflow/MOBAflow.csproj -c FastDebug --no-restore -p:BuildMOBApiDependency=false -p:CopyMOBApiToOutput=false` | 0 errors |
| Android compile | `dotnet build MOBAsmart/MOBAsmart.csproj -f net10.0-android -c FastDebug --no-restore` | 0 errors; slice 1: `System.Windows.Extensions` no longer restored for `Sound` |
| Analyzer gates | cross-platform, Windows and Android gates of [GitHub quality CI](../.github/workflows/quality.yml) | baselines unchanged except for moved diagnostics |
| Slice 2 only | filters `MobaWinUiServiceCollectionExtensionsTests` and the photo path fixtures | DI container validates; photo paths stay inside the photo root |

Gates per slice PR: local secrets scan and `scripts/Test-LineEndings.ps1 -Staged` before commit, draft PR, green
SonarCloud check on the current commit with zero OPEN/CONFIRMED PR issues, green CI, independent Claude review.
Evidence stays in the PR description and the check runs of its final commit. No app launch by the agent; the
maintainer checks open, save, save-as, unsaved-changes prompt, audio-file picker and photo pick/assign in the
desktop app (Light and Dark) and records the result on #313.

## Risks, mitigations, stop conditions and rollback

| Risk | Mitigation | Stop condition |
| --- | --- | --- |
| **Picker window handle:** all pickers must still receive the window handle after the split. | One `WinUiWindowContext`, set once by the main window; a test per role that fails when the context is unset. | A picker opens without an owner window, or the open/save dialogs differ from before the split. |
| **Android packaging:** removing the package must not change the MOBAsmart output beyond the dropped assembly. | Compare the restore graph and bundle contents before and after slice 1. | Any other assembly or resource disappears, or the Android build or analyzer gate fails. |
| **Photo containment:** photo storage must keep the `PhotoPathHelper` containment from RF-15. | Move the code unchanged and keep the existing photo path tests. | A photo path test fails or a path escapes the photo root. |
| **Analyzer baselines:** moved diagnostics travel with their file. | Move baseline entries in the same commit. | A gate reports new diagnostics that are not moved ones. |

On a stop condition the slice stops and is reported on #313. Each slice is one squash-merged PR, so rollback is
a revert of that PR, which returns to the last green main (slice 1 and slice 2 do not depend on each other's
files). Persistence is unchanged: no serialized format or schema is touched, so no data migration is needed.
