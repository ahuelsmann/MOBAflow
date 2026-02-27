// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Backend.Data;
using Common.Multiplex;

/// <summary>
/// Provides the selectable Viessmann Multiplex signals (main and distant signal) from master data (data.json).
/// Filters by multiplexer (5229/52292) and role (main/distant).
/// </summary>
internal sealed class ViessmannSignalService
{
    private readonly DataManager _dataManager;

    public ViessmannSignalService(DataManager dataManager)
    {
        _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
    }

    /// <summary>
    /// Returns all entries selectable as main signal for the specified multiplexer.
    /// Only article numbers that the multiplexer supports and that have role "main" in master data.
    /// </summary>
    public IReadOnlyList<(string ArticleNumber, string DisplayName)> GetMainSignalOptions(string multiplexerArticleNumber)
    {
        var definition = MultiplexerHelper.GetDefinition(multiplexerArticleNumber);
        var supportedArticles = definition.SignalAspectCommandsBySignalArticle.Keys;
        var fromData = _dataManager.ViessmannMultiplexSignals;
        var main = fromData
            .Where(s => string.Equals(s.Role, "main", StringComparison.OrdinalIgnoreCase) && supportedArticles.Contains(s.ArticleNumber))
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
        var fromData = _dataManager.ViessmannMultiplexSignals;
        var distant = fromData
            .Where(s => string.Equals(s.Role, "distant", StringComparison.OrdinalIgnoreCase) && supportedArticles.Contains(s.ArticleNumber))
            .Select(s => (s.ArticleNumber, $"{s.ArticleNumber} - {s.DisplayName}"))
            .ToList();
        if (distant.Count == 0)
            return MultiplexerHelper.GetDistantSignalOptions(multiplexerArticleNumber);
        return distant;
    }
}
