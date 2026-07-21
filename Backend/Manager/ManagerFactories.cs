// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Manager;

using Domain;

using Interface;

using Microsoft.Extensions.Logging;

using Service;

public interface IJourneyManager : IDisposable
{
    event EventHandler<StationChangedEventArgs>? StationChanged;

    event EventHandler<JourneyFeedbackEventArgs>? FeedbackReceived;

    event EventHandler<JourneyCompletedEventArgs>? JourneyCompleted;

    JourneySessionState? GetState(Guid journeyId);

    void Reset(Journey journey);

    void CancelPendingWork();
}

public interface IPlatformManager : IDisposable
{
    event EventHandler<PlatformChangedEventArgs>? PlatformChanged;

    IReadOnlyDictionary<Guid, PlatformSessionState> States { get; }

    PlatformSessionState? GetState(Guid platformId);

    void ResetAll();
}

public sealed class JourneyManagerFactory(
    IZ21 z21,
    IWorkflowService workflowService,
    IJourneyStopTransitionService? stopTransitionService = null,
    IJourneyRuntimeStateStore? runtimeStateStore = null,
    ILogger<JourneyManager>? logger = null,
    TimeProvider? timeProvider = null)
{
    public IJourneyManager Create(Project project, ActionExecutionContext executionContext) =>
        new JourneyManager(
            z21,
            project,
            workflowService,
            executionContext,
            logger,
            stopTransitionService,
            runtimeStateStore,
            timeProvider);
}

public sealed class PlatformManagerFactory(
    IZ21 z21,
    IWorkflowService workflowService,
    ILogger<PlatformManager>? logger = null)
{
    public IPlatformManager Create(Project project, Station station, ActionExecutionContext? executionContext = null) =>
        new PlatformManager(z21, project, station, workflowService, executionContext, logger);
}
