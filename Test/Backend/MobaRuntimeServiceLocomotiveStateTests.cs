// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Microsoft.Extensions.Logging;

using Moba.Backend.Interface;
using Moba.Backend.Model;
using Moba.Backend.Service;
using Moba.Common.Configuration;

using Moq;

/// <summary>
/// Verifies locomotive drive/function state synchronization between runtime commands and Z21 loco info.
/// </summary>
[TestFixture]
internal sealed class MobaRuntimeServiceLocomotiveStateTests
{
    [Test]
    public async Task LocoInfoAfterDriveCommand_PreservesExistingFunctionBitsDuringGracePeriod()
    {
        var z21Mock = CreateZ21Mock();
        using var runtime = CreateRuntime(z21Mock.Object);

        await runtime.SetLocomotiveDriveAsync(address: 3, speed: 0, forward: false);

        z21Mock.Raise(
            z => z.OnLocoInfoChanged += null,
            new LocoInfo
            {
                Address = 3,
                Speed = 0,
                IsForward = false,
                Functions = 0b1111
            });

        Assert.That(runtime.Current.LocomotiveStates[3].Functions, Is.Zero,
            "Z21 loco info must not overwrite function bits immediately after a local drive command");
    }

    private static Mock<IZ21> CreateZ21Mock()
    {
        var z21Mock = new Mock<IZ21>();
        z21Mock.SetupGet(z => z.TrafficMonitor).Returns((Z21Monitor?)null);
        z21Mock.SetupGet(z => z.IsConnected).Returns(false);
        z21Mock.Setup(z => z.SetLocoDriveAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return z21Mock;
    }

    private static MobaRuntimeService CreateRuntime(IZ21 z21)
    {
        var workflowServiceMock = new Mock<IWorkflowService>();
        var loggerMock = new Mock<ILogger<MobaRuntimeService>>();

        return new MobaRuntimeService(
            z21,
            workflowServiceMock.Object,
            new ActionExecutionContext { Z21 = z21 },
            new AppSettings
            {
                Z21 = new Z21Settings { CurrentIpAddress = string.Empty }
            },
            loggerMock.Object);
    }
}
