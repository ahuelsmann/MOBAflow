// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Display.Protocol;

/// <summary>
/// Identifies a display protocol version.
/// </summary>
/// <param name="Major">Breaking protocol generation.</param>
/// <param name="Minor">Backward-compatible feature level within a major generation.</param>
public readonly record struct DisplayProtocolVersion(byte Major, byte Minor) : IComparable<DisplayProtocolVersion>
{
    /// <summary>
    /// Returns whether two versions can participate in minor-version negotiation.
    /// </summary>
    public bool HasCompatibleMajorVersion(DisplayProtocolVersion other) =>
        Major != 0 && Major == other.Major;

    /// <inheritdoc />
    public int CompareTo(DisplayProtocolVersion other)
    {
        var majorComparison = Major.CompareTo(other.Major);
        return majorComparison != 0 ? majorComparison : Minor.CompareTo(other.Minor);
    }

    /// <inheritdoc />
    public override string ToString() => $"{Major}.{Minor}";
}
