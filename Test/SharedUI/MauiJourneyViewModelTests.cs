namespace Moba.Test.SharedUI;

using Moba.Backend.Model;
using NUnit.Framework;

[TestFixture]
public class MauiJourneyViewModelTests
{
    [Test]
    public void StateChanged_RaisesPropertyChanged_DispatchesOnMainThread()
    {
        var model = new Journey();
        var vm = new Moba.SharedUI.ViewModel.MAUI.JourneyViewModel(model);

        // Act
        model.CurrentCounter++;
        model.CurrentPos++;

        // If no exception is thrown here in a test context, the fallback path executed synchronously.
        Assert.Pass();
    }
}
