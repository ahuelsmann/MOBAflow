// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Common.Extension;

using Microsoft.Extensions.Logging;

using Protocol;

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
        _statusText = string.IsNullOrEmpty(_endpointSource.DisplayAddress)
            ? "Looking for the Z21..."
            : $"Connecting to {_endpointSource.DisplayAddress}...";
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

            var rediscover = _z21ConnectionFailureCount >= ConnectionFailuresBeforeRescan;
            var endpoint = await _endpointSource.ResolveAsync(rediscover).ConfigureAwait(false);
            if (rediscover)
            {
                _z21ConnectionFailureCount = 0;
            }

            if (endpoint.Address is not { } address)
            {
                _isZ21Connecting = false;
                _statusText = endpoint.Error ?? "No Z21 address";
                PublishSnapshot();
                return;
            }

            var portsToTry = BuildConnectionPorts(endpoint.Port);
            Exception? lastException = null;

            foreach (var port in portsToTry)
            {
                try
                {
                    _isZ21Connecting = true;
                    _statusText = "Connecting to Z21...";
                    PublishSnapshot();

                    _z21.SetSystemStatePollingInterval(_settings.Z21.SystemStatePollingIntervalSeconds);
                    await _z21.ConnectAsync(address, port).ConfigureAwait(false);
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
}
