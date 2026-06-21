// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Moba.Backend;
using Moba.Backend.Interface;
using Moba.Backend.Manager;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Common.Discovery;
using Moba.Common.Events;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

/// <summary>
/// Verifies Z21 auto-connect discovery integration in <see cref="MobaRuntimeService"/>.
/// </summary>
[TestFixture]
internal sealed class MobaRuntimeServiceAutoConnectDiscoveryTests
{
    [Test]
    public async Task StartAsync_UsesDiscovery_WhenConfiguredIpIsEmpty()
    {
        var settings = new AppSettings { Z21 = { CurrentIpAddress = string.Empty } };
        var z21Mock = new Mock<IZ21>();
        var discoveryMock = new Mock<IZ21DiscoveryService>();
        discoveryMock
            .Setup(service => service.DiscoverZ21Async(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("192.168.0.111");

        z21Mock.SetupGet(z21 => z21.TrafficMonitor).Returns((Moba.Backend.Service.Z21Monitor?)null);
        z21Mock.Setup(z21 => z21.ConnectAsync(It.IsAny<System.Net.IPAddress>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var runtime = CreateRuntime(settings, z21Mock.Object, discoveryMock.Object);
        await runtime.StartAsync();

        discoveryMock.Verify(service => service.DiscoverZ21Async(null, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        Assert.That(settings.Z21.CurrentIpAddress, Is.EqualTo("192.168.0.111"));
    }

    private static MobaRuntimeService CreateRuntime(AppSettings settings, IZ21 z21, IZ21DiscoveryService discovery)
    {
        var workflowMock = new Mock<IWorkflowService>();
        var contextFactory = new ActionExecutionContextFactory(new ActionExecutionContext { Z21 = z21 });
        return new MobaRuntimeService(
            z21,
            workflowMock.Object,
            contextFactory,
            settings,
            NullLogger<MobaRuntimeService>.Instance,
            new EventBus(NullLogger<EventBus>.Instance),
            journeyManagerFactory: null,
            z21Discovery: discovery);
    }
}
