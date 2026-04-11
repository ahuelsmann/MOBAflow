// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.View;

using Microsoft.UI.Xaml;
using Controls.Docking;
using ViewModel;

public sealed partial class DockingPage
{
    private readonly DockingPageViewModel _viewModel;
    private bool _isInitialized;

    public DockingPage(DockingPageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        DockManager.DocumentTabDockRequested += OnDocumentTabDockRequested;
        DockManager.DockPanelDockRequested += OnDockPanelDockRequested;
        DockManager.DockPanelAutoHideRequested += OnDockPanelAutoHideRequested;
        DockManager.DockPanelStateChanged += OnDockPanelStateChanged;

        Loaded += OnPageLoaded;
        Unloaded += OnPageUnloaded;
    }

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        if (_isInitialized)
        {
            return;
        }

        await _viewModel.InitializeAsync();
        _isInitialized = true;
    }

    private async void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.PersistAsync();
    }

    private void OnDocumentTabDockRequested(object? sender, DocumentTabDockRequestedEventArgs e)
    {
        _viewModel.HandleDocumentDockRequested(e.Document, e.Position);
    }

    private void OnDockPanelDockRequested(object? sender, DockPanelDockRequestedEventArgs e)
    {
        _viewModel.HandlePanelDockRequested(e.PanelId, e.Position);
    }

    private void OnDockPanelAutoHideRequested(object? sender, DockPanelAutoHideRequestedEventArgs e)
    {
        _viewModel.HandlePanelAutoHideChanged(e.PanelId, e.Position, e.IsAutoHidden);
    }

    private void OnDockPanelStateChanged(object? sender, DockPanelStateChangedEventArgs e)
    {
        _viewModel.HandlePanelStateChanged(e.PanelId, e.IsExpanded);
    }
}