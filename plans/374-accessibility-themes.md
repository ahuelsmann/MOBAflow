# RF-16: Accessibility and themes

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/374
**Spec Kit**: Not applicable - accessibility semantics, theme resources and keyboard access to existing commands without new product behavior; a step that would introduce a new interaction model gets its own Spec Kit feature first
**Status**: Active; steps 1 to 5 are implemented; step 6 is the single approved app check

## Purpose and scope

[Issue #374](https://github.com/ahuelsmann/MOBAflow/issues/374) owns acceptance and live status.
The [umbrella plan](QUALITY-AND-REFACTORING-PLAN.md) owns programme dependencies. This is the single RF-16 child plan.
Since 2026-10-20 each worked issue has one active delivery PR, so the steps below are commits of the RF-16 PR.

Target: an operator can run every critical workflow with keyboard only, with Narrator, at 200% text size and in the
Light, Dark and High Contrast themes. Fixed railway signal colors remain where they carry meaning.

## Critical workflow inventory

Recorded on main `9d35158a`. "Desktop" means MOBAflow (WinUI); MOBAsmart is listed separately.

| Workflow | Desktop surfaces | Why critical |
| --- | --- | --- |
| Connect to the Z21, switch track power, emergency stop | `MainWindow` shell, `OverviewPage`, `SettingsPage` | Safety: stopping trains must never depend on the mouse |
| Drive a locomotive (speed, direction, functions, stop) | `TrainControlPage`, `LocomotivesPage` | Most frequent operator task |
| Operate the signal box (signals, switches, routes) | `SignalBoxPage`, `SignalBoxCanvasControl`, `SignalBoxPropertiesControl` | Elements selectable only by pointer, no accessible names |
| Run and reset journeys | `JourneysPage`, `StationsPage`, `TrainsPage`, `JourneyMapPage` | Automation start and recovery |
| Open, save and switch projects | `SolutionPage`, `MainWindow` | Data safety |
| Edit the track plan | `TrackPlanPage` (Win2D canvas) | Custom-drawn editor, most unnamed buttons |
| Read status and help | `MonitorPage`, `HelpPage`, `InfoPage` | Diagnosis and orientation |

## Measured findings

Static scan of the 44 desktop XAML files and the desktop/SharedUI C# sources:

| Finding | Count | Where |
| --- | ---: | --- |
| Buttons without an accessible name (no `Content` text, `Label` or `AutomationProperties.Name`) | 43 | `TrackPlanPage` 8; `JourneysPage`, `MonitorPage`, `StationsPage`, `TrainsPage` 4 each; `Generic.xaml`, `EntityTemplates.xaml`, `SolutionPage`, `TrainControlPage` 3 each; wagon and locomotive pages 2 each; `FunctionSymbolPickerWindow` 1 |
| ... of those with only a tooltip | 31 | A tooltip is not an accessible name for Narrator |
| `AutomationProperties.Name` already set | 57 | Mostly `RecorderPage`, `WorkflowsPage`, `InfoPage`, `EventManagerPage`, `TrainControlPage` |
| Drawn surfaces without accessible elements | 2 | `SignalBoxCanvasControl` (XAML templates), `TrackPlanPage` (Win2D) |
| Literal XAML colors outside color dictionaries | 12 | `TrackPlanPage` 8, `Generic.xaml` 3, `SignalBoxToolboxControl` 1 |
| Literal XAML colors in color dictionaries | 149 | `App.xaml` 70 (theme dictionaries), `SignalGraphics` 46, `SignalBoxColors` 15, `TrackGraphics` 15, `Icons` 3 |
| `Colors.*` / `Color.FromArgb` in C# | 91 | `TrackPlanPage.xaml.cs` 24, `GaugeVisualRules` 13, `BrakeAndDoorState` 6, `KsSignalScreen` 5, others below 5 |
| Keyboard accelerators | 6 | Delete and Backspace in the signal box and track plan, `R` in the track plan, Ctrl+T in settings; none for stop or track power |
| Explicit tab order (`TabIndex`) | 0 | Tab order follows the visual tree |

MOBAsmart: 16 of 23 XAML files already use `SemanticProperties`; the remaining files and font scaling are step 5.

## Decisions

- **Accessible names:** every icon-only button gets an English `AutomationProperties.Name`, equal to its tooltip where
  one exists. An automated check (step 2) rejects new unnamed buttons, the same way the architecture guards read sources.
- **Fixed colors:** railway signal aspects (`SignalGraphics`, `SignalBoxColors`, `KsSignalScreen`), track graphics and
  gauge danger ranges keep fixed values because their meaning is the color. In High Contrast they still need a shape or
  text cue, so meaning is never conveyed by color alone. All other literal colors move to `ThemeResource` brushes.
- **Signal box** (user decision on #374, 2026-10-22: Tab and Enter): the signal box draws its elements with XAML
  templates, not Win2D. A transparent focus layer above the drawing gives every element a focusable button with the
  accessible name from `SignalBoxElementDescriptions` (SharedUI, for example "Signal A1, Ks1"); Enter or Space selects
  the element like a click, and operation continues in the properties panel. The layer is not hit-testable, so pointer
  selection, drag and drop and panning stay unchanged. Tab follows the element order of the plan.
- **Track plan:** a keyboard editing model for the track-plan editor would be new product behavior; it needs a Spec Kit
  feature and a user decision before implementation.
- **Emergency stop by keyboard** (user decision on #374, 2026-10-21): Pause always, and Space unless the focused element
  uses Space itself, trigger the existing train-control emergency stop of the selected locomotive; Ctrl+P and F10 toggle
  track power. `OperatorShortcuts` (SharedUI) owns the rule; `OperatorShortcutController` adapts WinUI keys and focus and
  is attached at startup, so `MainWindow` stays untouched while #293 is open. A global Z21 stop of all locomotives would
  be a separate Spec Kit feature.

## Delivery steps

1. **Inventory** (with this plan).
2. **Accessible names and guard:** the 43 buttons, plus a test that fails on a new unnamed icon-only button.
3. **Keyboard and Narrator for the critical workflows:** the emergency-stop and track-power shortcuts and the signal-box
   focus layer (#375). Second delivery: the signal aspect tiles in `SignalBoxPropertiesControl` and `SignalAspectPicker`
   were `Border` elements reacting to `PointerPressed`, so the signal-box workflow ended at the mouse once an element was
   selected. They are `ToggleButton`s now: Tab reaches them, Enter or Space sets the aspect, the accessible name is the
   aspect description ("Ks 1 - Proceed") and the checked state tells Narrator which aspect is current. The track-power
   switch in `MainWindow` has the name "Track Power", the accelerator hint Ctrl+P and a help text and tooltip naming
   Ctrl+P and F10. `AccessibleNameArchitectureTests` also rejects a `ToggleSwitch` without `Header`, on/off text or an
   automation name. Focus order on the critical pages follows the visual tree and stays unchanged; the controls use the
   system focus visuals.
4. **Theme resources and High Contrast:** the `HighContrast` theme dictionary, themed panel strips and Track Plan
   drawing (#375). Second delivery: while a Windows contrast theme is active, `KsSignalScreen` draws a ring in
   `SignalLampLitStrokeBrush` (`SystemColorWindowTextColor`) around every lit lamp, so a lit lamp differs from an off one
   by shape and not only by color; the lamp position then names the aspect as on the real Ks signal, and the pickers and
   the signal-box focus layer carry the aspect as text. The checked aspect tile uses the system toggle highlight, which
   the contrast theme defines. The toolbox detector marker uses the accent brush like the canvas detector. Fixed on
   purpose after review: signal lamp colors (`KsSignalScreen`, `SignalGraphics.xaml`, `SignalBoxColors.xaml`), gauge
   danger ranges and the Track Plan section palette (`Tag` values are data). The other C# color literals are fallbacks
   of `ThemeResourceResolver` lookups. `OccupiedToStrokeConverter` has fixed colors but no caller; removing it is a
   separate cleanup.
5. **MOBAsmart:** TalkBack reads each bottom tab with its selection state (`AppBottomTabBar`), each locomotive card as
   one item with name, digital address and selection (`LocomotivePickerItemView`), each signal aspect tile with its
   description and selection while its preview lamps and short label are excluded (`SignalAspectPicker`), and
   `KsSignalScreen` as "Signal aspect ..." with its speed indicators. Headings on Engines and SignalBox let TalkBack jump
   between sections. `FunctionToggleView`, `SpeedGaugeView` and the Counter and Control pages already had descriptions.
   Font scaling: MAUI labels follow the Android font size by default; fixed heights that would clip scaled text (reset
   button and count badge in `StatisticsPanel`) became minimum sizes. The speed digits inside the signal graphic keep
   their size because the description speaks the value. Not covered: the speed gauge draws its numbers on a
   `GraphicsView`, so they do not scale; its description carries the value.
6. **Acceptance and completion:** one approved app check after the PR builds, recorded on #374; then documentation and
   plan removal.

### What the step 6 app check verifies

The app is launched only with explicit approval; one check covers all items. Personal acceptance by the maintainer was
abolished on 2026-11-06, so states that this check does not inspect are recorded as limitations.

- **Narrator:** Signal Box: Tab through the elements, Enter selects, Tab into the properties panel reads each aspect tile
  with its description and on/off state; Enter sets the aspect. Train Control: emergency stop with its shortcut hint.
  Track Power switch: focus, the announced name and the Ctrl+P / F10 hint only; the switch is never toggled in this check.
- **Keyboard only:** the signal-box path above without a mouse. The shortcuts were verified on the Z21 on 2026-10-22.
- **200% text size:** Train Control, the Signal Box properties panel and Settings without clipped labels.
- **Light, Dark, High Contrast:** aspect tiles and the checked tile, the lit-lamp ring in High Contrast, the toolbox
  detector marker and the Track Plan drawing.
- **MOBAsmart (device or emulator, same approval):** TalkBack on the four tabs, a locomotive card, an aspect tile and
  the speed gauge; the largest Android font size on Counter, Engines and Control.

## Validation strategy

- Steps 2-5: compile MOBAflow (and MOBAsmart for step 5), run the Windows suite and the analyzer gates; the step 2 guard
  must fail on a deliberately unnamed button in its own self-test.
- Step 6 needs one explicitly approved app launch; Narrator, High Contrast, text scaling and TalkBack results come from
  that check and are not claimed by the automated steps.

## Dependencies and coordination

- RF-13 and RF-14 are complete; their decomposed editors are the surfaces in scope.
- Open PRs touch the same views: #293 (RF-24, journey and city pages) and #354 (RF-31, signal-box properties and help
  page). Steps 2-4 start on those views only after the PRs are integrated, to keep one writer per file.

## Risks

- **Tooltip text quality:** tooltips written for sighted users may be too short as names; each name is reviewed, not
  copied blindly.
- **Track-plan canvas (Win2D):** custom-drawn content cannot carry automation peers as easily as the signal box; if it
  cannot expose an element reliably, the side panels' existing controls become the documented accessible path.
