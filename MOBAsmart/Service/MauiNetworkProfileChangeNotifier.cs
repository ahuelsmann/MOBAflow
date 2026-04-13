// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Service;

using Microsoft.Maui.Networking;

using SharedUI.Interface;

/// <summary>
/// Forwards .NET MAUI <see cref="Connectivity"/> changes so the app can re-resolve the REST API after Wi‑Fi / route changes.
/// </summary>
public sealed class MauiNetworkProfileChangeNotifier : INetworkProfileChangeNotifier
{
    private bool _started;

    /// <inheritdoc />
    public event EventHandler? NetworkProfilePossiblyChanged;

    /// <inheritdoc />
    public void StartListening()
    {
        if (_started)
        {
            return;
        }

        _started = true;
        Connectivity.ConnectivityChanged += OnConnectivityChanged;
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        _ = sender;
        _ = e;
        NetworkProfilePossiblyChanged?.Invoke(this, EventArgs.Empty);
    }
}
