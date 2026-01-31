namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

public partial class MainWindowViewModel
{
    /// <summary>
    /// Command exposed for View (Event-to-Command) to handle ListView item clicks.
    /// Used for all entity types (Projects, Journeys, Stations, Workflows, Actions).
    /// </summary>
    public ICommand ItemClickedCommand => field ??= new RelayCommand<object?>(item =>
        // Set the selected item to display in the properties panel
        CurrentSelectedObject = item);

    /// <summary>
    /// Command for SolutionPage - sets SolutionPageSelectedObject.
    /// Used for: Projects
    /// </summary>
    public ICommand SolutionPageItemClickedCommand => field ??= new RelayCommand<object?>(item => SolutionPageSelectedObject = item);

    /// <summary>
    /// Command for JourneysPage - sets JourneysPageSelectedObject.
    /// Used for: Journeys, Stations
    /// </summary>
    public ICommand JourneysPageItemClickedCommand => field ??= new RelayCommand<object?>(item => JourneysPageSelectedObject = item);

    /// <summary>
    /// Command for WorkflowsPage - sets WorkflowsPageSelectedObject.
    /// Used for: Workflows, Actions
    /// </summary>
    public ICommand WorkflowsPageItemClickedCommand => field ??= new RelayCommand<object?>(item => WorkflowsPageSelectedObject = item);
}