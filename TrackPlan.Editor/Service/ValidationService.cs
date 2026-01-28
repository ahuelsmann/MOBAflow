// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Editor.Service;

using Moba.TrackLibrary.Base.TrackSystem;
using Moba.TrackPlan.Constraint;
using Moba.TrackPlan.Graph;

public sealed class ValidationService
{
    private readonly ITrackCatalog _catalog;

    public ValidationService(ITrackCatalog catalog)
    {
        _catalog = catalog;
    }

    public IReadOnlyList<ConstraintViolation> Validate(TopologyGraph graph)
    {
        var constraints = new ITopologyConstraint[]
        {
            new GeometryConnectionConstraint(_catalog),
            new DuplicateFeedbackPointNumberConstraint()
        };

        return graph.Validate(constraints).ToList();
    }

    // bestehende Serialize/Deserialize kannst du hier lassen
}