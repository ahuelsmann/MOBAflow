// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service.ProjectRuntimes;

using Domain;

using Interface;

using Microsoft.Extensions.Logging;

/// <summary>
/// Holds one runtime per project of the loaded solution. All runtimes run at the same time; the selected project
/// only decides which runtime the UI shows. Runtimes live as long as their solution.
/// </summary>
public sealed class ProjectRuntimeHost(ProjectRuntimeFactory factory, ILogger<ProjectRuntimeHost> logger)
    : IProjectRuntimeHost, IAsyncDisposable
{
    private static readonly Action<ILogger, Guid, Exception?> LogStopLocomotivesFailed =
        LoggerMessage.Define<Guid>(
            LogLevel.Error,
            new EventId(2, nameof(LogStopLocomotivesFailed)),
            "Setting the locomotives of project {ProjectId} to speed 0 failed");

    private static readonly Action<ILogger, string, Exception?> LogRuntimeStartFailed =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1, nameof(LogRuntimeStartFailed)),
            "Starting the runtime of project '{ProjectName}' failed");

    private readonly ProjectRuntimeFactory _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    private readonly ILogger<ProjectRuntimeHost> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly Dictionary<Guid, ProjectRuntime> _runtimes = [];
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Guid? _selectedId;
    private Guid? _requestedSelection;

    /// <summary>Raised after the selected runtime changed.</summary>
    public event EventHandler? SelectedRuntimeChanged;

    /// <summary>Gets the runtime of the selected project, if any.</summary>
    public ProjectRuntime? Selected => _selectedId is { } id ? Get(id) : null;

    /// <summary>Gets the runtimes of all projects.</summary>
    public IReadOnlyCollection<ProjectRuntime> Runtimes
    {
        get
        {
            lock (_runtimes)
            {
                return [.. _runtimes.Values];
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyCollection<Guid> ConnectedProjectIds =>
        [.. Runtimes.Where(runtime => runtime.IsConnected).Select(runtime => runtime.ProjectId)];

    /// <summary>Gets the runtime of a project, if it exists.</summary>
    public ProjectRuntime? Get(Guid projectId)
    {
        lock (_runtimes)
        {
            return _runtimes.GetValueOrDefault(projectId);
        }
    }

    /// <inheritdoc />
    public async Task LoadAsync(IReadOnlyList<Project> projects, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projects);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await DisposeAllAsync().ConfigureAwait(false);
            foreach (var project in projects)
            {
                await AddCoreAsync(project, cancellationToken).ConfigureAwait(false);
            }

            await _factory.DisconnectUnusedAsync().ConfigureAwait(false);
            SelectCore(_requestedSelection);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task AddAsync(Project project, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await AddCoreAsync(project, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await RemoveCoreAsync(projectId).ConfigureAwait(false);
            await _factory.DisconnectUnusedAsync().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task ReplaceAsync(Project project, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await RemoveCoreAsync(project.Id).ConfigureAwait(false);
            await AddCoreAsync(project, cancellationToken).ConfigureAwait(false);
            await _factory.DisconnectUnusedAsync().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public Task UpdateAsync(Project project, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        return Get(project.Id)?.Runtime.UpdateProjectAsync(project, cancellationToken) ?? Task.CompletedTask;
    }

    /// <inheritdoc />
    public void SelectProject(Guid? projectId)
    {
        // Remembered so that the selection applies once the project's runtime exists.
        _requestedSelection = projectId;
        SelectCore(projectId);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisposeAllAsync().ConfigureAwait(false);
        _gate.Dispose();
    }

    private async Task AddCoreAsync(Project project, CancellationToken cancellationToken)
    {
        var runtime = _factory.Create(project);
        lock (_runtimes)
        {
            _runtimes[project.Id] = runtime;
        }

        if (_requestedSelection == project.Id)
        {
            SelectCore(project.Id);
        }

        try
        {
            await runtime.StartAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A runtime that cannot start shows its state; the other projects keep running.
            LogRuntimeStartFailed(_logger, project.Name, ex);
        }
    }

    private async Task RemoveCoreAsync(Guid projectId)
    {
        var runtime = Detach(projectId);
        if (runtime is null)
        {
            return;
        }

        // No runtime is discarded while its trains could still be moving.
        try
        {
            await runtime.StopLocomotivesAsync().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogStopLocomotivesFailed(_logger, projectId, ex);
        }

        await runtime.DisposeAsync().ConfigureAwait(false);
    }

    private async Task DisposeAllAsync()
    {
        List<Guid> projectIds;
        lock (_runtimes)
        {
            projectIds = [.. _runtimes.Keys];
        }

        foreach (var projectId in projectIds)
        {
            await RemoveCoreAsync(projectId).ConfigureAwait(false);
        }
    }

    private ProjectRuntime? Detach(Guid projectId)
    {
        if (_selectedId == projectId)
        {
            SelectCore(null);
        }

        lock (_runtimes)
        {
            return _runtimes.Remove(projectId, out var runtime) ? runtime : null;
        }
    }

    private void SelectCore(Guid? projectId)
    {
        var next = projectId is { } id ? Get(id) : null;
        var nextId = next?.ProjectId;
        if (nextId == _selectedId)
        {
            return;
        }

        if (Selected is { } previous)
        {
            previous.Connection.EventBus.IsForwarding = false;
        }

        _selectedId = nextId;
        if (next is not null)
        {
            next.Connection.EventBus.IsForwarding = true;
        }

        SelectedRuntimeChanged?.Invoke(this, EventArgs.Empty);
    }
}
