// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.Input;

using System.Windows.Input;

public partial class MainWindowViewModel
{
    /// <summary>
    /// Sets the properties-panel object for a ListView item click. No page binds it at present;
    /// slice 6's shell audit classifies it together with <see cref="CurrentSelectedObject"/>.
    /// </summary>
    public ICommand ItemClickedCommand => field ??= new RelayCommand<object?>(item =>
        // Set the selected item to display in the properties panel
        CurrentSelectedObject = item);
}