// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Manager;

using Common.Extension;

using Domain;

using Interface;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Service;

public sealed class StationManager : IStationManager
{
    private readonly SemaphoreSlim _processingLock = new(1, 1);
    private readonly IZ21 _z21;
    private readonly Project _project;
    private readonly IWorkflowService _workflowService;
    private readonly IActionExecutionContextFactory _executionContextFactory;
    private readonly ILogger<StationManager> _logger;
    private readonly Dictionary<Guid, StationSessionState> _states = [];
    private readonly List<IPlatformManager> _platformManagers = [];
    private bool _disposed;

    public StationManager(
        IZ21 z21,
        Project project,
        IWorkflowService workflowService,
        ActionExecutionContext? executionContext = null,
        ILogger<StationManager>? logger = null,
        ILoggerFactory? loggerFactory = null,
        IPlatformManagerFactory? platformManagerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(z21);
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(workflowService);

        _z21 = z21;
        _project = project;
        _workflowService = workflowService;
        _executionContextFactory = new ActionExecutionContextFactory(executionContext ?? new ActionExecutionContext { Z21 = z21 });
        _logger = logger ?? NullLogger<StationManager>.Instance;
        platformManagerFactory ??= new PlatformManagerFactory(z21, workflowService, loggerFactory?.CreateLogger<PlatformManager>());

        foreach (var station in project.Stations)
        {
            _states[station.Id] = new StationSessionState { StationId = station.Id };
            var platformManager = platformManagerFactory.Create(project, station, executionContext);
            platformManager.PlatformChanged += OnPlatformChanged;
            _platformManagers.Add(platformManager);
        }

        _z21.Received += OnFeedbackReceived;
    }

    public event EventHandler<StationFeedbackEventArgs>? StationChanged;

    public event EventHandler<PlatformChangedEventArgs>? PlatformChanged;

    public IReadOnlyDictionary<Guid, StationSessionState> States => _states;

    public IReadOnlyList<IPlatformManager> PlatformManagers => _platformManagers;

    public StationSessionState? GetState(Guid stationId) => _states.GetValueOrDefault(stationId);

    public void ResetAll()
    {
        foreach (var state in _states.Values)
        {
            state.Counter = 0;
            state.IsOccupied = false;
            state.LastFeedbackTime = null;
            state.LastPlatformId = null;
        }

        foreach (var platformManager in _platformManagers)
        {
            platformManager.ResetAll();
        }
    }

    private void OnFeedbackReceived(FeedbackResult feedback)
    {
        ProcessFeedbackAsync(feedback)
            .Observe(ex => _logger.LogWarning(ex, "Station feedback processing failed for InPort {InPort}", feedback.InPort));
    }

    public async Task ProcessFeedbackAsync(FeedbackResult feedback)
    {
        if (_disposed)
        {
            return;
        }

        await _processingLock.WaitAsync().ConfigureAwait(false);
        try
        {
            foreach (var station in _project.Stations)
            {
                if (station.InPort == 0 || station.InPort != feedback.InPort)
                {
                    continue;
                }

                await HandleStationFeedbackAsync(station).ConfigureAwait(false);
            }
        }
        finally
        {
            _processingLock.Release();
        }
    }

    private async Task HandleStationFeedbackAsync(Station station)
    {
        if (!_states.TryGetValue(station.Id, out var state))
        {
            state = new StationSessionState { StationId = station.Id };
            _states[station.Id] = state;
        }

        state.Counter++;
        state.IsOccupied = !state.IsOccupied;
        state.LastFeedbackTime = DateTime.Now;

        StationChanged?.Invoke(this, new StationFeedbackEventArgs
        {
            Station = station,
            SessionState = state
        });

        await ExecuteWorkflowAsync(station).ConfigureAwait(false);
    }

    private async Task ExecuteWorkflowAsync(Station station)
    {
        if (!station.WorkflowId.HasValue)
        {
            return;
        }

        var workflow = _project.Workflows.FirstOrDefault(workflow => workflow.Id == station.WorkflowId.Value);
        if (workflow == null)
        {
            _logger.LogWarning("Workflow with ID {WorkflowId} not found", station.WorkflowId.Value);
            return;
        }

        var executionContext = _executionContextFactory.Create(new ActionExecutionContextState
        {
            CurrentProject = _project,
            CurrentStation = station
        });

        await _workflowService.ExecuteAsync(workflow, executionContext).ConfigureAwait(false);
    }

    private void OnPlatformChanged(object? sender, PlatformChangedEventArgs e)
    {
        if (_states.TryGetValue(e.Station.Id, out var state))
        {
            state.LastPlatformId = e.Platform.Id;
            state.IsOccupied = e.SessionState.IsOccupied;
            state.LastFeedbackTime = e.SessionState.LastFeedbackTime;
        }

        PlatformChanged?.Invoke(this, e);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _z21.Received -= OnFeedbackReceived;
        foreach (var platformManager in _platformManagers)
        {
            platformManager.PlatformChanged -= OnPlatformChanged;
            platformManager.Dispose();
        }

        _processingLock.Dispose();
        _disposed = true;
    }
}