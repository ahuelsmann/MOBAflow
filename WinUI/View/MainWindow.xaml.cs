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
    }

    private void SolutionTreeView_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
    {
        if (args.AddedItems.Count > 0 && args.AddedItems[0] is TreeNodeViewModel node)
        {
            ViewModel.OnNodeSelected(node);
        }
    }
}