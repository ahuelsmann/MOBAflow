// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using SharedUI.Shell;

using System.Diagnostics;
using System.Reflection;

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
    private readonly NavigationRegistry _navigationRegistry;
    private readonly Stack<string> _navigationHistory = new();
    private Frame? _contentFrame;
    #endregion

    public NavigationService(IServiceProvider serviceProvider, NavigationRegistry navigationRegistry)
    {
        _serviceProvider = serviceProvider;
        _navigationRegistry = navigationRegistry;
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
            if (!_navigationRegistry.TryGetPage(tag, out var registration))
            {
                throw new ArgumentException($"Unknown navigation tag: {tag}", nameof(tag));
            }

            var previousTag = CurrentPageTag;
            if (previousTag is not null)
            {
                _navigationHistory.Push(previousTag);
            }

            // Check if this is a plugin (Source != "Shell") 
            if (registration.Source != "Shell")
            {
                // Plugin: Use ContentProvider pattern
                var contentProvider = _serviceProvider.GetRequiredService(registration.PageType);

                // Call CreateContent() method via reflection
                var createContentMethod = registration.PageType.GetMethod("CreateContent", BindingFlags.Public | BindingFlags.Instance) ?? throw new InvalidOperationException(
                        $"Plugin type {registration.PageType.Name} must have a public CreateContent() method that returns UIElement.");
                var content = createContentMethod.Invoke(contentProvider, null) as UIElement ?? throw new InvalidOperationException(
                        $"Plugin {registration.PageType.Name}.CreateContent() returned null or non-UIElement.");

                // Wrap in PluginHostPage (which is a known type in WinUI project)
                var hostPage = new PluginHostPage();
                hostPage.SetPluginContent(content);
                _contentFrame.Content = hostPage;
            }
            else
            {
                // Core page: Direct instantiation (type is known to XamlTypeInfo)
                var page = _serviceProvider.GetRequiredService(registration.PageType);
                _contentFrame.Content = page;
            }

            CurrentPageTag = tag;
            Navigated?.Invoke(this, new NavigationEventArgs
            {
                PageTag = tag,
                PreviousPageTag = previousTag
            });

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Navigation to '{tag}' failed: {ex.Message}");
            throw;
        }
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
            if (_navigationRegistry.TryGetPage(previousTag, out var registration))
            {
                var page = _serviceProvider.GetRequiredService(registration.PageType);
                _contentFrame!.Content = page;

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