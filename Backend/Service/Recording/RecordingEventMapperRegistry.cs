// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service.Recording;

using Common.Events;
using Moba.Common.Recording;

/// <summary>
/// Maps only explicitly declared event types to sanitized recording projections.
/// </summary>
public interface IRecordingEventMapper
{
    /// <summary>Gets the exact event types supported by this mapper.</summary>
    IReadOnlyCollection<Type> EventTypes { get; }

    /// <summary>Maps one supported event without retaining the source object.</summary>
    RecordingEntryProjection Map(IEvent sourceEvent);
}

/// <summary>
/// Resolves exact event types through an immutable allow-list.
/// </summary>
public sealed class RecordingEventMapperRegistry
{
    private readonly IReadOnlyDictionary<Type, IRecordingEventMapper> _mappers;

    /// <summary>Initializes the registry and rejects ambiguous duplicate registrations.</summary>
    public RecordingEventMapperRegistry(IEnumerable<IRecordingEventMapper> mappers)
    {
        ArgumentNullException.ThrowIfNull(mappers);

        var registrations = new Dictionary<Type, IRecordingEventMapper>();
        foreach (var mapper in mappers)
        {
            ArgumentNullException.ThrowIfNull(mapper);
            foreach (var eventType in mapper.EventTypes)
            {
                if (!typeof(IEvent).IsAssignableFrom(eventType))
                {
                    throw new ArgumentException($"Type '{eventType.FullName}' is not an event.", nameof(mappers));
                }

                if (!registrations.TryAdd(eventType, mapper))
                {
                    throw new ArgumentException($"Event type '{eventType.FullName}' has more than one recording mapper.", nameof(mappers));
                }
            }
        }

        _mappers = registrations;
    }

    /// <summary>Maps an event when its exact runtime type is allow-listed.</summary>
    public bool TryMap(IEvent sourceEvent, out RecordingEntryProjection? projection)
    {
        ArgumentNullException.ThrowIfNull(sourceEvent);

        if (_mappers.TryGetValue(sourceEvent.GetType(), out var mapper))
        {
            projection = mapper.Map(sourceEvent);
            return true;
        }

        projection = null;
        return false;
    }
}