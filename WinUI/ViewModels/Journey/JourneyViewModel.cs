namespace Moba.WinUI.ViewModels.Journey;

using Backend.Model;
using Microsoft.UI.Dispatching;

/// <summary>
/// WinUI-specific JourneyViewModel that dispatches PropertyChanged events to the UI thread.
/// This prevents COMException when updating UI controls from background threads (Z21 UDP callbacks).
/// </summary>
public class JourneyViewModel : SharedUI.ViewModel.JourneyViewModel
{
    private readonly DispatcherQueue? _dispatcherQueue;

    public JourneyViewModel(Journey model) : base(model)
    {
        // Get the DispatcherQueue for the current thread (UI thread)
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        // Unsubscribe from base event and re-subscribe with UI thread dispatching
        Model.StateChanged -= OnModelStateChanged;
        Model.StateChanged += OnModelStateChangedWithDispatch;
    }

    /// <summary>
    /// Handles model state changes and dispatches PropertyChanged to UI thread.
    /// </summary>
    private void OnModelStateChangedWithDispatch(object? sender, EventArgs e)
    {
        if (_dispatcherQueue == null)
        {
            // No dispatcher available (e.g., unit tests) → call directly
            OnModelStateChanged(sender, e);
            return;
        }

        // Dispatch to UI thread
        _dispatcherQueue.TryEnqueue(() =>
        {
            OnModelStateChanged(sender, e);
        });
    }

    /// <summary>
    /// Original handler from base class (now called on UI thread).
    /// </summary>
    private void OnModelStateChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(CurrentCounter));
        OnPropertyChanged(nameof(CurrentPos));
    }
}
