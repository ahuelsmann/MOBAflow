// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Common.Extension;

using Microsoft.Extensions.Logging;

using System.Net;
using System.Threading;

/// <summary>
/// Z21 auto-connect timer and endpoint resolution for <see cref="MobaRuntimeService"/>.
/// </summary>
public sealed partial class MobaRuntimeService
{
    private void BeginAutoConnectToZ21()
    {
        if (string.IsNullOrEmpty(_settings.Z21.CurrentIpAddress))
        {
            _isZ21Connecting = false;
            _statusText = "No Z21 IP configured";
            PublishSnapshot();
            return;
        }

        _isZ21Connecting = true;
        _statusText = $"Connecting to {_settings.Z21.CurrentIpAddress}...";
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

            if (!TryGetConfiguredEndpoint(out var address, out var port, out var errorMessage))
            {
                _isZ21Connecting = false;
                _statusText = errorMessage;
                PublishSnapshot();
                return;
            }

            try
            {
                _isZ21Connecting = true;
                _statusText = "Connecting to Z21...";
                PublishSnapshot();

                _z21.SetSystemStatePollingInterval(_settings.Z21.SystemStatePollingIntervalSeconds);
                await _z21.ConnectAsync(address!, port).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _isZ21Connecting = false;
                _statusText = $"Z21 unavailable: {ex.Message}";
                PublishSnapshot();
                _logger.LogWarning(ex, "Automatic Z21 connection attempt failed");
            }
        }
        finally
        {
            Interlocked.Exchange(ref _autoConnectAttemptInProgress, 0);
        }
    }

    private bool TryGetConfiguredEndpoint(out IPAddress? address, out int port, out string errorMessage)
    {
        address = null;
        port = 21105;
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(_settings.Z21.CurrentIpAddress))
        {
            errorMessage = "No IP address configured in AppSettings";
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
