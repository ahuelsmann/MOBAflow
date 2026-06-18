// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MAUI.Service;

using Common.Events;
using Common.Runtime;

using SharedUI.Interface;

/// <summary>
/// Bridges remote runtime hub snapshots into the local event bus for shared ViewModels.
/// </summary>
public sealed class RemoteRuntimeBridge : IDisposable
{
    private readonly IRuntimeHubRemoteClient _runtimeHubRemoteClient;
    private readonly IEventBus _eventBus;

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
        _eventBus.Publish(new RemoteRuntimeSnapshotChangedEvent(snapshot));
        return Task.CompletedTask;
    }
}
