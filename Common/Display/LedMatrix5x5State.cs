// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Common.Display;

public sealed class LedMatrix5x5State
{
    public const int CellCount = 25;
    public const uint OffColorArgb = 0xFFD3D3D3;

    private readonly uint[] cells = Enumerable.Repeat(OffColorArgb, CellCount).ToArray();

    public uint GetCellColorArgb(int index)
    {
        return IsValidIndex(index) ? cells[index] : OffColorArgb;
    }

    public bool SetCellColorArgb(int index, uint colorArgb)
    {
        if (!IsValidIndex(index))
        {
            return false;
        }

        cells[index] = colorArgb;
        return true;
    }

    public bool ClearCellColor(int index)
    {
        return SetCellColorArgb(index, OffColorArgb);
    }

    public static bool IsValidIndex(int index)
    {
        return index >= 0 && index < CellCount;
    }
}