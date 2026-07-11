// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

/// <summary>
/// Identifies one connector exposed by a track definition.
/// Connector geometry is owned by the providing track library.
/// </summary>
public sealed record ConnectorDefinition(string Id, string Name);

/// <summary>
/// Library-neutral description of a purchasable track template.
/// </summary>
public sealed record TrackDefinition(
    string LibraryId,
    string TemplateId,
    string DisplayName,
    string Category,
    IReadOnlyList<ConnectorDefinition> Connectors);

/// <summary>
/// A placed occurrence of a <see cref="TrackDefinition"/> in a layout.
/// </summary>
public sealed record TrackInstance(
    Guid Id,
    string LibraryId,
    string TemplateId,
    double X,
    double Y,
    double RotationDegrees,
    int? FeedbackInPort = null);

/// <summary>
/// Topological connection between two named instance connectors.
/// </summary>
public sealed record Connection(Guid SourceTrackId, string SourceConnectorId, Guid TargetTrackId, string TargetConnectorId);

/// <summary>
/// Stable, renderer-independent layout aggregate. It deliberately stores topology separately
/// from geometric definitions so the same persisted layout can be rendered by another UI stack.
/// </summary>
public sealed class Layout
{
    private readonly Dictionary<Guid, TrackInstance> _tracks = [];
    private readonly List<Connection> _connections = [];

    public IReadOnlyCollection<TrackInstance> Tracks => _tracks.Values;

    public IReadOnlyList<Connection> Connections => _connections;

    public bool TryGetTrack(Guid id, out TrackInstance track) => _tracks.TryGetValue(id, out track!);

    public void AddTrack(TrackInstance track)
    {
        ArgumentNullException.ThrowIfNull(track);
        if (!_tracks.TryAdd(track.Id, track))
            throw new InvalidOperationException($"Track instance '{track.Id}' already exists.");
    }

    public bool RemoveTrack(Guid id)
    {
        if (!_tracks.Remove(id))
            return false;

        _connections.RemoveAll(connection => connection.SourceTrackId == id || connection.TargetTrackId == id);
        return true;
    }

    public void ReplaceTrack(TrackInstance track)
    {
        ArgumentNullException.ThrowIfNull(track);
        if (!_tracks.ContainsKey(track.Id))
            throw new KeyNotFoundException($"Track instance '{track.Id}' does not exist.");
        _tracks[track.Id] = track;
    }

    public void Connect(Connection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        if (!_tracks.ContainsKey(connection.SourceTrackId) || !_tracks.ContainsKey(connection.TargetTrackId))
            throw new InvalidOperationException("Both track instances must exist before they can be connected.");

        DisconnectConnector(connection.SourceTrackId, connection.SourceConnectorId);
        DisconnectConnector(connection.TargetTrackId, connection.TargetConnectorId);
        _connections.Add(connection);
    }

    public void DisconnectConnector(Guid trackId, string connectorId)
    {
        _connections.RemoveAll(connection =>
            (connection.SourceTrackId == trackId && connection.SourceConnectorId == connectorId)
            || (connection.TargetTrackId == trackId && connection.TargetConnectorId == connectorId));
    }
}

/// <summary>
/// Extension point implemented by every installed track library.
/// </summary>
public interface ITrackLibrary
{
    string LibraryId { get; }

    string DisplayName { get; }

    IReadOnlyList<TrackDefinition> Definitions { get; }

    bool TryGetDefinition(string templateId, out TrackDefinition definition);
}

/// <summary>Resolves installed track libraries by their persisted stable identity.</summary>
public sealed class TrackLibraryRegistry
{
    private readonly Dictionary<string, ITrackLibrary> _libraries;

    public TrackLibraryRegistry(IEnumerable<ITrackLibrary> libraries)
    {
        ArgumentNullException.ThrowIfNull(libraries);
        _libraries = libraries.ToDictionary(library => library.LibraryId, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<ITrackLibrary> Libraries => _libraries.Values;

    public bool TryGetLibrary(string libraryId, out ITrackLibrary library) => _libraries.TryGetValue(libraryId, out library!);

    public TrackDefinition ResolveDefinition(string libraryId, string templateId)
    {
        if (!TryGetLibrary(libraryId, out var library))
            throw new InvalidOperationException($"Track library '{libraryId}' is not installed.");
        if (!library.TryGetDefinition(templateId, out var definition))
            throw new InvalidOperationException($"Track template '{templateId}' is not available in library '{libraryId}'.");
        return definition;
    }
}

/// <summary>
/// Read-only lookup used by runtime integrations to project feedback onto a layout.
/// This prevents Z21-facing code from depending on a concrete editor implementation.
/// </summary>
public interface ITrackFeedbackLookup
{
    IEnumerable<Guid> GetTrackIdsByFeedbackInPort(int inPort);
}

/// <summary>
/// Runtime-only assignment state. It is intentionally separate from <see cref="Layout"/>
/// so operating feedback cannot mutate a persisted physical design.
/// </summary>
public sealed class RailroadState
{
    private readonly Dictionary<Guid, bool> _occupancyByTrack = [];

    private readonly Dictionary<Guid, DateTimeOffset> _lastFeedbackByTrack = [];

    public bool IsOccupied(Guid trackId) => _occupancyByTrack.TryGetValue(trackId, out var occupied) && occupied;

    public void SetOccupied(Guid trackId, bool occupied) => _occupancyByTrack[trackId] = occupied;

    public DateTimeOffset? GetLastFeedback(Guid trackId) => _lastFeedbackByTrack.TryGetValue(trackId, out var timestamp) ? timestamp : null;

    public void MarkFeedback(Guid trackId, DateTimeOffset timestamp)
    {
        _lastFeedbackByTrack[trackId] = timestamp;
        _occupancyByTrack[trackId] = true;
    }
}
