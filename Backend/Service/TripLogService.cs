// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service;

using Interface;
using Common.Events;
using Domain;

using Microsoft.Extensions.Logging;

/// <summary>
/// Trip log service implementation.
/// Records driving and stopping phases in Project.TripLogEntries.
/// </summary>
public sealed class TripLogService : ITripLogService
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<TripLogService>? _logger;

    /// <inheritdoc />
    public event EventHandler? EntryAdded;

    /// <summary>
    /// Initializes a new instance of the <see cref="TripLogService"/> class.
    /// </summary>
    /// <param name="eventBus">Event bus used to publish <see cref="TripLogEntryAddedEvent"/> notifications.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public TripLogService(IEventBus eventBus, ILogger<TripLogService>? logger = null)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _logger = logger;
    }

    /// <inheritdoc />
    public void RecordStateChange(Project? project, int locoAddress, int speed, DateTime timestamp)
    {
        if (project == null || locoAddress < 1) return;

        var entries = project.TripLogEntries;

        lock (entries)
        {
            // Close open entry (if any)
            var openEntry = entries.FirstOrDefault(e => e.EndTime == null);
            if (openEntry != null)
            {
                openEntry.EndTime = timestamp;
                _logger?.LogDebug(
                    "TripLog: geschlossen {IsStop} Adr={Addr} {Start}–{End} Speed={Speed}",
                    openEntry.IsStopSegment, openEntry.LocoAddress, openEntry.StartTime, openEntry.EndTime, openEntry.Speed);
            }

            // Create new entry
            var isStop = speed == 0;
            var entry = new TripLogEntry
            {
                LocoAddress = locoAddress,
                StartTime = timestamp,
                EndTime = null,
                Speed = speed,
                IsStopSegment = isStop
            };
            entries.Add(entry);
            EntryAdded?.Invoke(this, EventArgs.Empty);
            _eventBus.Publish(new TripLogEntryAddedEvent());

            _logger?.LogDebug(
                "TripLog: neu {IsStop} Adr={Addr} Start={Start} Speed={Speed}",
                isStop, locoAddress, timestamp, speed);
        }
    }
}
