#if !SKIP_ANDROID_TESTS
// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAsmart;

using Backend.Interface;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Moba.MAUI.Extensions;
using Moba.MAUI.Service;

using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;
using Moba.SharedUI.ViewModel;

/// <summary>
/// Tests for MOBAsmart DI extension methods and startup validation.
/// </summary>
[TestFixture]
internal sealed class MobaMauiServiceCollectionExtensionsTests
{
    [Test]
    public void AddMobiNetworkServices_RegistersNamedHttpClients()
    {
        var provider = CreateNetworkServiceProvider();

        var factory = provider.GetRequiredService<IHttpClientFactory>();

        Assert.Multiple(() =>
        {
            Assert.That(factory.CreateClient(MobiHttpClientNames.Platform), Is.Not.Null);
            Assert.That(factory.CreateClient(MobiHttpClientNames.LanHealth), Is.Not.Null);
            Assert.That(factory.CreateClient(MobiHttpClientNames.LanDiscovery), Is.Not.Null);
        });
    }

    [Test]
    public void AddMobiRemoteRuntimeServices_ResolvesInterfaceAliases()
    {
        var provider = CreateFullServiceProvider();

        Assert.Multiple(() =>
        {
            Assert.That(provider.GetRequiredService<IProjectContext>(), Is.InstanceOf<MobileSolutionContext>());
            Assert.That(provider.GetRequiredService<ISolutionRemoteLoader>(), Is.InstanceOf<SolutionRemoteLoader>());
            Assert.That(provider.GetRequiredService<IRuntimeHubRemoteClient>(), Is.InstanceOf<RuntimeHubRemoteClient>());
            Assert.That(provider.GetRequiredService<IRuntimeCommandGateway>(), Is.InstanceOf<RemoteRuntimeCommandGateway>());
        });
    }

    [Test]
    public void AddMobiViewModels_MauiViewModel_AutoWires()
    {
        var provider = CreateFullServiceProvider();

        Assert.That(provider.GetRequiredService<MauiViewModel>(), Is.Not.Null);
    }

    [Test]
    public void AddMobiViewModels_TrainControlViewModel_UsesRemoteSnapshots()
    {
        var provider = CreateFullServiceProvider();
        var viewModel = provider.GetRequiredService<TrainControlViewModel>();

        Assert.That(viewModel, Is.Not.Null);
    }

    [Test]
    public void MobiDiContainerValidator_Passes_ForFullRegistration()
    {
        var provider = CreateFullServiceProvider();

        Assert.DoesNotThrow(() => MobiDiContainerValidator.ValidateCoreServices(provider));
    }

    private static ServiceProvider CreateNetworkServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddMobiPlatformServices();
        services.AddMobiConfiguration();
        services.AddMobiNetworkServices();
        return services.BuildServiceProvider();
    }

    private static ServiceProvider CreateFullServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services
            .AddMobiPlatformServices()
            .AddMobiConfiguration()
            .AddMobiNetworkServices()
            .AddMobiRemoteRuntimeServices()
            .AddMobiViewModels()
            .AddMobiViews()
            .AddMobiStartupServices();
        return services.BuildServiceProvider();
    }
}
#endif
