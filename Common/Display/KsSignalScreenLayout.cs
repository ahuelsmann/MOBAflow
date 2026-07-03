// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Display;

/// <summary>
/// Fixed design dimensions for the shared Ks signal screen grid (WinUI and MAUI).
/// Article 4046: main + distant signal with Zs3/Zs3v speed indicators.
/// </summary>
public static class KsSignalScreenLayout
{
    public const int ColumnCount = 4;

    public const double DesignWidth = 90;

    public const double Padding = 6;

    public const double CellSpacing = 3;

    public const double SmallLampDiameter = 12;

    public const double LargeLampDiameter = 18;

    public const double SmallRowHeight = 15;

    public const double LargeRowHeight = 21;

    public const double SpeedRowHeight = 40;

    public const double SpeedBoxWidth = 36;

    public const double SpeedBoxHeight = 28;

    public const double HeadCornerRadius = 4;

    /// <summary>Five lamp rows: 2 small + 2 large + 2 small, plus row spacing.</summary>
    public const double BaseLampGridHeight = (SmallRowHeight * 3) + (LargeRowHeight * 2) + (CellSpacing * 4);

    /// <summary>Full grid height including both speed-indicator rows (4046 maximum).</summary>
    public const double DesignHeight = BaseLampGridHeight + (SpeedRowHeight * 2) + (Padding * 2);

    /// <summary>Fixed column width so the inner grid fits <see cref="DesignWidth"/>.</summary>
    public static double ColumnWidth =>
        (DesignWidth - (Padding * 2) - (CellSpacing * (ColumnCount - 1))) / ColumnCount;

    /// <summary>Design height for the current speed-indicator visibility.</summary>
    public static double GetDesignHeight(bool showTopSpeed, bool showBottomSpeed)
    {
        var height = (Padding * 2) + BaseLampGridHeight;
        if (showTopSpeed)
        {
            height += SpeedRowHeight + CellSpacing;
        }

        if (showBottomSpeed)
        {
            height += SpeedRowHeight + CellSpacing;
        }

        return height;
    }
}
