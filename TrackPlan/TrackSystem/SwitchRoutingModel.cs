namespace Moba.TrackPlan.TrackSystem;

public sealed class SwitchRoutingModel
{
    public required string InEndId { get; init; }
    public required string StraightEndId { get; init; }
    public required string DivergingEndId { get; init; }

    public string GetActiveOutEnd(bool straight)
        => straight ? StraightEndId : DivergingEndId;
}