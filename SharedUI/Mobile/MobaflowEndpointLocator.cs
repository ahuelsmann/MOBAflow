// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Mobile;

using Common.Configuration;

using Interface;

using Microsoft.Extensions.Logging;

/// <summary>
/// A MOBApi endpoint on the local network.
/// </summary>
/// <param name="IpAddress">The trimmed IP address.</param>
/// <param name="Port">The HTTP port.</param>
public sealed record MobaflowEndpoint(string IpAddress, int Port);

/// <summary>
/// Finds and remembers the MOBApi endpoint MOBAsmart connects to: LAN discovery anchored on the subnet of the selected
/// project's Z21, the rediscovery cadence and the recent-address history in the app settings.
/// </summary>
public sealed partial class MobaflowEndpointLocator
{
    /// <summary>Discovery runs more often while MOBAflow and MOBAsmart may still be starting together.</summary>
    private static readonly TimeSpan StartupWindow = TimeSpan.FromSeconds(90);

    private static readonly TimeSpan StartupRediscoverInterval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan RediscoverInterval = TimeSpan.FromSeconds(25);
    private static readonly TimeSpan HealthyCheckDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan UnhealthyCheckDelay = TimeSpan.FromSeconds(15);

    /// <summary>Each pass runs multicast and a LAN HTTP subnet probe, which is expensive on Wi-Fi.</summary>
    private static readonly TimeSpan[] StartupRetryDelays = [TimeSpan.Zero, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15)];

    private readonly AppSettings _settings;
    private readonly ISettingsService _settingsService;
    private readonly IRestDiscoveryService _discovery;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MobaflowEndpointLocator> _logger;
    private readonly DateTimeOffset _startedAt;
    private readonly Lock _discoveryClockLock = new();
    private DateTimeOffset? _lastDiscoveryAt;

    public MobaflowEndpointLocator(
        AppSettings settings,
        ISettingsService settingsService,
        IRestDiscoveryService discovery,
        TimeProvider timeProvider,
        ILogger<MobaflowEndpointLocator> logger)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _discovery = discovery ?? throw new ArgumentNullException(nameof(discovery));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _startedAt = _timeProvider.GetUtcNow();
    }

    /// <summary>
    /// Gets the endpoint saved in the app settings; null when none is known.
    /// </summary>
    public MobaflowEndpoint? StoredEndpoint
    {
        get
        {
            var address = _settings.RestApi.CurrentIpAddress?.Trim();
            return string.IsNullOrWhiteSpace(address) || _settings.RestApi.Port <= 0
                ? null
                : new MobaflowEndpoint(address, _settings.RestApi.Port);
        }
    }

    /// <summary>
    /// Runs the fast discovery (UDP and recent addresses) and, when requested and nothing was found, the full subnet
    /// scan. A found endpoint is remembered. Failures other than cancellation are logged and return null.
    /// </summary>
    public async Task<MobaflowEndpoint?> DiscoverAsync(string? anchor, bool fullScan, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            MarkDiscovery();
            var found = ToEndpoint(await _discovery.DiscoverServerFastAsync(anchor, cancellationToken).ConfigureAwait(false));
            if (found is null && fullScan)
            {
                found = ToEndpoint(await _discovery.DiscoverServerAsync(anchor, cancellationToken).ConfigureAwait(false));
            }

            return await RememberIfFoundAsync(found).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogDiscoveryFailed(_logger, fullScan, ex);
            return null;
        }
    }

    /// <summary>
    /// Runs the full subnet scan once. A found endpoint is remembered; failures other than cancellation are logged.
    /// </summary>
    public async Task<MobaflowEndpoint?> DiscoverFullAsync(string? anchor, CancellationToken cancellationToken)
    {
        try
        {
            var found = ToEndpoint(await _discovery.DiscoverServerAsync(anchor, cancellationToken).ConfigureAwait(false));
            return await RememberIfFoundAsync(found).ConfigureAwait(false);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            LogAnchoredDiscoveryFailed(_logger, anchor ?? "(none)", ex);
            return null;
        }
    }

    /// <summary>
    /// Startup discovery: full scans after 0, 5 and 15 seconds until one finds MOBApi. The anchor is read before each
    /// pass because the selected project can change meanwhile.
    /// </summary>
    public async Task<MobaflowEndpoint?> DiscoverWithRetriesAsync(Func<string?> anchor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(anchor);
        MarkDiscovery();
        foreach (var delay in StartupRetryDelays)
        {
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, _timeProvider, cancellationToken).ConfigureAwait(false);
            }

            var found = ToEndpoint(await _discovery.DiscoverServerAsync(anchor(), cancellationToken).ConfigureAwait(false));
            if (found is not null)
            {
                return await RememberIfFoundAsync(found).ConfigureAwait(false);
            }
        }

        return null;
    }

    /// <summary>
    /// Rediscovers while MOBApi is unreachable: every 10 seconds during the first 90 seconds after start, then every
    /// 25 seconds. Returns null when it is not due yet or nothing was found.
    /// </summary>
    public async Task<MobaflowEndpoint?> DiscoverWhenDueAsync(string? anchor, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var interval = now - _startedAt < StartupWindow ? StartupRediscoverInterval : RediscoverInterval;
        lock (_discoveryClockLock)
        {
            if (_lastDiscoveryAt is { } last && now - last < interval)
            {
                return null;
            }

            _lastDiscoveryAt = now;
        }

        try
        {
            var found = ToEndpoint(await _discovery.DiscoverServerFastAsync(anchor, cancellationToken).ConfigureAwait(false))
                ?? ToEndpoint(await _discovery.DiscoverServerAsync(anchor, cancellationToken).ConfigureAwait(false));
            return await RememberIfFoundAsync(found).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogPeriodicDiscoveryFailed(_logger, ex);
            return null;
        }
    }

    /// <summary>
    /// Runs the full subnet scan without remembering the result, for a one-off use such as a photo upload.
    /// </summary>
    public async Task<MobaflowEndpoint?> FindAsync(string? anchor, CancellationToken cancellationToken) =>
        ToEndpoint(await _discovery.DiscoverServerAsync(anchor, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Lets the next periodic rediscovery run immediately, for example after the operator asked to search again.
    /// </summary>
    public void ResetDiscoveryCadence()
    {
        lock (_discoveryClockLock)
        {
            _lastDiscoveryAt = null;
        }
    }

    /// <summary>
    /// Adds a reachable address to the recent-address history and saves the settings when the history changed.
    /// </summary>
    public async Task RememberReachableAddressAsync(string ipAddress)
    {
        ArgumentNullException.ThrowIfNull(ipAddress);
        if (RestApiRecentEndpointHistory.RecordRecentIp(_settings.RestApi, ipAddress.Trim()))
        {
            await SaveSettingsAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Gets the delay before the next health check: 30 seconds while disabled or while the session is healthy,
    /// otherwise 10 seconds during the first 90 seconds after start and 15 seconds later.
    /// </summary>
    public TimeSpan GetHealthCheckDelay(bool connectionEnabled, bool sessionHealthy)
    {
        if (!connectionEnabled || sessionHealthy)
        {
            return HealthyCheckDelay;
        }

        return _timeProvider.GetUtcNow() - _startedAt < StartupWindow ? StartupRediscoverInterval : UnhealthyCheckDelay;
    }

    private void MarkDiscovery()
    {
        lock (_discoveryClockLock)
        {
            _lastDiscoveryAt = _timeProvider.GetUtcNow();
        }
    }

    private async Task<MobaflowEndpoint?> RememberIfFoundAsync(MobaflowEndpoint? found)
    {
        if (found is null)
        {
            return null;
        }

        MarkDiscovery();
        _settings.RestApi.CurrentIpAddress = found.IpAddress;
        _settings.RestApi.Port = found.Port;
        RestApiRecentEndpointHistory.RecordRecentIp(_settings.RestApi, found.IpAddress);
        await SaveSettingsAsync().ConfigureAwait(false);
        return found;
    }

    private async Task SaveSettingsAsync()
    {
        try
        {
            await _settingsService.SaveSettingsAsync(_settings).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Read-only storage must not break the connection; the in-memory settings still apply for this session.
            LogSettingsSaveFailed(_logger, ex);
        }
    }

    private static MobaflowEndpoint? ToEndpoint((string? ip, int? port) discovered) =>
        string.IsNullOrWhiteSpace(discovered.ip) || discovered.port is not { } port
            ? null
            : new MobaflowEndpoint(discovered.ip.Trim(), port);

    [LoggerMessage(Level = LogLevel.Debug, Message = "MOBAflow LAN discovery failed (fullScan={FullScan})")]
    private static partial void LogDiscoveryFailed(ILogger logger, bool fullScan, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "REST discovery with anchor {Anchor} failed")]
    private static partial void LogAnchoredDiscoveryFailed(ILogger logger, string anchor, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Periodic REST discovery failed")]
    private static partial void LogPeriodicDiscoveryFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Saving the MOBApi endpoint in the settings failed")]
    private static partial void LogSettingsSaveFailed(ILogger logger, Exception exception);
}
