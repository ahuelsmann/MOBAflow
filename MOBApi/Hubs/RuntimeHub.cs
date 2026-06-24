// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MOBApi.Hubs;

using Common.Runtime;
using Common.Security;

using Microsoft.AspNetCore.SignalR;

using Moba.MOBApi.Service;

using System.Net;

/// <summary>
/// SignalR hub for MOBAflow runtime snapshots and remote control commands.
/// </summary>
public sealed class RuntimeHub : Hub
{
    private readonly IRuntimeSnapshotCache _snapshotCache;
    private readonly ISolutionCache _solutionCache;
    private readonly IRuntimeHostRegistry _hostRegistry;
    private readonly IRuntimeRemoteRegistry _remoteRegistry;
    private readonly IRuntimeBroadcastMetrics _broadcastMetrics;
    private readonly IRuntimeCommandQueue _commandQueue;

    public RuntimeHub(
        IRuntimeSnapshotCache snapshotCache,
        ISolutionCache solutionCache,
        IRuntimeHostRegistry hostRegistry,
        IRuntimeRemoteRegistry remoteRegistry,
        IRuntimeBroadcastMetrics broadcastMetrics,
        IRuntimeCommandQueue commandQueue)
    {
        _snapshotCache = snapshotCache;
        _solutionCache = solutionCache;
        _hostRegistry = hostRegistry;
        _remoteRegistry = remoteRegistry;
        _broadcastMetrics = broadcastMetrics;
        _commandQueue = commandQueue;
    }

    public async Task RegisterHost()
    {
        if (!IsLocalhostConnection())
        {
            throw new HubException("Only localhost connections may register as runtime host.");
        }

        _hostRegistry.SetHost(Context.ConnectionId!);
        await Groups.AddToGroupAsync(Context.ConnectionId!, "runtime-host").ConfigureAwait(false);
        await BroadcastSessionStateAsync().ConfigureAwait(false);
    }

    public async Task RegisterRemote(string clientId)
    {
        EnsureRemoteAuthorized();

        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new HubException("ClientId is required.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId!, "runtime-remote").ConfigureAwait(false);
        _remoteRegistry.Register(Context.ConnectionId!, clientId);

        if (_snapshotCache.TryGet(out var entry))
        {
            await Clients.Caller.SendAsync(RuntimeHubMethods.SnapshotUpdated, entry.Json).ConfigureAwait(false);
        }

        if (_solutionCache.TryGet(out var solutionEntry) && !string.IsNullOrWhiteSpace(solutionEntry.SourcePath))
        {
            await Clients.Caller
                .SendAsync(RuntimeHubMethods.SolutionUpdated, solutionEntry.UpdatedAt.ToString("O"))
                .ConfigureAwait(false);
        }

        await Clients.Caller.SendAsync(RuntimeHubMethods.SessionStateChanged, BuildSessionOperational()).ConfigureAwait(false);
    }

    public async Task PushSnapshot(string snapshotJson)
    {
        EnsureHost();

        var snapshot = RuntimeJsonSerializer.Deserialize(snapshotJson)
            ?? throw new HubException("Invalid runtime snapshot payload.");

        _snapshotCache.Set(snapshotJson, snapshot.IsConnected);
        var broadcastJson = _snapshotCache.TryGet(out var cachedEntry) ? cachedEntry.Json : snapshotJson;
        await Clients.Group("runtime-remote").SendAsync(RuntimeHubMethods.SnapshotUpdated, broadcastJson).ConfigureAwait(false);
        await Clients.Group("runtime-remote").SendAsync(RuntimeHubMethods.SessionStateChanged, BuildSessionOperational(snapshot.IsConnected)).ConfigureAwait(false);
        _broadcastMetrics.RecordSnapshotBroadcast();
    }

    public async Task SetSignalAspect(string signalId, string aspect)
    {
        EnsureRemoteAuthorized();

        if (!Guid.TryParse(signalId, out _))
        {
            throw new HubException("Invalid signal id.");
        }

        if (await TryForwardSetSignalAspectAsync(signalId, aspect).ConfigureAwait(false))
        {
            return;
        }

        if (!Enum.TryParse<Domain.SignalAspect>(aspect, out var parsedAspect))
        {
            throw new HubException("Invalid signal aspect.");
        }

        _commandQueue.Enqueue(new RuntimeCommandEnvelope
        {
            Type = RuntimeCommandType.SetSignalAspect,
            SignalId = Guid.Parse(signalId),
            SignalAspect = parsedAspect
        });
    }

    public async Task SetLocomotiveDrive(int address, int speed, bool forward)
    {
        EnsureRemoteAuthorized();

        if (await TryForwardSetLocomotiveDriveAsync(address, speed, forward).ConfigureAwait(false))
        {
            return;
        }

        _commandQueue.Enqueue(new RuntimeCommandEnvelope
        {
            Type = RuntimeCommandType.SetLocomotiveDrive,
            LocomotiveAddress = address,
            Speed = speed,
            Forward = forward
        });
    }

    public async Task SetLocomotiveFunction(int address, int functionIndex, bool isOn)
    {
        EnsureRemoteAuthorized();

        if (await TryForwardSetLocomotiveFunctionAsync(address, functionIndex, isOn).ConfigureAwait(false))
        {
            return;
        }

        _commandQueue.Enqueue(new RuntimeCommandEnvelope
        {
            Type = RuntimeCommandType.SetLocomotiveFunction,
            LocomotiveAddress = address,
            FunctionIndex = functionIndex,
            FunctionIsOn = isOn
        });
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_hostRegistry.IsHost(Context.ConnectionId!))
        {
            _hostRegistry.ClearHost(Context.ConnectionId!);
            await BroadcastSessionStateAsync().ConfigureAwait(false);
        }

        _remoteRegistry.Unregister(Context.ConnectionId!);

        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }

    private async Task<bool> TryForwardSetSignalAspectAsync(string signalId, string aspect)
    {
        var hostId = _hostRegistry.HostConnectionId;
        if (string.IsNullOrEmpty(hostId))
        {
            return false;
        }

        await Clients.Client(hostId)
            .SendAsync(RuntimeHubMethods.ExecuteSetSignalAspect, signalId, aspect)
            .ConfigureAwait(false);
        return true;
    }

    private async Task<bool> TryForwardSetLocomotiveDriveAsync(int address, int speed, bool forward)
    {
        var hostId = _hostRegistry.HostConnectionId;
        if (string.IsNullOrEmpty(hostId))
        {
            return false;
        }

        await Clients.Client(hostId)
            .SendAsync(RuntimeHubMethods.ExecuteSetLocomotiveDrive, address, speed, forward)
            .ConfigureAwait(false);
        return true;
    }

    private async Task<bool> TryForwardSetLocomotiveFunctionAsync(int address, int functionIndex, bool isOn)
    {
        var hostId = _hostRegistry.HostConnectionId;
        if (string.IsNullOrEmpty(hostId))
        {
            return false;
        }

        await Clients.Client(hostId)
            .SendAsync(RuntimeHubMethods.ExecuteSetLocomotiveFunction, address, functionIndex, isOn)
            .ConfigureAwait(false);
        return true;
    }

    private void EnsureHost()
    {
        if (!_hostRegistry.IsHost(Context.ConnectionId!))
        {
            throw new HubException("Connection is not registered as runtime host.");
        }
    }

    private bool BuildSessionOperational(bool? isConnectedOverride = null)
    {
        var isConnected = isConnectedOverride
            ?? (_snapshotCache.TryGet(out var entry) && entry.IsConnected);
        return _hostRegistry.HasHost && isConnected;
    }

    private async Task BroadcastSessionStateAsync()
    {
        await Clients.Group("runtime-remote")
            .SendAsync(RuntimeHubMethods.SessionStateChanged, BuildSessionOperational())
            .ConfigureAwait(false);
    }

    private bool IsLocalhostConnection()
    {
        var remoteIp = Context.GetHttpContext()?.Connection.RemoteIpAddress;
        return remoteIp != null && IPAddress.IsLoopback(remoteIp);
    }

    private void EnsureRemoteAuthorized()
    {
        var httpContext = Context.GetHttpContext();
        if (httpContext != null
            && MobaApiAuth.IsLocalConnection(
                httpContext.Connection.RemoteIpAddress,
                httpContext.Connection.LocalIpAddress))
        {
            return;
        }

        if (httpContext?.Items[MobaApiAuth.AuthenticatedItemKey] is true)
        {
            return;
        }

        var configuredApiKey = MobaApiAuth.ReadConfiguredApiKey();
        if (!string.IsNullOrEmpty(configuredApiKey)
            && httpContext?.Request.Headers.TryGetValue(MobaApiAuth.ApiKeyHeaderName, out var headerValues) == true
            && MobaApiAuth.TryGetProvidedApiKey(headerValues.FirstOrDefault(), out var apiKey)
            && MobaApiAuth.KeysMatch(apiKey, configuredApiKey))
        {
            return;
        }

        throw new HubException("Invalid or missing MOBApi pairing key.");
    }
}
