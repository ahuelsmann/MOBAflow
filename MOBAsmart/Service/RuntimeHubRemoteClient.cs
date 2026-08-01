// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MAUI.Service;

using Common.Configuration;
using Common.Runtime;
using Common.Security;

using Domain;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

using SharedUI.Interface;

using System.Text;
using System.Text.Json;

/// <summary>
/// SignalR remote client for MOBApi runtime hub.
/// </summary>
public sealed class RuntimeHubRemoteClient : IRuntimeHubRemoteClient
{
    private readonly IRemoteControlAuthenticatedHttpClient _authenticatedHttpClient;
    private readonly IRemoteControlHttpClientFactory _authenticatedHttpClientFactory;
    private readonly AppSettings _appSettings;
    private readonly ILogger<RuntimeHubRemoteClient>? _logger;
    private readonly RemoteControlSessionService _sessionService;
    private HubConnection? _hubConnection;
    private bool _hasActiveHost;
    private string _serverIp = string.Empty;
    private int _serverPort;
    private string _clientId = string.Empty;

    public RuntimeHubRemoteClient(
        AppSettings appSettings,
        RemoteControlSessionService sessionService,
        IRemoteControlAuthenticatedHttpClient authenticatedHttpClient,
        IRemoteControlHttpClientFactory authenticatedHttpClientFactory,
        ILogger<RuntimeHubRemoteClient>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(appSettings);
        ArgumentNullException.ThrowIfNull(sessionService);
        ArgumentNullException.ThrowIfNull(authenticatedHttpClient);
        ArgumentNullException.ThrowIfNull(authenticatedHttpClientFactory);
        _appSettings = appSettings;
        _sessionService = sessionService;
        _authenticatedHttpClient = authenticatedHttpClient;
        _authenticatedHttpClientFactory = authenticatedHttpClientFactory;
        _logger = logger;
    }

    public event Func<MobaRuntimeSnapshot, Task>? SnapshotReceived;

    public event Func<bool, Task>? SessionStateChanged;

    public event Func<DateTimeOffset, Task>? SolutionUpdated;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public bool HasActiveHost => _hasActiveHost;

    public async Task ConnectAsync(
        string serverIp,
        int serverPort,
        string clientId,
        CancellationToken cancellationToken = default,
        bool forceReconnect = false)
    {
        _clientId = clientId;

        var connection = await _sessionService
            .GetConnectionSessionAsync(TimeSpan.FromSeconds(30), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new RemoteCredentialRejectedException();

        if (!forceReconnect
            && _hubConnection != null
            && IsConnected
            && string.Equals(_serverIp, serverIp, StringComparison.OrdinalIgnoreCase)
            && _serverPort == serverPort)
        {
            return;
        }

        if (_hubConnection != null)
        {
            try
            {
                await _hubConnection.StopAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "RuntimeHub remote stop before reconnect failed");
            }

            await _hubConnection.DisposeAsync().ConfigureAwait(false);
            _hubConnection = null;
        }

        var httpsPort = connection.Endpoint.HttpsPort
            ?? throw new InvalidOperationException("The authenticated MOBApi endpoint requires an HTTPS port.");
        var hubUrl = new Uri(
            new UriBuilder(
                Uri.UriSchemeHttps,
                connection.Endpoint.IpAddress,
                httpsPort).Uri,
            "runtime-hub");
        _logger?.LogInformation("Connecting to RuntimeHub remote: {HubUrl}", hubUrl);

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = GetAccessTokenAsync;
                options.Headers[PinnedRemoteControlTransport.ClientReleaseHeaderName] =
                    PinnedRemoteControlTransport.ClientRelease;
                options.HttpMessageHandlerFactory = _ =>
                    _authenticatedHttpClientFactory.CreateHandler(connection.Endpoint);
                options.WebSocketConfiguration = webSocketOptions =>
                {
                    webSocketOptions.RemoteCertificateValidationCallback = (_, certificate, _, _) =>
                        _authenticatedHttpClientFactory.ValidateServerCertificate(connection.Endpoint, certificate);
                };
            })
            .WithAutomaticReconnect(
            [
                TimeSpan.Zero,
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10)
            ])
            .Build();

        _hubConnection.On<string>(RuntimeHubMethods.SnapshotUpdated, OnSnapshotUpdatedAsync);
        _hubConnection.On<bool>(RuntimeHubMethods.SessionStateChanged, OnSessionStateChangedAsync);
        _hubConnection.On<string>(RuntimeHubMethods.SolutionUpdated, OnSolutionUpdatedAsync);
        _hubConnection.Reconnected += OnReconnectedAsync;
        _hubConnection.Closed += OnClosedAsync;

        await _hubConnection.StartAsync(cancellationToken).ConfigureAwait(false);
        _serverIp = serverIp;
        _serverPort = serverPort;
        await RegisterRemoteAsync(cancellationToken).ConfigureAwait(false);
        await TryFetchInitialSnapshotAsync(cancellationToken).ConfigureAwait(false);
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
            _logger?.LogDebug(ex, "RuntimeHub remote disconnect failed");
        }
    }

    public Task RequestLatestSnapshotAsync(CancellationToken cancellationToken = default) =>
        TryFetchInitialSnapshotAsync(cancellationToken);

    public async Task SetSignalAspectAsync(Guid signalId, SignalAspect aspect, CancellationToken cancellationToken = default)
    {
        if (_hubConnection != null && IsConnected)
        {
            try
            {
                await _hubConnection
                    .InvokeAsync(RuntimeHubMethods.SetSignalAspect, signalId.ToString(), aspect.ToString(), cancellationToken)
                    .ConfigureAwait(false);
                return;
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "Signal aspect hub invoke failed, trying REST fallback");
            }
        }

        await PostRestFallbackAsync(
            "signal-aspect",
            new { signalId, aspect },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task SetLocomotiveDriveAsync(int address, int speed, bool forward, CancellationToken cancellationToken = default)
    {
        if (_hubConnection != null && IsConnected)
        {
            try
            {
                await _hubConnection
                    .InvokeAsync(RuntimeHubMethods.SetLocomotiveDrive, address, speed, forward, cancellationToken)
                    .ConfigureAwait(false);
                return;
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "Drive hub invoke failed, trying REST fallback");
            }
        }

        await PostRestFallbackAsync(
            "locomotive/drive",
            new { address, speed, forward },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task SetLocomotiveFunctionAsync(int address, int functionIndex, bool isOn, CancellationToken cancellationToken = default)
    {
        if (_hubConnection != null && IsConnected)
        {
            try
            {
                await _hubConnection
                    .InvokeAsync(RuntimeHubMethods.SetLocomotiveFunction, address, functionIndex, isOn, cancellationToken)
                    .ConfigureAwait(false);
                return;
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "Function hub invoke failed, trying REST fallback");
            }
        }

        await PostRestFallbackAsync(
            "locomotive/function",
            new { address, functionIndex, isOn },
            cancellationToken).ConfigureAwait(false);
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

    private async Task PostRestFallbackAsync(string relativePath, object body, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(body);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _authenticatedHttpClient
            .PostAsync($"api/runtime/commands/{relativePath}", content, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    private async Task OnSnapshotUpdatedAsync(string snapshotJson)
    {
        var snapshot = RuntimeJsonSerializer.Deserialize(snapshotJson);
        if (snapshot == null)
        {
            return;
        }

        if (SnapshotReceived != null)
        {
            await SnapshotReceived.Invoke(snapshot).ConfigureAwait(false);
        }
    }

    private async Task OnSessionStateChangedAsync(bool isOperational)
    {
        _hasActiveHost = isOperational;
        if (SessionStateChanged != null)
        {
            await SessionStateChanged.Invoke(isOperational).ConfigureAwait(false);
        }
    }

    private async Task OnSolutionUpdatedAsync(string updatedAtIso)
    {
        if (!DateTimeOffset.TryParse(updatedAtIso, out var updatedAt))
        {
            return;
        }

        if (SolutionUpdated != null)
        {
            await SolutionUpdated.Invoke(updatedAt).ConfigureAwait(false);
        }
    }

    private async Task RegisterRemoteAsync(CancellationToken cancellationToken)
    {
        if (_hubConnection == null || !IsConnected || string.IsNullOrWhiteSpace(_clientId))
        {
            return;
        }

        await _hubConnection
            .InvokeAsync(RuntimeHubMethods.RegisterRemote, _clientId, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task OnReconnectedAsync(string? _)
    {
        _logger?.LogInformation("RuntimeHub remote reconnected, re-registering client {ClientId}", _clientId);
        try
        {
            await RegisterRemoteAsync(CancellationToken.None).ConfigureAwait(false);
            await TryFetchInitialSnapshotAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "RuntimeHub remote re-register failed after reconnect");
        }
    }

    private async Task OnClosedAsync(Exception? ex)
    {
        _hasActiveHost = false;
        if (ex != null)
        {
            _logger?.LogDebug(ex, "RuntimeHub remote connection closed");
        }

        if (SessionStateChanged != null)
        {
            await SessionStateChanged.Invoke(false).ConfigureAwait(false);
        }
    }

    private async Task TryFetchInitialSnapshotAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_serverIp) || _serverPort <= 0)
        {
            return;
        }

        try
        {
            using var response = await _authenticatedHttpClient
                .GetAsync("api/runtime/snapshot", cancellationToken)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var snapshot = RuntimeJsonSerializer.Deserialize(json);
            if (snapshot == null)
            {
                return;
            }

            if (SnapshotReceived != null)
            {
                await SnapshotReceived.Invoke(snapshot).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Runtime REST snapshot fetch failed");
        }
    }

    private async Task<string?> GetAccessTokenAsync()
    {
        var connection = await _sessionService
            .GetConnectionSessionAsync(TimeSpan.FromSeconds(30))
            .ConfigureAwait(false)
            ?? throw new RemoteCredentialRejectedException();
        return connection.AccessSession.AccessToken;
    }
}