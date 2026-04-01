namespace Moba.WinUI.Controls.SignalBox;

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Data;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Moba.SharedUI.ViewModel;
using Domain;

public class GridCoordinateConverter : IValueConverter
{
    private const int GridCellSize = 60;

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int intValue)
        {
            return (double)(intValue * GridCellSize);
        }
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public sealed partial class SignalBoxCanvasControl
{
    private const int GridCellSize = 60;
    private const int GridColumns = 32;
    private const int GridRows = 18;

    private bool _isPanning;
    private Point _panStartPoint;
    private double _panStartHorizontalOffset;
    private double _panStartVerticalOffset;

    public static readonly DependencyProperty PlanViewModelProperty = DependencyProperty.Register(
        nameof(PlanViewModel),
        typeof(SignalBoxPlanViewModel),
        typeof(SignalBoxCanvasControl),
        new PropertyMetadata(null, OnPlanViewModelChanged));

    public SignalBoxPlanViewModel? PlanViewModel
    {
        get => (SignalBoxPlanViewModel?)GetValue(PlanViewModelProperty);
        set => SetValue(PlanViewModelProperty, value);
    }

    public SignalBoxCanvasControl()
    {
        InitializeComponent();
    }

    private static void OnPlanViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SignalBoxCanvasControl control)
        {
            control.Bindings.Update();
        }
    }

    private void OnCanvasDragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy | DataPackageOperation.Move;
    }

    private async void OnCanvasDrop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.Text) || PlanViewModel == null)
            return;

        if (sender is not UIElement canvas)
            return;

        var text = await e.DataView.GetTextAsync();
        if (string.IsNullOrEmpty(text))
            return;

        var pos = e.GetPosition(canvas);
        int gridX = Math.Clamp((int)(pos.X / GridCellSize), 0, GridColumns - 1);
        int gridY = Math.Clamp((int)(pos.Y / GridCellSize), 0, GridRows - 1);

        var existingElement = PlanViewModel.HitTest(gridX, gridY);

        if (text.StartsWith("NEW:"))
        {
            var typeTag = text[4..];

            if (existingElement != null)
            {
                PlanViewModel.RemoveElement(existingElement);
            }

            CreateElementByType(typeTag, gridX, gridY);
        }
        else if (text.StartsWith("MOVE:") && Guid.TryParse(text[5..], out var elementId))
        {
            var element = PlanViewModel.FindById(elementId);
            if (element != null && (existingElement == null || existingElement.Id == elementId))
            {
                element.X = gridX;
                element.Y = gridY;
                
                // Force UI update
                var index = PlanViewModel.Elements.IndexOf(element);
                if (index != -1)
                {
                    PlanViewModel.Elements.RemoveAt(index);
                    PlanViewModel.Elements.Insert(index, element);
                }
                
                PlanViewModel.SelectedElement = element;
            }
        }
    }

    private void CreateElementByType(string typeTag, int gridX, int gridY)
    {
        if (PlanViewModel == null) return;

        SbElement? element = typeTag switch
        {
            "TrackStraight" => PlanViewModel.AddTrackStraight(gridX, gridY),
            "TrackCurve" => PlanViewModel.AddTrackCurve(gridX, gridY),
            "Switch" => PlanViewModel.AddSwitch(gridX, gridY),
            "Signal-4043" => CreateMultiplexSignal(PlanViewModel, gridX, gridY, "4043"),
            "Signal-4042" => CreateMultiplexSignal(PlanViewModel, gridX, gridY, "4042"),
            "Signal-4046" => CreateMultiplexSignal(PlanViewModel, gridX, gridY, "4046"),
            "Signal-4045" => CreateMultiplexSignal(PlanViewModel, gridX, gridY, "4045"),
            "Signal-4040" => CreateMultiplexSignal(PlanViewModel, gridX, gridY, "4040"),
            "Detector" => PlanViewModel.AddDetector(gridX, gridY),
            _ => null
        };

        if (element != null)
        {
            PlanViewModel.SelectedElement = element;
        }
    }

    private static SbSignal CreateMultiplexSignal(SignalBoxPlanViewModel planViewModel, int gridX, int gridY, string mainSignalArticle)
    {
        var signal = planViewModel.AddSignal(gridX, gridY);
        signal.SignalSystem = SignalSystemType.Ks;
        signal.MultiplexerArticleNumber = "5229";
        signal.IsMultiplexed = true;
        signal.MainSignalArticleNumber = mainSignalArticle;
        signal.BaseAddress = signal.Address;
        return signal;
    }

    private void OnCanvasPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (e.Handled) return;

        if (sender is not UIElement canvas)
            return;

        var point = e.GetCurrentPoint(canvas);

        if (point.Properties.IsRightButtonPressed)
        {
            _isPanning = true;
            _panStartPoint = point.Position;
            _panStartHorizontalOffset = CanvasScrollViewer.HorizontalOffset;
            _panStartVerticalOffset = CanvasScrollViewer.VerticalOffset;
            canvas.CapturePointer(e.Pointer);
        }
        else if (point.Properties.IsLeftButtonPressed)
        {
            var pos = e.GetCurrentPoint(canvas).Position;
            int gridX = Math.Clamp((int)(pos.X / GridCellSize), 0, GridColumns - 1);
            int gridY = Math.Clamp((int)(pos.Y / GridCellSize), 0, GridRows - 1);

            var elementUnderCursor = PlanViewModel?.HitTest(gridX, gridY);
            if (elementUnderCursor != null)
            {
                if (PlanViewModel != null)
                {
                    PlanViewModel.SelectedElement = elementUnderCursor;
                }
            }
            else
            {
                PlanViewModel?.ClearSelection();
            }
        }
    }

    private void OnCanvasPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isPanning) return;

        if (sender is not UIElement canvas)
            return;

        var point = e.GetCurrentPoint(canvas).Position;
        var deltaX = point.X - _panStartPoint.X;
        var deltaY = point.Y - _panStartPoint.Y;

        CanvasScrollViewer.ChangeView(
            _panStartHorizontalOffset - deltaX,
            _panStartVerticalOffset - deltaY,
            null,
            true);
    }

    private void OnCanvasPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            if (sender is UIElement canvas)
                canvas.ReleasePointerCapture(e.Pointer);
        }
    }
}
