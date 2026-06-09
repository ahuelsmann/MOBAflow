// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Interface;

/// <summary>
/// Signals that the device's network profile may have changed (e.g. Wi‑Fi network or route).
/// MAUI provides an implementation; other hosts omit registration.
/// </summary>
public interface INetworkProfileChangeNotifier
{
    /// <summary>
    /// Subscribes to platform connectivity change events. Safe to call once at startup.
    /// </summary>
    void StartListening();

    /// <summary>
    /// Unsubscribes from platform connectivity change events. Safe to call more than once.
    /// </summary>
    void StopListening();

    /// <summary>
    /// Raised when connectivity changes in a way that may invalidate cached LAN endpoints.
    /// </summary>
    event EventHandler? NetworkProfilePossiblyChanged;
}