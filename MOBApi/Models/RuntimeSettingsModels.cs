// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MOBApi.Models;

/// <summary>
/// Z21 endpoint pushed by MOBAflow WinUI for MOBAsmart clients.
/// </summary>
public sealed class RuntimeSettingsRequest
{
    public string? Z21IpAddress { get; set; }

    public int Z21Port { get; set; } = 21105;
}
