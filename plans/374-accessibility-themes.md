# RF-16: Accessibility and themes

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/374
**Spec Kit**: Not applicable - accessibility semantics, theme resources and keyboard access to existing commands without new product behavior; a step that would introduce a new interaction model gets its own Spec Kit feature first
**Status**: Active; step 1 (inventory) ships with this plan

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
| Operate the signal box (signals, switches, routes) | `SignalBoxPage`, `SignalBoxCanvasControl`, `SignalBoxPropertiesControl` | Custom-drawn canvas without automation peers |
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
| Custom-drawn surfaces (Win2D) without automation peers | 3 | `SignalBoxCanvasControl`, `SignalBoxPage`, `TrackPlanPage` |
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
- **Custom-drawn canvases:** Narrator access to the signal box comes from automation peers that expose the existing
  elements and their commands. A new keyboard editing model for the track-plan editor would be new product behavior; it
  needs a Spec Kit feature and a user decision before implementation.
- **Emergency stop by keyboard:** adding a shortcut to the existing stop and track-power commands is in scope, but the key
  choice is a user decision (material UX), recorded on #374 before step 3.

## Delivery steps

1. **Inventory** (with this plan).
2. **Accessible names and guard:** the 43 buttons, plus a test that fails on a new unnamed icon-only button.
3. **Keyboard and Narrator for the critical workflows:** focus order, visible focus, the emergency-stop and track-power
   shortcuts after the user's key decision, and automation peers for the signal-box canvas.
4. **Theme resources and High Contrast:** the 12 non-dictionary XAML colors and the general-purpose C# colors;
   `HighContrast` theme dictionaries; a non-color cue for signal aspects.
5. **MOBAsmart:** TalkBack descriptions for the remaining views and font scaling.
6. **Acceptance and completion:** Accessibility Insights FastPass on every critical surface; Narrator, keyboard only and
   200% text size; Light, Dark and High Contrast. Evidence is recorded on #374; then documentation and plan removal.

## Validation strategy

- Steps 2-5: compile MOBAflow (and MOBAsmart for step 5), run the Windows suite and the analyzer gates; the step 2 guard
  must fail on a deliberately unnamed button in its own self-test.
- Step 6 is manual and needs the maintainer: the app is launched only with explicit approval, so Narrator, High Contrast
  and Accessibility Insights checks are the user's acceptance, not an automated claim.

## Dependencies and coordination

- RF-13 and RF-14 are complete; their decomposed editors are the surfaces in scope.
- Open PRs touch the same views: #293 (RF-24, journey and city pages) and #354 (RF-31, signal-box properties and help
  page). Steps 2-4 start on those views only after the PRs are integrated, to keep one writer per file.

## Risks

- **Tooltip text quality:** tooltips written for sighted users may be too short as names; each name is reviewed, not
  copied blindly.
- **Win2D canvases:** automation peers for custom-drawn content are the largest technical risk; if a peer cannot expose
  an element reliably, the side panels' existing controls become the documented accessible path.
