// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Configuration;

/// <summary>
/// Display page specific settings for ESP communication.
/// </summary>
public class DisplaySettings
{
    /// <summary>
    /// Explicit ESP32 target IP for display UDP transport.
    /// Empty until the user configures a device; do not ship a guessed default.
    /// </summary>
    public string Esp32IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// UDP destination port defined by display protocol v1.0.
    /// </summary>
    public int Port { get; set; } = 4210;
}
