// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend;

/// <summary>
/// Centralized constants for Z21 digital command station configuration.
/// </summary>
public static class Z21Constants
{
    /// <summary>
    /// Default IP address for Z21 connections (typical home network configuration).
    /// </summary>
    public const string DefaultIpAddress = "192.168.0.111";

    /// <summary>
    /// Default UDP port for Z21 communication.
    /// </summary>
    public const int DefaultPort = 21105;

    /// <summary>
    /// Localhost IP address for testing without physical Z21 hardware.
    /// </summary>
    public const string LocalhostIpAddress = "127.0.0.1";
}

