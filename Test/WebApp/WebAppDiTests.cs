namespace Moba.Test.WebApp;

using Microsoft.Extensions.DependencyInjection;
using Moba.Backend.Interface;
using Moba.Backend.Network;
using Moba.SharedUI.Service;
using Moba.SharedUI.Interface; // ✅ Factory interfaces

[TestFixture]
public class WebAppDiTests
{
    // Dummy factories for testing (no dependencies required)
    private class DummyJourneyFactory : IJourneyViewModelFactory
    {
        public Moba.SharedUI.ViewModel.JourneyViewModel Create(Moba.Backend.Model.Journey model) 
            => new Moba.SharedUI.ViewModel.JourneyViewModel(model);
    }

    private class DummyStationFactory : IStationViewModelFactory
    {
        public Moba.SharedUI.ViewModel.StationViewModel Create(Moba.Backend.Model.Station model) 
            => new Moba.SharedUI.ViewModel.StationViewModel(model);
    }

    private class DummyWorkflowFactory : IWorkflowViewModelFactory
    {
        public Moba.SharedUI.ViewModel.WorkflowViewModel Create(Moba.Backend.Model.Workflow model) 
            => new Moba.SharedUI.ViewModel.WorkflowViewModel(model);
    }

    private class DummyLocomotiveFactory : ILocomotiveViewModelFactory
    {
        public Moba.SharedUI.ViewModel.LocomotiveViewModel Create(Moba.Backend.Model.Locomotive model) 
            => new Moba.SharedUI.ViewModel.LocomotiveViewModel(model);
    }

    private class DummyTrainFactory : ITrainViewModelFactory
    {
        public Moba.SharedUI.ViewModel.TrainViewModel Create(Moba.Backend.Model.Train model) 
            => new Moba.SharedUI.ViewModel.TrainViewModel(model);
    }

    private class DummyWagonFactory : IWagonViewModelFactory
    {
        public Moba.SharedUI.ViewModel.WagonViewModel Create(Moba.Backend.Model.Wagon model) 
            => new Moba.SharedUI.ViewModel.WagonViewModel(model);
    }

    [Test]
    public void Services_AreRegistered()
    {
        var services = new ServiceCollection();

        // Backend dependencies required by Z21
        services.AddSingleton<IUdpClientWrapper, UdpWrapper>();
        services.AddSingleton<IZ21, Moba.Backend.Z21>();
        services.AddSingleton<IJourneyManagerFactory, Moba.Backend.Manager.JourneyManagerFactory>();
        
        // ✅ Solution as Singleton (required by MainWindowViewModel)
        services.AddSingleton<Moba.Backend.Model.Solution>(sp => new Moba.Backend.Model.Solution());
        
        // All ViewModel factories (using dummy implementations for testing)
        services.AddSingleton<IJourneyViewModelFactory, DummyJourneyFactory>();
        services.AddSingleton<IStationViewModelFactory, DummyStationFactory>();
        services.AddSingleton<IWorkflowViewModelFactory, DummyWorkflowFactory>();
        services.AddSingleton<ILocomotiveViewModelFactory, DummyLocomotiveFactory>();
        services.AddSingleton<ITrainViewModelFactory, DummyTrainFactory>();
        services.AddSingleton<IWagonViewModelFactory, DummyWagonFactory>();
        
        // TreeViewBuilder requires all ViewModel factories
        services.AddSingleton<TreeViewBuilder>();

        var sp = services.BuildServiceProvider();

        // Verify all services can be resolved
        Assert.That(sp.GetService<IUdpClientWrapper>(), Is.Not.Null);
        Assert.That(sp.GetService<IZ21>(), Is.Not.Null);
        Assert.That(sp.GetService<IJourneyManagerFactory>(), Is.Not.Null);
        Assert.That(sp.GetService<Moba.Backend.Model.Solution>(), Is.Not.Null);
        Assert.That(sp.GetService<IJourneyViewModelFactory>(), Is.Not.Null);
        Assert.That(sp.GetService<IStationViewModelFactory>(), Is.Not.Null);
        Assert.That(sp.GetService<IWorkflowViewModelFactory>(), Is.Not.Null);
        Assert.That(sp.GetService<ILocomotiveViewModelFactory>(), Is.Not.Null);
        Assert.That(sp.GetService<ITrainViewModelFactory>(), Is.Not.Null);
        Assert.That(sp.GetService<IWagonViewModelFactory>(), Is.Not.Null);
        Assert.That(sp.GetService<TreeViewBuilder>(), Is.Not.Null);
    }
}
