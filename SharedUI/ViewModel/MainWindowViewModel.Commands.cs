namespace Moba.SharedUI.ViewModel
{
    using CommunityToolkit.Mvvm.Input;

    using System.Windows.Input;

    public partial class MainWindowViewModel
    {
        private ICommand? _itemClickedCommand;
        private ICommand? _solutionPageItemClickedCommand;
        private ICommand? _journeysPageItemClickedCommand;
        private ICommand? _workflowsPageItemClickedCommand;

        /// <summary>
        /// Command exposed for View (Event-to-Command) to handle ListView item clicks.
        /// Used for all entity types (Projects, Journeys, Stations, Workflows, Actions).
        /// </summary>
        public ICommand ItemClickedCommand => _itemClickedCommand ??= new RelayCommand<object?>(item =>
        {            
            // Set the selected item to display in the properties panel
            CurrentSelectedObject = item;
        });

        /// <summary>
        /// Command for SolutionPage - sets SolutionPageSelectedObject.
        /// Used for: Projects
        /// </summary>
        public ICommand SolutionPageItemClickedCommand => _solutionPageItemClickedCommand ??= new RelayCommand<object?>(item =>
        {
            SolutionPageSelectedObject = item;
        });

        /// <summary>
        /// Command for JourneysPage - sets JourneysPageSelectedObject.
        /// Used for: Journeys, Stations
        /// </summary>
        public ICommand JourneysPageItemClickedCommand => _journeysPageItemClickedCommand ??= new RelayCommand<object?>(item =>
        {
            JourneysPageSelectedObject = item;
        });

        /// <summary>
        /// Command for WorkflowsPage - sets WorkflowsPageSelectedObject.
        /// Used for: Workflows, Actions
        /// </summary>
        public ICommand WorkflowsPageItemClickedCommand => _workflowsPageItemClickedCommand ??= new RelayCommand<object?>(item =>
        {
            WorkflowsPageSelectedObject = item;
        });
    }
}