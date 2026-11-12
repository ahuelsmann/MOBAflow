// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.SharedUI.TrainControl;
using Moba.Test.Helpers;

/// <summary>
/// Function bits per address, local-command grace period and all-functions-off runs.
/// </summary>
[TestFixture]
internal sealed class FunctionPanelTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

    private ManualTimeProvider _clock = null!;

    [SetUp]
    public void SetUp()
    {
        _clock = new ManualTimeProvider(Start);
    }

    [Test]
    public void RememberBits_KeepsTheLastBitsPerAddress()
    {
        using var panel = new FunctionPanel(_clock);
        panel.RememberBits(3, 0b101);
        panel.RememberBits(3, 0b011);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(panel.TryGetRememberedBits(3, out var bits), Is.True);
            Assert.That(bits, Is.EqualTo(0b011u));
            Assert.That(panel.TryGetRememberedBits(4, out _), Is.False);
        }
    }

    [Test]
    public void ShouldPreserveLocalFunction_HoldsForThreeSecondsPerFunction()
    {
        using var panel = new FunctionPanel(_clock);
        panel.MarkLocalFunction(3, 1);
        panel.MarkLocalFunction(0, 1);
        panel.MarkLocalFunction(3, 32);
        var justSwitched = panel.ShouldPreserveLocalFunction(3, 1);
        var otherFunction = panel.ShouldPreserveLocalFunction(3, 2);
        _clock.Advance(TimeSpan.FromSeconds(3));
        var atThreeSeconds = panel.ShouldPreserveLocalFunction(3, 1);
        _clock.Advance(TimeSpan.FromMilliseconds(1));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(justSwitched, Is.True);
            Assert.That(otherFunction, Is.False);
            Assert.That(atThreeSeconds, Is.True);
            Assert.That(panel.ShouldPreserveLocalFunction(3, 1), Is.False);
            Assert.That(panel.ShouldPreserveLocalFunction(0, 1), Is.False);
            Assert.That(panel.ShouldPreserveLocalFunction(3, 32), Is.False);
        }
    }

    [Test]
    public void MergeSnapshotBits_KeepsLocallySwitchedFunctionsAndTakesTheRest()
    {
        using var panel = new FunctionPanel(_clock);
        panel.MarkLocalFunction(3, 0);

        // F0 was switched on locally; the snapshot still reports it off and F1/F2 changed on the decoder.
        var merged = panel.MergeSnapshotBits(3, shownBits: 0b001, snapshotBits: 0b110);

        Assert.That(merged, Is.EqualTo(0b111u));
    }

    [Test]
    public void BeginAllFunctionsOff_SuppressesSnapshotsAndCancelsAPreviousRun()
    {
        using var panel = new FunctionPanel(_clock);
        var (firstVersion, firstToken) = panel.BeginAllFunctionsOff();
        var (secondVersion, secondToken) = panel.BeginAllFunctionsOff();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(panel.SuppressSnapshotBits, Is.True);
            Assert.That(firstToken.IsCancellationRequested, Is.True);
            Assert.That(secondToken.IsCancellationRequested, Is.False);
            Assert.That(secondVersion, Is.EqualTo(firstVersion));
            Assert.That(panel.IsResetCurrent(secondVersion), Is.True);
        }
    }

    [Test]
    public void InvalidatePendingResets_MakesARunningResetStaleAndCancelsIt()
    {
        using var panel = new FunctionPanel(_clock);
        var (version, token) = panel.BeginAllFunctionsOff();

        panel.InvalidatePendingResets();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(panel.IsResetCurrent(version), Is.False);
            Assert.That(token.IsCancellationRequested, Is.True);
        }
    }
}
