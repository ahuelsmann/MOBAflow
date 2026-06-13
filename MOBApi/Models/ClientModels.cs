// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MOBApi.Models;

/// <summary>
/// Request payload for client registration.
/// </summary>
public record RegisterClientRequest(string ClientId, string? DeviceName);

/// <summary>
/// Request payload for client unregistration.
/// </summary>
public record UnregisterClientRequest(string ClientId);

/// <summary>
/// In-memory info for a connected client (MAUI app).
/// </summary>
public class ConnectedClientInfo
{
    public string ClientId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public DateTime ConnectedAt { get; set; }
}
