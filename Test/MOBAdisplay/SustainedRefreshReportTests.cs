// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdisplay;

using Moba.Display.Protocol;

[TestFixture]
[Category("Unit")]
internal sealed class SustainedRefreshReportTests
{
    private const int NormalRefreshHz = 10;
    private static readonly TimeSpan TwoHours = TimeSpan.FromHours(2);

    [Test]
    public void CleanRun_Passes()
    {
        var report = Create(rendered: 72_000, presented: 72_000, failed: 0, accepted: 72_000, rejected: 0);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(report.ExpectedFrames, Is.EqualTo(72_000));
            Assert.That(report.LostFrames, Is.Zero);
            Assert.That(report.IsAcceptanceConfiguration, Is.True);
            Assert.That(report.Passed, Is.True);
        }
    }

    [Test]
    public void LossAtOnePercent_Passes_AndAboveOnePercent_Fails()
    {
        // 720 of 72,000 expected frames not presented is exactly the 1 percent limit.
        var atLimit = Create(rendered: 71_700, presented: 71_280, failed: 420, accepted: 71_280, rejected: 120);
        var aboveLimit = Create(rendered: 71_700, presented: 71_279, failed: 421, accepted: 71_279, rejected: 121);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(atLimit.LostFrames, Is.EqualTo(720));
            Assert.That(atLimit.Passed, Is.True);
            Assert.That(aboveLimit.LostFrames, Is.EqualTo(721));
            Assert.That(aboveLimit.Passed, Is.False);
        }
    }

    [Test]
    public void FrameRejectedOnTheDeviceAndFailedOnTheHost_CountsOnce()
    {
        // A checksum mismatch raises the device's rejected counter and fails the frame on the host.
        var report = Create(rendered: 72_000, presented: 71_600, failed: 400, accepted: 71_600, rejected: 400);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(report.LostFrames, Is.EqualTo(400));
            Assert.That(report.Passed, Is.True);
        }
    }

    [Test]
    public void RepairedIncompleteTransfers_AreNotLost()
    {
        // The device counts an incomplete transfer as rejected even when the host repairs and presents the frame.
        var report = Create(rendered: 72_000, presented: 72_000, failed: 0, accepted: 72_000, rejected: 900);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(report.LostFrames, Is.Zero);
            Assert.That(report.Passed, Is.True);
        }
    }

    [Test]
    public void UptimeThatDidNotAdvanceByTheRun_IsReportedAsReboot()
    {
        var report = Create(rendered: 72_000, presented: 72_000, failed: 0, accepted: 72_000, rejected: 0, uptimeAfter: 600);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(report.RebootDetected, Is.True);
            Assert.That(report.Passed, Is.False);
        }
    }

    [Test]
    public void RejectedCounterThatWentBackwards_IsReportedAsReboot()
    {
        var report = Create(rendered: 72_000, presented: 72_000, failed: 0, accepted: 72_000, rejected: -1);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(report.RebootDetected, Is.True);
            Assert.That(report.Passed, Is.False);
        }
    }

    [Test]
    public void RebootWithinTheUptimeTolerance_IsReportedFromTheAcceptedCounter()
    {
        // The run starts 2 s after boot and the device reboots 3 s later: the uptime still looks plausible,
        // but the restarted counter holds fewer accepted frames than the host saw presented.
        var before = new HealthResponsePayload(DisplayHealthState.Ready, DisplayResultCode.Ok, 2, 100_000, 0, 0, 0);
        var after = before with { UptimeSeconds = 7_197, AcceptedFrameCount = 71_970 };
        var report = new SustainedRefreshReport(TwoHours, NormalRefreshHz, NormalRefreshHz, 72_000, 72_000, 0, 0, before, after);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(report.RebootDetected, Is.True);
            Assert.That(report.Passed, Is.False);
        }
    }

    [Test]
    public void ShortRun_IsMarkedAsNotAnAcceptanceRun()
    {
        var before = new HealthResponsePayload(DisplayHealthState.Ready, DisplayResultCode.Ok, 1_000, 100_000, 0, 0, 0);
        var after = before with { UptimeSeconds = 1_060, AcceptedFrameCount = 600 };
        var report = new SustainedRefreshReport(
            TimeSpan.FromMinutes(1), NormalRefreshHz, NormalRefreshHz, 600, 600, 0, 0, before, after);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(report.Passed, Is.True);
            Assert.That(report.IsAcceptanceConfiguration, Is.False);
            Assert.That(report.Format(), Does.EndWith("Result: PASSED (not an acceptance run)"));
        }
    }

    [Test]
    public void Format_ContainsTheAcceptanceFigures()
    {
        var text = Create(rendered: 71_900, presented: 71_890, failed: 10, accepted: 71_890, rejected: 0).Format();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(text, Does.Contain("Acceptance configuration: yes (requires 2 hours at 10 Hz)"));
            Assert.That(text, Does.Contain("Expected frames: 72000"));
            Assert.That(text, Does.Contain("Skipped timer ticks: 100"));
            Assert.That(text, Does.Contain("Lost frames: 110"));
            Assert.That(text, Does.Contain("Reboot detected: no"));
            Assert.That(text, Does.EndWith("Result: PASSED"));
        }
    }

    private static SustainedRefreshReport Create(
        long rendered,
        long presented,
        long failed,
        long accepted,
        long rejected,
        uint uptimeAfter = 1_000 + 7_200)
    {
        const uint acceptedBefore = 1_000;
        const uint rejectedBefore = 10;
        var before = new HealthResponsePayload(
            DisplayHealthState.Ready, DisplayResultCode.Ok, 1_000, 100_000, acceptedBefore, rejectedBefore, 0);
        var after = before with
        {
            UptimeSeconds = uptimeAfter,
            AcceptedFrameCount = (uint)(acceptedBefore + accepted),
            RejectedFrameCount = (uint)(rejectedBefore + rejected)
        };
        return new SustainedRefreshReport(
            TwoHours, NormalRefreshHz, NormalRefreshHz, rendered, presented, failed, 0, before, after);
    }
}
