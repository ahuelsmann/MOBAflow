// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Domain;

using System.Text.Json.Serialization;

/// <summary>
/// SignalBoxPlan (Gleisbild) - Topological representation of a railway interlocking.
/// Stores elements and their connections for signal box visualization.
/// Symbol-based approach following DB (Deutsche Bahn) standards.
/// </summary>
public class SignalBoxPlan
{
    /// <summary>
    /// Unique identifier for this plan.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Name of the signal box / station.
    /// </summary>
    public string Name { get; set; } = "Stellwerk";

    /// <summary>
    /// Grid configuration (width, height, cell size).
    /// </summary>
    public GridConfig Grid { get; set; } = new(20, 12, 60);

    /// <summary>
    /// All elements in the signal box plan (typed hierarchy).
    /// </summary>
    public List<SbElement> Elements { get; set; } = [];

    /// <summary>
    /// Connections between elements (topology).
    /// </summary>
    public List<SignalBoxConnection> Connections { get; set; } = [];

    /// <summary>
    /// Routes (Fahrstrassen) defined for this plan.
    /// </summary>
    public List<SignalBoxRoute> Routes { get; set; } = [];
}

// ═══════════════════════════════════════════════════════════════════════════
// SIGNAL BOX ELEMENTS - Typed Element Hierarchy
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Connection point direction (cardinal and diagonal directions).
/// Maps to 8 compass directions for topological connections.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ConnectionPointDirection
{
    /// <summary>North (0°, top)</summary>
    North = 0,

    /// <summary>East (90°, right)</summary>
    East = 1,

    /// <summary>South (180°, bottom)</summary>
    South = 2,

    /// <summary>West (270°, left)</summary>
    West = 3,

    /// <summary>NorthEast (45°, top-right)</summary>
    NorthEast = 4,

    /// <summary>SouthEast (135°, bottom-right)</summary>
    SouthEast = 5,

    /// <summary>SouthWest (225°, bottom-left)</summary>
    SouthWest = 6,

    /// <summary>NorthWest (315°, top-left)</summary>
    NorthWest = 7
}

/// <summary>
/// Abstract base class for all signal box plan elements.
/// Grid-based positioning: Cell [0,0] = top-left, unbounded size.
/// One element per cell maximum.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(SbTrackStraight), "TrackStraight")]
[JsonDerivedType(typeof(SbTrackCurve), "TrackCurve")]
[JsonDerivedType(typeof(SbSwitch), "Switch")]
[JsonDerivedType(typeof(SbSignal), "Signal")]
[JsonDerivedType(typeof(SbDetector), "Detector")]
public abstract record SbElement
{
    /// <summary>Unique identifier for this element.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Grid position X (column). Cell [0,0] = top-left.</summary>
    public int X { get; set; }

    /// <summary>Grid position Y (row). Cell [0,0] = top-left.</summary>
    public int Y { get; set; }

    /// <summary>Rotation in degrees (0, 90, 180, 270).</summary>
    public int Rotation { get; set; }

    /// <summary>Display name (e.g., "W1" for Weiche 1, "N1" for Signal N1).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Current state of the element (free, occupied, route set).</summary>
    public SignalBoxElementState State { get; set; } = SignalBoxElementState.Free;
}

/// <summary>
/// Straight track element (horizontal/vertical).
/// </summary>
public sealed record SbTrackStraight : SbElement;

/// <summary>
/// Curved track element (90 degrees only).
/// </summary>
public sealed record SbTrackCurve : SbElement;

/// <summary>
/// Switch element (Weiche) with DCC address and position.
/// </summary>
public sealed record SbSwitch : SbElement
{
    /// <summary>DCC address for Z21 control (0 = not configured).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Address { get; set; }

    /// <summary>Current switch position.</summary>
    public SwitchPosition SwitchPosition { get; set; } = SwitchPosition.Straight;
}

/// <summary>
/// Signal element with DCC address, system type and current aspect.
/// </summary>
public sealed record SbSignal : SbElement
{
    /// <summary>DCC address for Z21 control (0 = not configured).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Address { get; set; }

    /// <summary>Signal system type (Ks, Hv, Hl, Form, Sv).</summary>
    public SignalSystemType SignalSystem { get; set; } = SignalSystemType.Ks;

    /// <summary>Current signal aspect.</summary>
    public SignalAspect SignalAspect { get; set; } = SignalAspect.Hp0;
}

/// <summary>
/// Detector element for occupancy feedback.
/// </summary>
public sealed record SbDetector : SbElement
{
    /// <summary>Feedback address for occupancy detection (0 = not configured).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int FeedbackAddress { get; set; }
}

/// <summary>
/// Connection between two elements (topological link).
/// </summary>
public class SignalBoxConnection
{
    /// <summary>
    /// Source element ID.
    /// </summary>
    public Guid FromElementId { get; set; }

    /// <summary>
    /// Source connection point (directional).
    /// </summary>
    public ConnectionPointDirection FromDirection { get; set; } = ConnectionPointDirection.North;

    /// <summary>
    /// Target element ID.
    /// </summary>
    public Guid ToElementId { get; set; }

    /// <summary>
    /// Target connection point (directional).
    /// </summary>
    public ConnectionPointDirection ToDirection { get; set; } = ConnectionPointDirection.North;
}

/// <summary>
/// A route (Fahrstrasse) in the signal box.
/// </summary>
public class SignalBoxRoute
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Route name (e.g., "F1" for Fahrstrasse 1).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Start signal ID.
    /// </summary>
    public Guid StartSignalId { get; set; }

    /// <summary>
    /// End signal or destination ID.
    /// </summary>
    public Guid EndSignalId { get; set; }

    /// <summary>
    /// List of element IDs that are part of this route.
    /// </summary>
    public List<Guid> ElementIds { get; set; } = [];

    /// <summary>
    /// Switch positions required for this route.
    /// Key: Switch element ID, Value: Position (Straight/Diverging).
    /// </summary>
    public Dictionary<Guid, SwitchPosition> SwitchPositions { get; set; } = [];
}

/// <summary>
/// Signal system types used in German railways.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SignalSystemType
{
    /// <summary>Kombinationssignal - Modern unified system (since 1993)</summary>
    Ks,

    /// <summary>Haupt-/Vorsignal - Classic West German light signals</summary>
    Hv,

    /// <summary>Hl-System - East German signals (DR)</summary>
    Hl,

    /// <summary>Formsignale - Semaphore signals (historical)</summary>
    Form,

    /// <summary>Simplified Ks - (Sv) for S-Bahn</summary>
    Sv
}

/// <summary>
/// Signal aspect (current display state).
/// Based on German Ks-Signalsystem but applicable to other systems.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SignalAspect
{
    /// <summary>Hp0 - Stop (red)</summary>
    Hp0,

    /// <summary>Ks1 - Proceed (green)</summary>
    Ks1,

    /// <summary>Ks2 - Expect stop (yellow)</summary>
    Ks2,

    /// <summary>Ks1 Blinking - Proceed with speed restriction (green blinking)</summary>
    Ks1Blink,

    /// <summary>Kennlicht - Signal operationally switched off (white top)</summary>
    Kennlicht,

    /// <summary>Dunkel - Signal off (all dark)</summary>
    Dunkel,

    /// <summary>Ra12/Sh1 - Shunting allowed (2x white diagonal)</summary>
    Ra12,

    /// <summary>Zs1 - Replacement signal (white blinking)</summary>
    Zs1,

    /// <summary>Zs7 - Caution signal (3x yellow triangular)</summary>
    Zs7
}

/// <summary>
/// Switch position (current state).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SwitchPosition
{
    /// <summary>Straight position (Grundstellung)</summary>
    Straight,

    /// <summary>Diverging left position</summary>
    DivergingLeft,

    /// <summary>Diverging right position (for three-way switches)</summary>
    DivergingRight
}

/// <summary>
/// Element state for a signal box item.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SignalBoxElementState
{
    /// <summary>Free section (default).</summary>
    Free,

    /// <summary>Occupied section.</summary>
    Occupied,

    /// <summary>Route set.</summary>
    RouteSet,

    /// <summary>Route clearing.</summary>
    RouteClearing,

    /// <summary>Blocked section.</summary>
    Blocked,

    /// <summary>Fault condition.</summary>
    Fault
}

// ═══════════════════════════════════════════════════════════════════════════
// VALUE OBJECTS - Grid and Position
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Grid configuration value object.
/// Encapsulates grid dimensions and cell size for the signal box plan.
/// Validates that all values are positive and non-zero.
/// </summary>
public record GridConfig(int Width, int Height, int CellSize)
{
    /// <summary>Width of the grid in cells.</summary>
    public int Width { get; init; } = ValidatePositive(Width, nameof(Width));

    /// <summary>Height of the grid in cells.</summary>
    public int Height { get; init; } = ValidatePositive(Height, nameof(Height));

    /// <summary>Cell size in pixels (for rendering).</summary>
    public int CellSize { get; init; } = ValidatePositive(CellSize, nameof(CellSize));

    /// <summary>
    /// Validates that a value is positive (non-zero and greater than zero).
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The parameter name for error reporting.</param>
    /// <returns>The validated value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if value is zero or negative.</exception>
    private static int ValidatePositive(int value, string paramName)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, paramName);
        return value;
    }
}

/// <summary>
/// Grid position value object.
/// Encapsulates X/Y coordinates on the signal box plan grid.
/// Validates that coordinates are non-negative.
/// Cell [0,0] is at top-left.
/// </summary>
public record GridPosition(int X, int Y)
{
    /// <summary>Column position (horizontal).</summary>
    public int X { get; init; } = ValidateNonNegative(X, nameof(X));

    /// <summary>Row position (vertical).</summary>
    public int Y { get; init; } = ValidateNonNegative(Y, nameof(Y));

    /// <summary>
    /// Validates that a value is non-negative (zero or greater).
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The parameter name for error reporting.</param>
    /// <returns>The validated value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if value is negative.</exception>
    private static int ValidateNonNegative(int value, string paramName)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value, paramName);
        return value;
    }
}
