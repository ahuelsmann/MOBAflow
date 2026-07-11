// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

/// <summary>
/// One expected track-feedback occurrence within a journey.
/// The order in <see cref="Journey.FeedbackSequence"/> is the journey's route logic.
/// </summary>
public sealed class JourneyFeedbackStep
{
    /// <summary>Creates a feedback step with a stable identifier.</summary>
    public JourneyFeedbackStep()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>Gets or sets the unique identifier of this sequence step.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the expected Z21 feedback input port.</summary>
    public uint InPort { get; set; }

    /// <summary>Gets or sets the optional workflow to execute when this step is reached.</summary>
    public Guid? WorkflowId { get; set; }
}
