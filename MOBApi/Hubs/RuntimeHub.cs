// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MOBApi.Hubs;

using Common.Runtime;

using Microsoft.AspNetCore.SignalR;

using Moba.MOBApi.Service;

/// <summary>
/// SignalR hub for MOBAflow runtime snapshots and remote control commands. Every project has its own runtime:
/// a remote registers for one project, receives only that project's snapshots and commands only that runtime.
/// </summary>
public sealed class RuntimeHub : Hub
{
    /// <summary>All remotes; solution updates go to every remote regardless of its project.</summary>
    public const string RuntimeRemoteGroup = "runtime-remote";

    private readonly IRuntimeSnapshotCache _snapshotCache;
    private readonly ISolutionCache _solutionCache;
    private readonly IRuntimeHostRegistry _hostRegistry;
    private readonly IRuntimeBroadcastMetrics _broadcastMetrics;
    private readonly IRuntimeCommandAdmission _commandAdmission;
    private readonly IRuntimeRemoteRegistry _remoteRegistry;

    public RuntimeHub(
        IRuntimeSnapshotCache snapshotCache,
        ISolutionCache solutionCache,
        IRuntimeHostRegistry hostRegistry,
        IRuntimeBroadcastMetrics broadcastMetrics,
        IRuntimeCommandAdmission commandAdmission,
        IRuntimeRemoteRegistry remoteRegistry)
    {
        _snapshotCache = snapshotCache;
        _solutionCache = solutionCache;
        _hostRegistry = hostRegistry;
        _broadcastMetrics = broadcastMetrics;
        _commandAdmission = commandAdmission;
        _remoteRegistry = remoteRegistry;
    }

    /// <summary>The group of remotes that show and control one project.</summary>
    public static string ProjectGroup(Guid projectId) => $"runtime-remote:{projectId:N}";

    /// <summary>
    /// Registers the MOBAflow connection that owns the project runtimes.
    /// </summary>
    public async Task RegisterHost()
    {
        _hostRegistry.SetHost(Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, "runtime-host").ConfigureAwait(false);
        foreach (var entry in _snapshotCache.GetAll())
        {
            await BroadcastSessionStateAsync(entry.ProjectId).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Registers a remote for one project; registering again moves the remote to another project.
    /// </summary>
    public async Task RegisterRemote(string clientId, string projectId)
    {
        var presenceId = clientId?.Trim();
        if (string.IsNullOrWhiteSpace(presenceId))
        {
            throw new HubException("ClientId is required.");
        }

        var project = ParseProject(projectId);
        var previous = _remoteRegistry.Register(Context.ConnectionId, presenceId, project);
        if (previous is { } previousProject && previousProject != project)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, ProjectGroup(previousProject)).ConfigureAwait(false);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, RuntimeRemoteGroup).ConfigureAwait(false);
        await Groups.AddToGroupAsync(Context.ConnectionId, ProjectGroup(project)).ConfigureAwait(false);

        if (_snapshotCache.TryGet(project, out var entry))
        {
            await Clients.Caller.SendAsync(RuntimeHubMethods.SnapshotUpdated, entry.Json).ConfigureAwait(false);
        }

        if (_solutionCache.TryGet(out var solutionEntry) && !string.IsNullOrWhiteSpace(solutionEntry.SourcePath))
        {
            await Clients.Caller
                .SendAsync(RuntimeHubMethods.SolutionUpdated, solutionEntry.UpdatedAt.ToString("O"))
                .ConfigureAwait(false);
        }

        await Clients.Caller.SendAsync(RuntimeHubMethods.SessionStateChanged, BuildSessionOperational(project)).ConfigureAwait(false);
    }

    /// <summary>
    /// Stores the snapshot of one project's runtime and sends it to the remotes of that project.
    /// </summary>
    public async Task PushSnapshot(string snapshotJson)
    {
        EnsureHost();
        RuntimeSnapshotCacheEntry entry;
        try
        {
            entry = _snapshotCache.Set(snapshotJson);
        }
        catch (ArgumentException ex)
        {
            throw new HubException(ex.Message);
        }

        var group = Clients.Group(ProjectGroup(entry.ProjectId));
        await group.SendAsync(RuntimeHubMethods.SnapshotUpdated, entry.Json).ConfigureAwait(false);
        await group.SendAsync(RuntimeHubMethods.SessionStateChanged, BuildSessionOperational(entry.ProjectId)).ConfigureAwait(false);
        _broadcastMetrics.RecordSnapshotBroadcast(System.Text.Encoding.UTF8.GetByteCount(entry.Json));
    }

    public async Task SetSignalAspect(string projectId, string signalId, string aspect)
    {
        var project = ParseProject(projectId);
        if (!Guid.TryParse(signalId, out var parsedSignalId))
        {
            throw new HubException("Invalid signal id.");
        }

        if (!Enum.TryParse<Domain.SignalAspect>(aspect, out var parsedAspect))
        {
            throw new HubException("Invalid signal aspect.");
        }

        await AdmitAsync(
                new RuntimeCommandEnvelope
                {
                    ProjectId = project,
                    Type = RuntimeCommandType.SetSignalAspect,
                    SignalId = parsedSignalId,
                    SignalAspect = parsedAspect
                },
                RuntimeHubMethods.ExecuteSetSignalAspect,
                [project.ToString(), parsedSignalId.ToString(), parsedAspect.ToString()])
            .ConfigureAwait(false);
    }

    public async Task SetLocomotiveDrive(string projectId, int address, int speed, bool forward)
    {
        var project = ParseProject(projectId);
        await AdmitAsync(
                new RuntimeCommandEnvelope
                {
                    ProjectId = project,
                    Type = RuntimeCommandType.SetLocomotiveDrive,
                    LocomotiveAddress = address,
                    Speed = speed,
                    Forward = forward
                },
                RuntimeHubMethods.ExecuteSetLocomotiveDrive,
                [project.ToString(), address, speed, forward])
            .ConfigureAwait(false);
    }

    public async Task SetLocomotiveFunction(string projectId, int address, int functionIndex, bool isOn)
    {
        var project = ParseProject(projectId);
        await AdmitAsync(
                new RuntimeCommandEnvelope
                {
                    ProjectId = project,
                    Type = RuntimeCommandType.SetLocomotiveFunction,
                    LocomotiveAddress = address,
                    FunctionIndex = functionIndex,
                    FunctionIsOn = isOn
                },
                RuntimeHubMethods.ExecuteSetLocomotiveFunction,
                [project.ToString(), address, functionIndex, isOn])
            .ConfigureAwait(false);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_hostRegistry.IsHost(Context.ConnectionId))
        {
            _hostRegistry.ClearHost(Context.ConnectionId);
            foreach (var entry in _snapshotCache.GetAll())
            {
                await BroadcastSessionStateAsync(entry.ProjectId).ConfigureAwait(false);
            }
        }

        _remoteRegistry.Unregister(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }

    private static Guid ParseProject(string? projectId) =>
        Guid.TryParse(projectId, out var parsed) && parsed != Guid.Empty
            ? parsed
            : throw new HubException("ProjectId is required.");

    /// <summary>
    /// Validates a command and forwards it to MOBAflow; without a connected host the command waits in the queue.
    /// </summary>
    private async Task AdmitAsync(RuntimeCommandEnvelope command, string hostMethod, object?[] hostArguments)
    {
        var validation = _commandAdmission.Validate(command);
        if (!validation.IsAccepted)
        {
            throw new HubException(validation.Error);
        }

        var hostId = _hostRegistry.HostConnectionId;
        if (!string.IsNullOrEmpty(hostId))
        {
            await Clients.Client(hostId).SendCoreAsync(hostMethod, hostArguments).ConfigureAwait(false);
            return;
        }

        var queued = _commandAdmission.Enqueue(command);
        if (!queued.IsAccepted)
        {
            throw new HubException(queued.Status == RuntimeCommandAdmissionStatus.QueueFull
                ? "Command queue is full."
                : queued.Error);
        }
    }

    private void EnsureHost()
    {
        if (!_hostRegistry.IsHost(Context.ConnectionId!))
        {
            throw new HubException("Connection is not registered as runtime host.");
        }
    }

    private bool BuildSessionOperational(Guid projectId) =>
        _hostRegistry.HasHost && _snapshotCache.TryGet(projectId, out var entry) && entry.IsConnected;

    private async Task BroadcastSessionStateAsync(Guid projectId)
    {
        await Clients.Group(ProjectGroup(projectId))
            .SendAsync(RuntimeHubMethods.SessionStateChanged, BuildSessionOperational(projectId))
            .ConfigureAwait(false);
    }
}
