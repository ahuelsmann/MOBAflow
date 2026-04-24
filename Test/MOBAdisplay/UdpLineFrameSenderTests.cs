using System.Net;
using System.Net.Sockets;
using System.Text;

using Moba.Display.Rendering;
using Moba.Display.Transport;

namespace Moba.Test.MOBAdisplay;

[TestFixture]
internal sealed class UdpLineFrameSenderTests
{
    [Test]
    public void SendFrame_SendsStartLinesAndDone()
    {
        var frame = new byte[FrameDimensions.FrameByteCount];
        Array.Fill<byte>(frame, 0xAA);

        using var receiver = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        var port = ((IPEndPoint)receiver.Client.LocalEndPoint!).Port;
        receiver.Client.ReceiveTimeout = 2000;

        var packets = new List<byte[]>();
        var done = new ManualResetEventSlim(false);
        var receiveTask = Task.Run(() =>
        {
            try
            {
                while (!done.IsSet)
                {
                    IPEndPoint? remote = null;
                    var packet = receiver.Receive(ref remote);
                    packets.Add(packet);
                    if (Encoding.ASCII.GetString(packet) == "FRAME_DONE")
                    {
                        done.Set();
                        break;
                    }
                }
            }
            catch (SocketException)
            {
                // timeout
            }
        });

        using var sender = new UdpLineFrameSender();
        sender.SendFrame(frame, IPAddress.Loopback.ToString(), port);

        Assert.That(done.Wait(TimeSpan.FromSeconds(2)), Is.True, "Expected FRAME_DONE marker was not received.");
        receiveTask.Wait(TimeSpan.FromSeconds(1));

        Assert.That(Encoding.ASCII.GetString(packets[0]), Is.EqualTo("FRAME_START"));
        Assert.That(Encoding.ASCII.GetString(packets[^1]), Is.EqualTo("FRAME_DONE"));
        Assert.That(packets.Count - 2, Is.EqualTo(FrameDimensions.Height));
    }
}
