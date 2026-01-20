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
    /// Grid width in cells.
    /// </summary>
    public int GridWidth { get; set; } = 20;

    /// <summary>
    /// Grid height in cells.
    /// </summary>
    public int GridHeight { get; set; } = 12;

    /// <summary>
    /// Cell size in pixels (for rendering).
    /// </summary>
    public int CellSize { get; set; } = 60;

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
    /// Source connection point (0=North, 1=East, 2=South, 3=West, 4=NE, 5=SE, 6=SW, 7=NW).
    /// </summary>
    public int FromPoint { get; set; }

    /// <summary>
    /// Target element ID.
    /// </summary>
    public Guid ToElementId { get; set; }

    /// <summary>
    /// Target connection point.
    /// </summary>
    public int ToPoint { get; set; }
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
    public Dictionary<Guid, string> SwitchPositions { get; set; } = [];
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
