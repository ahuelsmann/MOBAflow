// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;

/// <summary>
/// Service zur Überwachung des Window-Input-Activation-Status.
/// Ermöglicht Z21-Refresh bei Fokus und SignalR-Pausierung bei Inaktivität.
/// </summary>
public sealed class WindowActivationService : IDisposable
{
    private readonly InputActivationListener _activationListener;
    private readonly ILogger<WindowActivationService> _logger;
    private InputActivationState _previousState;

    public event EventHandler<InputActivationStateChangedEventArgs>? ActivationStateChanged;

    public WindowActivationService(AppWindow appWindow, ILogger<WindowActivationService> logger)
    {
        _logger = logger;
        _activationListener = InputActivationListener.GetForWindowId(appWindow.Id);
        _previousState = _activationListener.State;
        _activationListener.InputActivationChanged += OnInputActivationChanged;
    }

    private void OnInputActivationChanged(InputActivationListener sender, InputActivationListenerActivationChangedEventArgs args)
    {
        var oldState = _previousState;
        var newState = sender.State;
        _previousState = newState;

        _logger.LogDebug("Window activation state changed from {OldState} to {NewState}",
            oldState, newState);

        ActivationStateChanged?.Invoke(this, new InputActivationStateChangedEventArgs(
            oldState, newState));
    }

    public InputActivationState CurrentState => _activationListener.State;

    public void Dispose()
    {
        _activationListener.InputActivationChanged -= OnInputActivationChanged;
    }
}

public sealed class InputActivationStateChangedEventArgs : EventArgs
{
    public InputActivationState OldState { get; }
    public InputActivationState NewState { get; }

    public InputActivationStateChangedEventArgs(InputActivationState oldState, InputActivationState newState)
    {
        OldState = oldState;
        NewState = newState;
    }
}