// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdisplay;

using Moba.Display.Protocol;

using System.Globalization;
using System.Text;

/// <summary>
/// Evaluates one sustained-refresh run against the Issue #36 acceptance thresholds:
/// at most 1 percent dropped or rejected frames and no device reboot.
/// </summary>
internal sealed record SustainedRefreshReport(
    TimeSpan Duration,
    int RefreshHz,
    long RenderedFrames,
    long PresentedFrames,
    long FailedFrames,
    long Recoveries,
    HealthResponsePayload HealthBefore,
    HealthResponsePayload HealthAfter)
{
    /// <summary>Maximum share of expected frames that may be dropped or rejected.</summary>
    public const double MaximumLossRatio = 0.01;

    // Uptime is reported in whole seconds and sampled around the run, so allow a small clock difference.
    private static readonly TimeSpan UptimeTolerance = TimeSpan.FromSeconds(5);

    /// <summary>Frames the scheduler should have produced at the configured refresh rate.</summary>
    public long ExpectedFrames => (long)Math.Floor(Duration.TotalSeconds * RefreshHz);

    /// <summary>Timer ticks the host skipped, for example because a transfer took longer than one period.</summary>
    public long SkippedFrames => Math.Max(0, ExpectedFrames - RenderedFrames);

    /// <summary>Frames the device counted as accepted during the run.</summary>
    public long DeviceAcceptedFrames => (long)HealthAfter.AcceptedFrameCount - HealthBefore.AcceptedFrameCount;

    /// <summary>Frames the device counted as rejected during the run.</summary>
    public long DeviceRejectedFrames => (long)HealthAfter.RejectedFrameCount - HealthBefore.RejectedFrameCount;

    /// <summary>
    /// True when the device uptime did not advance by the run duration, or the frame counters went backwards.
    /// </summary>
    public bool RebootDetected =>
        HealthAfter.UptimeSeconds + UptimeTolerance.TotalSeconds < HealthBefore.UptimeSeconds + Duration.TotalSeconds
        || DeviceAcceptedFrames < 0
        || DeviceRejectedFrames < 0;

    /// <summary>Dropped (skipped or failed on the host) plus device-rejected frames.</summary>
    public long LostFrames => SkippedFrames + FailedFrames + Math.Max(0, DeviceRejectedFrames);

    /// <summary>Share of expected frames that were lost.</summary>
    public double LossRatio => ExpectedFrames == 0 ? 1 : (double)LostFrames / ExpectedFrames;

    /// <summary>True when every acceptance threshold of the run is met.</summary>
    public bool Passed => ExpectedFrames > 0 && !RebootDetected && LossRatio <= MaximumLossRatio;

    /// <summary>Formats the report for the Issue #36 acceptance record.</summary>
    public string Format()
    {
        var culture = CultureInfo.InvariantCulture;
        var builder = new StringBuilder();
        builder.AppendLine(culture, $"Duration: {Duration:hh\\:mm\\:ss} at {RefreshHz} Hz");
        builder.AppendLine(culture, $"Expected frames: {ExpectedFrames}");
        builder.AppendLine(culture, $"Rendered and sent: {RenderedFrames}");
        builder.AppendLine(culture, $"Presented (host confirmed): {PresentedFrames}");
        builder.AppendLine(culture, $"Failed on the host: {FailedFrames}");
        builder.AppendLine(culture, $"Skipped timer ticks: {SkippedFrames}");
        builder.AppendLine(culture, $"Recoveries after a failure: {Recoveries}");
        builder.AppendLine(culture, $"Device accepted / rejected: {DeviceAcceptedFrames} / {DeviceRejectedFrames}");
        builder.AppendLine(culture, $"Device uptime before / after: {HealthBefore.UptimeSeconds} s / {HealthAfter.UptimeSeconds} s");
        builder.AppendLine(culture, $"Reboot detected: {(RebootDetected ? "yes" : "no")}");
        builder.AppendLine(culture, $"Lost frames: {LostFrames} ({LossRatio:P2}, limit {MaximumLossRatio:P0})");
        builder.Append(culture, $"Result: {(Passed ? "PASSED" : "FAILED")}");
        return builder.ToString();
    }
}
