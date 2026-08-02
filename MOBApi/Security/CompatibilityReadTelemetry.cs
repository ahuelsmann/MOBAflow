// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MOBApi.Security;

/// <summary>
/// Exposes aggregate compatibility-read outcomes without retaining request payloads or credentials.
/// </summary>
internal interface ICompatibilityReadTelemetry
{
    CompatibilityReadTelemetrySnapshot GetSnapshot();
}

/// <summary>
/// Represents aggregate compatibility-read outcomes for operational readiness checks.
/// </summary>
internal sealed record CompatibilityReadTelemetrySnapshot(
    long AnonymousReadCount,
    long AuthenticatedReadCount,
    bool AnonymousRollbackActive,
    long AnonymousRollbackActivationCount);

internal interface ICompatibilityReadTelemetryRecorder
{
    void RecordAnonymousRead();

    void RecordAuthenticatedRead();

    void RecordAnonymousRollbackActivated(DateTimeOffset rollbackUntilUtc);
}

internal sealed class CompatibilityReadTelemetry(TimeProvider timeProvider)
    : ICompatibilityReadTelemetry, ICompatibilityReadTelemetryRecorder
{
    private long _anonymousReadCount;
    private long _authenticatedReadCount;
    private long _anonymousRollbackUntilUtcTicks;
    private long _anonymousRollbackActivationCount;

    public CompatibilityReadTelemetrySnapshot GetSnapshot() =>
        new(
            Interlocked.Read(ref _anonymousReadCount),
            Interlocked.Read(ref _authenticatedReadCount),
            new DateTimeOffset(Interlocked.Read(ref _anonymousRollbackUntilUtcTicks), TimeSpan.Zero) >
            timeProvider.GetUtcNow(),
            Interlocked.Read(ref _anonymousRollbackActivationCount));

    public void RecordAnonymousRead() => Interlocked.Increment(ref _anonymousReadCount);

    public void RecordAuthenticatedRead() => Interlocked.Increment(ref _authenticatedReadCount);

    public void RecordAnonymousRollbackActivated(DateTimeOffset rollbackUntilUtc)
    {
        Interlocked.Exchange(ref _anonymousRollbackUntilUtcTicks, rollbackUntilUtc.UtcTicks);
        Interlocked.Increment(ref _anonymousRollbackActivationCount);
    }
}