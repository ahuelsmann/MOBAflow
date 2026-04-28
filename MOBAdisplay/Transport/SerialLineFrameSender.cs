using System.IO.Ports;
using System.Text;

using Moba.Display.Rendering;
using Moba.Display.Runtime;

namespace Moba.Display.Transport;

/// <summary>
/// Writes the same FRAME_START / lines / FRAME_DONE packets as UDP, sequentially to a COM port (USB‑UART).
/// </summary>
public sealed class SerialLineFrameSender : IDisposable
{
    private static readonly byte[] FrameStart = Encoding.ASCII.GetBytes("FRAME_START");
    private static readonly byte[] FrameDone = Encoding.ASCII.GetBytes("FRAME_DONE");

    private readonly object _gate = new();
    private SerialPort? _heldPort;
    private string? _heldPortIdentity;

    /// <inheritdoc cref="IFrameSender.SendFrameAsync"/>
    public async Task SendFrameAsync(
        ReadOnlyMemory<byte> rgb565Frame,
        FrameLoopOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.Transport != DisplayTransportKind.Serial)
        {
            throw new InvalidOperationException("Serial transport only.");
        }

        if (rgb565Frame.Length != FrameDimensions.FrameByteCount)
        {
            throw new ArgumentException("RGB565 frame size is invalid.", nameof(rgb565Frame));
        }

        var portName = options.SerialPortName?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(portName))
        {
            throw new ArgumentException("Serial port name must be provided.", nameof(options));
        }

        cancellationToken.ThrowIfCancellationRequested();

        await Task.Run(
                () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    lock (_gate)
                    {
                        var baud = Math.Clamp(options.SerialBaudRate, 115_200, 3_684_640);
                        var identity = $"{portName}\0{baud}";
                        bool needReconnect = _heldPort is null
                            || !_heldPort.IsOpen
                            || !string.Equals(_heldPortIdentity, identity, StringComparison.Ordinal);

                        if (needReconnect)
                        {
                            _heldPort?.Dispose();
                            _heldPortIdentity = identity;
                            var sp = new SerialPort(portName)
                            {
                                BaudRate = baud,
                                ReadTimeout = 100,
                                // Full-frame burst (~134 KiB) must not hit the default 5 s write cap when the USB UART TX buffer drains slowly.
                                WriteTimeout = SerialPort.InfiniteTimeout,
                                WriteBufferSize = 262_144,
                                DataBits = 8,
                                StopBits = StopBits.One,
                                Parity = Parity.None,
                                Handshake = Handshake.None,
                                DtrEnable = true,
                                RtsEnable = true,
                            };
                            try
                            {
                                sp.Open();
                            }
                            catch
                            {
                                sp.Dispose();
                                throw;
                            }

                            _heldPort = sp;
                        }

                        var serial = _heldPort!;
                        var stream = serial.BaseStream;

                        stream.Write(FrameStart, 0, FrameStart.Length);

                        var bytesPerLine = FrameDimensions.Width * FrameDimensions.BytesPerPixel;
                        var lineBuf = new byte[bytesPerLine];
                        for (var row = 0; row < FrameDimensions.Height; row++)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            rgb565Frame.Slice(row * bytesPerLine, bytesPerLine).Span.CopyTo(lineBuf);
                            stream.Write(lineBuf, 0, lineBuf.Length);
                            Thread.Sleep(1);
                            cancellationToken.ThrowIfCancellationRequested();
                        }

                        stream.Write(FrameDone, 0, FrameDone.Length);
                        stream.Flush();
                    }
                },
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Releases the COM port so another program (monitor, UDP‑only firmware use) may open it.
    /// </summary>
    public void ReleasePort()
    {
        lock (_gate)
        {
            _heldPortIdentity = null;
            _heldPort?.Dispose();
            _heldPort = null;
        }
    }

    public void Dispose()
    {
        ReleasePort();
        GC.SuppressFinalize(this);
    }
}
