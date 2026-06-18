// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Tests.Common;

using Moba.Common.Display;

[TestFixture]
public sealed class LedMatrix5x5StateTests
{
    private const uint RedArgb = 0xFFFF0000;
    private const uint BlueArgb = 0xFF0000FF;

    [Test]
    public void SetCellColorArgb_Should_Set_FirstCell_When_IndexIsZero()
    {
        var state = new LedMatrix5x5State();

        var changed = state.SetCellColorArgb(0, RedArgb);

        Assert.Multiple(() =>
        {
            Assert.That(changed, Is.True);
            Assert.That(state.GetCellColorArgb(0), Is.EqualTo(RedArgb));
        });
    }

    [Test]
    public void SetCellColorArgb_Should_Set_LastCell_When_IndexIsTwentyFour()
    {
        var state = new LedMatrix5x5State();

        var changed = state.SetCellColorArgb(24, BlueArgb);

        Assert.Multiple(() =>
        {
            Assert.That(changed, Is.True);
            Assert.That(state.GetCellColorArgb(24), Is.EqualTo(BlueArgb));
        });
    }

    [Test]
    public void ClearCellColor_Should_Set_OffState()
    {
        var state = new LedMatrix5x5State();
        state.SetCellColorArgb(12, RedArgb);

        var changed = state.ClearCellColor(12);

        Assert.Multiple(() =>
        {
            Assert.That(changed, Is.True);
            Assert.That(state.GetCellColorArgb(12), Is.EqualTo(LedMatrix5x5State.OffColorArgb));
        });
    }

    [TestCase(-1)]
    [TestCase(25)]
    public void SetCellColorArgb_Should_Ignore_InvalidIndex(int index)
    {
        var state = new LedMatrix5x5State();

        var changed = state.SetCellColorArgb(index, RedArgb);

        Assert.Multiple(() =>
        {
            Assert.That(changed, Is.False);
            Assert.That(state.GetCellColorArgb(index), Is.EqualTo(LedMatrix5x5State.OffColorArgb));
        });
    }

    [TestCase(-1)]
    [TestCase(25)]
    public void ClearCellColor_Should_Ignore_InvalidIndex(int index)
    {
        var state = new LedMatrix5x5State();

        var changed = state.ClearCellColor(index);

        Assert.That(changed, Is.False);
    }
}