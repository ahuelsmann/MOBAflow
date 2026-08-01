// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MOBApi.Security;

using Microsoft.Extensions.Options;

internal sealed class AnonymousReadRollbackStartupReporter(
    IOptions<ControlPlaneSecurityOptions> options,
    TimeProvider timeProvider,
    ICompatibilityReadTelemetryRecorder telemetry,
    ILogger<AnonymousReadRollbackStartupReporter> logger) : IHostedService
{
    private static readonly EventId RollbackActivatedEvent =
        new(5001, "AnonymousReadRollbackActivated");

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var rollbackUntilUtc = options.Value.AnonymousReadRollbackUntilUtc;
        if (!options.Value.LegacyAnonymousReadsEnabled && rollbackUntilUtc > timeProvider.GetUtcNow())
        {
            telemetry.RecordAnonymousRollbackActivated(rollbackUntilUtc.Value);
            logger.LogWarning(
                RollbackActivatedEvent,
                "Security audit: anonymous read-only rollback is active until {RollbackUntilUtc}. " +
                "Control, host publication, pairing administration, and credential administration remain protected.",
                rollbackUntilUtc.Value);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}