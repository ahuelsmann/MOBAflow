// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using System.Collections.Concurrent;

namespace Moba.WinUI.Service;

public sealed class PluginRegistry
{
    private readonly ConcurrentDictionary<string, PluginPageRegistration> _pages = new(StringComparer.OrdinalIgnoreCase);

    public ICollection<PluginPageRegistration> Pages => _pages.Values;

    public bool TryGetPage(string tag, out PluginPageRegistration registration)
    {
        return _pages.TryGetValue(tag, out registration!);
    }

    public void AddOrUpdate(string tag, string title, string? iconGlyph, Type pageType, string source)
    {
        ArgumentNullException.ThrowIfNull(tag);
        ArgumentNullException.ThrowIfNull(pageType);

        _pages[tag] = new PluginPageRegistration(tag, title, iconGlyph, pageType, source);
    }
}

public sealed record PluginPageRegistration(string Tag, string Title, string? IconGlyph, Type PageType, string Source);
