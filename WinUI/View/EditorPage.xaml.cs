// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.UI.Xaml.Controls;
using Moba.SharedUI.ViewModel;

/// <summary>
/// Editor page with tabs for Journeys, Workflows, Trains, Locomotives, Wagons, and Settings.
/// </summary>
public sealed partial class EditorPage : Page
{
    public EditorPageViewModel ViewModel { get; }

    public EditorPage(EditorPageViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
