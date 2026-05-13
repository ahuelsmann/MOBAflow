// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

/// <summary>
/// Represents a persistable 5x5 LED matrix image.
/// </summary>
public class MatrixImage
{
    public const int CellCount = 25;
    public const uint OffColorArgb = 0xFFD3D3D3;

    /// <summary>
    /// Initializes a new instance of the <see cref="MatrixImage"/> class.
    /// </summary>
    public MatrixImage()
    {
        Id = Guid.NewGuid();
        Name = string.Empty;
        Cells = Enumerable.Repeat(OffColorArgb, CellCount).ToList();
    }

    /// <summary>
    /// Gets or sets the stable identifier of the matrix image.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the display name of the matrix image.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the 25 cell colors encoded as ARGB values.
    /// </summary>
    public List<uint> Cells { get; set; }

    /// <summary>
    /// Ensures the cell list contains exactly 25 values.
    /// </summary>
    public void NormalizeCells()
    {
        Cells ??= [];

        while (Cells.Count < CellCount)
        {
            Cells.Add(OffColorArgb);
        }

        if (Cells.Count > CellCount)
        {
            Cells.RemoveRange(CellCount, Cells.Count - CellCount);
        }
    }
}
