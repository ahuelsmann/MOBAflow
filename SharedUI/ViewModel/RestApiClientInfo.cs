// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

/// <summary>
/// Display info for a client connected to the REST API (e.g. MAUI app).
/// </summary>
public sealed class RestApiClientInfo
{
    /// <summary>Client identifier.</summary>
    public string ClientId { get; init; } = "";

    /// <summary>Display name (e.g. "MOBAsmart").</summary>
    public string DeviceName { get; init; } = "";

    /// <summary>When the client registered (UTC).</summary>
    public DateTime ConnectedAt { get; init; }
}
