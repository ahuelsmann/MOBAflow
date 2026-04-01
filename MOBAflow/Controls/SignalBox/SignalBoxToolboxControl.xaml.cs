namespace Moba.WinUI.Controls.SignalBox;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;

public sealed partial class SignalBoxToolboxControl : UserControl
{
    public SignalBoxToolboxControl()
    {
        InitializeComponent();
    }

    private void OnToolPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = (Brush)Application.Current.Resources["SubtleFillColorTertiaryBrush"];
        }
    }

    private void OnToolPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = (Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"];
        }
    }

    private void OnToolDragStarting(UIElement sender, DragStartingEventArgs e)
    {
        if (sender is Border border && border.Tag is string typeTag && !string.IsNullOrEmpty(typeTag))
        {
            e.Data.SetText($"NEW:{typeTag}");
            e.Data.RequestedOperation = DataPackageOperation.Copy;
        }
        else
        {
            // Cancel drag if no valid tag found - prevents stale drag data issues
            e.Cancel = true;
        }
    }
}
