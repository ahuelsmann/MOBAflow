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
    /// All elements in the signal box plan.
    /// </summary>
    public List<SignalBoxPlanElement> Elements { get; set; } = [];

    /// <summary>
    /// Connections between elements (topology).
    /// </summary>
    public List<SignalBoxConnection> Connections { get; set; } = [];

    /// <summary>
    /// Routes (Fahrstrassen) defined for this plan.
    /// </summary>
    public List<SignalBoxRoute> Routes { get; set; } = [];
}

/// <summary>
/// An element in the signal box plan (track, switch, signal, etc.).
/// </summary>
public class SignalBoxPlanElement
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Element type (see SignalBoxSymbol enum).
    /// </summary>
    public SignalBoxSymbol Symbol { get; set; }

    /// <summary>
    /// Grid position X (column).
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// Grid position Y (row).
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// Rotation in degrees (0, 45, 90, 135, 180, 225, 270, 315).
    /// </summary>
    public int Rotation { get; set; }

    /// <summary>
    /// Display name (e.g., "W1" for Weiche 1, "N1" for Signal N1).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// DCC address for Z21 control (0 = not configured).
    /// </summary>
    public int Address { get; set; }

    /// <summary>
    /// Feedback address for occupancy detection (0 = not configured).
    /// </summary>
    public int FeedbackAddress { get; set; }

        /// <summary>
        /// Signal system type (for signals).
        /// </summary>
        public SignalSystemType SignalSystem { get; set; } = SignalSystemType.Ks;

        /// <summary>
        /// Current signal aspect (for signal elements).
        /// </summary>
        public SignalAspect SignalAspect { get; set; } = SignalAspect.Hp0;

        /// <summary>
        /// Current switch position (for switch elements).
        /// </summary>
        public SwitchPosition SwitchPosition { get; set; } = SwitchPosition.Straight;

        /// <summary>
        /// Additional properties as key-value pairs.
        /// </summary>
        public Dictionary<string, string> Properties { get; set; } = [];
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

// ═══════════════════════════════════════════════════════════════════════════
// SIGNAL BOX SYMBOLS - Complete DB (Deutsche Bahn) Symbol Set
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Complete symbol set for DB railway interlocking (Stellwerk).
/// Based on DS 301 (Signalbuch) and standard Gleisbild symbols.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SignalBoxSymbol
{
    // ───────────────────────────────────────────────────────────────────────
    // TRACK ELEMENTS (Gleise)
    // ───────────────────────────────────────────────────────────────────────
    
    /// <summary>Straight track (horizontal/vertical)</summary>
    TrackStraight,
    
    /// <summary>Curved track 45 degrees</summary>
    TrackCurve45,
    
    /// <summary>Curved track 90 degrees</summary>
    TrackCurve90,
    
    /// <summary>Track end / Buffer stop (Prellbock)</summary>
    TrackEnd,
    
    /// <summary>Track crossing (level crossing without connection)</summary>
    TrackCrossing,
    
    /// <summary>Bridge / Overpass symbol</summary>
    TrackBridge,
    
    /// <summary>Tunnel entrance</summary>
    TrackTunnel,

    // ───────────────────────────────────────────────────────────────────────
    // SWITCHES - Simple (Einfache Weichen)
    // ───────────────────────────────────────────────────────────────────────
    
    /// <summary>Simple switch left (EW links) - Einfache Weiche nach links</summary>
    SwitchSimpleLeft,
    
    /// <summary>Simple switch right (EW rechts) - Einfache Weiche nach rechts</summary>
    SwitchSimpleRight,
    
    /// <summary>Curved switch left (Bogenweiche links)</summary>
    SwitchCurvedLeft,
    
    /// <summary>Curved switch right (Bogenweiche rechts)</summary>
    SwitchCurvedRight,

    // ───────────────────────────────────────────────────────────────────────
    // SWITCHES - Complex (Komplexe Weichen)
    // ───────────────────────────────────────────────────────────────────────
    
    /// <summary>Three-way switch (Dreiwegeweiche / DWW)</summary>
    SwitchThreeWay,
    
    /// <summary>Double slip switch (Doppelkreuzungsweiche / DKW)</summary>
    SwitchDoubleSlip,
    
    /// <summary>Single slip switch (Einfache Kreuzungsweiche / EKW)</summary>
    SwitchSingleSlip,
    
    /// <summary>Diamond crossing (Kreuzung ohne Weiche)</summary>
    SwitchDiamond,
    
    /// <summary>Scissor crossing (Hosentraegerweiche)</summary>
    SwitchScissor,

    // ───────────────────────────────────────────────────────────────────────
    // MAIN SIGNALS - Ks System (Kombinationssignal)
    // ───────────────────────────────────────────────────────────────────────
    
    /// <summary>Ks main signal (Ks-Hauptsignal)</summary>
    SignalKsMain,
    
    /// <summary>Ks distant signal (Ks-Vorsignal)</summary>
    SignalKsDistant,
    
    /// <summary>Ks combination signal (Ks-Mehrabschnittssignal)</summary>
    SignalKsCombined,

    // ───────────────────────────────────────────────────────────────────────
    // MAIN SIGNALS - H/V System (Haupt-/Vorsignal)
    // ───────────────────────────────────────────────────────────────────────
    
    /// <summary>H/V main signal (Hp-Signal)</summary>
    SignalHvMain,
    
    /// <summary>H/V distant signal (Vr-Signal)</summary>
    SignalHvDistant,
    
    /// <summary>H/V combined main/distant (Hp+Vr kombiniert)</summary>
    SignalHvCombined,

    // ───────────────────────────────────────────────────────────────────────
    // MAIN SIGNALS - Hl System (DDR/Ostdeutschland)
    // ───────────────────────────────────────────────────────────────────────
    
    /// <summary>Hl main signal</summary>
    SignalHlMain,
    
    /// <summary>Hl distant signal</summary>
    SignalHlDistant,

    // ───────────────────────────────────────────────────────────────────────
    // SEMAPHORE SIGNALS (Formsignale)
    // ───────────────────────────────────────────────────────────────────────
    
    /// <summary>Semaphore main signal (Form-Hauptsignal)</summary>
    SignalFormMain,
    
    /// <summary>Semaphore distant signal (Form-Vorsignal)</summary>
    SignalFormDistant,

    // ───────────────────────────────────────────────────────────────────────
    // SHUNTING SIGNALS (Rangiersignale)
    // ───────────────────────────────────────────────────────────────────────
    
    /// <summary>Shunting signal Ra 11 (Rangierhalttafel)</summary>
    SignalRa11,
    
    /// <summary>Shunting signal Ra 12 (Grenzzeichen)</summary>
    SignalRa12,
    
    /// <summary>Shunting light signal Sh (Rangiersignal Licht)</summary>
    SignalSh,
    
    /// <summary>Dwarf signal (Zwergsignal)</summary>
    SignalDwarf,

    // ───────────────────────────────────────────────────────────────────────
    // ADDITIONAL SIGNALS (Zusatzsignale)
    // ───────────────────────────────────────────────────────────────────────
    
    /// <summary>Speed indicator Zs 3 (Geschwindigkeitsanzeiger)</summary>
    SignalZs3,
    
    /// <summary>Speed pre-indicator Zs 3v (Geschwindigkeitsvoranzeiger)</summary>
    SignalZs3v,
    
    /// <summary>Direction indicator Zs 2 (Richtungsanzeiger)</summary>
    SignalZs2,
    
    /// <summary>Replacement signal Zs 1 (Ersatzsignal)</summary>
    SignalZs1,
    
    /// <summary>Caution signal Zs 7 (Vorsichtsignal)</summary>
    SignalZs7,
    
    /// <summary>Counter-direction signal Zs 8 (Gegengleisanzeiger)</summary>
    SignalZs8,

    // ───────────────────────────────────────────────────────────────────────
    // PROTECTION SIGNALS (Schutzsignale)
    // ───────────────────────────────────────────────────────────────────────
    
    /// <summary>Block signal (Blocksignal)</summary>
    SignalBlock,
    
    /// <summary>Exit signal (Ausfahrsignal)</summary>
    SignalExit,
    
    /// <summary>Entry signal (Einfahrsignal)</summary>
    SignalEntry,

    // ───────────────────────────────────────────────────────────────────────
    // TRAIN PROTECTION (Zugbeeinflussung)
    // ───────────────────────────────────────────────────────────────────────
    
    /// <summary>PZB magnet (Indusi) 500Hz</summary>
    Pzb500,
    
    /// <summary>PZB magnet (Indusi) 1000Hz</summary>
    Pzb1000,
    
    /// <summary>PZB magnet (Indusi) 2000Hz</summary>
    Pzb2000,
    
    /// <summary>ETCS balise</summary>
    EtcsBalise,

    // ───────────────────────────────────────────────────────────────────────
    // INFRASTRUCTURE (Infrastruktur)
    // ───────────────────────────────────────────────────────────────────────
    
    /// <summary>Platform (Bahnsteig)</summary>
    Platform,
    
    /// <summary>Platform edge (Bahnsteigkante)</summary>
    PlatformEdge,
    
    /// <summary>Level crossing (Bahnuebergang / BUEUE)</summary>
    LevelCrossing,
    
    /// <summary>Track block indicator</summary>
    TrackBlock,
    
    /// <summary>Derail / Catch point (Gleissperre)</summary>
    Derail,

    // ───────────────────────────────────────────────────────────────────────
    // DETECTION & FEEDBACK (Erkennung & Rueckmeldung)
    // ───────────────────────────────────────────────────────────────────────
    
    /// <summary>Axle counter (Achszaehler)</summary>
    AxleCounter,
    
    /// <summary>Track circuit boundary (Gleisstromkreis-Grenze)</summary>
    TrackCircuit,
    
    /// <summary>Feedback point / Detector</summary>
    Detector,

    // ───────────────────────────────────────────────────────────────────────
    // LABELS & MARKERS
    // ───────────────────────────────────────────────────────────────────────
    
    /// <summary>Text label</summary>
    Label,
    
    /// <summary>Track number marker (Gleisnummer)</summary>
    TrackNumber,
    
    /// <summary>Kilometer marker (Kilometrierung)</summary>
    KilometerMarker,
    
    /// <summary>Station name</summary>
    StationName
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
