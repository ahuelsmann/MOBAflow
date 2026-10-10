// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using Common.Discovery;
using Backend.Interface;

using Common.Runtime;

using Domain;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

using SharedUI.Interface;

/// <summary>
/// SignalR host client: pushes runtime snapshots and executes remote commands on the local runtime.
/// </summary>
public sealed class RuntimeHubHostClient : IRuntimeHubHostClient
{
    private readonly ProjectRuntimeCommandRouter _router;
    private readonly ILogger<RuntimeHubHostClient>? _logger;
    private HubConnection? _hubConnection;

    public RuntimeHubHostClient(
        ProjectRuntimeCommandRouter router,
        ILogger<RuntimeHubHostClient>? logger = null)
    {
        _router = router ?? throw new ArgumentNullException(nameof(router));
        _logger = logger;
    }

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public async Task ConnectAsync(string serverIp, int serverPort, CancellationToken cancellationToken = default)
    {
        var hubUrl = new MobApiEndpoint(serverIp, serverPort).Resolve("runtime-hub");
        _logger?.LogInformation("Connecting to RuntimeHub: {HubUrl}", hubUrl);

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect(
            [
                TimeSpan.Zero,
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10)
            ])
            .Build();

        _hubConnection.On<string, string, string>(RuntimeHubMethods.ExecuteSetSignalAspect, OnExecuteSetSignalAspectAsync);
        _hubConnection.On<string, int, int, bool>(RuntimeHubMethods.ExecuteSetLocomotiveDrive, OnExecuteSetLocomotiveDriveAsync);
        _hubConnection.On<string, int, int, bool>(RuntimeHubMethods.ExecuteSetLocomotiveFunction, OnExecuteSetLocomotiveFunctionAsync);
        _hubConnection.Reconnected += OnReconnectedAsync;

        await _hubConnection.StartAsync(cancellationToken).ConfigureAwait(false);
        await RegisterHostAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DisconnectAsync()
    {
        if (_hubConnection == null)
        {
            return;
        }

        try
        {
            await _hubConnection.StopAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "RuntimeHub disconnect failed");
        }
    }

    public async Task PushSnapshotAsync(MobaRuntimeSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        if (_hubConnection == null || !IsConnected)
        {
            return;
        }

        var json = RuntimeJsonSerializer.Serialize(snapshot);
        await _hubConnection.InvokeAsync(RuntimeHubMethods.PushSnapshot, json, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await DisconnectAsync().ConfigureAwait(false);
            await _hubConnection.DisposeAsync().ConfigureAwait(false);
            _hubConnection = null;
        }

        GC.SuppressFinalize(this);
    }

    private async Task RegisterHostAsync(CancellationToken cancellationToken)
    {
        if (_hubConnection == null || !IsConnected)
        {
            return;
        }

        await _hubConnection.InvokeAsync(RuntimeHubMethods.RegisterHost, cancellationToken).ConfigureAwait(false);
        _logger?.LogInformation("RuntimeHub host registered");
    }

    private async Task OnReconnectedAsync(string? _)
    {
        _logger?.LogInformation("RuntimeHub host reconnected, re-registering");
        try
        {
            await RegisterHostAsync(CancellationToken.None).ConfigureAwait(false);
            foreach (var snapshot in _router.Snapshots)
            {
                await PushSnapshotAsync(
                    RuntimeSnapshotRemoteFilter.ForMobasmartBroadcast(snapshot),
                    CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "RuntimeHub host re-register failed after reconnect");
        }
    }

    private async Task OnExecuteSetSignalAspectAsync(string projectId, string signalId, string aspect)
    {
        if (!Guid.TryParse(signalId, out var id) || !Enum.TryParse<SignalAspect>(aspect, out var parsedAspect))
        {
            return;
        }

        if (ForProject(projectId) is { } gateway)
        {
            await gateway.SetSignalAspectAsync(id, parsedAspect).ConfigureAwait(false);
        }
    }

    private async Task OnExecuteSetLocomotiveDriveAsync(string projectId, int address, int speed, bool forward)
    {
        if (ForProject(projectId) is { } gateway)
        {
            await gateway.SetLocomotiveDriveAsync(address, speed, forward).ConfigureAwait(false);
        }
    }

    private async Task OnExecuteSetLocomotiveFunctionAsync(string projectId, int address, int functionIndex, bool isOn)
    {
        if (ForProject(projectId) is { } gateway)
        {
            await gateway.SetLocomotiveFunctionAsync(address, functionIndex, isOn).ConfigureAwait(false);
        }
    }

    /// <summary>Every project has its own runtime; a command for a project that is not loaded is dropped.</summary>
    private IRuntimeCommandGateway? ForProject(string projectId) =>
        Guid.TryParse(projectId, out var id) ? _router.ForProject(id) : null;
}
