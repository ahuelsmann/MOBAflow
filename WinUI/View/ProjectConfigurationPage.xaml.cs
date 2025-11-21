namespace Moba.WinUI.View;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

using Moba.SharedUI.ViewModel;

/// <summary>
/// Page for configuring project entities (Journeys, Stations, Workflows, Trains) using table-like editing.
/// </summary>
public sealed partial class ProjectConfigurationPage : Page
{
    public ProjectConfigurationPage()
    {
        InitializeComponent();
    }

    public ProjectConfigurationPageViewModel? ViewModel { get; private set; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is ProjectConfigurationPageViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;
            
            // Debug output
            System.Diagnostics.Debug.WriteLine($"ProjectConfigurationPage loaded: {ViewModel.Journeys.Count} journeys, {ViewModel.Workflows.Count} workflows, {ViewModel.Trains.Count} trains");
            
            // Notify bindings to update
            Bindings.Update();
        }
    }
}
