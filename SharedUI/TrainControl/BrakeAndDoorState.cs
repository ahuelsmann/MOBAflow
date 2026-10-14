// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.TrainControl;

/// <summary>
/// Signal colours of the train-control buttons and indicators; readable in light and dark themes.
/// </summary>
public static class TrainControlSignalColors
{
    /// <summary>Not relevant in the current state.</summary>
    public const string White = "#8A8A8A";

    /// <summary>Released or allowed.</summary>
    public const string Green = "#06D6A0";

    /// <summary>In transition or blocked.</summary>
    public const string Yellow = "#FFD700";

    /// <summary>Applied or locked.</summary>
    public const string Red = "#E63946";

    /// <summary>Neutral grey for inactive state.</summary>
    public const string Gray = "#888888";
}

/// <summary>
/// The parking brake and door release of the driver's desk: the rules for the buttons and indicators, and the
/// door transition. Releasing the doors blinks for 5 s; closing them blinks for 5 s plus a random blocking time
/// of 0-60 s during which the door-blocked indicator is on. The caller owns the bound state and receives the
/// blink opacity and blocking changes through callbacks on its own context.
/// </summary>
public sealed class BrakeAndDoorState : IDisposable
{
    private static readonly TimeSpan TransitionTime = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan BlinkInterval = TimeSpan.FromMilliseconds(400);
    private const double BlinkLowOpacity = 0.35;
    private const int MaxClosingBlockSeconds = 60;

    private readonly TimeProvider _timeProvider;
    private readonly Func<int, int, int> _nextRandom;
    private CancellationTokenSource? _transitionCts;

    /// <summary>
    /// Initializes a new instance of the <see cref="BrakeAndDoorState"/> class.
    /// </summary>
    /// <param name="timeProvider">Clock for the blink and blocking timers.</param>
    /// <param name="nextRandom">Optional: random number source (min inclusive, max exclusive); defaults to <see cref="Random.Shared"/>.</param>
    public BrakeAndDoorState(TimeProvider timeProvider, Func<int, int, int>? nextRandom = null)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _timeProvider = timeProvider;
        _nextRandom = nextRandom ?? Random.Shared.Next;
    }

    /// <summary>Brake button: red when applied, green when released.</summary>
    public static string GetBrakeButtonColor(bool isBrakeApplied) =>
        isBrakeApplied ? TrainControlSignalColors.Red : TrainControlSignalColors.Green;

    /// <summary>Brake button glyph: Pause when applied, Play when released.</summary>
    public static string GetBrakeButtonGlyph(bool isBrakeApplied) => isBrakeApplied ? "" : "";

    /// <summary>Applying the brake is always possible; releasing it only with closed doors.</summary>
    public static bool IsBrakeButtonEnabled(bool isBrakeApplied, bool isDoorLocked) => !isBrakeApplied || isDoorLocked;

    /// <summary>
    /// Door button: white without the brake, yellow while blinking, green when released at standstill, else red.
    /// </summary>
    public static string GetDoorButtonColor(bool isBrakeApplied, bool isBlinking, bool isDoorLocked, int speedKmh)
    {
        if (!isBrakeApplied)
        {
            return TrainControlSignalColors.White;
        }

        if (isBlinking)
        {
            return TrainControlSignalColors.Yellow;
        }

        return !isDoorLocked && speedKmh == 0 ? TrainControlSignalColors.Green : TrainControlSignalColors.Red;
    }

    /// <summary>The door button works with the brake applied, at standstill and not during a transition.</summary>
    public static bool IsDoorButtonEnabled(bool isBrakeApplied, int speedKmh, bool isBlinking) =>
        isBrakeApplied && speedKmh == 0 && !isBlinking;

    /// <summary>The door icon shows "closed" for the target state while blinking, otherwise for the current one.</summary>
    public static bool IsDoorCloseIconVisible(bool isBlinking, bool isDoorLockedNext, bool isDoorLocked) =>
        isBlinking ? isDoorLockedNext : isDoorLocked;

    /// <summary>The speed may only rise with the brake released and the doors closed.</summary>
    public static bool CanIncreaseSpeed(bool isBrakeApplied, bool isDoorLocked) => !isBrakeApplied && isDoorLocked;

    /// <summary>Door-blocked indicator background: yellow while blocked.</summary>
    public static string GetDoorBlockedColor(bool isDoorBlocked) =>
        isDoorBlocked ? TrainControlSignalColors.Yellow : TrainControlSignalColors.White;

    /// <summary>Door-blocked indicator icon: red while blocked, grey otherwise.</summary>
    public static string GetDoorBlockedIconColor(bool isDoorBlocked) =>
        isDoorBlocked ? TrainControlSignalColors.Red : TrainControlSignalColors.Gray;

    /// <summary>Picks the extra blocking time (0-60 s) for a closing door.</summary>
    public int NextClosingBlockSeconds() => _nextRandom(0, MaxClosingBlockSeconds + 1);

    /// <summary>
    /// Runs one door transition and replaces a running one. Returns true when it completed and false when a
    /// newer transition or <see cref="Dispose"/> cancelled it. Callbacks continue on the caller's context.
    /// </summary>
    public async Task<bool> RunDoorTransitionAsync(
        bool isClosing,
        int blockingSeconds,
        Action<double> setBlinkOpacity,
        Action<bool> setDoorBlocked)
    {
        ArgumentNullException.ThrowIfNull(setBlinkOpacity);
        ArgumentNullException.ThrowIfNull(setDoorBlocked);
        if (_transitionCts is not null)
        {
            await _transitionCts.CancelAsync().ConfigureAwait(true);
        }

        _transitionCts = new CancellationTokenSource();
        var token = _transitionCts.Token;
        var blinkTime = isClosing ? TransitionTime + TimeSpan.FromSeconds(blockingSeconds) : TransitionTime;
        try
        {
            var timer = isClosing
                ? RunBlockingTimerAsync(blockingSeconds, setDoorBlocked, token)
                : Task.Delay(TransitionTime, _timeProvider, token);
            await Task.WhenAll(timer, BlinkAsync(blinkTime, setBlinkOpacity, token)).ConfigureAwait(true);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _transitionCts?.Cancel();
        _transitionCts?.Dispose();
        _transitionCts = null;
    }

    private async Task RunBlockingTimerAsync(int blockingSeconds, Action<bool> setDoorBlocked, CancellationToken token)
    {
        await Task.Delay(TransitionTime, _timeProvider, token).ConfigureAwait(true);
        setDoorBlocked(true);
        await Task.Delay(TimeSpan.FromSeconds(blockingSeconds), _timeProvider, token).ConfigureAwait(true);
    }

    private async Task BlinkAsync(TimeSpan blinkTime, Action<double> setBlinkOpacity, CancellationToken token)
    {
        var high = true;
        for (var elapsed = TimeSpan.Zero; elapsed < blinkTime && !token.IsCancellationRequested; elapsed += BlinkInterval)
        {
            try
            {
                await Task.Delay(BlinkInterval, _timeProvider, token).ConfigureAwait(true);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            high = !high;
            setBlinkOpacity(high ? 1.0 : BlinkLowOpacity);
        }
    }
}
