// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using System.Diagnostics.Metrics;

/// <summary>
/// Counters of the Z21 event pipeline and the runtime fail-safe, readable live with
/// <c>dotnet-counters monitor --name MOBAflow --counters Moba.Backend</c>. Each counter accompanies an existing log entry.
/// </summary>
public static class BackendTelemetry
{
    /// <summary>Meter of the Backend counters.</summary>
    public const string MeterName = "Moba.Backend";

    /// <summary>Z21 events the pipeline rejected because it was full or stopped, tagged with <c>event_type</c>.</summary>
    public const string RejectedZ21EventsName = "moba.z21.events.rejected";

    /// <summary>Z21 events whose publication failed at the event-bus boundary, tagged with <c>event_type</c>.</summary>
    public const string FailedZ21DispatchesName = "moba.z21.events.dispatch_failed";

    /// <summary>Pipeline drains that exceeded their timeout during shutdown.</summary>
    public const string PipelineShutdownTimeoutsName = "moba.z21.pipeline.shutdown_timeouts";

    /// <summary>Times a runtime entered the fail-safe state after an unexpected loss of its Z21 connection.</summary>
    public const string FailSafeTriggersName = "moba.runtime.fail_safe_triggered";

    private static readonly Meter Meter = new(MeterName);

    private static readonly Counter<long> RejectedZ21Events = Meter.CreateCounter<long>(
        RejectedZ21EventsName, unit: "{event}", description: "Z21 events rejected because the pipeline was full or stopped.");

    private static readonly Counter<long> FailedZ21Dispatches = Meter.CreateCounter<long>(
        FailedZ21DispatchesName, unit: "{event}", description: "Z21 events whose publication failed at the event-bus boundary.");

    private static readonly Counter<long> PipelineShutdownTimeouts = Meter.CreateCounter<long>(
        PipelineShutdownTimeoutsName, unit: "{drain}", description: "Z21 event pipeline drains that exceeded their timeout.");

    private static readonly Counter<long> FailSafeTriggers = Meter.CreateCounter<long>(
        FailSafeTriggersName, unit: "{trigger}", description: "Fail-safe states entered after an unexpected loss of the Z21 connection.");

    internal static void RecordRejectedZ21Event(string eventType) =>
        RejectedZ21Events.Add(1, new KeyValuePair<string, object?>("event_type", eventType));

    internal static void RecordFailedZ21Dispatch(string eventType) =>
        FailedZ21Dispatches.Add(1, new KeyValuePair<string, object?>("event_type", eventType));

    internal static void RecordPipelineShutdownTimeout() => PipelineShutdownTimeouts.Add(1);

    internal static void RecordFailSafeTrigger() => FailSafeTriggers.Add(1);
}
