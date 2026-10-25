// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Service;

using Backend.Interface;

using Contracts.Runtime;

using Domain;

using Interface;

/// <summary>
/// Tells whether a project's runtime currently controls a locomotive: the runtime is connected to its Z21 and holds
/// a state for the locomotive's address, because it commanded the locomotive or saw it there. Train Control requests
/// that state as soon as it selects a locomotive or connects, so a selected locomotive counts as controlled at any speed.
/// </summary>
public sealed class RuntimeLocomotiveControl : IDisposable
{
    private readonly IProjectRuntimeSnapshots _snapshots;
    private readonly IUiDispatcher _dispatcher;
    private readonly Lock _gate = new();
    private readonly Dictionary<Guid, HashSet<int>> _controlledByProject = [];
    private int _refreshRequests;
    private bool _disposed;

    public RuntimeLocomotiveControl(IProjectRuntimeSnapshots snapshots, IUiDispatcher dispatcher)
    {
        _snapshots = snapshots ?? throw new ArgumentNullException(nameof(snapshots));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

        // Subscribe first, so a snapshot published while the current ones are read is not lost.
        _snapshots.RuntimeSnapshotChanged += OnRuntimeSnapshotChanged;
        RequestRefresh();
    }

    /// <summary>Raised on the UI thread when the controlled locomotives of a project change.</summary>
    public event EventHandler? Changed;

    /// <summary>Gets whether the runtime of <paramref name="projectId"/> currently controls <paramref name="locomotive"/>.</summary>
    public bool IsControlled(Guid projectId, Locomotive locomotive)
    {
        ArgumentNullException.ThrowIfNull(locomotive);
        if (locomotive.DigitalAddress is not { } address)
            return false;

        lock (_gate)
        {
            return _controlledByProject.TryGetValue(projectId, out var controlled) && controlled.Contains((int)address);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _snapshots.RuntimeSnapshotChanged -= OnRuntimeSnapshotChanged;
    }

    private void OnRuntimeSnapshotChanged(object? sender, ProjectRuntimeSnapshotEventArgs args) => RequestRefresh();

    // Snapshots arrive from runtime threads with every speed change, and one published concurrently can arrive after a
    // newer one. An event therefore only requests a refresh: one thread at a time reads the current snapshot of every
    // project, so a late event cannot replace a newer state. Requests made during a refresh lead to one more refresh.
    private void RequestRefresh()
    {
        if (Interlocked.Increment(ref _refreshRequests) != 1)
            return;

        do
        {
            Volatile.Write(ref _refreshRequests, 1);
            if (Refresh())
            {
                _dispatcher.InvokeOnUi(() =>
                {
                    if (!_disposed)
                        Changed?.Invoke(this, EventArgs.Empty);
                });
            }
        }
        while (Interlocked.Decrement(ref _refreshRequests) != 0);
    }

    /// <summary>Reads the current snapshots; returns whether the controlled locomotives of a project changed.</summary>
    private bool Refresh()
    {
        var changed = false;
        foreach (var snapshot in _snapshots.Snapshots)
        {
            var controlled = ControlledAddresses(snapshot);
            lock (_gate)
            {
                if (_controlledByProject.TryGetValue(snapshot.ProjectId, out var previous) && previous.SetEquals(controlled))
                    continue;
                _controlledByProject[snapshot.ProjectId] = controlled;
                changed = true;
            }
        }

        return changed;
    }

    private static HashSet<int> ControlledAddresses(MobaRuntimeSnapshot snapshot) =>
        snapshot.IsConnected ? [.. snapshot.LocomotiveStates.Keys] : [];
}
