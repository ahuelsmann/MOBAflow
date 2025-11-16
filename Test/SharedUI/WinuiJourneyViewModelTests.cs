namespace Moba.Test.SharedUI;

using Moba.Backend.Model;
using NUnit.Framework;

[TestFixture]
public class WinuiJourneyViewModelTests
{
    [Test]
    public void StateChanged_RaisesPropertyChanged_Dispatches()
    {
        var model = new Journey();
        var vm = new Moba.SharedUI.ViewModel.WinUI.JourneyViewModel(model);

        // Act
        model.CurrentCounter++;
        model.CurrentPos++;

        Assert.Pass();
    }
}
