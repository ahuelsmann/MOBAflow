// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdisplay;

using Moba.Display.Transport;

using System.Net.Sockets;

[TestFixture]
internal sealed class UdpDisplayDatagramTransportTests
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
}