// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Domain;
using Domain.Enum;

using Interface;

/// <summary>
/// Provides pure payload validation and external-effect descriptions for Workflow 2.0 actions.
/// </summary>
public sealed class WorkflowEffectPlanner : IWorkflowEffectPlanner
{
    /// <inheritdoc />
    public WorkflowActionPlan Plan(WorkflowAction action)
    {
        ArgumentNullException.ThrowIfNull(action);

        return action.Type switch
        {
            ActionType.Command => PlanCommand(action),
            ActionType.Audio => PlanAudio(action),
            ActionType.Announcement => PlanAnnouncement(action),
            ActionType.ExecuteScript => PlanScript(action),
            ActionType.SelectSignalAspect => PlanSignal(action),
            ActionType.TrainDestinationDisplay => PlanDisplay(action),
            ActionType.ChangeJourneyStop => PlanJourneyTransition(action),
            _ => Invalid("type", $"Action type '{action.Type}' is not supported.")
        };
    }

    private static WorkflowActionPlan PlanCommand(WorkflowAction action)
    {
        if (action.Command == null)
            return Invalid("command", "Command action requires a command payload.");
        if (!WorkflowActionParameterBinding.TryGetCommandBytes(action, out var bytes))
            return Invalid("command.bytesBase64", "Command action requires non-empty Base64 command bytes.");

        return Valid(
            action.Type,
            WorkflowEffectCategory.CommandStation,
            $"Send {bytes.Length} raw command byte(s) to the command station.",
            "z21:command-station");
    }

    private static WorkflowActionPlan PlanAudio(WorkflowAction action)
    {
        if (action.Audio == null)
            return Invalid("audio", "Audio action requires an audio payload.");
        if (!WorkflowActionParameterBinding.TryGetAudioFilePath(action, out _))
            return Invalid("audio.filePath", "Audio action requires a file path.");

        return Valid(
            action.Type,
            WorkflowEffectCategory.AudioOutput,
            "Play the configured audio file.",
            "audio-output:default");
    }

    private static WorkflowActionPlan PlanAnnouncement(WorkflowAction action)
    {
        if (action.Announcement == null)
            return Invalid("announcement", "Announcement action requires an announcement payload.");

        return Valid(
            action.Type,
            WorkflowEffectCategory.SpeechOutput,
            "Speak the configured or journey-provided announcement.",
            "audio-output:default");
    }

    private static WorkflowActionPlan PlanScript(WorkflowAction action)
    {
        if (action.PowerShell == null)
            return Invalid("powerShell", "ExecuteScript action requires a PowerShell payload.");
        if (string.IsNullOrWhiteSpace(action.PowerShell.ScriptPath))
            return Invalid("powerShell.scriptPath", "ExecuteScript action requires a script path.");

        return Valid(
            action.Type,
            WorkflowEffectCategory.ScriptProcess,
            "Run the configured PowerShell script.",
            "process:powershell");
    }

    private static WorkflowActionPlan PlanSignal(WorkflowAction action)
    {
        var payload = action.SelectSignalAspect;
        if (payload == null)
            return Invalid("selectSignalAspect", "SelectSignalAspect action requires a signal payload.");
        if (payload.BaseAddress <= 0)
            return Invalid("selectSignalAspect.baseAddress", "Signal base address must be greater than zero.");
        if (string.IsNullOrWhiteSpace(payload.MultiplexerArticleNumber))
            return Invalid("selectSignalAspect.multiplexerArticleNumber", "Multiplexer article number is required.");
        if (string.IsNullOrWhiteSpace(payload.SignalArticleNumber))
            return Invalid("selectSignalAspect.signalArticleNumber", "Signal article number is required.");

        ResolvedMultiplexerCommand command;
        try
        {
            command = MultiplexerCommandResolver.Resolve(
                payload.BaseAddress,
                payload.MultiplexerArticleNumber,
                payload.SignalArticleNumber,
                payload.SignalAspect);
        }
        catch (ArgumentException ex)
        {
            return Invalid("selectSignalAspect", ex.Message);
        }

        var resourceKey = $"z21:turnout:{command.DccAddress}:{command.Output}";
        return Valid(
            action.Type,
            WorkflowEffectCategory.Signal,
            $"Select signal aspect {payload.SignalAspect}.",
            resourceKey);
    }

    private static WorkflowActionPlan PlanDisplay(WorkflowAction action)
    {
        if (action.TrainDestinationDisplay == null)
            return Invalid("trainDestinationDisplay", "TrainDestinationDisplay action requires a display payload.");
        if (!WorkflowActionParameterBinding.TryGetDisplayDeviceId(action, out var displayDeviceId))
            return Invalid("trainDestinationDisplay.displayDeviceId", "Display device identifier cannot be empty.");

        return Valid(
            action.Type,
            WorkflowEffectCategory.Display,
            "Update the selected train destination display.",
            $"display:{displayDeviceId:D}");
    }

    private static WorkflowActionPlan PlanJourneyTransition(WorkflowAction action)
    {
        var payload = action.ChangeJourneyStop;
        if (payload == null)
            return Invalid("changeJourneyStop", "ChangeJourneyStop action requires a journey transition payload.");
        if (!payload.MoveToNextStop && (!payload.TargetStationId.HasValue || payload.TargetStationId == Guid.Empty))
            return Invalid("changeJourneyStop.targetStationId", "A specific journey stop requires a target station identifier.");

        return Valid(
            action.Type,
            WorkflowEffectCategory.JourneyState,
            payload.MoveToNextStop ? "Move the active journey to its next stop." : "Move the active journey to the selected stop.",
            "journey:current");
    }

    private static WorkflowActionPlan Valid(
        ActionType actionType,
        WorkflowEffectCategory category,
        string description,
        string resourceKey) =>
        new(
            [],
            new WorkflowPlannedEffect(
                actionType,
                category,
                description,
                [new WorkflowResourceDescriptor(resourceKey, category, WorkflowResourceAccess.ExclusiveWrite)]));

    private static WorkflowActionPlan Invalid(string fieldPath, string message) =>
        new([new WorkflowActionPlanningIssue(fieldPath, message)], null);
}
