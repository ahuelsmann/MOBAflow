// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Service;

using Backend.Interface;

using Contracts.Runtime;

using Domain;

using Interface;

/// <summary>
/// Tells whether a project's runtime currently controls a locomotive: the runtime is connected to its Z21 and holds
/// a state for the locomotive's address, because it commanded the locomotive or saw it there. Train Control requests
/// that state as soon as it selects a locomotive, so a selected locomotive counts as controlled at any speed.
/// </summary>
public sealed class RuntimeLocomotiveControl : IDisposable
{
    private readonly IProjectRuntimeSnapshots _snapshots;
    private readonly IUiDispatcher _dispatcher;
    private readonly Lock _gate = new();
    private readonly Dictionary<Guid, HashSet<int>> _controlledByProject = [];
    private bool _disposed;

    public RuntimeLocomotiveControl(IProjectRuntimeSnapshots snapshots, IUiDispatcher dispatcher)
    {
        _snapshots = snapshots ?? throw new ArgumentNullException(nameof(snapshots));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        foreach (var snapshot in _snapshots.Snapshots)
            _controlledByProject[snapshot.ProjectId] = ControlledAddresses(snapshot);
        _snapshots.RuntimeSnapshotChanged += OnRuntimeSnapshotChanged;
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

    // Snapshots arrive from runtime threads with every speed change; only a different set of addresses is reported.
    private void OnRuntimeSnapshotChanged(object? sender, ProjectRuntimeSnapshotEventArgs args)
    {
        var controlled = ControlledAddresses(args.Snapshot);
        lock (_gate)
        {
            if (_controlledByProject.TryGetValue(args.Snapshot.ProjectId, out var previous) && previous.SetEquals(controlled))
                return;
            _controlledByProject[args.Snapshot.ProjectId] = controlled;
        }

        _dispatcher.InvokeOnUi(() =>
        {
            if (!_disposed)
                Changed?.Invoke(this, EventArgs.Empty);
        });
    }

    private static HashSet<int> ControlledAddresses(MobaRuntimeSnapshot snapshot) =>
        snapshot.IsConnected ? [.. snapshot.LocomotiveStates.Keys] : [];
}
