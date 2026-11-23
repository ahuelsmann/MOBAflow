# Implementation Plan: Validate Settings inputs

**GitHub Issue**: #470
**Spec Kit**: Required
**Input**: [spec.md](spec.md) and the [maintainer decision](https://github.com/ahuelsmann/MOBAflow/issues/470#issuecomment-7105502485).

## Architecture

Keep `SettingsPageViewModel` an `ObservableObject`. WinUI `x:Bind` binds to editable drafts and explicit error properties. Effective settings remain in the existing settings model. Validate first, then reuse existing accepted-value update, change notification and persistence paths. Validate display endpoint candidates through the existing SharedUI reference to MOBAdisplay. Keep lexical path checks separate from injectable filesystem existence checks. SharedUI has no WinUI dependencies, and no project or package references need to change.

Text edits keep the current focus-loss behavior. NumberBoxes must surface the raw `double` at the guard, including fields that currently bind to an `int` property. Keep the persisted types and check range/integrality before casts where applicable. The UI must not clamp or coerce a rejected draft into a valid write. Browse and Reset resynchronize drafts and errors without a second write.

## Field inventory and delivery order

The WinUI bindings in [SettingsPage.xaml](../../MOBAflow/View/SettingsPage.xaml) and the existing [view model](../../SharedUI/ViewModel/SettingsPageViewModel.cs) are the source for the inventory. Confirm the complete binding list before writing code and update this table if main has changed.

| Slice | Fields and validation |
| --- | --- |
| 1 | **DisplayIpAddress**, **DisplayPort**, **RestApiPort**, **PhotoStoragePath**. Implement the draft/error basis, endpoint and filesystem tests and the A11y pattern for these inputs. Install the finite-input/checked-conversion guard for all nine NumberBoxes listed below. Verify session UI choices such as section sorting and automatic startup/load toggles constrain their input and preserve behavior. |
| 2 | **Z21AutoConnectRetryInterval** (documented 5–60 whole seconds), **Z21SystemStatePollingInterval** (0 or 1–30 whole seconds), **HealthCheckIntervalSeconds** (existing minimum 5 seconds and storage bound), **CountOfFeedbackPoints** (nonnegative whole count within the existing supported range). Verify the health-check switch, feedback event and the four signal-box polarity switches; retain boolean behavior and runtime contracts. |
| 3 | **PiperSentenceSilenceSeconds** (0–2 seconds with fractional values), **SpeechRate** (−10 to 10 whole units), **SpeechVolume** (0–100 percent, validate before conversion), **PiperExecutablePath**, **PiperModelPath**, **PiperConfigPath**. Preserve the selected provider, System Speech voice, Piper language/voice, diagnostics switch and test-message behavior. Validate choices against their existing source and preserve documented empty/default values. Review user wiki wording about blocked edits and the field ranges. |

The nine NumberBoxes are Display UDP Port, REST API Port, Z21 reconnect interval, Z21 system-state polling interval, health check interval, feedback point count, speech rate, speech volume and Piper sentence pause. Appearance/section controls, feature toggles and other constrained booleans/enumerations carry their existing behavior and valid choice sets; their inventory entries must not create new persistence or networking work.

## Constitution Check

The app retains its existing transport, settings schema and project boundaries. Changes follow test-first delivery in isolated branches and keep validation independent of hardware. Practical UI checks require separate launch permission.

## Validation Strategy

Write failing portable NUnit tests before the validators and view-model changes. Use a filesystem stub or a temporary directory; do not depend on local paths, live networking, a speech engine or hardware. For each invalid input assert unchanged effective settings, no persistence and no application event. Pair invalid cases with valid correction and unchanged-value cases. Use the Windows build to check `x:Bind` and include focused WinUI or existing architecture tests as applicable. Run the affected portable/Windows suites and normal CI, checking the exact PR head. Capture practical WinUI Light/Dark/High Contrast, keyboard, Narrator and Browse/Reset evidence with the maintainer separately.

## Coordination

- Produce this spec-only Draft PR first. It locks only `specs/021-settings-input-validation/` and consumes no code WIP slot.
- Cut three sequential class M code slices. Start Slice 1 after spec integration, free locks and an available slot under the code WIP cap. Merge each slice before starting the next. #471 is integrated and its Settings locks have been cleared.
- Code slices may lock `SharedUI/Validation`, the affected settings view-model or view files, and the related tests. Name their exact scopes in the PR header and have the COO mirror them in Kanban at dispatch.
- P2 is the writer. P1 Opus Head accompanies the slices and provides a fresh independent final review, with P3 Argon Head for UI/UX and accessibility and cross-family verification as required. Keep final checks tied to the reviewed head.
