# RF-14 Signal-Box Property Editing Refactor

## Issue

- GitHub issue: #92
- Programme: #47
- Consumer: #34 Slice 7
- Dependency: RF-06/#90 (complete)

## Outcome

Move signal-box property state, validation, and domain mutation out of
`SignalBoxPropertiesControl` into a platform-neutral ViewModel. Keep the WinUI
control responsible only for translating input and rendering the projected
state.

## Scope

1. Add a testable signal-box properties ViewModel in `SharedUI`.
2. Isolate multiplexer, signal-article, supported-aspect, address, and
   speed-indicator state behind platform-neutral interfaces.
3. Route name, rotation, address, switch-position, signal-article,
   multiplexer, speed-indicator, and signal-aspect changes through the
   ViewModel.
4. Project explicit change notifications for persistence, visual refresh,
   deletion, and signal-aspect hardware dispatch.
5. Reduce `SignalBoxPropertiesControl` to WinUI input and presentation
   adapters.
6. Add focused ViewModel tests and a source-boundary architecture test that
   rejects direct signal-box model mutation in the control.
7. Remove superseded control-owned mutation and validation paths.

## Out of scope

- Interlocking product behavior owned by #34.
- Track-plan editor behavior owned by RF-13.
- Visual redesign, keyboard completion, or accessibility work owned by RF-16.
- New signal articles, multiplexer mappings, or persistence schema changes.
- Starting MOBAflow or MOBAsmart during validation.

## Design

- `ISignalArticleCatalog` exposes selectable main and distant signal articles
  without introducing a WinUI dependency into `SharedUI`.
- `SignalBoxPropertiesViewModel` owns the selected element, projected editor
  state, validation, and mutation commands.
- ViewModel events describe required side effects:
  visual refresh, solution persistence, deletion, and signal-aspect dispatch.
- `SignalBoxPage` translates those events to the existing plan ViewModel and
  `MainWindowViewModel` runtime APIs.
- `SignalBoxPropertiesControl` only reads projected state and forwards WinUI
  events to ViewModel commands or setters.

## Validation

- Focused NUnit tests for every property-editing responsibility and negative
  validation path.
- Architecture test proving `SignalBoxPropertiesControl.xaml.cs` no longer
  mutates `SbElement`, `SbSignal`, `SbSwitch`, or `SbDetector` directly.
- Cross-platform Release build and analyzer ratchet.
- Windows Release build, complete desktop tests, and coverage ratchet.
- Android Release publish, analyzer ratchet, and AAB validation because
  `SharedUI` changes.
- Secret scan for every changed file.
- SonarCloud green with zero `OPEN` or `CONFIRMED` PR findings.

## Completion

Delete this plan after implementation and validation. Preserve this committed
plan through its immutable commit link in Issue #92 and the pull request.
