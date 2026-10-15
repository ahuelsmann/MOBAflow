// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using Moba.Backend.Model;
using Moba.Backend.Protocol;

/// <summary>
/// Verifies Z21 LAN broadcast prerequisites documented in docs/z21-lan-protokoll.pdf v1.13.
/// </summary>
[TestFixture]
internal sealed class Z21BroadcastProtocolTests
{
    /// <summary>Max subscribed locomotive addresses per client without the all-loco-info flag (protocol v1.13).</summary>
    private const int MaxSubscribedLocomotiveAddresses = 16;

    // Independent protocol v1.13 literals, so a changed or dropped production flag fails these tests.
    private const uint SpecDrivingFlag = 0x0000_0001;
    private const uint SpecRbusFlag = 0x0000_0002;
    private const uint SpecSystemStateFlag = 0x0000_0100;
    private const uint SpecAllLocoInfoFlag = 0x0001_0000;
    private const uint SpecMobaFlowBasicFlags = 0x0000_0103;

    /// <summary>LAN_SET_BROADCASTFLAGS: length 8, header 0x50, flags 0x00000103 little-endian.</summary>
    private static readonly byte[] SpecBasicBroadcastFlagsPacket = [0x08, 0x00, 0x50, 0x00, 0x03, 0x01, 0x00, 0x00];

    [Test]
    public void BroadcastFlags_MatchProtocolSpecificationLiterals()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Z21Protocol.BroadcastFlags.Driving, Is.EqualTo(SpecDrivingFlag));
            Assert.That(Z21Protocol.BroadcastFlags.Rbus, Is.EqualTo(SpecRbusFlag));
            Assert.That(Z21Protocol.BroadcastFlags.SystemState, Is.EqualTo(SpecSystemStateFlag));
            Assert.That(Z21Protocol.BroadcastFlags.AllLocoInfo, Is.EqualTo(SpecAllLocoInfoFlag));
            Assert.That(Z21Protocol.BroadcastFlags.Basic, Is.EqualTo(SpecMobaFlowBasicFlags),
                "MOBAflow basic subscription is driving + R-Bus + system state, without all-loco-info.");
        }
    }

    [Test]
    public void MobaFlowBasicBroadcastFlags_IncludesDrivingButNotAllLocoInfo()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                Z21Protocol.BroadcastFlags.Basic & Z21Protocol.BroadcastFlags.Driving,
                Is.Not.Zero);
            Assert.That(
                Z21Protocol.BroadcastFlags.Basic & Z21Protocol.BroadcastFlags.AllLocoInfo,
                Is.Zero);
        });
    }

    [Test]
    public void BuildBroadcastFlagsBasic_MatchesMobaFlowBasicFlags()
    {
        var packet = Z21Command.BuildBroadcastFlagsBasic();

        Assert.That(packet, Is.EqualTo(SpecBasicBroadcastFlagsPacket));
    }

    [Test]
    public void LocoInfoBroadcast_RequiresPerAddressSubscription_UnlessAllLocoInfoFlag()
    {
        var simulator = new Z21BroadcastSimulator();
        simulator.SetClientBroadcastFlags("clientA", Z21Protocol.BroadcastFlags.Driving);
        simulator.SetClientBroadcastFlags("clientB", Z21Protocol.BroadcastFlags.Driving);

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
            Z21Protocol.BroadcastFlags.Driving | Z21Protocol.BroadcastFlags.AllLocoInfo);

        simulator.PublishLocoDriveChanged(address: 7, speed: 10, forward: false);

        Assert.That(simulator.GetLocoInfoBroadcastCount("clientB"), Is.EqualTo(1));
    }

    [Test]
    public void LocoSubscription_IsLimitedToSixteenAddresses()
    {
        var simulator = new Z21BroadcastSimulator();
        simulator.SetClientBroadcastFlags("clientA", Z21Protocol.BroadcastFlags.Driving);

        for (var address = 1; address <= MaxSubscribedLocomotiveAddresses; address++)
        {
            Assert.That(simulator.SubscribeLocomotive("clientA", address), Is.True);
        }

        Assert.That(
            simulator.SubscribeLocomotive("clientA", MaxSubscribedLocomotiveAddresses + 1),
            Is.False);
    }

    [Test]
    public void TurnoutInfoBroadcast_ReachesClientsWithDrivingFlag()
    {
        var simulator = new Z21BroadcastSimulator();
        simulator.SetClientBroadcastFlags("clientA", Z21Protocol.BroadcastFlags.Driving);
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

            if (set.Count >= MaxSubscribedLocomotiveAddresses)
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
                if ((flags & Z21Protocol.BroadcastFlags.Driving) == 0)
                {
                    continue;
                }

                var receivesAll = (flags & Z21Protocol.BroadcastFlags.AllLocoInfo) != 0;
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
                if ((flags & Z21Protocol.BroadcastFlags.Driving) == 0)
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
