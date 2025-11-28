using Microsoft.Extensions.DependencyInjection;
using Moba.Backend.Interface;
using Moba.Backend.Network;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;

namespace Moba.Test.MAUI;

/// <summary>
/// Tests for MAUI Dependency Injection configuration.
/// Verifies that all services are properly registered and can be resolved.
/// </summary>
[TestFixture]
[Category("DI")]
public class MauiDiTests
{
    private IServiceProvider _serviceProvider = null!;

    [SetUp]
    public void SetUp()
    {
        // Register all services exactly as in MauiProgram.cs
        var services = new ServiceCollection();

        // Platform services
        services.AddSingleton<IUiDispatcher, DummyUiDispatcher>();
        services.AddSingleton<INotificationService, DummyNotificationService>();

        // Backend services
        services.AddSingleton<IUdpClientWrapper, UdpWrapper>();
        services.AddSingleton<IZ21, Moba.Backend.Z21>();
        services.AddSingleton<IJourneyManagerFactory, Moba.Backend.Manager.JourneyManagerFactory>();

        // Data and Solution
        services.AddSingleton(sp => new Moba.Backend.Data.DataManager());
        services.AddSingleton<Moba.Backend.Model.Solution>(sp => new Moba.Backend.Model.Solution());

        // ViewModel Factories (using dummy implementations for testing)
        services.AddSingleton<IJourneyViewModelFactory, DummyJourneyFactory>();
        services.AddSingleton<IStationViewModelFactory, DummyStationFactory>();
        services.AddSingleton<IWorkflowViewModelFactory, DummyWorkflowFactory>();
        services.AddSingleton<ILocomotiveViewModelFactory, DummyLocomotiveFactory>();
        services.AddSingleton<ITrainViewModelFactory, DummyTrainFactory>();
        services.AddSingleton<IWagonViewModelFactory, DummyWagonFactory>();

        // SharedUI services
        services.AddSingleton<TreeViewBuilder>();

        _serviceProvider = services.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    #region Core Services Tests

    [Test]
    public void UiDispatcher_ShouldBeRegistered()
    {
        var service = _serviceProvider.GetService<IUiDispatcher>();
        Assert.That(service, Is.Not.Null);
        Assert.That(service, Is.InstanceOf<DummyUiDispatcher>());
    }

    [Test]
    public void NotificationService_ShouldBeRegistered()
    {
        var service = _serviceProvider.GetService<INotificationService>();
        Assert.That(service, Is.Not.Null);
        Assert.That(service, Is.InstanceOf<DummyNotificationService>());
    }

    #endregion

    #region Backend Services Tests

    [Test]
    public void UdpClientWrapper_ShouldBeRegistered()
    {
        var service = _serviceProvider.GetService<IUdpClientWrapper>();
        Assert.That(service, Is.Not.Null);
        Assert.That(service, Is.InstanceOf<UdpWrapper>());
    }

    [Test]
    public void Z21_ShouldBeRegistered()
    {
        var service = _serviceProvider.GetService<IZ21>();
        Assert.That(service, Is.Not.Null);
        Assert.That(service, Is.InstanceOf<Moba.Backend.Z21>());
    }

    [Test]
    public void JourneyManagerFactory_ShouldBeRegistered()
    {
        var service = _serviceProvider.GetService<IJourneyManagerFactory>();
        Assert.That(service, Is.Not.Null);
        Assert.That(service, Is.InstanceOf<Moba.Backend.Manager.JourneyManagerFactory>());
    }

    [Test]
    public void DataManager_ShouldBeRegistered()
    {
        var service = _serviceProvider.GetService<Moba.Backend.Data.DataManager>();
        Assert.That(service, Is.Not.Null);
    }

    [Test]
    public void Solution_ShouldBeRegistered()
    {
        var service = _serviceProvider.GetService<Moba.Backend.Model.Solution>();
        Assert.That(service, Is.Not.Null);
    }

    [Test]
    public void Solution_ShouldBeSingleton()
    {
        var instance1 = _serviceProvider.GetService<Moba.Backend.Model.Solution>();
        var instance2 = _serviceProvider.GetService<Moba.Backend.Model.Solution>();
        
        Assert.That(instance1, Is.SameAs(instance2), "Solution should be a singleton");
    }

    #endregion

    #region ViewModel Factory Tests

    [Test]
    public void JourneyViewModelFactory_ShouldBeRegistered()
    {
        var factory = _serviceProvider.GetService<IJourneyViewModelFactory>();
        Assert.That(factory, Is.Not.Null);
        Assert.That(factory, Is.InstanceOf<DummyJourneyFactory>());
    }

    [Test]
    public void StationViewModelFactory_ShouldBeRegistered()
    {
        var factory = _serviceProvider.GetService<IStationViewModelFactory>();
        Assert.That(factory, Is.Not.Null);
        Assert.That(factory, Is.InstanceOf<DummyStationFactory>());
    }

    [Test]
    public void WorkflowViewModelFactory_ShouldBeRegistered()
    {
        var factory = _serviceProvider.GetService<IWorkflowViewModelFactory>();
        Assert.That(factory, Is.Not.Null);
        Assert.That(factory, Is.InstanceOf<DummyWorkflowFactory>());
    }

    [Test]
    public void LocomotiveViewModelFactory_ShouldBeRegistered()
    {
        var factory = _serviceProvider.GetService<ILocomotiveViewModelFactory>();
        Assert.That(factory, Is.Not.Null);
        Assert.That(factory, Is.InstanceOf<DummyLocomotiveFactory>());
    }

    [Test]
    public void TrainViewModelFactory_ShouldBeRegistered()
    {
        var factory = _serviceProvider.GetService<ITrainViewModelFactory>();
        Assert.That(factory, Is.Not.Null);
        Assert.That(factory, Is.InstanceOf<DummyTrainFactory>());
    }

    [Test]
    public void WagonViewModelFactory_ShouldBeRegistered()
    {
        var factory = _serviceProvider.GetService<IWagonViewModelFactory>();
        Assert.That(factory, Is.Not.Null);
        Assert.That(factory, Is.InstanceOf<DummyWagonFactory>());
    }

    [Test]
    public void AllFactories_ShouldBeRegistered()
    {
        Assert.DoesNotThrow(() =>
        {
            _serviceProvider.GetRequiredService<IJourneyViewModelFactory>();
            _serviceProvider.GetRequiredService<IStationViewModelFactory>();
            _serviceProvider.GetRequiredService<IWorkflowViewModelFactory>();
            _serviceProvider.GetRequiredService<ILocomotiveViewModelFactory>();
            _serviceProvider.GetRequiredService<ITrainViewModelFactory>();
            _serviceProvider.GetRequiredService<IWagonViewModelFactory>();
        });
    }

    #endregion

    #region SharedUI Services Tests

    [Test]
    public void TreeViewBuilder_ShouldBeRegistered()
    {
        var service = _serviceProvider.GetService<TreeViewBuilder>();
        Assert.That(service, Is.Not.Null);
    }

    [Test]
    public void TreeViewBuilder_ShouldResolveWithFactories()
    {
        // TreeViewBuilder requires all ViewModel factories
        Assert.DoesNotThrow(() =>
        {
            var builder = _serviceProvider.GetRequiredService<TreeViewBuilder>();
            Assert.That(builder, Is.Not.Null);
        });
    }

    #endregion

    #region Integration Tests

    [Test]
    public void AllCriticalServices_ShouldResolve()
    {
        // Verify all critical services can be resolved together
        Assert.DoesNotThrow(() =>
        {
            _serviceProvider.GetRequiredService<IZ21>();
            _serviceProvider.GetRequiredService<Moba.Backend.Model.Solution>();
            _serviceProvider.GetRequiredService<IJourneyViewModelFactory>();
            _serviceProvider.GetRequiredService<TreeViewBuilder>();
        });
    }

    #endregion

    #region Dummy Implementations for Testing

    // Dummy implementations to avoid platform-specific dependencies in tests

    private class DummyUiDispatcher : IUiDispatcher
    {
        public void InvokeOnUi(Action action) => action();
    }

    private class DummyNotificationService : INotificationService
    {
        public void PlayNotificationSound()
        {
            // No-op for testing
        }
    }

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

    #endregion
}
