// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MOBAflow.Service;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;

using SharedUI.Configuration;
using SharedUI.Desktop.Shell;

/// <summary>
/// Navigation service for MOBAflow WinUI application.
/// Handles page navigation via DI-resolved pages.
/// 
/// For plugins: Uses ContentProvider pattern to avoid XamlTypeInfo resolution issues.
/// Plugins provide a class with CreateContent() method that returns UIElement.
/// </summary>
internal sealed class NavigationService : INavigationService
{
    #region Fields
    private readonly IServiceProvider _serviceProvider;
    private readonly List<PageMetadata> _pages;
    private readonly UiSettings _settings;
    private readonly ILogger<NavigationService> _logger;
    private readonly Stack<string> _navigationHistory = new();
    private Frame? _contentFrame;
    #endregion

    public NavigationService(
        IServiceProvider serviceProvider,
        List<PageMetadata> pages,
        UiSettings settings,
        ILogger<NavigationService> logger)
    {
        _serviceProvider = serviceProvider;
        _pages = pages;
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger;
    }

    /// <inheritdoc />
    public string? CurrentPageTag { get; private set; }

    /// <inheritdoc />
    public bool CanGoBack => HasAvailableHistoryEntry(_navigationHistory, _pages, _settings.FeatureToggles);

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
    public async Task NavigateToPageAsync(string tag, object? parameter = null)
    {
        if (_contentFrame is null)
            throw new InvalidOperationException("NavigationService not initialized. Call InitializeAsync(Frame) first.");

        try
        {
            var page = ResolveAvailablePage(_pages, _settings.FeatureToggles, tag);

            var previousTag = CurrentPageTag;
            if (previousTag is not null
                && !string.Equals(previousTag, tag, StringComparison.Ordinal)
                && _pages.FirstOrDefault(p => p.Tag == previousTag) is { } previousPage
                && MainWindowNavigationBootstrapper.IsPageVisible(previousPage, _settings.FeatureToggles))
            {
                _navigationHistory.Push(previousTag);
            }

            // For now, all pages are core pages (Plugins will use attributes later)
            // Resolve Page from DI and navigate
            var pageInstance = _serviceProvider.GetRequiredService(page.PageType);
            if (parameter != null && pageInstance is INavigationParameterReceiver parameterReceiver)
            {
                parameterReceiver.ReceiveNavigationParameter(parameter);
            }
            _contentFrame.Content = pageInstance;

            CurrentPageTag = tag;
            Navigated?.Invoke(this, new NavigationEventArgs
            {
                PageTag = tag,
                Parameter = parameter,
                PreviousPageTag = previousTag
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Navigation to page tag '{Tag}' failed", tag);
            throw;
        }

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<bool> NavigateToAsync(string pageTag, object? parameter = null)
    {
        try
        {
            await NavigateToPageAsync(pageTag, parameter);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Navigation to '{PageTag}' failed", pageTag);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> GoBackAsync()
    {
        var page = PopNextAvailableHistoryPage(_navigationHistory, _pages, _settings.FeatureToggles);
        if (page is null)
        {
            return false;
        }

        try
        {
            var pageInstance = _serviceProvider.GetRequiredService(page.PageType);
            _contentFrame!.Content = pageInstance;

            var oldTag = CurrentPageTag;
            CurrentPageTag = page.Tag;

            // Raise navigation event
            Navigated?.Invoke(this, new NavigationEventArgs
            {
                PageTag = page.Tag,
                PreviousPageTag = oldTag
            });

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GoBack navigation to '{PreviousTag}' failed", page.Tag);
            _navigationHistory.Push(page.Tag);
        }

        return false;
    }

    internal static PageMetadata ResolveAvailablePage(
        IReadOnlyList<PageMetadata> pages,
        FeatureToggleSettings toggles,
        string tag)
    {
        ArgumentNullException.ThrowIfNull(pages);
        ArgumentNullException.ThrowIfNull(toggles);

        var page = pages.FirstOrDefault(p => p.Tag == tag);
        if (page is null)
        {
            throw new ArgumentException($"Unknown navigation tag: {tag}", nameof(tag));
        }

        if (!MainWindowNavigationBootstrapper.IsPageVisible(page, toggles))
        {
            throw new InvalidOperationException($"Navigation page '{tag}' is disabled by feature toggles.");
        }

        return page;
    }

    internal static PageMetadata? PopNextAvailableHistoryPage(
        Stack<string> history,
        IReadOnlyList<PageMetadata> pages,
        FeatureToggleSettings toggles)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(pages);
        ArgumentNullException.ThrowIfNull(toggles);

        while (history.Count > 0)
        {
            var previousTag = history.Pop();
            var page = pages.FirstOrDefault(p => p.Tag == previousTag);
            if (page is not null && MainWindowNavigationBootstrapper.IsPageVisible(page, toggles))
            {
                return page;
            }
        }

        return null;
    }

    internal static bool HasAvailableHistoryEntry(
        IEnumerable<string> history,
        IReadOnlyList<PageMetadata> pages,
        FeatureToggleSettings toggles)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(pages);
        ArgumentNullException.ThrowIfNull(toggles);

        return history.Any(tag =>
            pages.FirstOrDefault(p => p.Tag == tag) is { } page
            && MainWindowNavigationBootstrapper.IsPageVisible(page, toggles));
    }

    /// <summary>
    /// Navigate to Overview page (default startup page).
    /// </summary>
    public Task NavigateToOverviewAsync()
    {
        return NavigateToPageAsync("overview");
    }
}

internal interface INavigationParameterReceiver
{
    void ReceiveNavigationParameter(object parameter);
}
