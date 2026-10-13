// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.SharedUI.TrainControl;
using Moba.Test.Helpers;

/// <summary>
/// Drive debounce, speed ramp and local-command grace period of the train-control speed controller.
/// </summary>
[TestFixture]
internal sealed class SpeedControllerTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);
    private static readonly int[] RampUpSteps = [5, 10, 12];
    private static readonly int[] RampDownSteps = [80, 60, 40, 20, 0];
    private static readonly int[] ClampedSteps = [1, 2];
    private static readonly string[] SecondOnly = ["second"];

    [Test]
    public void QueueDrive_SendsOnlyTheLastChangeOnce75MsAfterIt()
    {
        var clock = new ManualTimeProvider(Start);
        using var controller = new SpeedController(clock);
        var sent = new List<string>();

        controller.QueueDrive(() => sent.Add("first"));
        clock.Advance(TimeSpan.FromMilliseconds(50));
        controller.QueueDrive(() => sent.Add("second"));
        clock.Advance(TimeSpan.FromMilliseconds(74));
        var beforeDebounce = sent.Count;
        clock.Advance(TimeSpan.FromMilliseconds(1));

        Assert.That(SpinWait.SpinUntil(() => sent.Count == 1, TimeSpan.FromSeconds(5)), Is.True);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(beforeDebounce, Is.Zero);
            Assert.That(sent, Is.EqualTo(SecondOnly));
        }
    }

    [Test]
    public void Dispose_DropsAPendingDrive()
    {
        var clock = new ManualTimeProvider(Start);
        var sent = 0;
        var controller = new SpeedController(clock);
        controller.QueueDrive(() => sent++);

        controller.Dispose();
        clock.Advance(TimeSpan.FromSeconds(1));

        Assert.That(sent, Is.Zero);
    }

    [Test]
    public async Task RampAsync_StepsUpAndStopsExactlyAtTheTarget()
    {
        var clock = new ImmediateTimeProvider();
        using var controller = new SpeedController(clock);
        var steps = new List<int>();

        await controller.RampAsync(0, 12, stepSize: 5, intervalMs: 100, RecordInto(steps), CancellationToken.None).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(steps, Is.EqualTo(RampUpSteps));
            Assert.That(clock.Delays, Is.EqualTo(new[] { TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100) }));
        }
    }

    [Test]
    public async Task RampAsync_StepsDownAndClampsStepAndInterval()
    {
        var down = new ImmediateTimeProvider();
        var clamped = new ImmediateTimeProvider();
        using var downController = new SpeedController(down);
        using var clampedController = new SpeedController(clamped);
        var downSteps = new List<int>();
        var clampedSteps = new List<int>();

        await downController.RampAsync(100, 0, stepSize: 50, intervalMs: 10, RecordInto(downSteps), CancellationToken.None).ConfigureAwait(false);
        await clampedController.RampAsync(0, 2, stepSize: 0.2, intervalMs: 900, RecordInto(clampedSteps), CancellationToken.None).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(downSteps, Is.EqualTo(RampDownSteps));
            Assert.That(down.Delays, Is.All.EqualTo(TimeSpan.FromMilliseconds(50)));
            Assert.That(clampedSteps, Is.EqualTo(ClampedSteps));
            Assert.That(clamped.Delays, Is.All.EqualTo(TimeSpan.FromMilliseconds(500)));
        }
    }

    [Test]
    public void BeginRamp_CancelsTheRunningRamp()
    {
        using var controller = new SpeedController(TimeProvider.System);

        var first = controller.BeginRamp();
        var second = controller.BeginRamp();
        var secondCancelledEarly = second.IsCancellationRequested;
        controller.CancelRamp();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first.IsCancellationRequested, Is.True);
            Assert.That(secondCancelledEarly, Is.False);
            Assert.That(second.IsCancellationRequested, Is.True);
        }
    }

    [Test]
    public async Task RampAsync_StopsWhenCancelled()
    {
        using var controller = new SpeedController(TimeProvider.System);
        var token = controller.BeginRamp();
        controller.CancelRamp();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            controller.RampAsync(0, 10, 5, 100, _ => Task.CompletedTask, token)).ConfigureAwait(false);
    }

    [Test]
    public void ShouldPreserveLocalDrive_HoldsForThreeSecondsAfterALocalCommand()
    {
        var clock = new ManualTimeProvider(Start);
        using var controller = new SpeedController(clock);

        controller.MarkLocalDrive(3);
        controller.MarkLocalDrive(0);
        var justSent = controller.ShouldPreserveLocalDrive(3);
        clock.Advance(TimeSpan.FromSeconds(3));
        var atThreeSeconds = controller.ShouldPreserveLocalDrive(3);
        clock.Advance(TimeSpan.FromMilliseconds(1));
        var afterwards = controller.ShouldPreserveLocalDrive(3);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(justSent, Is.True);
            Assert.That(atThreeSeconds, Is.True);
            Assert.That(afterwards, Is.False);
            Assert.That(controller.ShouldPreserveLocalDrive(0), Is.False);
            Assert.That(controller.ShouldPreserveLocalDrive(4), Is.False);
        }
    }

    private static Func<int, Task> RecordInto(List<int> steps) => speed =>
    {
        steps.Add(speed);
        return Task.CompletedTask;
    };

    /// <summary>A clock whose delays complete at once and are recorded.</summary>
    private sealed class ImmediateTimeProvider : TimeProvider
    {
        public List<TimeSpan> Delays { get; } = [];

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            Delays.Add(dueTime);
            return System.CreateTimer(callback, state, TimeSpan.Zero, Timeout.InfiniteTimeSpan);
        }
    }
}
