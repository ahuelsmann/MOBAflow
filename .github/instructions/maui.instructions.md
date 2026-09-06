---
description: 'MOBAsmart Android UI, navigation, resources and platform adapters.'
applyTo: 'MOBAsmart/**/*.cs,MOBAsmart/**/*.xaml'
---

# MOBAsmart MAUI

The Android app is `MOBAsmart/`; use [AGENTS.md](../../AGENTS.md) for build and validation commands.
Preserve the existing shared runtime and mobile local/remote coordination.

## UI and resources

- Keep UI strings English; announcement content/voice remain user-configurable.
- Reuse resources in `MOBAsmart/Resources/` and styles from surrounding views. Use semantic theme resources and
  `AppThemeBinding` where appropriate; a fixed White/Gray pair is not sufficient Light/Dark support.
- Prefer `Border`, `CollectionView`, `Grid`, `VerticalStackLayout` and `HorizontalStackLayout` in new UI.
  Avoid introducing legacy renderers or deprecated controls; use MAUI handlers for platform customization.
- Put scrolling content in a bounded Grid row. Avoid nesting ScrollView/CollectionView in an unconstrained stack.
  Use `BindableLayout` only for small lists that do not need virtualization.
- Use the existing resource pipeline: SVG source assets are referenced by their generated image resource names
  at runtime. Verify the `MauiImage` items instead of assuming file paths.
- Use `Background` for brush/gradient values. `BackgroundColor` remains appropriate where the existing control
  expects a color; do not mechanically replace it in unrelated views.

## Threading and platform services

- EventBus handlers registered through `AddEventBusWithUiDispatch()` already run on the UI thread.
  Do not dispatch them again. For callbacks outside that bus, inspect the thread context and use the platform
  dispatcher adapter when UI access requires it. Keep MAUI `MainThread` APIs out of shared/backend types.
- Keep feature commands in ViewModels and service interfaces; code-behind handles platform view coordination.
- Follow the existing Shell routes/navigation service. Do not mix in a second navigation root such as
  NavigationPage, TabbedPage or FlyoutPage. Resolve route names from current registration.
- Keep Android-specific code in platform implementations/conditional sections. Extend the existing
  `MOBAsmart/Extensions/MobaMauiServiceCollectionExtensions.cs` registration.

## Touch and layout

- Preserve practical touch targets (at least 44 by 44 device-independent units) and accessibility labels.
  A visually compact control still needs an adequate hit area.
- For scaled CheckBox/Switch layouts, verify both visible spacing and hit targets; scaling preserves layout space,
  so a small negative label margin may be appropriate. Do not apply a global scale or margin rule.
- Steppers forward increment/decrement through commands and allow the value field to fit its supported numbers.
  Size compact visuals inside accessible touch areas rather than shrinking the whole interaction target.
- Verify affected states in Light/Dark themes, on narrow screens and with text scaling. Compile checks cannot
  establish touch behavior; report emulator/device checks separately when unavailable.