// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Multiplex;

using Domain;

public interface IMultiplexerProvider
{
    MultiplexerDefinition GetDefinition(string articleNumber);

    IEnumerable<MultiplexerDefinition> GetAllDefinitions();

    IEnumerable<string> GetSupportedArticles();

    bool TryGetTurnoutCommand(
        string multiplexerArticle,
        string? signalArticleNumber,
        SignalAspect aspect,
        out MultiplexerTurnoutCommand command);

    IReadOnlyCollection<SignalAspect> GetSupportedAspects(string multiplexerArticle, string? signalArticleNumber);

    bool SupportsAspect(string multiplexerArticle, string? signalArticleNumber, SignalAspect aspect);

    bool TryGetMaxAddressOffset(string multiplexerArticle, string? signalArticleNumber, out int maxOffset);
}

public sealed class DefaultMultiplexerProvider : IMultiplexerProvider
{
    public MultiplexerDefinition GetDefinition(string articleNumber) => MultiplexerHelper.GetDefinition(articleNumber);

    public IEnumerable<MultiplexerDefinition> GetAllDefinitions() => MultiplexerHelper.GetAllDefinitions();

    public IEnumerable<string> GetSupportedArticles() => MultiplexerHelper.GetSupportedArticles();

    public bool TryGetTurnoutCommand(
        string multiplexerArticle,
        string? signalArticleNumber,
        SignalAspect aspect,
        out MultiplexerTurnoutCommand command) =>
        MultiplexerHelper.TryGetTurnoutCommand(multiplexerArticle, signalArticleNumber, aspect, out command);

    public IReadOnlyCollection<SignalAspect> GetSupportedAspects(string multiplexerArticle, string? signalArticleNumber) =>
        MultiplexerHelper.GetSupportedAspects(multiplexerArticle, signalArticleNumber);

    public bool SupportsAspect(string multiplexerArticle, string? signalArticleNumber, SignalAspect aspect) =>
        MultiplexerHelper.SupportsAspect(multiplexerArticle, signalArticleNumber, aspect);

    public bool TryGetMaxAddressOffset(string multiplexerArticle, string? signalArticleNumber, out int maxOffset) =>
        MultiplexerHelper.TryGetMaxAddressOffset(multiplexerArticle, signalArticleNumber, out maxOffset);
}

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

    /// <summary>
    /// Gets the largest <see cref="MultiplexerTurnoutCommand.AddressOffset"/> among all aspects
    /// for the given multiplexer and signal article (used for DCC range validation).
    /// </summary>
    /// <param name="multiplexerArticle">Multiplexer article number (e.g. 5229).</param>
    /// <param name="signalArticleNumber">Signal article or null to use the definition default main signal.</param>
    /// <param name="maxOffset">Maximum offset when the article is found; otherwise 0.</param>
    /// <returns>True when the signal article has a mapping table in the definition.</returns>
    public static bool TryGetMaxAddressOffset(
        string multiplexerArticle,
        string? signalArticleNumber,
        out int maxOffset)
    {
        maxOffset = 0;
        try
        {
            var definition = GetDefinition(multiplexerArticle);
            var resolvedArticle = string.IsNullOrWhiteSpace(signalArticleNumber)
                ? definition.MainSignalArticleNumber
                : signalArticleNumber;
            if (string.IsNullOrWhiteSpace(resolvedArticle) ||
                !definition.SignalAspectCommandsBySignalArticle.TryGetValue(resolvedArticle, out var mapping))
            {
                return false;
            }

            foreach (var cmd in mapping.Values)
            {
                if (cmd.AddressOffset > maxOffset)
                {
                    maxOffset = cmd.AddressOffset;
                }
            }

            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Article number of the Viessmann distant signal (Ks distant signal); all others count as main signal.
    /// </summary>
    private const string DistantSignalArticle = "4040";

    /// <summary>
    /// Display names for Viessmann multiplex signals (HO scale, see https://viessmann-modell.com/sortiment/spur-h0/signale/).
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> SignalArticleDisplayNames = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["4040"] = "Ks-Vorsignal",
        ["4042"] = "Ks-Einfahrsignal",
        ["4043"] = "Ks-Ausfahrsignal",
        ["4045"] = "Ks-Einfahrsignal (Mehrbereich)",
        ["4046"] = "Ks-Ausfahrsignal (Mehrbereich)",
        ["4721"] = "Licht-Blocksignal (Bauart 1969)",
        ["4722"] = "Licht-Einfahrsignal (Bauart 1969)",
        ["4723"] = "Licht-Ausfahrsignal (Bauart 1969)",
        ["4724"] = "Licht-Blocksignal mit Vorsignal",
        ["4725"] = "Licht-Einfahrsignal mit Vorsignal",
        ["4726"] = "Licht-Ausfahrsignal mit Vorsignal",
        ["4727"] = "Licht-Sperrsignal",
        ["4728"] = "Licht-Sperrsignal",
        ["4751"] = "Ausfahrsignalköpfe mit Vorsignal",
        ["4752"] = "Blocksignalköpfe mit Vorsignal",
        ["4753"] = "Einfahrsignalköpfe mit Vorsignal"
    };

    /// <summary>
    /// Returns all Viessmann multiplex signals selectable as main signal for the given multiplexer.
    /// </summary>
    /// <param name="multiplexerArticleNumber">Multiplexer article number (e.g. "5229", "52292").</param>
    /// <returns>Collection (article number, display name) for the main signal combo box.</returns>
    public static IReadOnlyList<(string ArticleNumber, string DisplayName)> GetMainSignalOptions(string multiplexerArticleNumber)
    {
        var definition = GetDefinition(multiplexerArticleNumber);
        var result = new List<(string, string)>();
        foreach (var article in definition.SignalAspectCommandsBySignalArticle.Keys)
        {
            if (string.Equals(article, DistantSignalArticle, StringComparison.Ordinal))
                continue;
            var displayName = SignalArticleDisplayNames.TryGetValue(article, out var name) ? name : $"Ks-Signal ({article})";
            result.Add((article, $"{article} - {displayName}"));
        }
        // Sort: 4046 first (typical standard main signal), then ascending
        result.Sort((a, b) =>
        {
            if (a.Item1 == "4046") return -1;
            if (b.Item1 == "4046") return 1;
            return string.CompareOrdinal(a.Item1, b.Item1);
        });
        return result;
    }

    /// <summary>
    /// Returns all Viessmann multiplex signals selectable as distant signal for the given multiplexer.
    /// </summary>
    /// <param name="multiplexerArticleNumber">Multiplexer article number (e.g. "5229").</param>
    /// <returns>Collection (article number, display name) for the distant signal combo box.</returns>
    public static IReadOnlyList<(string ArticleNumber, string DisplayName)> GetDistantSignalOptions(string multiplexerArticleNumber)
    {
        var definition = GetDefinition(multiplexerArticleNumber);
        var result = new List<(string, string)>();
        if (definition.DistantSignalArticleNumber == null)
            return result;
        foreach (var article in definition.SignalAspectCommandsBySignalArticle.Keys)
        {
            if (!string.Equals(article, DistantSignalArticle, StringComparison.Ordinal))
                continue;
            var displayName = SignalArticleDisplayNames.TryGetValue(article, out var name) ? name : $"Ks-Vorsignal ({article})";
            result.Add((article, $"{article} - {displayName}"));
        }
        return result;
    }

    // ===========================================================================
    // MULTIPLEXER DEFINITIONS - Viessmann 5229 & 52292
    // ===========================================================================

    /// <summary>
    /// 5229 - Multiplexer for light signals with multiplex technology.
    /// Controls 1 main signal (e.g. 4046) + 1 distant signal (e.g. 4040, synchronized).
    ///
    /// DCC mapping at base address B (four addresses: B..B+3 for offsets 0..3):
    /// - Address [B] (Offset 0): Hp0 = output 0, true; Ks1 = output 1, true
    /// - Address [B+1] (Offset 1): Ra12 = output 0, true; Zs1 = output 1, true
    /// - Address [B+2] (Offset 2): Ks2 = output 0, true; Ks1Blink = output 1, true
    /// - Address [B+3] (Offset 3): Kennlicht = output 0, true; Dunkel = output 1, true
    /// Polarity reversible per address: Settings → interlocking/Viessmann signals or signalBox.invertPolarityOffset0 … Offset3.
    /// </summary>
    private static MultiplexerDefinition Create5229Definition()
    {
        return new MultiplexerDefinition
        {
            ArticleNumber = "5229",
            DisplayName = "5229 - Multiplexer for light signals",
            MainSignalCount = 1,
            AddressesPerSignal = 4,
            MainSignalArticleNumber = "4046",
            DistantSignalArticleNumber = "4040",
            SignalAspectCommandsBySignalArticle = new Dictionary<string, IReadOnlyDictionary<SignalAspect, MultiplexerTurnoutCommand>>
            {
                ["4040"] = new Dictionary<SignalAspect, MultiplexerTurnoutCommand>
                {
                    { SignalAspect.Hp0, new MultiplexerTurnoutCommand(0, 0, false) },
                    { SignalAspect.Ks1, new MultiplexerTurnoutCommand(0, 0, true) },
                    { SignalAspect.Ks1Blink, new MultiplexerTurnoutCommand(1, 0, true) },
                    { SignalAspect.Ks2, new MultiplexerTurnoutCommand(0, 0, false) }
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
                    // Address [B] (Offset 0) -> Hp0 / Ks1
                    // Both use Activate=true (required for Viessmann multiplexer to switch)
                    { SignalAspect.Hp0, new MultiplexerTurnoutCommand(0, 0, true) },
                    { SignalAspect.Ks1, new MultiplexerTurnoutCommand(0, 1, true) },

                    // Address [B+1] (Offset 1) -> Sh1 / weitere Kombinationen
                    { SignalAspect.Ra12, new MultiplexerTurnoutCommand(1, 0, true) },   // Sh1 / Ra12
                    { SignalAspect.Zs1, new MultiplexerTurnoutCommand(1, 1, true) },    // 2. Ausgang auf derselben Adresse

                    // Address [B+2] (Offset 2) -> Ks2 / Ks1 mit Geschwindigkeitsanzeiger
                    { SignalAspect.Ks2, new MultiplexerTurnoutCommand(2, 0, true) },
                    { SignalAspect.Ks1Blink, new MultiplexerTurnoutCommand(2, 1, true) },

                    // Address [B+3] (Offset 3) -> Kennlicht / Dunkel
                    { SignalAspect.Kennlicht, new MultiplexerTurnoutCommand(3, 0, true) },
                    { SignalAspect.Dunkel, new MultiplexerTurnoutCommand(3, 1, true) }
                }
            }
        };
    }

    /// <summary>
    /// 52292 - Double multiplexer for 2 light signals with multiplex technology.
    /// Controls 2 main signals (e.g. 2x 4046) independently.
    /// </summary>
    private static MultiplexerDefinition Create52292Definition()
    {
        return new MultiplexerDefinition
        {
            ArticleNumber = "52292",
            DisplayName = "52292 - Double multiplexer for 2 light signals",
            MainSignalCount = 2,
            AddressesPerSignal = 4,
            MainSignalArticleNumber = "4046",
            DistantSignalArticleNumber = null,
            SignalAspectCommandsBySignalArticle = Create5229Definition().SignalAspectCommandsBySignalArticle
        };
    }
}