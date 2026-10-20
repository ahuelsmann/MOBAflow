// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Backend.Model;
using Moba.Backend.Protocol;
using Moba.Backend.Service;
using Moba.Common.Events;
using Moba.Test.TestData;

using Mocks;

/// <summary>
/// Receive-side contracts of <see cref="Z21"/>: packet dispatch, connection confirmation, ordering and overload.
/// </summary>
[TestFixture]
internal sealed class Z21ReceiveTests
{
    private static readonly byte[] LocoInfoAddress3 =
    [
        0x0B, 0x00, 0x40, 0x00, 0xEF,
        0x00, 0x03, // address 3
        0x04,       // 128 speed steps
        0x8B,       // forward, raw speed 11 => speed step 10
        0x11,       // DB4: F0 and F1
        0x05        // DB5: F5 and F7
    ];

    private static readonly byte[] SystemStatePacket =
    [
        0x14, 0x00, 0x84, 0x00,
        0xE8, 0x03, // main current 1000 mA
        0x14, 0x00, // programming current 20 mA
        0xDE, 0x03, // filtered main current 990 mA
        0x23, 0x00, // temperature 35 C
        0x50, 0x46, // supply voltage 18000 mV
        0x5C, 0x44, // VCC voltage 17500 mV
        0x02,       // central state
        0x01,       // extended central state
        0x00, 0x00
    ];

    private static readonly byte[] SerialNumber0x12345678 = [0x08, 0x00, 0x10, 0x00, 0x78, 0x56, 0x34, 0x12];

    private static readonly byte[] HardwareInfo =
    [
        0x0C, 0x00, 0x1A, 0x00,
        0x01, 0x02, 0x00, 0x00, // hardware type 0x0201
        0x20, 0x01, 0x00, 0x00  // firmware 0x0120
    ];

    private static readonly int[] ExpectedLocoAddresses = [3];
    private static readonly bool[] FirstFeedbackActive = [true];
    private static readonly bool[] InactiveThenActive = [false, true];

    private static readonly byte[] LanXVersionXBus0x40 = [0x09, 0x00, 0x40, 0x00, 0x63, 0x21, 0x40, 0x00, 0x12];

    [Test]
    public async Task PacketsShorterThanTheHeader_AreDroppedBeforeTrafficLogging()
    {
        var monitor = new Z21Monitor(NullLogger<Z21Monitor>.Instance);
        var harness = new Harness(monitor);

        harness.Udp.RaiseReceived([0x03, 0x00, 0x40]);
        harness.Udp.RaiseReceived([0x04, 0x00, 0x99, 0x00]);
        await harness.Z21.DisposeAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(monitor.GetPackets().Single().Data, Has.Length.EqualTo(4));
            Assert.That(harness.Events, Is.Empty);
            Assert.That(harness.Z21.IsConnected, Is.False);
        }
    }

    [Test]
    public async Task FirstResponse_ConfirmsTheConnectionExactlyOnce()
    {
        var harness = new Harness();
        var connectedChanges = new List<bool>();
        harness.Z21.OnConnectedChanged += connectedChanges.Add;

        harness.Udp.RaiseReceived(Z21Packets.XBusStatusChangedAllFlags);
        harness.Udp.RaiseReceived(Z21Packets.XBusStatusChangedAllFlags);
        await harness.Z21.DisposeAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(harness.Z21.IsConnected, Is.True);
            Assert.That(connectedChanges, Has.Count.EqualTo(1).And.All.True);
            Assert.That(harness.Events.OfType<Z21ConnectionEstablishedEvent>().Count(), Is.EqualTo(1));
            Assert.That(harness.Events[0], Is.TypeOf<Z21ConnectionEstablishedEvent>());
        }
    }

    [Test]
    public async Task XBusStatus_PublishesEveryFlag()
    {
        var harness = new Harness();
        XBusStatus? raised = null;
        harness.Z21.OnXBusStatusChanged += status => raised = status;

        harness.Udp.RaiseReceived(Z21Packets.XBusStatusChangedAllFlags);
        await harness.Z21.DisposeAsync().ConfigureAwait(false);

        var published = harness.Events.OfType<XBusStatusChangedEvent>().Single();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(published.EmergencyStop, Is.True);
            Assert.That(published.TrackOff, Is.True);
            Assert.That(published.ShortCircuit, Is.True);
            Assert.That(published.Programming, Is.False);
            Assert.That(raised, Is.EqualTo(new XBusStatus(true, true, true, false)));
        }
    }

    [Test]
    public async Task LocoInfo_PublishesDecodedStateAndConfirmsConnection()
    {
        var harness = new Harness();
        var raised = new List<LocoInfo>();
        harness.Z21.OnLocoInfoChanged += raised.Add;

        harness.Udp.RaiseReceived(LocoInfoAddress3);
        await harness.Z21.DisposeAsync().ConfigureAwait(false);

        var published = harness.Events.OfType<LocomotiveInfoChangedEvent>().Single();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(harness.Z21.IsConnected, Is.True);
            Assert.That(raised.Select(info => info.Address), Is.EqualTo(ExpectedLocoAddresses));
            Assert.That(published.Address, Is.EqualTo(3));
            Assert.That(published.Speed, Is.EqualTo(10));
            Assert.That(published.IsForward, Is.True);
            Assert.That(published.IsF0On, Is.True);
            Assert.That(published.IsF1On, Is.True);
            Assert.That(published.IsF2On, Is.False);
            Assert.That(published.IsF5On, Is.True);
            Assert.That(published.IsF6On, Is.False);
            Assert.That(published.IsF7On, Is.True);
        }
    }

    [Test]
    public async Task TruncatedLocoInfo_IsIgnored()
    {
        var harness = new Harness();

        harness.Udp.RaiseReceived(LocoInfoAddress3[..10]);
        await harness.Z21.DisposeAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(harness.Events, Is.Empty);
            Assert.That(harness.Z21.IsConnected, Is.False);
        }
    }

    [Test]
    public async Task SystemState_PublishesMeasurementsAndUpdatesCurrentState()
    {
        var harness = new Harness();
        SystemState? raised = null;
        harness.Z21.OnSystemStateChanged += state => raised = state;

        harness.Udp.RaiseReceived(SystemStatePacket);
        await harness.Z21.DisposeAsync().ConfigureAwait(false);

        var published = harness.Events.OfType<SystemStateChangedEvent>().Single();
        var current = harness.Z21.CurrentSystemState;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(harness.Z21.IsConnected, Is.True);
            Assert.That(raised, Is.SameAs(current));
            Assert.That(
                (current.MainCurrent, current.ProgCurrent, current.FilteredMainCurrent, current.Temperature,
                    current.SupplyVoltage, current.VccVoltage, current.CentralState, current.CentralStateEx),
                Is.EqualTo((1000, 20, 990, 35, 18000, 17500, (byte)0x02, (byte)0x01)));
            Assert.That(
                (published.MainCurrent, published.ProgCurrent, published.FilteredMainCurrent, published.Temperature,
                    published.SupplyVoltage, published.VccVoltage, published.CentralState, published.CentralStateEx),
                Is.EqualTo((1000, 20, 990, 35, 18000, 17500, 0x02, 0x01)));
        }
    }

    [Test]
    public async Task TruncatedSystemState_KeepsPreviousStateAndPublishesNothing()
    {
        var harness = new Harness();
        var previous = harness.Z21.CurrentSystemState;

        harness.Udp.RaiseReceived(SystemStatePacket[..17]);
        await harness.Z21.DisposeAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(harness.Z21.CurrentSystemState, Is.SameAs(previous));
            Assert.That(harness.Events, Is.Empty);
            Assert.That(harness.Z21.IsConnected, Is.False);
        }
    }

    [Test]
    public async Task SerialNumberAndHardwareInfo_AccumulateVersionInformation()
    {
        var harness = new Harness();
        var raisedSerialNumbers = new List<uint>();
        harness.Z21.OnVersionInfoChanged += info => raisedSerialNumbers.Add(info.SerialNumber);

        harness.Udp.RaiseReceived(SerialNumber0x12345678);
        harness.Udp.RaiseReceived(HardwareInfo);
        await harness.Z21.DisposeAsync().ConfigureAwait(false);

        var published = harness.Events.OfType<VersionInfoChangedEvent>()
            .Select(@event => (@event.SerialNumber, @event.HardwareTypeCode, @event.FirmwareVersionCode));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(harness.Z21.IsConnected, Is.True);
            Assert.That(published, Is.EqualTo(new[] { (0x12345678u, 0, 0), (0x12345678u, 0x0201, 0x0120) }));
            Assert.That(harness.Z21.VersionInfo?.SerialNumber, Is.EqualTo(0x12345678u));
            Assert.That(harness.Z21.VersionInfo?.HardwareTypeCode, Is.EqualTo(0x0201u));
            Assert.That(harness.Z21.VersionInfo?.FirmwareVersionCode, Is.EqualTo(0x0120u));
            Assert.That(raisedSerialNumbers, Has.Count.EqualTo(2).And.All.EqualTo(0x12345678u));
        }
    }

    [Test]
    public async Task LanXVersion_ShowsTheXBusVersionOnlyWhileTheHardwareIsUnknown()
    {
        var unknownHardware = new Harness();
        unknownHardware.Udp.RaiseReceived(LanXVersionXBus0x40);
        await unknownHardware.Z21.DisposeAsync().ConfigureAwait(false);

        var knownSerialNumber = new Harness();
        knownSerialNumber.Udp.RaiseReceived(SerialNumber0x12345678);
        knownSerialNumber.Udp.RaiseReceived(LanXVersionXBus0x40);
        await knownSerialNumber.Z21.DisposeAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(unknownHardware.Z21.IsConnected, Is.True);
            Assert.That(unknownHardware.Z21.VersionInfo?.FirmwareVersionCode, Is.EqualTo(0x40u));
            Assert.That(
                unknownHardware.Events.OfType<VersionInfoChangedEvent>().Single().FirmwareVersionCode,
                Is.EqualTo(0x40));
            Assert.That(knownSerialNumber.Z21.VersionInfo?.FirmwareVersionCode, Is.Zero);
            Assert.That(knownSerialNumber.Events.OfType<VersionInfoChangedEvent>().Count(), Is.EqualTo(2));
        }
    }

    [Test]
    public async Task MixedPackets_ArePublishedInArrivalOrder()
    {
        var harness = new Harness();

        harness.Udp.RaiseReceived(Z21Packets.XBusStatusChangedAllFlags);
        harness.Udp.RaiseReceived(LocoInfoAddress3);
        harness.Udp.RaiseReceived(Z21Packets.RBusFeedbackInPort5);
        harness.Udp.RaiseReceived(SystemStatePacket);
        harness.Udp.RaiseReceived(SerialNumber0x12345678);
        await harness.Z21.DisposeAsync().ConfigureAwait(false);

        Assert.That(harness.Events.Select(@event => @event.GetType()), Is.EqualTo(new[]
        {
            typeof(Z21ConnectionEstablishedEvent),
            typeof(XBusStatusChangedEvent),
            typeof(LocomotiveInfoChangedEvent),
            typeof(FeedbackReceivedEvent),
            typeof(FeedbackStateChangedEvent),
            typeof(SystemStateChangedEvent),
            typeof(VersionInfoChangedEvent)
        }));
    }

    [Test]
    public async Task Overload_RejectsNewestEventsWithoutBlockingTheReceiverAndKeepsAcceptedOrder()
    {
        using var releaseSubscriber = new ManualResetEventSlim();
        var subscriberEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var harness = new Harness(blockFirstFeedback: () =>
        {
            subscriberEntered.TrySetResult();
            releaseSubscriber.Wait(TimeSpan.FromSeconds(10));
        });

        // InPort 1 active: FeedbackReceived blocks the consumer, FeedbackStateChanged(true) waits in the queue.
        harness.Udp.RaiseReceived(RBusGroup0(0x01));
        await subscriberEntered.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        // Each inactive/active pair queues three events: StateChanged(false), Received, StateChanged(true).
        await Task.Run(() =>
        {
            for (var pair = 0; pair < 600; pair++)
            {
                harness.Udp.RaiseReceived(RBusGroup0(0x00));
                harness.Udp.RaiseReceived(RBusGroup0(0x01));
            }
        }).WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        var duringOverload = harness.Z21.EventPipelineSnapshot;
        releaseSubscriber.Set();
        await harness.Z21.DisposeAsync().ConfigureAwait(false);

        var acceptedPairs = (Z21EventPipeline.DefaultCapacity - 1) / 3;
        var expectedStates = FirstFeedbackActive
            .Concat(Enumerable.Repeat(InactiveThenActive, acceptedPairs).SelectMany(states => states));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(duringOverload.QueueDepth, Is.EqualTo(Z21EventPipeline.DefaultCapacity));
            Assert.That(duringOverload.PeakQueueDepth, Is.EqualTo(Z21EventPipeline.DefaultCapacity));
            Assert.That(duringOverload.EnqueuedEvents, Is.EqualTo(Z21EventPipeline.DefaultCapacity + 1));
            Assert.That(duringOverload.RejectedEvents, Is.EqualTo((600 * 3) - (Z21EventPipeline.DefaultCapacity - 1)));
            Assert.That(harness.Z21.EventPipelineSnapshot.PublishedEvents, Is.EqualTo(Z21EventPipeline.DefaultCapacity + 1));
            Assert.That(
                harness.Events.OfType<FeedbackStateChangedEvent>().Select(@event => @event.IsActive),
                Is.EqualTo(expectedStates));
        }
    }

    private static byte[] RBusGroup0(byte firstStateByte) =>
    [
        0x0F, 0x00, 0x80, 0x00, 0x00,
        firstStateByte, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00
    ];

    private sealed class Harness
    {
        private readonly Lock _lock = new();
        private readonly List<IEvent> _events = [];

        public Harness(Z21Monitor? monitor = null, Action? blockFirstFeedback = null)
        {
            var eventBus = new EventBus(NullLogger<EventBus>.Instance);
            Subscribe<Z21ConnectionEstablishedEvent>(eventBus);
            Subscribe<XBusStatusChangedEvent>(eventBus);
            Subscribe<LocomotiveInfoChangedEvent>(eventBus);
            Subscribe<FeedbackStateChangedEvent>(eventBus);
            Subscribe<SystemStateChangedEvent>(eventBus);
            Subscribe<VersionInfoChangedEvent>(eventBus);
            Subscribe<TurnoutInfoChangedEvent>(eventBus);
            var feedbackCount = 0;
            eventBus.Subscribe<FeedbackReceivedEvent>(@event =>
            {
                if (Interlocked.Increment(ref feedbackCount) == 1)
                {
                    blockFirstFeedback?.Invoke();
                }

                Record(@event);
            });
            Z21 = new Z21(Udp, eventBus, trafficMonitor: monitor);
        }

        public FakeUdpClientWrapper Udp { get; } = new();

        public Z21 Z21 { get; }

        public IEvent[] Events
        {
            get
            {
                lock (_lock)
                {
                    return _events.ToArray();
                }
            }
        }

        private void Subscribe<TEvent>(EventBus eventBus) where TEvent : class, IEvent =>
            eventBus.Subscribe<TEvent>(Record);

        private void Record(IEvent @event)
        {
            lock (_lock)
            {
                _events.Add(@event);
            }
        }
    }
}
