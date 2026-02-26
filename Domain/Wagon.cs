// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

using Enum;

/// <summary>
/// Upcoming feature: Represents a railroad wagon.
/// </summary>
public class Wagon
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Wagon"/> class with a new identifier and default name.
    /// </summary>
    public Wagon()
    {
        Id = Guid.NewGuid();
        Name = "New Wagon";
    }

    /// <summary>
    /// Gets or sets the unique identifier of the wagon.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the display name of the wagon.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the position of the wagon in the train.
    /// </summary>
    public uint Pos { get; set; }

    /// <summary>
    /// Gets or sets the digital address of the wagon (for function decoders).
    /// </summary>
    public uint? DigitalAddress { get; set; }

    /// <summary>
    /// Gets or sets the manufacturer name of the wagon model.
    /// </summary>
    public string? Manufacturer { get; set; }

    /// <summary>
    /// Gets or sets the article number of the wagon model.
    /// </summary>
    public string? ArticleNumber { get; set; }

    /// <summary>
    /// Gets or sets the series designation of the wagon.
    /// </summary>
    public string? Series { get; set; }

    /// <summary>
    /// Gets or sets the primary color scheme of the wagon.
    /// </summary>
    public ColorScheme? ColorPrimary { get; set; }

    /// <summary>
    /// Gets or sets the secondary color scheme of the wagon.
    /// </summary>
    public ColorScheme? ColorSecondary { get; set; }

    /// <summary>
    /// Gets or sets additional details of the wagon.
    /// </summary>
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