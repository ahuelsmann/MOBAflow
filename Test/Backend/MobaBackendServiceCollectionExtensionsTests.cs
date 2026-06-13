// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Moba.Backend;
using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Sound;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Tests for <see cref="MobaBackendServiceCollectionExtensions.AddMobaBackendServices"/>.
/// Verifies that shared backend services register once and resolve to the expected implementations.
/// </summary>
[TestFixture]
internal sealed class MobaBackendServiceCollectionExtensionsTests
{
    [Test]
    public void AddMobaBackendServices_NullCollection_Throws()
    {
        IServiceCollection? services = null;

        Assert.Throws<ArgumentNullException>(() => services!.AddMobaBackendServices());
    }

    [Test]
    public void AddMobaBackendServices_RegistersCoreRuntimeServices()
    {
        var provider = BuildServiceProvider();

        Assert.Multiple(() =>
        {
            Assert.That(provider.GetService<IZ21>(), Is.InstanceOf<Z21>());
            Assert.That(provider.GetService<IMobaRuntime>(), Is.InstanceOf<MobaRuntimeService>());
            Assert.That(provider.GetService<IWorkflowService>(), Is.InstanceOf<WorkflowService>());
            Assert.That(provider.GetService<IProjectValidator>(), Is.InstanceOf<ProjectValidator>());
        });
    }

    [Test]
    public void AddMobaBackendServices_RegistersZ21CapabilityAliasesToSameInstance()
    {
        var provider = BuildServiceProvider();
        var z21 = provider.GetRequiredService<IZ21>();

        Assert.Multiple(() =>
        {
            Assert.That(provider.GetRequiredService<IZ21Connection>(), Is.SameAs(z21));
            Assert.That(provider.GetRequiredService<ILocoControl>(), Is.SameAs(z21));
            Assert.That(provider.GetRequiredService<IAccessoryControl>(), Is.SameAs(z21));
            Assert.That(provider.GetRequiredService<IZ21Diagnostics>(), Is.SameAs(z21));
        });
    }

    [Test]
    public void AddMobaBackendServices_RegistersAllWorkflowActionHandlers()
    {
        var handlers = BuildServiceProvider().GetServices<IWorkflowActionHandler>().ToList();

        Assert.That(handlers, Has.Count.EqualTo(6));
        Assert.That(handlers.Select(h => h.GetType().Name), Is.Unique);
    }

    [Test]
    public void AddMobaBackendServices_IsIdempotentForSingletonRegistrations()
    {
        var services = CreateServiceCollection();
        services.AddMobaBackendServices();
        services.AddMobaBackendServices();

        var provider = services.BuildServiceProvider();
        var first = provider.GetRequiredService<IZ21>();
        var second = provider.GetRequiredService<IZ21>();

        Assert.That(second, Is.SameAs(first));
    }

    private static ServiceProvider BuildServiceProvider()
        => CreateServiceCollection().BuildServiceProvider();

    private static ServiceCollection CreateServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddSingleton(new AppSettings());
        services.AddSingleton<IEventBus>(sp => new EventBus(sp.GetRequiredService<ILogger<EventBus>>()));
        services.AddSingleton<ISpeakerEngine, NullSpeakerEngine>();
        services.AddSingleton<ISoundPlayer, NullSoundPlayer>();
        services.AddMobaBackendServices();
        return services;
    }
}
