// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using SharedUI.ViewModel;

/// <summary>
/// Solution page displaying projects list with properties panel.
/// </summary>
public sealed partial class SolutionPage
{
    public MainWindowViewModel ViewModel { get; }

    public SolutionPage(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}