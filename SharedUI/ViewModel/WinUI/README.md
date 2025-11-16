# WinUI ViewModel Adapter Guidelines

This file documents the required behavior for WinUI-specific ViewModel adapters inside `SharedUI`.

Rules:

- WinUI adapters MUST only perform UI thread dispatching and minimal wiring.
- Do NOT include business logic or platform-specific services in these adapters.
- Use `IUiDispatcher.InvokeOnUi(Action)` for all UI updates.
- Keep constructors thin and test-friendly (provide a fallback `ImmediateDispatcher` for tests).

Example:

```csharp
// In SharedUI/ViewModel/WinUI/JourneyViewModel.cs
public class JourneyViewModel : ViewModel.JourneyViewModel
{
    private readonly IUiDispatcher _dispatcher;
    public JourneyViewModel(Journey model, IUiDispatcher dispatcher) : base(model)
    {
        _dispatcher = dispatcher;
        Model.StateChanged += (_, _) => _dispatcher.InvokeOnUi(() => OnPropertyChanged(...));
    }
}
```
