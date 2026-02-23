// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.View;

using Controls.Docking;

using Microsoft.UI.Xaml;

using ViewModel;

internal sealed partial class DockingPage
{
    private readonly DockingPageViewModel _viewModel;
    private bool _isInitialized;

    public DockingPage(DockingPageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        Loaded += OnPageLoaded;
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
    }

    private void OnTabDockedToSide(object sender, DocumentTabDockedEventArgs e)
    {
        _viewModel.OpenDocuments.Remove(e.Document);
    }

    private void OnPanelUndocked(object? sender, DockPanelUndockedEventArgs e)
    {
        var tab = new DocumentTab
        {
            Title = e.Panel.PanelTitle,
            IconGlyph = e.Panel.PanelIconGlyph,
            Content = e.Panel.PanelContent,
            Tag = e.Panel.PanelTitle.ToLowerInvariant()
        };

        _viewModel.OpenDocuments.Add(tab);
        _viewModel.ActiveDocument = tab;
    }
}