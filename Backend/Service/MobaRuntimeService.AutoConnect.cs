// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Common.Discovery;
using Common.Extension;

using Microsoft.Extensions.Logging;

using Protocol;

using System.Net;
using System.Threading;

/// <summary>
/// Z21 auto-connect timer and endpoint resolution for <see cref="MobaRuntimeService"/>.
/// </summary>
public sealed partial class MobaRuntimeService
{
    private const int ConnectionFailuresBeforeRescan = 3;

    private int _z21ConnectionFailureCount;

    private void BeginAutoConnectToZ21()
    {
        _isZ21Connecting = true;
        _statusText = string.IsNullOrEmpty(_settings.Z21.CurrentIpAddress)
            ? "Discovering Z21 on LAN..."
            : $"Connecting to {_settings.Z21.CurrentIpAddress}...";
        PublishSnapshot();

        StartAutoConnectTimer();
        AttemptZ21ConnectionAsync()
            .Observe(ex => _logger.LogError(ex, "Initial automatic Z21 connection attempt failed unexpectedly"));
    }

    private void StartAutoConnectTimer()
    {
        StopAutoConnectTimer();

        var retryInterval = TimeSpan.FromSeconds(_settings.Z21.AutoConnectRetryIntervalSeconds);
        _z21AutoConnectTimer = new Timer(
            state =>
            {
                _ = state;
                if (!_isConnected && !_isManualDisconnectRequested)
                {
                    AttemptZ21ConnectionAsync().Observe(ex => _logger.LogError(ex, "Automatic Z21 connection attempt failed unexpectedly"));
                }
            },
            null,
            retryInterval,
            retryInterval);

        _logger.LogInformation(
            "Z21 auto-connect retry timer started ({RetryInterval}s interval)",
            _settings.Z21.AutoConnectRetryIntervalSeconds);
    }

    private void StopAutoConnectTimer()
    {
        _z21AutoConnectTimer?.Dispose();
        _z21AutoConnectTimer = null;
    }

    private async Task AttemptZ21ConnectionAsync()
    {
        if (Interlocked.CompareExchange(ref _autoConnectAttemptInProgress, 1, 0) == 1)
        {
            return;
        }

        try
        {
            if (_isConnected || _isManualDisconnectRequested)
            {
                return;
            }

            if (ShouldRunZ21Discovery())
            {
                await TryDiscoverAndApplyZ21EndpointAsync().ConfigureAwait(false);
            }

            if (!TryGetConfiguredEndpoint(out var address, out var primaryPort, out var errorMessage))
            {
                _isZ21Connecting = false;
                _statusText = errorMessage;
                PublishSnapshot();
                return;
            }

            var portsToTry = BuildConnectionPorts(primaryPort);
            Exception? lastException = null;

            foreach (var port in portsToTry)
            {
                try
                {
                    _isZ21Connecting = true;
                    _statusText = "Connecting to Z21...";
                    PublishSnapshot();

                    _z21.SetSystemStatePollingInterval(_settings.Z21.SystemStatePollingIntervalSeconds);
                    await _z21.ConnectAsync(address!, port).ConfigureAwait(false);
                    _z21ConnectionFailureCount = 0;
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }
            }

            _z21ConnectionFailureCount++;
            _isZ21Connecting = false;
            _statusText = $"Z21 unavailable: {lastException?.Message ?? "Connection failed"}";
            PublishSnapshot();
            _logger.LogWarning(lastException, "Automatic Z21 connection attempt failed (failures={FailureCount})", _z21ConnectionFailureCount);
        }
        finally
        {
            Interlocked.Exchange(ref _autoConnectAttemptInProgress, 0);
        }
    }

    private bool ShouldRunZ21Discovery()
        => string.IsNullOrWhiteSpace(_settings.Z21.CurrentIpAddress)
           || _z21ConnectionFailureCount >= ConnectionFailuresBeforeRescan;

    private async Task TryDiscoverAndApplyZ21EndpointAsync()
    {
        var preferred = string.IsNullOrWhiteSpace(_settings.Z21.CurrentIpAddress)
            ? null
            : _settings.Z21.CurrentIpAddress.Trim();

        var discovered = await _z21Discovery.DiscoverZ21Async(preferred).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(discovered))
        {
            return;
        }

        _settings.Z21.CurrentIpAddress = discovered.Trim();
        _z21ConnectionFailureCount = 0;
        _logger.LogInformation("Discovered Z21 at {Ip}", discovered);
    }

    private static IReadOnlyList<int> BuildConnectionPorts(int configuredPort)
    {
        if (configuredPort == Z21Protocol.DefaultPort)
        {
            return [Z21Protocol.DefaultPort, Z21Protocol.AlternativePort];
        }

        if (configuredPort == Z21Protocol.AlternativePort)
        {
            return [Z21Protocol.AlternativePort, Z21Protocol.DefaultPort];
        }

        return [configuredPort];
    }

    private bool TryGetConfiguredEndpoint(out IPAddress? address, out int port, out string errorMessage)
    {
        address = null;
        port = Z21Protocol.DefaultPort;
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(_settings.Z21.CurrentIpAddress))
        {
            errorMessage = "No Z21 found on LAN";
            return false;
        }

        if (!IPAddress.TryParse(_settings.Z21.CurrentIpAddress, out address))
        {
            errorMessage = $"Invalid Z21 IP address '{_settings.Z21.CurrentIpAddress}'";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(_settings.Z21.DefaultPort)
            && int.TryParse(_settings.Z21.DefaultPort, out var parsedPort))
        {
            port = parsedPort;
        }

        return true;
    }
}
