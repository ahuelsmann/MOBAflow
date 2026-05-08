// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Events;

/// <summary>
/// REST API status changed event - fired when the REST API (MOBApi) connectivity state changes.
/// Published by RestApiStatusService, consumed by MainWindowViewModel.
/// </summary>
public sealed record RestApiStatusChangedEvent : EventBase
{
    /// <summary>
    /// Status text description (e.g., "Running on port 5001" or "Waiting for the REST API to start...").
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the REST API is currently reachable.
    /// </summary>
    public bool IsReachable { get; init; }

    /// <summary>
    /// Connected clients information. Null when API is not reachable.
    /// </summary>
    public List<RestApiClientInfo>? Clients { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RestApiStatusChangedEvent"/> record.
    /// </summary>
    public RestApiStatusChangedEvent(string status, bool isReachable, List<RestApiClientInfo>? clients = null)
    {
        Status = status;
        IsReachable = isReachable;
        Clients = clients;
    }
}

/// <summary>
/// Photo assigned event - fired when a photo is uploaded and should be assigned to the selected entity.
/// Published by RestApiStatusService, consumed by MainWindowViewModel which determines the actual target.
/// </summary>
public sealed record PhotoAssignedEvent : EventBase
{
    /// <summary>
    /// Full path to the uploaded photo file.
    /// </summary>
    public string PhotoPath { get; init; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="PhotoAssignedEvent"/> record.
    /// </summary>
    public PhotoAssignedEvent(string photoPath)
    {
        PhotoPath = photoPath;
    }
}

/// <summary>
/// REST API client information.
/// </summary>
public sealed record RestApiClientInfo
{
    /// <summary>
    /// Client identifier (e.g., connection ID).
    /// </summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>
    /// Device name (e.g., "MOBAsmart").
    /// </summary>
    public string DeviceName { get; init; } = string.Empty;

    /// <summary>
    /// Connection timestamp.
    /// </summary>
    public DateTime ConnectedAt { get; init; }
}

/// <summary>
/// Photo assignment target types.
/// </summary>
public enum PhotoAssignmentTarget
{
    /// <summary>
    /// No entity selected - photo not assigned.
    /// </summary>
    None,

    /// <summary>
    /// Photo assigned to a locomotive.
    /// </summary>
    Locomotive,

    /// <summary>
    /// Photo assigned to a passenger wagon.
    /// </summary>
    PassengerWagon,

    /// <summary>
    /// Photo assigned to a goods wagon.
    /// </summary>
    GoodsWagon
}
