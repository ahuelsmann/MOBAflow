// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

using Enum;

/// <summary>
/// Represents a locomotive.
/// </summary>
public class Locomotive
{
    public Locomotive()
    {
        Id = Guid.NewGuid();
        Name = "New Locomotive";
    }

    public Guid Id { get; set; }
    public string Name { get; set; }

    /// <summary>
    /// The position is relevant for double traction and describes the position of the locomotive within a train.
    /// </summary>
    public uint Pos { get; set; } = 1;

    public uint? DigitalAddress { get; set; }

    public string? Manufacturer { get; set; }

    public string? ArticleNumber { get; set; }

    public string? Series { get; set; }

    public ColorScheme? ColorPrimary { get; set; }

    public ColorScheme? ColorSecondary { get; set; }

    public bool IsPushing { get; set; }

    public Details? Details { get; set; }
}