---
description: 'Composition roots, service lifetimes and transient-view subscriptions.'
applyTo: 'Backend/Extensions/**/*.cs,MOBAflow/**/*.cs,MOBAsmart/**/*.cs,SharedUI/**/*.cs'
---

# Dependency injection and lifetimes

Use constructor injection and the runtime boundaries in [AGENTS.md](../../AGENTS.md).
Service-provider lookups belong in composition roots/factories, not feature behavior.

## Registration entry points

- [Backend](../../Backend/Extensions/MobaBackendServiceCollectionExtensions.cs): `AddMobaBackendServices`
  supplies common runtime services. Preserve its `TryAdd` behavior and workflow-handler collection registrations.
- [WinUI](../../MOBAflow/Extensions/MobaWinUiServiceCollectionExtensions.cs) and
  [MAUI](../../MOBAsmart/Extensions/MobaMauiServiceCollectionExtensions.cs) compose their platform-specific services.
  Register shared backend services through the common extension rather than maintaining duplicate lists.
- Preserve [EventBus UI registration](../../SharedUI/Extensions/EventBusUiExtensions.cs) and its dispatcher dependency.
  Consumers of `IEventBus` in UI hosts must retain the decorated instance.
- Prefer direct type registrations when constructors can be resolved. Use factory registrations where runtime
  arguments, platform-specific construction or interface aliases require them; explain non-obvious choices.
  Register interface aliases against the same singleton instance when both access paths share state.

## Choosing a lifetime

| Concern | Approach |
| --- | --- |
| Runtime/connection and shared application state | Preserve the existing singleton |
| Navigable page | Transient, as in the current platform registration |
| Per-page ephemeral state | Transient or owned by that page |
| Domain model wrapper | Existing factory or explicit construction with the model; do not register each model instance |
| Simple view of existing shared state | Reuse the appropriate ViewModel |
| Distinct editor/feature state | Use a dedicated ViewModel when the behavior warrants it |

Do not default every ViewModel to singleton or add dependencies to MainWindowViewModel merely to avoid a new type.
Required dependencies must resolve; optional dependencies may use the established null-object/fallback pattern.
Do not mark a missing required dependency optional to hide a registration error.

## Page lifecycle

A transient page can outlive navigation if a singleton ViewModel still references its handlers. Subscribe to
singleton events on `Loaded` and unsubscribe on `Unloaded`, using matching handlers and avoiding duplicate
subscriptions on repeated loads. A constructor-only subscription can leak the page; a constructor subscription
paired only with unload also fails when the same page is reloaded.

Keep `InitializeComponent`, `ViewModel`/`DataContext` binding and visual coordination in the page.
Feature commands remain in the ViewModel. Register new navigation destinations in the existing platform registry
or Shell routes. Check XAML inclusion when adding/renaming pages.

## Validation

Use the real registration extensions in affected DI tests:

- `Test/Backend/MobaBackendServiceCollectionExtensionsTests.cs`
- `Test/MOBAflow/MobaWinUiServiceCollectionExtensionsTests.cs` (Windows target)
- `Test/MOBAsmart/MobaMauiServiceCollectionExtensionsTests.cs` (opt-in mobile coverage)

Cover the changed registration, shared-instance identity and lifetime behavior as relevant. Keep platform
services/test doubles appropriate to the test host. Do not add eager resolution of every page at startup just
for validation; use tests and the existing startup validation path.