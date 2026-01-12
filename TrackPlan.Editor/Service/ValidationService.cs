// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Editor.Service;

using Moba.TrackPlan.Constraint;
using Moba.TrackPlan.Graph;
using Moba.TrackPlan.TrackSystem;

public sealed class ValidationService
{
    private readonly ITrackCatalog _catalog;

    public ValidationService(ITrackCatalog catalog)
    {
        _catalog = catalog;
    }

    public IReadOnlyList<ConstraintViolation> Validate(TopologyGraph graph)
    {
        graph.Constraints.Clear();

        graph.Constraints.Add(new GeometryConnectionConstraint(_catalog));
        graph.Constraints.Add(new DuplicateFeedbackPointNumberConstraint());
        // weitere Constraints bei Bedarf…

        return graph.Validate().ToList();
    }

    // bestehende Serialize/Deserialize kannst du hier lassen
}