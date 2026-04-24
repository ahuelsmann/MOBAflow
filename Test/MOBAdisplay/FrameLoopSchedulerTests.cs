using Moba.Display.Rendering;
using Moba.Display.Runtime;
using Moba.Display.Transport;

namespace Moba.Test.MOBAdisplay;

[TestFixture]
internal sealed class FrameLoopSchedulerTests
{
    [Test]
    public async Task StartAsync_ForwardsTrackNumberToRenderer()
    {
        var renderer = new FakeFrameRenderer();
        var sender = new FakeFrameSender();
        var scheduler = new FrameLoopScheduler(renderer, sender)
        {
            TrackNumber = 42
        };

        await scheduler.StartAsync(new FrameLoopOptions
        {
            IpAddress = "127.0.0.1",
            Port = 4210,
            RefreshHz = 50
        });

        await Task.Delay(150);
        await scheduler.StopAsync();

        Assert.That(renderer.LastTrackNumber, Is.EqualTo(42));
    }

    [Test]
    public async Task StartAsync_RendersAndSendsFrames()
    {
        var renderer = new FakeFrameRenderer();
        var sender = new FakeFrameSender();
        var scheduler = new FrameLoopScheduler(renderer, sender);
        var eventCount = 0;
        scheduler.FrameReady += (_, _) => eventCount++;

        await scheduler.StartAsync(new FrameLoopOptions
        {
            IpAddress = "127.0.0.1",
            Port = 4210,
            RefreshHz = 20
        });

        await Task.Delay(250);
        await scheduler.StopAsync();

        Assert.That(renderer.RenderCount, Is.GreaterThanOrEqualTo(3));
        Assert.That(sender.SendCount, Is.GreaterThanOrEqualTo(3));
        Assert.That(eventCount, Is.GreaterThanOrEqualTo(3));
    }

    private sealed class FakeFrameRenderer : IFrameRenderer
    {
        public int RenderCount { get; private set; }

        public int LastTrackNumber { get; private set; }

        public void Render(FrameContext context, Span<byte> destinationRgb565)
        {
            LastTrackNumber = context.TrackNumber;
            RenderCount++;
            destinationRgb565.Fill(0x22);
        }
    }

    private sealed class FakeFrameSender : IFrameSender
    {
        public int SendCount { get; private set; }

        public Task SendFrameAsync(ReadOnlyMemory<byte> rgb565Frame, string ipAddress, int port, CancellationToken cancellationToken = default)
        {
            _ = rgb565Frame;
            _ = ipAddress;
            _ = port;
            _ = cancellationToken;
            SendCount++;
            return Task.CompletedTask;
        }
    }
}
