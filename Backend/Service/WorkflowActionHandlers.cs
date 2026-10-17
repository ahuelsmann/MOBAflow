// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Domain;
using Domain.Enum;
using Interface;
using Microsoft.Extensions.Logging;

/// <summary>Sends validated command bytes through the selected effect adapter.</summary>
public sealed class CommandWorkflowActionHandler(
    ILogger<CommandWorkflowActionHandler> logger,
    IWorkflowEffectAdapter effectAdapter) : IWorkflowActionHandler
{
    private readonly ILogger<CommandWorkflowActionHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IWorkflowEffectAdapter _effectAdapter = effectAdapter ?? throw new ArgumentNullException(nameof(effectAdapter));
    public ActionType ActionType => ActionType.Command;

    public Task ExecuteAsync(WorkflowAction action, ActionExecutionContext context) =>
        ExecuteAsync(action, context, CancellationToken.None);

    public Task ExecuteAsync(WorkflowAction action, ActionExecutionContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (action.Command == null)
            throw new ArgumentException("Command action requires a command payload");
        var request = new WorkflowEffectPlanner().Plan(action).Request;
        if (request == null)
        {
            _logger.LogWarning("Command action skipped: no valid bytes provided");
            return Task.CompletedTask;
        }
        return _effectAdapter.ExecuteAsync(request, context, cancellationToken);
    }
}

/// <summary>Validates an audio intent without probing its environment.</summary>
public sealed class AudioWorkflowActionHandler(IWorkflowEffectAdapter effectAdapter) : IWorkflowActionHandler
{
    private readonly IWorkflowEffectAdapter _effectAdapter = effectAdapter ?? throw new ArgumentNullException(nameof(effectAdapter));
    public ActionType ActionType => ActionType.Audio;

    public Task ExecuteAsync(WorkflowAction action, ActionExecutionContext context) =>
        ExecuteAsync(action, context, CancellationToken.None);

    public Task ExecuteAsync(WorkflowAction action, ActionExecutionContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (action.Audio == null)
            throw new ArgumentException("Audio action requires an audio payload");
        var request = new WorkflowEffectPlanner().Plan(action).Request
            ?? throw new ArgumentException("Audio action requires a valid FilePath parameter");
        return _effectAdapter.ExecuteAsync(request, context, cancellationToken);
    }
}

/// <summary>Resolves announcement text against the owning invocation before selecting output.</summary>
public sealed class AnnouncementWorkflowActionHandler(
    IAnnouncementService announcementService,
    ILogger<AnnouncementWorkflowActionHandler> logger,
    IWorkflowEffectAdapter effectAdapter) : IWorkflowActionHandler
{
    private readonly IAnnouncementService _announcementService = announcementService ?? throw new ArgumentNullException(nameof(announcementService));
    private readonly ILogger<AnnouncementWorkflowActionHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IWorkflowEffectAdapter _effectAdapter = effectAdapter ?? throw new ArgumentNullException(nameof(effectAdapter));
    public ActionType ActionType => ActionType.Announcement;

    public Task ExecuteAsync(WorkflowAction action, ActionExecutionContext context) =>
        ExecuteAsync(action, context, CancellationToken.None);

    public Task ExecuteAsync(WorkflowAction action, ActionExecutionContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var templateText = action.Announcement?.Message ?? context.JourneyTemplateText;
        if (string.IsNullOrEmpty(templateText) || context.CurrentStation == null)
        {
            _logger.LogWarning("Announcement '{ActionName}' skipped: template or station unavailable", action.Name);
            return Task.CompletedTask;
        }
        var stationIndex = context.CurrentStationIndex.GetValueOrDefault(1);
        var request = new WorkflowAnnouncementEffectRequest(action.Id, action.Name, templateText)
        {
            Station = context.CurrentStation,
            StationIndex = stationIndex,
            RenderedText = _announcementService.GenerateAnnouncementText(templateText, context.CurrentStation, stationIndex, action.Name)
        };
        return _effectAdapter.ExecuteAsync(request, context, cancellationToken);
    }
}

/// <summary>Validates display intent while retaining unconfigured production delivery.</summary>
public sealed class TrainDestinationDisplayWorkflowActionHandler(
    ILogger<TrainDestinationDisplayWorkflowActionHandler> logger,
    IWorkflowEffectAdapter effectAdapter) : IWorkflowActionHandler
{
    private readonly ILogger<TrainDestinationDisplayWorkflowActionHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IWorkflowEffectAdapter _effectAdapter = effectAdapter ?? throw new ArgumentNullException(nameof(effectAdapter));
    public ActionType ActionType => ActionType.TrainDestinationDisplay;

    public Task ExecuteAsync(WorkflowAction action, ActionExecutionContext context) =>
        ExecuteAsync(action, context, CancellationToken.None);

    public Task ExecuteAsync(WorkflowAction action, ActionExecutionContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var request = new WorkflowEffectPlanner().Plan(action).Request;
        if (request == null)
        {
            _logger.LogWarning("Train destination display action '{ActionName}' skipped: display service not configured", action.Name);
            return Task.CompletedTask;
        }
        return _effectAdapter.ExecuteAsync(request, context, cancellationToken);
    }
}

/// <summary>Shares signal resolution before dispatching the resolved accessory intent.</summary>
public sealed class SelectSignalAspectWorkflowActionHandler(IWorkflowEffectAdapter effectAdapter) : IWorkflowActionHandler
{
    private readonly IWorkflowEffectAdapter _effectAdapter = effectAdapter ?? throw new ArgumentNullException(nameof(effectAdapter));
    public ActionType ActionType => ActionType.SelectSignalAspect;

    public Task ExecuteAsync(WorkflowAction action, ActionExecutionContext context) =>
        ExecuteAsync(action, context, CancellationToken.None);

    public Task ExecuteAsync(WorkflowAction action, ActionExecutionContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payload = action.SelectSignalAspect
            ?? throw new ArgumentException("Select signal aspect action requires a signal aspect payload");
        var command = MultiplexerCommandResolver.Resolve(payload.BaseAddress, payload.MultiplexerArticleNumber,
            payload.SignalArticleNumber, payload.SignalAspect);
        var request = new WorkflowSignalEffectRequest(action.Id, action.Name, payload.SignalAspect, command);
        return _effectAdapter.ExecuteAsync(request, context, cancellationToken);
    }
}

/// <summary>Validates script intent; file probes and process execution belong to production output.</summary>
public sealed class ExecuteScriptWorkflowActionHandler(IWorkflowEffectAdapter effectAdapter) : IWorkflowActionHandler
{
    private readonly IWorkflowEffectAdapter _effectAdapter = effectAdapter ?? throw new ArgumentNullException(nameof(effectAdapter));
    public ActionType ActionType => ActionType.ExecuteScript;

    public Task ExecuteAsync(WorkflowAction action, ActionExecutionContext context) =>
        ExecuteAsync(action, context, CancellationToken.None);

    public Task ExecuteAsync(WorkflowAction action, ActionExecutionContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (action.PowerShell == null)
            throw new ArgumentException("ExecuteScript action requires a PowerShell payload");
        var request = new WorkflowEffectPlanner().Plan(action).Request
            ?? throw new ArgumentException("ExecuteScript action requires a script path");
        return _effectAdapter.ExecuteAsync(request, context, cancellationToken);
    }
}
/// <summary>Moves the active journey to the next or an explicitly configured stop.</summary>
public sealed class ChangeJourneyStopWorkflowActionHandler(IJourneyStopTransitionService transitionService) : IWorkflowActionHandler
{
    private readonly IJourneyStopTransitionService _transitionService = transitionService ?? throw new ArgumentNullException(nameof(transitionService));

    public ActionType ActionType => ActionType.ChangeJourneyStop;

    public Task ExecuteAsync(WorkflowAction action, ActionExecutionContext context) =>
        ExecuteAsync(action, context, CancellationToken.None);

    public Task ExecuteAsync(WorkflowAction action, ActionExecutionContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payload = action.ChangeJourneyStop ?? throw new ArgumentException("Change journey stop action requires a payload");
        var journey = context.CurrentJourney ?? throw new InvalidOperationException("Change journey stop action requires a journey context");
        var state = context.CurrentJourneySessionState ?? throw new InvalidOperationException("Change journey stop action requires a journey state");

        var transition = new JourneyStopTransition
        {
            Mode = payload.MoveToNextStop ? JourneyStopTransitionMode.Next : JourneyStopTransitionMode.SpecificStation,
            StationId = payload.TargetStationId
        };
        var result = context.ApplyJourneyStopTransition is not null
            ? context.ApplyJourneyStopTransition(transition)
            : _transitionService.Apply(journey, state, transition);
        context.CurrentStation = result.CurrentStation;
        context.CurrentStationIndex = result.CurrentStation == null ? null : journey.Stations.IndexOf(result.CurrentStation) + 1;
        return Task.CompletedTask;
    }
}
