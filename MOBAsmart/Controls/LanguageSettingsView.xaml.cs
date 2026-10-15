// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Controls;

using Moba.SharedUI.ViewModel;

/// <summary>Hosts language preferences without owning feature behavior.</summary>
public partial class LanguageSettingsView : ContentView
{
    /// <summary>Creates the view bound to the shared language settings state.</summary>
    public LanguageSettingsView(LanguageSettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
