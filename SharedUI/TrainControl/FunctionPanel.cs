// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.TrainControl;

using Common.Display;

using Domain;

/// <summary>
/// State behind the F0-F31 function buttons: the last known function bits per locomotive address, the grace
/// period in which a locally switched function wins over runtime snapshots, the all-functions-off operation
/// with its reset version, and edits of a locomotive's function symbols and colours. The caller owns the bound
/// buttons and applies the returned bits.
/// </summary>
public sealed class FunctionPanel : IDisposable
{
    /// <summary>The highest function index (F31).</summary>
    public const int MaxFunctionIndex = 31;

    private const string ClearedAppearance = "none";
    private static readonly TimeSpan LocalFunctionGracePeriod = TimeSpan.FromSeconds(3);

    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<int, uint> _functionBitsByAddress = [];
    private readonly Dictionary<int, Dictionary<int, DateTimeOffset>> _lastLocalFunctionAt = [];
    private CancellationTokenSource? _allFunctionsOffCts;
    private int _resetVersion;

    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionPanel"/> class.
    /// </summary>
    /// <param name="timeProvider">Clock for the local-command grace period.</param>
    public FunctionPanel(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Gets or sets whether snapshot function bits are ignored, for example while all functions are switched off.
    /// </summary>
    public bool SuppressSnapshotBits { get; set; }

    /// <summary>Remembers the function bits of <paramref name="address"/>.</summary>
    public void RememberBits(int address, uint bits) => _functionBitsByAddress[address] = bits;

    /// <summary>Returns the remembered function bits of <paramref name="address"/>.</summary>
    public bool TryGetRememberedBits(int address, out uint bits) => _functionBitsByAddress.TryGetValue(address, out bits);

    /// <summary>Records a function switched from this device.</summary>
    public void MarkLocalFunction(int address, int functionIndex)
    {
        if (address < 1 || functionIndex is < 0 or > MaxFunctionIndex)
        {
            return;
        }

        if (!_lastLocalFunctionAt.TryGetValue(address, out var perFunction))
        {
            perFunction = [];
            _lastLocalFunctionAt[address] = perFunction;
        }

        perFunction[functionIndex] = _timeProvider.GetUtcNow();
    }

    /// <summary>
    /// Whether a function switched from this device within the last 3 s should win over a runtime snapshot.
    /// </summary>
    public bool ShouldPreserveLocalFunction(int address, int functionIndex)
    {
        if (functionIndex is < 0 or > MaxFunctionIndex
            || !_lastLocalFunctionAt.TryGetValue(address, out var perFunction)
            || !perFunction.TryGetValue(functionIndex, out var commandedAt))
        {
            return false;
        }

        if (_timeProvider.GetUtcNow() - commandedAt > LocalFunctionGracePeriod)
        {
            perFunction.Remove(functionIndex);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Combines the shown bits with snapshot bits: functions switched locally within the grace period keep their
    /// shown state, all others take the snapshot's.
    /// </summary>
    public uint MergeSnapshotBits(int address, uint shownBits, uint snapshotBits)
    {
        var merged = 0u;
        for (var functionIndex = 0; functionIndex <= MaxFunctionIndex; functionIndex++)
        {
            var source = ShouldPreserveLocalFunction(address, functionIndex) ? shownBits : snapshotBits;
            merged |= source & (1u << functionIndex);
        }

        return merged;
    }

    /// <summary>
    /// Starts switching all functions off: cancels a previous run, suppresses snapshot bits and returns the reset
    /// version and the token of this run. The caller clears <see cref="SuppressSnapshotBits"/> when done.
    /// </summary>
    public (int ResetVersion, CancellationToken Token) BeginAllFunctionsOff()
    {
        var resetVersion = _resetVersion;
        CancelAllFunctionsOff();
        _allFunctionsOffCts = new CancellationTokenSource();
        SuppressSnapshotBits = true;
        return (resetVersion, _allFunctionsOffCts.Token);
    }

    /// <summary>Whether a reset started with <paramref name="resetVersion"/> may still change the buttons.</summary>
    public bool IsResetCurrent(int resetVersion) => resetVersion == _resetVersion;

    /// <summary>Makes running resets stale and cancels a running all-functions-off, for example after a toggle.</summary>
    public void InvalidatePendingResets()
    {
        _resetVersion++;
        CancelAllFunctionsOff();
    }

    /// <summary>Cancels a running all-functions-off.</summary>
    public void CancelAllFunctionsOff()
    {
        _allFunctionsOffCts?.Cancel();
        _allFunctionsOffCts?.Dispose();
        _allFunctionsOffCts = null;
    }

    /// <inheritdoc />
    public void Dispose() => CancelAllFunctionsOff();

    /// <summary>Sets the PNG symbol of a function; false for an invalid index or an empty symbol.</summary>
    public static bool SetSymbol(Locomotive locomotive, int functionIndex, string glyph)
    {
        ArgumentNullException.ThrowIfNull(locomotive);
        if (functionIndex is < 0 or > MaxFunctionIndex || string.IsNullOrEmpty(glyph))
        {
            return false;
        }

        locomotive.FunctionSymbols = SetAt(locomotive.FunctionSymbols, functionIndex, glyph);
        return true;
    }

    /// <summary>
    /// Sets the symbol (when given) and the colour (when a valid hex colour) of a function; false for an invalid index.
    /// </summary>
    public static bool SetAppearance(Locomotive locomotive, int functionIndex, string? glyph, string? colorHex)
    {
        ArgumentNullException.ThrowIfNull(locomotive);
        if (functionIndex is < 0 or > MaxFunctionIndex)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(glyph))
        {
            locomotive.FunctionSymbols = SetAt(locomotive.FunctionSymbols, functionIndex, glyph);
        }

        if (LocomotiveFunctionAppearanceResolver.IsValidHexColor(colorHex))
        {
            locomotive.FunctionColors = SetAt(locomotive.FunctionColors, functionIndex, colorHex!);
        }

        return true;
    }

    /// <summary>Marks a function as having no symbol and no colour; false for an invalid index.</summary>
    public static bool ClearAppearance(Locomotive locomotive, int functionIndex)
    {
        ArgumentNullException.ThrowIfNull(locomotive);
        if (functionIndex is < 0 or > MaxFunctionIndex)
        {
            return false;
        }

        locomotive.FunctionSymbols = SetAt(locomotive.FunctionSymbols, functionIndex, ClearedAppearance);
        locomotive.FunctionColors = SetAt(locomotive.FunctionColors, functionIndex, ClearedAppearance);
        return true;
    }

    private static List<string> SetAt(List<string>? values, int index, string value)
    {
        values ??= [];
        while (values.Count <= index)
        {
            values.Add(string.Empty);
        }

        values[index] = value;
        return values;
    }
}
