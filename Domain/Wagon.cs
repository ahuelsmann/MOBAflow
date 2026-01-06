// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

using Enum;

/// <summary>
/// Upcoming feature: Represents a railroad wagon.
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

    /// <summary>
    /// Invoice date for purchase tracking (optional).
    /// </summary>
    public DateTime? InvoiceDate { get; set; }

    /// <summary>
    /// Delivery date for purchase tracking (optional).
    /// </summary>
    public DateTime? DeliveryDate { get; set; }

    /// <summary>
    /// Relative path to wagon photo (stored in project photos folder).
    /// Example: "locomotives/{id}.jpg" or "wagons/{id}.png"
    /// </summary>
    public string? PhotoPath { get; set; }
}