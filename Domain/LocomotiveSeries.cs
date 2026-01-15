// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

/// <summary>
/// Represents a locomotive series from the library (e.g., "BR 103.1", "ICE 3").
/// Pure data object for master data.
/// </summary>
public class LocomotiveSeries
{
    /// <summary>
    /// Name/designation of the series (e.g., "BR 103.1", "ICE 4 (BR 412)").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Maximum speed in km/h.
    /// </summary>
    public int Vmax { get; set; }

    /// <summary>
    /// Type classification (e.g., "Elektrolok", "Dampflok", "Triebzug").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Railway epoch(s) when this series was in service (e.g., "IV-VI", "III").
    /// </summary>
    public string Epoch { get; set; } = string.Empty;

    /// <summary>
    /// Short description of the locomotive.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
