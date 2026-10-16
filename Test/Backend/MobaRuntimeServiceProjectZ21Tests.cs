// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Moba.Test.Helpers;
using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Common.Events;
using Moba.Domain;

using Moq;

using System.Net;

/// <summary>
/// Without an endpoint source of its own (MOBAsmart), <see cref="MobaRuntimeService"/> connects to the Z21 of the
/// project it runs and never searches the network.
/// </summary>
[TestFixture]
internal sealed partial class MobaRuntimeServiceProjectZ21Tests : IDisposable
{
    private readonly TestRuntimes _runtimes = new();

    public void Dispose() => _runtimes.Dispose();

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

    private MobaRuntimeService CreateRuntime(IZ21 z21) => _runtimes.Create(z21);
}
