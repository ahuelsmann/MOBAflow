// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Service;

using Common.Configuration;
using Common.Multiplex;

using Domain;

/// <summary>
/// Resolves Viessmann multiplexer signal aspects to concrete Z21 turnout commands.
/// </summary>
public static class MultiplexerCommandResolver
{
    private const int MinDccAddress = 1;
    private const int MaxDccAddress = 2044;

    /// <summary>
    /// Resolves the DCC address, output and activation bit for a multiplexed signal aspect.
    /// </summary>
    public static ResolvedMultiplexerCommand Resolve(
        int baseAddress,
        string multiplexerArticleNumber,
        string? signalArticleNumber,
        SignalAspect signalAspect,
        SignalBoxSettings? signalBoxSettings = null)
    {
        ValidateBaseAddress(baseAddress);

        if (!MultiplexerHelper.TryGetMaxAddressOffset(
                multiplexerArticleNumber,
                signalArticleNumber,
                out var maxOffset))
        {
            throw new ArgumentException(
                $"No multiplexer mapping found for multiplexer '{multiplexerArticleNumber}' and signal article '{signalArticleNumber ?? "(default)"}'.",
                nameof(multiplexerArticleNumber));
        }

        if (baseAddress + maxOffset > MaxDccAddress)
        {
            throw new ArgumentOutOfRangeException(
                nameof(baseAddress),
                "Base DCC address plus multiplexer offset exceeds 2044.");
        }

        if (!MultiplexerHelper.TryGetTurnoutCommand(
                multiplexerArticleNumber,
                signalArticleNumber,
                signalAspect,
                out var turnoutCommand))
        {
            throw new ArgumentException(
                $"Signal aspect '{signalAspect}' is not supported for multiplexer '{multiplexerArticleNumber}' and signal article '{signalArticleNumber ?? "(default)"}'.",
                nameof(signalAspect));
        }

        var dccAddress = baseAddress + turnoutCommand.AddressOffset;
        if (dccAddress is < MinDccAddress or > MaxDccAddress)
        {
            throw new ArgumentOutOfRangeException(
                nameof(baseAddress),
                $"Calculated DCC address {dccAddress} is outside the valid range ({MinDccAddress}-{MaxDccAddress}).");
        }

        var activate = turnoutCommand.Activate;
        if (signalBoxSettings?.GetInvertPolarityForOffset(turnoutCommand.AddressOffset) == true)
        {
            activate = !activate;
        }

        return new ResolvedMultiplexerCommand(
            dccAddress,
            turnoutCommand.Output,
            activate,
            turnoutCommand.AddressOffset,
            turnoutCommand.Activate);
    }

    private static void ValidateBaseAddress(int baseAddress)
    {
        if (baseAddress is < MinDccAddress or > MaxDccAddress)
        {
            throw new ArgumentOutOfRangeException(
                nameof(baseAddress),
                "Base DCC address must be in the range 1-2044.");
        }

        if (baseAddress % 2 == 0)
        {
            throw new ArgumentException(
                "Base DCC address must be odd for Viessmann multiplexer address pairing.",
                nameof(baseAddress));
        }
    }
}

/// <summary>
/// Concrete turnout command resolved from a multiplexer signal aspect.
/// </summary>
public sealed record ResolvedMultiplexerCommand(
    int DccAddress,
    int Output,
    bool Activate,
    int AddressOffset,
    bool OriginalActivate);