// Copyright (c) 2026 Andreas Huelsmann. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root.

namespace Moba.WinUI.Service;

using Microsoft.Extensions.DependencyInjection;
using SharedUI.Shell;

/// <summary>
/// Factory for creating pages with dependency injection support.
/// </summary>
public sealed class PageFactory : IPageFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, Type> _pageRegistry = new(StringComparer.OrdinalIgnoreCase);

    public PageFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public IEnumerable<string> RegisteredPageTags => _pageRegistry.Keys;

    /// <inheritdoc />
    public void RegisterPage(string pageTag, Type pageType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pageTag);
        ArgumentNullException.ThrowIfNull(pageType);

        _pageRegistry[pageTag] = pageType;
    }

    /// <inheritdoc />
    public object? CreatePage(string pageTag)
    {
        if (!_pageRegistry.TryGetValue(pageTag, out var pageType))
        {
            return null;
        }

        return _serviceProvider.GetRequiredService(pageType);
    }

    /// <inheritdoc />
    public TPage CreatePage<TPage>() where TPage : class
    {
        return _serviceProvider.GetRequiredService<TPage>();
    }
}
