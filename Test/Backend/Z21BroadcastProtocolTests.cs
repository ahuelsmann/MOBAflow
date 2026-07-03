// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using Moba.Backend.Model;
using Moba.Backend.Protocol;
using Moba.Common.Z21;

/// <summary>
/// Verifies Z21 LAN broadcast prerequisites documented in docs/z21-lan-protokoll.pdf v1.13.
/// </summary>
[TestFixture]
internal sealed class Z21BroadcastProtocolTests
{
    [Test]
    public void MobaFlowBasicBroadcastFlags_IncludesDrivingButNotAllLocoInfo()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                Z21BroadcastRequirements.MobaFlowBasicBroadcastFlags & Z21BroadcastRequirements.DrivingBroadcastFlag,
                Is.Not.Zero);
            Assert.That(
                Z21BroadcastRequirements.MobaFlowBasicBroadcastFlags & Z21BroadcastRequirements.AllLocoInfoBroadcastFlag,
                Is.Zero);
        });
    }

    [Test]
    public void BuildBroadcastFlagsBasic_MatchesMobaFlowBasicFlags()
    {
        var packet = Z21Command.BuildBroadcastFlagsBasic();
        var flags = BitConverter.ToUInt32(packet, 4);

        Assert.That(flags, Is.EqualTo(Z21BroadcastRequirements.MobaFlowBasicBroadcastFlags));
    }

    [Test]
    public void LocoInfoBroadcast_RequiresPerAddressSubscription_UnlessAllLocoInfoFlag()
    {
        var simulator = new Z21BroadcastSimulator();
        simulator.SetClientBroadcastFlags("clientA", Z21BroadcastRequirements.DrivingBroadcastFlag);
        simulator.SetClientBroadcastFlags("clientB", Z21BroadcastRequirements.DrivingBroadcastFlag);

        simulator.SubscribeLocomotive("clientA", address: 3);
        simulator.PublishLocoDriveChanged(address: 3, speed: 40, forward: true);

        Assert.That(simulator.GetLocoInfoBroadcastCount("clientA"), Is.EqualTo(1));
        Assert.That(simulator.GetLocoInfoBroadcastCount("clientB"), Is.Zero,
            "Client B has Driving flag but did not subscribe to loco address 3.");
    }

    [Test]
    public void LocoInfoBroadcast_WithAllLocoInfoFlag_ReachesUnsubscribedClient()
    {
        var simulator = new Z21BroadcastSimulator();
        simulator.SetClientBroadcastFlags(
            "clientB",
            Z21BroadcastRequirements.DrivingBroadcastFlag | Z21BroadcastRequirements.AllLocoInfoBroadcastFlag);

        simulator.PublishLocoDriveChanged(address: 7, speed: 10, forward: false);

        Assert.That(simulator.GetLocoInfoBroadcastCount("clientB"), Is.EqualTo(1));
    }

    [Test]
    public void LocoSubscription_IsLimitedToSixteenAddresses()
    {
        var simulator = new Z21BroadcastSimulator();
        simulator.SetClientBroadcastFlags("clientA", Z21BroadcastRequirements.DrivingBroadcastFlag);

        for (var address = 1; address <= Z21BroadcastRequirements.MaxSubscribedLocomotiveAddresses; address++)
        {
            Assert.That(simulator.SubscribeLocomotive("clientA", address), Is.True);
        }

        Assert.That(
            simulator.SubscribeLocomotive("clientA", Z21BroadcastRequirements.MaxSubscribedLocomotiveAddresses + 1),
            Is.False);
    }

    [Test]
    public void TurnoutInfoBroadcast_ReachesClientsWithDrivingFlag()
    {
        var simulator = new Z21BroadcastSimulator();
        simulator.SetClientBroadcastFlags("clientA", Z21BroadcastRequirements.DrivingBroadcastFlag);
        simulator.SetClientBroadcastFlags("clientB", 0);

        simulator.PublishTurnoutChanged(functionAddress: 4, outputPosition: true);

        Assert.Multiple(() =>
        {
            Assert.That(simulator.GetTurnoutInfoBroadcastCount("clientA"), Is.EqualTo(1));
            Assert.That(simulator.GetTurnoutInfoBroadcastCount("clientB"), Is.Zero);
        });
    }

    [Test]
    public void TryParseTurnoutInfo_DecodesPositionOnly_NotSignalAspect()
    {
        // ZZ=10 -> P=1 per spec section 5.3
        byte[] packet =
        [
            0x09, 0x00, 0x40, 0x00,
            0x43, 0x00, 0x04, 0x02,
            0x45
        ];

        var ok = Z21MessageParser.TryParseTurnoutInfo(packet, out var turnout);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.True);
            Assert.That(turnout, Is.Not.Null);
            Assert.That(turnout!.FunctionAddress, Is.EqualTo(4));
            Assert.That(turnout.IsSwitched, Is.True);
            Assert.That(turnout.OutputPosition, Is.True);
        });
    }

    [Test]
    public void TryParseTurnoutInfo_NotSwitched_ReturnsIsSwitchedFalse()
    {
        byte[] packet =
        [
            0x09, 0x00, 0x40, 0x00,
            0x43, 0x00, 0x04, 0x00,
            0x47
        ];

        var ok = Z21MessageParser.TryParseTurnoutInfo(packet, out var turnout);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.True);
            Assert.That(turnout!.IsSwitched, Is.False);
        });
    }

    /// <summary>
    /// Minimal in-memory model of Z21 per-client broadcast routing from LAN_SET_BROADCASTFLAGS.
    /// </summary>
    private sealed class Z21BroadcastSimulator
    {
        private readonly Dictionary<string, uint> _clientFlags = new(StringComparer.Ordinal);
        private readonly Dictionary<string, HashSet<int>> _locoSubscriptions = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _locoBroadcastCounts = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _turnoutBroadcastCounts = new(StringComparer.Ordinal);

        public void SetClientBroadcastFlags(string clientId, uint flags) => _clientFlags[clientId] = flags;

        public bool SubscribeLocomotive(string clientId, int address)
        {
            if (!_locoSubscriptions.TryGetValue(clientId, out var set))
            {
                set = [];
                _locoSubscriptions[clientId] = set;
            }

            if (set.Count >= Z21BroadcastRequirements.MaxSubscribedLocomotiveAddresses)
            {
                return false;
            }

            set.Add(address);
            return true;
        }

        public void PublishLocoDriveChanged(int address, int speed, bool forward)
        {
            _ = speed;
            _ = forward;

            foreach (var (clientId, flags) in _clientFlags)
            {
                if ((flags & Z21BroadcastRequirements.DrivingBroadcastFlag) == 0)
                {
                    continue;
                }

                var receivesAll = (flags & Z21BroadcastRequirements.AllLocoInfoBroadcastFlag) != 0;
                var subscribed = _locoSubscriptions.TryGetValue(clientId, out var set) && set.Contains(address);
                if (!receivesAll && !subscribed)
                {
                    continue;
                }

                _locoBroadcastCounts[clientId] = GetLocoInfoBroadcastCount(clientId) + 1;
            }
        }

        public void PublishTurnoutChanged(int functionAddress, bool outputPosition)
        {
            _ = functionAddress;
            _ = outputPosition;

            foreach (var (clientId, flags) in _clientFlags)
            {
                if ((flags & Z21BroadcastRequirements.DrivingBroadcastFlag) == 0)
                {
                    continue;
                }

                _turnoutBroadcastCounts[clientId] = GetTurnoutInfoBroadcastCount(clientId) + 1;
            }
        }

        public int GetLocoInfoBroadcastCount(string clientId) =>
            _locoBroadcastCounts.TryGetValue(clientId, out var count) ? count : 0;

        public int GetTurnoutInfoBroadcastCount(string clientId) =>
            _turnoutBroadcastCounts.TryGetValue(clientId, out var count) ? count : 0;
    }
}
