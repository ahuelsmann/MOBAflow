// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

using System.Text.Json.Serialization;
using Enum;

/// <summary>
/// Upcoming feature: Represents a locomotive.
/// </summary>
public class Locomotive
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Locomotive"/> class with a new identifier and default name.
    /// </summary>
    public Locomotive()
    {
        Id = Guid.NewGuid();
        Name = "New Locomotive";
    }

    /// <summary>
    /// Gets or sets the unique identifier of the locomotive.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the display name of the locomotive.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The position is relevant for double traction and describes the position of the locomotive within a train.
    /// </summary>
    public uint Pos { get; set; } = 1;

    /// <summary>
    /// Gets or sets the digital address of the locomotive (DCC address).
    /// </summary>
    public uint? DigitalAddress { get; set; }

    /// <summary>
    /// Gets or sets the manufacturer name of the locomotive model.
    /// </summary>
    public string? Manufacturer { get; set; }

    /// <summary>
    /// Gets or sets the article number of the locomotive model.
    /// </summary>
    public string? ArticleNumber { get; set; }

    /// <summary>
    /// Resolved reference to the locomotive series from the library (Vmax, Type, Epoch, etc.).
    /// Set at runtime when the series name can be matched; not persisted.
    /// </summary>
    [JsonIgnore]
    public LocomotiveSeries? LocomotiveSeriesRef { get; set; }

    /// <summary>
    /// Custom font symbol glyphs for function buttons F0–F20 on the Train Control page.
    /// Unicode codepoint strings (e.g. "\uE7B7"). Index 0 = F0, 1 = F1, … 20 = F20.
    /// Null or fewer than 21 entries: missing indices use default symbols.
    /// </summary>
    public List<string>? FunctionSymbols { get; set; }

    /// <summary>
    /// Gets or sets the primary color scheme of the locomotive.
    /// </summary>
    public ColorScheme? ColorPrimary { get; set; }

    /// <summary>
    /// Gets or sets the secondary color scheme of the locomotive.
    /// </summary>
    public ColorScheme? ColorSecondary { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the locomotive is pushing at the end of the train.
    /// </summary>
    public bool IsPushing { get; set; }

    /// <summary>
    /// Gets or sets additional details of the locomotive.
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
    /// Relative path to locomotive photo (stored in project photos folder).
    /// Example: "locomotives/{id}.jpg"
    /// </summary>
    public string? PhotoPath { get; set; }
}