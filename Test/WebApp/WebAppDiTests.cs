namespace Moba.Test.WebApp;

using Microsoft.Extensions.DependencyInjection;
using Moba.Backend.Interface;
using Moba.SharedUI.Service;

[TestFixture]
public class WebAppDiTests
{
    private class DummyFactory : IJourneyViewModelFactory
    {
        public Moba.SharedUI.ViewModel.JourneyViewModel Create(Moba.Backend.Model.Journey model) => new Moba.SharedUI.ViewModel.JourneyViewModel(model);
    }

    [Test]
    public void Services_AreRegistered()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IZ21, Moba.Backend.Z21>();
        services.AddSingleton<IJourneyManagerFactory, Moba.Backend.Manager.JourneyManagerFactory>();
        services.AddSingleton<IJourneyViewModelFactory, DummyFactory>();

        var sp = services.BuildServiceProvider();

        Assert.That(sp.GetService<IZ21>(), Is.Not.Null);
        Assert.That(sp.GetService<IJourneyManagerFactory>(), Is.Not.Null);
        Assert.That(sp.GetService<IJourneyViewModelFactory>(), Is.Not.Null);
    }
}
