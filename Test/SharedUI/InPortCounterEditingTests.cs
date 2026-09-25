// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;
using Moq;

[TestFixture]
public sealed class InPortCounterEditingTests
{
    [Test]
    public async Task Editing_PreservesFullIntegerPrecisionAndDoesNotCreateFeedback()
    {
        var commands = new Mock<IRuntimeCommandGateway>(MockBehavior.Strict);
        commands.Setup(value => value.SetInPortCounterAsync(2, ulong.MaxValue, default)).Returns(Task.CompletedTask);
        commands.Setup(value => value.ResetInPortCounterAsync(2, default)).Returns(Task.CompletedTask);
        var row = new InPortStatistic(commands.Object) { InPort = 2, CounterValue = "18446744073709551615" };
        row.Count = 3;
        Assert.That(row.CounterValue, Is.EqualTo("18446744073709551615"), "Feedback must not overwrite an edit in progress.");
        await row.SetCounterCommand.ExecuteAsync(null);
        await row.ResetCounterCommand.ExecuteAsync(null);
        Assert.That(row.CounterError, Is.Empty);
        commands.VerifyAll();
        commands.VerifyNoOtherCalls();
    }

    [TestCase("")]
    [TestCase("-1")]
    [TestCase("1.5")]
    [TestCase("1e3")]
    [TestCase("18446744073709551616")]
    public async Task InvalidInput_DoesNotReachRuntime(string input)
    {
        var commands = new Mock<IRuntimeCommandGateway>(MockBehavior.Strict);
        var row = new InPortStatistic(commands.Object) { InPort = 1, CounterValue = input };
        await row.SetCounterCommand.ExecuteAsync(null);
        Assert.That(row.CounterError, Is.Not.Empty);
        commands.VerifyNoOtherCalls();
    }

    [Test]
    public async Task FailedSave_IsShownAndRetainsTheEnteredValue()
    {
        var commands = new Mock<IRuntimeCommandGateway>();
        commands.Setup(value => value.SetInPortCounterAsync(1, 14, default)).ThrowsAsync(new IOException("Counter save failed."));
        var row = new InPortStatistic(commands.Object) { InPort = 1, CounterValue = "14" };
        await row.SetCounterCommand.ExecuteAsync(null);
        Assert.Multiple(() =>
        {
            Assert.That(row.CounterError, Is.EqualTo("Counter save failed."));
            Assert.That(row.CounterValue, Is.EqualTo("14"));
        });
    }
}
