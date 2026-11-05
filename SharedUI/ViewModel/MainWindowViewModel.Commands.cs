// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.Input;

using System.Windows.Input;

public partial class MainWindowViewModel
{
    /// <summary>
    /// Command exposed for View (Event-to-Command) to handle ListView item clicks on the Solution page.
    /// </summary>
    public ICommand ItemClickedCommand => field ??= new RelayCommand<object?>(item =>
        // Set the selected item to display in the properties panel
        CurrentSelectedObject = item);
}