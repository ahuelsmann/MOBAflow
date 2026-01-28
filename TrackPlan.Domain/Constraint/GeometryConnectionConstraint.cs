// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Constraint;

using Moba.TrackLibrary.Base.TrackSystem;
using Moba.TrackPlan.Graph;

/// <summary>
/// Validates that connected track ends have geometrically compatible angles,
/// so that only "befahrbare" connections are allowed.
/// </summary>
public sealed class GeometryConnectionConstraint : ITopologyConstraint
{
    private readonly ITrackCatalog _catalog;

    /// <summary>
    /// Maximum allowed deviation between two connected end angles in degrees.
    /// </summary>
    private const double MaxAngleDeviationDeg = 5.0;

    public GeometryConnectionConstraint(ITrackCatalog catalog)
    {
        _catalog = catalog;
    }

    public IEnumerable<ConstraintViolation> Validate(TopologyGraph graph)
    {
        var visitedPairs = new HashSet<(Guid, Guid)>();

        foreach (var edgeA in graph.Edges)
        {
            foreach (var edgeB in graph.Edges)
            {
                if (edgeA.Id == edgeB.Id)
                    continue;

                // vermeidet doppelte Meldungen für A–B und B–A
                var pairKey = edgeA.Id < edgeB.Id
                    ? (edgeA.Id, edgeB.Id)
                    : (edgeB.Id, edgeA.Id);

                if (!visitedPairs.Add(pairKey))
                    continue;

                foreach (var portA in edgeA.Connections)
                    foreach (var portB in edgeB.Connections)
                    {
                        // nur Ports am selben Knoten vergleichen
                        if (portA.Value.NodeId != portB.Value.NodeId)
                            continue;

                        var templateA = _catalog.GetById(edgeA.TemplateId);
                        var templateB = _catalog.GetById(edgeB.TemplateId);

                        // unbekannte Templates ignorieren
                        if (templateA is null || templateB is null)
                            continue;

                        var angleA = GetAngle(templateA, portA.Key);
                        var angleB = GetAngle(templateB, portB.Key);

                        var deviation = AngleDeviation(angleA, angleB);

                        if (deviation > MaxAngleDeviationDeg)
                        {
                            yield return new ConstraintViolation(
                                kind: "geometry-mismatch",
                                message: $"Unfahrbare Verbindung: {edgeA.TemplateId} ↔ {edgeB.TemplateId} (Δ={deviation:F1}°)",
                                affectedEdges: new[] { edgeA.Id, edgeB.Id }
                            );
                        }
                    }
            }
        }
    }

    private static double GetAngle(TrackTemplate template, string endId)
    {
        var end = template.Ends.FirstOrDefault(e => e.Id == endId)
                  ?? throw new InvalidOperationException(
                      $"TrackTemplate '{template.Id}' has no end with Id '{endId}'.");

        return end.AngleDeg;
    }

    /// <summary>
    /// Kleinste Winkelabweichung zwischen zwei Winkeln in Grad (0..180).
    /// </summary>
    private static double AngleDeviation(double aDeg, double bDeg)
    {
        var diff = NormalizeDeg(aDeg - bDeg);
        return diff > 180.0 ? 360.0 - diff : diff;
    }

    private static double NormalizeDeg(double angleDeg)
    {
        var result = angleDeg % 360.0;
        return result < 0 ? result + 360.0 : result;
    }
}
