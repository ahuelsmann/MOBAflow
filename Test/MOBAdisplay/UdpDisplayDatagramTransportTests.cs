// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdisplay;

using Moba.Display.Runtime;
using Moba.Display.Transport;

using System.Net;
using System.Net.Sockets;

[TestFixture]
[Category("Unit")]
internal sealed partial class UdpDisplayDatagramTransportTests
{
    private static readonly byte[] ExpectedDatagram = [0x4D, 0x42];

    [Test]
    public async Task RunReceiveLoopAsync_Should_ContinueAfterTransientSocketFailure()
    {
        // Arrange
        var attempts = 0;
        var received = new List<byte[]>();
        using var cancellation = new CancellationTokenSource();

        async ValueTask<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken _)
        {
            attempts++;
            if (attempts == 1)
            {
                throw new SocketException((int)SocketError.ConnectionReset);
            }

            await cancellation.CancelAsync().ConfigureAwait(false);
            return ExpectedDatagram;
        }

        // Act
        await UdpDisplayDatagramTransport.RunReceiveLoopAsync(
            ReceiveAsync,
            datagram => received.Add(datagram.ToArray()),
            cancellation.Token).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(attempts, Is.EqualTo(2));
            Assert.That(received, Has.Count.EqualTo(1));
            Assert.That(received[0], Is.EqualTo(ExpectedDatagram));
        }
    }

    [Test]
    public async Task UdpDisplayFrameSender_Should_ReuseConnectionForSameEndpoint()
    {
        // Arrange
        var connections = new ConnectionRecorder();
        using var sender = new UdpDisplayFrameSender(connections.Create);
        var options = CreateOptions("127.0.0.1", 4210);

        // Act
        await sender.SendFrameAsync(ExpectedDatagram, options).ConfigureAwait(false);
        await sender.SendFrameAsync(ExpectedDatagram, options).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(connections.Count, Is.EqualTo(1));
            Assert.That(connections.First?.SendCount, Is.EqualTo(2));
            Assert.That(connections.First?.IsDisposed, Is.False);
        }
    }

    [Test]
    public async Task UdpDisplayFrameSender_Should_ReplaceConnectionWhenEndpointChanges()
    {
        // Arrange
        var connections = new ConnectionRecorder();
        using var sender = new UdpDisplayFrameSender(connections.Create);

        // Act
        await sender.SendFrameAsync(
            ExpectedDatagram,
            CreateOptions("127.0.0.1", 4210)).ConfigureAwait(false);
        await sender.SendFrameAsync(
            ExpectedDatagram,
            CreateOptions("127.0.0.2", 4211)).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(connections.Count, Is.EqualTo(2));
            Assert.That(connections.First?.Endpoint, Is.EqualTo(new IPEndPoint(IPAddress.Loopback, 4210)));
            Assert.That(connections.First?.IsDisposed, Is.True);
            Assert.That(connections.Second?.Endpoint, Is.EqualTo(new IPEndPoint(IPAddress.Parse("127.0.0.2"), 4211)));
        }
    }

    [TestCase("invalid", 1, 1, 4210)]
    [TestCase("127.0.0.1", 0, 1, 4210)]
    [TestCase("127.0.0.1", 1, 0, 4210)]
    [TestCase("127.0.0.1", 1, 1, 0)]
    public void UdpDisplayFrameSender_Should_RejectInvalidOptionsBeforeCreatingConnection(
        string ipAddress,
        int width,
        int height,
        int port)
    {
        // Arrange
        var connections = new ConnectionRecorder();
        using var sender = new UdpDisplayFrameSender(connections.Create);
        var options = new FrameLoopOptions
        {
            IpAddress = ipAddress,
            Port = port,
            Width = width,
            Height = height
        };

        // Act
        var exception = Assert.CatchAsync<ArgumentException>(
            async () => await sender.SendFrameAsync(ExpectedDatagram, options).ConfigureAwait(false));

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(connections.Count, Is.Zero);
        }
    }

    [Test]
    public async Task UdpDisplayFrameSender_Should_DisposeConnectionAndRejectFurtherSends()
    {
        // Arrange
        var connections = new ConnectionRecorder();
        var sender = new UdpDisplayFrameSender(connections.Create);
        var options = CreateOptions("127.0.0.1", 4210);
        await sender.SendFrameAsync(ExpectedDatagram, options).ConfigureAwait(false);

        // Act
        sender.Dispose();
        var exception = Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await sender.SendFrameAsync(ExpectedDatagram, options).ConfigureAwait(false));

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(connections.First?.IsDisposed, Is.True);
        }
    }

    private static FrameLoopOptions CreateOptions(string ipAddress, int port) =>
        new()
        {
            IpAddress = ipAddress,
            Port = port,
            Width = 1,
            Height = 1
        };

    private sealed class ConnectionRecorder
    {
        public RecordingConnection? First { get; private set; }

        public RecordingConnection? Second { get; private set; }

        public int Count { get; private set; }

        public RecordingConnection Create(IPEndPoint endpoint)
        {
            var connection = new RecordingConnection(endpoint);
            if (Count == 0)
            {
                First = connection;
            }
            else if (Count == 1)
            {
                Second = connection;
            }
            else
            {
                throw new InvalidOperationException("The test expected at most two display connections.");
            }

            Count++;
            return connection;
        }
    }

    private sealed partial class RecordingConnection(IPEndPoint endpoint) : IDisplayFrameSessionConnection
    {
        public IPEndPoint Endpoint { get; } = endpoint;

        public int SendCount { get; private set; }

        public bool IsDisposed { get; private set; }

        public Task SendFrameAsync(
            ReadOnlyMemory<byte> rgb565Frame,
            ushort width,
            ushort height,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            Assert.That(rgb565Frame.Length, Is.EqualTo(width * height * 2));
            SendCount++;
            return Task.CompletedTask;
        }

        public void Dispose() => IsDisposed = true;
    }
}