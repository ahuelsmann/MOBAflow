// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Runtime;

/// <summary>
/// Compares locomotive fleet snapshots by value so runtime sync can skip redundant UI updates.
/// </summary>
public static class LocomotiveFleetSnapshotComparer
{
    /// <summary>
    /// Returns true when two snapshots describe the same fleet metadata.
    /// </summary>
    public static bool ContentEquals(LocomotiveFleetSnapshot left, LocomotiveFleetSnapshot right)
    {
        return left.LocomotiveId == right.LocomotiveId
               && left.Name == right.Name
               && left.DigitalAddress == right.DigitalAddress
               && left.PhotoPath == right.PhotoPath
               && SequenceEqual(left.FunctionSymbols, right.FunctionSymbols)
               && SequenceEqual(left.FunctionColors, right.FunctionColors)
               && SequenceEqual(left.FunctionLabels, right.FunctionLabels);
    }

    /// <summary>
    /// Returns true when two ordered fleet lists contain the same metadata in the same order.
    /// </summary>
    public static bool OrderedContentEquals(
        IReadOnlyList<LocomotiveFleetSnapshot> left,
        IReadOnlyList<LocomotiveFleetSnapshot> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (var index = 0; index < left.Count; index++)
        {
            if (!ContentEquals(left[index], right[index]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool SequenceEqual(IReadOnlyList<string>? left, IReadOnlyList<string>? right)
    {
        if (left is null || right is null)
        {
            return left == right;
        }

        if (left.Count != right.Count)
        {
            return false;
        }

        for (var index = 0; index < left.Count; index++)
        {
            if (left[index] != right[index])
            {
                return false;
            }
        }

        return true;
    }
}
