# MAUI ViewModel Adapter Guidelines

This file documents the required behavior for MAUI-specific ViewModel adapters inside `SharedUI`.

Rules:

- MAUI adapters MUST only perform UI thread dispatching and minimal wiring.
- Use `IUiDispatcher.InvokeOnUi(Action)` which uses `MainThread.BeginInvokeOnMainThread` on mobile platforms.
- Keep constructors thin and test-friendly (provide a fallback `ImmediateDispatcher` for tests).

Example:

```csharp
// In SharedUI/ViewModel/MAUI/JourneyViewModel.cs
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
