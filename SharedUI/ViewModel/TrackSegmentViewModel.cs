// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using Domain.TrackPlan;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// ViewModel wrapper for TrackSegment (track plan visualization).
/// Handles UI state (selection, triggering) and feedback integration.
/// </summary>
public partial class TrackSegmentViewModel : ObservableObject
{
    #region Fields
    // Model
    private readonly TrackSegment _segment;
    #endregion

    public TrackSegmentViewModel(TrackSegment segment)
    {
        ArgumentNullException.ThrowIfNull(segment);
        _segment = segment;
    }

    /// <summary>
    /// Access to underlying domain model (for editing in TrackPlanEditorViewModel).
    /// </summary>
    public TrackSegment Model => _segment;

    #region Domain Properties (1:1 mapping)

    /// <summary>
    /// Unique identifier for this segment.
    /// </summary>
    public string Id => _segment.Id;

    /// <summary>
    /// Display name for this segment.
    /// </summary>
    public string Name => _segment.Name;

    /// <summary>
    /// Type of track segment.
    /// </summary>
    public TrackSegmentType Type => _segment.Type;

    /// <summary>
    /// Piko A-Gleis article code (e.g., "G231", "R2", "WL").
    /// </summary>
    public string ArticleCode => _segment.ArticleCode;

    /// <summary>
    /// SVG path data for rendering this segment.
    /// </summary>
    public string PathData => _segment.PathData;

    /// <summary>
    /// X coordinate of the segment's center point.
    /// </summary>
    public double CenterX => _segment.CenterX;

    /// <summary>
    /// Y coordinate of the segment's center point.
    /// </summary>
    public double CenterY => _segment.CenterY;

    /// <summary>
    /// X coordinate rounded for display.
    /// </summary>
    public string CenterXDisplay => CenterX.ToString("F1");

    /// <summary>
    /// Y coordinate rounded for display.
    /// </summary>
    public string CenterYDisplay => CenterY.ToString("F1");

    /// <summary>
    /// Rotation angle in degrees.
    /// </summary>
    public double Rotation => _segment.Rotation;

    /// <summary>
    /// Track number for multi-track stations.
    /// </summary>
    public string? TrackNumber => _segment.TrackNumber;

    /// <summary>
    /// Layer this segment belongs to.
    /// </summary>
    public string Layer => _segment.Layer;

    #endregion

    #region Computed Properties (for UI binding)
    
    /// <summary>
    /// Start point X coordinate (extracted from PathData).
    /// </summary>
    public double StartPointX => GetStartPoint().X;

    /// <summary>
    /// Start point Y coordinate (extracted from PathData).
    /// </summary>
    public double StartPointY => GetStartPoint().Y;

    /// <summary>
    /// End point X coordinate (extracted from PathData).
    /// </summary>
    public double EndPointX => GetEndPoint().X;

    /// <summary>
    /// End point Y coordinate (extracted from PathData).
    /// </summary>
    public double EndPointY => GetEndPoint().Y;

    /// <summary>
    /// Label X position (centered on track).
    /// For curves, offset slightly towards the curve center.
    /// </summary>
    public double LabelX
    {
        get
        {
            var midX = (StartPointX + EndPointX) / 2;
            // For curves (R1-R9), adjust position slightly
            if (ArticleCode.StartsWith("R"))
            {
                // Curves: move label towards the inner arc
                // Offset by a bit for better centering on arc
                return midX - 8;
            }
            return midX - 12;
        }
    }

    /// <summary>
    /// Label Y position (centered on track, slightly above).
    /// </summary>
    public double LabelY
    {
        get
        {
            var midY = (StartPointY + EndPointY) / 2;
            // For curves, position below the midpoint
            if (ArticleCode.StartsWith("R"))
            {
                return midY + 2;
            }
            return midY - 6;
        }
    }

    /// <summary>
    /// InPort label Y position (above the article code label).
    /// </summary>
    public double InPortLabelY => LabelY - 14;

    private (double X, double Y) GetStartPoint()
    {
        var coords = ExtractCoordinates(PathData);
        return coords.Count > 0 ? coords[0] : (CenterX, CenterY);
    }

    private (double X, double Y) GetEndPoint()
    {
        var coords = ExtractCoordinates(PathData);
        return coords.Count > 1 ? coords[^1] : (CenterX, CenterY);
    }

    /// <summary>
    /// Gets all unique endpoints of this segment.
    /// For turnouts (WL, WR, DWW, DKW), this returns all connection points (3-4 points).
    /// For simple tracks, this returns start and end (2 points).
    /// </summary>
    public List<(double X, double Y)> GetAllEndpoints()
    {
        var coords = ExtractCoordinates(PathData);
        if (coords.Count == 0)
            return [(CenterX, CenterY)];

        // For segments with multiple M commands (turnouts), extract all unique endpoints
        // An endpoint is where tracks can connect - typically the start of each sub-path
        // and the end of the last point in each sub-path
        var endpoints = new List<(double X, double Y)>();

        // Parse PathData to find all sub-path endpoints
        var pathData = PathData ?? string.Empty;
        var subPaths = ParseSubPaths(pathData);

        foreach (var subPath in subPaths)
        {
            if (subPath.Count > 0)
            {
                endpoints.Add(subPath[0]); // Start of sub-path
                if (subPath.Count > 1)
                    endpoints.Add(subPath[^1]); // End of sub-path
            }
        }

        // Remove duplicates (points within 1px tolerance)
        var uniqueEndpoints = new List<(double X, double Y)>();
        foreach (var ep in endpoints)
        {
            var isDuplicate = uniqueEndpoints.Any(existing =>
                Math.Abs(existing.X - ep.X) < 1 && Math.Abs(existing.Y - ep.Y) < 1);
            if (!isDuplicate)
                uniqueEndpoints.Add(ep);
        }

        return uniqueEndpoints.Count > 0 ? uniqueEndpoints : coords;
    }

    /// <summary>
    /// Parse PathData into separate sub-paths (each starting with M).
    /// </summary>
    private static List<List<(double X, double Y)>> ParseSubPaths(string pathData)
    {
        var subPaths = new List<List<(double X, double Y)>>();
        var currentSubPath = new List<(double X, double Y)>();

        var ic = CultureInfo.InvariantCulture;
        var commandRegex = new Regex(@"([MLAHVCSQTZ])\s*([-\d.,\s]+)", RegexOptions.IgnoreCase);
        var numberRegex = new Regex(@"-?\d+\.?\d*");

        foreach (Match cmdMatch in commandRegex.Matches(pathData))
        {
            var command = cmdMatch.Groups[1].Value.ToUpperInvariant();
            var args = cmdMatch.Groups[2].Value;
            var numbers = numberRegex.Matches(args)
                .Select(m => double.TryParse(m.Value, NumberStyles.Float, ic, out var v) ? v : 0)
                .ToList();

            switch (command)
            {
                case "M" when numbers.Count >= 2:
                    // New sub-path starts
                    if (currentSubPath.Count > 0)
                        subPaths.Add(currentSubPath);
                    currentSubPath = [(numbers[0], numbers[1])];
                    break;

                case "L" when numbers.Count >= 2:
                    currentSubPath.Add((numbers[0], numbers[1]));
                    break;

                case "A" when numbers.Count >= 7:
                    currentSubPath.Add((numbers[5], numbers[6]));
                    break;
            }
        }

        if (currentSubPath.Count > 0)
            subPaths.Add(currentSubPath);

        return subPaths;
    }

    private static List<(double X, double Y)> ExtractCoordinates(string? pathData)
    {
        var points = new List<(double X, double Y)>();
        if (string.IsNullOrEmpty(pathData))
            return points;

        var ic = CultureInfo.InvariantCulture;

        // Match coordinates in SVG path format: "M x y", "L x y", "A ... x y"
        // Supports both comma-separated (x,y) and space-separated (x y) formats
        // Look for command letters followed by coordinates
        var commandRegex = new Regex(@"([MLAHVCSQTZ])\s*([-\d.,\s]+)", RegexOptions.IgnoreCase);
        var numberRegex = new Regex(@"-?\d+\.?\d*");

        foreach (Match cmdMatch in commandRegex.Matches(pathData))
        {
            var command = cmdMatch.Groups[1].Value.ToUpperInvariant();
            var args = cmdMatch.Groups[2].Value;
            var numbers = numberRegex.Matches(args)
                .Select(m => double.TryParse(m.Value, NumberStyles.Float, ic, out var v) ? v : 0)
                .ToList();

            switch (command)
            {
                case "M" or "L" when numbers.Count >= 2:
                    // MoveTo / LineTo: x y
                    points.Add((numbers[0], numbers[1]));
                    break;

                case "A" when numbers.Count >= 7:
                    // Arc: rx ry x-axis-rotation large-arc sweep x y
                    // The endpoint is at indices 5 and 6
                    points.Add((numbers[5], numbers[6]));
                    break;
            }
        }

        return points;
    }
    #endregion

    #region Connection State
    /// <summary>
    /// Indicates whether the start endpoint is connected to another segment.
    /// </summary>
    [ObservableProperty]
    private bool isStartConnected;

    /// <summary>
    /// Indicates whether the end endpoint is connected to another segment.
    /// </summary>
    [ObservableProperty]
    private bool isEndConnected;

    /// <summary>
    /// ID of the segment connected at the start endpoint.
    /// </summary>
    public string? StartConnectedSegmentId { get; private set; }

    /// <summary>
    /// ID of the segment connected at the end endpoint.
    /// </summary>
    public string? EndConnectedSegmentId { get; private set; }

    /// <summary>
    /// Visibility for start endpoint circle (hidden when connected).
    /// </summary>
    public bool ShowStartEndpoint => !IsStartConnected;

    /// <summary>
    /// Visibility for end endpoint circle (hidden when connected).
    /// </summary>
    public bool ShowEndEndpoint => !IsEndConnected;

    partial void OnIsStartConnectedChanged(bool _)
    {
        OnPropertyChanged(nameof(ShowStartEndpoint));
    }

    partial void OnIsEndConnectedChanged(bool _)
    {
        OnPropertyChanged(nameof(ShowEndEndpoint));
    }

    /// <summary>
    /// Set the connection state for an endpoint.
    /// </summary>
    public void SetConnectionState(bool isStart, bool isConnected, string? connectedSegmentId)
    {
        if (isStart)
        {
            IsStartConnected = isConnected;
            StartConnectedSegmentId = connectedSegmentId;
        }
        else
        {
            IsEndConnected = isConnected;
            EndConnectedSegmentId = connectedSegmentId;
        }
    }
    #endregion

    #region ViewModel Properties (UI state)
    /// <summary>
    /// Indicates whether this segment is currently selected in the UI.
    /// </summary>
    [ObservableProperty]
    private bool isSelected;

    /// <summary>
    /// Indicates whether this segment's sensor is currently triggered (feedback active).
    /// </summary>
    [ObservableProperty]
    private bool isTriggered;

    /// <summary>
    /// Assigned InPort for feedback sensor. Null if no sensor assigned.
    /// </summary>
    public uint? AssignedInPort
    {
        get => _segment.AssignedInPort;
        set
        {
            if (_segment.AssignedInPort != value)
            {
                _segment.AssignedInPort = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasInPort));
                OnPropertyChanged(nameof(InPortDisplayText));
            }
        }
    }

    /// <summary>
    /// Indicates whether this segment has an InPort assigned.
    /// </summary>
    public bool HasInPort => _segment.AssignedInPort.HasValue;

    /// <summary>
    /// Display text for the InPort (e.g., "[1]" or empty).
    /// </summary>
    public string InPortDisplayText => _segment.AssignedInPort.HasValue
        ? $"[{_segment.AssignedInPort.Value}]"
        : string.Empty;

    #endregion

    #region Computed Properties for Styling

    /// <summary>
    /// Stroke thickness based on selection state.
    /// Thicker lines make tracks easier to click with mouse.
    /// </summary>
    public double StrokeThickness => IsSelected ? 35 : 25;

    /// <summary>
    /// Display text combining article code and InPort.
    /// </summary>
    public string DisplayLabel => HasInPort
        ? $"{ArticleCode} [{AssignedInPort}]"
        : ArticleCode;

    #endregion

    /// <summary>
    /// Updates triggered state based on feedback from Z21.
    /// </summary>
    /// <param name="inPort">The InPort that was triggered.</param>
    /// <param name="isOccupied">Whether the track is occupied.</param>
    public void UpdateFeedback(uint inPort, bool isOccupied)
    {
        if (AssignedInPort == inPort)
        {
            IsTriggered = isOccupied;
        }
    }

    /// <summary>
    /// Set rotation angle and notify UI.
    /// </summary>
    public void SetRotation(double rotation)
    {
        _segment.Rotation = rotation;
        OnPropertyChanged(nameof(Rotation));
    }

    /// <summary>
    /// Set position and notify UI.
    /// Also updates PathData to reflect new position.
    /// </summary>
    public void SetPosition(double centerX, double centerY)
    {
        _segment.CenterX = centerX;
        _segment.CenterY = centerY;
        OnPropertyChanged(nameof(CenterX));
        OnPropertyChanged(nameof(CenterY));
    }

    /// <summary>
    /// Update PathData with new coordinates (for absolute positioning).
    /// </summary>
    public void SetPathData(string pathData)
    {
        _segment.PathData = pathData;
        OnPropertyChanged(nameof(PathData));
    }

    /// <summary>
    /// Move the segment by delta and update PathData.
    /// </summary>
    public void MoveBy(double deltaX, double deltaY)
    {
        // Update position
        _segment.CenterX += deltaX;
        _segment.CenterY += deltaY;
        
        // Update PathData by adjusting all coordinates
        var newPathData = MovePathData(_segment.PathData, deltaX, deltaY);
        _segment.PathData = newPathData;
        
        OnPropertyChanged(nameof(CenterX));
        OnPropertyChanged(nameof(CenterY));
        OnPropertyChanged(nameof(CenterXDisplay));
        OnPropertyChanged(nameof(CenterYDisplay));
        OnPropertyChanged(nameof(PathData));
        
        // Also notify computed properties that depend on PathData
        OnPropertyChanged(nameof(StartPointX));
        OnPropertyChanged(nameof(StartPointY));
        OnPropertyChanged(nameof(EndPointX));
        OnPropertyChanged(nameof(EndPointY));
        OnPropertyChanged(nameof(LabelX));
        OnPropertyChanged(nameof(LabelY));
    }

    /// <summary>
    /// Move all coordinates in PathData by delta.
    /// Handles SVG path format: "M x y L x y A rx ry rotation large sweep x y"
    /// </summary>
    private static string MovePathData(string pathData, double deltaX, double deltaY)
    {
        if (string.IsNullOrEmpty(pathData))
            return pathData;

        var ic = CultureInfo.InvariantCulture;
        var result = new StringBuilder();

        // Parse SVG commands with their arguments
        var commandRegex = new Regex(@"([MLAHVCSQTZ])\s*([-\d.\s]+)", RegexOptions.IgnoreCase);
        var numberRegex = new Regex(@"-?\d+\.?\d*");

        var lastIndex = 0;

        foreach (Match cmdMatch in commandRegex.Matches(pathData))
        {
            // Append any text before this match (whitespace, etc.)
            if (cmdMatch.Index > lastIndex)
            {
                result.Append(pathData.AsSpan(lastIndex, cmdMatch.Index - lastIndex));
            }

            var command = cmdMatch.Groups[1].Value.ToUpperInvariant();
            var args = cmdMatch.Groups[2].Value;
            var numbers = numberRegex.Matches(args)
                .Select(m => double.TryParse(m.Value, NumberStyles.Float, ic, out var v) ? v : 0)
                .ToList();

            result.Append(command);
            result.Append(' ');

            switch (command)
            {
                case "M" or "L" when numbers.Count >= 2:
                    // MoveTo / LineTo: move x y coordinates
                    result.Append(string.Format(ic, "{0:F0} {1:F0} ", numbers[0] + deltaX, numbers[1] + deltaY));
                    break;

                case "A" when numbers.Count >= 7:
                    // Arc: rx ry x-axis-rotation large-arc sweep x y
                    // Only move the endpoint (last two numbers), keep rx, ry, rotation, flags unchanged
                    result.Append(string.Format(ic, "{0:F0} {1:F0} {2:F0} {3:F0} {4:F0} {5:F0} {6:F0} ",
                        numbers[0],  // rx (unchanged)
                        numbers[1],  // ry (unchanged)
                        numbers[2],  // x-axis-rotation (unchanged)
                        numbers[3],  // large-arc-flag (unchanged)
                        numbers[4],  // sweep-flag (unchanged)
                        numbers[5] + deltaX,  // endpoint x (moved)
                        numbers[6] + deltaY)); // endpoint y (moved)
                    break;

                default:
                    // Unknown command - keep original arguments
                    result.Append(args);
                    break;
            }

            lastIndex = cmdMatch.Index + cmdMatch.Length;
        }

        // Append any remaining text after last match
        if (lastIndex < pathData.Length)
        {
            result.Append(pathData.AsSpan(lastIndex));
        }

        return result.ToString().Trim();
    }

    partial void OnIsSelectedChanged(bool _)
    {
        OnPropertyChanged(nameof(StrokeThickness));
    }
}
