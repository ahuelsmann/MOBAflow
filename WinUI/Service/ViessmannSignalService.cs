// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Backend.Data;
using Common.Multiplex;

/// <summary>
/// Liefert die wählbaren Viessmann Multiplex-Signale (Haupt- und Vorsignal) aus den Stammdaten (data.json).
/// Filtert nach Multiplexer (5229/52292) und Rolle (main/distant).
/// </summary>
internal sealed class ViessmannSignalService
{
    private readonly DataManager _dataManager;

    public ViessmannSignalService(DataManager dataManager)
    {
        _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
    }

    /// <summary>
    /// Liefert alle als Hauptsignal wählbaren Einträge für den angegebenen Multiplexer.
    /// Nur Artikelnummern, die der Multiplexer unterstützt und in den Stammdaten mit Rolle "main" stehen.
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
    /// Liefert alle als Vorsignal wählbaren Einträge für den angegebenen Multiplexer.
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
