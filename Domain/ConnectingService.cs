// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

/// <summary>
/// Represents a connecting service (e.g., transfer connection) for a station.
/// Pure data object without behavior.
/// </summary>
public class ConnectingService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectingService"/> class with a default name.
    /// </summary>
    public ConnectingService()
    {
        Name = "New Connecting Service";
    }

    /// <summary>
    /// Gets or sets the display name of the connecting service.
    /// </summary>
    public string Name { get; set; }
}