// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Domain;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using System.Net;

/// <summary>
/// Without an endpoint source of its own (MOBAsmart), <see cref="MobaRuntimeService"/> connects to the Z21 of the
/// project it runs and never searches the network.
/// </summary>
[TestFixture]
internal sealed class MobaRuntimeServiceProjectZ21Tests
{
    [Test]
    public async Task StartAsync_ConnectsToTheZ21OfTheActiveProject()
    {
        var z21Mock = CreateZ21();
        using var runtime = CreateRuntime(z21Mock.Object);
        await runtime.ActivateProjectAsync(new Project { Name = "Yard", Z21 = { IpAddress = "192.168.0.112", Port = 21106 } })
            .ConfigureAwait(false);

        await runtime.StartAsync().ConfigureAwait(false);

        z21Mock.Verify(
            z21 => z21.ConnectAsync(IPAddress.Parse("192.168.0.112"), 21106, It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task StartAsync_WithoutProject_StaysDisconnectedAndSaysWhy()
    {
        var z21Mock = CreateZ21();
        using var runtime = CreateRuntime(z21Mock.Object);

        await runtime.StartAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(runtime.Current.StatusText, Is.EqualTo("No project selected"));
            z21Mock.Verify(
                z21 => z21.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }

    private static Mock<IZ21> CreateZ21()
    {
        var z21Mock = new Mock<IZ21>();
        z21Mock.SetupGet(z21 => z21.TrafficMonitor).Returns((Z21Monitor?)null);
        z21Mock.Setup(z21 => z21.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return z21Mock;
    }

    private static MobaRuntimeService CreateRuntime(IZ21 z21)
    {
        var workflowMock = new Mock<IWorkflowService>();
        var contextFactory = new ActionExecutionContextFactory(new ActionExecutionContext { Z21 = z21 });
        return new MobaRuntimeService(
            z21,
            workflowMock.Object,
            contextFactory,
            new AppSettings(),
            NullLogger<MobaRuntimeService>.Instance,
            new EventBus(NullLogger<EventBus>.Instance));
    }
}
