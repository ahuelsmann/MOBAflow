// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using Microsoft.Extensions.Logging.Abstractions;

using Moba.Backend.Protocol;
using Moba.Common.Events;

using Mocks;

using System.Net;

using TestData;

[TestFixture]
internal class Z21WrapperTests
{
    [Test]
    public async Task ConnectAsync_UsesWrapper_AndSendsHandshakeAndBroadcast()
    {
        var fake = new FakeUdpClientWrapper();
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var z21 = new Z21(fake, eventBus);

        await z21.ConnectAsync(IPAddress.Loopback);

        // Wait a bit for async operations to complete
        await Task.Delay(200);

        // ConnectAsync sends: Handshake + SetBroadcastFlags + GetStatus + RequestVersionInfo (4 commands)
        Assert.That(fake.SentPayloads, Has.Count.GreaterThanOrEqualTo(2), "At least 2 payloads should be sent");
        var handshake = fake.SentPayloads[0];
        var broadcast = fake.SentPayloads[1];
        Assert.That(BitConverter.ToString([.. handshake.Take(4)]), Is.EqualTo("04-00-85-00"));
        Assert.That(BitConverter.ToString([.. broadcast.Take(4)]), Is.EqualTo("08-00-50-00"));

        await z21.DisconnectAsync();
    }

    [Test]
    public void Received_RaisesFeedback_ForRBusPacket()
    {
        var signal = new ManualResetEventSlim(false);
        var fake = new FakeUdpClientWrapper();
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var z21 = new Z21(fake, eventBus);

        FeedbackResult? captured = null;

        void Handler(FeedbackResult f)
        {
            captured = f;
            signal.Set();
        }

        z21.Received += Handler;
        try
        {
            fake.RaiseReceived(Z21Packets.RBusFeedbackInPort5);

            Assert.That(signal.Wait(TimeSpan.FromSeconds(1)), Is.True, "Received event not raised");
            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.InPort, Is.EqualTo(5u));
        }
        finally
        {
            z21.Received -= Handler;
            signal.Dispose();
        }
    }

    [Test]
    public void Received_RaisesOnlyForRisingEdges_AndSupportsReactivation()
    {
        var fake = new FakeUdpClientWrapper();
        var z21 = new Z21(fake, new EventBus(NullLogger<EventBus>.Instance));
        var activations = new List<int>();
        z21.Received += feedback => activations.Add(feedback.InPort);

        fake.RaiseReceived(Z21Packets.RBusFeedbackInPort5);
        fake.RaiseReceived(Z21Packets.RBusFeedbackInPort5);
        var released = Z21Packets.RBusFeedbackInPort5.ToArray();
        Array.Clear(released, 5, Math.Min(8, released.Length - 5));
        fake.RaiseReceived(released);
        fake.RaiseReceived(Z21Packets.RBusFeedbackInPort5);

        Assert.That(activations, Is.EqualTo(new[] { 5, 5 }));
    }

    [Test]
    public void Received_RaisesEveryNewlyActiveInPort_InOnePacket()
    {
        var fake = new FakeUdpClientWrapper();
        var z21 = new Z21(fake, new EventBus(NullLogger<EventBus>.Instance));
        var activations = new List<int>();
        z21.Received += feedback => activations.Add(feedback.InPort);
        var packet = Z21Packets.RBusFeedbackInPort5.ToArray();
        packet[5] = 0b0011_0000;

        fake.RaiseReceived(packet);

        Assert.That(activations, Is.EqualTo(new[] { 5, 6 }));
    }

    [Test]
    public void XBusStatusChanged_IsRaised_WhenStatusPacketArrives()
    {
        var signal = new ManualResetEventSlim(false);
        var fake = new FakeUdpClientWrapper();
        var eventBus = new EventBus(NullLogger<EventBus>.Instance);
        var z21 = new Z21(fake, eventBus);
        XBusStatus? status = null;

        void Handler(XBusStatus s)
        {
            status = s;
            signal.Set();
        }

        z21.OnXBusStatusChanged += Handler;
        try
        {
            fake.RaiseReceived(Z21Packets.XBusStatusChangedAllFlags);

            Assert.That(signal.Wait(TimeSpan.FromSeconds(1)), Is.True, "XBusStatusChanged not raised");
            Assert.That(status, Is.Not.Null);
            Assert.That(status!.EmergencyStop, Is.True);
            Assert.That(status.TrackOff, Is.True);
            Assert.That(status.ShortCircuit, Is.True);
            Assert.That(status.Programming, Is.False);
        }
        finally
        {
            z21.OnXBusStatusChanged -= Handler;
            signal.Dispose();
        }
    }
}
