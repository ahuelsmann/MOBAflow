// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Multiplex;

using Moba.Domain;

/// <summary>
/// Defines an address offset and turnout activation for a signal aspect.
/// </summary>
public readonly record struct MultiplexerTurnoutCommand(int AddressOffset, bool Activate);

/// <summary>
/// Defines a Viessmann multiplex decoder (e.g., 5229, 52292) with its signal mappings.
/// Each multiplexer has a fixed set of DCC addresses that map to signal aspects.
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
    /// Maps signal article numbers to their supported signal aspect commands.
    /// </summary>
    public Dictionary<string, IReadOnlyDictionary<SignalAspect, MultiplexerTurnoutCommand>>
        SignalAspectCommandsBySignalArticle { get; init; } = [];

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
    /// Gets the turnout command mapping for a signal article and aspect.
    /// </summary>
    /// <param name="signalArticleNumber">Viessmann signal article number (e.g., "4046").</param>
    /// <param name="aspect">Signal aspect to map.</param>
    /// <param name="command">Resolved turnout command mapping.</param>
    /// <returns>True when a mapping exists for the requested signal aspect.</returns>
    public bool TryGetTurnoutCommand(
        string? signalArticleNumber,
        SignalAspect aspect,
        out MultiplexerTurnoutCommand command)
    {
        var resolvedArticle = string.IsNullOrWhiteSpace(signalArticleNumber)
            ? MainSignalArticleNumber
            : signalArticleNumber;

        if (string.IsNullOrWhiteSpace(resolvedArticle) ||
            !SignalAspectCommandsBySignalArticle.TryGetValue(resolvedArticle, out var mapping))
        {
            command = default;
            return false;
        }

        return mapping.TryGetValue(aspect, out command);
    }

    /// <summary>
    /// Gets the supported signal aspects for a given signal article number.
    /// </summary>
    /// <param name="signalArticleNumber">Viessmann signal article number (e.g., "4046").</param>
    /// <returns>Supported signal aspects for the signal article.</returns>
    public IReadOnlyCollection<SignalAspect> GetSupportedAspects(string? signalArticleNumber)
    {
        var resolvedArticle = string.IsNullOrWhiteSpace(signalArticleNumber)
            ? MainSignalArticleNumber
            : signalArticleNumber;

        if (string.IsNullOrWhiteSpace(resolvedArticle) ||
            !SignalAspectCommandsBySignalArticle.TryGetValue(resolvedArticle, out var mapping))
        {
            return Array.Empty<SignalAspect>();
        }

        return mapping.Keys.ToArray();
    }
}
