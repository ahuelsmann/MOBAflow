// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Multiplex;

using Moba.Domain;

/// <summary>
/// Defines a Viessmann multiplex decoder (e.g., 5229, 52292) with its signal mappings.
/// Each multiplexer has a fixed set of DCC addresses that map to signal aspects.
/// 
/// Example: 5229 Multiplexer für Lichtsignale
/// - BaseAddress 201 → Hp0 (Rot)
/// - BaseAddress+1 202 → Ks1 (Grün)
/// - BaseAddress+2 203 → Ks2 (Gelb)
/// - BaseAddress+3 204 → Ks1Blink
/// </summary>
public class MultiplexerDefinition
{
    /// <summary>
    /// Viessmann article number (e.g., "5229", "52292").
    /// </summary>
    public string ArticleNumber { get; init; } = null!;

    /// <summary>
    /// Human-readable name (e.g., "5229 - Multiplexer für Lichtsignale").
    /// </summary>
    public string DisplayName { get; init; } = null!;

    /// <summary>
    /// Number of main signals this multiplexer controls (1 for 5229, 2 for 52292).
    /// </summary>
    public int MainSignalCount { get; init; }

    /// <summary>
    /// Number of consecutive DCC addresses reserved for this multiplexer.
    /// Usually 4 per signal (for Hp0, Ks1, Ks2, Ks1Blink aspects).
    /// </summary>
    public int AddressesPerSignal { get; init; }

    /// <summary>
    /// Maps address offset (0, 1, 2, 3...) to signal aspect.
    /// Offset 0 = BaseAddress (Hp0)
    /// Offset 1 = BaseAddress+1 (Ks1)
    /// etc.
    /// </summary>
    public Dictionary<int, SignalAspect> AspectMapping { get; init; } = [];

    /// <summary>
    /// Associated main signal article number (e.g., "4046").
    /// </summary>
    public string MainSignalArticleNumber { get; init; } = null!;

    /// <summary>
    /// Associated distant signal article number (e.g., "4040").
    /// Optional for multiplexers with only main signals.
    /// </summary>
    public string? DistantSignalArticleNumber { get; init; }

    /// <summary>
    /// Gets the DCC address for a specific signal aspect.
    /// </summary>
    /// <param name="baseAddress">The starting DCC address (e.g., 201)</param>
    /// <param name="aspect">The signal aspect (e.g., Ks1)</param>
    /// <returns>The DCC address to send to Z21</returns>
    /// <exception cref="ArgumentException">If aspect is not mapped for this multiplexer</exception>
    public int GetAddressForAspect(int baseAddress, SignalAspect aspect)
    {
        var offset = AspectMapping.FirstOrDefault(
            x => x.Value == aspect).Key;

        if (AspectMapping.Values.All(a => a != aspect))
        {
            throw new ArgumentException(
                $"Signal aspect '{aspect}' not supported by multiplexer {ArticleNumber}",
                nameof(aspect));
        }

        return baseAddress + offset;
    }

    /// <summary>
    /// Gets the command value (0-255) to send to Z21 for a specific aspect.
    /// For standard mappings, this is simply the offset in the AspectMapping.
    /// </summary>
    public int GetCommandValue(SignalAspect aspect)
    {
        var offset = AspectMapping.FirstOrDefault(
            x => x.Value == aspect).Key;

        if (AspectMapping.Values.All(a => a != aspect))
        {
            throw new ArgumentException(
                $"Signal aspect '{aspect}' not supported by multiplexer {ArticleNumber}",
                nameof(aspect));
        }

        return offset;
    }
}
