// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Localization;

using System.Globalization;

/// <summary>Resolves UI text and formats complete templates without platform dependencies.</summary>
public interface IUiLocalizer
{
    /// <summary>The language/display culture fixed for the current app session.</summary>
    CultureInfo Culture { get; }
    /// <summary>Returns translated text with complete English fallback.</summary>
    string GetText(string key);
    /// <summary>Formats a complete UI template in this session's display culture.</summary>
    string Format(string key, params object[] arguments);
}
