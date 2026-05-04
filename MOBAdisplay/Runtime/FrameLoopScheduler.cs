using Moba.Display.Rendering;
using Moba.Display.Transport;

namespace Moba.Display.Runtime;

public sealed class FrameLoopScheduler
{
    private readonly IFrameRenderer _frameRenderer;
    private readonly IFrameSender _frameSender;
    private readonly byte[] _frameBuffer = new byte[FrameDimensions.FrameByteCount];
    private CancellationTokenSource? _runCts;
    private Task? _runTask;
    private volatile int _trackNumber;

    public FrameLoopScheduler(IFrameRenderer frameRenderer, IFrameSender frameSender)
    {
        ArgumentNullException.ThrowIfNull(frameRenderer);
        ArgumentNullException.ThrowIfNull(frameSender);
        _frameRenderer = frameRenderer;
        _frameSender = frameSender;
    }

    /// <summary>
    /// Track number rendered in the top-left cell. May be updated from any thread while the loop runs.
    /// </summary>
    public int TrackNumber
    {
        get => _trackNumber;
        set => _trackNumber = value;
    }

    public event EventHandler<FrameReadyEventArgs>? FrameReady;

    /// <summary>
    /// Fired after each <see cref="IFrameSender.SendFrameAsync"/> completes (success) or throws (failure).
    /// </summary>
    public event EventHandler<FrameTransmissionCompletedEventArgs>? FrameTransmissionCompleted;

    public bool IsRunning => _runTask is { IsCompleted: false };

    public Task StartAsync(FrameLoopOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (IsRunning)
        {
            return Task.CompletedTask;
        }

        _runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _runTask = RunLoopAsync(options, _runCts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (!IsRunning || _runCts is null || _runTask is null)
        {
            return;
        }

        _runCts.Cancel();
        try
        {
            await _runTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // expected on shutdown
        }
    }

    private async Task RunLoopAsync(FrameLoopOptions options, CancellationToken cancellationToken)
    {
        var period = TimeSpan.FromMilliseconds(Math.Max(1, 1000 / Math.Max(1, options.RefreshHz)));
        using var timer = new PeriodicTimer(period);

        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            var timestamp = DateTime.Now;
            var context = new FrameContext(timestamp, _trackNumber);
            _frameRenderer.Render(context, _frameBuffer);

            // Notify preview first so the UI keeps updating even when the ESP32 is unreachable.
            var copy = _frameBuffer.ToArray();
            try
            {
                FrameReady?.Invoke(this, new FrameReadyEventArgs(timestamp, copy));
            }
            catch
            {
                // Preview subscribers must never break the loop.
            }

            try
            {
                await _frameSender.SendFrameAsync(_frameBuffer, options, cancellationToken).ConfigureAwait(false);
                FrameTransmissionCompleted?.Invoke(
                    this,
                    new FrameTransmissionCompletedEventArgs(timestamp, true, null));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Keep the loop alive for local preview; surface failure via FrameTransmissionCompleted.
                FrameTransmissionCompleted?.Invoke(
                    this,
                    new FrameTransmissionCompletedEventArgs(timestamp, false, ex.Message));
            }
        }
    }
}