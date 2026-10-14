// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.TrainControl;

/// <summary>
/// Timing of locomotive drive commands: the debounce of speed-slider changes, the stepwise speed ramp and the
/// grace period in which a locally sent drive command wins over older runtime snapshots. The caller applies
/// each step to its bound state; ramps continue on the caller's context so UI updates stay on the UI thread.
/// </summary>
public sealed class SpeedController : IDisposable
{
    private static readonly TimeSpan DriveDebounce = TimeSpan.FromMilliseconds(75);
    private static readonly TimeSpan LocalDriveGracePeriod = TimeSpan.FromSeconds(3);

    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<int, DateTimeOffset> _lastLocalDriveAt = [];
    private CancellationTokenSource? _driveDebounceCts;
    private CancellationTokenSource? _rampCts;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpeedController"/> class.
    /// </summary>
    /// <param name="timeProvider">Clock for the debounce, ramp steps and grace period.</param>
    public SpeedController(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Runs <paramref name="sendDrive"/> once the speed has been quiet for 75 ms; a newer change replaces a
    /// pending one.
    /// </summary>
    public void QueueDrive(Action sendDrive)
    {
        ArgumentNullException.ThrowIfNull(sendDrive);
        _driveDebounceCts?.Cancel();
        _driveDebounceCts?.Dispose();
        _driveDebounceCts = new CancellationTokenSource();
        _ = SendAfterDebounceAsync(sendDrive, _driveDebounceCts.Token);
    }

    /// <summary>Cancels a running ramp and returns the token for a new one.</summary>
    public CancellationToken BeginRamp()
    {
        CancelRamp();
        _rampCts = new CancellationTokenSource();
        return _rampCts.Token;
    }

    /// <summary>Cancels a running ramp.</summary>
    public void CancelRamp()
    {
        _rampCts?.Cancel();
        _rampCts?.Dispose();
        _rampCts = null;
    }

    /// <summary>
    /// Changes the speed from <paramref name="fromSpeed"/> to <paramref name="toSpeed"/> in steps of
    /// <paramref name="stepSize"/> (1-20), waiting <paramref name="intervalMs"/> (50-500 ms) between steps.
    /// <paramref name="applyStep"/> runs on the caller's context for every step.
    /// </summary>
    public async Task RampAsync(
        int fromSpeed,
        int toSpeed,
        double stepSize,
        double intervalMs,
        Func<int, Task> applyStep,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(applyStep);
        var step = (int)Math.Clamp(stepSize, 1, 20);
        var interval = TimeSpan.FromMilliseconds((int)Math.Clamp(intervalMs, 50, 500));
        var currentSpeed = fromSpeed;
        while (currentSpeed != toSpeed)
        {
            cancellationToken.ThrowIfCancellationRequested();
            currentSpeed = toSpeed > currentSpeed
                ? Math.Min(currentSpeed + step, toSpeed)
                : Math.Max(currentSpeed - step, toSpeed);

            // Steps update bound state, so they continue on the caller's (UI) context.
            await applyStep(currentSpeed).ConfigureAwait(true);
            if (currentSpeed != toSpeed)
            {
                await Task.Delay(interval, _timeProvider, cancellationToken).ConfigureAwait(true);
            }
        }
    }

    /// <summary>Records a drive command sent from this device to <paramref name="address"/>.</summary>
    public void MarkLocalDrive(int address)
    {
        if (address >= 1)
        {
            _lastLocalDriveAt[address] = _timeProvider.GetUtcNow();
        }
    }

    /// <summary>
    /// Whether a drive command sent from this device within the last 3 s should win over a runtime snapshot.
    /// </summary>
    public bool ShouldPreserveLocalDrive(int address)
    {
        if (address < 1 || !_lastLocalDriveAt.TryGetValue(address, out var commandedAt))
        {
            return false;
        }

        if (_timeProvider.GetUtcNow() - commandedAt > LocalDriveGracePeriod)
        {
            _lastLocalDriveAt.Remove(address);
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        CancelRamp();
        _driveDebounceCts?.Cancel();
        _driveDebounceCts?.Dispose();
        _driveDebounceCts = null;
    }

    private async Task SendAfterDebounceAsync(Action sendDrive, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(DriveDebounce, _timeProvider, cancellationToken).ConfigureAwait(false);
            sendDrive();
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer speed change.
        }
    }
}
