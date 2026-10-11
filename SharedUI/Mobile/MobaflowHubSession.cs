// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Mobile;

using Common.Configuration;

using Interface;

using Microsoft.Extensions.Logging;

/// <summary>
/// Result of <see cref="MobaflowHubSession.EnsureConnectedAsync"/>; a null flag leaves the shown state unchanged.
/// </summary>
/// <param name="IsConnected">Whether the runtime hub session is usable after the call.</param>
/// <param name="HubConnected">The runtime hub connection state to show, or null to keep it.</param>
/// <param name="RemoteZ21Connected">Whether MOBAflow reports an active Z21, or null to keep it.</param>
public sealed record HubConnectionOutcome(bool IsConnected, bool? HubConnected, bool? RemoteZ21Connected)
{
    /// <summary>Nothing changed, for example without a hub client or after cancellation.</summary>
    public static HubConnectionOutcome Unchanged { get; } = new(false, null, null);

    /// <summary>The session is off: no hub and no remote Z21.</summary>
    public static HubConnectionOutcome Disconnected { get; } = new(false, false, false);

    /// <summary>Every connect attempt failed; the last remote Z21 state stays.</summary>
    public static HubConnectionOutcome ConnectFailed { get; } = new(false, false, null);

    /// <summary>The hub is connected; <paramref name="hasActiveHost"/> tells whether MOBAflow runs the Z21.</summary>
    public static HubConnectionOutcome Connected(bool hasActiveHost) => new(true, true, hasActiveHost);
}

/// <summary>
/// The MOBAsmart session with MOBApi: REST health check, client registration for MOBAflow's overview and the
/// SignalR runtime hub connection with retries and forced reconnect. It returns results and leaves UI state to
/// the caller.
/// </summary>
public sealed partial class MobaflowHubSession
{
    private static readonly TimeSpan ConnectHealthCheckTimeout = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan ClientRegistrationInterval = TimeSpan.FromSeconds(120);

    private static readonly TimeSpan[] ConnectRetryDelays =
    [
        TimeSpan.Zero,
        TimeSpan.FromMilliseconds(500),
        TimeSpan.FromMilliseconds(1500),
        TimeSpan.FromMilliseconds(3000),
    ];

    private readonly AppSettings _settings;
    private readonly MobaflowEndpointLocator _endpointLocator;
    private readonly IPhotoUploadService _healthService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MobaflowHubSession> _logger;
    private readonly IRuntimeHubRemoteClient? _hubClient;
    private readonly IRestApiClientRegistration? _clientRegistration;
    private DateTimeOffset? _lastClientRegistration;

    /// <summary>
    /// Initializes a new instance of the <see cref="MobaflowHubSession"/> class.
    /// </summary>
    /// <param name="settings">Application settings with the MOBAflow connection switch.</param>
    /// <param name="endpointLocator">Provides the stored MOBApi endpoint.</param>
    /// <param name="healthService">Checks whether MOBApi answers on an endpoint.</param>
    /// <param name="timeProvider">Clock for the registration cadence and connect retries.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="hubClient">Optional: SignalR client for the runtime hub.</param>
    /// <param name="clientRegistration">Optional: registers the phone in MOBAflow's client overview.</param>
    public MobaflowHubSession(
        AppSettings settings,
        MobaflowEndpointLocator endpointLocator,
        IPhotoUploadService healthService,
        TimeProvider timeProvider,
        ILogger<MobaflowHubSession> logger,
        IRuntimeHubRemoteClient? hubClient = null,
        IRestApiClientRegistration? clientRegistration = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(endpointLocator);
        ArgumentNullException.ThrowIfNull(healthService);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);
        _settings = settings;
        _endpointLocator = endpointLocator;
        _healthService = healthService;
        _timeProvider = timeProvider;
        _logger = logger;
        _hubClient = hubClient;
        _clientRegistration = clientRegistration;
    }

    /// <summary>Gets whether the runtime hub is connected.</summary>
    public bool IsConnected => _hubClient?.IsConnected ?? false;

    /// <summary>
    /// Checks whether MOBApi answers; while <paramref name="connecting"/>, a short timeout keeps the toggle responsive.
    /// </summary>
    public Task<bool> CheckHealthAsync(MobaflowEndpoint endpoint, bool connecting)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        return _healthService.HealthCheckAsync(endpoint.IpAddress, endpoint.Port, connecting ? ConnectHealthCheckTimeout : null);
    }

    /// <summary>
    /// Registers the phone on the first reachable check and then every two minutes, so MOBAflow keeps listing it.
    /// </summary>
    public Task RegisterClientWhenDueAsync(MobaflowEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        if (_clientRegistration is null)
        {
            return Task.CompletedTask;
        }

        var now = _timeProvider.GetUtcNow();
        if (_lastClientRegistration is { } last && now - last < ClientRegistrationInterval)
        {
            return Task.CompletedTask;
        }

        _lastClientRegistration = now;
        return _clientRegistration.RegisterAsync(endpoint.IpAddress, endpoint.Port);
    }

    /// <summary>
    /// Keeps the runtime hub connected to the stored endpoint. A connected session survives a failed REST check;
    /// <paramref name="forceReconnect"/> rebuilds it. Each connect attempt first runs
    /// <paramref name="beforeConnectAttempt"/>, which selects the phone's project.
    /// </summary>
    public async Task<HubConnectionOutcome> EnsureConnectedAsync(
        bool reachable,
        bool forceReconnect,
        Func<Task> beforeConnectAttempt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(beforeConnectAttempt);
        if (_hubClient is null)
        {
            return HubConnectionOutcome.Unchanged;
        }

        if (!_settings.RestApi.IsConnectionEnabled)
        {
            await DisconnectAsync().ConfigureAwait(false);
            return HubConnectionOutcome.Disconnected;
        }

        if (_endpointLocator.StoredEndpoint is not { } endpoint)
        {
            return HubConnectionOutcome.Disconnected;
        }

        if (!reachable)
        {
            // Keep the SignalR session when REST health flickers; SignalR reconnect handles transport gaps.
            return _hubClient.IsConnected
                ? HubConnectionOutcome.Connected(_hubClient.HasActiveHost)
                : HubConnectionOutcome.Unchanged;
        }

        if (_hubClient.IsConnected && !forceReconnect)
        {
            return HubConnectionOutcome.Connected(_hubClient.HasActiveHost);
        }

        if (forceReconnect)
        {
            await DisconnectAsync().ConfigureAwait(false);
        }

        var clientId = _clientRegistration?.ClientId;
        if (string.IsNullOrWhiteSpace(clientId))
        {
            clientId = Guid.NewGuid().ToString("N");
        }

        foreach (var delay in ConnectRetryDelays)
        {
            if (delay > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(delay, _timeProvider, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return HubConnectionOutcome.Unchanged;
                }
            }

            if (!_settings.RestApi.IsConnectionEnabled)
            {
                return HubConnectionOutcome.Unchanged;
            }

            try
            {
                await beforeConnectAttempt().ConfigureAwait(false);
                await _hubClient
                    .ConnectAsync(endpoint.IpAddress, endpoint.Port, clientId, cancellationToken, forceReconnect)
                    .ConfigureAwait(false);
                return HubConnectionOutcome.Connected(_hubClient.HasActiveHost);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogConnectAttemptFailed(_logger, delay.TotalMilliseconds, ex);
            }
        }

        return HubConnectionOutcome.ConnectFailed;
    }

    /// <summary>
    /// Closes the runtime hub connection if it is open; a failing disconnect is logged and ignored.
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (_hubClient?.IsConnected != true)
        {
            return;
        }

        try
        {
            await _hubClient.DisconnectAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogDisconnectFailed(_logger, ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Runtime hub connect attempt failed (delay {DelayMs} ms)")]
    private static partial void LogConnectAttemptFailed(ILogger logger, double delayMs, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Runtime hub disconnect failed")]
    private static partial void LogDisconnectFailed(ILogger logger, Exception exception);
}
