using Moba.SharedUI.ViewModel;

namespace Moba.Test.SharedUI;

public class CounterViewModelTests
{
    [Test]
    public void CounterViewModel_InitializesStatistics()
    {
        var vm = new CounterViewModel();
        Assert.That(vm.Statistics, Is.Not.Null);
        Assert.That(vm.Statistics.Count, Is.EqualTo(3));
        Assert.That(vm.Statistics[0].InPort, Is.EqualTo(1));
    }

    [Test]
    public void ResetCounters_ClearsCounts()
    {
        var vm = new CounterViewModel();
        vm.Statistics[0].Count = 5;
        vm.Statistics[1].Count = 3;

        // Execute command
        vm.ResetCountersCommand.Execute(null);

        Assert.That(vm.Statistics[0].Count, Is.EqualTo(0));
        Assert.That(vm.Statistics[1].Count, Is.EqualTo(0));
    }
}
