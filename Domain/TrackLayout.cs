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
/// Extension point implemented by every installed track library.
/// </summary>
public interface ITrackLibrary
{
    string LibraryId { get; }

    string DisplayName { get; }

    IReadOnlyList<TrackDefinition> Definitions { get; }

    bool TryGetDefinition(string templateId, out TrackDefinition definition);
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
/// Runtime-only assignment state. It is intentionally separate from <see cref="TrackPlanDocument"/>
/// so operating feedback cannot mutate a persisted physical design.
/// </summary>
public sealed class RailroadState
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, bool> _occupancyByTrack = [];
    private readonly Dictionary<Guid, DateTimeOffset> _lastFeedbackByTrack = [];
    private readonly Dictionary<int, bool> _switchPositionsByAddress = [];
    private readonly Dictionary<int, string> _signalAspectsByAddress = [];

    public bool IsOccupied(Guid trackId)
    {
        lock (_gate)
            return _occupancyByTrack.TryGetValue(trackId, out var occupied) && occupied;
    }

    public void SetOccupied(Guid trackId, bool occupied)
    {
        lock (_gate)
            _occupancyByTrack[trackId] = occupied;
    }

    public DateTimeOffset? GetLastFeedback(Guid trackId)
    {
        lock (_gate)
            return _lastFeedbackByTrack.TryGetValue(trackId, out var timestamp) ? timestamp : null;
    }

    public void MarkFeedback(Guid trackId, DateTimeOffset timestamp)
    {
        lock (_gate)
        {
            _lastFeedbackByTrack[trackId] = timestamp;
            _occupancyByTrack[trackId] = true;
        }
    }

    /// <summary>Clears occupancy that has not received feedback within the supplied timeout.</summary>
    public void ExpireFeedback(DateTimeOffset now, TimeSpan timeout)
    {
        if (timeout < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout));

        lock (_gate)
        {
            foreach (var trackId in _lastFeedbackByTrack
                         .Where(entry => now - entry.Value >= timeout)
                         .Select(entry => entry.Key)
                         .ToList())
            {
                _occupancyByTrack[trackId] = false;
                _lastFeedbackByTrack.Remove(trackId);
            }
        }
    }

    public void ClearFeedback(Guid trackId)
    {
        lock (_gate)
        {
            _occupancyByTrack[trackId] = false;
            _lastFeedbackByTrack.Remove(trackId);
        }
    }

    public bool? GetSwitchPosition(int address)
    {
        lock (_gate)
            return _switchPositionsByAddress.TryGetValue(address, out var isLeft) ? isLeft : null;
    }

    public void SetSwitchPosition(int address, bool isLeft)
    {
        lock (_gate)
            _switchPositionsByAddress[address] = isLeft;
    }

    public string? GetSignalAspect(int address)
    {
        lock (_gate)
            return _signalAspectsByAddress.TryGetValue(address, out var aspect) ? aspect : null;
    }

    public void SetSignalAspect(int address, string aspect)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aspect);
        lock (_gate)
            _signalAspectsByAddress[address] = aspect;
    }
}
