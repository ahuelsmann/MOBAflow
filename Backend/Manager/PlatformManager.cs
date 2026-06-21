// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Manager;

using Common.Extension;

using Domain;

using Interface;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Service;

public sealed class PlatformManager : IPlatformManager
{
    private readonly SemaphoreSlim _processingLock = new(1, 1);
    private readonly IZ21 _z21;
    private readonly Station _station;
    private readonly Project _project;
    private readonly IWorkflowService _workflowService;
    private readonly ActionExecutionContextFactory _executionContextFactory;
    private readonly ILogger<PlatformManager> _logger;
    private readonly Dictionary<Guid, PlatformSessionState> _states = [];
    private bool _disposed;

    public PlatformManager(
        IZ21 z21,
        Project project,
        Station station,
        IWorkflowService workflowService,
        ActionExecutionContext? executionContext = null,
        ILogger<PlatformManager>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(z21);
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(station);
        ArgumentNullException.ThrowIfNull(workflowService);

        _z21 = z21;
        _project = project;
        _station = station;
        _workflowService = workflowService;
        _executionContextFactory = new ActionExecutionContextFactory(executionContext ?? new ActionExecutionContext { Z21 = z21 });
        _logger = logger ?? NullLogger<PlatformManager>.Instance;

        foreach (var platform in station.Platforms)
        {
            _states[platform.Id] = new PlatformSessionState
            {
                StationId = station.Id,
                PlatformId = platform.Id
            };
        }

        _z21.Received += OnFeedbackReceived;
    }

    public event EventHandler<PlatformChangedEventArgs>? PlatformChanged;

    public IReadOnlyDictionary<Guid, PlatformSessionState> States => _states;

    public PlatformSessionState? GetState(Guid platformId) => _states.GetValueOrDefault(platformId);

    public void ResetAll()
    {
        foreach (var state in _states.Values)
        {
            state.Counter = 0;
            state.IsOccupied = false;
            state.LastFeedbackTime = null;
        }
    }

    private void OnFeedbackReceived(FeedbackResult feedback)
    {
        ProcessFeedbackAsync(feedback)
            .Observe(ex => _logger.LogWarning(ex, "Platform feedback processing failed for InPort {InPort}", feedback.InPort));
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
            foreach (var platform in _station.Platforms)
            {
                if (platform.InPort == 0 || platform.InPort != feedback.InPort)
                {
                    continue;
                }

                await HandlePlatformFeedbackAsync(platform).ConfigureAwait(false);
            }
        }
        finally
        {
            _processingLock.Release();
        }
    }

    private async Task HandlePlatformFeedbackAsync(Platform platform)
    {
        if (!_states.TryGetValue(platform.Id, out var state))
        {
            state = new PlatformSessionState
            {
                StationId = _station.Id,
                PlatformId = platform.Id
            };
            _states[platform.Id] = state;
        }

        state.Counter++;
        state.IsOccupied = !state.IsOccupied;
        state.LastFeedbackTime = DateTime.Now;

        PlatformChanged?.Invoke(this, new PlatformChangedEventArgs
        {
            Station = _station,
            Platform = platform,
            SessionState = state
        });

        await ExecuteWorkflowAsync(platform).ConfigureAwait(false);
    }

    private async Task ExecuteWorkflowAsync(Platform platform)
    {
        if (!platform.WorkflowId.HasValue)
        {
            return;
        }

        var workflow = _project.Workflows.FirstOrDefault(workflow => workflow.Id == platform.WorkflowId.Value);
        if (workflow == null)
        {
            _logger.LogWarning("Workflow with ID {WorkflowId} not found", platform.WorkflowId.Value);
            return;
        }

        var executionContext = _executionContextFactory.Create(new ActionExecutionContextState
        {
            CurrentProject = _project,
            CurrentStation = _station,
            CurrentPlatform = platform
        });

        await _workflowService.ExecuteAsync(workflow, executionContext).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _z21.Received -= OnFeedbackReceived;
        _processingLock.Dispose();
        _disposed = true;
    }
}