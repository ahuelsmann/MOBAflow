// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.SharedUI.TrainControl;
using Moba.Test.Helpers;

/// <summary>
/// Button and indicator rules of the parking brake and door release, and the timed door transition.
/// </summary>
[TestFixture]
internal sealed class BrakeAndDoorStateTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(100);
    private static readonly double[] FirstBlinks = [0.35, 1.0];
    private static readonly bool[] BlockedOnce = [true];

    [Test]
    public void BrakeRules_FollowTheBrakeAndDoorState()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(BrakeAndDoorState.GetBrakeButtonColor(isBrakeApplied: true), Is.EqualTo(TrainControlSignalColors.Red));
            Assert.That(BrakeAndDoorState.GetBrakeButtonColor(isBrakeApplied: false), Is.EqualTo(TrainControlSignalColors.Green));
            Assert.That(BrakeAndDoorState.GetBrakeButtonGlyph(isBrakeApplied: true), Is.EqualTo(""));
            Assert.That(BrakeAndDoorState.IsBrakeButtonEnabled(isBrakeApplied: true, isDoorLocked: false), Is.False);
            Assert.That(BrakeAndDoorState.IsBrakeButtonEnabled(isBrakeApplied: true, isDoorLocked: true), Is.True);
            Assert.That(BrakeAndDoorState.CanIncreaseSpeed(isBrakeApplied: false, isDoorLocked: true), Is.True);
            Assert.That(BrakeAndDoorState.CanIncreaseSpeed(isBrakeApplied: false, isDoorLocked: false), Is.False);
        }
    }

    [TestCase(false, false, true, 0, TrainControlSignalColors.White)]
    [TestCase(true, true, true, 0, TrainControlSignalColors.Yellow)]
    [TestCase(true, false, false, 0, TrainControlSignalColors.Green)]
    [TestCase(true, false, false, 5, TrainControlSignalColors.Red)]
    [TestCase(true, false, true, 0, TrainControlSignalColors.Red)]
    public void GetDoorButtonColor_ShowsTheDoorState(bool brake, bool blinking, bool locked, int speedKmh, string expected) =>
        Assert.That(BrakeAndDoorState.GetDoorButtonColor(brake, blinking, locked, speedKmh), Is.EqualTo(expected));

    [Test]
    public void DoorRules_FollowBrakeSpeedAndTransition()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(BrakeAndDoorState.IsDoorButtonEnabled(isBrakeApplied: true, speedKmh: 0, isBlinking: false), Is.True);
            Assert.That(BrakeAndDoorState.IsDoorButtonEnabled(isBrakeApplied: true, speedKmh: 1, isBlinking: false), Is.False);
            Assert.That(BrakeAndDoorState.IsDoorButtonEnabled(isBrakeApplied: true, speedKmh: 0, isBlinking: true), Is.False);
            Assert.That(BrakeAndDoorState.IsDoorCloseIconVisible(isBlinking: true, isDoorLockedNext: true, isDoorLocked: false), Is.True);
            Assert.That(BrakeAndDoorState.IsDoorCloseIconVisible(isBlinking: false, isDoorLockedNext: true, isDoorLocked: false), Is.False);
            Assert.That(BrakeAndDoorState.GetDoorBlockedColor(isDoorBlocked: true), Is.EqualTo(TrainControlSignalColors.Yellow));
            Assert.That(BrakeAndDoorState.GetDoorBlockedIconColor(isDoorBlocked: false), Is.EqualTo(TrainControlSignalColors.Gray));
        }
    }

    [Test]
    public void NextClosingBlockSeconds_DrawsBetweenZeroAndSixtySeconds()
    {
        (int Min, int Max)? range = null;
        using var state = new BrakeAndDoorState(TimeProvider.System, (min, max) =>
        {
            range = (min, max);
            return 42;
        });

        var seconds = state.NextClosingBlockSeconds();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(seconds, Is.EqualTo(42));
            Assert.That(range, Is.EqualTo((0, 61)));
        }
    }

    [Test]
    public async Task RunDoorTransitionAsync_OpensAfterFiveSecondsOfBlinking()
    {
        var clock = new ManualTimeProvider(Start);
        using var state = new BrakeAndDoorState(clock);
        var opacities = new List<double>();
        var blocked = new List<bool>();

        var transition = state.RunDoorTransitionAsync(isClosing: false, 0, opacities.Add, blocked.Add);
        await AdvanceAsync(clock, TimeSpan.FromSeconds(4.9)).ConfigureAwait(false);
        var doneEarly = transition.IsCompleted;
        await AdvanceAsync(clock, TimeSpan.FromSeconds(0.5)).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(doneEarly, Is.False);
            Assert.That(await transition.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false), Is.True);
            Assert.That(opacities.Take(2), Is.EqualTo(FirstBlinks));
            Assert.That(blocked, Is.Empty);
        }
    }

    [Test]
    public async Task RunDoorTransitionAsync_BlocksAClosingDoorAfterFiveSeconds()
    {
        var clock = new ManualTimeProvider(Start);
        using var state = new BrakeAndDoorState(clock);
        var blocked = new List<bool>();

        var transition = state.RunDoorTransitionAsync(isClosing: true, blockingSeconds: 2, _ => { }, blocked.Add);
        await AdvanceAsync(clock, TimeSpan.FromSeconds(5.1)).ConfigureAwait(false);
        var blockedAfterFiveSeconds = blocked.ToList();
        var doneEarly = transition.IsCompleted;
        await AdvanceAsync(clock, TimeSpan.FromSeconds(2.5)).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(blockedAfterFiveSeconds, Is.EqualTo(BlockedOnce));
            Assert.That(doneEarly, Is.False);
            Assert.That(await transition.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false), Is.True);
        }
    }

    [Test]
    public async Task RunDoorTransitionAsync_ANewTransitionCancelsTheRunningOne()
    {
        var clock = new ManualTimeProvider(Start);
        using var state = new BrakeAndDoorState(clock);

        var first = state.RunDoorTransitionAsync(isClosing: false, 0, _ => { }, _ => { });
        var second = state.RunDoorTransitionAsync(isClosing: false, 0, _ => { }, _ => { });
        var firstResult = await first.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        await AdvanceAsync(clock, TimeSpan.FromSeconds(5.5)).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(firstResult, Is.False);
            Assert.That(await second.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false), Is.True);
        }
    }

    /// <summary>Advances the clock in small steps so each step's continuations can register their next timer.</summary>
    private static async Task AdvanceAsync(ManualTimeProvider clock, TimeSpan total)
    {
        for (var elapsed = TimeSpan.Zero; elapsed < total; elapsed += Step)
        {
            clock.Advance(Step);
            await Task.Delay(5).ConfigureAwait(false);
        }
    }
}
