// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

using Enum;

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
        RepeatCount = 1;
        Enabled = true;
        StopTransition = new JourneyStopTransition();
        Conditions = [];
    }

    /// <summary>Gets or sets the unique identifier of this sequence step.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the expected Z21 feedback input port.</summary>
    public uint InPort { get; set; }

    /// <summary>Gets or sets the optional workflow to execute when this step is reached.</summary>
    public Guid? WorkflowId { get; set; }

    /// <summary>Gets or sets how many matching feedback activations complete this step.</summary>
    public uint RepeatCount { get; set; }

    /// <summary>Gets or sets the delay between completing this step and starting its workflow.</summary>
    public int DelayMs { get; set; }

    /// <summary>Gets or sets whether this step participates in journey execution.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets the optional stop transition applied before workflow execution.</summary>
    public JourneyStopTransition StopTransition { get; set; }

    /// <summary>Gets or sets conditions that must be satisfied before this step accepts feedback.</summary>
    public List<JourneyFeedbackCondition> Conditions { get; set; }
}

/// <summary>Describes how completing a feedback step changes the current journey stop.</summary>
public sealed class JourneyStopTransition
{
    public JourneyStopTransitionMode Mode { get; set; }

    public Guid? StationId { get; set; }
}

/// <summary>One optional runtime condition for accepting a journey feedback step.</summary>
public sealed class JourneyFeedbackCondition
{
    public JourneyFeedbackConditionType Type { get; set; }

    public Guid? StationId { get; set; }
}
