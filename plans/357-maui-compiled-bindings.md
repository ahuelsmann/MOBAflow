# RF-08: Compile MAUI XAML bindings

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/357
**Spec Kit**: Not applicable - build-quality change without behavior change
**Status**: Active; slice 1 ships with this plan, including the typed `ControlPage` command binding that MAUI
10.0.120 (#381) turned into an Android Release build error.

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
| `Controls/FunctionToggleView.xaml` | 2 | all `Source={x:Reference Root}` | 2 (touched by #308) |
| `View/ControlPage.xaml` | 1 | `Source={x:Reference ControlRoot}, Path=BindingContext.ToggleFunctionCommand` inside a `FunctionButtonViewModel` template | 2 (touched by #308) |

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
  rejects new uncompiled or wrong bindings.

## Independently reviewable delivery slices

1. **Enable compilation, the three self-bound controls and the typed `ControlPage` command binding** (with this
   plan): 90 `XC0025` → 2 `XC0022` warnings in `FunctionToggleView` (each printed twice in the log), 0 errors.
   Measured on main `cbe08089` with MAUI 10.0.120 in FastDebug and Release.
2. **`FunctionToggleView`** after #308.
3. **Warnings as errors, documentation and completion.**

## Validation strategy

Every slice: Android Release rebuild with the warning count, Android analyzer gate, portable and Windows tests
(SharedUI is unchanged). Maintainer device check of locomotive selection, signal aspects, function toggles and the
control page, including startup and interaction responsiveness, recorded on #357. No device deployment by the agent.

## Risks

- **Runtime differences:** compiled bindings fail at build time where reflection bindings silently failed; a
  compiled binding that resolves differently would be caught by the compiler (`XC0045`) rather than at runtime.
- **Open PRs:** #308 touches `FunctionToggleView` and `ControlPage`; #294 (localization) touches other MAUI files
  only.
