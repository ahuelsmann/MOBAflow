// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service.Recording;

using Common.Events;
using Moba.Backend.Interface;
using Moba.Common.Recording;
using System.Text.Json;

/// <summary>
/// Maps the initial allow-list of relevant Z21 state events.
/// </summary>
public sealed class Z21RecordingEventMapper : IRecordingEventMapper
{
    private const string InformationSeverity = "information";
    private static readonly Type[] SupportedEventTypes =
    [
        typeof(Z21ConnectionEstablishedEvent),
        typeof(Z21ConnectionLostEvent),
        typeof(Z21TrackPowerChangedEvent),
        typeof(XBusStatusChangedEvent),
        typeof(SystemStateChangedEvent),
        typeof(FeedbackReceivedEvent),
        typeof(SignalAspectChangedEvent),
        typeof(SwitchPositionChangedEvent)
    ];

    /// <inheritdoc />
    public IReadOnlyCollection<Type> EventTypes => SupportedEventTypes;

    /// <inheritdoc />
    public RecordingEntryProjection Map(IEvent sourceEvent) => sourceEvent switch
    {
        Z21ConnectionEstablishedEvent => Create(
            "z21.connection.established",
            InformationSeverity,
            JsonSerializer.SerializeToElement(new { connected = true }),
            "Z21 connection established"),
        Z21ConnectionLostEvent => Create(
            "z21.connection.lost",
            "warning",
            JsonSerializer.SerializeToElement(new { connected = false }),
            "Z21 connection lost"),
        Z21TrackPowerChangedEvent power => Create(
            "z21.track-power.changed",
            InformationSeverity,
            JsonSerializer.SerializeToElement(new { isOn = power.IsOn }),
            power.IsOn ? "Track power on" : "Track power off"),
        XBusStatusChangedEvent status => Create(
            "z21.xbus-status.changed",
            status.EmergencyStop || status.ShortCircuit ? "error" : InformationSeverity,
            JsonSerializer.SerializeToElement(new
            {
                emergencyStop = status.EmergencyStop,
                trackOff = status.TrackOff,
                shortCircuit = status.ShortCircuit,
                programming = status.Programming
            }),
            BuildXBusDisplayText(status)),
        SystemStateChangedEvent state => Create(
            "z21.system-state.changed",
            InformationSeverity,
            JsonSerializer.SerializeToElement(new
            {
                mainCurrent = state.MainCurrent,
                progCurrent = state.ProgCurrent,
                filteredMainCurrent = state.FilteredMainCurrent,
                temperature = state.Temperature,
                supplyVoltage = state.SupplyVoltage,
                vccVoltage = state.VccVoltage,
                centralState = state.CentralState,
                centralStateEx = state.CentralStateEx
            }),
            "Z21 system state updated"),
        FeedbackReceivedEvent feedback => Create(
            "z21.feedback.activated",
            InformationSeverity,
            JsonSerializer.SerializeToElement(new { inPort = feedback.InPort }),
            $"Feedback input {feedback.InPort} activated"),
        SignalAspectChangedEvent signal => Create(
            "z21.signal-aspect.changed",
            InformationSeverity,
            JsonSerializer.SerializeToElement(new
            {
                signalId = signal.SignalId,
                aspect = signal.Aspect,
                previousAspect = signal.PreviousAspect
            }),
            $"Signal {signal.SignalId} changed to {signal.Aspect}"),
        SwitchPositionChangedEvent @switch => Create(
            "z21.switch-position.changed",
            InformationSeverity,
            JsonSerializer.SerializeToElement(new
            {
                switchId = @switch.SwitchId,
                isLeft = @switch.IsLeft,
                previousPosition = @switch.PreviousPosition
            }),
            $"Switch {@switch.SwitchId} changed position"),
        _ => throw new ArgumentException($"Unsupported Z21 event '{sourceEvent.GetType().Name}'.", nameof(sourceEvent))
    };

    private static RecordingEntryProjection Create(
        string typeKey,
        string severity,
        JsonElement payload,
        string displayText) =>
        new(
            "z21",
            "event-bus",
            typeKey,
            severity,
            null,
            null,
            payload,
            displayText,
            RecordingReplayApplicability.ReplayApplicable);

    private static string BuildXBusDisplayText(XBusStatusChangedEvent status)
    {
        if (status.ShortCircuit) return "Z21 short circuit active";
        if (status.EmergencyStop) return "Z21 emergency stop active";
        if (status.TrackOff) return "Z21 track power off";
        if (status.Programming) return "Z21 programming mode active";
        return "Z21 status normal";
    }
}

/// <summary>
/// Maps only safety-relevant scalar fields from broad runtime snapshots.
/// </summary>
public sealed class RuntimeSnapshotRecordingEventMapper : IRecordingEventMapper
{
    private static readonly Type[] SupportedEventTypes = [typeof(RuntimeSnapshotChangedEvent)];

    /// <inheritdoc />
    public IReadOnlyCollection<Type> EventTypes => SupportedEventTypes;

    /// <inheritdoc />
    public RecordingEntryProjection Map(IEvent sourceEvent)
    {
        if (sourceEvent is not RuntimeSnapshotChangedEvent runtimeEvent)
        {
            throw new ArgumentException($"Unsupported runtime event '{sourceEvent.GetType().Name}'.", nameof(sourceEvent));
        }

        var snapshot = runtimeEvent.Snapshot;
        return new RecordingEntryProjection(
            "runtime",
            "moba-runtime",
            "runtime.state.changed",
            snapshot.IsEmergencyStopActive || snapshot.IsShortCircuitActive ? "error" : "information",
            null,
            null,
            JsonSerializer.SerializeToElement(new
            {
                isConnected = snapshot.IsConnected,
                isTrackPowerOn = snapshot.IsTrackPowerOn,
                isZ21Connecting = snapshot.IsZ21Connecting,
                isManualDisconnectRequested = snapshot.IsManualDisconnectRequested,
                isEmergencyStopActive = snapshot.IsEmergencyStopActive,
                isShortCircuitActive = snapshot.IsShortCircuitActive,
                isProgrammingModeActive = snapshot.IsProgrammingModeActive,
                isOperatorAckRequired = snapshot.IsOperatorAckRequired
            }),
            BuildDisplayText(snapshot.IsConnected, snapshot.IsTrackPowerOn, snapshot.IsOperatorAckRequired),
            RecordingReplayApplicability.ReplayApplicable);
    }

    private static string BuildDisplayText(bool isConnected, bool isTrackPowerOn, bool isOperatorAckRequired)
    {
        if (!isConnected) return "Runtime disconnected";
        if (isOperatorAckRequired) return "Runtime requires operator acknowledgement";
        return isTrackPowerOn ? "Runtime connected with track power on" : "Runtime connected with track power off";
    }
}

/// <summary>
/// Maps immutable journey transition events to replay-safe journal entries.
/// </summary>
public sealed class JourneyRecordingEventMapper : IRecordingEventMapper
{
    private static readonly Type[] SupportedEventTypes = [typeof(JourneyRuntimeTransitionEvent)];

    /// <inheritdoc />
    public IReadOnlyCollection<Type> EventTypes => SupportedEventTypes;

    /// <inheritdoc />
    public RecordingEntryProjection Map(IEvent sourceEvent)
    {
        if (sourceEvent is not JourneyRuntimeTransitionEvent transition)
        {
            throw new ArgumentException($"Unsupported journey event '{sourceEvent.GetType().Name}'.", nameof(sourceEvent));
        }

        var references = new List<RecordingEntityReference>
        {
            new("project", transition.ProjectId),
            new("journey", transition.JourneyId)
        };
        if (transition.StationId is Guid stationId)
        {
            references.Add(new RecordingEntityReference("station", stationId));
        }

        return new RecordingEntryProjection(
            "journey",
            "journey-manager",
            "journey.transition",
            transition.Kind == JourneyRuntimeTransitionKind.Stopped ? "warning" : "information",
            transition.JourneyRunId,
            references,
            JsonSerializer.SerializeToElement(new
            {
                projectId = transition.ProjectId,
                journeyId = transition.JourneyId,
                journeyRunId = transition.JourneyRunId,
                kind = transition.Kind.ToString(),
                feedbackIndex = transition.FeedbackIndex,
                currentOccurrence = transition.CurrentOccurrence,
                requiredOccurrences = transition.RequiredOccurrences,
                inPort = transition.InPort,
                stationId = transition.StationId,
                stationIndex = transition.StationIndex,
                isActive = transition.IsActive
            }),
            $"Journey {transition.JourneyId}: {transition.Kind}",
            RecordingReplayApplicability.ReplayApplicable);
    }
}

/// <summary>
/// Maps correlated Workflow 2.0 lifecycle events without persisting free-form details.
/// </summary>
public sealed class WorkflowLifecycleRecordingEventMapper : IRecordingEventMapper
{
    private static readonly Type[] SupportedEventTypes = [typeof(WorkflowLifecycleEvent)];

    /// <inheritdoc />
    public IReadOnlyCollection<Type> EventTypes => SupportedEventTypes;

    /// <inheritdoc />
    public RecordingEntryProjection Map(IEvent sourceEvent)
    {
        if (sourceEvent is not WorkflowLifecycleEvent lifecycleEvent)
        {
            throw new ArgumentException($"Unsupported workflow event '{sourceEvent.GetType().Name}'.", nameof(sourceEvent));
        }

        var references = new List<RecordingEntityReference>
        {
            new("execution", lifecycleEvent.ExecutionId),
            new("workflow", lifecycleEvent.WorkflowId)
        };
        if (lifecycleEvent.ParentExecutionId is Guid parentExecutionId)
        {
            references.Add(new RecordingEntityReference("parent-execution", parentExecutionId));
        }

        if (lifecycleEvent.StepId is Guid stepId)
        {
            references.Add(new RecordingEntityReference("step", stepId));
        }

        return new RecordingEntryProjection(
            "workflow",
            "workflow-service",
            "workflow.lifecycle",
            GetSeverity(lifecycleEvent.Kind),
            lifecycleEvent.SourceCorrelationId,
            references,
            JsonSerializer.SerializeToElement(new
            {
                kind = lifecycleEvent.Kind.ToString(),
                sourceCorrelationId = lifecycleEvent.SourceCorrelationId,
                executionId = lifecycleEvent.ExecutionId,
                parentExecutionId = lifecycleEvent.ParentExecutionId,
                workflowId = lifecycleEvent.WorkflowId,
                stepId = lifecycleEvent.StepId,
                sourceSequence = lifecycleEvent.Sequence,
                attempt = lifecycleEvent.Attempt,
                mode = lifecycleEvent.Mode.ToString(),
                elapsedTicks = lifecycleEvent.Elapsed?.Ticks,
                result = NormalizeResult(lifecycleEvent.Result)
            }),
            $"Workflow {lifecycleEvent.WorkflowId}: {lifecycleEvent.Kind}",
            RecordingReplayApplicability.ReplayApplicable);
    }

    private static string GetSeverity(WorkflowLifecycleKind kind) => kind switch
    {
        WorkflowLifecycleKind.WorkflowFailed or
        WorkflowLifecycleKind.ValidationFailed or
        WorkflowLifecycleKind.StepFailed => "error",
        WorkflowLifecycleKind.WorkflowCancelled or
        WorkflowLifecycleKind.RetryScheduled => "warning",
        _ => "information"
    };

    private static string? NormalizeResult(string? result)
    {
        if (result is "True" or "False")
        {
            return result;
        }

        return Enum.TryParse<WorkflowExecutionStatus>(result, ignoreCase: false, out var status) &&
               Enum.IsDefined(status)
            ? status.ToString()
            : null;
    }
}
