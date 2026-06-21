// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MAUI.Service;

using Common.Events;
using Common.Runtime;

using SharedUI.Interface;

/// <summary>
/// Bridges remote runtime hub snapshots into the local event bus for shared ViewModels.
/// Coalesces rapid snapshot bursts so the UI thread stays responsive (especially under debugger attach).
/// </summary>
public sealed class RemoteRuntimeBridge : IDisposable
{
    private const int SnapshotCoalesceMilliseconds = 50;

    private readonly object _snapshotGate = new();
    private readonly IRuntimeHubRemoteClient _runtimeHubRemoteClient;
    private readonly IEventBus _eventBus;
    private MobaRuntimeSnapshot? _pendingSnapshot;
    private int _snapshotCoalesceScheduled;

    public RemoteRuntimeBridge(IRuntimeHubRemoteClient runtimeHubRemoteClient, IEventBus eventBus)
    {
        _runtimeHubRemoteClient = runtimeHubRemoteClient;
        _eventBus = eventBus;
        _runtimeHubRemoteClient.SnapshotReceived += OnSnapshotReceivedAsync;
    }

    public void Dispose()
    {
        _runtimeHubRemoteClient.SnapshotReceived -= OnSnapshotReceivedAsync;
    }

    private Task OnSnapshotReceivedAsync(MobaRuntimeSnapshot snapshot)
    {
        lock (_snapshotGate)
        {
            _pendingSnapshot = snapshot;
            if (_snapshotCoalesceScheduled != 0)
            {
                return Task.CompletedTask;
            }

            _snapshotCoalesceScheduled = 1;
        }

        _ = PublishCoalescedSnapshotAsync();
        return Task.CompletedTask;
    }

    private async Task PublishCoalescedSnapshotAsync()
    {
        try
        {
            await Task.Delay(SnapshotCoalesceMilliseconds).ConfigureAwait(false);

            MobaRuntimeSnapshot? snapshot;
            lock (_snapshotGate)
            {
                snapshot = _pendingSnapshot;
                _pendingSnapshot = null;
            }

            if (snapshot != null)
            {
                _eventBus.Publish(new RemoteRuntimeSnapshotChangedEvent(snapshot));
            }
        }
        finally
        {
            var reschedule = false;
            lock (_snapshotGate)
            {
                _snapshotCoalesceScheduled = 0;
                if (_pendingSnapshot != null)
                {
                    _snapshotCoalesceScheduled = 1;
                    reschedule = true;
                }
            }

            if (reschedule)
            {
                _ = PublishCoalescedSnapshotAsync();
            }
        }
    }
}
