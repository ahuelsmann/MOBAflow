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
