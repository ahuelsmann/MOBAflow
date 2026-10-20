// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Domain;

using Moq;

/// <summary>
/// Operator command paths of <see cref="MobaRuntimeService"/>: what reaches the Z21, the resulting runtime state and
/// the failure and offline paths.
/// </summary>
[TestFixture]
internal sealed class MobaRuntimeServiceCommandTests
{
    private const string MultiplexerArticle = "5229";
    private const string MainSignalArticle = "4046";
    private const int SignalBaseAddress = 201;

    [Test]
    public async Task SetTrackPowerAsync_SendsOffWithoutOn()
    {
        var z21 = CreateZ21Mock();
        using var runtime = CreateRuntime(z21.Object);

        await runtime.SetTrackPowerAsync(false).ConfigureAwait(false);

        z21.Verify(value => value.SetTrackPowerOffAsync(It.IsAny<CancellationToken>()), Times.Once);
        z21.Verify(value => value.SetTrackPowerOnAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SetTrackPowerAsync_ReportsASendFailureInTheStatusInsteadOfThrowing()
    {
        var z21 = CreateZ21Mock();
        z21.Setup(value => value.SetTrackPowerOffAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Z21 unreachable"));
        using var runtime = CreateRuntime(z21.Object);

        await runtime.SetTrackPowerAsync(false).ConfigureAwait(false);

        Assert.That(runtime.Current.StatusText, Is.EqualTo("Track power error: Z21 unreachable"));
    }

    [TestCase(-5, 0)]
    [TestCase(0, 0)]
    [TestCase(7, 7)]
    public void SetSystemStatePollingInterval_ClampsNegativeValuesAndForwardsTheSetting(int requested, int expected)
    {
        var z21 = CreateZ21Mock();
        var settings = new AppSettings();
        using var runtime = CreateRuntime(z21.Object, settings);

        runtime.SetSystemStatePollingInterval(requested);

        Assert.That(settings.Z21.SystemStatePollingIntervalSeconds, Is.EqualTo(expected));
        z21.Verify(value => value.SetSystemStatePollingInterval(expected), Times.Once);
    }

    [Test]
    public async Task SetLocomotiveFunctionAsync_TracksEachFunctionBitAndKeepsDriveState()
    {
        var z21 = CreateZ21Mock();
        using var runtime = CreateRuntime(z21.Object);
        await runtime.SetLocomotiveDriveAsync(3, 40, forward: false).ConfigureAwait(false);

        await runtime.SetLocomotiveFunctionAsync(3, 2, isOn: true).ConfigureAwait(false);
        await runtime.SetLocomotiveFunctionAsync(3, 31, isOn: true).ConfigureAwait(false);
        await runtime.SetLocomotiveFunctionAsync(3, 2, isOn: false).ConfigureAwait(false);

        var state = runtime.Current.LocomotiveStates[3];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(state.Functions, Is.EqualTo(1u << 31));
            Assert.That(state.Speed, Is.EqualTo(40));
            Assert.That(state.IsForward, Is.False);
        }

        z21.Verify(value => value.SetLocoFunctionAsync(3, 2, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SetLocomotiveFunctionAsync_ForAnUnknownLocomotive_AssumesAStoppedForwardLocomotive()
    {
        var z21 = CreateZ21Mock();
        using var runtime = CreateRuntime(z21.Object);

        await runtime.SetLocomotiveFunctionAsync(5, 0, isOn: true).ConfigureAwait(false);

        var state = runtime.Current.LocomotiveStates[5];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(state.Functions, Is.EqualTo(1u));
            Assert.That(state.Speed, Is.Zero);
            Assert.That(state.IsForward, Is.True);
        }
    }

    [Test]
    public async Task SetAllLocomotiveFunctionsOffAsync_ClearsEveryFunctionAndKeepsDriveState()
    {
        var z21 = CreateZ21Mock();
        using var runtime = CreateRuntime(z21.Object);
        await runtime.SetLocomotiveDriveAsync(3, 25, forward: false).ConfigureAwait(false);
        await runtime.SetLocomotiveFunctionAsync(3, 4, isOn: true).ConfigureAwait(false);

        await runtime.SetAllLocomotiveFunctionsOffAsync(3).ConfigureAwait(false);

        var state = runtime.Current.LocomotiveStates[3];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(state.Functions, Is.Zero);
            Assert.That(state.Speed, Is.EqualTo(25));
            Assert.That(state.IsForward, Is.False);
        }

        z21.Verify(value => value.SetAllLocoFunctionsOffAsync(3, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SetAllLocomotiveFunctionsOffAsync_ForAnUnknownLocomotive_AssumesAStoppedForwardLocomotive()
    {
        var z21 = CreateZ21Mock();
        using var runtime = CreateRuntime(z21.Object);

        await runtime.SetAllLocomotiveFunctionsOffAsync(8).ConfigureAwait(false);

        var state = runtime.Current.LocomotiveStates[8];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(state.Functions, Is.Zero);
            Assert.That(state.Speed, Is.Zero);
            Assert.That(state.IsForward, Is.True);
        }
    }

    [Test]
    public async Task LocomotiveCommands_ThatTheZ21Rejects_LeaveTheRuntimeStateUnchanged()
    {
        var z21 = CreateZ21Mock();
        z21.Setup(value => value.SetLocoDriveAsync(3, It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Send failed"));
        z21.Setup(value => value.SetLocoFunctionAsync(3, It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Send failed"));
        using var runtime = CreateRuntime(z21.Object);

        await Assert.ThrowsAsync<IOException>(() => runtime.SetLocomotiveDriveAsync(3, 40, true)).ConfigureAwait(false);
        await Assert.ThrowsAsync<IOException>(() => runtime.SetLocomotiveFunctionAsync(3, 1, true)).ConfigureAwait(false);

        Assert.That(runtime.Current.LocomotiveStates.ContainsKey(3), Is.False);
    }

    [Test]
    public async Task RequestLocomotiveInfoAsync_SubscribesTheAddressAtTheZ21()
    {
        var z21 = CreateZ21Mock();
        using var runtime = CreateRuntime(z21.Object);

        await runtime.RequestLocomotiveInfoAsync(42).ConfigureAwait(false);

        z21.Verify(value => value.GetLocoInfoAsync(42, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AcknowledgeFailSafeAsync_ReleasesOnlyAConnectedSystem()
    {
        var z21 = CreateZ21Mock();
        using var runtime = CreateRuntime(z21.Object);
        ReportConnectionLoss(z21);
        Assert.That(runtime.Current.IsOperatorAckRequired, Is.True);

        await runtime.AcknowledgeFailSafeAsync().ConfigureAwait(false);
        var whileDisconnected = runtime.Current;
        ConfirmConnection(z21);
        await runtime.AcknowledgeFailSafeAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(whileDisconnected.IsOperatorAckRequired, Is.True);
            Assert.That(whileDisconnected.LastFailSafeReason, Is.EqualTo("Unexpected loss of the Z21 connection."));
            Assert.That(runtime.Current.IsOperatorAckRequired, Is.False);
            Assert.That(runtime.Current.LastFailSafeReason, Is.EqualTo("Operator released the system for normal operation."));
        }
    }

    [Test]
    public async Task AcknowledgeFailSafeAsync_HonorsCancellation()
    {
        var z21 = CreateZ21Mock();
        using var runtime = CreateRuntime(z21.Object);
        ReportConnectionLoss(z21);
        ConfirmConnection(z21);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync().ConfigureAwait(false);

        await Assert.CatchAsync<OperationCanceledException>(
            () => runtime.AcknowledgeFailSafeAsync(cancellation.Token)).ConfigureAwait(false);

        Assert.That(runtime.Current.IsOperatorAckRequired, Is.True);
    }

    [Test]
    public async Task SimulateFeedbackAsync_WithoutAnActiveProject_ReportsTheMissingProjectAndSendsNothing()
    {
        var z21 = CreateZ21Mock();
        using var runtime = CreateRuntime(z21.Object);

        await runtime.SimulateFeedbackAsync(4).ConfigureAwait(false);

        Assert.That(runtime.Current.StatusText, Is.EqualTo("Error: No active project. Load or select a project first."));
        z21.Verify(value => value.SimulateFeedback(It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task SimulateFeedbackAsync_ReportsARejectedInPortInTheStatus()
    {
        var z21 = CreateZ21Mock();
        z21.Setup(value => value.SimulateFeedback(999)).Throws(new ArgumentOutOfRangeException("inPort", "InPort out of range"));
        using var runtime = CreateRuntime(z21.Object);
        await runtime.ActivateProjectAsync(new Project()).ConfigureAwait(false);

        await runtime.SimulateFeedbackAsync(4).ConfigureAwait(false);
        var afterValidPort = runtime.Current.StatusText;
        await runtime.SimulateFeedbackAsync(999).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(afterValidPort, Is.EqualTo("Simulated feedback for InPort 4"));
            Assert.That(runtime.Current.StatusText, Does.StartWith("Error: InPort out of range"));
        }
    }

    [Test]
    public async Task ResetJourneyAsync_ResetsOnlyAKnownJourneyOfTheActiveProject()
    {
        var z21 = CreateZ21Mock();
        using var runtime = CreateRuntime(z21.Object);
        await runtime.ResetJourneyAsync(Guid.NewGuid()).ConfigureAwait(false);
        var withoutProject = runtime.Current.StatusText;
        var journey = new Journey { Name = "Shuttle", Stations = [new Station { Name = "A" }, new Station { Name = "B" }] };
        await runtime.ActivateProjectAsync(new Project { Journeys = [journey] }).ConfigureAwait(false);
        var beforeReset = runtime.Current.JourneyStates[journey.Id].JourneyRunId;

        await runtime.ResetJourneyAsync(Guid.NewGuid()).ConfigureAwait(false);
        var afterUnknownJourney = runtime.Current;
        await runtime.ResetJourneyAsync(journey.Id).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(withoutProject, Is.Not.EqualTo("Journey 'Shuttle' reset"));
            Assert.That(afterUnknownJourney.StatusText, Is.Not.EqualTo("Journey 'Shuttle' reset"));
            Assert.That(afterUnknownJourney.JourneyStates[journey.Id].JourneyRunId, Is.EqualTo(beforeReset));
            Assert.That(runtime.Current.StatusText, Is.EqualTo("Journey 'Shuttle' reset"));
            Assert.That(runtime.Current.JourneyStates[journey.Id].JourneyRunId, Is.Not.EqualTo(beforeReset));
        }
    }

    [Test]
    public async Task SetSignalAspectAsync_ForAnUnknownSignal_SendsNothing()
    {
        var z21 = CreateZ21Mock(isConnected: true);
        using var runtime = CreateRuntime(z21.Object);
        await runtime.ActivateProjectAsync(ProjectWithSignal(CreateSignal())).ConfigureAwait(false);

        await runtime.SetSignalAspectAsync(Guid.NewGuid(), SignalAspect.Ks2).ConfigureAwait(false);

        VerifyNoTurnoutCommand(z21);
    }

    [Test]
    public async Task SetSignalAspectAsync_WithoutMultiplexerConfiguration_SendsNothing()
    {
        var z21 = CreateZ21Mock(isConnected: true);
        using var runtime = CreateRuntime(z21.Object);
        var notMultiplexed = CreateSignal();
        notMultiplexed.IsMultiplexed = false;
        var withoutArticle = CreateSignal();
        withoutArticle.MultiplexerArticleNumber = string.Empty;
        await runtime.ActivateProjectAsync(ProjectWithSignal(notMultiplexed, withoutArticle)).ConfigureAwait(false);

        await runtime.SetSignalAspectAsync(notMultiplexed.Id, SignalAspect.Ks2).ConfigureAwait(false);
        await runtime.SetSignalAspectAsync(withoutArticle.Id, SignalAspect.Ks2).ConfigureAwait(false);

        VerifyNoTurnoutCommand(z21);
    }

    [Test]
    public async Task SetSignalAspectAsync_WhileTheZ21IsOffline_UpdatesTheRuntimeAspectWithoutSending()
    {
        var z21 = CreateZ21Mock(isConnected: false);
        using var runtime = CreateRuntime(z21.Object);
        var signal = CreateSignal();
        await runtime.ActivateProjectAsync(ProjectWithSignal(signal)).ConfigureAwait(false);

        await runtime.SetSignalAspectAsync(signal.Id, SignalAspect.Ks2).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(SignalAspectOf(runtime, signal.Id), Is.EqualTo(SignalAspect.Ks2));
            Assert.That(runtime.Current.StatusText, Is.EqualTo("Signal 'A1': Ks2 (Z21 offline)"));
        }

        VerifyNoTurnoutCommand(z21);
    }

    [Test]
    public async Task SetSignalAspectAsync_WhileConnected_SendsTheResolvedMultiplexerCommand()
    {
        var z21 = CreateZ21Mock(isConnected: true);
        var settings = new AppSettings();
        using var runtime = CreateRuntime(z21.Object, settings);
        var signal = CreateSignal();
        await runtime.ActivateProjectAsync(ProjectWithSignal(signal)).ConfigureAwait(false);
        var expected = MultiplexerCommandResolver.Resolve(
            SignalBaseAddress, MultiplexerArticle, MainSignalArticle, SignalAspect.Ks2, settings.SignalBox);

        await runtime.SetSignalAspectAsync(signal.Id, SignalAspect.Ks2).ConfigureAwait(false);

        z21.Verify(value => value.SetTurnoutAsync(
            expected.DccAddress, expected.Output, expected.Activate, false, It.IsAny<CancellationToken>()), Times.Once);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(SignalAspectOf(runtime, signal.Id), Is.EqualTo(SignalAspect.Ks2));
            Assert.That(runtime.Current.StatusText, Is.EqualTo(
                $"Signal 'A1' set: DCC address {expected.DccAddress}, output {expected.Output}, activate={expected.Activate}"));
        }
    }

    [Test]
    public async Task SetSignalAspectAsync_WithAnUnknownMultiplexerArticle_LogsWithoutSendingOrThrowing()
    {
        var z21 = CreateZ21Mock(isConnected: true);
        using var runtime = CreateRuntime(z21.Object);
        var signal = CreateSignal();
        signal.MultiplexerArticleNumber = "0000";
        await runtime.ActivateProjectAsync(ProjectWithSignal(signal)).ConfigureAwait(false);

        await runtime.SetSignalAspectAsync(signal.Id, SignalAspect.Ks2).ConfigureAwait(false);

        VerifyNoTurnoutCommand(z21);
    }

    [Test]
    public async Task SetSignalAspectAsync_ReportsAndRethrowsASendFailure()
    {
        var z21 = CreateZ21Mock(isConnected: true);
        z21.Setup(value => value.SetTurnoutAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Send failed"));
        using var runtime = CreateRuntime(z21.Object);
        var signal = CreateSignal();
        await runtime.ActivateProjectAsync(ProjectWithSignal(signal)).ConfigureAwait(false);

        await Assert.ThrowsAsync<IOException>(
            () => runtime.SetSignalAspectAsync(signal.Id, SignalAspect.Ks2)).ConfigureAwait(false);

        Assert.That(runtime.Current.StatusText, Is.EqualTo("Signal error: Send failed"));
    }

    [Test]
    public async Task SendTurnoutCommandAsync_WhileOffline_ReportsTheMissingConnectionAndSendsNothing()
    {
        var z21 = CreateZ21Mock(isConnected: false);
        using var runtime = CreateRuntime(z21.Object);

        await runtime.SendTurnoutCommandAsync(12, 1, activate: true).ConfigureAwait(false);

        Assert.That(runtime.Current.StatusText, Is.EqualTo("Z21 not connected"));
        VerifyNoTurnoutCommand(z21);
    }

    [Test]
    public async Task SendTurnoutCommandAsync_WhileConnected_ForwardsTheCommandAndReportsIt()
    {
        var z21 = CreateZ21Mock(isConnected: true);
        using var runtime = CreateRuntime(z21.Object);

        await runtime.SendTurnoutCommandAsync(12, 1, activate: false, queue: true).ConfigureAwait(false);

        z21.Verify(value => value.SetTurnoutAsync(12, 1, false, true, It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(
            runtime.Current.StatusText,
            Is.EqualTo("Turnout set: DCC address 12, output 1, activate=False, queue=True"));
    }

    [Test]
    public async Task RequestSystemStateAsync_AsksOnlyAConnectedZ21()
    {
        var offline = CreateZ21Mock(isConnected: false);
        using (var runtime = CreateRuntime(offline.Object))
        {
            await runtime.RequestSystemStateAsync().ConfigureAwait(false);
        }

        var online = CreateZ21Mock(isConnected: true);
        using (var runtime = CreateRuntime(online.Object))
        {
            await runtime.RequestSystemStateAsync().ConfigureAwait(false);
        }

        offline.Verify(value => value.GetStatusAsync(It.IsAny<CancellationToken>()), Times.Never);
        online.Verify(value => value.GetStatusAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void TrafficMonitorAccess_ReturnsACopyAndToleratesAMissingMonitor()
    {
        var withoutMonitor = CreateZ21Mock();
        using (var runtime = CreateRuntime(withoutMonitor.Object))
        {
            Assert.That(runtime.GetTrafficPackets(), Is.Empty);
            Assert.DoesNotThrow(runtime.ClearTrafficMonitor);
        }

        var monitor = new Z21Monitor(NullLogger<Z21Monitor>.Instance);
        monitor.LogSentPacket([0x04, 0x00, 0x30, 0x00]);
        var withMonitor = CreateZ21Mock();
        withMonitor.SetupGet(value => value.TrafficMonitor).Returns(monitor);
        using (var runtime = CreateRuntime(withMonitor.Object))
        {
            var packets = runtime.GetTrafficPackets();
            runtime.ClearTrafficMonitor();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(packets, Has.Count.EqualTo(1));
                Assert.That(monitor.GetPackets(), Is.Empty);
            }
        }
    }

    private static SbSignal CreateSignal() => new()
    {
        Name = "A1",
        IsMultiplexed = true,
        MultiplexerArticleNumber = MultiplexerArticle,
        MainSignalArticleNumber = MainSignalArticle,
        BaseAddress = SignalBaseAddress
    };

    private static Project ProjectWithSignal(params SbSignal[] signals) =>
        new() { Name = "Signals", SignalBoxPlan = new SignalBoxPlan { Elements = [.. signals] } };

    private static SignalAspect? SignalAspectOf(MobaRuntimeService runtime, Guid signalId) =>
        runtime.Current.SignalBoxElements.Single(element => element.ElementId == signalId).SignalAspect;

    private static void VerifyNoTurnoutCommand(Mock<IZ21> z21) =>
        z21.Verify(value => value.SetTurnoutAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);

    // IZ21 publishes Action events, so they have no asynchronous handlers to await.
    private static void ReportConnectionLoss(Mock<IZ21> z21) =>
        z21.Raise(value => value.OnConnectionLost += null);

    private static void ConfirmConnection(Mock<IZ21> z21) =>
        z21.Raise(value => value.OnConnectedChanged += null, true);

    private static Mock<IZ21> CreateZ21Mock(bool isConnected = false)
    {
        var z21Mock = new Mock<IZ21>();
        z21Mock.SetupGet(z => z.TrafficMonitor).Returns((Z21Monitor?)null);
        z21Mock.SetupGet(z => z.IsConnected).Returns(isConnected);
        return z21Mock;
    }

    private static MobaRuntimeService CreateRuntime(IZ21 z21, AppSettings? settings = null) =>
        new(
            z21,
            new Mock<IWorkflowService>().Object,
            new ActionExecutionContext { Z21 = z21 },
            settings ?? new AppSettings(),
            new Mock<ILogger<MobaRuntimeService>>().Object);
}
