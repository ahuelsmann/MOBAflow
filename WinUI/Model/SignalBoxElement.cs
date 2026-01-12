// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Model;

/// <summary>
/// Represents an element in the signal box track diagram (Gleisbild).
/// Elements include tracks, switches (Weichen), and signals.
/// </summary>
public class SignalBoxElement
{
    /// <summary>
    /// Unique identifier for this element.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Type of the element (Track, Switch, Signal, etc.)
    /// </summary>
    public SignalBoxElementType Type { get; set; }

    /// <summary>
    /// Grid position X (column).
    /// </summary>
    public int GridX { get; set; }

    /// <summary>
    /// Grid position Y (row).
    /// </summary>
    public int GridY { get; set; }

    /// <summary>
    /// Rotation angle in degrees (0, 45, 90, 135, 180, 225, 270, 315).
    /// </summary>
    public int Rotation { get; set; }

    /// <summary>
    /// Current state of the element.
    /// </summary>
    public SignalBoxElementState State { get; set; } = SignalBoxElementState.Free;

    /// <summary>
    /// Display name or address.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// DCC address for Z21 control (0 = not configured).
    /// </summary>
    public int Address { get; set; }

    /// <summary>
    /// For switches: Current switch position.
    /// </summary>
    public SwitchPosition SwitchPosition { get; set; } = SwitchPosition.Straight;

    /// <summary>
    /// For signals: Current signal aspect.
    /// </summary>
    public SignalAspect SignalAspect { get; set; } = SignalAspect.Hp0;

    /// <summary>
    /// Signal system type (for signals only).
    /// </summary>
    public SignalSystem SignalSystem { get; set; } = SignalSystem.Ks;
}

/// <summary>
/// Types of signal box elements.
/// </summary>
public enum SignalBoxElementType
{
    // Track elements
    TrackStraight,
    TrackCurve45,
    TrackCurve90,
    TrackEndStop,      // Prellbock

    // Switch elements (Weichen)
    SwitchLeft,        // Weiche links
    SwitchRight,       // Weiche rechts
    SwitchDouble,      // Doppelweiche / Kreuzungsweiche
    SwitchCrossing,    // Kreuzung

    // Signals - Generic
    SignalMain,        // Hauptsignal (generic)
    SignalDistant,     // Vorsignal (generic)
    SignalCombined,    // Kombinationssignal (generic)
    SignalShunting,    // Rangiersignal
    SignalSpeed,       // Geschwindigkeitsanzeiger (Zs 3)

    // Signals - H/V System (Hp/Vr)
    SignalHvHp,        // Hp-Signal (Hauptsignal H/V)
    SignalHvVr,        // Vr-Signal (Vorsignal H/V)

    // Signals - Ks System
    SignalKsMain,      // Ks-Hauptsignal
    SignalKsDistant,   // Ks-Vorsignal
    SignalKsCombined,  // Ks-Mehrabschnittssignal

    // Signals - Sv System (S-Bahn)
    SignalSvMain,      // Sv-Hauptsignal
    SignalSvDistant,   // Sv-Vorsignal

    // Other elements
    Platform,          // Bahnsteig
    FeedbackPoint,     // Rueckmelder
    Buffer,            // Prellbock
    Label              // Text-Label
}

/// <summary>
/// State of a track section or element.
/// </summary>
public enum SignalBoxElementState
{
    Free,              // Frei (gray)
    Occupied,          // Besetzt (red)
    RouteSet,          // Fahrstrasse gesetzt (green)
    RouteClearing,     // Fahrstrasse raeumt (yellow)
    Blocked,           // Gesperrt (blue)
    Fault              // Stoerung (white blinking)
}

/// <summary>
/// Switch position.
/// </summary>
public enum SwitchPosition
{
    Straight,          // Grundstellung / Geradeaus
    Diverging,         // Abzweigend
    Unknown            // Unbekannt
}

/// <summary>
/// Signal aspect (Signalbegriff).
/// Based on German railway signaling.
/// </summary>
public enum SignalAspect
{
    // Hauptsignale (Hp)
    Hp0,               // Halt (Rot)
    Hp1,               // Fahrt (Gruen)
    Hp2,               // Langsamfahrt (Gruen + Gelb)

    // Vorsignale (Vr)
    Vr0,               // Halt erwarten (Gelb + Gelb)
    Vr1,               // Fahrt erwarten (Gruen + Gruen)
    Vr2,               // Langsamfahrt erwarten (Gruen + Gelb)

    // Ks-Signale (Kombinationssignal)
    Ks1,               // Fahrt (Gruen)
    Ks1Blink,          // Fahrt mit Geschwindigkeitsbegrenzung (Gruen blinkend)
    Ks2,               // Halt erwarten (Gelb)

    // Rangiersignale
    Sh0,               // Halt (Rot + Rot)
    Sh1,               // Rangierfahrt erlaubt (Weiss + Weiss)

    // Zusatzsignale
    Zs1,               // Ersatzsignal (Weiss blinkend)
    Zs3,               // Geschwindigkeitsanzeiger
    Zs3v               // Geschwindigkeitsvoranzeiger
}

/// <summary>
/// Signal system type.
/// </summary>
public enum SignalSystem
{
    Ks,                // Kombinationssignal (modern, seit 1993)
    HV,                // H/V-System (Licht- und Formsignale, klassisch)
    Sv,                // Sv-System (S-Bahn Berlin/Hamburg)
    Form               // Formsignale (Flügelsignale)
}
