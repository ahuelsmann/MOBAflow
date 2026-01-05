// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using System.Reflection;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

using Moba.SharedUI.ViewModel;

namespace Moba.Plugin;

/// <summary>
/// Sample Plugin Page demonstrating XAML-based plugin development.
/// Uses embedded XAML resource loaded at runtime via XamlReader.Load().
/// This approach enables true dynamic plugin loading without requiring
/// the plugin to be a project reference of the host application.
/// </summary>
public sealed class SamplePluginPage : Page
{
    public SamplePluginViewModel ViewModel { get; }

    public SamplePluginPage(SamplePluginViewModel viewModel, MainWindowViewModel mainWindowViewModel)
    {
        _ = mainWindowViewModel; // Available if needed for additional functionality
        
        ViewModel = viewModel;
        LoadXamlContent();
        DataContext = ViewModel;
    }

    /// <summary>
    /// Loads XAML content from embedded resource and sets it as page content.
    /// Uses XamlReader.Load() which parses XAML at runtime.
    /// </summary>
    private void LoadXamlContent()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "Moba.Plugin.SamplePluginPage.xaml";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            // Fallback: show error message if XAML resource not found
            Content = new TextBlock
            {
                Text = $"Error: Could not load embedded XAML resource '{resourceName}'",
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red),
                Margin = new Thickness(20)
            };
            return;
        }

        using var reader = new StreamReader(stream);
        var xamlContent = reader.ReadToEnd();

        // Parse XAML at runtime - returns the root element (Page)
        var loadedPage = XamlReader.Load(xamlContent) as Page;
        
        // Transfer the content from the loaded page to this page
        if (loadedPage?.Content is { } content)
        {
            Content = content;
        }
    }
}
