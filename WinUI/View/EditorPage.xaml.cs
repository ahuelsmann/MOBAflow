// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.UI.Xaml.Controls;
using Moba.SharedUI.ViewModel;

/// <summary>
/// Editor page with SelectorBar.
/// Works directly with MainWindowViewModel (no Frame navigation yet).
/// </summary>
public sealed partial class EditorPage : Page
{
    public MainWindowViewModel ViewModel { get; }

    public EditorPage(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        
        System.Diagnostics.Debug.WriteLine("EditorPage initialized with MainWindowViewModel");
    }

    /// <summary>
    /// Handles SelectorBar selection changes for future view switching.
    /// </summary>
    private void EditorSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        // TODO: Implement Frame navigation in future iteration
        var selectedItem = EditorSelectorBar.SelectedItem;
        System.Diagnostics.Debug.WriteLine($"SelectorBar changed to: {selectedItem}");
    }
}
