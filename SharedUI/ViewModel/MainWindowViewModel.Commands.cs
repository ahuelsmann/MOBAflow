using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Moba.SharedUI.ViewModel
{
    public partial class MainWindowViewModel
    {
        private ICommand? _itemClickedCommand;

        /// <summary>
        /// Command exposed for View (Event-to-Command) to handle ListView item clicks.
        /// Used for all entity types (Projects, Journeys, Stations, Workflows, Actions).
        /// </summary>
        public ICommand ItemClickedCommand => _itemClickedCommand ??= new RelayCommand<object?>(item =>
        {            
            // Set the selected item to display in the properties panel
            CurrentSelectedObject = item;
        });
    }
}