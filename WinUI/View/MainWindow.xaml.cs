namespace Moba.WinUI.View;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Service;

using SharedUI.ViewModel;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        ViewModel = new MainWindowViewModel(new IoService(hwnd));
        Nav.DataContext = ViewModel;
        Nav.SelectionChanged += Nav_SelectionChanged;
    }

    public MainWindowViewModel ViewModel { get; }

    private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer is NavigationViewItem item)
        {
            switch (item.Tag as string)
            {
                case "overview":
                    ContentFrame.Content = new TextBlock { Text = "Übersicht", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                    break;
                case "projects":
                    ContentFrame.Content = new TextBlock { Text = $"Projekte: {ViewModel.Solution?.Projects.Count ?? 0}", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                    break;
            }
        }
    }
}