namespace Moba.Test.SharedUI;

using Moba.Backend.Model;

[TestFixture]
public class WinUIAdapterDispatchTests
{
    private class TestWinUiAdapter : Moba.SharedUI.ViewModel.WinUI.JourneyViewModel
    {
        public bool Dispatched { get; private set; }
        public TestWinUiAdapter(Journey model) : base(model) {}
        protected override void Dispatch(System.Action action)
        {
            Dispatched = true; // simulate that dispatch path was used
            action();
        }
    }

    [Test]
    public void StateChanged_UsesDispatch()
    {
        var model = new Journey();
        var vm = new TestWinUiAdapter(model);

        model.CurrentCounter++;

        Assert.That(vm.Dispatched, Is.True);
        Assert.That(vm.CurrentCounter, Is.EqualTo(1u));
    }
}
