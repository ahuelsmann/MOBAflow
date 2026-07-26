// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Interface;

/// <summary>
/// Provides selectable signal articles independently of the UI platform.
/// </summary>
public interface ISignalArticleCatalog
{
    IReadOnlyList<SignalArticleOption> GetMainSignalOptions(string multiplexerArticleNumber);

    IReadOnlyList<SignalArticleOption> GetDistantSignalOptions(string multiplexerArticleNumber);
}

/// <summary>
/// Describes a selectable signal article.
/// </summary>
/// <param name="ArticleNumber">Manufacturer article number.</param>
/// <param name="DisplayName">User-facing option label.</param>
public sealed record SignalArticleOption(string ArticleNumber, string DisplayName);
