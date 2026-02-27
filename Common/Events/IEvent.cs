// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Events;

/// <summary>
/// Base interface for all events in the MOBAflow event bus system.
/// Events represent state changes in the backend (Z21, Feedback, etc.) that should be communicated to ViewModels.
/// </summary>
public interface IEvent
{
    /// <summary>
    /// Timestamp when the event was created (UTC).
    /// </summary>
    DateTime CreatedUtc { get; }
}

/// <summary>
/// Base record for events - provides common timestamp.
/// </summary>
public abstract record EventBase : IEvent
{
    /// <summary>
    /// Gets the UTC timestamp when the event instance was created.
    /// </summary>
    public DateTime CreatedUtc { get; } = DateTime.UtcNow;
}
