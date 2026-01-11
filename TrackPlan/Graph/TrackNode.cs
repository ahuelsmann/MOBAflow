namespace Moba.TrackPlan.Graph;

public sealed class TrackNode
{
    public required Guid Id { get; init; }
    public List<TrackPort> Ports { get; init; } = [];
}