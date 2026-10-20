// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Backend.Protocol;
using Moba.Backend.Service;
using Moba.Common.Events;

using Mocks;

using System.Net;

/// <summary>
/// Send-side contracts of <see cref="Z21"/>: argument limits, the packet that reaches the transport and failure paths.
/// Packet encodings themselves are covered by <see cref="Z21CommandTests"/>.
/// </summary>
[TestFixture]
internal sealed class Z21OperatorCommandTests
{
    // Z21 LAN protocol, independent of the production command table.
    private static readonly byte[] TrackPowerOn = [0x07, 0x00, 0x40, 0x00, 0x21, 0x81, 0xA0];
    private static readonly byte[] TrackPowerOff = [0x07, 0x00, 0x40, 0x00, 0x21, 0x80, 0xA1];
    private static readonly byte[] EmergencyStop = [0x06, 0x00, 0x40, 0x00, 0x80, 0x80];
    private static readonly byte[] GetStatus = [0x07, 0x00, 0x40, 0x00, 0x21, 0x24, 0x05];
    private static readonly byte[] Logoff = [0x04, 0x00, 0x30, 0x00];
    private static readonly byte[] GetSerialNumber = [0x04, 0x00, 0x10, 0x00];

    [Test]
    public async Task TrackPowerAndEmergencyCommands_SendTheProtocolPacketsAndLogThem()
    {
        var monitor = new Z21Monitor(NullLogger<Z21Monitor>.Instance);
        var (z21, udp) = Create(monitor);
        using (z21)
        {
            await z21.SetTrackPowerOnAsync().ConfigureAwait(false);
            await z21.SetTrackPowerOffAsync().ConfigureAwait(false);
            await z21.SetEmergencyStopAsync().ConfigureAwait(false);
            await z21.GetStatusAsync().ConfigureAwait(false);
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(udp.SentPayloads, Is.EqualTo(new[] { TrackPowerOn, TrackPowerOff, EmergencyStop, GetStatus }));
            Assert.That(
                monitor.GetPackets().Where(packet => packet.IsSent).Select(packet => packet.Data),
                Is.EquivalentTo(udp.SentPayloads));
        }
    }

    [TestCase(0, 0)]
    [TestCase(10000, 0)]
    [TestCase(3, -1)]
    [TestCase(3, 127)]
    public async Task SetLocoDriveAsync_RejectsOutOfRangeAddressOrSpeedWithoutSending(int address, int speed)
    {
        var (z21, udp) = Create();
        using (z21)
        {
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => z21.SetLocoDriveAsync(address, speed, forward: true)).ConfigureAwait(false);
        }

        Assert.That(udp.SentPayloads, Is.Empty);
    }

    [TestCase(1, 0)]
    [TestCase(9999, 126)]
    public async Task SetLocoDriveAsync_SendsTheDrivePacketAtTheRangeLimits(int address, int speed)
    {
        var (z21, udp) = Create();
        using (z21)
        {
            await z21.SetLocoDriveAsync(address, speed, forward: false).ConfigureAwait(false);
        }

        Assert.That(udp.SentPayloads, Is.EqualTo(new[] { Z21Command.BuildSetLocoDrive(address, speed, forward: false) }));
    }

    [TestCase(0, 0)]
    [TestCase(10000, 0)]
    [TestCase(3, -1)]
    public async Task SetLocoFunctionAsync_RejectsOutOfRangeAddressOrFunctionWithoutSending(int address, int functionIndex)
    {
        var (z21, udp) = Create();
        using (z21)
        {
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => z21.SetLocoFunctionAsync(address, functionIndex, on: true)).ConfigureAwait(false);
        }

        Assert.That(udp.SentPayloads, Is.Empty);
    }

    [TestCase(1, 0)]
    [TestCase(9999, 31)]
    public async Task SetLocoFunctionAsync_SendsTheFunctionPacketAtTheRangeLimits(int address, int functionIndex)
    {
        var (z21, udp) = Create();
        using (z21)
        {
            await z21.SetLocoFunctionAsync(address, functionIndex, on: false).ConfigureAwait(false);
        }

        Assert.That(udp.SentPayloads, Is.EqualTo(new[] { Z21Command.BuildSetLocoFunction(address, functionIndex, on: false) }));
    }

    [Test]
    public async Task SetAllLocoFunctionsOffAsync_RejectsAnAddressAboveTheDccRange()
    {
        var (z21, udp) = Create();
        using (z21)
        {
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => z21.SetAllLocoFunctionsOffAsync(10000)).ConfigureAwait(false);
        }

        Assert.That(udp.SentPayloads, Is.Empty);
    }

    [TestCase(0)]
    [TestCase(10000)]
    public async Task GetLocoInfoAsync_RejectsOutOfRangeAddressesWithoutSending(int address)
    {
        var (z21, udp) = Create();
        using (z21)
        {
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => z21.GetLocoInfoAsync(address)).ConfigureAwait(false);
        }

        Assert.That(udp.SentPayloads, Is.Empty);
    }

    [TestCase(1)]
    [TestCase(9999)]
    public async Task GetLocoInfoAsync_SendsTheSubscriptionRequest(int address)
    {
        var (z21, udp) = Create();
        using (z21)
        {
            await z21.GetLocoInfoAsync(address).ConfigureAwait(false);
        }

        Assert.That(udp.SentPayloads, Is.EqualTo(new[] { Z21Command.BuildGetLocoInfo(address) }));
    }

    [TestCase(0, 0)]
    [TestCase(2045, 0)]
    [TestCase(1, -1)]
    [TestCase(1, 2)]
    public async Task SetTurnoutAsync_RejectsOutOfRangeDecoderOrOutputWithoutSending(int decoderAddress, int output)
    {
        var (z21, udp) = Create();
        using (z21)
        {
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => z21.SetTurnoutAsync(decoderAddress, output, activate: true)).ConfigureAwait(false);
        }

        Assert.That(udp.SentPayloads, Is.Empty);
    }

    [TestCase(1, 0, true, false)]
    [TestCase(2044, 1, false, true)]
    public async Task SetTurnoutAsync_SendsTheTurnoutPacketAtTheRangeLimits(int decoderAddress, int output, bool activate, bool queue)
    {
        var (z21, udp) = Create();
        using (z21)
        {
            await z21.SetTurnoutAsync(decoderAddress, output, activate, queue).ConfigureAwait(false);
        }

        Assert.That(
            udp.SentPayloads,
            Is.EqualTo(new[] { Z21Command.BuildSetTurnout(decoderAddress, output, activate, queue) }));
    }

    [TestCase(-1, 0)]
    [TestCase(256, 0)]
    [TestCase(0, -1)]
    [TestCase(0, 256)]
    public async Task SetExtAccessoryAsync_RejectsOutOfRangeAddressOrValueWithoutSending(int address, int value)
    {
        var (z21, udp) = Create();
        using (z21)
        {
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => z21.SetExtAccessoryAsync(address, value)).ConfigureAwait(false);
        }

        Assert.That(udp.SentPayloads, Is.Empty);
    }

    [TestCase(0, 0)]
    [TestCase(255, 255)]
    public async Task SetExtAccessoryAsync_SendsTheAccessoryPacketAtTheRangeLimits(int address, int value)
    {
        var (z21, udp) = Create();
        using (z21)
        {
            await z21.SetExtAccessoryAsync(address, value).ConfigureAwait(false);
        }

        Assert.That(udp.SentPayloads, Is.EqualTo(new[] { Z21Command.BuildSetExtAccessory(address, value) }));
    }

    [TestCase(0)]
    [TestCase(2045)]
    public async Task GetTurnoutInfoAsync_RejectsOutOfRangeDecodersWithoutSending(int decoderAddress)
    {
        var (z21, udp) = Create();
        using (z21)
        {
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => z21.GetTurnoutInfoAsync(decoderAddress)).ConfigureAwait(false);
        }

        Assert.That(udp.SentPayloads, Is.Empty);
    }

    [TestCase(1)]
    [TestCase(2044)]
    public async Task GetTurnoutInfoAsync_SendsTheStatusRequest(int decoderAddress)
    {
        var (z21, udp) = Create();
        using (z21)
        {
            await z21.GetTurnoutInfoAsync(decoderAddress).ConfigureAwait(false);
        }

        Assert.That(udp.SentPayloads, Is.EqualTo(new[] { Z21Command.BuildGetTurnoutInfo(decoderAddress) }));
    }

    [Test]
    public async Task SendCommandAsync_RejectsMissingBytesAndHonorsCancellationBeforeSending()
    {
        var (z21, udp) = Create();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync().ConfigureAwait(false);
        using (z21)
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => z21.SendCommandAsync(null!)).ConfigureAwait(false);
            await Assert.CatchAsync<OperationCanceledException>(() => z21.SendCommandAsync(GetStatus, cancellation.Token)).ConfigureAwait(false);
        }

        Assert.That(udp.SentPayloads, Is.Empty);
    }

    [Test]
    public async Task SendCommandAsync_SurfacesTransportFailuresAndKeepsTheSendPathUsable()
    {
        var (z21, udp) = Create();
        udp.SendFailuresRemaining = 1;
        using (z21)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => z21.SendCommandAsync(TrackPowerOff)).ConfigureAwait(false);
            await z21.SendCommandAsync(TrackPowerOff).WaitAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        }

        Assert.That(udp.SentPayloads, Is.EqualTo(new[] { TrackPowerOff }));
    }

    [Test]
    public async Task RecoverConnectionAsync_ConnectsAClosedTransportAndSendsTheWakeUpSequenceInOrder()
    {
        var (z21, udp) = Create();
        using (z21)
        {
            await z21.RecoverConnectionAsync(IPAddress.Loopback).ConfigureAwait(false);
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(udp.Connected, Is.True);
            Assert.That(udp.SentPayloads, Is.EqualTo(new[]
            {
                EmergencyStop,
                Logoff,
                GetSerialNumber,
                Z21Command.BuildHandshake(),
                Z21Command.BuildBroadcastFlagsBasic(),
                GetStatus
            }));
        }
    }

    [Test]
    public async Task RecoverConnectionAsync_RethrowsATransportFailure()
    {
        var (z21, udp) = Create();
        udp.SendFailuresRemaining = 1;
        using (z21)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => z21.RecoverConnectionAsync(IPAddress.Loopback)).ConfigureAwait(false);
        }

        Assert.That(udp.SentPayloads, Is.Empty);
    }

    private static (Z21 Z21, FakeUdpClientWrapper Udp) Create(Z21Monitor? monitor = null)
    {
        var udp = new FakeUdpClientWrapper();
        return (new Z21(udp, new EventBus(NullLogger<EventBus>.Instance), trafficMonitor: monitor), udp);
    }
}
