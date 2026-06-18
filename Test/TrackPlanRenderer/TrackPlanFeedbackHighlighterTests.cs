// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.TrackPlanRenderer;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Common.Events;
using Moba.SharedUI.Service;
using Moba.TrackLibrary.PikoA;

[TestFixture]
internal class TrackPlanFeedbackHighlighterTests
{
    private static (TrackPlanFeedbackHighlighter Highlighter, EventBus Bus) CreateHighlighter(
        EditableTrackPlan plan,
        TimeSpan? hold = null,
        TimeSpan? ignore = null)
    {
        var bus = new EventBus(NullLogger<EventBus>.Instance);
        var highlighter = new TrackPlanFeedbackHighlighter(bus, plan)
        {
            HoldDuration = hold ?? TimeSpan.FromMilliseconds(1500),
            IgnoreWindow = ignore ?? TimeSpan.FromMilliseconds(1500)
        };
        highlighter.Activate();
        return (highlighter, bus);
    }

    [Test]
    public void Feedback_For_Matching_InPort_Activates_Segment()
    {
        var plan = new EditableTrackPlan();
        var placed = new PlacedSegment(new G62(), 0, 0, 0, InPort: 7);
        plan.AddSegment(placed);

        var (highlighter, bus) = CreateHighlighter(plan);
        using (highlighter)
        {
            var changedCount = 0;
            highlighter.HighlightsChanged += (_, _) => changedCount++;

            bus.Publish(new FeedbackReceivedEvent(7));

            Assert.That(highlighter.HasActiveHighlights, Is.True);
            Assert.That(highlighter.GetPulseIntensity(placed.Segment.No), Is.GreaterThan(0));
            Assert.That(changedCount, Is.GreaterThanOrEqualTo(1));
        }
    }

    [Test]
    public void Feedback_For_Unmatched_InPort_DoesNothing()
    {
        var plan = new EditableTrackPlan();
        var placed = new PlacedSegment(new G62(), 0, 0, 0, InPort: 7);
        plan.AddSegment(placed);

        var (highlighter, bus) = CreateHighlighter(plan);
        using (highlighter)
        {
            var changedCount = 0;
            highlighter.HighlightsChanged += (_, _) => changedCount++;

            bus.Publish(new FeedbackReceivedEvent(42));

            Assert.That(highlighter.HasActiveHighlights, Is.False);
            Assert.That(highlighter.GetPulseIntensity(placed.Segment.No), Is.EqualTo(0));
            Assert.That(changedCount, Is.EqualTo(0));
        }
    }

    [Test]
    public void Feedback_For_Segment_Without_InPort_DoesNothing()
    {
        var plan = new EditableTrackPlan();
        var placed = new PlacedSegment(new G62(), 0, 0, 0);
        plan.AddSegment(placed);

        var (highlighter, bus) = CreateHighlighter(plan);
        using (highlighter)
        {
            bus.Publish(new FeedbackReceivedEvent(1));

            Assert.That(highlighter.HasActiveHighlights, Is.False);
        }
    }

    [Test]
    public void Feedback_Respects_IgnoreWindow()
    {
        var plan = new EditableTrackPlan();
        var placed = new PlacedSegment(new G62(), 0, 0, 0, InPort: 3);
        plan.AddSegment(placed);

        // Large ignore window so the re-trigger is definitely absorbed.
        var (highlighter, bus) = CreateHighlighter(plan, ignore: TimeSpan.FromSeconds(10));
        using (highlighter)
        {
            bus.Publish(new FeedbackReceivedEvent(3));
            var firstIntensity = highlighter.GetPulseIntensity(placed.Segment.No);

            // Small sleep so the clock advances a measurable amount; pulse should NOT be restarted.
            Thread.Sleep(50);
            var changedAfter = 0;
            highlighter.HighlightsChanged += (_, _) => changedAfter++;

            bus.Publish(new FeedbackReceivedEvent(3));

            // Re-trigger inside ignore-window must not raise a new HighlightsChanged synchronously.
            Assert.That(changedAfter, Is.EqualTo(0));
            Assert.That(highlighter.GetPulseIntensity(placed.Segment.No), Is.GreaterThan(0));
            Assert.That(firstIntensity, Is.GreaterThan(0));
        }
    }

    [Test]
    public void Pulse_Expires_After_HoldDuration()
    {
        var plan = new EditableTrackPlan();
        var placed = new PlacedSegment(new G62(), 0, 0, 0, InPort: 5);
        plan.AddSegment(placed);

        // Very short hold so the test does not slow the suite down.
        var (highlighter, bus) = CreateHighlighter(plan,
            hold: TimeSpan.FromMilliseconds(50),
            ignore: TimeSpan.FromMilliseconds(50));
        using (highlighter)
        {
            bus.Publish(new FeedbackReceivedEvent(5));
            Assert.That(highlighter.HasActiveHighlights, Is.True);

            Thread.Sleep(150);

            // Intensity is computed on demand and must be 0 past hold duration.
            Assert.That(highlighter.GetPulseIntensity(placed.Segment.No), Is.EqualTo(0));

            // After an explicit purge the internal pulse set is also cleared.
            highlighter.PurgeExpiredPulses();
            Assert.That(highlighter.HasActiveHighlights, Is.False);
        }
    }

    [Test]
    public void Feedback_Activates_All_Segments_Sharing_Same_InPort()
    {
        var plan = new EditableTrackPlan();
        var a = new PlacedSegment(new G62(), 0, 0, 0, InPort: 9);
        var b = new PlacedSegment(new R9(), 100, 0, 0, InPort: 9);
        var c = new PlacedSegment(new G239(), 200, 0, 0, InPort: 10);
        plan.AddSegment(a);
        plan.AddSegment(b);
        plan.AddSegment(c);

        var (highlighter, bus) = CreateHighlighter(plan);
        using (highlighter)
        {
            bus.Publish(new FeedbackReceivedEvent(9));

            Assert.That(highlighter.GetPulseIntensity(a.Segment.No), Is.GreaterThan(0));
            Assert.That(highlighter.GetPulseIntensity(b.Segment.No), Is.GreaterThan(0));
            Assert.That(highlighter.GetPulseIntensity(c.Segment.No), Is.EqualTo(0));
        }
    }

    [Test]
    public void Dispose_Unsubscribes_From_EventBus()
    {
        var plan = new EditableTrackPlan();
        var placed = new PlacedSegment(new G62(), 0, 0, 0, InPort: 2);
        plan.AddSegment(placed);

        var bus = new EventBus(NullLogger<EventBus>.Instance);
        var highlighter = new TrackPlanFeedbackHighlighter(bus, plan);
        highlighter.Activate();
        highlighter.Dispose();

        bus.Publish(new FeedbackReceivedEvent(2));

        Assert.That(highlighter.GetPulseIntensity(placed.Segment.No), Is.EqualTo(0));
    }
}