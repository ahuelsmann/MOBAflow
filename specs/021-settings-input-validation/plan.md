# Implementation Plan: Validate Settings inputs

**GitHub Issue**: #470
**Spec Kit**: Required
**Input**: [spec.md](spec.md) and the [maintainer decision](https://github.com/ahuelsmann/MOBAflow/issues/470#issuecomment-7105502485).

## Architecture

Keep `SettingsPageViewModel` an `ObservableObject`. WinUI `x:Bind` binds to editable drafts and explicit error properties. Effective settings remain in the existing settings model. Validate first, then reuse existing accepted-value update, change notification and persistence paths. Validate display endpoint candidates through the existing SharedUI reference to MOBAdisplay. Keep lexical path checks separate from injectable filesystem existence checks. SharedUI has no WinUI dependencies, and no project or package references need to change.

Text edits keep the current focus-loss behavior. NumberBoxes must surface the raw `double` at the guard, including fields that currently bind to an `int` property. Keep the persisted types and check range/integrality before casts where applicable. Use WinUI `ValidationMode="Disabled"` where field validation owns rejected drafts. The default `InvalidInputOverwritten` can replace invalid text before the application can explain it. Keep `Minimum` and `Maximum` for the spin buttons; enforce authoritative bounds, finiteness and integrality in the validation path. Check typed out-of-range and nonnumeric text on focus loss or Enter so WinUI value coercion never produces an accepted write. Browse, Piper voice selection and Reset resynchronize affected drafts and errors without a second write. [WinUI NumberBox guidance](https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/number-box)

Use the [Event Manager inline error pattern](../../MOBAflow/View/EventManagerPage.xaml) under text and number inputs, with caption style, critical brush, wrapping, polite announcements and visibility bound to the error state. Keep field errors out of the page InfoBar. Each Piper error sits under the whole existing horizontal row, preserving the button's `Margin="0,28,0,0"`.

## Field inventory and delivery order

The WinUI bindings in [SettingsPage.xaml](../../MOBAflow/View/SettingsPage.xaml) and the existing [view model](../../SharedUI/Desktop/ViewModel/SettingsPageViewModel.cs) are the source for the inventory. Confirm the complete binding list before writing code and update this table if main has changed.

| Slice | Fields and validation |
| --- | --- |
| 1 | **DisplayIpAddress**, **DisplayPort**, **RestApiPort**, **PhotoStoragePath**. Implement the draft/error basis, endpoint and filesystem tests and the A11y pattern for these inputs. Install the finite-input/checked-conversion guard for all nine NumberBoxes listed below. Add accessible names and help text for the REST API port and photo folder field, plus a distinct photo Browse name. Verify section sorting, startup/load switches, #471 Feature Toggles and `IsDarkMode` retain current behavior. |
| 2 | **Z21AutoConnectRetryInterval** (documented 5–60 whole seconds), **Z21SystemStatePollingInterval** (0 or 1–30 whole seconds), **HealthCheckIntervalSeconds** (5 to `int.MaxValue / 1000` whole seconds so `HealthCheckService` can convert to milliseconds without overflow), **CountOfFeedbackPoints** (0 through an explicit safe InPort allocation ceiling). Use the existing user-facing InPort range 1–512 as the proposed ceiling, verify it against counter and transport limits before coding, and keep legacy loaded values untouched. Check field names/help text, the health-check switch and the feedback event. `InvertPolarityOffset0..3Setting` is ViewModel-only and outside the SettingsPage control inventory; retain its existing behavior. |
| 3 | **PiperSentenceSilenceSeconds** (0–2 seconds with fractional values), **SpeechRate** (−10 to 10 whole units), **SpeechVolume** (0–100 percent, validate before conversion), **PiperExecutablePath**, **PiperModelPath**, **PiperConfigPath**. Preserve the selected provider, System Speech voice, Piper language/voice, diagnostics switch and test-message behavior. Validate choices against their existing source and preserve documented empty/default values and provider fallback. Give each Piper Browse button a distinct accessible name, preserve row alignment and review user wiki wording about blocked edits and ranges. |

The nine NumberBoxes are Display UDP Port, REST API Port, Z21 reconnect interval, Z21 system-state polling interval, health check interval, feedback point count, speech rate, speech volume and Piper sentence pause. Appearance/section controls, feature toggles and other constrained choices keep their existing valid choice sets and behavior. Regression-check `FeatureTogglesChanged`, `FeatureToggleRegistry.Normalize`, live menu refresh and Overview fallback from #471, and MainWindow's `IsDarkMode` sync.

Reset tests must verify the effective values and binding notifications together. Audit the existing `SettingsPageViewModel.ResetToDefaultsAsync` and `SettingsService.ApplyLoadedSettings` omissions for affected properties, especially photo/REST, health and speech/Piper settings. Refresh the relevant drafts, errors and applied bindings once, while preserving the feature-toggle and theme sync paths.

## Constitution Check

The app retains its existing transport, settings schema and project boundaries. Changes follow test-first delivery in isolated branches and keep validation independent of hardware. Practical UI checks require separate launch permission.

## Validation Strategy

Write failing portable NUnit tests in `SettingsInputValidatorTests` and `SettingsPageViewModelTests` before the validators and view-model changes. Use `SaveSettingsAsync` `Times.Never` for rejection and `Times.Once` for valid correction. Check CI's `shared-ui` Stryker lane (`MutationTest/mutation-lanes.json`) and require green, or a documented rule-based skip, on the reviewed head. Use a filesystem stub or a temporary directory; do not depend on local paths, live networking, a speech engine or hardware. For each invalid input assert unchanged effective settings, no persistence and no application event. Pair invalid cases with valid correction and unchanged-value cases. Use the Windows build to check `x:Bind` and include focused WinUI or existing architecture tests as applicable. Run the affected portable/Windows suites and normal CI, checking the exact PR head. Capture practical WinUI Light/Dark/High Contrast, keyboard, Narrator and Browse/Reset evidence with the maintainer separately.

## Coordination

- Produce this spec-only Draft PR first. It locks only `specs/021-settings-input-validation/` and consumes no code WIP slot.
- Cut three sequential class M code slices. Start Slice 1 after spec integration, free locks and an available slot under the code WIP cap. Merge each slice before starting the next. #471 is integrated and its Settings locks have been cleared.
- Code slices may lock `SharedUI/Validation`, the affected settings view-model or view files, and the related tests. Name their exact scopes in the PR header and have the COO mirror them in Kanban at dispatch.
- P2 is the writer. P1 Opus Head accompanies the slices and provides a fresh independent final review, with P3 Carbon Head for UI/UX and accessibility and cross-family verification as required. Keep final checks tied to the reviewed head.
