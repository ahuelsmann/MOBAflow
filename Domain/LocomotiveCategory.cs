// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

/// <summary>
/// Represents a category of locomotives (e.g., "Elektrolokomotiven", "ICE-Züge").
/// Pure data object for organizing master data.
/// </summary>
public class LocomotiveCategory
{
    public LocomotiveCategory()
    {
        Series = [];
    }

    /// <summary>
    /// Name of the category (e.g., "Dampflokomotiven", "Elektrolokomotiven").
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// List of locomotive series in this category.
    /// </summary>
    public List<LocomotiveSeries> Series { get; set; }
}
