// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Configuration;
using Common.Extension;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Navigation;
using Moba.WinUI.Service;
using Moba.WinUI.ViewModel;
using SharedUI.Interface;

internal sealed partial class HelpPage : INavigationParameterReceiver
{
    private readonly Dictionary<TreeViewNode, string> _sections = [];

    /// <summary>Gets the selection state of the page; the page owns it because it has no dependencies.</summary>
    public HelpPageViewModel ViewModel { get; } = new();
    private readonly AppSettings _settings;
    private readonly ISettingsService? _settingsService;
    private readonly ILogger<HelpPage>? _logger;
    private long _navigationExpandedToken;
    private long _documentationExpandedToken;
    private string? _pendingSection;

    private double _navigationExpandedWidth = 280;
    private double _documentationExpandedStarValue = 1;

    public HelpPage(
        AppSettings settings,
        ISettingsService? settingsService = null,
        ILogger<HelpPage>? logger = null)
    {
        _settings = settings;
        _settingsService = settingsService;
        _logger = logger;
        InitializeComponent();
        Loaded += OnPageLoaded;
        Unloaded += OnPageUnloaded;
        _navigationExpandedToken = NavigationPanel.RegisterPropertyChangedCallback(Controls.CollapsibleColumnBase.IsExpandedProperty, OnPanelIsExpandedChanged);
        _documentationExpandedToken = DocumentationPanel.RegisterPropertyChangedCallback(Controls.CollapsibleColumnBase.IsExpandedProperty, OnPanelIsExpandedChanged);
    }

    public void ReceiveNavigationParameter(object parameter)
    {
        if (parameter is string section)
        {
            _pendingSection = section;
        }
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        RestoreLayout();
        InitializePage();
        if (!string.IsNullOrWhiteSpace(_pendingSection))
        {
            ShowContent(_pendingSection);
            _pendingSection = null;
        }
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        HandlePageUnloadedAsync().Observe(ex => _logger?.LogWarning(ex, "Persist layout on unload failed"));
    }

    private async Task HandlePageUnloadedAsync()
    {
        try
        {
            SaveLayout();
            if (_settingsService != null)
            {
                await _settingsService.SaveSettingsAsync(_settings);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Persist layout on unload failed");
        }

        if (_navigationExpandedToken != 0)
        {
            NavigationPanel.UnregisterPropertyChangedCallback(Controls.CollapsibleColumnBase.IsExpandedProperty, _navigationExpandedToken);
            _navigationExpandedToken = 0;
        }
        if (_documentationExpandedToken != 0)
        {
            DocumentationPanel.UnregisterPropertyChangedCallback(Controls.CollapsibleColumnBase.IsExpandedProperty, _documentationExpandedToken);
            _documentationExpandedToken = 0;
        }
        Loaded -= OnPageLoaded;
        Unloaded -= OnPageUnloaded;
    }

    private void RestoreLayout()
    {
        var layout = _settings.Layout.HelpPage;

        if (layout.NavigationColumnWidth > 0)
        {
            _navigationExpandedWidth = layout.NavigationColumnWidth;
        }
        if (layout.DocumentationColumnStarValue > 0)
        {
            _documentationExpandedStarValue = layout.DocumentationColumnStarValue;
        }

        NavigationPanel.IsExpanded = layout.IsNavigationExpanded;
        DocumentationPanel.IsExpanded = layout.IsDocumentationExpanded;
        ColNavigation.Width = layout.IsNavigationExpanded ? new GridLength(_navigationExpandedWidth) : GridLength.Auto;
        ColDocumentation.Width = layout.IsDocumentationExpanded ? new GridLength(_documentationExpandedStarValue, GridUnitType.Star) : GridLength.Auto;
    }

    private void OnPanelIsExpandedChanged(DependencyObject sender, DependencyProperty dp)
    {
        if (sender == NavigationPanel)
        {
            ApplyNavigationColumnState();
        }
        else if (sender == DocumentationPanel)
        {
            ApplyDocumentationColumnState();
        }
    }

    private void ApplyNavigationColumnState()
    {
        if (!NavigationPanel.IsExpanded)
        {
            if (ColNavigation.Width.IsAbsolute)
            {
                _navigationExpandedWidth = ColNavigation.Width.Value;
            }
            ColNavigation.Width = GridLength.Auto;
        }
        else
        {
            ColNavigation.Width = new GridLength(_navigationExpandedWidth);
        }
    }

    private void ApplyDocumentationColumnState()
    {
        if (!DocumentationPanel.IsExpanded)
        {
            if (ColDocumentation.Width.IsStar)
            {
                _documentationExpandedStarValue = ColDocumentation.Width.Value;
            }
            ColDocumentation.Width = GridLength.Auto;
        }
        else
        {
            ColDocumentation.Width = new GridLength(_documentationExpandedStarValue, GridUnitType.Star);
        }
    }

    private void SaveLayout()
    {
        var layout = _settings.Layout.HelpPage;

        layout.IsNavigationExpanded = NavigationPanel.IsExpanded;
        layout.IsDocumentationExpanded = DocumentationPanel.IsExpanded;

        if (ColNavigation.Width.IsAbsolute)
        {
            _navigationExpandedWidth = ColNavigation.Width.Value;
        }
        if (ColDocumentation.Width.IsStar)
        {
            _documentationExpandedStarValue = ColDocumentation.Width.Value;
        }

        layout.NavigationColumnWidth = _navigationExpandedWidth;
        layout.DocumentationColumnStarValue = _documentationExpandedStarValue;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is string section)
        {
            ShowContent(section);
        }
    }

    private void InitializePage()
    {
        foreach (var category in HelpTopics.Categories)
        {
            var node = new TreeViewNode { Content = category.Title, IsExpanded = category.IsExpandedByDefault };
            foreach (var section in category.Sections)
                AddSection(node, section);
            NavigationTreeView.RootNodes.Add(node);
        }

        ShowContent(HelpTopics.DefaultSection);
    }

    private void AddSection(TreeViewNode parent, string topic)
    {
        var node = new TreeViewNode { Content = topic };
        _sections[node] = topic;
        parent.Children.Add(node);
    }

    private void OnNavigationItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem is TreeViewNode node && _sections.TryGetValue(node, out var section))
        {
            ShowContent(section);
        }
    }

    private void ShowContent(string section)
    {
        ViewModel.ShowSection(section);
        HeaderTextBlock.Text = ViewModel.SelectedSection;

        var para = new Paragraph { Margin = new Thickness(0) };
        foreach (var line in ViewModel.SelectedContent.Split('\n'))
        {
            para.Inlines.Add(new Run { Text = line });
            para.Inlines.Add(new LineBreak());
        }

        ContentRichTextBlock.Blocks.Clear();
        ContentRichTextBlock.Blocks.Add(para);
    }
}
