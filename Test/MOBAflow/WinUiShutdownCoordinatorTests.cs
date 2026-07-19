#if WINDOWS
// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAflow;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.WinUI.Service;

[TestFixture]
internal sealed class WinUiShutdownCoordinatorTests
{
    [Test]
    public async Task ShutdownAsync_ShouldRunSequenceOnlyOnce_WhenRequestedConcurrently()
    {
        // Arrange
        var sequence = new List<string>();
        var preparationGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task PrepareApplicationAsync()
        {
            sequence.Add("prepare");
            await preparationGate.Task;
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
        await Task.WhenAll(firstShutdown, secondShutdown);
        Assert.That(sequence, Is.EqualTo(new[] { "prepare", "dispose", "exit" }));
    }

    [Test]
    public async Task ShutdownAsync_ShouldDisposeAndExit_WhenPreparationFails()
    {
        // Arrange
        var sequence = new List<string>();
        var coordinator = new WinUiShutdownCoordinator(
            () => Task.FromException(new InvalidOperationException("Preparation failed")),
            () =>
            {
                sequence.Add("dispose");
                return ValueTask.CompletedTask;
            },
            () => sequence.Add("exit"),
            NullLogger<WinUiShutdownCoordinator>.Instance);

        // Act
        await coordinator.ShutdownAsync();

        // Assert
        Assert.That(sequence, Is.EqualTo(new[] { "dispose", "exit" }));
    }
}
#endif
