// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Domain;
using Moba.SharedUI.Service;

[TestFixture]
public class WinUIAdapterDispatchTests
{
    private class TestUiDispatcher : IUiDispatcher
    {
        private readonly ManualResetEventSlim _dispatchedEvent = new(false);
        
        public bool Dispatched { get; private set; }
        public ManualResetEventSlim DispatchedEvent => _dispatchedEvent;
        
        public void InvokeOnUi(Action action)
        {
            Dispatched = true; // Track that dispatch was called
            action(); // Execute the action
            _dispatchedEvent.Set(); // Signal that dispatch happened
        }
    }

    [Test]
    public void StateChanged_UsesDispatch()
    {
        var model = new Journey();
        var dispatcher = new TestUiDispatcher();
        var vm = new Moba.SharedUI.ViewModel.WinUI.JourneyViewModel(model, dispatcher);

        model.CurrentCounter++;

        // Wait for the PropertyChanged event to be processed (max 1 second)
        bool signaled = dispatcher.DispatchedEvent.Wait(TimeSpan.FromSeconds(1));
        
        Assert.That(signaled, Is.True, "Dispatch was not called within timeout");
        Assert.That(dispatcher.Dispatched, Is.True);
        Assert.That(vm.CurrentCounter, Is.EqualTo(1u));
    }
}
