// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Localization;

using System.Globalization;
using System.Resources;

/// <summary>Reads the shared UI catalogue with .NET satellite-resource fallback.</summary>
public sealed class UiLocalizer(CultureInfo culture) : IUiLocalizer
{
    private static readonly ResourceManager Resources = new("Moba.SharedUI.Localization.UiStrings", typeof(UiLocalizer).Assembly);

    /// <inheritdoc />
    public CultureInfo Culture { get; } = culture ?? throw new ArgumentNullException(nameof(culture));

    /// <inheritdoc />
    public string GetText(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return Resources.GetString(key, Culture)
            ?? throw new InvalidOperationException($"Missing UI resource: {key}");
    }

    /// <inheritdoc />
    public string Format(string key, params object[] arguments) => string.Format(Culture, GetText(key), arguments);
}
