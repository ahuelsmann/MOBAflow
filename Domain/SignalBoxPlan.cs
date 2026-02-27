// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Domain;

using System.Text.Json.Serialization;

/// <summary>
/// SignalBoxPlan (track diagram) - Aggregate Root for railway interlocking topology.
/// Provides validated mutation methods (AddElement, RemoveElement, etc.) that enforce
/// invariants such as cell uniqueness, referential integrity, and cascading deletes.
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
    /// Prefer AddElement/RemoveElement for validated mutations.
    /// </summary>
    public List<SbElement> Elements { get; set; } = [];

    /// <summary>
    /// Connections between elements (topology).
    /// Prefer AddConnection/RemoveConnection for validated mutations.
    /// </summary>
    public List<SignalBoxConnection> Connections { get; set; } = [];

    /// <summary>
    /// Routes defined for this plan.
    /// Prefer AddRoute/RemoveRoute for validated mutations.
    /// </summary>
    public List<SignalBoxRoute> Routes { get; set; } = [];

    // ═══════════════════════════════════════════════════════════════════════
    // Aggregate Methods - Validated mutations with invariant protection
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Adds an element to the plan. Validates that the grid cell is not already occupied.
    /// </summary>
    /// <param name="element">The element to add</param>
    /// <exception cref="ArgumentNullException">Thrown when element is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when the target cell is already occupied</exception>
    public void AddElement(SbElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        if (Elements.Any(e => e.X == element.X && e.Y == element.Y))
        {
            throw new InvalidOperationException(
                $"Cell [{element.X},{element.Y}] is already occupied. One element per cell maximum.");
        }

        Elements.Add(element);
    }

    /// <summary>
    /// Removes an element and cascades deletion to all connections and routes referencing it.
    /// </summary>
    /// <param name="elementId">The ID of the element to remove</param>
    /// <returns>True if the element was found and removed, false otherwise</returns>
    public bool RemoveElement(Guid elementId)
    {
        var element = Elements.FirstOrDefault(e => e.Id == elementId);
        if (element is null)
            return false;

        Elements.Remove(element);

        // Cascade: remove all connections referencing this element
        Connections.RemoveAll(c => c.FromElementId == elementId || c.ToElementId == elementId);

        // Cascade: remove routes that reference this element
        Routes.RemoveAll(r =>
            r.StartSignalId == elementId ||
            r.EndSignalId == elementId ||
            r.ElementIds.Contains(elementId) ||
            r.SwitchPositions.ContainsKey(elementId));

        return true;
    }

    /// <summary>
    /// Finds an element by its ID.
    /// </summary>
    /// <param name="elementId">The element ID to search for</param>
    /// <returns>The element if found, null otherwise</returns>
    public SbElement? FindElement(Guid elementId)
    {
        return Elements.FirstOrDefault(e => e.Id == elementId);
    }

    /// <summary>
    /// Adds a connection between two elements. Validates that both referenced elements exist.
    /// </summary>
    /// <param name="connection">The connection to add</param>
    /// <exception cref="ArgumentNullException">Thrown when connection is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when a referenced element does not exist</exception>
    public void AddConnection(SignalBoxConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        if (Elements.All(e => e.Id != connection.FromElementId))
        {
            throw new InvalidOperationException(
                $"Source element {connection.FromElementId} does not exist in the plan.");
        }

        if (Elements.All(e => e.Id != connection.ToElementId))
        {
            throw new InvalidOperationException(
                $"Target element {connection.ToElementId} does not exist in the plan.");
        }

        Connections.Add(connection);
    }

    /// <summary>
    /// Removes a connection between two elements.
    /// </summary>
    /// <param name="fromElementId">Source element ID</param>
    /// <param name="toElementId">Target element ID</param>
    /// <returns>True if a connection was found and removed, false otherwise</returns>
    public bool RemoveConnection(Guid fromElementId, Guid toElementId)
    {
        var connection = Connections.FirstOrDefault(c =>
            c.FromElementId == fromElementId && c.ToElementId == toElementId);

        return connection is not null && Connections.Remove(connection);
    }

    /// <summary>
    /// Adds a route to the plan. Validates that start/end signals exist and are SbSignal elements,
    /// and that all referenced elements and switches exist.
    /// </summary>
    /// <param name="route">The route to add</param>
    /// <exception cref="ArgumentNullException">Thrown when route is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when referenced elements are missing or invalid</exception>
    public void AddRoute(SignalBoxRoute route)
    {
        ArgumentNullException.ThrowIfNull(route);

        if (FindElement(route.StartSignalId) is not SbSignal)
        {
            throw new InvalidOperationException(
                $"Start signal {route.StartSignalId} does not exist or is not a signal element.");
        }

        if (FindElement(route.EndSignalId) is not SbSignal)
        {
            throw new InvalidOperationException(
                $"End signal {route.EndSignalId} does not exist or is not a signal element.");
        }

        var missingElementIds = route.ElementIds
            .Where(id => Elements.All(e => e.Id != id))
            .ToList();

        if (missingElementIds.Count > 0)
        {
            throw new InvalidOperationException(
                $"Route references non-existent elements: {string.Join(", ", missingElementIds)}");
        }

        var missingSwitchIds = route.SwitchPositions.Keys
            .Where(id => Elements.All(e => e.Id != id) || FindElement(id) is not SbSwitch)
            .ToList();

        if (missingSwitchIds.Count > 0)
        {
            throw new InvalidOperationException(
                $"Route references non-existent or non-switch elements for switch positions: {string.Join(", ", missingSwitchIds)}");
        }

        Routes.Add(route);
    }

    /// <summary>
    /// Removes a route by its ID.
    /// </summary>
    /// <param name="routeId">The ID of the route to remove</param>
    /// <returns>True if the route was found and removed, false otherwise</returns>
    public bool RemoveRoute(Guid routeId)
    {
        var route = Routes.FirstOrDefault(r => r.Id == routeId);
        return route is not null && Routes.Remove(route);
    }

    /// <summary>
    /// Removes all elements, connections, and routes from the plan.
    /// </summary>
    public void Clear()
    {
        Elements.Clear();
        Connections.Clear();
        Routes.Clear();
    }
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
/// Switch element with DCC address and position.
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
/// Supports both traditional aspects and extended multiplex-decoders (up to 256 states).
/// </summary>
public sealed record SbSignal : SbElement
{
    /// <summary>DCC address for Z21 control (0 = not configured).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Address { get; set; }

    /// <summary>Signal system type (Ks, Hv, Hl, Form, Sv).</summary>
    public SignalSystemType SignalSystem { get; set; } = SignalSystemType.Ks;

    /// <summary>Current signal aspect (traditional aspect-based display).</summary>
    public SignalAspect SignalAspect { get; set; } = SignalAspect.Hp0;

    // ==================== MULTIPLEX CONFIGURATION ====================

    /// <summary>
    /// Indicates whether this signal uses a multiplex decoder (e.g., 5229).
    /// When true, signals are controlled via turnout commands based on the 5229.md address tables.
    /// When false, traditional DCC turnout/aspect commands are used.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsMultiplexed { get; set; } = false;

    /// <summary>
    /// Viessmann article number of the multiplexer decoder (e.g., "5229", "52292").
    /// Used to look up the address mapping and aspect configuration.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? MultiplexerArticleNumber { get; set; }

    /// <summary>
    /// Viessmann article number of the main signal (e.g., "4046").
    /// Determines which aspect mapping table is applied.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? MainSignalArticleNumber { get; set; }

    /// <summary>
    /// Viessmann article number of the distant signal (e.g., "4040").
    /// Stored for reference when a synchronized distant signal is configured.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? DistantSignalArticleNumber { get; set; }

    /// <summary>
    /// Base DCC address for the multiplex decoder.
    /// Additional addresses are calculated relative to this base using the tables in 5229.md.
    /// Example (4046):
    ///   [B]   red (-) / green (+)
    ///   [B+1] red (-) / green (+)
    ///   [B+2] red (-) / green (+)
    ///   [B+3] red (-) / green (+)
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int BaseAddress { get; set; }

    /// <summary>
    /// Last computed turnout activation (1 = green (+), 0 = red (-)).
    /// This is derived from the multiplexer mapping for status display.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int ExtendedAccessoryValue { get; set; } = 0;
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
/// A route in the signal box.
/// </summary>
public class SignalBoxRoute
{
    /// <summary>
    /// Gets or sets the unique identifier of the route.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Route name (e.g., "F1" for route 1).
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
    /// <summary>Straight position (normal position)</summary>
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
