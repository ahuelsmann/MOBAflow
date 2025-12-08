// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

using Moba.Domain.Enum;

/// <summary>
/// Represents a railroad wagon.
/// </summary>
public class Wagon
{
    public Wagon()
    {
        Id = Guid.NewGuid();
        Name = "New Wagon";
    }

    public Guid Id { get; set; }
    public string Name { get; set; }
    public uint Pos { get; set; }
    public uint? DigitalAddress { get; set; }
    public string? Manufacturer { get; set; }
    public string? ArticleNumber { get; set; }
    public string? Series { get; set; }
    public ColorScheme? ColorPrimary { get; set; }
    public ColorScheme? ColorSecondary { get; set; }
    public Details? Details { get; set; }
}