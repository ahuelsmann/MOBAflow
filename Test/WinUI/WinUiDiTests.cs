namespace Moba.Test.DI;

using Microsoft.Extensions.DependencyInjection;
using Moba.Backend.Interface;
using Moba.Backend.Network;
using Moba.SharedUI.Service;

[TestFixture]
public class ServiceRegistrationTests
{
    private class DummyIoService : IIoService
    {
        public Task<(Moba.Backend.Model.Solution? solution, string? path, string? error)> LoadAsync() => Task.FromResult<(Moba.Backend.Model.Solution?, string?, string?)>((new Moba.Backend.Model.Solution(), null, null));
        public Task<(bool success, string? path, string? error)> SaveAsync(Moba.Backend.Model.Solution solution, string? currentPath) => Task.FromResult<(bool, string?, string?)>((true, currentPath ?? "test.json", null));
    }

    private class DummyJourneyVmFactory : IJourneyViewModelFactory
    {
        public Moba.SharedUI.ViewModel.JourneyViewModel Create(Moba.Backend.Model.Journey model) => new Moba.SharedUI.ViewModel.JourneyViewModel(model);
    }

    [Test]
    public void Services_AreRegistered()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIoService, DummyIoService>();
        
        // Backend dependencies required by Z21
        services.AddSingleton<IUdpClientWrapper, UdpWrapper>();
        services.AddSingleton<IZ21, Moba.Backend.Z21>();
        services.AddSingleton<IJourneyManagerFactory, Moba.Backend.Manager.JourneyManagerFactory>();
        services.AddSingleton<IJourneyViewModelFactory, DummyJourneyVmFactory>();
        
        // TreeViewBuilder requires IJourneyViewModelFactory
        services.AddSingleton<TreeViewBuilder>();

        var sp = services.BuildServiceProvider();

        Assert.That(sp.GetService<IIoService>(), Is.Not.Null);
        Assert.That(sp.GetService<IUdpClientWrapper>(), Is.Not.Null);
        Assert.That(sp.GetService<IZ21>(), Is.Not.Null);
        Assert.That(sp.GetService<IJourneyManagerFactory>(), Is.Not.Null);
        Assert.That(sp.GetService<IJourneyViewModelFactory>(), Is.Not.Null);
        Assert.That(sp.GetService<TreeViewBuilder>(), Is.Not.Null);
    }
}
