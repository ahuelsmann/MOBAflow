// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdisplay;

using Moba.Display.Protocol;

[TestFixture]
[Category("Unit")]
internal sealed class SustainedRefreshReportTests
{
    private static readonly TimeSpan TwoHours = TimeSpan.FromHours(2);

    [Test]
    public void CleanRun_Passes()
    {
        var report = Create(rendered: 72_000, presented: 72_000, failed: 0, accepted: 72_000, rejected: 0);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(report.ExpectedFrames, Is.EqualTo(72_000));
            Assert.That(report.LostFrames, Is.Zero);
            Assert.That(report.Passed, Is.True);
        }
    }

    [Test]
    public void LossAtOnePercent_Passes_AndAboveOnePercent_Fails()
    {
        // 720 of 72,000 frames is exactly the 1 percent limit: skipped, failed and device-rejected frames add up.
        var atLimit = Create(rendered: 71_700, presented: 71_400, failed: 300, accepted: 71_580, rejected: 120);
        var aboveLimit = Create(rendered: 71_700, presented: 71_400, failed: 300, accepted: 71_579, rejected: 121);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(atLimit.LostFrames, Is.EqualTo(720));
            Assert.That(atLimit.Passed, Is.True);
            Assert.That(aboveLimit.LostFrames, Is.EqualTo(721));
            Assert.That(aboveLimit.Passed, Is.False);
        }
    }

    [Test]
    public void UptimeThatDidNotAdvanceByTheRun_IsReportedAsReboot()
    {
        var report = Create(rendered: 72_000, presented: 72_000, failed: 0, accepted: 500, rejected: 0, uptimeAfter: 600);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(report.RebootDetected, Is.True);
            Assert.That(report.Passed, Is.False);
        }
    }

    [Test]
    public void FrameCountersThatWentBackwards_AreReportedAsReboot()
    {
        var report = Create(rendered: 72_000, presented: 72_000, failed: 0, accepted: -1, rejected: 0);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(report.RebootDetected, Is.True);
            Assert.That(report.Passed, Is.False);
        }
    }

    [Test]
    public void Format_ContainsTheAcceptanceFigures()
    {
        var text = Create(rendered: 71_900, presented: 71_890, failed: 10, accepted: 71_890, rejected: 0).Format();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(text, Does.Contain("Expected frames: 72000"));
            Assert.That(text, Does.Contain("Skipped timer ticks: 100"));
            Assert.That(text, Does.Contain("Reboot detected: no"));
            Assert.That(text, Does.Contain("Result: PASSED"));
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
        return new SustainedRefreshReport(TwoHours, 10, rendered, presented, failed, 0, before, after);
    }
}
