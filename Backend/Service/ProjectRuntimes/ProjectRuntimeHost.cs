// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service.ProjectRuntimes;

using Common.Events;
using Contracts.Runtime;

using Domain;

using Interface;

using Microsoft.Extensions.Logging;

/// <summary>
/// Holds one runtime per project of the loaded solution. All runtimes run at the same time; the selected project
/// only decides which runtime the UI shows. Runtimes live as long as their solution.
/// </summary>
public sealed class ProjectRuntimeHost(
    ProjectRuntimeFactory factory, ILogger<ProjectRuntimeHost> logger, ILocomotiveTrainReferenceAnalyzer analyzer)
    : IProjectRuntimeHost, IProjectRuntimeSnapshots, ILocomotiveTrainRuntimeUsageReader, IAsyncDisposable
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
    private readonly ILocomotiveTrainReferenceAnalyzer _usageAnalyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
    private readonly Dictionary<Guid, ProjectRuntime> _runtimes = [];
    // The order of the projects in the solution; the earliest project of a Z21 owns its connection.
    private readonly List<Guid> _order = [];
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<Guid, Guid> _snapshotSubscriptions = [];
    private Guid? _selectedId;
    private Guid? _requestedSelection;
    private int _disposed;
    private static readonly Action<ILogger, Guid, Exception?> LogUsageReadFailed =
        LoggerMessage.Define<Guid>(LogLevel.Error, new EventId(3, nameof(LogUsageReadFailed)),
            "Reading locomotive train usage failed for project {ProjectId}");
    private static readonly Action<ILogger, Guid, Exception?> LogUsageRevalidationFailed =
        LoggerMessage.Define<Guid>(LogLevel.Error, new EventId(4, nameof(LogUsageRevalidationFailed)),
            "Revalidating locomotive train usage failed for project {ProjectId}");

    /// <inheritdoc />
    public async Task<LocomotiveTrainRuntimeObservation> ReadAsync(
        LocomotiveTrainUsageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        cancellationToken.ThrowIfCancellationRequested();
        var entered = false;
        try
        {
            if (Volatile.Read(ref _disposed) != 0)
                return LocomotiveTrainRuntimeObservation.Unknown(request, LocomotiveTrainCoverageReason.OwnerUnavailable);
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            entered = true;
            var owner = Get(request.ProjectId);
            if (Volatile.Read(ref _disposed) != 0 || owner is null || owner.IsDisposed)
                return LocomotiveTrainRuntimeObservation.Unknown(request, LocomotiveTrainCoverageReason.OwnerUnavailable);
            if (!ReferenceEquals(owner.Project, request.ExpectedProject))
                return LocomotiveTrainRuntimeObservation.Unknown(request, LocomotiveTrainCoverageReason.ContextMismatch);
            var captured = await owner.CaptureLocomotiveTrainUsageAsync(_usageAnalyzer, request.LocomotiveId, cancellationToken)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (Volatile.Read(ref _disposed) != 0 || owner.IsDisposed || !ReferenceEquals(Get(request.ProjectId), owner))
                return LocomotiveTrainRuntimeObservation.Unknown(request, LocomotiveTrainCoverageReason.OwnerUnavailable);
            if (captured is null)
                return LocomotiveTrainRuntimeObservation.Unknown(request, LocomotiveTrainCoverageReason.ContextMismatch);

            var receipt = new LocomotiveTrainUsageReceipt(this, owner, captured.Context, captured.Definition);
            var observation = new LocomotiveTrainRuntimeObservation(
                request.RequestId, request.ProjectId, DateTimeOffset.UtcNow, captured.Result, receipt);
            receipt.Observation = observation;
            return observation;
        }
        catch (ObjectDisposedException)
        {
            return LocomotiveTrainRuntimeObservation.Unknown(request, LocomotiveTrainCoverageReason.OwnerUnavailable);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogUsageReadFailed(_logger, request.ProjectId, exception);
            return LocomotiveTrainRuntimeObservation.Unknown(request, LocomotiveTrainCoverageReason.QueryFailed);
        }
        finally
        {
            if (entered)
                ReleaseUsageGate();
        }
    }

    /// <inheritdoc />
    public async Task<LocomotiveTrainObservationValidity> IsCurrentAsync(
        LocomotiveTrainRuntimeObservation observation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(observation);
        cancellationToken.ThrowIfCancellationRequested();
        if (Volatile.Read(ref _disposed) != 0)
            return LocomotiveTrainObservationValidity.Unavailable;
        var receipt = observation.Receipt;
        if (receipt is null || !ReferenceEquals(receipt.Host, this) || !ReferenceEquals(receipt.Observation, observation)
            || receipt.Owner is not ProjectRuntime owner)
            return LocomotiveTrainObservationValidity.Stale;
        var entered = false;
        try
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            entered = true;
            if (Volatile.Read(ref _disposed) != 0)
                return LocomotiveTrainObservationValidity.Unavailable;
            if (!ReferenceEquals(Get(observation.ProjectId), owner))
                return LocomotiveTrainObservationValidity.Stale;
            if (owner.IsDisposed)
                return LocomotiveTrainObservationValidity.Unavailable;
            var current = await owner.IsLocomotiveTrainUsageCurrentAsync(receipt, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (Volatile.Read(ref _disposed) != 0)
                return LocomotiveTrainObservationValidity.Unavailable;
            if (!ReferenceEquals(Get(observation.ProjectId), owner))
                return LocomotiveTrainObservationValidity.Stale;
            if (owner.IsDisposed)
                return LocomotiveTrainObservationValidity.Unavailable;
            return current
                ? LocomotiveTrainObservationValidity.Current : LocomotiveTrainObservationValidity.Stale;
        }
        catch (ObjectDisposedException)
        {
            return LocomotiveTrainObservationValidity.Unavailable;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogUsageRevalidationFailed(_logger, observation.ProjectId, exception);
            return LocomotiveTrainObservationValidity.Unavailable;
        }
        finally
        {
            if (entered)
                ReleaseUsageGate();
        }
    }

    private void ReleaseUsageGate()
    {
        try
        {
            _gate.Release();
        }
        catch (ObjectDisposedException) when (Volatile.Read(ref _disposed) != 0)
        {
            // Existing disposal bypasses the host gate; a read must not release a disposed semaphore.
        }
    }

    /// <inheritdoc />
    public event EventHandler<ProjectRuntimeSnapshotEventArgs>? RuntimeSnapshotChanged;

    /// <inheritdoc />
    public IReadOnlyList<MobaRuntimeSnapshot> Snapshots => [.. Runtimes.Select(runtime => runtime.Runtime.Current)];

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
            await DisposeAllAsync(cancellationToken).ConfigureAwait(false);
            lock (_runtimes)
            {
                _order.Clear();
                _order.AddRange(projects.Select(project => project.Id));
            }

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
            lock (_runtimes)
            {
                if (!_order.Contains(project.Id))
                {
                    _order.Add(project.Id);
                }
            }

            await AddCoreAsync(project, cancellationToken).ConfigureAwait(false);
            await ReconcileZ21OwnersAsync([Get(project.Id)?.Z21Key], cancellationToken).ConfigureAwait(false);
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
            var removedKey = Get(projectId)?.Z21Key;
            lock (_runtimes)
            {
                _order.Remove(projectId);
            }

            await RemoveCoreAsync(projectId, cancellationToken).ConfigureAwait(false);
            await ReconcileZ21OwnersAsync([removedKey], cancellationToken).ConfigureAwait(false);
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
            var previousKey = Get(project.Id)?.Z21Key;
            lock (_runtimes)
            {
                if (!_order.Contains(project.Id))
                {
                    _order.Add(project.Id);
                }
            }

            await RemoveCoreAsync(project.Id, cancellationToken).ConfigureAwait(false);
            await AddCoreAsync(project, cancellationToken).ConfigureAwait(false);
            await ReconcileZ21OwnersAsync([previousKey, Get(project.Id)?.Z21Key], cancellationToken).ConfigureAwait(false);
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
        _factory.RenameOwner(project);
        return Get(project.Id)?.UpdateAsync(project, cancellationToken) ?? Task.CompletedTask;
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
        Volatile.Write(ref _disposed, 1);
        await DisposeAllAsync().ConfigureAwait(false);
        _gate.Dispose();
    }

    private async Task AddCoreAsync(Project project, CancellationToken cancellationToken)
    {
        var runtime = _factory.Create(project);
        lock (_runtimes)
        {
            _runtimes[project.Id] = runtime;
            _snapshotSubscriptions[project.Id] =
                runtime.Connection.EventBus.Subscribe<RuntimeSnapshotChangedEvent>(OnRuntimeSnapshotChanged);
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

    /// <summary>
    /// The earliest project of a Z21 in the solution owns its connection. When a later project holds it, for
    /// example after an assignment changed or the owner was removed, the runtimes of that Z21 are created again
    /// in solution order.
    /// </summary>
    private async Task ReconcileZ21OwnersAsync(IEnumerable<string?> keys, CancellationToken cancellationToken)
    {
        foreach (var key in keys.OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase))
        {
            List<ProjectRuntime> sharing;
            lock (_runtimes)
            {
                sharing = [.. _order
                    .Select(projectId => _runtimes.GetValueOrDefault(projectId))
                    .OfType<ProjectRuntime>()
                    .Where(runtime => string.Equals(runtime.Z21Key, key, StringComparison.OrdinalIgnoreCase))];
            }

            if (sharing.Count == 0 || sharing[0].Connection.ConflictProjectName is null)
            {
                continue;
            }

            foreach (var runtime in sharing)
            {
                await RemoveCoreAsync(runtime.ProjectId, cancellationToken).ConfigureAwait(false);
            }

            foreach (var runtime in sharing)
            {
                await AddCoreAsync(runtime.Project, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task RemoveCoreAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var runtime = Detach(projectId);
        if (runtime is null)
        {
            return;
        }

        // No runtime is discarded while its trains could still be moving. A cancelled stop (for example the
        // shutdown timeout) still discards the runtime and then ends the operation.
        try
        {
            await runtime.StopLocomotivesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogStopLocomotivesFailed(_logger, projectId, ex);
        }
        finally
        {
            await runtime.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task DisposeAllAsync(CancellationToken cancellationToken = default)
    {
        List<Guid> projectIds;
        lock (_runtimes)
        {
            projectIds = [.. _runtimes.Keys];
        }

        foreach (var projectId in projectIds)
        {
            await RemoveCoreAsync(projectId, cancellationToken).ConfigureAwait(false);
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
            if (!_runtimes.Remove(projectId, out var runtime))
            {
                return null;
            }

            if (_snapshotSubscriptions.Remove(projectId, out var subscription))
            {
                runtime.Connection.EventBus.Unsubscribe(subscription);
            }

            return runtime;
        }
    }

    private void OnRuntimeSnapshotChanged(RuntimeSnapshotChangedEvent e) => RuntimeSnapshotChanged?.Invoke(this, new ProjectRuntimeSnapshotEventArgs(e.Snapshot));

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
