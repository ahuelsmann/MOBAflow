// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
#if !SKIP_ANDROID_TESTS
namespace Moba.Test.SharedUI;

using Moba.Domain;
using Moba.SharedUI.Interface;

[TestFixture]
public class MauiAdapterDispatchTests
{
    private class TestDispatcher : IUiDispatcher
    {
        public bool Dispatched { get; private set; }
        public void InvokeOnUi(Action action)
        {
            Dispatched = true; // simulate MainThread dispatch
            action();
        }
    }

    [Test]
    public void StateChanged_UsesDispatcher()
    {
        var model = new Journey();
        var dispatcher = new TestDispatcher();
        var vm = new Moba.SharedUI.ViewModel.MAUI.JourneyViewModel(model, dispatcher);

        model.CurrentCounter++;

        Assert.That(dispatcher.Dispatched, Is.True);
        Assert.That(vm.CurrentCounter, Is.EqualTo(1u));
    }
}
#endif
