// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using Moba.Backend.Protocol;
using Mocks;
using System.Net;
using Moba.Test.TestData;

[TestFixture]
public class Z21WrapperTests
{
    [Test]
    public async Task ConnectAsync_UsesWrapper_AndSendsHandshakeAndBroadcast()
    {
        var fake = new FakeUdpClientWrapper();
        var z21 = new Z21(fake);

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
        var fake = new FakeUdpClientWrapper();
        var z21 = new Z21(fake);

        FeedbackResult? captured = null;
        // ReSharper disable once AccessToDisposedClosure
        using var signal = new ManualResetEventSlim(false);
        z21.Received += f => { captured = f; signal.Set(); };

        fake.RaiseReceived(Z21Packets.RBusFeedbackInPort5);

        Assert.That(signal.Wait(TimeSpan.FromSeconds(1)), Is.True, "Received event not raised");
        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.InPort, Is.EqualTo(5u));
    }

    [Test]
    public void XBusStatusChanged_IsRaised_WhenStatusPacketArrives()
    {
        var fake = new FakeUdpClientWrapper();
        var z21 = new Z21(fake);
        XBusStatus? status = null;
        // ReSharper disable once AccessToDisposedClosure
        using var signal = new ManualResetEventSlim(false);
        z21.OnXBusStatusChanged += s => { status = s; signal.Set(); };

        fake.RaiseReceived(Z21Packets.XBusStatusChangedAllFlags);

        Assert.That(signal.Wait(TimeSpan.FromSeconds(1)), Is.True, "XBusStatusChanged not raised");
        Assert.That(status, Is.Not.Null);
        Assert.That(status!.EmergencyStop, Is.True);
        Assert.That(status.TrackOff, Is.True);
        Assert.That(status.ShortCircuit, Is.True);
        Assert.That(status.Programming, Is.False);
    }
}
