// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MOBApi.Hubs;

using Common.Runtime;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

using Moba.MOBApi.Security;
using Moba.MOBApi.Service;

using System.Net;

/// <summary>
/// SignalR hub for MOBAflow runtime snapshots and remote control commands.
/// </summary>
[Authorize(Policy = ControlPlaneCapabilities.Read)]
public sealed class RuntimeHub : Hub
{
    private const string RuntimeRemoteGroup = "runtime-remote";
    private readonly IRuntimeSnapshotCache _snapshotCache;
    private readonly ISolutionCache _solutionCache;
    private readonly IRuntimeHostRegistry _hostRegistry;
    private readonly IRuntimeBroadcastMetrics _broadcastMetrics;
    private readonly IRuntimeCommandQueue _commandQueue;
    private readonly IControlPlaneHubConnectionRegistry _connectionRegistry;
    private readonly ICompatibilityReadMigration _readMigration;
    private readonly IHostCredentialService? _hostCredentialService;

    public RuntimeHub(
        IRuntimeSnapshotCache snapshotCache,
        ISolutionCache solutionCache,
        IRuntimeHostRegistry hostRegistry,
        IRuntimeBroadcastMetrics broadcastMetrics,
        IRuntimeCommandQueue commandQueue,
        RuntimeHubSecurityServices securityServices,
        IHostCredentialService? hostCredentialService = null)
    {
        ArgumentNullException.ThrowIfNull(securityServices);
        _snapshotCache = snapshotCache;
        _solutionCache = solutionCache;
        _hostRegistry = hostRegistry;
        _broadcastMetrics = broadcastMetrics;
        _commandQueue = commandQueue;
        _connectionRegistry = securityServices.ConnectionRegistry;
        _readMigration = securityServices.ReadMigration;
        _hostCredentialService = hostCredentialService;
    }

    public override Task OnConnectedAsync()
    {
        _connectionRegistry.RegisterAuthenticated(Context);
        return base.OnConnectedAsync();
    }

    [Authorize(Policy = ControlPlaneCapabilities.HostConsume)]
    public async Task RegisterHost()
    {
        if (!IsLocalhostConnection())
        {
            throw new HubException("Only localhost connections may register as runtime host.");
        }

        _hostRegistry.SetHost(Context.ConnectionId);
        _hostCredentialService?.ConfirmHostConnection();
        await Groups.AddToGroupAsync(Context.ConnectionId, "runtime-host").ConfigureAwait(false);
        await BroadcastSessionStateAsync().ConfigureAwait(false);
    }

    public async Task RegisterRemote(string clientId)
    {
        var credentialId = Context.UserIdentifier;
        var isAnonymousCompatibility = string.IsNullOrWhiteSpace(credentialId);
        var presenceId = string.IsNullOrWhiteSpace(credentialId) ? clientId?.Trim() : credentialId;
        if (string.IsNullOrWhiteSpace(presenceId))
        {
            throw new HubException("ClientId is required for a compatibility read connection.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, RuntimeRemoteGroup).ConfigureAwait(false);
        _connectionRegistry.RegisterRemote(Context, presenceId, isAnonymousCompatibility);

        if (isAnonymousCompatibility &&
            await _readMigration.EvaluateAnonymousReadAsync(
                    CompatibilityReadTransport.SignalR,
                    Context.ConnectionAborted)
                .ConfigureAwait(false) == CompatibilityReadDecision.UpgradeRequired)
        {
            _connectionRegistry.Unregister(Context);
            throw new HubException("A current authenticated client is required for read access.");
        }

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

        if (!string.IsNullOrWhiteSpace(credentialId))
        {
            await _readMigration.RecordAuthenticatedReadAsync(
                    credentialId,
                    CompatibilityReadTransport.SignalR,
                    Context.GetHttpContext()?.Request.Headers[CompatibilityReadHeaders.ClientRelease].FirstOrDefault(),
                    CancellationToken.None)
                .ConfigureAwait(false);
        }
    }

    [Authorize(Policy = ControlPlaneCapabilities.HostPublish)]
    public async Task PushSnapshot(string snapshotJson)
    {
        EnsureHost();

        var snapshot = RuntimeJsonSerializer.Deserialize(snapshotJson)
            ?? throw new HubException("Invalid runtime snapshot payload.");

        _snapshotCache.Set(snapshotJson, snapshot.IsConnected);
        var broadcastJson = _snapshotCache.TryGet(out var cachedEntry) ? cachedEntry.Json : snapshotJson;
        await Clients.Group(RuntimeRemoteGroup).SendAsync(RuntimeHubMethods.SnapshotUpdated, broadcastJson).ConfigureAwait(false);
        await Clients.Group(RuntimeRemoteGroup).SendAsync(RuntimeHubMethods.SessionStateChanged, BuildSessionOperational(snapshot.IsConnected)).ConfigureAwait(false);
        _broadcastMetrics.RecordSnapshotBroadcast(System.Text.Encoding.UTF8.GetByteCount(broadcastJson));
    }

    [Authorize(Policy = ControlPlaneCapabilities.RuntimeControl)]
    public async Task SetSignalAspect(string signalId, string aspect)
    {
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

    [Authorize(Policy = ControlPlaneCapabilities.RuntimeControl)]
    public async Task SetLocomotiveDrive(int address, int speed, bool forward)
    {
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

    [Authorize(Policy = ControlPlaneCapabilities.RuntimeControl)]
    public async Task SetLocomotiveFunction(int address, int functionIndex, bool isOn)
    {
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
        if (_hostRegistry.IsHost(Context.ConnectionId))
        {
            _hostRegistry.ClearHost(Context.ConnectionId);
            _hostCredentialService?.BeginDisconnectGrace();
            await BroadcastSessionStateAsync().ConfigureAwait(false);
        }

        _connectionRegistry.Unregister(Context);

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
        await Clients.Group(RuntimeRemoteGroup)
            .SendAsync(RuntimeHubMethods.SessionStateChanged, BuildSessionOperational())
            .ConfigureAwait(false);
    }

    private bool IsLocalhostConnection()
    {
        var remoteIp = Context.GetHttpContext()?.Connection.RemoteIpAddress;
        return remoteIp != null && IPAddress.IsLoopback(remoteIp);
    }
}
