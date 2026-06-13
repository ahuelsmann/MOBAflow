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

    JourneySessionState? GetState(Guid journeyId);

    void Reset(Journey journey);
}

public interface IStationManager : IDisposable
{
    event EventHandler<StationFeedbackEventArgs>? StationChanged;

    event EventHandler<PlatformChangedEventArgs>? PlatformChanged;

    IReadOnlyDictionary<Guid, StationSessionState> States { get; }

    StationSessionState? GetState(Guid stationId);

    void ResetAll();
}

public interface IPlatformManager : IDisposable
{
    event EventHandler<PlatformChangedEventArgs>? PlatformChanged;

    IReadOnlyDictionary<Guid, PlatformSessionState> States { get; }

    PlatformSessionState? GetState(Guid platformId);

    void ResetAll();
}

public interface IJourneyManagerFactory
{
    IJourneyManager Create(Project project, ActionExecutionContext executionContext);
}

public interface IStationManagerFactory
{
    IStationManager Create(Project project, ActionExecutionContext executionContext);
}

public interface IPlatformManagerFactory
{
    IPlatformManager Create(Project project, Station station, ActionExecutionContext? executionContext = null);
}

public sealed class JourneyManagerFactory(
    IZ21 z21,
    IWorkflowService workflowService,
    ILogger<JourneyManager>? logger = null) : IJourneyManagerFactory
{
    public IJourneyManager Create(Project project, ActionExecutionContext executionContext) =>
        new JourneyManager(z21, project, workflowService, executionContext, logger);
}

public sealed class StationManagerFactory(
    IZ21 z21,
    IWorkflowService workflowService,
    ILogger<StationManager>? logger = null,
    ILoggerFactory? loggerFactory = null,
    IPlatformManagerFactory? platformManagerFactory = null) : IStationManagerFactory
{
    public IStationManager Create(Project project, ActionExecutionContext executionContext) =>
        new StationManager(z21, project, workflowService, executionContext, logger, loggerFactory, platformManagerFactory);
}

public sealed class PlatformManagerFactory(
    IZ21 z21,
    IWorkflowService workflowService,
    ILogger<PlatformManager>? logger = null) : IPlatformManagerFactory
{
    public IPlatformManager Create(Project project, Station station, ActionExecutionContext? executionContext = null) =>
        new PlatformManager(z21, project, station, workflowService, executionContext, logger);
}
