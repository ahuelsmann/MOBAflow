namespace Moba.WinUI.Behavior;

using Common.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Xaml.Interactivity;

using SharedUI.Interface;

using Windows.Foundation;

public sealed class GridColumnResizeBehavior : Behavior<Grid>
{
    public static readonly DependencyProperty PersistenceKeyProperty =
        DependencyProperty.Register(
            nameof(PersistenceKey),
            typeof(string),
            typeof(GridColumnResizeBehavior),
            new PropertyMetadata(null));

    public static readonly DependencyProperty HitAreaWidthProperty =
        DependencyProperty.Register(
            nameof(HitAreaWidth),
            typeof(double),
            typeof(GridColumnResizeBehavior),
            new PropertyMetadata(16d));

    public static readonly DependencyProperty SplitterModeProperty =
        DependencyProperty.RegisterAttached(
            "SplitterMode",
            typeof(string),
            typeof(GridColumnResizeBehavior),
            new PropertyMetadata(null));

    private BoundaryCandidate? _activeBoundary;
    private bool _isDragging;
    private Point _dragStartPoint;
    private double _leftStartWidth;
    private double _rightStartWidth;
    private ResizeMode _resizeMode;
    private AppSettings? _settings;
    private ISettingsService? _settingsService;
    private readonly PointerEventHandler _pointerPressedHandler;

    public GridColumnResizeBehavior()
    {
        _pointerPressedHandler = OnPointerPressed;
    }

    public string? PersistenceKey
    {
        get => (string?)GetValue(PersistenceKeyProperty);
        set => SetValue(PersistenceKeyProperty, value);
    }

    public double HitAreaWidth
    {
        get => (double)GetValue(HitAreaWidthProperty);
        set => SetValue(HitAreaWidthProperty, value);
    }

    public static string? GetSplitterMode(DependencyObject obj)
    {
        return (string?)obj.GetValue(SplitterModeProperty);
    }

    public static void SetSplitterMode(DependencyObject obj, string? value)
    {
        obj.SetValue(SplitterModeProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject == null)
            return;

        if (AssociatedObject.Background == null)
            AssociatedObject.Background = new SolidColorBrush(Colors.Transparent);

        AssociatedObject.Loaded += OnLoaded;
        AssociatedObject.PointerExited += OnPointerExited;
        AssociatedObject.PointerCaptureLost += OnPointerCaptureLost;
        AssociatedObject.PointerMoved += OnPointerMoved;
        AssociatedObject.PointerReleased += OnPointerReleased;
        AssociatedObject.AddHandler(UIElement.PointerPressedEvent, _pointerPressedHandler, true);
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject != null)
        {
            AssociatedObject.Loaded -= OnLoaded;
            AssociatedObject.PointerExited -= OnPointerExited;
            AssociatedObject.PointerCaptureLost -= OnPointerCaptureLost;
            AssociatedObject.PointerMoved -= OnPointerMoved;
            AssociatedObject.PointerReleased -= OnPointerReleased;
            AssociatedObject.RemoveHandler(UIElement.PointerPressedEvent, _pointerPressedHandler);
        }

        ResetCursor();
        base.OnDetaching();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        RestorePersistedWidths();
        ResetCursor();
    }

    private void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        _ = sender;
        _ = e;

        if (!_isDragging)
            ResetCursor();
    }

    private void OnPointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        _ = sender;
        _ = e;

        _isDragging = false;
        _activeBoundary = null;
        ResetCursor();
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (AssociatedObject == null)
            return;

        var currentPoint = e.GetCurrentPoint(AssociatedObject).Position;

        if (!_isDragging)
        {
            UpdateCursor(FindCandidate(currentPoint.X));
            return;
        }

        ShowResizeCursor();
        ApplyDrag(currentPoint.X);
        e.Handled = true;
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (AssociatedObject == null)
            return;

        var currentPoint = e.GetCurrentPoint(AssociatedObject);
        if (!currentPoint.Properties.IsLeftButtonPressed)
            return;

        var candidate = FindCandidate(currentPoint.Position.X);
        if (candidate == null)
            return;

        var columns = AssociatedObject.ColumnDefinitions;
        var leftColumn = columns[candidate.LeftColumnIndex];
        var rightColumn = columns[candidate.RightColumnIndex];
        var resizeMode = GetResizeMode(candidate, leftColumn, rightColumn);
        if (resizeMode == ResizeMode.None)
            return;

        _activeBoundary = candidate;
        _dragStartPoint = currentPoint.Position;
        _isDragging = true;
        _leftStartWidth = leftColumn.ActualWidth;
        _rightStartWidth = rightColumn.ActualWidth;
        _resizeMode = resizeMode;

        ShowResizeCursor();
        AssociatedObject.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDragging || AssociatedObject == null)
            return;

        _isDragging = false;
        AssociatedObject.ReleasePointerCapture(e.Pointer);
        PersistCurrentWidths();

        var currentPoint = e.GetCurrentPoint(AssociatedObject).Position;
        UpdateCursor(FindCandidate(currentPoint.X));

        e.Handled = true;
    }

    private void ApplyDrag(double currentX)
    {
        if (AssociatedObject == null || _activeBoundary == null)
            return;

        var columns = AssociatedObject.ColumnDefinitions;
        var leftColumn = columns[_activeBoundary.LeftColumnIndex];
        var rightColumn = columns[_activeBoundary.RightColumnIndex];
        var delta = currentX - _dragStartPoint.X;

        switch (_resizeMode)
        {
            case ResizeMode.LeftOnly:
                leftColumn.Width = new GridLength(Clamp(_leftStartWidth + delta, leftColumn.MinWidth, GetMaxWidth(leftColumn)));
                break;

            case ResizeMode.RightOnly:
                rightColumn.Width = new GridLength(Clamp(_rightStartWidth - delta, rightColumn.MinWidth, GetMaxWidth(rightColumn)));
                break;

            case ResizeMode.Both:
                var totalWidth = _leftStartWidth + _rightStartWidth;
                var minLeft = Math.Max(leftColumn.MinWidth, totalWidth - GetMaxWidth(rightColumn));
                var maxLeft = Math.Min(GetMaxWidth(leftColumn), totalWidth - rightColumn.MinWidth);
                if (maxLeft < minLeft)
                    return;

                var newLeft = Clamp(_leftStartWidth + delta, minLeft, maxLeft);
                var newRight = totalWidth - newLeft;
                leftColumn.Width = new GridLength(newLeft);
                rightColumn.Width = new GridLength(newRight);
                break;
        }
    }

    private BoundaryCandidate? FindCandidate(double pointerX)
    {
        if (AssociatedObject == null)
            return null;

        return BuildCandidates()
            .Where(candidate => pointerX >= candidate.HitStart && pointerX <= candidate.HitEnd)
            .Where(candidate => GetResizeMode(
                candidate,
                AssociatedObject.ColumnDefinitions[candidate.LeftColumnIndex],
                AssociatedObject.ColumnDefinitions[candidate.RightColumnIndex]) != ResizeMode.None)
            .OrderBy(candidate => Math.Abs(pointerX - candidate.CenterX))
            .FirstOrDefault();
    }

    private List<BoundaryCandidate> BuildCandidates()
    {
        if (AssociatedObject == null)
            return [];

        var columns = AssociatedObject.ColumnDefinitions;
        if (columns.Count < 2)
            return [];

        var offsets = new double[columns.Count];
        double currentOffset = 0;
        for (var i = 0; i < columns.Count; i++)
        {
            offsets[i] = currentOffset;
            currentOffset += columns[i].ActualWidth;
            if (i < columns.Count - 1)
                currentOffset += AssociatedObject.ColumnSpacing;
        }

        var candidates = new List<BoundaryCandidate>();

        for (var i = 0; i < columns.Count; i++)
        {
            if (!IsSplitterColumn(columns[i]) || i == 0 || i == columns.Count - 1)
                continue;

            var leftIndex = FindPreviousContentColumnIndex(columns, i);
            var rightIndex = FindNextContentColumnIndex(columns, i);
            if (leftIndex < 0 || rightIndex < 0)
                continue;

            var actualWidth = columns[i].ActualWidth;
            var centerX = offsets[i] + (actualWidth / 2);
            var effectiveWidth = Math.Max(actualWidth, HitAreaWidth);
            var hitStart = centerX - (effectiveWidth / 2);
            var hitEnd = centerX + (effectiveWidth / 2);

            candidates.Add(new BoundaryCandidate(
                leftIndex,
                rightIndex,
                centerX,
                hitStart,
                hitEnd,
                true,
                FindSplitterMode(i)));
        }

        for (var i = 0; i < columns.Count - 1; i++)
        {
            if (IsSplitterColumn(columns[i]) || IsSplitterColumn(columns[i + 1]))
                continue;

            var boundaryStart = offsets[i] + columns[i].ActualWidth;
            var boundaryEnd = boundaryStart + AssociatedObject.ColumnSpacing;
            candidates.Add(new BoundaryCandidate(
                i,
                i + 1,
                (boundaryStart + boundaryEnd) / 2,
                boundaryStart - (HitAreaWidth / 2),
                boundaryEnd + (HitAreaWidth / 2),
                false,
                null));
        }

        return candidates;
    }

    private static int FindPreviousContentColumnIndex(IList<ColumnDefinition> columns, int splitterIndex)
    {
        for (var i = splitterIndex - 1; i >= 0; i--)
        {
            if (!IsSplitterColumn(columns[i]))
                return i;
        }

        return -1;
    }

    private static int FindNextContentColumnIndex(IList<ColumnDefinition> columns, int splitterIndex)
    {
        for (var i = splitterIndex + 1; i < columns.Count; i++)
        {
            if (!IsSplitterColumn(columns[i]))
                return i;
        }

        return -1;
    }

    private static bool IsSplitterColumn(ColumnDefinition column)
    {
        return column.Width.GridUnitType == GridUnitType.Auto && column.ActualWidth <= 24;
    }

    private static ResizeMode GetResizeMode(BoundaryCandidate candidate, ColumnDefinition leftColumn, ColumnDefinition rightColumn)
    {
        if (TryParseResizeMode(candidate.ModeHint, out var explicitMode))
            return explicitMode;

        var leftKind = GetColumnKind(leftColumn);
        var rightKind = GetColumnKind(rightColumn);

        if (candidate.IsSplitter)
        {
            if (leftKind != ColumnKind.Auto)
                return ResizeMode.LeftOnly;

            return rightKind != ColumnKind.Auto
                ? ResizeMode.RightOnly
                : ResizeMode.None;
        }

        if (leftKind == ColumnKind.Auto && rightKind == ColumnKind.Auto)
            return ResizeMode.None;

        if (leftKind == ColumnKind.Auto)
            return ResizeMode.RightOnly;

        if (rightKind == ColumnKind.Auto)
            return ResizeMode.LeftOnly;

        if (leftKind == ColumnKind.Star && rightKind == ColumnKind.Pixel)
            return ResizeMode.RightOnly;

        if (leftKind == ColumnKind.Pixel && rightKind == ColumnKind.Star)
            return ResizeMode.LeftOnly;

        return ResizeMode.Both;
    }

    private static ColumnKind GetColumnKind(ColumnDefinition column)
    {
        return column.Width.GridUnitType switch
        {
            GridUnitType.Auto => ColumnKind.Auto,
            GridUnitType.Star => ColumnKind.Star,
            _ => ColumnKind.Pixel
        };
    }

    private string? FindSplitterMode(int splitterColumnIndex)
    {
        if (AssociatedObject == null)
            return null;

        return AssociatedObject.Children
            .OfType<FrameworkElement>()
            .Where(child => Grid.GetColumn(child) == splitterColumnIndex)
            .Select(GetSplitterMode)
            .FirstOrDefault(mode => !string.IsNullOrWhiteSpace(mode));
    }

    private static bool TryParseResizeMode(string? modeHint, out ResizeMode resizeMode)
    {
        resizeMode = ResizeMode.None;
        if (string.IsNullOrWhiteSpace(modeHint))
            return false;

        if (string.Equals(modeHint, "Left", StringComparison.OrdinalIgnoreCase))
        {
            resizeMode = ResizeMode.LeftOnly;
            return true;
        }

        if (string.Equals(modeHint, "Right", StringComparison.OrdinalIgnoreCase))
        {
            resizeMode = ResizeMode.RightOnly;
            return true;
        }

        if (string.Equals(modeHint, "Both", StringComparison.OrdinalIgnoreCase))
        {
            resizeMode = ResizeMode.Both;
            return true;
        }

        return false;
    }

    private void RestorePersistedWidths()
    {
        EnsureServices();
        if (_settings == null || string.IsNullOrWhiteSpace(PersistenceKey) || AssociatedObject == null)
            return;

        for (var i = 0; i < AssociatedObject.ColumnDefinitions.Count; i++)
        {
            if (!TryGetPersistedWidth(i, out var width))
                continue;

            var column = AssociatedObject.ColumnDefinitions[i];
            column.Width = new GridLength(Clamp(width, column.MinWidth, GetMaxWidth(column)));
        }
    }

    private void PersistCurrentWidths()
    {
        EnsureServices();
        if (_settings == null || string.IsNullOrWhiteSpace(PersistenceKey) || AssociatedObject == null || _activeBoundary == null)
            return;

        if (_resizeMode is ResizeMode.LeftOnly or ResizeMode.Both)
            PersistWidth(_activeBoundary.LeftColumnIndex, AssociatedObject.ColumnDefinitions[_activeBoundary.LeftColumnIndex].ActualWidth);

        if (_resizeMode is ResizeMode.RightOnly or ResizeMode.Both)
            PersistWidth(_activeBoundary.RightColumnIndex, AssociatedObject.ColumnDefinitions[_activeBoundary.RightColumnIndex].ActualWidth);

        if (_settingsService != null)
            _ = _settingsService.SaveSettingsAsync(_settings);
    }

    private void PersistWidth(int columnIndex, double width)
    {
        if (_settings == null || string.IsNullOrWhiteSpace(PersistenceKey))
            return;

        _settings.Layout.ColumnWidths[BuildWidthKey(columnIndex)] = width;

        if (PersistenceKey == "JourneysPage")
        {
            if (columnIndex == 0)
                _settings.Layout.JourneysPage.JourneysColumnWidth = width;
            else if (columnIndex == 2)
                _settings.Layout.JourneysPage.StationsColumnWidth = width;
        }
    }

    private bool TryGetPersistedWidth(int columnIndex, out double width)
    {
        width = default;
        if (_settings == null || string.IsNullOrWhiteSpace(PersistenceKey))
            return false;

        if (_settings.Layout.ColumnWidths.TryGetValue(BuildWidthKey(columnIndex), out width))
            return width > 0;

        if (PersistenceKey == "JourneysPage")
        {
            if (columnIndex == 0)
            {
                width = _settings.Layout.JourneysPage.JourneysColumnWidth;
                return width > 0;
            }

            if (columnIndex == 2)
            {
                width = _settings.Layout.JourneysPage.StationsColumnWidth;
                return width > 0;
            }
        }

        return false;
    }

    private string BuildWidthKey(int columnIndex)
    {
        return $"{PersistenceKey}:{columnIndex}";
    }

    private void EnsureServices()
    {
        if (_settings != null)
            return;

        _settings = App.Current.Services.GetService<AppSettings>();
        _settingsService = App.Current.Services.GetService<ISettingsService>();
    }

    private static double GetMaxWidth(ColumnDefinition column)
    {
        return double.IsInfinity(column.MaxWidth)
            ? double.MaxValue
            : column.MaxWidth;
    }

    private static double Clamp(double value, double min, double max)
    {
        return Math.Clamp(value, min, max);
    }

    private void UpdateCursor(BoundaryCandidate? candidate)
    {
        if (candidate == null)
        {
            ResetCursor();
            return;
        }

        ShowResizeCursor();
    }

    private void ShowResizeCursor()
    {
        if (AssociatedObject is CursorGrid cursorGrid)
            cursorGrid.ShowResizeCursor();
    }

    private void ResetCursor()
    {
        if (AssociatedObject is CursorGrid cursorGrid)
            cursorGrid.ShowDefaultCursor();
    }

    private sealed record BoundaryCandidate(
        int LeftColumnIndex,
        int RightColumnIndex,
        double CenterX,
        double HitStart,
        double HitEnd,
        bool IsSplitter,
        string? ModeHint);

    private enum ResizeMode
    {
        None,
        LeftOnly,
        RightOnly,
        Both
    }

    private enum ColumnKind
    {
        Auto,
        Pixel,
        Star
    }
}
