// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MOBApi.Security;

using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Exposes aggregate compatibility and readiness evidence to the host-only controller.
/// </summary>
[UnconditionalSuppressMessage(
    "Maintainability",
    "CA1515:Consider making public types internal",
    Justification = "ASP.NET Core controller activation requires this constructor-injected contract to be public.")]
public interface ICompatibilityStatusProvider
{
    object GetStatus();
}

/// <summary>
/// Exposes the evidence-based state of the anonymous-read migration window.
/// </summary>
internal interface ICompatibilityReadiness
{
    CompatibilityReadinessSnapshot GetSnapshot();
}

/// <summary>
/// Describes why the anonymous-read migration is or is not ready for manual enforcement.
/// </summary>
internal enum CompatibilityReadinessState
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
internal sealed record CompatibilityReadinessSnapshot(
    CompatibilityReadinessState State,
    DateTimeOffset? ObservationStartedUtc,
    DateTimeOffset? EligibleAfterUtc,
    bool IsReady);

internal sealed record CompatibilityStatusResponse(
    CompatibilityReadTelemetrySnapshot Telemetry,
    CompatibilityReadinessSnapshot Readiness);

internal sealed class CompatibilityStatusProvider(
    ICompatibilityReadTelemetry telemetry,
    ICompatibilityReadiness readiness) : ICompatibilityStatusProvider
{
    public object GetStatus() => new CompatibilityStatusResponse(
        telemetry.GetSnapshot(),
        readiness.GetSnapshot());
}

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