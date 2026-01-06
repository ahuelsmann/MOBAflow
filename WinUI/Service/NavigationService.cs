// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Moba.WinUI.View;

using System.Diagnostics;
using System.Reflection;

/// <summary>
/// Navigation service for MOBAflow WinUI application.
/// Handles page navigation via DI-resolved pages.
/// 
/// For plugins: Uses ContentProvider pattern to avoid XamlTypeInfo resolution issues.
/// Plugins provide a class with CreateContent() method that returns UIElement.
/// </summary>
public class NavigationService
{
    #region Fields
    private readonly IServiceProvider _serviceProvider;
    private readonly PluginRegistry _pluginRegistry;
    private Frame? _contentFrame;
    #endregion

    public NavigationService(IServiceProvider serviceProvider, PluginRegistry pluginRegistry)
    {
        _serviceProvider = serviceProvider;
        _pluginRegistry = pluginRegistry;
    }

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
            if (!_pluginRegistry.TryGetPage(tag, out var registration))
            {
                throw new ArgumentException($"Unknown navigation tag: {tag}", nameof(tag));
            }

            // Check if this is a plugin (Source != "Core") 
            if (registration.Source != "Core")
            {
                // Plugin: Use ContentProvider pattern
                var contentProvider = _serviceProvider.GetRequiredService(registration.PageType);
                
                // Call CreateContent() method via reflection
                var createContentMethod = registration.PageType.GetMethod("CreateContent", BindingFlags.Public | BindingFlags.Instance);
                if (createContentMethod is null)
                {
                    throw new InvalidOperationException(
                        $"Plugin type {registration.PageType.Name} must have a public CreateContent() method that returns UIElement.");
                }

                var content = createContentMethod.Invoke(contentProvider, null) as UIElement;
                if (content is null)
                {
                    throw new InvalidOperationException(
                        $"Plugin {registration.PageType.Name}.CreateContent() returned null or non-UIElement.");
                }

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

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Navigation to '{tag}' failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Navigate to Overview page (default startup page).
    /// </summary>
    public Task NavigateToOverviewAsync()
    {
        return NavigateToPageAsync("overview");
    }
}