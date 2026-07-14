#if WINDOWS
// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAflow;

using Moba.Backend.Interface;
using Moba.Common.Configuration;
using Moba.Common.Events;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Moba.SharedUI.Interface;
using Moba.WinUI.Extensions;
using Moba.WinUI.Service;

/// <summary>
/// Tests for MOBAflow DI extension methods and startup validation.
/// </summary>
[TestFixture]
internal sealed class MobaWinUiServiceCollectionExtensionsTests
{
    [Test]
    public void AddMobaWinUiPlatformServices_RegistersUiDispatcherAndEventBus()
    {
        var provider = CreatePlatformServiceProvider();

        Assert.Multiple(() =>
        {
            Assert.That(provider.GetRequiredService<IUiDispatcher>(), Is.Not.Null);
            Assert.That(provider.GetRequiredService<IEventBus>(), Is.Not.Null);
        });
    }

    [Test]
    public void AddMobaWinUiBackendServices_ResolvesIMobaRuntime()
    {
        var provider = CreateBackendServiceProvider();

        Assert.That(provider.GetRequiredService<IMobaRuntime>(), Is.Not.Null);
    }

    [Test]
    public void AddMobaWinUiIoAndNetworkServices_ResolvesInterfaceAliases()
    {
        var provider = CreateNetworkServiceProvider();

        Assert.Multiple(() =>
        {
            var ioService = provider.GetRequiredService<IIoService>();
            Assert.That(ioService, Is.Not.Null);
            Assert.That(provider.GetRequiredService<ISolutionIoService>(), Is.SameAs(ioService));
            Assert.That(provider.GetRequiredService<IRuntimeCommandGateway>(), Is.Not.Null);
        });
    }

    [Test]
    public void WinUiDiContainerValidator_Passes_ForCoreServices()
    {
        var provider = CreateValidatorServiceProvider();

        Assert.DoesNotThrow(() => WinUiDiContainerValidator.ValidateCoreServices(provider));
    }

    private static ServiceProvider CreatePlatformServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddMobaWinUiConfiguration(CreateTestConfiguration());
        services.AddMobaWinUiPlatformServices();
        return services.BuildServiceProvider();
    }

    private static ServiceProvider CreateBackendServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services
            .AddMobaWinUiConfiguration(CreateTestConfiguration())
            .AddMobaWinUiPlatformServices()
            .AddMobaWinUiSpeechServices()
            .AddMobaWinUiBackendServices();
        return services.BuildServiceProvider();
    }

    private static ServiceProvider CreateNetworkServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services
            .AddMobaWinUiConfiguration(CreateTestConfiguration())
            .AddMobaWinUiPlatformServices()
            .AddMobaWinUiSpeechServices()
            .AddMobaWinUiBackendServices()
            .AddMobaWinUiIoAndNetworkServices()
            .AddMobaWinUiDomainServices();
        return services.BuildServiceProvider();
    }

    private static ServiceProvider CreateValidatorServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services
            .AddMobaWinUiConfiguration(CreateTestConfiguration())
            .AddMobaWinUiPlatformServices()
            .AddMobaWinUiSpeechServices()
            .AddMobaWinUiBackendServices()
            .AddMobaWinUiIoAndNetworkServices()
            .AddMobaWinUiDomainServices()
            .AddMobaWinUiShellServices()
            .AddMobaWinUiViewModelsAndWindow();

        // The container test validates registrations, not WinUI COM activation.
        // Last-registration-wins keeps production DI intact while preventing a
        // headless runner from touching DispatcherQueue during ViewModel creation.
        services.AddSingleton(Moq.Mock.Of<IUiDispatcher>());

        return services.BuildServiceProvider();
    }

    private static IConfiguration CreateTestConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
    }
}
#endif
