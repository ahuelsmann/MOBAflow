// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Backend.Data;

using Common.Multiplex;

/// <summary>
/// Provides the selectable Viessmann Multiplex signals (main and distant signal) from master data (data.json).
/// Main-signal articles follow Viessmann manual section 7 (&quot;Verwendbare Signale&quot;), captured in data.json
/// (Ks 4042/4043/4045/4046; Lichtsignale 4721–4728; Signalköpfe 4751–4753; Ks-Vorsignal 4040 as distant).
/// </summary>
internal sealed class ViessmannSignalService
{
    private readonly MasterDataStore _masterDataStore;

    public ViessmannSignalService(MasterDataStore masterDataStore)
    {
        _masterDataStore = masterDataStore ?? throw new ArgumentNullException(nameof(masterDataStore));
    }

    /// <summary>
    /// Returns all entries selectable as main signal for the specified multiplexer.
    /// Uses role "main" from master data (data.json). Articles without a turnout mapping in
    /// <see cref="MultiplexerHelper"/> still appear but aspect commands fail until mappings are added.
    /// </summary>
    public IReadOnlyList<(string ArticleNumber, string DisplayName)> GetMainSignalOptions(string multiplexerArticleNumber)
    {
        _ = MultiplexerHelper.GetDefinition(multiplexerArticleNumber);
        var fromData = _masterDataStore.MultiplexSignals;
        var main = fromData
            .Where(s => string.Equals(s.Role, "main", StringComparison.OrdinalIgnoreCase))
            .Select(s => (s.ArticleNumber, $"{s.ArticleNumber} - {s.DisplayName}"))
            .ToList();
        if (main.Count == 0)
            return MultiplexerHelper.GetMainSignalOptions(multiplexerArticleNumber);
        main.Sort((a, b) =>
        {
            if (a.ArticleNumber == "4046") return -1;
            if (b.ArticleNumber == "4046") return 1;
            return string.CompareOrdinal(a.ArticleNumber, b.ArticleNumber);
        });
        return main;
    }

    /// <summary>
    /// Returns all entries selectable as distant signal for the specified multiplexer.
    /// </summary>
    public IReadOnlyList<(string ArticleNumber, string DisplayName)> GetDistantSignalOptions(string multiplexerArticleNumber)
    {
        var definition = MultiplexerHelper.GetDefinition(multiplexerArticleNumber);
        if (definition.DistantSignalArticleNumber == null)
            return [];
        var supportedArticles = definition.SignalAspectCommandsBySignalArticle.Keys;
        var fromData = _masterDataStore.MultiplexSignals;
        var distant = fromData
            .Where(s => string.Equals(s.Role, "distant", StringComparison.OrdinalIgnoreCase) && supportedArticles.Contains(s.ArticleNumber))
            .Select(s => (s.ArticleNumber, $"{s.ArticleNumber} - {s.DisplayName}"))
            .ToList();
        if (distant.Count == 0)
            return MultiplexerHelper.GetDistantSignalOptions(multiplexerArticleNumber);
        return distant;
    }
}
