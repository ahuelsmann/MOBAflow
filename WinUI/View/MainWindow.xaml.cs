namespace Moba.WinUI.View;

using Microsoft.UI.Xaml.Controls;

using ViewModels;

public sealed partial class MainWindow
{
    public MainWindowViewModel ViewModel { get; }

    public MainWindow(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;

        InitializeComponent();

        ViewModel.ExitApplicationRequested += OnExitApplicationRequested;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        Closed += MainWindow_Closed;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.IsZ21Connected))
        {
            Z21StatusIcon.Glyph = ViewModel.IsZ21Connected ? "\uE8EB" : "\uF384";
        }
    }

    private void MainWindow_Closed(object sender, Microsoft.UI.Xaml.WindowEventArgs args)
    {
        ViewModel.OnWindowClosing();
    }

    private static void OnExitApplicationRequested(object? sender, EventArgs e)
    {
        Microsoft.UI.Xaml.Application.Current.Exit();
    }

    private void SolutionTreeView_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
    {
        if (args.AddedItems.Count > 0 && args.AddedItems[0] is SharedUI.ViewModel.TreeNodeViewModel node)
        {
            ViewModel.OnNodeSelected(node);
        }
    }
}