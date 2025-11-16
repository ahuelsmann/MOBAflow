# UI Guidelines (WinUI / MAUI / Blazor)

## Principles
- Keep Views minimal; place logic in ViewModels.
- Prefer data templates and x:Bind (WinUI) or bindable properties (MAUI).
- Reuse SharedUI ViewModels; create platform-specific ViewModels only for threading or platform APIs.

## WinUI
- Use `x:Bind` with `Mode=OneWay` where possible for performance.
- Avoid heavy work in code-behind; use commands for actions.
- Use `DispatcherQueue` in platform-specific ViewModels when handling background events.

## MAUI
- Use `CollectionView` instead of `ListView`/`TableView`.
- Respect Android Material theme constraints (see project docs).
- Dispatch UI updates via `MainThread.BeginInvokeOnMainThread()`.

## Blazor
- Prefer components with `@code` backed by injected services.
- Use `InvokeAsync(StateHasChanged)` when updating UI from non-UI threads.

## Styling & Themes
- Keep colors in shared resource dictionaries where possible.
- Provide high-contrast theme variants.

## Performance
- Virtualize large lists.
- Avoid blocking the UI thread.

---

Keep UI code accessible, testable and platform-agnostic where possible.