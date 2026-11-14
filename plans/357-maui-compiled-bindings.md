# RF-08: Compile MAUI XAML bindings

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/357
**Spec Kit**: Not applicable - build-quality change without behavior change
**Status**: Complete with slice 3. Slice 1 (#358) shipped this plan, compilation with `Source`, the three self-bound
controls and the typed `ControlPage` command binding that MAUI 10.0.120 (#381) turned into an Android Release build
error. Slice 2 (#423) declares `x:DataType` on `FunctionToggleView`; project-owned XAML builds with 0 `XC0022`,
`XC0025` and `XC0045`. Slice 3 makes these three codes build errors in `MOBAsmart.csproj` and closes #357.

## Purpose and scope

[Issue #357](https://github.com/ahuelsmann/MOBAflow/issues/357) owns acceptance and live status.
The [umbrella plan](QUALITY-AND-REFACTORING-PLAN.md) owns programme dependencies. This is the single RF-08 child plan.

Target: project-owned MAUI XAML compiles its bindings, so binding path errors fail the build instead of showing up
on the device, and CI keeps that state. No UI, behavior or setting changes.

## Inventory

Measured with `dotnet build MOBAsmart/MOBAsmart.csproj -f net10.0-android -c Release -t:Rebuild` on main `1e88cafa`:
90 `XC0025` warnings ("Binding was not compiled because it has an explicitly set Source property and compilation of
bindings with Source is not enabled"). Each warning appears twice in the build log.

| File | Bindings | Pattern | Slice |
| --- | --- | --- | --- |
| `Controls/SignalAspectPicker.xaml` | 27 | all `Source={x:Reference Root}` (the control itself) | 1 |
| `Controls/LocomotivePickerItemView.xaml` | 13 | all `Source={x:Reference Root}` | 1 |
| `Controls/CounterStepperButton.xaml` | 2 | all `Source={x:Reference Root}` | 1 |
| `Controls/FunctionToggleView.xaml` | 2 | all `Source={x:Reference Root}` | 2 (after #308) |
| `View/ControlPage.xaml` | 1 | `Source={x:Reference ControlRoot}, Path=BindingContext.ToggleFunctionCommand` inside a `FunctionButtonViewModel` template | 1 (typed source; see design decisions) |

Six files already declare `x:DataType`; `XC0103` comes from CommunityToolkit.Maui and stays suppressed.

## Design decisions

- **Compile bindings with `Source`.** `MauiEnableXamlCBindingWithSourceCompilation` makes XamlC compile them against
  the `x:DataType` in scope. A self-bound control therefore declares its own type as `x:DataType` on the root
  element; the binding semantics are unchanged, and the compiler now validates each path.
- **Ancestor bindings in templates (slice 2).** The `ControlPage` binding reaches the page's ViewModel through
  `BindingContext.…`; with compilation it is resolved against the template's `FunctionButtonViewModel` and reports
  `XC0045`. XamlC honours `TreatWarningsAsErrors`, which `Directory.Build.props` sets for Release, and exempts only
  `XC0022`, `XC0023` and `XC0025` (`Microsoft.Maui.Controls.targets`, MAUI 10.0.120). The `XC0045` is therefore a
  warning in FastDebug but an **error** in Android Release. Slice 1 therefore gives that binding the page
  ViewModel as its typed source: `Source={RelativeSource AncestorType={x:Type tc:TrainControlViewModel}}` with a
  binding-level `x:DataType=tc:TrainControlViewModel` and `Path=ToggleFunctionCommand`. The command instance is the
  same object that `ControlRoot.BindingContext` held, so behavior is unchanged and the path is compiled and checked.
  The owner of #308 carries this binding over, since that PR also edits `ControlPage.xaml`.
- **Gate.** After slice 2, slice 3 treats `XC0022`, `XC0025` and `XC0045` as errors in `MOBAsmart.csproj`, so CI
  rejects new uncompiled or wrong bindings. The project lists the three codes in `WarningsAsErrors`. XamlC
  (`LoggingHelperExtensions.LogWarningOrError` in `Microsoft.Maui.Controls.Build.Tasks` 10.0.120) reports a code
  from that list as an error even when it is in `WarningsNotAsErrors`, so MAUI's default exemption of `XC0022` and
  `XC0025` no longer applies to these codes. The list has no configuration condition: FastDebug, Debug and Release
  all fail on such a binding. `MauiStrictXamlCompilation` was not used, because it only lifts the exemption under
  `TreatWarningsAsErrors` (Release) and would also change `XC0023`, which is outside this package. No `NoWarn`
  entry and no analyzer baseline change is involved; `XC0103` stays suppressed as before.

## Independently reviewable delivery slices

1. **Enable compilation, the three self-bound controls and the typed `ControlPage` command binding** (with this
   plan): 90 `XC0025` → 2 `XC0022` warnings in `FunctionToggleView` (each printed twice in the log), 0 errors.
   Measured on main `cbe08089` with MAUI 10.0.120 in FastDebug and Release.
2. **`FunctionToggleView`** after #308: the control declares `x:DataType="controls:FunctionToggleView"`, the same
   self-binding pattern as `CounterStepperButton`. Its two `Source={x:Reference Root}` bindings (`ToggleCommand`,
   `FunctionIndex`) are now compiled; the runtime `BindingContext` (a `FunctionButtonViewModel` from the
   `ControlPage` template) is untouched because the control reads it in code-behind, not through bindings.
   `ControlPage` needed no further change; its typed `ToggleFunctionCommand` binding from slice 1 and #308 stays.
   Result: 2 `XC0022` → 0 XamlC warnings, 0 errors, in FastDebug and Android Release.
3. **Warnings as errors, documentation and completion.** `MOBAsmart.csproj` adds `XC0022`, `XC0025` and `XC0045` to
   `WarningsAsErrors`. No XAML changed: main already built without these warnings after slice 2. The Android
   Release lane of the slice 3 pull request is the evidence that the project still builds with the gate; that
   lane runs on every pull request that changes `MOBAsmart/**`, which is where a new uncompiled binding would be
   introduced. A binding in a referenced project cannot trigger these codes, because only MOBAsmart contains MAUI
   XAML.

## Validation strategy

Every slice: Android Release rebuild with the warning count, Android analyzer gate, portable and Windows tests
(SharedUI is unchanged). Slice 3 was written in a cloud session without the Android workload, so its build evidence
is the pull request's Android Release lane, not a local rebuild. No device deployment by the agent. Personal acceptance was abolished on 2026-11-06; the
rendered function toggles are an uninspected visual state until the user reports from the app, not a passed check.

## Risks

- **Runtime differences:** compiled bindings fail at build time where reflection bindings silently failed; a
  compiled binding that resolves differently would be caught by the compiler (`XC0045`) rather than at runtime.
- **Open PRs:** #308 (merged) touched `FunctionToggleView` and `ControlPage` before slice 2; #294 (localization)
  touches other MAUI files only.
