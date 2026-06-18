// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Manager;

public sealed class PlatformSessionState
{
    public Guid PlatformId { get; init; }

    public Guid StationId { get; init; }

    public int Counter { get; set; }

    public bool IsOccupied { get; set; }

    public DateTime? LastFeedbackTime { get; set; }
}