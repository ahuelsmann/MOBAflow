// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Display.Protocol;

using System.Security.Cryptography;

/// <summary>
/// Generates non-zero protocol identifiers without reusing an active value during normal operation.
/// </summary>
public sealed class DisplayIdentifierSequence
{
    private static long _processValue = RandomNumberGenerator.GetInt32(1, int.MaxValue);
    private readonly object _syncRoot = new();
    private readonly bool _usesProcessSequence;
    private uint _lastValue;

    /// <summary>
    /// Initializes a sequence backed by the process-wide randomized identifier stream.
    /// </summary>
    public DisplayIdentifierSequence()
    {
        _usesProcessSequence = true;
    }

    /// <summary>
    /// Initializes a sequence after the supplied last-issued value.
    /// </summary>
    /// <param name="lastValue">Last value already issued, or zero for a new sequence.</param>
    public DisplayIdentifierSequence(uint lastValue)
    {
        _lastValue = lastValue;
    }

    /// <summary>
    /// Returns the next non-zero identifier, skipping zero when the unsigned range wraps.
    /// </summary>
    public uint Next()
    {
        if (_usesProcessSequence)
        {
            uint processValue;
            do
            {
                processValue = unchecked((uint)Interlocked.Increment(ref _processValue));
            }
            while (processValue == 0);

            return processValue;
        }

        lock (_syncRoot)
        {
            _lastValue = unchecked(_lastValue + 1);
            if (_lastValue == 0)
            {
                _lastValue = 1;
            }

            return _lastValue;
        }
    }
}