// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Multiplex;

using Domain;

/// <summary>
/// Helper class for managing Viessmann multiplex decoder configurations.
/// Contains predefined multiplexer definitions and utility methods for address calculation.
/// </summary>
public static class MultiplexerHelper
{
    /// <summary>
    /// Registry of all supported multiplexer models.
    /// </summary>
    private static readonly Dictionary<string, MultiplexerDefinition> Definitions = new();

    static MultiplexerHelper()
    {
        // Register all known multiplexer configurations
        RegisterDefinition(Create5229Definition());
        RegisterDefinition(Create52292Definition());
    }

    /// <summary>
    /// Registers a multiplexer definition in the registry.
    /// </summary>
    private static void RegisterDefinition(MultiplexerDefinition definition)
    {
        Definitions[definition.ArticleNumber] = definition;
    }

    /// <summary>
    /// Gets a multiplexer definition by article number.
    /// </summary>
    /// <param name="articleNumber">Article number (e.g., "5229")</param>
    /// <returns>The definition</returns>
    /// <exception cref="ArgumentException">If multiplexer is not supported</exception>
    public static MultiplexerDefinition GetDefinition(string articleNumber)
    {
        if (string.IsNullOrWhiteSpace(articleNumber))
            throw new ArgumentException("Article number cannot be empty", nameof(articleNumber));

        if (!Definitions.TryGetValue(articleNumber, out var definition))
        {
            throw new ArgumentException(
                $"Unsupported multiplexer article number: {articleNumber}. " +
                $"Supported: {string.Join(", ", Definitions.Keys)}",
                nameof(articleNumber));
        }

        return definition;
    }

    /// <summary>
    /// Gets all available multiplexer definitions.
    /// </summary>
    public static IEnumerable<MultiplexerDefinition> GetAllDefinitions() => Definitions.Values;

    /// <summary>
    /// Gets all supported article numbers.
    /// </summary>
    public static IEnumerable<string> GetSupportedArticles() => Definitions.Keys;

    /// <summary>
    /// Gets a turnout command mapping for a specific signal aspect.
    /// </summary>
    public static bool TryGetTurnoutCommand(
        string multiplexerArticle,
        string? signalArticleNumber,
        SignalAspect aspect,
        out MultiplexerTurnoutCommand command)
    {
        var definition = GetDefinition(multiplexerArticle);
        return definition.TryGetTurnoutCommand(signalArticleNumber, aspect, out command);
    }

    /// <summary>
    /// Gets all supported aspects for a given multiplexer and signal article.
    /// </summary>
    public static IReadOnlyCollection<SignalAspect> GetSupportedAspects(
        string multiplexerArticle,
        string? signalArticleNumber)
    {
        var definition = GetDefinition(multiplexerArticle);
        return definition.GetSupportedAspects(signalArticleNumber);
    }

    /// <summary>
    /// Validates that the given multiplexer supports the given signal aspect.
    /// </summary>
    public static bool SupportsAspect(string multiplexerArticle, string? signalArticleNumber, SignalAspect aspect)
    {
        try
        {
            var definition = GetDefinition(multiplexerArticle);
            return definition.TryGetTurnoutCommand(signalArticleNumber, aspect, out _);
        }
        catch
        {
            return false;
        }
    }

    // ===========================================================================
    // MULTIPLEXER DEFINITIONS - Viessmann 5229 & 52292
    // ===========================================================================

    /// <summary>
    /// 5229 - Multiplexer für Lichtsignale mit Multiplex-Technologie
    /// Controls 1 main signal (e.g., 4046) + 1 distant signal (e.g., 4040, synchronized).
    /// 
    /// Address mapping per 5229.md (signal addresses section).
    /// </summary>
    private static MultiplexerDefinition Create5229Definition()
    {
        return new MultiplexerDefinition
        {
            ArticleNumber = "5229",
            DisplayName = "5229 - Multiplexer für Lichtsignale",
            MainSignalCount = 1,
            AddressesPerSignal = 4,
            MainSignalArticleNumber = "4046",
            DistantSignalArticleNumber = "4040",
            SignalAspectCommandsBySignalArticle = new Dictionary<string, IReadOnlyDictionary<SignalAspect, MultiplexerTurnoutCommand>>
            {
                ["4040"] = new Dictionary<SignalAspect, MultiplexerTurnoutCommand>
                {
                    { SignalAspect.Hp0, new MultiplexerTurnoutCommand(0, 0, false) },   // Adr 201, Activate=FALSE → ROT
                    { SignalAspect.Ks1, new MultiplexerTurnoutCommand(0, 0, true) }    // Adr 201, Activate=TRUE  → GRÜN
                },
                ["4042"] = new Dictionary<SignalAspect, MultiplexerTurnoutCommand>
                {
                    { SignalAspect.Hp0, new MultiplexerTurnoutCommand(0, 0, false) },
                    { SignalAspect.Ks1, new MultiplexerTurnoutCommand(0, 0, true) }
                },
                ["4043"] = new Dictionary<SignalAspect, MultiplexerTurnoutCommand>
                {
                    { SignalAspect.Hp0, new MultiplexerTurnoutCommand(0, 0, false) },
                    { SignalAspect.Ks1, new MultiplexerTurnoutCommand(0, 0, true) }
                },
                ["4045"] = new Dictionary<SignalAspect, MultiplexerTurnoutCommand>
                {
                    { SignalAspect.Hp0, new MultiplexerTurnoutCommand(0, 0, false) },
                    { SignalAspect.Ks1, new MultiplexerTurnoutCommand(0, 0, true) }
                },
                ["4046"] = new Dictionary<SignalAspect, MultiplexerTurnoutCommand>
                {
                    { SignalAspect.Hp0, new MultiplexerTurnoutCommand(0, 0, false) },   // Adr 201, Activate=FALSE → ROT
                    { SignalAspect.Ks1, new MultiplexerTurnoutCommand(0, 0, true) }    // Adr 201, Activate=TRUE  → GRÜN
                }
            }
        };
    }

    /// <summary>
    /// 52292 - Doppel-Multiplexer für 2 Lichtsignale mit Multiplex-Technologie
    /// Controls 2 main signals (e.g., 2x 4046) independently.
    /// </summary>
    private static MultiplexerDefinition Create52292Definition()
    {
        return new MultiplexerDefinition
        {
            ArticleNumber = "52292",
            DisplayName = "52292 - Doppel-Multiplexer für 2 Lichtsignale",
            MainSignalCount = 2,
            AddressesPerSignal = 4,
            MainSignalArticleNumber = "4046",
            DistantSignalArticleNumber = null,
            SignalAspectCommandsBySignalArticle = Create5229Definition().SignalAspectCommandsBySignalArticle
        };
    }
}
