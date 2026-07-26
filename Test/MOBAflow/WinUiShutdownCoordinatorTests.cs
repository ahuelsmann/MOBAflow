#if WINDOWS
// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAflow;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.WinUI.Service;

[TestFixture]
internal sealed class WinUiShutdownCoordinatorTests
{
    private static readonly string[] ExpectedRetrySequence = ["dispose", "exit"];

    [Test]
    public async Task ShutdownAsync_ShouldRunSequenceOnlyOnce_WhenRequestedConcurrently()
    {
        // Arrange
        var sequence = new List<string>();
        var preparationGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<bool> PrepareApplicationAsync()
        {
            sequence.Add("prepare");
            await preparationGate.Task;
            return true;
        }

        ValueTask DisposeServicesAsync()
        {
            sequence.Add("dispose");
            return ValueTask.CompletedTask;
        }

        var coordinator = new WinUiShutdownCoordinator(
            PrepareApplicationAsync,
            DisposeServicesAsync,
            () => sequence.Add("exit"),
            NullLogger<WinUiShutdownCoordinator>.Instance);

        // Act
        var firstShutdown = coordinator.ShutdownAsync();
        var secondShutdown = coordinator.ShutdownAsync();

        // Assert
        Assert.That(secondShutdown, Is.SameAs(firstShutdown));
        Assert.That(sequence, Is.EqualTo(new[] { "prepare" }));

        preparationGate.SetResult();
        var results = await Task.WhenAll(firstShutdown, secondShutdown);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(sequence, Is.EqualTo(new[] { "prepare", "dispose", "exit" }));
            Assert.That(results, Is.All.True);
        }
    }

    [Test]
    public async Task ShutdownAsync_ShouldDisposeAndExit_WhenPreparationFails()
    {
        // Arrange
        var sequence = new List<string>();
        var coordinator = new WinUiShutdownCoordinator(
            () => Task.FromException<bool>(new InvalidOperationException("Preparation failed")),
            () =>
            {
                sequence.Add("dispose");
                return ValueTask.CompletedTask;
            },
            () => sequence.Add("exit"),
            NullLogger<WinUiShutdownCoordinator>.Instance);

        // Act
        var result = await coordinator.ShutdownAsync();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(sequence, Is.EqualTo(new[] { "dispose", "exit" }));
            Assert.That(result, Is.True);
        }
    }

    [Test]
    public async Task ShutdownAsync_ShouldAllowRetry_WhenPreparationIsCancelled()
    {
        var prepareAttempts = 0;
        var sequence = new List<string>();
        var coordinator = new WinUiShutdownCoordinator(
            () => Task.FromResult(++prepareAttempts > 1),
            () =>
            {
                sequence.Add("dispose");
                return ValueTask.CompletedTask;
            },
            () => sequence.Add("exit"),
            NullLogger<WinUiShutdownCoordinator>.Instance);

        var firstResult = await coordinator.ShutdownAsync().ConfigureAwait(false);
        var secondResult = await coordinator.ShutdownAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(firstResult, Is.False);
            Assert.That(secondResult, Is.True);
            Assert.That(prepareAttempts, Is.EqualTo(2));
            Assert.That(sequence, Is.EqualTo(ExpectedRetrySequence));
        }
    }
}
#endif
