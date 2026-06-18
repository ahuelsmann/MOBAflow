// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml.Media;
using System.Collections.Concurrent;

/// <summary>
/// Reuses <see cref="SolidColorBrush"/> instances for identical binding inputs.
/// Avoids allocating a new brush on every converter or x:Bind evaluation.
/// </summary>
internal static class BrushCache
{
    private static readonly ConcurrentDictionary<string, SolidColorBrush> Brushes = new();

    public static SolidColorBrush GetOrAdd(string key, Func<SolidColorBrush> factory)
        => Brushes.GetOrAdd(key, _ => factory());
}
