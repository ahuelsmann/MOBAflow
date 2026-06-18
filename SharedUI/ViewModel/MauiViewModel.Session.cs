// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;

using Interface;

/// <summary>
/// MOBAsmart session availability for SignalBox and Control tabs.
/// </summary>
public sealed partial class MauiViewModel
{
    private bool _isRuntimeHubConnected;
    private bool _remoteZ21Connected;

    /// <summary>
    /// Gets whether the MOBApi runtime hub is connected.
    /// </summary>
    public bool IsRuntimeHubConnected => _isRuntimeHubConnected;

    /// <summary>
    /// Gets whether MOBAflow reports an active Z21 connection in the remote snapshot.
    /// </summary>
    public bool IsRemoteZ21Connected => _remoteZ21Connected;

    /// <summary>
    /// Gets whether SignalBox and Control tabs may be used.
    /// </summary>
    public bool IsSessionOperational => IsRestApiReachable && IsRuntimeHubConnected && IsRemoteZ21Connected;

    /// <summary>
    /// Gets whether the Counter tab can use local Z21 feedback.
    /// </summary>
    public bool IsCounterAvailable => IsConnected;

    /// <summary>
    /// Gets whether SignalBox tab interaction is allowed.
    /// </summary>
    public bool IsSignalBoxAvailable => IsSessionOperational;

    /// <summary>
    /// Gets whether Control tab interaction is allowed.
    /// </summary>
    public bool IsControlAvailable => IsSessionOperational;

    public string SessionStatusText => IsSessionOperational
        ? "MOBAflow session active"
        : "MOBAflow session unavailable";

    public string SessionLockedHint =>
        "Signal box and control require an active connection to MOBAflow and the Z21 on the PC.";

    internal void SetRuntimeHubConnected(bool isConnected)
    {
        if (_isRuntimeHubConnected == isConnected)
        {
            return;
        }

        _isRuntimeHubConnected = isConnected;
        OnPropertyChanged(nameof(IsRuntimeHubConnected));
        NotifySessionAvailabilityChanged();
    }

    internal void SetRemoteZ21Connected(bool isConnected)
    {
        if (_remoteZ21Connected == isConnected)
        {
            return;
        }

        _remoteZ21Connected = isConnected;
        OnPropertyChanged(nameof(IsRemoteZ21Connected));
        NotifySessionAvailabilityChanged();
    }

    private void NotifySessionAvailabilityChanged()
    {
        OnPropertyChanged(nameof(IsSessionOperational));
        OnPropertyChanged(nameof(IsSignalBoxAvailable));
        OnPropertyChanged(nameof(IsControlAvailable));
        OnPropertyChanged(nameof(SessionStatusText));
        OnPropertyChanged(nameof(SessionLockedHint));
    }
}
