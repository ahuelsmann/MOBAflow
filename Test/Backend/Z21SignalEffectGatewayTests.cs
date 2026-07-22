// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using Moba.Backend.Interface;
using Moba.Backend.Service.Interlocking;
using Moba.Common.Configuration;
using Moba.Domain;

using Moq;

[TestFixture]
internal sealed class Z21SignalEffectGatewayTests
{
    [Test]
    public async Task ExecuteAsync_ConfiguredMultiplexer_SendsResolvedZ21Command()
    {
        var signalId = Guid.NewGuid();
        var z21 = new Mock<IZ21>();
        z21.SetupGet(item => item.IsConnected).Returns(true);
        z21.Setup(item => item.SetTurnoutAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var gateway = CreateGateway(signalId, z21.Object, isMultiplexed: true);

        var result = await gateway.ExecuteAsync(new SignalEffectCommand(
            Guid.NewGuid(),
            signalId,
            SignalAspect.Ks1,
            Guid.NewGuid()));

        Assert.That(result.Status, Is.EqualTo(SignalEffectStatus.Succeeded));
        z21.Verify(item => item.SetTurnoutAsync(
            201,
            1,
            true,
            false,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ExecuteAsync_Offline_ReturnsOfflineWithoutSending()
    {
        var signalId = Guid.NewGuid();
        var z21 = new Mock<IZ21>();
        z21.SetupGet(item => item.IsConnected).Returns(false);
        var gateway = CreateGateway(signalId, z21.Object, isMultiplexed: true);

        var result = await gateway.ExecuteAsync(new SignalEffectCommand(
            Guid.NewGuid(),
            signalId,
            SignalAspect.Hp0,
            Guid.NewGuid()));

        Assert.That(result.Status, Is.EqualTo(SignalEffectStatus.Offline));
        z21.Verify(item => item.SetTurnoutAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ExecuteAsync_UnconfiguredSignal_FailsClosed()
    {
        var signalId = Guid.NewGuid();
        var z21 = new Mock<IZ21>();
        z21.SetupGet(item => item.IsConnected).Returns(true);
        var gateway = CreateGateway(signalId, z21.Object, isMultiplexed: false);

        var result = await gateway.ExecuteAsync(new SignalEffectCommand(
            Guid.NewGuid(),
            signalId,
            SignalAspect.Ks1,
            Guid.NewGuid()));

        Assert.That(result.Status, Is.EqualTo(SignalEffectStatus.Failed));
        z21.Verify(item => item.SetTurnoutAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Z21SignalEffectGateway CreateGateway(
        Guid signalId,
        IZ21 z21,
        bool isMultiplexed)
    {
        var definition = new InterlockingDefinition
        {
            Signals =
            [
                new SignalDefinition
                {
                    Id = signalId,
                    Name = "S1",
                    IsMultiplexed = isMultiplexed,
                    MultiplexerArticleNumber = isMultiplexed ? "5229" : null,
                    MainSignalArticleNumber = isMultiplexed ? "4046" : null,
                    BaseAddress = 201
                }
            ]
        };
        return new Z21SignalEffectGateway(definition, z21, new AppSettings());
    }
}
