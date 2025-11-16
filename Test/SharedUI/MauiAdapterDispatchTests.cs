namespace Moba.Test.SharedUI;

using Moba.Backend.Model;

[TestFixture]
public class MauiAdapterDispatchTests
{
    private class TestMauiAdapter : Moba.SharedUI.ViewModel.MAUI.JourneyViewModel
    {
        public bool Dispatched { get; private set; }
        public TestMauiAdapter(Journey model) : base(model) {}
        protected override void Dispatch(System.Action action)
        {
            Dispatched = true; // simulate MainThread dispatch
            action();
        }
    }

    [Test]
    public void StateChanged_UsesDispatch()
    {
        var model = new Journey();
        var vm = new TestMauiAdapter(model);

        model.CurrentCounter++;

        Assert.That(vm.Dispatched, Is.True);
        Assert.That(vm.CurrentCounter, Is.EqualTo(1u));
    }
}
