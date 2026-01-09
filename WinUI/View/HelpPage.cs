// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.View;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;

using System;
using System.IO;
using System.Threading.Tasks;

/// <summary>
/// Help page with integrated Wiki viewer using WebView2.
/// Displays documentation with a navigation tree on the left.
/// Built programmatically in C# without XAML for reliability.
/// </summary>
public sealed class HelpPage : Page
{
    private readonly string _wikiBasePath;
    private bool _webView2Initialized;
    private readonly WebView2 _contentWebView;
    private readonly TreeView _wikiTreeView;
    private readonly TextBlock _fallbackTitle;
    private readonly TextBlock _fallbackContent;
    private readonly ScrollViewer _fallbackScrollViewer;

    public HelpPage()
    {
        _wikiBasePath = Path.Combine(AppContext.BaseDirectory, "Resources", "Help");

        _contentWebView = new WebView2 { DefaultBackgroundColor = Colors.Transparent };
        _wikiTreeView = new TreeView { SelectionMode = TreeViewSelectionMode.Single };
        _wikiTreeView.ItemInvoked += WikiTreeView_ItemInvoked;

        _fallbackTitle = new TextBlock { FontSize = 24, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
        _fallbackContent = new TextBlock { TextWrapping = TextWrapping.Wrap };
        _fallbackScrollViewer = new ScrollViewer { Visibility = Visibility.Collapsed, Padding = new Thickness(24) };

        Content = BuildLayout();
        InitializeTreeView();
        Loaded += HelpPage_Loaded;
    }

    private async void HelpPage_Loaded(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        await InitializeWebView2Async();
        await LoadHomePageAsync();
    }

    private Grid BuildLayout()
    {
        var rootGrid = new Grid();
        rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) });
        rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Left Panel: Wiki Navigation
        var leftPanel = BuildLeftPanel();
        Grid.SetColumn(leftPanel, 0);
        rootGrid.Children.Add(leftPanel);

        // Divider
        var divider = new Border
        {
            Width = 1,
            Background = new SolidColorBrush(Colors.Gray)
        };
        Grid.SetColumn(divider, 1);
        rootGrid.Children.Add(divider);

        // Right Panel: WebView Content
        var rightPanel = BuildRightPanel();
        Grid.SetColumn(rightPanel, 2);
        rootGrid.Children.Add(rightPanel);

        return rootGrid;
    }

    private Grid BuildLeftPanel()
    {
        var grid = new Grid { Padding = new Thickness(16) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new TextBlock
        {
            Text = "Wiki Navigation",
            FontSize = 18,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 16)
        };
        Grid.SetRow(header, 0);
        grid.Children.Add(header);

        var searchBox = new AutoSuggestBox
        {
            PlaceholderText = "Search wiki...",
            Margin = new Thickness(0, 0, 0, 16)
        };
        Grid.SetRow(searchBox, 1);
        grid.Children.Add(searchBox);

        Grid.SetRow(_wikiTreeView, 2);
        grid.Children.Add(_wikiTreeView);

        return grid;
    }

    private Grid BuildRightPanel()
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        // Toolbar
        var commandBar = new CommandBar { DefaultLabelPosition = CommandBarDefaultLabelPosition.Right };
        commandBar.PrimaryCommands.Add(new AppBarButton { Icon = new SymbolIcon(Symbol.Back), Label = "Back" });
        commandBar.PrimaryCommands.Add(new AppBarButton { Icon = new SymbolIcon(Symbol.Forward), Label = "Forward" });
        commandBar.PrimaryCommands.Add(new AppBarButton { Icon = new SymbolIcon(Symbol.Refresh), Label = "Refresh" });
        commandBar.PrimaryCommands.Add(new AppBarSeparator());

        var homeButton = new AppBarButton { Icon = new SymbolIcon(Symbol.Home), Label = "Home" };
        homeButton.Click += async (s, e) => await LoadHomePageAsync();
        commandBar.PrimaryCommands.Add(homeButton);

        Grid.SetRow(commandBar, 0);
        grid.Children.Add(commandBar);

        // WebView
        Grid.SetRow(_contentWebView, 1);
        grid.Children.Add(_contentWebView);

        // Fallback
        var fallbackPanel = new StackPanel { Spacing = 16 };
        fallbackPanel.Children.Add(_fallbackTitle);
        fallbackPanel.Children.Add(_fallbackContent);
        _fallbackScrollViewer.Content = fallbackPanel;
        Grid.SetRow(_fallbackScrollViewer, 1);
        grid.Children.Add(_fallbackScrollViewer);

        return grid;
    }

    private void InitializeTreeView()
    {
        var rootNode = new TreeViewNode { Content = "MOBAflow Platform Wiki", IsExpanded = true };

        var quickStartNode = new TreeViewNode { Content = "Quick Start" };
        quickStartNode.Children.Add(new TreeViewNode { Content = "Track Statistics" });
        rootNode.Children.Add(quickStartNode);

        var desktopNode = new TreeViewNode { Content = "MOBAflow (Desktop)", IsExpanded = true };
        desktopNode.Children.Add(new TreeViewNode { Content = "User Guide" });
        desktopNode.Children.Add(new TreeViewNode { Content = "Azure Speech Setup" });
        rootNode.Children.Add(desktopNode);

        var mobileNode = new TreeViewNode { Content = "MOBAsmart (Mobile)" };
        mobileNode.Children.Add(new TreeViewNode { Content = "User Guide" });
        mobileNode.Children.Add(new TreeViewNode { Content = "Wiki" });
        rootNode.Children.Add(mobileNode);

        var webNode = new TreeViewNode { Content = "MOBAdash (Web)" };
        webNode.Children.Add(new TreeViewNode { Content = "User Guide" });
        rootNode.Children.Add(webNode);

        _wikiTreeView.RootNodes.Add(rootNode);
    }

    private async Task InitializeWebView2Async()
    {
        try
        {
            await _contentWebView.EnsureCoreWebView2Async();
            _webView2Initialized = true;

            _contentWebView.CoreWebView2.Settings.IsScriptEnabled = false;
            _contentWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            _contentWebView.CoreWebView2.Settings.IsZoomControlEnabled = true;
        }
        catch (Exception)
        {
            _webView2Initialized = false;
            _contentWebView.Visibility = Visibility.Collapsed;
            _fallbackScrollViewer.Visibility = Visibility.Visible;
        }
    }

    private async Task LoadHomePageAsync()
    {
        var indexPath = Path.Combine(_wikiBasePath, "INDEX.html");
        if (File.Exists(indexPath))
        {
            await LoadHtmlFileAsync(indexPath);
        }
        else
        {
            await LoadWelcomeContentAsync();
        }
    }

    private async Task LoadHtmlFileAsync(string filePath)
    {
        if (!_webView2Initialized)
        {
            await LoadFallbackContentAsync(filePath);
            return;
        }

        if (File.Exists(filePath))
        {
            _contentWebView.Source = new Uri(filePath);
        }
    }

    private Task LoadWelcomeContentAsync()
    {
        var welcomeHtml = GenerateWelcomeHtml();

        if (_webView2Initialized)
        {
            _contentWebView.NavigateToString(welcomeHtml);
        }
        else
        {
            _fallbackTitle.Text = "MOBAflow Wiki";
            _fallbackContent.Text = "Welcome to the MOBAflow Platform Wiki!\n\n" +
                "Select a topic from the navigation tree on the left to get started.\n\n" +
                "Available sections:\n" +
                "- Quick Start Guide\n" +
                "- MOBAflow (Desktop) User Guide\n" +
                "- MOBAsmart (Mobile) User Guide\n" +
                "- MOBAdash (Web) User Guide";
        }

        return Task.CompletedTask;
    }

    private static string GenerateWelcomeHtml()
    {
        return """
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="UTF-8">
                <style>
                    body {
                        font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                        padding: 24px;
                        background-color: transparent;
                        color: #1a1a1a;
                    }
                    @media (prefers-color-scheme: dark) {
                        body { color: #f0f0f0; }
                    }
                    h1 { font-size: 28px; font-weight: 600; margin-bottom: 16px; }
                    h2 { font-size: 20px; font-weight: 600; margin-top: 24px; margin-bottom: 12px; }
                    p { line-height: 1.6; margin-bottom: 12px; }
                    .card {
                        background: rgba(128, 128, 128, 0.1);
                        border-radius: 8px;
                        padding: 16px;
                        margin: 16px 0;
                    }
                </style>
            </head>
            <body>
                <h1>MOBAflow Platform Wiki</h1>
                <p>Welcome to the MOBAflow Platform Wiki! Select a topic from the navigation tree to get started.</p>
                
                <div class="card">
                    <h2>MOBAflow (Desktop)</h2>
                    <p>Full-featured Windows application for journey management, workflow automation, and track plan editing.</p>
                </div>
                
                <div class="card">
                    <h2>MOBAsmart (Mobile)</h2>
                    <p>Android app for mobile lap counting, Z21 monitoring, and feedback statistics.</p>
                </div>
                
                <div class="card">
                    <h2>MOBAdash (Web)</h2>
                    <p>Browser-based dashboard for real-time monitoring and statistics from any device.</p>
                </div>
            </body>
            </html>
            """;
    }

    private Task LoadFallbackContentAsync(string filePath)
    {
        if (File.Exists(filePath))
        {
            var content = File.ReadAllText(filePath);
            _fallbackTitle.Text = Path.GetFileNameWithoutExtension(filePath);
            _fallbackContent.Text = StripHtmlTags(content);
        }

        return Task.CompletedTask;
    }

    private static string StripHtmlTags(string html)
    {
        return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", string.Empty);
    }

    private void WikiTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        _ = sender;

        if (args.InvokedItem is TreeViewNode node)
        {
            var content = node.Content?.ToString();
            if (string.IsNullOrEmpty(content))
                return;

            var fileName = MapContentToFileName(content);
            if (!string.IsNullOrEmpty(fileName))
            {
                var filePath = Path.Combine(_wikiBasePath, fileName);
                _ = LoadHtmlFileAsync(filePath);
            }
        }
    }

    private static string? MapContentToFileName(string content)
    {
        return content switch
        {
            "MOBAflow Platform Wiki" => "INDEX.html",
            "Track Statistics" => "QUICK-START-TRACK-STATISTICS.html",
            "User Guide" => "MOBAFLOW-USER-GUIDE.html",
            "Azure Speech Setup" => "AZURE-SPEECH-SETUP.html",
            "Wiki" => "MOBASMART-WIKI.html",
            _ => null
        };
    }
}
