namespace Moba.WinUI.View;

using Microsoft.UI.Xaml.Controls;

using SharedUI.ViewModel;

public sealed partial class MainWindow
{
    public MainWindowViewModel ViewModel { get; }

    public MainWindow(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        // Registriere Event-Handler für das ViewModel-Event
        ViewModel.ExitApplicationRequested += OnExitApplicationRequested;

        // Behandle das Closed-Event des Fensters
        Closed += MainWindow_Closed;
    }

    private void MainWindow_Closed(object sender, Microsoft.UI.Xaml.WindowEventArgs args)
    {
        // Benachrichtige das ViewModel über das Schließen des Fensters
        ViewModel.OnWindowClosing();
    }

    private static void OnExitApplicationRequested(object? sender, EventArgs e)
    {
        // Beende die Anwendung (wird vom ViewModel ausgelöst)
        Microsoft.UI.Xaml.Application.Current.Exit();
    }

    private void SolutionTreeView_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
    {
        if (args.AddedItems.Count > 0 && args.AddedItems[0] is TreeNodeViewModel node)
        {
            ViewModel.OnNodeSelected(node);
        }
    }
}