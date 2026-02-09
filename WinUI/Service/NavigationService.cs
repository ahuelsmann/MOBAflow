// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SharedUI.Shell;
using System.Diagnostics;
using System.Linq;
using View;

/// <summary>
/// Navigation service for MOBAflow WinUI application.
/// Handles page navigation via DI-resolved pages.
/// 
/// For plugins: Uses ContentProvider pattern to avoid XamlTypeInfo resolution issues.
/// Plugins provide a class with CreateContent() method that returns UIElement.
/// </summary>
public class NavigationService : INavigationService
{
    #region Fields
    private readonly IServiceProvider _serviceProvider;
    private readonly List<PageMetadata> _pages;
    private readonly Stack<string> _navigationHistory = new();
    private Frame? _contentFrame;
    #endregion

    public NavigationService(IServiceProvider serviceProvider, List<PageMetadata> pages)
    {
        _serviceProvider = serviceProvider;
        _pages = pages;
    }

    /// <inheritdoc />
    public string? CurrentPageTag { get; private set; }

    /// <inheritdoc />
    public bool CanGoBack => _navigationHistory.Count > 0;

    /// <inheritdoc />
    public event EventHandler<NavigationEventArgs>? Navigated;

    /// <summary>
    /// Initialize the navigation service with the content frame.
    /// Must be called before any navigation operations.
    /// </summary>
    public Task InitializeAsync(Frame contentFrame)
    {
        _contentFrame = contentFrame ?? throw new ArgumentNullException(nameof(contentFrame));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Navigate to a page by tag identifier.
    /// For core pages: Direct Page instantiation
    /// For plugins: Uses ContentProvider pattern (CreateContent returns UIElement)
    /// </summary>
    public async Task NavigateToPageAsync(string tag)
    {
        if (_contentFrame is null)
            throw new InvalidOperationException("NavigationService not initialized. Call InitializeAsync(Frame) first.");

        try
        {
            var page = _pages.FirstOrDefault(p => p.Tag == tag);
            if (page == null)
            {
                throw new ArgumentException($"Unknown navigation tag: {tag}", nameof(tag));
            }

            var previousTag = CurrentPageTag;
            if (previousTag is not null)
            {
                _navigationHistory.Push(previousTag);
            }

            // For now, all pages are core pages (Plugins will use attributes later)
            // Resolve Page from DI and navigate
            var pageInstance = _serviceProvider.GetRequiredService(page.PageType);
            _contentFrame.Content = pageInstance;

            CurrentPageTag = tag;
            Navigated?.Invoke(this, new Moba.SharedUI.Shell.NavigationEventArgs
            {
                PageTag = tag,
                PreviousPageTag = previousTag
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Navigation failed: {ex.Message}");
            throw;
        }

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public bool NavigateTo(string pageTag, object? parameter = null)
    {
        try
        {
            NavigateToPageAsync(pageTag).GetAwaiter().GetResult();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public bool GoBack()
    {
        if (!CanGoBack) return false;

        var previousTag = _navigationHistory.Pop();
        try
        {
            // Navigate without adding to history
            var page = _pages.FirstOrDefault(p => p.Tag == previousTag);
            if (page != null)
            {
                var pageInstance = _serviceProvider.GetRequiredService(page.PageType);
                _contentFrame!.Content = pageInstance;

                var oldTag = CurrentPageTag;
                CurrentPageTag = previousTag;

                Navigated?.Invoke(this, new NavigationEventArgs
                {
                    PageTag = previousTag,
                    PreviousPageTag = oldTag
                });
                return true;
            }
        }
        catch
        {
            // Ignore
        }
        return false;
    }

    /// <summary>
    /// Navigate to Overview page (default startup page).
    /// </summary>
    public Task NavigateToOverviewAsync()
    {
        return NavigateToPageAsync("overview");
    }
}
