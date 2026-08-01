// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MOBApi.Security;

using Microsoft.Extensions.Options;

/// <summary>
/// Exposes the evidence-based state of the anonymous-read migration window.
/// </summary>
public interface ICompatibilityReadiness
{
    CompatibilityReadinessSnapshot GetSnapshot();
}

/// <summary>
/// Describes why the anonymous-read migration is or is not ready for manual enforcement.
/// </summary>
public enum CompatibilityReadinessState
{
    StableClientReleaseMissing,
    AuthenticatedTrafficAbsent,
    CriticalDefectOpen,
    Observing,
    Ready
}

/// <summary>
/// Represents aggregate readiness evidence without retaining credentials or request data.
/// </summary>
public sealed record CompatibilityReadinessSnapshot(
    CompatibilityReadinessState State,
    DateTimeOffset? ObservationStartedUtc,
    DateTimeOffset? EligibleAfterUtc,
    bool IsReady);

internal sealed class CompatibilityReadiness(
    ICompatibilityReadTelemetry telemetry,
    IOptions<ControlPlaneSecurityOptions> options,
    TimeProvider timeProvider) : ICompatibilityReadiness
{
    private static readonly TimeSpan RequiredObservationPeriod = TimeSpan.FromDays(14);

    public CompatibilityReadinessSnapshot GetSnapshot()
    {
        var observationStartedUtc = options.Value.StableAuthenticatedClientReleaseUtc;
        if (observationStartedUtc is null)
        {
            return new CompatibilityReadinessSnapshot(
                CompatibilityReadinessState.StableClientReleaseMissing,
                null,
                null,
                false);
        }

        if (options.Value.LastCriticalDefectResolvedUtc is { } lastCriticalDefectResolvedUtc &&
            lastCriticalDefectResolvedUtc > observationStartedUtc)
        {
            observationStartedUtc = lastCriticalDefectResolvedUtc;
        }

        var eligibleAfterUtc = observationStartedUtc.Value + RequiredObservationPeriod;
        if (telemetry.GetSnapshot().AuthenticatedReadCount == 0)
        {
            return new CompatibilityReadinessSnapshot(
                CompatibilityReadinessState.AuthenticatedTrafficAbsent,
                observationStartedUtc,
                eligibleAfterUtc,
                false);
        }

        if (options.Value.HasOpenCriticalMigrationDefect)
        {
            return new CompatibilityReadinessSnapshot(
                CompatibilityReadinessState.CriticalDefectOpen,
                observationStartedUtc,
                eligibleAfterUtc,
                false);
        }

        var isReady = timeProvider.GetUtcNow() >= eligibleAfterUtc;
        return new CompatibilityReadinessSnapshot(
            isReady ? CompatibilityReadinessState.Ready : CompatibilityReadinessState.Observing,
            observationStartedUtc,
            eligibleAfterUtc,
            isReady);
    }
}