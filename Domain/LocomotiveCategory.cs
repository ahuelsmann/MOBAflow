// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

/// <summary>
/// Represents a category of locomotives (e.g., electric locomotives, ICE trains).
/// Pure data object for organizing master data.
/// </summary>
public class LocomotiveCategory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LocomotiveCategory"/> class with an empty series list.
    /// </summary>
    public LocomotiveCategory()
    {
        Series = [];
    }

    /// <summary>
    /// Name of the category (e.g., steam locomotives, electric locomotives).
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// List of locomotive series in this category.
    /// </summary>
    public List<LocomotiveSeries> Series { get; set; }
}
