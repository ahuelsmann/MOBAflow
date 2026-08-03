// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MOBApi.Security;

using System.Security.Cryptography;
using System.Text.Json;

/// <summary>
/// Restores migration metrics and emits an auditable warning when read-only rollback is active.
/// </summary>
internal sealed class CompatibilityReadStartupReporter : IHostedService
{
    private static readonly Action<ILogger, DateTimeOffset?, Exception?> LogRollbackActive = LoggerMessage.Define<DateTimeOffset?>(
        LogLevel.Warning,
        new EventId(5001, "CompatibilityReadRollbackActive"),
        "The anonymous read-only rollback is active until {RollbackExpiresAt}. Anonymous control and administration remain disabled.");
    private static readonly Action<ILogger, Exception?> LogStateUnavailable = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(5004, "CompatibilityReadStateUnavailable"),
        "The protected compatibility-read state is unavailable. The service remains online in fail-closed mode; anonymous reads stay disabled.");
    private readonly ILogger<CompatibilityReadStartupReporter> _logger;
    private readonly CompatibilityReadMetrics _metrics;
    private readonly ICompatibilityReadMigrationRecovery _migration;
    private readonly TimeProvider _timeProvider;

    public CompatibilityReadStartupReporter(
        ICompatibilityReadMigrationRecovery migration,
        CompatibilityReadMetrics metrics,
        TimeProvider timeProvider,
        ILogger<CompatibilityReadStartupReporter> logger)
    {
        _migration = migration;
        _metrics = metrics;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        CompatibilityReadMigrationStatus status;
        try
        {
            status = await _migration.GetStatusAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (
            exception is CryptographicException or JsonException or InvalidDataException or IOException or UnauthorizedAccessException)
        {
            await _migration.EnterFailClosedModeAsync(cancellationToken).ConfigureAwait(false);
            LogStateUnavailable(_logger, exception);
            return;
        }

        _metrics.SetRollbackExpiry(status.RollbackExpiresAt);
        if (status.RollbackExpiresAt <= _timeProvider.GetUtcNow())
            return;

        LogRollbackActive(_logger, status.RollbackExpiresAt, null);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
