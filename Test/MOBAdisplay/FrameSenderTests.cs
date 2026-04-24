// Copyright (c) 2026 Andreas Huelsmann. 
// Licensed under MIT. See LICENSE and README.md for details.

using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

using Moba.Display;
using Moba.Display.Transport;

namespace Moba.Test.MOBAdisplay;

// These tests exercise the MOBAdisplay pipeline (bitmap -> RGB565 -> UDP frame).
// The real-device test is marked [Explicit] because it performs a live UDP send
// to a hard-coded ESP32 IP address and should only be run on demand.
[TestFixture]
internal class FrameSenderTests
{
    private string _testPng = string.Empty;

    [SetUp]
    public void Setup()
    {
        _testPng = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "TestFile", "MOBAdisplay", "test.png");

        Assert.That(File.Exists(_testPng), Is.True,
            $"Test image not found: {_testPng}");
    }

    [Test]
    [Platform("Win")]
    public void Convert_TestPng_ProducesRgb565Buffer()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Ignore("System.Drawing.Bitmap is only supported on Windows.");
        }

        using var bmp = new Bitmap(_testPng);
        var frame = BitmapToRgb565.Convert(bmp);

        Assert.That(frame, Is.Not.Null);
        Assert.That(frame.Length, Is.EqualTo(bmp.Width * bmp.Height * 2),
            "RGB565 buffer must contain exactly 2 bytes per pixel.");
    }

    [Test]
    [Platform("Win")]
    public void SendFrame_ToLocalUdpReceiver_DeliversChunksAndFrameDoneMarker()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Ignore("System.Drawing.Bitmap is only supported on Windows.");
        }

        using var bmp = new Bitmap(_testPng);
        var frame = BitmapToRgb565.Convert(bmp);

        using var receiver = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        int port = ((IPEndPoint)receiver.Client.LocalEndPoint!).Port;

        var received = new List<byte[]>();
        var done = new ManualResetEventSlim(false);

        var receiveTask = Task.Run(() =>
        {
            receiver.Client.ReceiveTimeout = 2000;
            try
            {
                while (!done.IsSet)
                {
                    IPEndPoint? remote = null;
                    var data = receiver.Receive(ref remote);
                    received.Add(data);

                    if (Encoding.ASCII.GetString(data) == "FRAME_DONE")
                    {
                        done.Set();
                        break;
                    }
                }
            }
            catch (SocketException)
            {
                // timeout ends the loop
            }
        });

        SendFrameToPort(frame, IPAddress.Loopback, port);

        Assert.That(done.Wait(TimeSpan.FromSeconds(2)), Is.True,
            "Expected FRAME_DONE marker was not received.");
        receiveTask.Wait(TimeSpan.FromSeconds(1));

        Assert.That(received, Is.Not.Empty);
        Assert.That(Encoding.ASCII.GetString(received[^1]), Is.EqualTo("FRAME_DONE"));

        int payloadBytes = received.Take(received.Count - 1).Sum(p => p.Length);
        Assert.That(payloadBytes, Is.EqualTo(frame.Length),
            "Sum of chunked payload bytes must equal the original frame size.");
    }

    // Mirrors FrameSender.SendFrame but targets a caller-supplied port so the
    // chunking behavior can be validated against a local loopback receiver.
    private static void SendFrameToPort(byte[] rgb565Frame, IPAddress ip, int port)
    {
        using var client = new UdpClient();
        client.Connect(new IPEndPoint(ip, port));

        const int chunkSize = 1024;
        for (int i = 0; i < rgb565Frame.Length; i += chunkSize)
        {
            int size = Math.Min(chunkSize, rgb565Frame.Length - i);
            client.Send(rgb565Frame.AsSpan(i, size).ToArray());
        }

        var end = Encoding.ASCII.GetBytes("FRAME_DONE");
        client.Send(end);
    }

    // NEW: Sends a frame using the ESP32 line-based protocol:
    // FRAME_START
    // 280 × (240×2 bytes)
    // FRAME_DONE
    private static void SendFrameLinesToPort(byte[] rgb565Frame, IPAddress ip, int port)
    {
        using var client = new UdpClient();
        client.Connect(new IPEndPoint(ip, port));

        const int WIDTH = 240;
        const int HEIGHT = 280;

        // FRAME_START
        var start = Encoding.ASCII.GetBytes("FRAME_START");
        client.Send(start);

        // Zeilen senden
        for (int y = 0; y < HEIGHT; y++)
        {
            int offset = y * WIDTH * 2;
            byte[] line = new byte[WIDTH * 2];
            Buffer.BlockCopy(rgb565Frame, offset, line, 0, WIDTH * 2);

            client.Send(line);
        }

        // FRAME_DONE
        var end = Encoding.ASCII.GetBytes("FRAME_DONE");
        client.Send(end);
    }

    // NEW: Test for the ESP32 line protocol
    [Test]
    [Platform("Win")]
    public void SendFrameLines_ToLocalUdpReceiver_ProducesStartLinesDone()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Assert.Ignore("System.Drawing.Bitmap is only supported on Windows.");

        using var bmp = new Bitmap(_testPng);
        var frame = BitmapToRgb565.Convert(bmp);

        using var receiver = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        int port = ((IPEndPoint)receiver.Client.LocalEndPoint!).Port;

        var received = new List<byte[]>();
        var done = new ManualResetEventSlim(false);

        var receiveTask = Task.Run(() =>
        {
            receiver.Client.ReceiveTimeout = 2000;
            try
            {
                while (!done.IsSet)
                {
                    IPEndPoint? remote = null;
                    var data = receiver.Receive(ref remote);
                    received.Add(data);

                    if (Encoding.ASCII.GetString(data) == "FRAME_DONE")
                    {
                        done.Set();
                        break;
                    }
                }
            }
            catch (SocketException)
            {
                // timeout ends the loop
            }
        });

        using var sender = new UdpLineFrameSender();
        sender.SendFrame(frame, IPAddress.Loopback.ToString(), port);

        Assert.That(done.Wait(TimeSpan.FromSeconds(2)), Is.True,
            "Expected FRAME_DONE marker was not received.");
        receiveTask.Wait(TimeSpan.FromSeconds(1));

        Assert.That(received.Count, Is.GreaterThan(2),
            "Expected FRAME_START + multiple lines + FRAME_DONE.");

        Assert.That(Encoding.ASCII.GetString(received[0]), Is.EqualTo("FRAME_START"));
        Assert.That(Encoding.ASCII.GetString(received[^1]), Is.EqualTo("FRAME_DONE"));

        int payloadBytes = received
            .Skip(1)
            .Take(received.Count - 2)
            .Sum(p => p.Length);

        Assert.That(payloadBytes, Is.EqualTo(frame.Length),
            "Sum of line payload bytes must equal the original frame size.");
    }

    // Live hardware smoke test. Run manually via:
    //   dotnet test --filter FullyQualifiedName~SendFrame_ToEsp32Display_Live
    [Test]
    [Explicit("Sends a real UDP frame to the ESP32 display at 192.168.0.82.")]
    [Platform("Win")]
    public void SendFrame_ToEsp32Display_Live()
    {
        using var bmp = new Bitmap(_testPng); // 240x280
        var frame = BitmapToRgb565.Convert(bmp);

        using var sender = new UdpLineFrameSender();
        sender.SendFrame(frame, IPAddress.Parse("192.168.0.82").ToString(), 4210);

        TestContext.Out.WriteLine("Frame gesendet.");
    }
}
