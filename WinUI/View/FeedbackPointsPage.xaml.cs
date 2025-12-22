// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.UI.Xaml.Controls;

using SharedUI.ViewModel;

/// <summary>
/// Code-behind for FeedbackPointsPage.
/// Minimal code-behind following MVVM pattern - all logic in MainWindowViewModel.
/// </summary>
public sealed partial class FeedbackPointsPage : Page
{
    public MainWindowViewModel ViewModel { get; }

    public FeedbackPointsPage(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}