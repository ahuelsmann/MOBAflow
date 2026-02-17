// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

using System.Text.Json.Serialization;
using Enum;

/// <summary>
/// Upcoming feature: Represents a locomotive.
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

    public ColorScheme? ColorPrimary { get; set; }

    public ColorScheme? ColorSecondary { get; set; }

    public bool IsPushing { get; set; }

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