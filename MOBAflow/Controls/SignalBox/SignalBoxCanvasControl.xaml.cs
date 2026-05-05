// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Controls.SignalBox;

using Domain;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;

using Common.Extension;
using SharedUI.ViewModel;

using System;

using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;

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
    private ILogger<SignalBoxCanvasControl>? _logger;

    /// <summary>
    /// Attaches an optional logger for drop-handling failures (control is not constructed via DI).
    /// </summary>
    internal void AttachLogger(ILogger<SignalBoxCanvasControl>? logger) => _logger = logger;

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

    private void OnCanvasDrop(object sender, DragEventArgs e)
    {
        HandleCanvasDropAsync(sender, e).Observe(ex => _logger?.LogWarning(ex, "Signal box canvas drop handling failed"));
    }

    private async Task HandleCanvasDropAsync(object sender, DragEventArgs e)
    {
        try
        {
            if (!e.DataView.Contains(StandardDataFormats.Text) || PlanViewModel == null)
                return;

            if (sender is not UIElement canvas)
                return;

            var text = await e.DataView.GetTextAsync();
            if (string.IsNullOrEmpty(text))
                return;

            var (gridX, gridY) = GetGridPosition(e.GetPosition(canvas));

            var existingElement = PlanViewModel.HitTest(gridX, gridY);

            if (text.StartsWith("NEW:"))
            {
                TryHandleNewElementDrop(text[4..], gridX, gridY, existingElement);
            }
            else if (text.StartsWith("MOVE:") && Guid.TryParse(text[5..], out var elementId))
            {
                TryHandleMoveDrop(elementId, gridX, gridY, existingElement);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Signal box canvas drop handling failed");
        }
    }

    private void TryHandleNewElementDrop(string typeTag, int gridX, int gridY, SbElement? existingElement)
    {
        if (PlanViewModel == null)
        {
            return;
        }

        if (existingElement != null)
        {
            PlanViewModel.RemoveElement(existingElement);
        }

        CreateElementByType(typeTag, gridX, gridY);
    }

    private void TryHandleMoveDrop(Guid elementId, int gridX, int gridY, SbElement? existingElement)
    {
        if (PlanViewModel == null)
        {
            return;
        }

        var element = PlanViewModel.FindById(elementId);
        if (element == null || (existingElement != null && existingElement.Id != elementId))
        {
            return;
        }

        element.X = gridX;
        element.Y = gridY;

        // Reinsert keeps the observable collection update explicit for the canvas binding.
        var index = PlanViewModel.Elements.IndexOf(element);
        if (index != -1)
        {
            PlanViewModel.Elements.RemoveAt(index);
            PlanViewModel.Elements.Insert(index, element);
        }

        PlanViewModel.SelectedElement = element;
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
            SelectElementAtPointer(canvas, e);
        }
    }

    private void SelectElementAtPointer(UIElement canvas, PointerRoutedEventArgs e)
    {
        if (PlanViewModel == null)
        {
            return;
        }

        var (gridX, gridY) = GetGridPosition(e.GetCurrentPoint(canvas).Position);
        var elementUnderCursor = PlanViewModel.HitTest(gridX, gridY);
        if (elementUnderCursor != null)
        {
            PlanViewModel.SelectedElement = elementUnderCursor;
            return;
        }

        PlanViewModel.ClearSelection();
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

    private static (int GridX, int GridY) GetGridPosition(Point position)
    {
        var gridX = Math.Clamp((int)(position.X / GridCellSize), 0, GridColumns - 1);
        var gridY = Math.Clamp((int)(position.Y / GridCellSize), 0, GridRows - 1);
        return (gridX, gridY);
    }
}
