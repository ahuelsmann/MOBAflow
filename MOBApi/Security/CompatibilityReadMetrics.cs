// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using System.Diagnostics.Metrics;

namespace Moba.MOBApi.Security;

/// <summary>
/// Publishes bounded compatibility-read metrics without reusable credential identifiers.
/// </summary>
public sealed class CompatibilityReadMetrics : IDisposable
{
    /// <summary>Meter name used by all control-plane compatibility instruments.</summary>
    public const string MeterName = "Moba.MOBApi.ControlPlane";

    /// <summary>Counter for low-cardinality read outcomes.</summary>
    public const string ReadOutcomeMetricName = "mobapi.compatibility_read.outcomes";

    /// <summary>Gauge that is one only while read-only rollback is active.</summary>
    public const string RollbackActiveMetricName = "mobapi.compatibility_read.rollback_active";

    private readonly Meter _meter = new(MeterName);
    private readonly Counter<long> _readOutcomes;
    private readonly TimeProvider _timeProvider;
    private long _rollbackExpiresAtUtcTicks;

    /// <summary>Creates compatibility instruments against the supplied clock.</summary>
    public CompatibilityReadMetrics(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        _readOutcomes = _meter.CreateCounter<long>(ReadOutcomeMetricName);
        _meter.CreateObservableGauge<long>(
            RollbackActiveMetricName,
            () => Volatile.Read(ref _rollbackExpiresAtUtcTicks) > _timeProvider.GetUtcNow().UtcTicks ? 1L : 0L);
    }

    /// <summary>Records one read outcome without credential or client labels.</summary>
    public void RecordReadOutcome(
        CompatibilityReadTransport transport,
        CompatibilityReadOutcome outcome)
    {
        _readOutcomes.Add(
            1,
            new KeyValuePair<string, object?>("transport", CompatibilityReadTransportNames.GetProtocolName(transport)),
            new KeyValuePair<string, object?>("outcome", outcome.ToString()));
    }

    /// <summary>Updates the expiry observed by the rollback-active gauge.</summary>
    public void SetRollbackExpiry(DateTimeOffset? rollbackExpiresAt)
    {
        Interlocked.Exchange(ref _rollbackExpiresAtUtcTicks, rollbackExpiresAt?.UtcTicks ?? 0);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _meter.Dispose();
    }
}
