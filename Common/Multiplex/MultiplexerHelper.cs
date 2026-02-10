// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Multiplex;

using Moba.Domain;

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
    /// Calculates the DCC address for a specific signal aspect.
    /// </summary>
    /// <param name="multiplexerArticle">Article number of the multiplexer</param>
    /// <param name="baseAddress">The starting DCC address</param>
    /// <param name="aspect">The desired signal aspect</param>
    /// <returns>The DCC address to send to Z21</returns>
    public static int GetAddressForAspect(
        string multiplexerArticle,
        int baseAddress,
        SignalAspect aspect)
    {
        var definition = GetDefinition(multiplexerArticle);
        return definition.GetAddressForAspect(baseAddress, aspect);
    }

    /// <summary>
    /// Calculates the command value for a specific signal aspect.
    /// </summary>
    public static int GetCommandValue(
        string multiplexerArticle,
        SignalAspect aspect)
    {
        var definition = GetDefinition(multiplexerArticle);
        return definition.GetCommandValue(aspect);
    }

    /// <summary>
    /// Validates that the given multiplexer supports the given signal aspects.
    /// </summary>
    public static bool SupportsAspect(string multiplexerArticle, SignalAspect aspect)
    {
        try
        {
            var definition = GetDefinition(multiplexerArticle);
            return definition.AspectMapping.Values.Contains(aspect);
        }
        catch
        {
            return false;
        }
    }

    // ============================================================================
    // MULTIPLEXER DEFINITIONS - Viessmann 5229 & 52292
    // ============================================================================

    /// <summary>
    /// 5229 - Multiplexer für Lichtsignale mit Multiplex-Technologie
    /// Controls 1 main signal (e.g., 4046) + 1 distant signal (e.g., 4040, synchronized).
    /// 
    /// Address mapping (example: baseAddress=201):
    ///   201: Hp0 (Stop - Red)
    ///   202: Ks1 (Go - Green)
    ///   203: Ks2 (Caution - Yellow)
    ///   204: Ks1Blink (Speed notice - Green blinking)
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
            AspectMapping = new()
            {
                { 0, SignalAspect.Hp0 },      // Address: baseAddress + 0
                { 1, SignalAspect.Ks1 },      // Address: baseAddress + 1
                { 2, SignalAspect.Ks2 },      // Address: baseAddress + 2
                { 3, SignalAspect.Ks1Blink }  // Address: baseAddress + 3
            }
        };
    }

    /// <summary>
    /// 52292 - Doppel-Multiplexer für 2 Lichtsignale mit Multiplex-Technologie
    /// Controls 2 main signals (e.g., 2x 4046) independently.
    /// 
    /// Address mapping (example: signal1 baseAddress=201, signal2 baseAddress=205):
    ///   Signal 1:
    ///     201: Hp0
    ///     202: Ks1
    ///     203: Ks2
    ///     204: Ks1Blink
    ///   
    ///   Signal 2:
    ///     205: Hp0
    ///     206: Ks1
    ///     207: Ks2
    ///     208: Ks1Blink
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
            DistantSignalArticleNumber = null,  // No synchronized distant signals
            AspectMapping = new()
            {
                { 0, SignalAspect.Hp0 },
                { 1, SignalAspect.Ks1 },
                { 2, SignalAspect.Ks2 },
                { 3, SignalAspect.Ks1Blink }
            }
        };
    }
}
