namespace Moba.Test.WebApp;

using Microsoft.Extensions.DependencyInjection;
using Moba.Backend.Interface;
using Moba.Backend.Network;
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

        // Backend dependencies required by Z21
        services.AddSingleton<IUdpClientWrapper, UdpWrapper>();
        services.AddSingleton<IZ21, Moba.Backend.Z21>();
        services.AddSingleton<IJourneyManagerFactory, Moba.Backend.Manager.JourneyManagerFactory>();
        services.AddSingleton<IJourneyViewModelFactory, DummyFactory>();
        
        // TreeViewBuilder requires IJourneyViewModelFactory
        services.AddSingleton<TreeViewBuilder>();

        var sp = services.BuildServiceProvider();

        Assert.That(sp.GetService<IUdpClientWrapper>(), Is.Not.Null);
        Assert.That(sp.GetService<IZ21>(), Is.Not.Null);
        Assert.That(sp.GetService<IJourneyManagerFactory>(), Is.Not.Null);
        Assert.That(sp.GetService<IJourneyViewModelFactory>(), Is.Not.Null);
        Assert.That(sp.GetService<TreeViewBuilder>(), Is.Not.Null);
    }
}
