// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Display.Protocol;

using System.Security.Cryptography;

/// <summary>
/// Generates non-zero protocol identifiers without reusing an active value during normal operation.
/// </summary>
public sealed class DisplayIdentifierSequence
{
    private static int _defaultSeed = RandomNumberGenerator.GetInt32(1, int.MaxValue);
    private readonly object _syncRoot = new();
    private uint _lastValue;

    /// <summary>
    /// Initializes a sequence in a process-unique randomized identifier range.
    /// </summary>
    public DisplayIdentifierSequence()
        : this(unchecked((uint)Interlocked.Increment(ref _defaultSeed)))
    {
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