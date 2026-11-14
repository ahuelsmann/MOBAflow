// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.SharedUI;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Domain;
using Moba.SharedUI.Extensions;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Mobile;
using Moba.SharedUI.Service;
using Moba.SharedUI.TrainControl;
using Moba.SharedUI.ViewModel;
using Moba.Test.Helpers;

using Moq;

/// <summary>
/// The platform-neutral MOBAsmart composition resolves completely once the host supplies its platform services.
/// MOBAsmart itself targets Android only, so its own registrations (Android implementations and pages) run on a
/// device; this fixture covers everything the app shares with the portable test target.
/// </summary>
[TestFixture]
internal sealed class MobileServiceCollectionExtensionsTests
{
    /// <summary>The services <c>MobaMauiServiceCollectionExtensions</c> registers with Android implementations.</summary>
    private static readonly Type[] HostPlatformServices =
    [
        typeof(IUiDispatcher),
        typeof(ISettingsService),
        typeof(IBackgroundService),
        typeof(IRestDiscoveryService),
        typeof(IPhotoUploadService),
        typeof(IPhotoCaptureService),
        typeof(IRestApiClientRegistration),
        typeof(INetworkProfileChangeNotifier),
        typeof(IRuntimeHubRemoteClient)
    ];

    private string _appData = string.Empty;

    [SetUp]
    public void SetUp() =>
        _appData = Path.Combine(Path.GetTempPath(), "moba-mobile-di-" + Guid.NewGuid().ToString("N"));

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_appData))
            Directory.Delete(_appData, recursive: true);
    }

    [Test]
    public async Task EveryRegistrationResolvesOnceTheHostSuppliesItsPlatformServices()
    {
        using var httpClient = new HttpClient();
        var services = CreateServices(httpClient);
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        await using var providerLifetime = provider.ConfigureAwait(false);

        var failures = ServiceRegistrations.ResolveAll(services, provider, skip: _ => false, out var resolved);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(failures, Is.Empty);
            Assert.That(resolved, Is.GreaterThan(80), "The composition registers the backend, the remote runtime and the ViewModels.");
        }
    }

    [Test]
    public async Task StartupValidationPassesAndTheRuntimeAliasesShareTheirOwners()
    {
        using var httpClient = new HttpClient();
        var provider = CreateServices(httpClient).BuildServiceProvider();
        await using var providerLifetime = provider.ConfigureAwait(false);

        Assert.DoesNotThrow(provider.ValidateMobaMobileServices);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(provider.GetRequiredService<IProjectContext>(), Is.SameAs(provider.GetRequiredService<MobileSolutionContext>()));
            Assert.That(provider.GetRequiredService<ISolutionRemoteLoader>(), Is.SameAs(provider.GetRequiredService<SolutionRemoteLoader>()));
            Assert.That(provider.GetRequiredService<IRuntimeCommandGateway>(), Is.InstanceOf<RecordingRuntimeCommandGateway>());
            Assert.That(provider.GetRequiredService<IMobileRuntimeCoordinator>(), Is.SameAs(provider.GetRequiredService<MobileRuntimeCoordinator>()));
            Assert.That(provider.GetRequiredService<IRuntimeRoutingState>(), Is.SameAs(provider.GetRequiredService<IMobileRuntimeCoordinator>()));
            Assert.That(provider.GetRequiredService<IMobaRuntime>(), Is.InstanceOf<MobaRuntimeService>());
            Assert.That(provider.GetRequiredService<TrainControlViewModelOptions>().Host, Is.EqualTo(TrainControlHost.Maui));
            Assert.That(provider.GetRequiredService<MobiStartupService>(), Is.Not.Null);
        }
    }

    [Test]
    public async Task TheRuntimeConnectsToTheZ21OfTheProjectSelectedOnThePhone()
    {
        using var httpClient = new HttpClient();
        var provider = CreateServices(httpClient).BuildServiceProvider();
        await using var providerLifetime = provider.ConfigureAwait(false);
        var endpoint = provider.GetRequiredService<IZ21EndpointSource>();
        var nothingSelected = await endpoint.ResolveAsync().ConfigureAwait(false);

        provider.GetRequiredService<MobileSolutionContext>().SelectedProject = new ProjectViewModel(
            new Project { Name = "Station", Z21 = new Z21Endpoint { IpAddress = "192.168.0.111" } },
            provider.GetRequiredService<SolutionViewModelServices>());
        var selected = await endpoint.ResolveAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(nothingSelected.Address, Is.Null);
            Assert.That(selected.Address?.ToString(), Is.EqualTo("192.168.0.111"));
        }
    }

    [TestCaseSource(nameof(HostPlatformServices))]
    public async Task StartupValidationFailsWhenTheHostForgetsAPlatformService(Type platformService)
    {
        using var httpClient = new HttpClient();
        var services = CreateServices(httpClient);
        foreach (var descriptor in services.Where(descriptor => descriptor.ServiceType == platformService).ToList())
            services.Remove(descriptor);
        var provider = services.BuildServiceProvider();
        await using var providerLifetime = provider.ConfigureAwait(false);

        Assert.Throws<InvalidOperationException>(provider.ValidateMobaMobileServices);
    }

    private ServiceCollection CreateServices(HttpClient httpClient)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        var settings = new Mock<ISettingsService>();
        settings.Setup(value => value.GetSettings()).Returns(new AppSettings());
        services.AddSingleton(settings.Object);
        services.AddSingleton<IUiDispatcher>(new ImmediateUiDispatcher());
        services.AddSingleton(Mock.Of<IBackgroundService>());
        services.AddSingleton(Mock.Of<IRestDiscoveryService>());
        services.AddSingleton(Mock.Of<IPhotoUploadService>());
        services.AddSingleton(Mock.Of<IPhotoCaptureService>());
        services.AddSingleton(Mock.Of<IRestApiClientRegistration>());
        services.AddSingleton(Mock.Of<INetworkProfileChangeNotifier>());
        services.AddSingleton(Mock.Of<IRuntimeHubRemoteClient>());
        services.AddMobaMobileServices(() => _appData, _ => httpClient);
        return services;
    }
}
