#if WINDOWS
// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAflow;

using Microsoft.UI.Dispatching;

using Moba.WinUI.Service;

[TestFixture]
internal sealed class UiDispatcherTests
{
    [Test]
    public void InvokeOnUi_ShouldExecuteImmediately_WhenCallerHasThreadAccess()
    {
        // Arrange
        var queue = new TestDispatcherQueue(hasThreadAccess: true);
        var dispatcher = new UiDispatcher(queue);
        var wasInvoked = false;

        // Act
        dispatcher.InvokeOnUi(() => wasInvoked = true);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(wasInvoked, Is.True);
            Assert.That(queue.PendingCount, Is.Zero);
        });
    }

    [Test]
    public void InvokeOnUi_ShouldQueueAndReturn_WhenCallerDoesNotHaveThreadAccess()
    {
        // Arrange
        var queue = new TestDispatcherQueue(hasThreadAccess: false);
        var dispatcher = new UiDispatcher(queue);
        var invocationOrder = new List<int>();

        // Act
        dispatcher.InvokeOnUi(() => invocationOrder.Add(1));
        dispatcher.InvokeOnUi(() => invocationOrder.Add(2));

        // Assert
        Assert.That(invocationOrder, Is.Empty);

        queue.RunAll();
        Assert.That(invocationOrder, Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void InvokeOnUiLowPriority_ShouldPreserveRequestedPriority()
    {
        // Arrange
        var queue = new TestDispatcherQueue(hasThreadAccess: false);
        var dispatcher = new UiDispatcher(queue);

        // Act
        dispatcher.InvokeOnUiLowPriority(() => { });

        // Assert
        Assert.That(queue.LastPriority, Is.EqualTo(DispatcherQueuePriority.Low));
    }

    [Test]
    public async Task InvokeOnUiAsync_ShouldCompleteAfterQueuedAction()
    {
        // Arrange
        var queue = new TestDispatcherQueue(hasThreadAccess: false);
        var dispatcher = new UiDispatcher(queue);
        var wasInvoked = false;

        // Act
        var invocationTask = dispatcher.InvokeOnUiAsync(() =>
        {
            wasInvoked = true;
            return Task.CompletedTask;
        });

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(invocationTask.IsCompleted, Is.False);
            Assert.That(wasInvoked, Is.False);
        });

        queue.RunAll();
        await invocationTask;
        Assert.That(wasInvoked, Is.True);
    }

    private sealed class TestDispatcherQueue : IWinUiDispatcherQueue
    {
        private readonly Queue<(DispatcherQueuePriority Priority, Action Action)> _pendingActions = new();

        public TestDispatcherQueue(bool hasThreadAccess)
        {
            HasThreadAccess = hasThreadAccess;
        }

        public bool HasThreadAccess { get; }

        public DispatcherQueuePriority? LastPriority { get; private set; }

        public int PendingCount => _pendingActions.Count;

        public bool TryEnqueue(DispatcherQueuePriority priority, Action action)
        {
            LastPriority = priority;
            _pendingActions.Enqueue((priority, action));
            return true;
        }

        public void RunAll()
        {
            while (_pendingActions.TryDequeue(out var pendingAction))
            {
                pendingAction.Action();
            }
        }
    }
}
#endif
