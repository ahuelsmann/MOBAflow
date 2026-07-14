// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.TrackLibrary.Base;

/// <summary>Library-neutral path command consumed by renderers.</summary>
public interface ITrackPathCommand;

/// <summary>Moves the current point without drawing.</summary>
public interface ITrackMoveTo : ITrackPathCommand
{
    double X { get; }
    double Y { get; }
}

/// <summary>Draws a straight line to the supplied point.</summary>
public interface ITrackLineTo : ITrackPathCommand
{
    double X { get; }
    double Y { get; }
}

/// <summary>Draws an arc to the supplied endpoint.</summary>
public interface ITrackArcTo : ITrackPathCommand
{
    double EndX { get; }
    double EndY { get; }
    double Radius { get; }
    bool Clockwise { get; }
    bool LargeArc { get; }
}

/// <summary>Geometry helpers that do not depend on a concrete track system.</summary>
public static class TrackPathGeometry
{
    public static (double MinX, double MinY, double MaxX, double MaxY) GetBounds(
        IReadOnlyList<ITrackPathCommand> path)
    {
        ArgumentNullException.ThrowIfNull(path);

        double minX = 0, minY = 0, maxX = 0, maxY = 0;
        double x = 0, y = 0;
        var hasPoint = false;

        foreach (var command in path)
        {
            switch (command)
            {
                case ITrackMoveTo move:
                    x = move.X;
                    y = move.Y;
                    break;
                case ITrackLineTo line:
                    x = line.X;
                    y = line.Y;
                    break;
                case ITrackArcTo arc:
                    x = arc.EndX;
                    y = arc.EndY;
                    break;
            }

            hasPoint = true;
            minX = Math.Min(minX, x);
            minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x);
            maxY = Math.Max(maxY, y);
        }

        return hasPoint ? (minX, minY, maxX, maxY) : (0, 0, 1, 1);
    }
}
