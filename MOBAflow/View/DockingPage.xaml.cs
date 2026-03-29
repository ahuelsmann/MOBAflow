// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.View;

using Microsoft.UI.Xaml;
using Controls.Docking;
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
        DockManager.TabDockedToSide += OnTabDockedToSide;

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

    private void OnTabDockedToSide(object? sender, DocumentTabDockedEventArgs e)
    {
        _viewModel.HandleDockedDocument(e.Document);
    }

}